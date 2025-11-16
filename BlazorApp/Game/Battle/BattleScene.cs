using BlazorApp.Game.BackgroundFactory;
using BlazorApp.Game.CharacterFactory;
using BlazorApp.Game.UIObjectFactory;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace BlazorApp.Game.Battle
{
    /// <summary>
    /// バトルシーン本体（進行制御・AI・解決処理）
    /// </summary>
    public partial class BattleScene : Scene
    {
        // === フィールド ===

        /// <summary> 現在のフェーズ </summary>
        public BattlePhase CurrentPhase { get; private set; } = BattlePhase.StartOfBattle;

        /// <summary> フェーズ経過時間 </summary>
        private float _phaseTimer = 0f;

        /// <summary> バトル初期設定 </summary>
        private BattleSetup _setup = new();

        /// <summary> 戦闘進行用コンテキスト </summary>
        private readonly BattleContext _ctx = new();

        /// <summary> プレイヤー入力Intentキュー </summary>
        private readonly Queue<IBattleIntent> _inbox = new();
        public void SubmitIntent(IBattleIntent intent) => _inbox.Enqueue(intent);

        /// <summary> ATB管理 </summary>
        private AtbTracker _atb;

        /// <summary> ATBが溜まったキャラのキュー </summary>
        private readonly Queue<int> _ready = new();

        /// <summary> ATBバー（未使用 → 削除OK） </summary>
        // private readonly Dictionary<int, UIObject> _atbBars = new();

        /// <summary> ATBリセット待機時間 </summary>
        private float _resetDelay = -1f;

        /// <summary> フェーズ中のATBゲージ凍結値 </summary>
        private readonly Dictionary<int, float> _frozenFill01 = new();

        /// <summary> ラッチされたアクター（予約者） </summary>
        private int _latchedActorId = -1;

        /// <summary> ステータスパネルの参照キャッシュ </summary>
        private readonly Dictionary<int, StatusPanelRefs> _statusPanelRefs = new();

        /// <summary> 回避疲労カウント </summary>
        private Dictionary<int, int> _evadeCount = new();

        /// <summary> 解決中のスロット番号 </summary>
        private int _currentResolvingSlot = -1;

        /// <summary> 解決中スロットのハイライトUI </summary>
        private UIObject? _resolveHighlight;

        /// <summary> 遅延フェーズ遷移用タイマー </summary>
        private float _pendingPhaseDelay = 0f;

        /// <summary> 遅延遷移先フェーズ </summary>
        private BattlePhase _pendingNextPhase = BattlePhase.StartOfTurn;

        /// <summary> 解決アニメの速度倍率（1.0=通常, 0.1=超早送り） </summary>
        private float _resolutionSpeedMultiplier = 1.0f;

        /// <summary> グローバルアニメ速度倍率 </summary>
        public static float GlobalAnimationSpeedMultiplier { get; private set; } = 1.0f;

        private bool _isUsingNingu = false; // ★ 同時押し防止フラグ

        private readonly HashSet<int> _smokeConcealActive = new(); // 使用者ID記録

        private bool _isTutorialMode = false;

        /// <summary>
        /// チュートリアル進行度（0～）
        /// </summary>
        private int _tutorialProgress = 0;

        /// <summary> グローバルアニメ速度倍率を変更 </summary>
        public void SetAnimationSpeed(float multiplier)
        {
            GlobalAnimationSpeedMultiplier = multiplier;
        }

        // === 初期化 / 更新 ===

        /// <summary> バトル初期化 </summary>
        public void Initialize(BattleSetup setup)
        {
            _setup = setup ?? new BattleSetup();
            GameMain.Instance.OnFadeInCompleted += HandleFadeInCompleted;

            _isTutorialMode = (setup.StageName == "プロローグ" ? true : false);

            // 背景生成
            Background = _setup.Background;
        }

        /// <summary> 毎フレーム更新 </summary>
        public override void Update(float deltaTime)
        {
            try
            {
                base.Update(deltaTime);
                _phaseTimer += deltaTime;

                // 遅延フェーズ遷移処理
                if (_pendingPhaseDelay > 0f)
                {
                    _pendingPhaseDelay -= deltaTime;
                    if (_pendingPhaseDelay <= 0f)
                    {
                        ChangePhase(_pendingNextPhase);
                        _pendingPhaseDelay = 0f;
                    }
                    return; // 遷移待機中は他処理を中断
                }

                // ATB管理（ChooseActorフェーズのみ稼働）
                if (CurrentPhase == BattlePhase.ChooseActor && _atb != null)
                {
                    _atb.Tick(deltaTime, Characters);
                    while (_atb.TryPop(out var id)) _ready.Enqueue(id);
                }

                // ATBリセット待機
                if (_resetDelay > 0f)
                {
                    _resetDelay -= deltaTime;
                    if (_resetDelay <= 0f && _latchedActorId >= 0)
                    {
                        _atb?.ConsumeLatchedFull(_latchedActorId);
                        _latchedActorId = -1;
                        _resetDelay = -1f;
                    }
                }

                // 解決フェーズ中のハイライト更新
                if (CurrentPhase == BattlePhase.Resolution)
                {
                    UpdateResolveHighlight();
                }

                // 敵ターゲットアイコンの追従
                foreach (var icon in _targetIcons)
                {
                    var idStr = icon.Name.Replace("TargetIcon_", "");
                    if (int.TryParse(idStr, out int targetId) &&
                        Characters.TryGetValue(targetId, out var ch))
                    {
                        icon.PosX = ch.PosX + 0;
                        icon.PosY = ch.PosY + 60;
                    }
                }

                // フェーズごとの処理
                HandlePhaseUpdate();

                if (CurrentPhase == BattlePhase.PrologueScript)
                {
                    // 更新しない
                }
                else
                {
                    // ステータスパネル更新
                    UpdateStatusPanels(deltaTime);
                }

                // Intent処理
                while (_inbox.Count > 0)
                {
                    var intent = _inbox.Dequeue();
                    HandleIntent(intent);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine($"[ERROR] Update Exception: {ex}");
#endif
            }
        }

        /// <summary>
        /// 遅延付きフェーズ変更を予約
        /// </summary>
        private void ChangePhaseWithDelay(BattlePhase next, float delaySeconds)
        {
            _pendingPhaseDelay = delaySeconds;
            _pendingNextPhase = next;
        }

        /// <summary>
        /// フェーズ変更処理
        /// </summary>
        private void ChangePhase(BattlePhase next)
        {
            CurrentPhase = next;
            _phaseTimer = 0f;

            // === ATB凍結解除 / 更新 ===
            if (next == BattlePhase.EndTurn)
            {
                _frozenFill01.Clear();
            }
            else
            {
                if (_atb != null)
                {
                    foreach (var ch in Characters.Values.ToList())
                    {
                        float current = _atb.GetFill01(ch.ObjectId);
                        if (_frozenFill01.TryGetValue(ch.ObjectId, out var prev))
                            _frozenFill01[ch.ObjectId] = MathF.Max(prev, current);
                        else
                            _frozenFill01[ch.ObjectId] = current;

                        if (current >= 1f) _frozenFill01[ch.ObjectId] = 1f;
                    }
                }
            }

            // === フェーズ固有処理 ===
            HandlePhaseEnterAsync(next);

#if DEBUG
            Console.WriteLine($"ChangePhase: {CurrentPhase}");
#endif
        }

        /// <summary>
        /// フェードイン完了時処理
        /// </summary>
        private void HandleFadeInCompleted()
        {
            GameMain.Instance.OnFadeInCompleted -= HandleFadeInCompleted;
        }

        // === フェーズ処理 ===

        /// <summary>
        /// 各フェーズのUpdate処理本体
        /// </summary>
        private void HandlePhaseUpdate()
        {
            switch (CurrentPhase)
            {
                case BattlePhase.StartOfBattle:

                    _ = PlayEventScriptAsync("Title");
                    ChangePhaseWithDelay(BattlePhase.PrologueScript, 3.0f);
                    break;

                case BattlePhase.PrologueScript:
                    break;

                case BattlePhase.StartOfTurn:
                    StartOfTurn();
                    ChangePhase(BattlePhase.ChooseActor);
                    break;

                case BattlePhase.ChooseActor:
                    HandleChooseActorPhase();
                    break;

                case BattlePhase.Reservation:
                    HandleReservationPhase();
                    break;

                case BattlePhase.Reservation_SelectTarget:
                case BattlePhase.Reservation_SelectAttack:
                case BattlePhase.Prediction_SelectAttack:
                case BattlePhase.Prediction_Confirm:
                case BattlePhase.Resolution:
                case BattlePhase.Test:
                    // 入力や解決待ち → ここでは処理なし
                    break;

                case BattlePhase.Prediction:
                    HandlePredictionPhase();
                    break;

                case BattlePhase.Results:
                    HandleResultsPhase();
                    break;

                case BattlePhase.EndTurn:
                    ResetFormation();
                    CleanupForNextTurn();
                    ChangePhaseWithDelay(BattlePhase.StartOfTurn, 0.5f);
                    break;

                case BattlePhase.EndBattle:
                    GameMain.Instance.StartFadeTransition(GameState.Home);
                    break;
            }
        }

        /// <summary> ChooseActorフェーズの処理 </summary>
        private void HandleChooseActorPhase()
        {
            if (_ready.Count == 0) return;

            int candidateId;
            do
            {
                candidateId = _ready.Dequeue();
            }
            while (Characters[candidateId].CurrentStats.IsDead && _ready.Count > 0);

            if (!Characters[candidateId].CurrentStats.IsDead)
            {
                _ctx.CurrentActorId = candidateId;
                _ctx.PlayerIsReservationSide = (Characters[candidateId].Type == "Player");

#if DEBUG
                Console.WriteLine($"CurrentActorId: {_ctx.CurrentActorId}, PlayerSide: {_ctx.PlayerIsReservationSide}");
#endif

                ChangePhase(BattlePhase.Reservation);
            }
        }

        /// <summary> Reservationフェーズの処理 </summary>
        private void HandleReservationPhase()
        {
            if (Characters[_ctx.CurrentActorId].Type == "Player")
            {
                var actor = Characters[_ctx.CurrentActorId];
                bool noHands = actor.CurrentStats.RemainingHands.Values.All(v => v <= 0);

                if (noHands)
                {
#if DEBUG
                    Console.WriteLine("▶ 手数ゼロのため即スキップします");
#endif
                    // 回復処理を通すため Results へ飛ばす
                    ChangePhase(BattlePhase.Results);
                    return;
                }

                ChangePhase(BattlePhase.Reservation_SelectTarget);
            }
            else
            {
                RunEnemyReservationAI();
                ChangePhase(BattlePhase.Prediction);
            }
        }

        /// <summary> Predictionフェーズの処理 </summary>
        private void HandlePredictionPhase()
        {
            if (_ctx.ReserveQueue.Count == 0)
            {
                ChangePhase(BattlePhase.Resolution);
                return;
            }

            if (_ctx.PlayerIsReservationSide)
            {
                RunEnemyPredictionAI();
                ChangePhase(BattlePhase.Prediction_Confirm);
            }
            else
            {
                ChangePhase(BattlePhase.Prediction_SelectAttack);
            }
        }

        /// <summary> Resultsフェーズの処理 </summary>
        private void HandleResultsPhase()
        {
        }

        private async void UseNingu(Character user, Equipment eq)
        {
            // === 1. 使用条件チェック ===
            if (_isUsingNingu) return;
            if (_ninguUsed.Contains(user.ObjectId)) return;
            if (eq == null) return;

            _isUsingNingu = true;

            int actualEffect = 0;

            switch (eq.Id)
            {
                // === 治痕薬：赤ゲージ（残痕）を全回復（緑ゲージ化） ===
                case "治痕薬":
                    int raw = (int)MathF.Ceiling(user.BaseStats.MaxHP * 1.0f);
                    int convert = Math.Min(user.CurrentStats.FatalHP, user.BaseStats.MaxHP);

                    if (convert > 0)
                    {
                        user.CurrentStats.FatalHP -= convert;
                        user.CurrentStats.ResidualHP += convert;
                        actualEffect = convert;
                        await ShowHealPopupAsync(user, convert);
                    }
                    break;

                // === 治命水：赤ゲージのまま30%だけ回復 ===
                case "治命水":
                    int heal = Math.Min(user.BaseStats.MaxHP - user.CurrentStats.FatalHP - user.CurrentStats.ResidualHP,
                                        (int)MathF.Ceiling(user.BaseStats.MaxHP * 0.3f));

                    if (heal > 0)
                    {
                        user.CurrentStats.FatalHP += heal;
                        actualEffect = heal;
                        await ShowHealPopupAsync(user, heal);
                    }
                    break;

                case "幻煙玉":
                    // 攻撃対象（直前に選択中の敵）を取得
                    if (Characters.TryGetValue(_ctx.CurrentTargetId, out var defender))
                    {
                        // このキャラの予約攻撃を不可視化するフラグをセット
                        _smokeConcealActive.Add(user.ObjectId);
                        actualEffect = 1;

                        ShowSmokeEffect(defender.PosX + 45, defender.PosY + 95);

                        // ★ 予約UIを即更新（全スロットを?に変える）
                        SetupReservationSlots();
                    }
                    break;

                case "先見丹":
                    // 1. 公開数を最大に設定
                    _ctx.RevealCount = _ctx.CurrentMaxReservations;

                    // 2. 敵予約枠を全公開状態で再生成
                    SetupEnemyReservationSlots(forceFullReveal: true);

                    // 🔥 専用演出（目＋フラッシュ）
                    await ShowEyeFlashEffectAsync();

                    actualEffect = 1;
                    break;

                case "影分身の巻物":
                    // 予測ターン専用：使用者の回避失敗率を半減（＝回避成功率アップ）
                    user.CurrentStats.TempBuffs["EvasionFailRateMultiplier"] = 0.5f;

                    await ShowKageBunshinEffectAsync(user.PosX + 16, user.PosY + 70);

                    actualEffect = 1;
                    break;

                case "影縫いの巻物":
                    // 予約ターン専用：相手の回避成功率を半減（＝相手の回避率ダウン）
                    if (Characters.TryGetValue(_ctx.CurrentTargetId, out var enemy))
                    {
                        enemy.CurrentStats.TempBuffs["EvasionSuccessRateMultiplier"] = 0.5f;
                        actualEffect = 1;

                        await ShowKageNuiEffectAsync(enemy.PosX + 40, enemy.PosY + 50);
                    }
                    break;
            }

            // === 効果ゼロなら使用扱いにしない ===
            if (actualEffect <= 0)
            {
                await ShowErrorQuickAsync("使用効果無し");   // ★追加：右下に赤字で短時間表示

                _isUsingNingu = false;
                return;
            }

            // === 3. 使用済み化 ===
            _ninguUsed.Add(user.ObjectId);

            if (_ninguIcons.TryGetValue(user.ObjectId, out var icon))
            {
                icon.Opacity = 0.3f;
                icon.Enabled = false;
                icon.MarkDirty();
            }

            // 0.5秒だけ操作ロック（連続タップ防止）
            await Task.Delay(500);
            _isUsingNingu = false;
        }

        /// <summary>
        /// フェーズ遷移時の処理（ChangePhaseから呼ばれる）
        /// </summary>
        private async Task HandlePhaseEnterAsync(BattlePhase next)
        {
            if (next == BattlePhase.PrologueScript)
            {
                await PlayEventScriptAsync("Prologue");

                // ここで通常フェーズに復帰
                ChangePhase(BattlePhase.StartOfTurn);
            }

            if (next == BattlePhase.Reservation)
            {
                _latchedActorId = _ctx.CurrentActorId;
                _resetDelay = 0.3f;

                if (Characters.TryGetValue(_ctx.CurrentActorId, out var actor))
                {
                    _ctx.CurrentMaxReservations = actor.CurrentStats.MaxReservationPerTurn;
                }
            }

            if (next == BattlePhase.Reservation_SelectTarget)
            {
                HighlightActor(_ctx.CurrentActorId);

                if (Characters.TryGetValue(_ctx.CurrentActorId, out var actor)
                    && actor.Type == "Player")
                {
                    actor.BattlePosX = actor.DefaultPosX + 50;
                    ShowTargetIcons();
                }
            }
            else
            {
                ClearTargetIcons();
            }

            if (next == BattlePhase.Reservation_SelectAttack
                || next == BattlePhase.Prediction_SelectAttack)
            {
                ShowDuelMask();
            }
            else if (next == BattlePhase.Resolution)
            {
                foreach (var ch in Characters.Values)
                {
                    ch.CurrentStats.EndOfTurnHands = new Dictionary<AttackType, int>(ch.CurrentStats.RemainingHands);
                }

                var maskUIs = UIObjects.Values
                    .Where(u => u.Name.StartsWith("予約マスク_"))
                    .ToList();
                foreach (var ui in maskUIs)
                    RemoveUI(ui);
                _reserveMasks.Clear();

                HideDuelMask();
                _ = ResolveAllSlotsWithAnimationAsync();
            }

            if (next == BattlePhase.Prediction)
            {
                _smokeConcealActive.Clear();

                if (_ctx.PlayerIsReservationSide)
                {
                    if (_ctx.ReserveQueue.Count == 0)
                        SetupReservationSlots();
                }
                else
                {
                    SetupEnemyReservationSlots();
                }
            }

            if (next == BattlePhase.Prediction_Confirm)
            {
                RevealAllReservationIcons();
            }

            if (next == BattlePhase.Results)
            {
                var result = CheckBattleResult();

                if (result != BattleResult.None)
                {
                    ClearTurnIcons();

                    if (result == BattleResult.Victory)
                    {
                        await PlayEventScriptAsync("Victory");

                        if (_isTutorialMode)
                        {
                            // チュートリアルのあとは仮でホームへ
                            GameMain.Instance.StartFadeTransition(GameState.Home);
                            return;
                        }

                        // 探索盤からの報酬が設定されている場合は、戦闘勝利後に探索盤の最終リザルトを表示する
                        //    （報酬が0でも空配列の可能性があるため、Gold>0 またはアイテム数で判定）
                        var rewards = _setup?.Rewards;
                        if (rewards != null &&
                            (rewards.Gold > 0 || (rewards.ItemIds != null && rewards.ItemIds.Count > 0)))
                        {
                            // リザルト表示
                            await BlazorApp.Game.SceneResultHelper.ShowStageResultAsync(
                                this,
                                rewards,
                                true
                            );
                        }
                    }
                    else
                    {
                        await PlayEventScriptAsync("Defeat");

                        if (_isTutorialMode)
                        {
                            // チュートリアルで負けた場合はタイトルへ戻る
                            GameMain.Instance.StartFadeTransition(GameState.Title);
                            return;
                        }
                    }

                    ChangePhaseWithDelay(BattlePhase.EndBattle, 0.5f);
                }
                else
                {
                    HandleHandsRecovery();

                    ChangePhase(BattlePhase.EndTurn);
                }
            }

            if (next == BattlePhase.Resolution
               || (_ctx.PlayerIsReservationSide && next == BattlePhase.Prediction_Confirm))
            {
                SnapIconsToTarget();
            }

            if (next == BattlePhase.Reservation_SelectAttack ||
                    next == BattlePhase.Prediction_SelectAttack)
            {
                ShowNinguIconForActivePlayer();
            }
            else
            {
                foreach (var icon in _ninguIcons.Values.ToList())
                    RemoveUI(icon);
                _ninguIcons.Clear();
            }

            HandlePhaseUiVisibility(next);

            // === チュートリアルモード時のみ ===
            if (_isTutorialMode)
            {
                // ★進行度ごとに分岐
                switch (_tutorialProgress)
                {
                    case 0:
                        if (next == BattlePhase.Prediction_SelectAttack)
                        {
                            await ShowTutorialTipsAsync(new[] {
                                "敵の攻撃内容を予測し、対応行動を選択する。\n" +
                                "攻撃側の洞察力と受け側の翻弄力によって、\n"+
                                "どの程度手の内が読めるかが決まる。",

                                "この敵は【穿】(突き攻撃)を得意とする。\n" +
                                "反撃の隙はなく、受けると致命傷となるが、\n" +
                                "単調な動きであり、有効な対応は【回避】。\n" +
                                "ただし、回避は繰り返すと成功率が低下する。"
                                }, 1
                            );
                            _tutorialProgress++;
                        }
                        break;

                    case 1:
                        if (next == BattlePhase.Prediction_SelectAttack)
                        {
                            await ShowTutorialTipsAsync(new[] {
                                "この敵は【迅】(切り払い攻撃)を得意とする。\n"+
                                "素早い横斬りは回避が難しく、流れに合わせて\n"+
                                "同じ軌跡で斬り返す【迅返】による反撃が有効。"
                                }, 2
                            );
                            _tutorialProgress++;
                        }
                        break;

                    case 2:
                        if (next == BattlePhase.Prediction_SelectAttack)
                        {
                            await ShowTutorialTipsAsync(new[] {
                                "この敵は【剛】(切り下ろし攻撃)を得意とする。\n"+
                                "重い斬撃は力の芯を見極め、その瞬間を叩き折る\n" +
                                "【剛断】による反撃が有効。"
                                }, 3
                            );
                            _tutorialProgress++;
                        }
                        break;

                    case 3:
                        if (next == BattlePhase.Reservation_SelectAttack)
                        {
                            await ShowTutorialTipsAsync(new[] {
                                "こちらから仕掛ける場合、相手の対応予測が必要。\n" +
                                "この敵が繰り出せる対応は単調なので、適切に\n" +
                                "有効打を叩き込めば良い。",

                                "一度に最大4回の行動を予約できるが、消費手数は\n" +
                                "次回の立ち回りに持ち越されるため注意が必要。\n"+
                                "尚、【穿】の残手数が無い状態でも回避行動は\n"+
                                "可能であるが、成功率が著しく低下する。"
                                }, 4
                            );
                            _tutorialProgress++;
                        }
                        break;

                    case 4:
                        if (next == BattlePhase.Reservation_SelectAttack)
                        {
                            await ShowTutorialTipsAsync(new[] {
                                "残痕を刻み致命傷へと昇華させる立ち回りが必要。\n"+
                                "攻撃の種類により、与える傷に特徴がある。\n"+
                                "【穿】残痕に対して大きな致命傷を与える。\n"+
                                "【迅】致命傷に至らないが残痕を大きく刻む。\n"+
                                "【剛】残痕/致命を同時に与える。",

                                "敏捷が高い程に行動力が早く溜り、行動力が\n"+
                                "最大になった者から先手を打つことができる。"
                                }, 5
                            );
                            _tutorialProgress++;
                        }
                        break;
                }
            }
        }

        // === AI処理 ===

        /// <summary> 敵AIの予約行動選択 </summary>
        private void RunEnemyReservationAI()
        {
            var rand = new Random();

            if (!Characters.TryGetValue(_ctx.CurrentActorId, out var enemy) || enemy.Type != "Enemy")
                return;

            // --- ターゲットはランダム（生存プレイヤーから） ---
            var candidates = Characters.Values
                .Where(ch => ch.Type == "Player" && !ch.CurrentStats.IsDead)
                .ToList();
            if (!candidates.Any()) return;

            var target = candidates[rand.Next(candidates.Count)];
            _ctx.CurrentTargetId = target.ObjectId;

            ArrangeForDuel(_ctx.CurrentTargetId, _ctx.CurrentActorId);

            // === パラメータ（必要に応じて微調整） ===
            const int MinHandsToKeepForPrediction = 3; // 予測フェーズ用に最低限残す合計手数
            const int MaxSameStreak = 2;               // 同じ手の連続許容回数（2連続までOK）
            const float AbundanceBoost = 1.4f;         // 多く持っている手の重み補正
            const float RepeatPenalty = 0.35f;         // 直前(or直前直前)と同手を選ぶ際の重み減衰

            // 直近選択の履歴（連続判定用）
            var history = new List<AttackType>();

            // スロットを順に埋める
            for (int slot = 0; slot < _ctx.CurrentMaxReservations; slot++)
            {
                // 使える手が無ければ終了
                var usable = enemy.CurrentStats.RemainingHands
                    .Where(kv => kv.Value > 0)
                    .Select(kv => kv.Key)
                    .ToList();
                if (!usable.Any()) break;

                int totalRemain = enemy.CurrentStats.RemainingHands.Values.Sum();

                // 加重ランダムで選ぶ（既存のヘルパーを利用）
                var chosen = ChooseAttackWeighted(
                    usable,
                    enemy.CurrentStats.RemainingHands,
                    history,
                    MaxSameStreak,
                    AbundanceBoost,
                    RepeatPenalty,
                    rand
                );

                // この1手を入れた「後」の残合計を計算
                int afterTotal = totalRemain - 1;

                bool shouldStopAfterThis = false;
                if (slot >= 3) // 0-based: 0手目,1手目までは判定しない
                {
                    // まだ予約枠が残っているのに、入れると3手を割るならここで終了
                    shouldStopAfterThis = (afterTotal < MinHandsToKeepForPrediction) && ((slot + 1) < _ctx.CurrentMaxReservations);
                }

                if (shouldStopAfterThis)
                {
                    break;
                }

                // 消費して予約へ
                enemy.CurrentStats.RemainingHands[chosen]--;
                _ctx.ReserveQueue.Add(new PlannedAction
                {
                    AttackerId = enemy.ObjectId,
                    TargetId = target.ObjectId,
                    Attack = chosen,
                    SlotIndex = slot,
                    IsRevealedToDefender = false
                });
                AddReserveIcon(chosen, slot);

                // 履歴更新（連続抑制用）
                history.Add(chosen);
                if (history.Count > MaxSameStreak) history.RemoveAt(0);
            }
        }

        // --- 加重ランダム選択ヘルパー ---
        private static AttackType ChooseAttackWeighted(
            List<AttackType> usable,
            Dictionary<AttackType, int> remainHands,
            List<AttackType> recentHistory, // 直近（最大 MaxSameStreak 個）
            int maxSameStreak,
            float abundanceBoost,
            float repeatPenalty,
            Random rand)
        {
            // どの手が「多い」か（最大残数）
            int maxRemain = usable.Max(t => remainHands[t]);

            // 直近の“連続対象”を拾う（例：2連続以上は抑制対象）
            AttackType? last = recentHistory.LastOrDefault();
            int sameStreak = 0;
            if (recentHistory.Count >= 1 && last.HasValue)
            {
                sameStreak = recentHistory.Count(h => h == last.Value);
            }

            // 重み生成
            var weights = new List<(AttackType t, double w)>();
            foreach (var t in usable)
            {
                double w = remainHands[t];

                // 多く持っている“傾向”を優先（同率最大はそのまま）
                if (remainHands[t] == maxRemain) w *= abundanceBoost;

                // 同一手の連続を抑制（直近と同じ && すでに MaxSameStreak-1 連続なら強く減衰）
                if (last.HasValue && t == last.Value && sameStreak >= (maxSameStreak - 1))
                    w *= repeatPenalty;

                // 念のため下限
                if (w < 0.05) w = 0.05;

                weights.Add((t, w));
            }

            // ルーレット選択
            double sum = weights.Sum(x => x.w);
            double r = rand.NextDouble() * sum;
            double acc = 0;
            foreach (var (t, w) in weights)
            {
                acc += w;
                if (r <= acc) return t;
            }

            return weights.Last().t;
        }

        /// <summary> 敵AIの予測(対応)行動選択 </summary>
        private void RunEnemyPredictionAI()
        {
            var rand = new Random();
            _ctx.PredictQueue.Clear();

            var attacker = Characters[_ctx.CurrentActorId]; // 予約側（= プレイヤー側のときが多い）
            if (!Characters.TryGetValue(_ctx.CurrentTargetId, out var defender)) return;

            // --- ① 相手（プレイヤー）の“現在”の残手数をスナップショット ---
            //     ※予約で消費済みの分は既に差し引かれている想定
            int oppSlashRemain = attacker.CurrentStats.RemainingHands[AttackType.Slash];
            int oppDownRemain = attacker.CurrentStats.RemainingHands[AttackType.Down];

            // --- ② 非公開枠で使ってよいカウンター数の上限を管理（予測キュー内で累積） ---
            int usedCounterSlash = 0;
            int usedCounterDown = 0;

            for (int i = 0; i < _ctx.ReserveQueue.Count; i++)
            {
                var act = _ctx.ReserveQueue[i];
                var atk = act.Attack;

                // まずは応答候補（回避は常に可）
                var candidates = new List<ResponseType> { ResponseType.Evade };
                if (defender.CurrentStats.RemainingHands[AttackType.Slash] > 0)
                    candidates.Add(ResponseType.CounterSlash);
                if (defender.CurrentStats.RemainingHands[AttackType.Down] > 0)
                    candidates.Add(ResponseType.CounterDown);

                ResponseType rsp;

                if (act.IsRevealedToDefender)
                {
                    // --- 公開枠は従来通り：知力判定で JudgeOutcome を用いて最適寄りに選ぶ ---
                    int intel = defender.CurrentStats.Intelligence;
                    int roll = rand.Next(100);

                    if (roll < intel)
                    {
                        var outcomes = candidates
                            .Select(r => (r, outcome: JudgeOutcome(atk, r, attacker, defender)))
                            .ToList();

                        // 攻撃者不利 > 引き分け > その他 の優先で選択
                        var best = outcomes.FirstOrDefault(x => x.outcome.OutcomeType == "攻撃者・不利");
                        if (!best.Equals(default)) rsp = best.r;
                        else if ((best = outcomes.FirstOrDefault(x => x.outcome.OutcomeType == "引き分け")) != default)
                            rsp = best.r;
                        else rsp = outcomes.First().r;
                    }
                    else
                    {
                        rsp = candidates[rand.Next(candidates.Count)];
                    }
                }
                else
                {
                    // --- 非公開枠は「上限付きアホ」 ---
                    // 相手の“残手数”を上限として、迅返/剛断の候補を絞る
                    var capped = new List<ResponseType> { ResponseType.Evade };

                    bool canUseSlashCounter =
                        defender.CurrentStats.RemainingHands[AttackType.Slash] > 0 &&
                        usedCounterSlash < oppSlashRemain; // ← 上限チェック（相手の迅残手数）

                    bool canUseDownCounter =
                        defender.CurrentStats.RemainingHands[AttackType.Down] > 0 &&
                        usedCounterDown < oppDownRemain;   // ← 上限チェック（相手の剛残手数）

                    if (canUseSlashCounter) capped.Add(ResponseType.CounterSlash);
                    if (canUseDownCounter) capped.Add(ResponseType.CounterDown);

                    // 1つも残らないことは基本ないが、保険で candidates を使う
                    var pool = capped.Count > 0 ? capped : candidates;
                    rsp = pool[rand.Next(pool.Count)];
                }

                // --- 残手数の消費（従来通り） ---
                if (rsp == ResponseType.CounterSlash)
                {
                    defender.CurrentStats.RemainingHands[AttackType.Slash]--;
                    if (!act.IsRevealedToDefender) usedCounterSlash++; // 非公開で選んだ分のみ“使用数”をカウント
                }
                else if (rsp == ResponseType.CounterDown)
                {
                    defender.CurrentStats.RemainingHands[AttackType.Down]--;
                    if (!act.IsRevealedToDefender) usedCounterDown++;
                }
                else if (rsp == ResponseType.Evade &&
                         defender.CurrentStats.RemainingHands[AttackType.Thrust] > 0)
                {
                    defender.CurrentStats.RemainingHands[AttackType.Thrust]--;
                }

                var outcome = JudgeOutcome(atk, rsp, attacker, defender);

                _ctx.PredictQueue.Add(new PredictedResponse
                {
                    ResponderId = _ctx.CurrentTargetId,
                    Response = rsp,
                    SlotIndex = i,
                    Outcome = outcome
                });

                ShowAdvantageIcon(outcome, i);
                AddPredictionIcon(rsp, i);
            }
        }

        // === 戦闘解決処理 ===

        /// <summary>
        /// 洞察/翻弄比率から公開手数を算出
        /// </summary>
        private int CalculateRevealedCount(Character reserver, Character predictor, int totalReservations)
        {
            // 幻煙玉：予約側が煙中なら完全不可視
            if (_smokeConcealActive.Contains(reserver.ObjectId))
                return 0;

            float insight = Math.Max(1, predictor.CurrentStats.Insight);
            float confuse = Math.Max(1, reserver.CurrentStats.Confuse);
            float ratio = insight / confuse * 100f;

            // === 新仕様 ===
            // ratio 0–50: 0枠固定 
            // 50–100: 0～1枠 (ratioにより線形確率変動)
            // 100–300: 1～2枠 (ratioにより線形確率変動)
            // 300以上: 4枠固定

            int min, max;
            float start, end;

            if (ratio < 50f) { min = 0; max = 0; start = 0f; end = 1f; }
            else if (ratio < 100f) { min = 0; max = 1; start = 50f; end = 100f; }
            else if (ratio < 300f) { min = 1; max = 2; start = 100f; end = 300f; }
            else { min = 4; max = 4; start = 0f; end = 1f; }

            // 区間内で確率を線形補間
            if (min == max)
                return Math.Clamp(min, 0, Math.Min(4, totalReservations));

            float t = Math.Clamp((ratio - start) / (end - start), 0f, 1f);

            // t=0 → min固定、t=1 → max固定、中間は確率的に変化
            var rand = new Random();
            bool chooseMax = rand.NextDouble() < t;  // ratioが上がるほどmaxを引く確率が上がる
            int reveal = chooseMax ? max : min;

            // 上限制限
            return Math.Clamp(reveal, 0, Math.Min(4, totalReservations));
        }

        /// <summary> 攻防判定 </summary>
        private ActionOutcome JudgeOutcome(AttackType atk, ResponseType rsp, Character attacker, Character defender)
        {
            var outcome = new ActionOutcome();

            var thrustRatio = 0.3f;
            var slashRatio = 0.8f;
            var downRatio = 0.6f;
            var counterRatio = 0.5f;

            switch (atk, rsp)
            {
                case (AttackType.Thrust, ResponseType.Evade):
                    outcome.EvadeRate = ApplyEvadeFatigue(1.0f, _ctx.CurrentTargetId);
                    outcome.AttackerAnimOnHit = "穿・攻撃";
                    outcome.DefenderAnimOnHit = "回避失敗・ダメージ";
                    outcome.DefenderResidualRatio = thrustRatio;
                    outcome.DefenderFatalRatio = 1 - thrustRatio;
                    break;

                case (AttackType.Slash, ResponseType.Evade):
                    outcome.EvadeRate = ApplyEvadeFatigue(0.3f, _ctx.CurrentTargetId);
                    outcome.AttackerAnimOnHit = "迅・攻撃";
                    outcome.DefenderAnimOnHit = "回避失敗・ダメージ";
                    outcome.DefenderResidualRatio = slashRatio;
                    outcome.DefenderFatalRatio = 1 - slashRatio;
                    break;

                case (AttackType.Down, ResponseType.Evade):
                    outcome.EvadeRate = ApplyEvadeFatigue(0.5f, _ctx.CurrentTargetId);
                    outcome.AttackerAnimOnHit = "剛・攻撃";
                    outcome.DefenderAnimOnHit = "回避失敗・ダメージ";
                    outcome.DefenderResidualRatio = downRatio;
                    outcome.DefenderFatalRatio = 1 - downRatio;
                    break;

                case (AttackType.Thrust, ResponseType.CounterSlash):
                    outcome.OutcomeType = "攻撃者・有利";
                    outcome.AttackerAnimOnHit = "穿・攻撃";
                    outcome.DefenderAnimOnHit = "迅失敗・ダメージ";
                    outcome.DefenderResidualRatio = thrustRatio;
                    outcome.DefenderFatalRatio = 1 - thrustRatio;
                    break;

                case (AttackType.Slash, ResponseType.CounterSlash):
                    outcome.OutcomeType = "攻撃者・不利";
                    outcome.AttackerAnimOnHit = "迅失敗・ダメージ";
                    outcome.DefenderAnimOnHit = "迅・攻撃";
                    outcome.AttackerResidualRatio = counterRatio;
                    outcome.AttackerFatalRatio = 1 - counterRatio;
                    break;

                case (AttackType.Down, ResponseType.CounterSlash):
                    outcome.OutcomeType = "攻撃者・有利";
                    outcome.AttackerAnimOnHit = "剛・攻撃";
                    outcome.DefenderAnimOnHit = "迅失敗・ダメージ";
                    outcome.DefenderResidualRatio = downRatio;
                    outcome.DefenderFatalRatio = 1 - downRatio;
                    break;

                case (AttackType.Thrust, ResponseType.CounterDown):
                    outcome.OutcomeType = "攻撃者・有利";
                    outcome.AttackerAnimOnHit = "穿・攻撃";
                    outcome.DefenderAnimOnHit = "剛失敗・ダメージ";
                    outcome.DefenderResidualRatio = thrustRatio;
                    outcome.DefenderFatalRatio = 1 - thrustRatio;
                    break;

                case (AttackType.Slash, ResponseType.CounterDown):
                    outcome.OutcomeType = "攻撃者・有利";
                    outcome.AttackerAnimOnHit = "迅・攻撃";
                    outcome.DefenderAnimOnHit = "剛失敗・ダメージ";
                    outcome.DefenderResidualRatio = slashRatio;
                    outcome.DefenderFatalRatio = 1 - slashRatio;
                    break;

                case (AttackType.Down, ResponseType.CounterDown):
                    outcome.OutcomeType = "攻撃者・不利";
                    outcome.AttackerAnimOnHit = "剛失敗・ダメージ";
                    outcome.DefenderAnimOnHit = "剛・攻撃";
                    outcome.AttackerResidualRatio = counterRatio;
                    outcome.AttackerFatalRatio = 1 - counterRatio;
                    break;
            }

            // ★ 不可避スキル（攻撃者）→ 回避率を強制0%（穿は半減）
            if (HasUnavoidableSkill(attacker))
            {
                if (atk == AttackType.Thrust)
                {
                    outcome.EvadeRate *= 0.5f;
                }
                else
                {
                    outcome.EvadeRate = 0f;
                }
            }
            else
            {
                // 回避率判定時にバフ/デバフを考慮
                // 影縫い：回避成功率半減
                if (defender.CurrentStats.TempBuffs.TryGetValue("EvasionSuccessRateMultiplier", out var succMul))
                    outcome.EvadeRate *= succMul;

                // 影分身：回避失敗率半減
                if (defender.CurrentStats.TempBuffs.TryGetValue("EvasionFailRateMultiplier", out var failMul))
                    outcome.EvadeRate = 1 - (1 - outcome.EvadeRate) * failMul;
            }

            if (rsp == ResponseType.Evade)
            {
                if (outcome.EvadeRate > 0.6f) outcome.OutcomeType = "攻撃者・不利";
                else if (outcome.EvadeRate > 0.3f) outcome.OutcomeType = "引き分け";
                else outcome.OutcomeType = "攻撃者・有利";
            }

            return outcome;
        }

        /// <summary>
        /// 回避疲労補正付きの回避率を返す
        /// </summary>
        private float ApplyEvadeFatigue(float baseRate, int defenderId)
        {
            if (defenderId < 0) return baseRate;

            if (!_evadeCount.ContainsKey(defenderId))
                _evadeCount[defenderId] = 0;

            var defender = Characters[defenderId];
            int remainThrust = defender.CurrentStats.RemainingHands[AttackType.Thrust];

            float result = baseRate;
            if (remainThrust <= 0)
            {
                int count = _evadeCount[defenderId];
                result -= 0.1f * (count + 1);
                _evadeCount[defenderId]++;
            }

            return MathF.Max(0f, result);
        }

        /// <summary>
        /// ターン終了時の手数回復処理（スキル反映対応）
        /// </summary>
        private void HandleHandsRecovery()
        {
            foreach (var ch in Characters.Values)
            {
                if (ch.CurrentStats.IsDead) continue;

                // === このキャラが予約行動を行ったかを確認 ===
                bool actedThisTurn = _ctx.ReserveQueue.Any(a => a.AttackerId == ch.ObjectId);

                // === このターンで予約が一切なかったか ===
                bool noReservation = !_ctx.ReserveQueue.Any();

                // --- 条件に応じた回復可否 ---
                if (actedThisTurn)
                {
#if DEBUG
                    Console.WriteLine($"{ch.Name}：予約行動を行ったため手数回復なし");
#endif
                    continue; // 予約行動したキャラは回復しない
                }

                // --- 通常回復処理 ---
                foreach (var atkType in ch.CurrentStats.MaxHands.Keys.ToList())
                {
                    int max = ch.CurrentStats.MaxHands[atkType];
                    int cur = ch.CurrentStats.RemainingHands[atkType];

                    // 基本回復量（例：毎ターン1回復）
                    int recover = 1;

                    // --- スキルによる追加回復 ---
                    int extraRecover = GetExtraHandRecovery(ch, atkType);
                    recover += extraRecover;

                    int newVal = Math.Min(max, cur + recover);
                    ch.CurrentStats.RemainingHands[atkType] = newVal;

#if DEBUG
                    if (extraRecover > 0)
                        Console.WriteLine($"{ch.Name} の {atkType} 手数回復 +{recover}（基本1＋スキル{extraRecover}）");
#endif
                }

                // === デバッグログ ===
#if DEBUG
                if (noReservation)
                    Console.WriteLine($"{ch.Name}：予約行動なし → 通常回復");
                else
                    Console.WriteLine($"{ch.Name}：予約行動未実施（他者が予約）→ 通常回復");
#endif
            }
        }

        /// <summary>
        /// スキルに基づく手数回復量ボーナスを返す
        /// </summary>
        private int GetExtraHandRecovery(Character ch, AttackType type)
        {
            int bonus = 0;

            // 対象キャラのスキル（敵ならキャラのみ、プレイヤーは装備含む）
            var allSkills = new List<Skill>(ch.Skills);

            if (ch.Type == "Player")
            {
                var eq = EquipmentManager.Instance.GetEquipped(ch, "武器");
                if (eq != null) allSkills.AddRange(eq.Skills);
            }

            foreach (var sk in allSkills)
            {
                switch (type)
                {
                    case AttackType.Thrust:
                        if (sk.Id == "技の極意(全)" || sk.Id == "技の極意(穿)" || sk.Id == "穿・回復数+1") bonus += 1;
                        break;
                    case AttackType.Slash:
                        if (sk.Id == "技の極意(全)" || sk.Id == "技の極意(迅)" || sk.Id == "迅・回復数+1") bonus += 1;
                        break;
                    case AttackType.Down:
                        if (sk.Id == "技の極意(全)" || sk.Id == "技の極意(剛)" || sk.Id == "剛・回復数+1") bonus += 1;
                        break;
                }
            }

            return bonus;
        }

        /// <summary>
        /// 解決フェーズで全スロットを処理（アニメ付き）
        /// </summary>
        private async Task ResolveAllSlotsWithAnimationAsync()
        {
            var rand = new Random();

            for (int i = 0; i < _ctx.ReserveQueue.Count; i++)
            {
                if (i >= _ctx.PredictQueue.Count) break;

                _currentResolvingSlot = i;

                var act = _ctx.ReserveQueue[i];
                var rsp = _ctx.PredictQueue[i];
                if (act.SlotIndex != rsp.SlotIndex) continue;

                var attacker = Characters[act.AttackerId];
                var defender = Characters[rsp.ResponderId];
                var outcome = rsp.Outcome;

                bool evaded = (rand.NextDouble() < outcome.EvadeRate);

                if (evaded)
                {
                    attacker.PlayAttackAnimation(outcome.AttackerAnimOnHit);
                    defender.PlayAnimation(defender.FixedEvadeAnim);
                }
                else
                {
                    if (outcome.DefenderAnimOnHit == "回避失敗・ダメージ"
                        || outcome.OutcomeType == "攻撃者・有利")
                    {
                        attacker.PlayAttackAnimation(outcome.AttackerAnimOnHit);
                        defender.PlayAnimation(outcome.DefenderAnimOnHit);
                        ShowBloodEffect(defender);
                    }
                    else
                    {
                        attacker.PlayAnimation(outcome.AttackerAnimOnHit);
                        defender.PlayAttackAnimation(outcome.DefenderAnimOnHit);
                        ShowBloodEffect(attacker);
                    }
                }

                float duration = attacker.Animations[outcome.AttackerAnimOnHit].Frames.Sum(f => f.Duration);

                await Task.Delay((int)(duration * 500 * _resolutionSpeedMultiplier));

                if (!evaded)
                {
                    ApplyDamage(attacker, defender, outcome.DefenderResidualRatio, outcome.DefenderFatalRatio);
                    if (outcome.AttackerResidualRatio > 0 || outcome.AttackerFatalRatio > 0)
                        ApplyDamage(defender, attacker, outcome.AttackerResidualRatio, outcome.AttackerFatalRatio);
                }

                await Task.Delay((int)(duration * 500 * _resolutionSpeedMultiplier));

                if (attacker.CurrentStats.IsDead || defender.CurrentStats.IsDead)
                {
                    _currentResolvingSlot = -1;
                    ClearResolveHighlight();
                    _resolutionSpeedMultiplier = 1.0f;
                    SetAnimationSpeed(1.0f);
                    await Task.Delay(500);
                    SubmitIntent(new ConfirmResolutionIntent());
                    return;
                }

                if (i < _ctx.ReserveQueue.Count - 1)
                    await Task.Delay((int)(200 * _resolutionSpeedMultiplier));
            }

            _currentResolvingSlot = -1;
            ClearResolveHighlight();
            _resolutionSpeedMultiplier = 1.0f;
            SetAnimationSpeed(1.0f);
            await Task.Delay(500);
            SubmitIntent(new ConfirmResolutionIntent());
        }

        /// <summary>
        /// 攻撃キャラに基づく残痕／致命ダメージ強化倍率を計算
        /// </summary>
        private (float residualRate, float fatalRate) GetDamageBonusRates(Character attacker)
        {
            float residualBonus = 0f;
            float fatalBonus = 0f;

            // 攻撃側の全スキルを取得
            var allSkills = new List<Skill>(attacker.Skills);
            if (attacker.Type == "Player")
            {
                var eq = EquipmentManager.Instance.GetEquipped(attacker, "武器");
                if (eq != null) allSkills.AddRange(eq.Skills);
            }

            foreach (var sk in allSkills)
            {
                switch (sk.Id)
                {
                    case "残痕強化":
                        residualBonus += 0.01f * sk.Level; // +1% per Lv
                        break;
                    case "致命強化":
                        fatalBonus += 0.01f * sk.Level;
                        break;
                }
            }

            return (1f + residualBonus, 1f + fatalBonus);
        }

        /// <summary> ダメージ適用 </summary>
        private void ApplyDamage(Character attacker, Character defender, float residualRatio, float fatalRatio)
        {
            int atkStat = attacker.CurrentStats.Attack;
            int defStat = defender.CurrentStats.Defense;

            int baseResidual = (int)MathF.Max(1, (float)(atkStat * atkStat) / (atkStat + defStat));
            int baseFatal = atkStat;

            // === ★ スキル倍率適用 ===
            var (resBonus, fatBonus) = GetDamageBonusRates(attacker);
            residualRatio *= resBonus;
            fatalRatio *= fatBonus;

            int residual = (int)(baseResidual * residualRatio);
            int fatal = (int)(baseFatal * fatalRatio);

            // === ★ 致命化スキル適用 ===
            if (HasCriticalConversionSkill(attacker))
            {
                if (fatal < residual)
                {
                    fatal = residual;
#if DEBUG
                    Console.WriteLine($"{attacker.Name} の『致命化』発動：致命 {fatal} に引き上げ");
#endif
                }
            }

            if (residual > 0) defender.CurrentStats.ApplyResidualDamage(residual);
            if (fatal > 0) defender.CurrentStats.ApplyFatalDamage(fatal);

            // ★ ダメージポップアップ表示
            ShowDamagePopup(defender, residual, fatal);
        }

        /// <summary>
        /// バトル中のタップ対象キャラを返す
        /// </summary>
        private int GetTapCharacter(float x, float y)
        {
            foreach (var ch in Characters.Values.ToList())
            {
                if (ch.HitTest(x, y))
                {
#if DEBUG
                    Console.WriteLine($"タップ対象キャラ: {ch.Name} ({ch.ObjectId})");
#endif
                    return ch.ObjectId;
                }
            }
            return 0;
        }

        // === UIイベントハブ ===

        /// <summary>
        /// 戦闘画面でのタップ処理
        /// </summary>
        public void OnTapInBattle(float x, float y)
        {
            var selectCharacter = GetTapCharacter(x, y);
        }

        /// <summary>
        /// 戦闘画面でのスワイプ処理
        /// </summary>
        public void OnSwipeInBattle(string direction)
        {
            // 現状は未使用（将来的に対応可）
        }

        /// <summary>
        /// 「反撃無制限」スキルを所持しているか判定
        /// </summary>
        private bool HasUnlimitedCounterSkill(Character ch)
        {
            var allSkills = new List<Skill>(ch.Skills);

            // プレイヤーなら装備スキルも確認
            if (ch.Type == "Player")
            {
                var eq = EquipmentManager.Instance.GetEquipped(ch, "武器");
                if (eq != null) allSkills.AddRange(eq.Skills);
            }

            return allSkills.Any(s =>
                s.Id == "反撃無制限");
        }

        /// <summary>
        /// 攻撃者が「不可避」スキルを持っているか
        /// </summary>
        private bool HasUnavoidableSkill(Character attacker)
        {
            var allSkills = new List<Skill>(attacker.Skills);

            if (attacker.Type == "Player")
            {
                var eq = EquipmentManager.Instance.GetEquipped(attacker, "武器");
                if (eq != null) allSkills.AddRange(eq.Skills);
            }

            return allSkills.Any(s => s.Id == "不可避");
        }

        /// <summary>
        /// 攻撃者が「致命化」スキルを持っているか判定
        /// </summary>
        private bool HasCriticalConversionSkill(Character attacker)
        {
            var allSkills = new List<Skill>(attacker.Skills);

            if (attacker.Type == "Player")
            {
                var eq = EquipmentManager.Instance.GetEquipped(attacker, "武器");
                if (eq != null) allSkills.AddRange(eq.Skills);
            }

            return allSkills.Any(s =>
                s.Id == "致命化");
        }

        /// <summary>
        /// 画面上のUIをすべてクリア（戦闘後エピローグ演出用）
        /// </summary>
        public void ClearAllBattleUI()
        {
            // --- 通常UIの削除 ---
            foreach (var ui in UIObjects.Values.ToList())
                RemoveUI(ui);
            UIObjects.Clear();

            // --- 共通UIの削除 ---
            CommonUIObjects.Clear();

            // --- ステータスパネルなどのキャッシュもクリア ---
            _statusPanels.Clear();
            _reserveIcons.Clear();
            _responseIcons.Clear();
            _advantageIcons.Clear();
            _reserveMasks.Clear();
            _targetIcons.Clear();
            _ninguIcons.Clear();

            // --- 表示キャッシュ系 ---
            _highlightUI = null;
            _turnTelop = null;
        }
    }

    /// <summary> バトル開始セットアップ情報 </summary>
    public sealed class BattleSetup
    {
        public List<Character> Allies { get; init; } = new();
        public List<Character> Enemies { get; init; } = new();
        public Background Background { get; init; } = new();
        public StealthRewardResult Rewards { get; init; } = new ();
        public string StageName { get; init; } = "Combat";
    }
}
