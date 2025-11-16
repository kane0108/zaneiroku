using System.Security.Cryptography;

namespace BlazorApp.Game.Battle
{
    /// <summary>
    /// バトルシーン：入力インテント処理
    /// </summary>
    public partial class BattleScene
    {
        /// <summary>
        /// インテント処理ハブ
        /// </summary>
        private void HandleIntent(IBattleIntent intent)
        {
            switch (CurrentPhase)
            {
                // --- 予約フェーズ: ターゲット選択 ---
                case BattlePhase.Reservation_SelectTarget:
                    if (intent is TestInputIntent)
                    {
                        ChangePhase(BattlePhase.Resolution);
                    }
                    else if (intent is CharacterTapIntent tap)
                    {
                        if (Characters.TryGetValue(tap.CharacterId, out var defender))
                        {
                            // プレイヤー側のみ有効
                            if (_ctx.CurrentActorId >= 0 &&
                                Characters.TryGetValue(_ctx.CurrentActorId, out var attacker) &&
                                attacker.Type == "Player")
                            {
                                if (defender.Type == "Enemy" && !defender.CurrentStats.IsDead)
                                {
                                    _ctx.CurrentTargetId = defender.ObjectId;

                                    ClearTargetIcons(); // ターゲットアイコンを消す

                                    // 陣形を変更
                                    ArrangeForDuel(_ctx.CurrentActorId, _ctx.CurrentTargetId);

                                    // 予約スロットを並べる
                                    SetupReservationSlots();

                                    ChangePhase(BattlePhase.Reservation_SelectAttack);
#if DEBUG
                                    Console.WriteLine($"対象決定: {_ctx.CurrentTargetId}");
#endif
                                }
                                else
                                {
#if DEBUG
                                    Console.WriteLine("無効: 味方キャラまたは死亡キャラをタップ");
#endif
                                }
                            }
                            else
                            {
#if DEBUG
                                Console.WriteLine("無効: 敵側予約フェーズでのキャラタップ");
#endif
                            }
                        }
                    }
                    break;

                // --- 予約フェーズ: 攻撃選択 ---
                case BattlePhase.Reservation_SelectAttack:
                    if (intent is TestInputIntent)
                    {
                        ChangePhase(BattlePhase.Resolution);
                    }
                    else if (intent is ConfirmInputTapIntent)
                    {
                        ChangePhase(BattlePhase.Prediction);
                    }
                    else if (intent is ReserveAttackIntent at)
                    {
                        CheckAndHideNinguIconIfUsed();

                        if (_ctx.ReserveQueue.Count < _ctx.CurrentMaxReservations)
                        {
                            var attacker = Characters[_ctx.CurrentActorId];
                            if (attacker.CurrentStats.RemainingHands[at.Attack] > 0)
                            {
                                attacker.CurrentStats.RemainingHands[at.Attack]--;

                                int slot = _ctx.ReserveQueue.Count;
                                _ctx.ReserveQueue.Add(new PlannedAction
                                {
                                    AttackerId = _ctx.CurrentActorId,
                                    TargetId = _ctx.CurrentTargetId,
                                    Attack = at.Attack,
                                    SlotIndex = slot,
                                    IsRevealedToDefender = (slot < _ctx.RevealCount)
                                });

                                // 公開枠なら攻撃アイコンを差し替え
                                SetReserveAttackIcon(at.Attack, slot);
#if DEBUG
                                Console.WriteLine($"{attacker.Name} {at.Attack} 残り: {attacker.CurrentStats.RemainingHands[at.Attack]}");
#endif
                            }
                            else
                            {
#if DEBUG
                                Console.WriteLine($"{attacker.Name} の {at.Attack} は残手数がありません！");
#endif
                            }
                        }
                    }
                    break;

                // --- 予測フェーズ ---
                case BattlePhase.Prediction:
                    if (intent is TestInputIntent)
                        ChangePhase(BattlePhase.Results);
                    break;

                // --- 予測フェーズ: 対応行動選択 ---
                case BattlePhase.Prediction_SelectAttack:
                    if (intent is TestInputIntent)
                    {
                        ChangePhase(BattlePhase.Results);
                    }
                    else if (intent is ChooseResponseIntent rsp)
                    {
                        CheckAndHideNinguIconIfUsed();

                        int slot = _ctx.PredictQueue.Count;
                        if (slot >= _ctx.ReserveQueue.Count)
                            return; // 全部埋まっているなら無視

                        var attacker = Characters[_ctx.CurrentActorId];

                        if (!Characters.TryGetValue(_ctx.CurrentTargetId, out var responder))
                            return;

                        // === 反撃無制限スキルチェック ===
                        bool hasUnlimitedCounter = HasUnlimitedCounterSkill(responder);

                        // --- 残手数チェック（スキル持ちなら常に可） ---
                        bool canUse = rsp.Response switch
                        {
                            ResponseType.CounterSlash => hasUnlimitedCounter || responder.CurrentStats.RemainingHands[AttackType.Slash] > 0,
                            ResponseType.CounterDown => hasUnlimitedCounter || responder.CurrentStats.RemainingHands[AttackType.Down] > 0,
                            ResponseType.Evade => true, // 回避は常に使用可
                            _ => false
                        };

                        if (!canUse) return;

                        // --- 残手数消費 ---
                        if (rsp.Response == ResponseType.CounterSlash && !hasUnlimitedCounter)
                            responder.CurrentStats.RemainingHands[AttackType.Slash]--;
                        else if (rsp.Response == ResponseType.CounterDown && !hasUnlimitedCounter)
                            responder.CurrentStats.RemainingHands[AttackType.Down]--;
                        else if (rsp.Response == ResponseType.Evade &&
                                    responder.CurrentStats.RemainingHands[AttackType.Thrust] > 0)
                        {
                            responder.CurrentStats.RemainingHands[AttackType.Thrust]--;
                        }

                        // 結果判定
                        var atk = _ctx.ReserveQueue[slot].Attack;
                        var outcome = JudgeOutcome(atk, rsp.Response, attacker, responder);

                        _ctx.PredictQueue.Add(new PredictedResponse
                        {
                            ResponderId = responder.ObjectId,
                            Response = rsp.Response,
                            SlotIndex = slot,
                            Outcome = outcome
                        });

                        AddPredictionIcon(rsp.Response, slot);
                        ShowAdvantageIcon(outcome, slot);

#if DEBUG
                        Console.WriteLine($"{responder.Name} 対応 {rsp.Response}");
#endif

                        // 全部埋まったら次フェーズへ
                        if (_ctx.PredictQueue.Count >= _ctx.ReserveQueue.Count)
                        {
                            ChangePhase(BattlePhase.Prediction_Confirm);
                        }
                    }
                    break;

                // --- 予測フェーズ確認 ---
                case BattlePhase.Prediction_Confirm:
                    if (intent is TestInputIntent)
                        ChangePhase(BattlePhase.Results);
                    else if (intent is ConfirmInputTapIntent)
                        ChangePhase(BattlePhase.Resolution);
                    break;

                // --- 解決フェーズ ---
                case BattlePhase.Resolution:
                    if (intent is TestInputIntent)
                        ChangePhase(BattlePhase.Results);
                    else if (intent is ConfirmResolutionIntent)
                        ChangePhase(BattlePhase.Results);
                    break;

                // --- リザルトフェーズ ---
                case BattlePhase.Results:
                    break;

                // --- テストフェーズ ---
                case BattlePhase.Test:
                    break;
            }
        }

        // === 陣形制御 ===

        /// <summary>
        /// 対峙する2キャラを中央に寄せる
        /// </summary>
        private void ArrangeForDuel(int attackerId, int defenderId)
        {
            float centerX = Common.CanvasWidth / 2f;
            float duelGap = -30f;
            float duelY = Common.CanvasHeight / 2f - 160f;

            foreach (var ch in Characters.Values)
            {
                if (ch.CurrentStats.IsDead) continue;

                var sp = ch.GetCurrentFrameSprite();
                float width = sp?.SourceRect.Width ?? 64;

                if (ch.ObjectId == attackerId)
                {
                    ch.BattlePosX = centerX - duelGap - width;
                    ch.BattlePosY = duelY;
                }
                else if (ch.ObjectId == defenderId)
                {
                    ch.BattlePosX = centerX + duelGap;
                    ch.BattlePosY = duelY;
                }
                else
                {
                    if (ch.Type == "Player")
                    {
                        ch.BattlePosX = ch.DefaultPosX - 40f;
                        ch.BattlePosY = ch.DefaultPosY;
                    }
                    else
                    {
                        ch.BattlePosX = ch.DefaultPosX + 40f;
                        ch.BattlePosY = ch.DefaultPosY;
                    }
                }
            }
        }

        /// <summary>
        /// 陣形を初期位置に戻す
        /// </summary>
        private void ResetFormation()
        {
            foreach (var ch in Characters.Values)
            {
                if (ch.CurrentStats.IsDead)
                {
                    // 死亡キャラは後方に退避
                    ch.BattlePosX = (ch.Type == "Player")
                        ? ch.DefaultPosX - 40f
                        : ch.DefaultPosX + 40f;
                    ch.BattlePosY = ch.DefaultPosY;
                }
                else
                {
                    ch.BattlePosX = ch.DefaultPosX;
                    ch.BattlePosY = ch.DefaultPosY;
                }
            }
        }
    }
}
