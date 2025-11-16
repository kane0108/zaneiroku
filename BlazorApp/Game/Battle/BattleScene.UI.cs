using BlazorApp.Game.UIObjectFactory;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.Mime.MediaTypeNames;

namespace BlazorApp.Game.Battle
{
    /// <summary>
    /// バトルシーン：UI生成・更新・演出
    /// </summary>
    public partial class BattleScene
    {
        // === フィールド ===
        private UIObject? _highlightUI;
        private readonly List<UIObject> _targetIcons = new();
        private readonly Dictionary<int, UIObject> _reserveIcons = new();
        private readonly Dictionary<int, UIObject> _responseIcons = new();
        private readonly Dictionary<int, UIObject> _advantageIcons = new();
        private readonly Dictionary<int, UIObject> _statusPanels = new();
        private readonly Dictionary<int, UIObject> _reserveMasks = new();

        private static readonly ColorRgb AtbStart = ColorRgb.FromHex("#CCCC00");
        private static readonly ColorRgb AtbEnd = ColorRgb.FromHex("#FFFFFF");

        private UIObject? _turnTelop;

        private readonly Dictionary<int, UIObject> _ninguIcons = new();
        private readonly HashSet<int> _ninguUsed = new();

        private TaskCompletionSource<bool>? _tipsTcs;

        // === UI生成 ===

        /// <summary>
        /// ターン数表示
        /// </summary>
        private void ShowTurnTelop(int turnNumber)
        {
            // 既存のを消す
            if (_turnTelop != null) RemoveUI(_turnTelop);

            string text = $"第{KanjiNumber.ToKanji(turnNumber)}合";

            var telop = new UIObject
            {
                Name = "ターン数テロップ",
                PosX = 5,
                PosY = 2,
                ZIndex = 45000,
                Text = text,
                FontSize = 10,
                FontFamily = "\"Yu Mincho, serif\"",
                TextColor = "#FFFFFF",
                Opacity = 0f,           // フェードイン演出
                StretchToText = true,   // ★ 背景を文字幅に合わせて伸縮
                TextOffsetX = 2f,
                TextOffsetY = 2f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-01.png", // 小さめ背景
                                    new System.Drawing.Rectangle(360*3, 0, 360, 180))
                                {
                                    ScaleX = 0.1f,
                                    ScaleY = 0.1f
                                }
                            }
                        }
                    }}
                }
            };

            telop.StartFadeIn(0.5f); // 0.5秒でフェードイン
            AddUI(telop);
            _turnTelop = telop;
        }

        /// <summary>
        /// 予約行動選択ボタンの生成
        /// </summary>
        private void CreateReservationSelectButton()
        {
            var attackBtn = new UIObject
            {
                Name = $"予約行動選択ボタン",
                ZIndex = 16000,
                CenterX = true,
                PosY = Common.AttackButtonHeight,
                Opacity = 0.8f,
                Visible = false,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation
                        {
                            Loop = false,
                            Frames = GameObjectAnimation.CreateFramesFromSheet(
                                "images/ui05-01.png",
                                256, 256,
                                count: 1,
                                duration: 1.0f,
                                scaleX: 0.7f,
                                scaleY: 0.7f
                            )
                        }
                    }
                },
                OnClick = () => SubmitIntent(new ReserveAttackIntent(AttackType.Thrust)),
                OnSwipe = (dir) =>
                {
                    if (dir == "左" || dir == "右")
                        SubmitIntent(new ReserveAttackIntent(AttackType.Slash));
                    if (dir == "上" || dir == "下")
                        SubmitIntent(new ReserveAttackIntent(AttackType.Down));
                },
            };

            AddUI(attackBtn);
        }

        /// <summary>
        /// 予測行動選択ボタンの生成
        /// </summary>
        private void CreatePredictionSelectButton()
        {
            var respBtn = new UIObject
            {
                Name = $"予測行動選択ボタン",
                ZIndex = 16000,
                CenterX = true,
                PosY = Common.AttackButtonHeight,
                Opacity = 0.8f,
                Visible = false,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation
                        {
                            Loop = false,
                            Frames = GameObjectAnimation.CreateFramesFromSheet(
                                "images/ui05-02.png",
                                256, 256,
                                count: 1,
                                duration: 1.0f,
                                scaleX: 0.7f,
                                scaleY: 0.7f
                            )
                        }
                    }
                },
                OnClick = () => SubmitIntent(new ChooseResponseIntent(ResponseType.Evade)),
                OnSwipe = (dir) =>
                {
                    if (dir == "左" || dir == "右")
                        SubmitIntent(new ChooseResponseIntent(ResponseType.CounterSlash));
                    if (dir == "上" || dir == "下")
                        SubmitIntent(new ChooseResponseIntent(ResponseType.CounterDown));
                },
            };

            AddUI(respBtn);
        }

        /// <summary>
        /// 用意完了ボタンの生成
        /// </summary>
        private void CreateConfirmReservationButton()
        {
            var confirmBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "用意完了ボタン";
                ui.ZIndex = 16000;
                ui.CenterX = true;
                ui.PosY = Common.CanvasHeight - 250;
                ui.Text = "用意完了";   // 世界観に合わせたラベル
                ui.FontSize = 22;
                ui.Opacity = 0.8f;
                ui.Visible = false;
                ui.Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = true,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui01-00.png", 360, 180, 2, 1.0f, offsetX: 360*2
                        )
                    }}
                };
                ui.CurrentAnimationName = "idle";
                ui.OnClick = () =>
                {
#if DEBUG
                    Console.WriteLine("▶ 用意完了ボタンが押されました！");
#endif
                    SubmitIntent(new ConfirmInputTapIntent());
                };
            });

            AddUI(confirmBtn);
        }

        /// <summary>
        /// 勝負ボタンの生成（Prediction_Confirm専用）
        /// </summary>
        private void CreateShobuButton()
        {
            var shobuBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "勝負ボタン";
                ui.ZIndex = 16000;
                ui.CenterX = true;
                ui.PosY = Common.CanvasHeight - 250;
                ui.Text = "勝負";
                ui.FontSize = 22;
                ui.Opacity = 0.8f;
                ui.Visible = false;
                ui.Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = true,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui01-00.png", 360, 180, 2, 1.0f, offsetX: 360*2
                        )
                    }}
                };
                ui.CurrentAnimationName = "idle";
                ui.OnClick = () =>
                {
#if DEBUG
                    Console.WriteLine("▶ 勝負ボタンが押されました！");
#endif
                    SubmitIntent(new ConfirmInputTapIntent());
                };
            });

            AddUI(shobuBtn);
        }

        /// <summary>
        /// 決着ボタンの生成（Prediction_Confirm専用）
        /// </summary>
        private void CreateKetchakuButton()
        {
            var ketchakuBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "決着ボタン";
                ui.ZIndex = 16000;
                ui.CenterX = true;
                ui.PosY = Common.CanvasHeight - 200;
                ui.Text = "決着";
                ui.FontSize = 18;
                ui.Opacity = 0.8f;
                ui.Visible = false;
                ui.Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = true,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui01-00.png", 360, 180, 2, 1.0f, offsetX: 360*2
                        )
                    }}
                };
                ui.CurrentAnimationName = "idle";
                ui.OnClick = () =>
                {
#if DEBUG
                    Console.WriteLine("▶ 決着ボタンが押されました！（超早送りON）");
#endif
                    _resolutionSpeedMultiplier = 0.1f; // 10倍速
                    SetAnimationSpeed(10.0f);          // アニメも10倍速

                    SubmitIntent(new ConfirmInputTapIntent());
                };
            });

            AddUI(ketchakuBtn);
        }

        /// <summary>
        /// テスト用ボタンの生成
        /// </summary>
        private void CreateTestButton()
        {
            var testBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.ZIndex = 50000;
                ui.PosX = 280f;
                ui.PosY = 580f;
                ui.Text = "TEST";
                ui.FontSize = 20;
                ui.Opacity = 0.2f;
                ui.OnClick = () => SubmitIntent(new TestInputIntent(0));
            });

            AddUI(testBtn);
        }

        // === UI更新 ===

        /// <summary>
        /// ATBフィルを返す
        /// </summary>
        public float GetAtbFill01(int id)
        {
            // 死亡者は常にゼロ表示
            if (Characters.TryGetValue(id, out var ch) && ch.CurrentStats.IsDead)
                return 0f;

            // それ以外は普通にAtbTrackerから取得
            if (CurrentPhase != BattlePhase.ChooseActor && _frozenFill01.TryGetValue(id, out var f))
                return f;

            var value = _atb?.GetFill01(id) ?? 0f;
            if (value >= 1f)
                _frozenFill01[id] = 1f;

            return value;
        }

        /// <summary>
        /// 選択中キャラの強調表示アイコンの生成
        /// </summary>
        private void HighlightActor(int actorId)
        {
            // 既存のハイライトを消す
            if (_highlightUI != null)
            {
                RemoveUI(_highlightUI);
                _highlightUI = null;
            }

            if (!Characters.TryGetValue(actorId, out var actor)) return;

            var ring = new UIObject
            {
                Name = $"Highlight_{actorId}",
                ZIndex = 12000,
                Opacity = 0.6f,
                PosY = -200,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation
                        {
                            Loop = true,
                            Frames = GameObjectAnimation.CreateFramesFromSheet(
                                "images/ui03-00.png",
                                128, 64,
                                count: 4,
                                duration: 0.2f,
                                scaleX: 0.8f,
                                scaleY: 0.8f
                            )
                        }
                    },

                },
                CurrentAnimationName = "idle"
            };

            _highlightUI = ring;
            AddUI(ring);
        }

        /// <summary>
        /// ターゲットアイコンの生成
        /// </summary>
        private void ShowTargetIcons()
        {
            ClearTargetIcons();

            foreach (var ch in Characters.Values.ToList())
            {
                // 敵かつ生存している場合のみ
                if (ch.Type == "Enemy" && !ch.CurrentStats.IsDead)
                {
                    var icon = new UIObject
                    {
                        Name = $"TargetIcon_{ch.ObjectId}",
                        ZIndex = 13000,
                        Opacity = 0.9f,
                        MirrorHorizontal = true,
                        Animations = new Dictionary<string, GameObjectAnimation>
                        {
                            { "idle", new GameObjectAnimation
                                {
                                    Loop = true,
                                     Frames = GameObjectAnimation.CreateFramesFromSheet(
                                        "images/ui04-00.png",
                                        64, 64,
                                        count: 2,
                                        duration: 0.3f,
                                        scaleX: 0.8f,
                                        scaleY: 0.8f,
                                        offsetX: 64
                                     )
                                }
                            }
                        },
                        CurrentAnimationName = "idle"
                    };
                    icon.PosY = -200;
                    _targetIcons.Add(icon);
                    AddUI(icon);
                }
            }
        }

        /// <summary>
        /// ターゲットアイコンの削除
        /// </summary>
        private void ClearTargetIcons()
        {
            foreach (var icon in _targetIcons)
            {
                RemoveUI(icon);
            }
            _targetIcons.Clear();
        }

        /// <summary>
        /// 予約開始時にスロット枠を並べる（公開枠はランダム）
        /// </summary>
        private void SetupReservationSlots()
        {
            foreach (var ui in UIObjects.Values.Where(u => u.Name.StartsWith("予約枠_")).ToList())
                RemoveUI(ui);

            _reserveIcons.Clear();
            _reserveMasks.Clear();

            var reservationSide = Characters[_ctx.CurrentActorId];
            var predictionSide = Characters.Values.First(c => c.Type != reservationSide.Type);

            _ctx.RevealCount = CalculateRevealedCount(reservationSide, predictionSide, _ctx.CurrentMaxReservations);

            // ランダム公開枠抽選
            var rand = new Random();
            var revealIndices = Enumerable.Range(0, _ctx.CurrentMaxReservations)
                .OrderBy(_ => rand.Next())
                .Take(_ctx.RevealCount)
                .ToHashSet();

            for (int i = 0; i < _ctx.CurrentMaxReservations; i++)
            {
                float posX = 30 + i * 50;
                float posY = Common.ReserveIconHeight;

                var frame = new UIObject
                {
                    Name = $"予約枠_{i}",
                    ZIndex = 18000,
                    PosX = posX,
                    PosY = posY,
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        { "idle", new GameObjectAnimation {
                            Loop = true,
                            Frames = GameObjectAnimation.CreateFramesFromSheet(
                                "images/ui04-00.png",
                                64, 64, 1, 1.0f,
                                offsetX: 64 * 5, offsetY: 64 * 2,
                                scaleX: 0.8f, scaleY: 0.8f
                            )
                        }}
                    },
                    CurrentAnimationName = "idle"
                };

                AddUI(frame);

                // ★敵側予約時だけ ? マスクをここで出す
                if (!_ctx.PlayerIsReservationSide && !revealIndices.Contains(i))
                {
                    var mask = new UIObject
                    {
                        Name = $"予約マスク_{i}",
                        ZIndex = 20000,
                        PosX = posX,
                        PosY = posY,
                        Opacity = 0.5f,
                        Animations = new Dictionary<string, GameObjectAnimation>
                        {
                            { "idle", new GameObjectAnimation {
                                Loop = true,
                                Frames = GameObjectAnimation.CreateFramesFromSheet(
                                    "images/ui04-00.png",
                                    64, 64, 1, 1.0f,
                                    offsetX: 64*5, offsetY: 64*1,
                                    scaleX: 0.8f, scaleY: 0.8f
                                )
                            }}
                        },
                        CurrentAnimationName = "idle"
                    };
                    AddUI(mask);
                    _reserveMasks[i] = mask;
                }
            }
        }

        /// <summary>
        /// 敵予約枠の「？」マスクをすべて外す（先見丹効果）
        /// </summary>
        private void RevealAllEnemyReservationMasks()
        {
            foreach (var kv in _reserveMasks)
            {
                var mask = kv.Value;
                if (mask == null) continue;
                RemoveUI(mask);
            }
            _reserveMasks.Clear();
            _ctx.RevealCount = _ctx.CurrentMaxReservations;
        }

        /// <summary>
        /// 予約アイコン（飛びアイコン）
        /// </summary>
        private void SetReserveAttackIcon(AttackType atk, int slotIndex)
        {
            float startX = Common.CanvasWidth / 2 - 32;
            float startY = Common.AttackButtonHeight + 50;

            int offsetX = atk switch
            {
                AttackType.Thrust => 64 * 2,
                AttackType.Slash => 64 * 3,
                AttackType.Down => 64 * 4,
                _ => 64 * 2
            };

            var icon = new UIObject
            {
                Name = $"予約アイコン_{slotIndex}",
                ZIndex = 19000, // 空枠より上、マスクより下
                PosX = startX,
                PosY = startY,
                TargetPosX = 30 + slotIndex * 50,
                TargetPosY = Common.ReserveIconHeight,
                EnableLerp = true,
                MoveLerpSpeed = 5f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = true,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png",
                            64, 64, 1, 1.0f,
                            offsetX, scaleX: 0.8f, scaleY: 0.8f
                        )
                    }}
                },
                CurrentAnimationName = "idle"
            };

            AddUI(icon);
            _reserveIcons[slotIndex] = icon;
        }

        /// <summary>
        /// 予約アイコン生成（枠用途）
        /// </summary>
        private void AddReserveIcon(AttackType atk, int slotIndex)
        {
            float startX = Common.CanvasWidth / 2 - 32 + 70;
            float startY = 200;

            var icon = new UIObject
            {
                Name = $"予約アイコン_{slotIndex}",
                ZIndex = 19000,
                PosX = startX,
                PosY = startY,
                TargetPosX = 30 + slotIndex * 50,
                TargetPosY = Common.ReserveIconHeight,
                EnableLerp = true,
                MoveLerpSpeed = 5f,
                Animations = new Dictionary<string, GameObjectAnimation>(), // 枠で後から差し替え
                CurrentAnimationName = "idle"
            };

            AddUI(icon);
            _reserveIcons[slotIndex] = icon;
        }

        /// <summary>
        /// 予約表示の確定
        /// </summary>
        private void RevealAllReservationIcons()
        {
            int usedCount = _ctx.ReserveQueue.Count;

            foreach (var act in _ctx.ReserveQueue)
                act.IsRevealedToDefender = true;

            if (_ctx.PlayerIsReservationSide)
            {
                // 既存の空枠を全部削除
                var frameUIs = UIObjects.Values
                    .Where(u => u.Name.StartsWith("予約枠_"))
                    .ToList();
                foreach (var ui in frameUIs)
                    RemoveUI(ui);

                var reservationSide = Characters[_ctx.CurrentActorId];
                var predictionSide = Characters.Values.First(c => c.Type != reservationSide.Type);

                var rand = new Random();
                var revealIndices = Enumerable.Range(0, usedCount)
                    .OrderBy(_ => rand.Next())
                    .Take(_ctx.RevealCount)
                    .ToHashSet();

                for (int i = 0; i < usedCount; i++)
                {
                    if (!revealIndices.Contains(i) && !_reserveMasks.ContainsKey(i))
                    {
                        if (_reserveIcons.TryGetValue(i, out var icon))
                        {
                            var mask = new UIObject
                            {
                                Name = $"予約マスク_{i}",
                                ZIndex = 20000,
                                PosX = icon.PosX,
                                PosY = icon.PosY,
                                Opacity = 0.5f,
                                Animations = new Dictionary<string, GameObjectAnimation>
                                {
                                    { "idle", new GameObjectAnimation {
                                        Loop = true,
                                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                                            "images/ui04-00.png",
                                            64, 64, 1, 1.0f,
                                            offsetX: 64*5, offsetY: 64*1,
                                            scaleX: 0.8f, scaleY: 0.8f
                                        )
                                    }}
                                },
                                CurrentAnimationName = "idle"
                            };
                            AddUI(mask);
                            _reserveMasks[i] = mask;
                        }
                    }
                }

                // ★ 余分なアイコン削除（予約していないスロット）
                var extraIcons = _reserveIcons.Keys.Where(k => k >= usedCount).ToList();
                foreach (var idx in extraIcons)
                {
                    if (_reserveIcons.TryGetValue(idx, out var icon))
                        RemoveUI(icon);
                    _reserveIcons.Remove(idx);
                }
            }
            else
            {
                // 敵予約側: 非公開枠の ? を攻撃アイコンに更新
                foreach (var act in _ctx.ReserveQueue)
                {
                    if (!_reserveIcons.TryGetValue(act.SlotIndex, out var icon)) continue;

                    int offsetX = act.Attack switch
                    {
                        AttackType.Thrust => 64 * 2,
                        AttackType.Slash => 64 * 3,
                        AttackType.Down => 64 * 4,
                        _ => 64 * 2
                    };

                    icon.Animations["idle"] = new GameObjectAnimation
                    {
                        Loop = true,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png",
                            64, 64, 1, 1.0f,
                            offsetX, scaleX: 0.8f, scaleY: 0.8f
                        )
                    };
                    icon.Opacity = 1.0f;
                    icon.CurrentAnimationName = "idle";
                    icon.MarkDirty();
                }
            }

            // 有利不利アイコンを表示
            foreach (var act in _ctx.ReserveQueue)
            {
                if (act.SlotIndex < _ctx.PredictQueue.Count)
                {
                    var rsp = _ctx.PredictQueue[act.SlotIndex];
                    ShowAdvantageIcon(rsp.Outcome, act.SlotIndex, force: true);
                }
            }
        }

        /// <summary>
        /// 敵側予約スロットの描画（公開枠はランダム）
        /// </summary>
        private void SetupEnemyReservationSlots(bool forceFullReveal = false)
        {
            foreach (var kv in _reserveIcons.ToList())
                RemoveUI(kv.Value);
            _reserveIcons.Clear();

            var reservationSide = Characters[_ctx.CurrentActorId];
            var predictionSide = Characters.Values.First(c => c.Type != reservationSide.Type);

            _ctx.RevealCount = CalculateRevealedCount(
                reservationSide, predictionSide, _ctx.ReserveQueue.Count);

            // ★公開スロットをランダム抽選
            var rand = new Random();
            var revealIndices = Enumerable.Range(0, _ctx.ReserveQueue.Count)
                .OrderBy(_ => rand.Next())
                .Take(_ctx.RevealCount)
                .ToHashSet();

            for (int i = 0; i < _ctx.ReserveQueue.Count; i++)
            {
                var act = _ctx.ReserveQueue[i];
                AddReserveIcon(act.Attack, i);

                if (!_reserveIcons.TryGetValue(i, out var icon)) continue;

                if (revealIndices.Contains(i) || forceFullReveal)
                {
                    // 公開枠
                    act.IsRevealedToDefender = true;

                    int offsetX = act.Attack switch
                    {
                        AttackType.Thrust => 64 * 2,
                        AttackType.Slash => 64 * 3,
                        AttackType.Down => 64 * 4,
                        _ => 64 * 2
                    };

                    icon.Animations["idle"] = new GameObjectAnimation
                    {
                        Loop = true,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png",
                            64, 64, 1, 1.0f,
                            offsetX, scaleX: 0.8f, scaleY: 0.8f
                        )
                    };
                }
                else
                {
                    // 非公開枠
                    act.IsRevealedToDefender = false;

                    icon.Animations["idle"] = new GameObjectAnimation
                    {
                        Loop = true,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png",
                            64, 64, 1, 1.0f,
                            offsetX: 64 * 5, offsetY: 64 * 1,
                            scaleX: 0.8f, scaleY: 0.8f
                        )
                    };
                }

                icon.CurrentAnimationName = "idle";
                icon.Opacity = 0.8f;
            }
        }

        /// <summary>
        /// 予測アイコン生成
        /// </summary>
        private void AddPredictionIcon(ResponseType rsp, int slotIndex)
        {
            int offsetX = rsp switch
            {
                ResponseType.Evade => 64 * 2,
                ResponseType.CounterSlash => 64 * 3,
                ResponseType.CounterDown => 64 * 4,
                _ => 64 * 2
            };

            float startX = Common.CanvasWidth / 2 - 32;
            float startY = Common.AttackButtonHeight + 50;

            var icon = new UIObject
            {
                Name = $"PredictionIcon_{slotIndex}",
                ZIndex = 20000,
                // 初期位置 = ボタン中央
                PosX = startX,
                PosY = startY,
                TargetPosX = 30 + slotIndex * 50,
                TargetPosY = Common.ReserveIconHeight + 65,
                EnableLerp = true,  // ★ これで補間有効化
                MoveLerpSpeed = 5f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = true,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png",
                            64, 64, 1, 1.0f,
                            offsetX, offsetY: 64, scaleX: 0.8f, scaleY: 0.8f
                        )
                    }}
                },
            };

            AddUI(icon);
            _responseIcons[slotIndex] = icon;
        }

        /// <summary>
        /// 有利不利アイコンの生成
        /// </summary>
        private void ShowAdvantageIcon(ActionOutcome outcome, int slotIndex, bool force = false)
        {
            var act = _ctx.ReserveQueue.FirstOrDefault(a => a.SlotIndex == slotIndex);

            // 非公開のときだけ抑制
            if (act != null && !act.IsRevealedToDefender)
            {
                if (!force) return; // 非公開枠はforce指定のときだけ描画
            }

            if (!_reserveIcons.TryGetValue(slotIndex, out var reserveIcon))
                return;

            // ★ 既存アイコンがあれば削除
            if (_advantageIcons.TryGetValue(slotIndex, out var oldIcon))
            {
                RemoveUI(oldIcon);
                _advantageIcons.Remove(slotIndex);
            }

            int offsetX = outcome.OutcomeType switch
            {
                "攻撃者・有利" => 64 * 1,
                "攻撃者・不利" => 64 * 0,
                "引き分け" => 64 * 2,
                _ => 64 * 2
            };

            var icon = new UIObject
            {
                Name = $"有利不利アイコン_{slotIndex}",
                ZIndex = 21000,
                PosX = reserveIcon.PosX + 10,
                PosY = reserveIcon.PosY + 40,
                Opacity = 0f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = true,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png",
                            64, 64, 2, 0.5f,
                            offsetX, offsetY: 128, scaleX: 0.5f, scaleY: 0.5f
                        )
                    }}
                },
                CurrentAnimationName = "idle"
            };

            AddUI(icon);
            _advantageIcons[slotIndex] = icon;

            icon.StartFadeIn(0.5f);
        }

        /// <summary>
        /// ターン終了時に一時的なUIを全て消去
        /// </summary>
        private void ClearTurnIcons()
        {
            // 予約アイコン（辞書管理外も含めて全削除）
            var extraReserve = UIObjects.Values
                .Where(u => u.Name.StartsWith("予約アイコン_"))
                .ToList();
            foreach (var ui in extraReserve)
                RemoveUI(ui);

            _reserveIcons.Clear();

            // 対応行動アイコン
            foreach (var icon in _responseIcons.Values.ToList())
                RemoveUI(icon);
            _responseIcons.Clear();

            // 有利不利アイコン
            foreach (var icon in _advantageIcons.Values.ToList())
                RemoveUI(icon);
            _advantageIcons.Clear();

            // 予約マスク
            var extraMasks = UIObjects.Values
                .Where(u => u.Name.StartsWith("予約マスク_"))
                .ToList();
            foreach (var mask in extraMasks)
                RemoveUI(mask);
            _reserveMasks.Clear();

            // 予約枠
            var frameUIs = UIObjects.Values
                .Where(u => u.Name.StartsWith("予約枠_"))
                .ToList();
            foreach (var ui in frameUIs)
                RemoveUI(ui);
        }

        /// <summary>
        /// アイコンを強制スナップ
        /// </summary>
        private void SnapIconsToTarget()
        {
            // 1. 予約アイコンをターゲット位置へ強制スナップ
            foreach (var icon in _reserveIcons.Values.ToList())
            {
                if (icon.EnableLerp)
                {
                    icon.PosX = icon.TargetPosX;
                    icon.PosY = icon.TargetPosY;
                }
            }

            // 2. 予測アイコンをリセット
            foreach (var kv in _responseIcons)
            {
                int slotIndex = kv.Key;
                var rspIcon = kv.Value;

                if (_reserveIcons.TryGetValue(slotIndex, out var reserve))
                {
                    rspIcon.PosX = reserve.PosX;
                    rspIcon.PosY = reserve.PosY + 65; // ★ 生成時と同じオフセット
                }
            }

            // 3. 有利不利アイコンをリセット
            foreach (var kv in _advantageIcons)
            {
                int slotIndex = kv.Key;
                var advIcon = kv.Value;

                if (_reserveIcons.TryGetValue(slotIndex, out var reserve))
                {
                    advIcon.PosX = reserve.PosX + 10;
                    advIcon.PosY = reserve.PosY + 40; // ★ 生成時と同じオフセット
                }
            }

            // 4. 予約マスクアイコンをリセット
            foreach (var act in _ctx.ReserveQueue)
            {
                if (!_reserveIcons.TryGetValue(act.SlotIndex, out var reserve)) continue;

                if (TryGetUI($"予約マスク_{act.SlotIndex}", out var mask))
                {
                    mask.PosX = reserve.PosX;
                    mask.PosY = reserve.PosY;
                }
            }
        }

        /// <summary>
        /// 戦闘中マスク画像の生成
        /// </summary>
        private void ShowDuelMask()
        {
            if (TryGetUI("戦闘中マスク画像", out var ui))
                return;

            var mask = new UIObject
            {
                Name = "戦闘中マスク画像",
                ZIndex = 0,
                PosX = 0,
                PosY = 0,
                Opacity = 0.3f, // 半透明度
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui06-00.png", 360, 640, 1, 1.0f
                        )
                    }}
                },
                CurrentAnimationName = "idle",

                // 入力を遮らないように
                Enabled = false, // ← ここ重要
            };

            AddUI(mask);
        }

        /// <summary>
        /// 戦闘中マスク画像の削除
        /// </summary>
        private void HideDuelMask()
        {
            if(TryGetUI("戦闘中マスク画像", out var ui))
            {
                RemoveUI(ui);
            }
        }

        // === ステータスパネル ===

        /// <summary>
        /// ステータスパネルの生成
        /// </summary>
        private void CreateStatusPanelFor(Character ch)
        {
            var panel = new UIObject
            {
                Name = $"StatusPanel_{ch.Name}",
                ZIndex = 15000,
                Opacity = 0.9f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui01-00.png",
                            360, 180,
                            count: 1,
                            duration: 1.0f,
                            offsetY: ch.MirrorHorizontal ? 180*2 : 180*3,
                            scaleX: 0.2f,
                            scaleY: 0.2f
                        )
                    }}
                },
                CurrentAnimationName = "idle"
            };

            // 子UIとして HPバー / HP数値 / ATBバー を追加
            var hpGreenBar = CreateHpBarChild(ch);
            var hpRedBar = CreateHpBarChild(ch); // 赤用に別インスタンス
            hpRedBar.TintColor = "#FF0000";        // 初期色
            var hpText = CreateHpTextChild(ch);
            var atbBar = CreateAtbBarChild(ch);
            var name = CreateNameTextChild(ch);
            var hands = CreateHandIconsChild(ch);

            panel.Children.Add(hpGreenBar);
            panel.Children.Add(hpRedBar);
            panel.Children.Add(hpText);
            panel.Children.Add(atbBar);
            panel.Children.Add(name);
            panel.Children.Add(hands);

            // 親子関係を反映
            hpGreenBar.Parent = panel;
            hpRedBar.Parent = panel;
            hpText.Parent = panel;
            atbBar.Parent = panel;
            name.Parent = panel;
            hands.Parent = panel;

            _statusPanels[ch.ObjectId] = panel;
            AddUI(panel);

            // ★ 子参照をキャッシュ
            _statusPanelRefs[ch.ObjectId] = new StatusPanelRefs
            {
                Panel = panel,
                HpBar = hpGreenBar,
                HpBarFatal = hpRedBar,
                HpText = hpText,
                AtbBar = atbBar,
                NameText = name,
                Hands = hands
            };
        }

        /// <summary>
        /// 全キャラのステータスパネルを生成
        /// </summary>
        private void CreateAllStatusPanels()
        {
            foreach (var ch in Characters.Values.ToList()) CreateStatusPanelFor(ch);
        }

        /// <summary>
        /// ステータスパネルの更新（差分描画対応）
        /// </summary>
        private void UpdateStatusPanels(float deltaTime)
        {
            foreach (var ch in Characters.Values.ToList())
            {
                if (!_statusPanelRefs.TryGetValue(ch.ObjectId, out var refs)) continue;

                var panel = refs.Panel;
                var hpBar = refs.HpBar;        // 緑ゲージ（残痕）
                var hpBarFatal = refs.HpBarFatal; // 赤ゲージ（致命）
                var hpText = refs.HpText;
                var atbBar = refs.AtbBar;
                var nameText = refs.NameText;
                var hands = refs.Hands;

                // パネル位置 = キャラに追従
                var sp = ch.GetCurrentFrameSprite();
                float w = (sp?.SourceRect.Width ?? 0);
                float h = (sp?.SourceRect.Height ?? 0);

                panel.PosX = Math.Clamp(
                    ch.BattlePosX + (w - 120) * 0.5f + (ch.MirrorHorizontal ? 48 : 0),
                    0, Common.CanvasWidth - 68);
                panel.PosY = ch.BattlePosY + h -8.0f;

                // --- HP残量比率 ---
                float visibleHpRatio = (float)(ch.CurrentStats.ResidualHP + ch.CurrentStats.FatalHP)
                                       / ch.CurrentStats.MaxHP;

                // 名前テキストは常時描画（差分制御なし）
                if (nameText != null)
                {
                    if (visibleHpRatio <= 0f)
                        nameText.TextColor = "#FF0000"; // 赤
                    else if (visibleHpRatio < 0.5f)
                        nameText.TextColor = "#FFFF00"; // 黄
                    else
                        nameText.TextColor = "#FFFFFF"; // 白;

                    nameText.Text = ch.Name;
                    nameText.ZIndex = 15010;
                }

                // HPバー（差分描画）
                // --- HPバー更新（緑＋赤） ---
                if (hpBar != null && hpBarFatal != null)
                {
                    var frameGreen = hpBar.GetCurrentAnimationFrame();
                    var frameRed = hpBarFatal.GetCurrentAnimationFrame();

                    if (frameGreen?.Sprite != null && frameRed?.Sprite != null)
                    {
                        // 補間更新
                        UpdateHpDisplay(ch.CurrentStats, deltaTime);

                        float totalWidth = 60f;
                        float greenWidth = totalWidth * ch.CurrentStats.DisplayResidual;
                        float redWidth = totalWidth * ch.CurrentStats.DisplayFatal;

                        // --- 緑バー（残痕HP） ---
                        float newGreenWidth = MathF.Max(1f, greenWidth); // ★最低1px
                        if (Math.Abs(frameGreen.Sprite.ScaleX - newGreenWidth) > 0.5f)
                        {
                            frameGreen.Sprite.ScaleX = newGreenWidth;
                            frameGreen.Sprite.ScaleY = 4f;
                            hpBar.TintColor = "#00BB00";
                            if (ch.CurrentStats.DisplayResidual <= 0.001f)
                            {
                                hpBar.Visible = false;
                            }
                            else
                            {
                                hpBar.Visible = true;
                            }

                            hpBar.ZIndex = 15010;
                            hpBar.MarkDirty();
                        }

                        // --- 赤バー（致命HP） ---
                        float newRedWidth = MathF.Max(1f, redWidth); // ★最低1px
                        if (Math.Abs(frameRed.Sprite.ScaleX - newRedWidth) > 0.5f)
                        {
                            frameRed.Sprite.ScaleX = newRedWidth;
                            frameRed.Sprite.ScaleY = 4f;
                            hpBarFatal.TintColor = "#DD0000";
                            if (ch.CurrentStats.DisplayFatal <= 0.001f)
                            {
                                hpBarFatal.Visible = false;
                            }
                            else
                            {
                                hpBarFatal.Visible = true;
                            }
                            hpBarFatal.ZIndex = 15010;
                            hpBarFatal.MarkDirty();
                        }

                        // 赤バーは緑の右端に配置
                        hpBarFatal.PosX = hpBar.PosX + greenWidth;
                    }
                }

                // --- HPテキスト ---
                if (hpText != null)
                {
                    int visibleHp = ch.CurrentStats.ResidualHP + ch.CurrentStats.FatalHP;
                    hpText.Text = $"{visibleHp}/{ch.CurrentStats.MaxHP}";
                    hpText.TextAlign = "right";
                    hpText.ZIndex = 15030;
                }

                // ATBバー（差分描画）
                if (atbBar != null)
                {
                    float fill01 = Math.Clamp(GetAtbFill01(ch.ObjectId), 0f, 1f);

                    var frame = atbBar.GetCurrentAnimationFrame();
                    if (frame?.Sprite != null)
                    {
                        float newScale = MathF.Max(1f, 40f * fill01);
                        if (Math.Abs(frame.Sprite.ScaleX - newScale) > 0.5f)
                        {
                            frame.Sprite.ScaleX = newScale;
                            atbBar.ZIndex = 15010;
                            atbBar.MarkDirty();
                        }
                    }

                    string newTint = LerpColor(AtbStart, AtbEnd, fill01);
                    if (atbBar.TintColor != newTint)
                    {
                        atbBar.TintColor = newTint;
                        atbBar.ZIndex = 15010;
                        atbBar.MarkDirty();
                    }
                }

                // 残手数アイコン（差分描画）
                if (hands != null)
                {
                    foreach (var atk in new[] { AttackType.Thrust, AttackType.Slash, AttackType.Down })
                    {
                        int max = ch.CurrentStats.MaxHands[atk];
                        int remain = ch.CurrentStats.RemainingHands.TryGetValue(atk, out var r) ? r : 0;

                        for (int i = 0; i < 6; i++)
                        {
                            var marker = hands.Children.FirstOrDefault(m => m.Name == $"{atk}_{ch.Name}_Marker{i}");
                            if (marker == null) continue;

                            int baseCount = ch.CurrentStats.EndOfTurnHands.TryGetValue(atk, out var b) ? b : 0;

                            if (i < baseCount)
                            {
                                marker.Visible = true;

                                if (i < remain)
                                {
                                    // まだ残っている → 中抜き
                                    if (marker.CurrentAnimationName != "empty")
                                    {
                                        marker.CurrentAnimationName = "empty";
                                        marker.MarkDirty();
                                    }
                                }
                                else
                                {
                                    // すでに使ったぶん → 塗りつぶし
                                    if (marker.CurrentAnimationName != "used")
                                    {
                                        marker.CurrentAnimationName = "used";
                                        marker.MarkDirty();
                                    }
                                }
                            }
                            else
                            {
                                // ターン終了後に回復したぶん
                                if (i < remain)
                                {
                                    marker.Visible = true;
                                    if (marker.CurrentAnimationName != "empty")
                                    {
                                        marker.CurrentAnimationName = "empty";
                                        marker.MarkDirty();
                                    }
                                }
                                else
                                {
                                    marker.Visible = false;
                                }
                            }
                        }
                    }
                }
            }

            // ★ ステータス更新後にハイライトを追従させる
            if (_highlightUI != null && Characters.TryGetValue(_ctx.CurrentActorId, out var actor))
            {
                // PosX/PosY ではなく BattlePosX/Y を使う
                float snapX = actor.MirrorHorizontal ? actor.BattlePosX + 25 : actor.BattlePosX;
                float snapY = actor.BattlePosY + 95;

                _highlightUI.PosX = snapX;
                _highlightUI.PosY = snapY;
            }
        }

        // === ユーティリティ ===

        /// <summary>
        /// HP表示更新
        /// </summary>
        private void UpdateHpDisplay(CharacterStats ch, float deltaTime)
        {
            float speed = 5f; // Lerp速度（大きいほど速い）

            // 目標値
            float targetResidual = (float)ch.ResidualHP / ch.MaxHP;
            float targetFatal = (float)ch.FatalHP / ch.MaxHP;

            // 緑ゲージ（残痕HP）
            ch.DisplayResidual += (targetResidual - ch.DisplayResidual) * speed * deltaTime;

            // 赤ゲージ（致命HP）
            ch.DisplayFatal += (targetFatal - ch.DisplayFatal) * speed * deltaTime;
        }

        /// <summary>
        /// 2色を比率で線形補間して返す
        /// </summary>
        private static string LerpColor(ColorRgb start, ColorRgb end, float t)
        {
            t = Math.Clamp(t, 0f, 1f);

            int r = (int)(start.R + (end.R - start.R) * t);
            int g = (int)(start.G + (end.G - start.G) * t);
            int b = (int)(start.B + (end.B - start.B) * t);

            return $"#{r:X2}{g:X2}{b:X2}";
        }

        /// <summary>
        /// キャラのHPバーを生成
        /// </summary>
        private UIObject CreateHpBarChild(Character ch)
        {
            return new UIObject
            {
                Name = $"HPBar_{ch.Name}",
                PosX = 5f,
                PosY = 23f,
                Opacity = 1f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui02-00.png", new System.Drawing.Rectangle(11*40, 0, 1, 1))
                                {
                                    ScaleX = 40f, // 初期の長さ（仮）
                                    ScaleY = 4f   // 高さ
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
        }

        /// <summary>
        /// ATBバーの生成
        /// </summary>
        private UIObject CreateAtbBarChild(Character ch)
        {
            return new UIObject
            {
                Name = $"ATBBar_{ch.Name}",
                PosX = 25.3f,
                PosY = 28f,
                Opacity = 1f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui02-00.png", new System.Drawing.Rectangle(11*40, 0, 1, 1))
                                {
                                    ScaleX = 40f, // 初期長さ（最大40px）
                                    ScaleY = 4f   // 高さ
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
        }

        /// <summary>
        /// HP数値表示の生成
        /// </summary>
        private UIObject CreateHpTextChild(Character ch)
        {
            return new UIObject
            {
                Name = $"HPText_{ch.Name}",
                PosX = 65f,
                PosY = 21f,
                Opacity = 1f,
                FontSize = 8,
                FontFamily = "monospace",
                TextColor = "#FFFFFF",
                Text = ""
            };
        }

        /// <summary>
        /// キャラ名テキストの生成
        /// </summary>
        private UIObject CreateNameTextChild(Character ch)
        {
            return new UIObject
            {
                Name = $"NameText_{ch.Name}",
                PosX = 5f,
                PosY = 11f,
                Opacity = 1f,
                FontSize = 10,
                FontFamily = "\"Yu Gothic UI, Meiryo, Hiragino Sans, Noto Sans CJK JP, sans-serif\"",
                TextColor = "#FFFFFF",
                Text = ch.Name              // 初期テキスト = キャラ名
            };
        }

        /// <summary>
        /// 残り手数アイコン
        /// </summary>
        private UIObject CreateHandIconsChild(Character ch)
        {
            var container = new UIObject
            {
                Name = $"HandsContainer_{ch.Name}",
                PosX = (ch.Type == "Enemy") ? 46f : 0f,
                PosY = -13f,
                ZIndex = 23000,
                Opacity = 0.8f,
            };

            var atkTypes = new[] { AttackType.Thrust, AttackType.Slash, AttackType.Down };
            for (int i = 0; i < atkTypes.Length; i++)
            {
                var atk = atkTypes[i];

                string tint = atk switch
                {
                    AttackType.Thrust => "#FFFF66", // 黄
                    AttackType.Slash => "#66AAFF", // 青
                    AttackType.Down => "#FF6666", // 赤
                    _ => "#FFFFFF"
                };

                for (int j = 0; j < 6; j++)
                {
                    var marker = new UIObject
                    {
                        Name = $"{atk}_{ch.Name}_Marker{j}",
                        PosX = i * 8f,   // 種類ごとに横並び
                        PosY = -j * 8f,  // 残数ごとに縦積み
                        Opacity = 1f,   // ★常に表示
                        ZIndex = j + 1,
                        Animations = new Dictionary<string, GameObjectAnimation>
                        {
                            { "empty", new GameObjectAnimation {
                                Loop = true,
                                Frames = GameObjectAnimation.CreateFramesFromSheet(
                                    "images/ui04-00.png", 64, 64, 1, 0.3f,
                                    offsetX: 64*6, scaleX:0.15f, scaleY:0.18f)
                            }},
                            { "used", new GameObjectAnimation {
                                Loop = true,
                                Frames = GameObjectAnimation.CreateFramesFromSheet(
                                    "images/ui04-00.png", 64, 64, 1, 0.3f,
                                    offsetX: 64*6, offsetY: 64, scaleX:0.15f, scaleY:0.18f)
                            }},
                        },
                        CurrentAnimationName = "full",
                        Parent = container,
                        TintColor = tint   // ★ ここで色指定
                    };
                    container.Children.Add(marker);
                }
            }

            return container;
        }

        /// <summary>
        /// ヒットエフェクト（血しぶき）をキャラの上に表示
        /// </summary>
        private void ShowBloodEffect(Character target)
        {
            var sheetPath = "images/ui04-00.png";

            var effect = new UIObject
            {
                Name = $"Blood_{target.ObjectId}_{Guid.NewGuid()}",
                ZIndex = 30000,
                Opacity = 1f,
                PosX = target.PosX + (target.MirrorHorizontal ? 80 : 60),
                PosY = target.PosY + 85,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "play", new GameObjectAnimation {
                        Loop = false,
                        Frames = new List<GameObjectAnimationFrame>
                        {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(sheetPath, new Rectangle(64*5, 0, 64, 64)) {
                                    ScaleX = 0.1f, ScaleY = 0.1f
                                },
                                OffsetX = - 0.1f*32,
                                OffsetY = - 0.1f*32,
                                Duration = 0.25f,
                            },
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(sheetPath, new Rectangle(64*5, 0, 64, 64)) {
                                    ScaleX = 0.5f, ScaleY = 0.5f
                                },
                                OffsetX = - 0.5f*32,
                                OffsetY = - 0.5f*32,
                                Duration = 0.10f,
                            },
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(sheetPath, new Rectangle(64*5, 0, 64, 64)) {
                                    ScaleX = 1.0f, ScaleY = 1.0f
                                },
                                OffsetX = - 1.0f*32,
                                OffsetY = - 1.0f*32,
                                Duration = 0.10f,
                            },
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(sheetPath, new Rectangle(64*5, 0, 64, 64)) {
                                    ScaleX = 1.5f, ScaleY = 1.5f
                                },
                                OffsetX = - 1.5f*32,
                                OffsetY = - 1.5f*32,
                                Duration = 0.10f,
                            },
                        }
                    }}
                },
                CurrentAnimationName = "play"
            };

            // ★終了イベントを購読して自動削除
            effect.OnAnimationCompleted += (obj, anim) =>
            {
                if (anim == "play")
                    RemoveUI(effect);
            };

            AddUI(effect);
        }

        /// <summary>
        /// ダメージポップアップ表示
        /// </summary>
        private void ShowDamagePopup(Character target, int residualDamage, int fatalDamage)
        {
            float baseX = target.PosX + (target.MirrorHorizontal ? 80 : 40); // キャラ位置にあわせて調整
            float baseY = target.PosY + 50;

            if (residualDamage > 0)
            {
                var popup = new DamagePopup(residualDamage, isFatal: false)
                {
                    PosX = baseX,
                    PosY = baseY
                };
                AddUI(popup);
            }

            if (fatalDamage > 0)
            {
                var popup = new DamagePopup(fatalDamage, isFatal: true)
                {
                    PosX = baseX + 20, // ずらして重ならないように
                    PosY = baseY - 10
                };
                AddUI(popup);
            }
        }

        // === 解決ハイライト ===

        /// <summary>
        /// 実行中行動のハイライトアイコンの生成
        /// </summary>
        private void EnsureResolveHighlight()
        {
            if (_resolveHighlight != null) return;

            _resolveHighlight = new UIObject
            {
                Name = "ResolveHighlight",
                ZIndex = 11000,
                Opacity = 0.4f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = true,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui02-00.png",
                            40, 40, offsetY: 40*2,
                            count: 2,
                            duration: 0.2f,
                            scaleX: 1.4f,
                            scaleY: 3.2f
                        )
                    }}
                },
                CurrentAnimationName = "idle"
            };

            AddUI(_resolveHighlight);
        }

        /// <summary>
        /// 実行中行動のハイライトアイコン更新
        /// </summary>
        private void UpdateResolveHighlight()
        {
            if (_currentResolvingSlot >= 0 && _reserveIcons.TryGetValue(_currentResolvingSlot, out var icon))
            {
                EnsureResolveHighlight();
                if (_resolveHighlight != null)
                {
                    _resolveHighlight.PosX = icon.PosX - 2f; // 少し大きめに囲む
                    _resolveHighlight.PosY = icon.PosY - 4f;
                    _resolveHighlight.Visible = true;
                }
            }
            else
            {
                if (_resolveHighlight != null)
                    _resolveHighlight.Visible = false;
            }
        }

        /// <summary>
        /// 実行中行動のハイライトアイコン削除
        /// </summary>
        private void ClearResolveHighlight()
        {
            if (_resolveHighlight != null)
            {
                RemoveUI(_resolveHighlight);
                _resolveHighlight = null;
            }
        }

        /// <summary>
        /// フェーズごとのUI表示切り替え
        /// </summary>
        private void HandlePhaseUiVisibility(BattlePhase next)
        {
            // --- 予約行動選択ボタン ---
            if (TryGetUI("予約行動選択ボタン", out var atkBtn))
            {
                if (next == BattlePhase.Reservation_SelectAttack && _ctx.PlayerIsReservationSide)
                {
                    (atkBtn as UIObject)?.StartFadeIn(0.4f);
                }
                else
                {
                    atkBtn.Visible = false;
                    atkBtn.Opacity = 0.0f;
                }
            }

            // --- 予測行動選択ボタン ---
            if (TryGetUI("予測行動選択ボタン", out var respBtn))
            {
                if (next == BattlePhase.Prediction_SelectAttack && !_ctx.PlayerIsReservationSide)
                {
                    (respBtn as UIObject)?.StartFadeIn(0.4f);
                }
                else
                {
                    respBtn.Visible = false;
                    respBtn.Opacity = 0.0f;
                }
            }

            // --- 用意完了ボタン ---
            if (TryGetUI("用意完了ボタン", out var confirmBtn))
            {
                if (next == BattlePhase.Reservation_SelectAttack &&
                    Characters[_ctx.CurrentActorId].Type == "Player")
                {
                    (confirmBtn as UIObject)?.StartFadeIn(0.4f);
                }
                else
                {
                    confirmBtn.Visible = false;
                    confirmBtn.Opacity = 0.0f;
                }
            }

            // --- 勝負ボタン ---
            if (TryGetUI("勝負ボタン", out var shobuBtn))
            {
                if (next == BattlePhase.Prediction_Confirm)
                    (shobuBtn as UIObject)?.StartFadeIn(0.4f);
                else
                {
                    shobuBtn.Visible = false;
                    shobuBtn.Opacity = 0.0f;
                }
            }

            // --- 決着ボタン ---
            if (TryGetUI("決着ボタン", out var ketchakuBtn))
            {
                if (next == BattlePhase.Prediction_Confirm)
                    (ketchakuBtn as UIObject)?.StartFadeIn(0.4f);
                else
                {
                    ketchakuBtn.Visible = false;
                    ketchakuBtn.Opacity = 0.0f;
                }
            }
        }

        /// <summary>
        /// 現在行動中キャラ（プレイヤー）の忍具アイコンを表示
        /// </summary>
        private void ShowNinguIconForActivePlayer()
        {
            // 既存アイコンを削除
            foreach (var ic in _ninguIcons.Values.ToList())
                RemoveUI(ic);
            _ninguIcons.Clear();

            Character? ch = null;

            // === フェーズごとにキャラを判定 ===
            if (_ctx.PlayerIsReservationSide)
            {
                // 予約側がプレイヤーなら、今のActorがそのキャラ
                if (Characters.TryGetValue(_ctx.CurrentActorId, out var c) && c.Type == "Player")
                    ch = c;
            }
            else if (!_ctx.PlayerIsReservationSide)
            {
                // 予測フェーズ中は、敵が行動側
                if (Characters.TryGetValue(_ctx.CurrentTargetId, out var c) && c.Type == "Player")
                    ch = c;
            }

            if (ch == null) return;

            var eq = EquipmentManager.Instance.GetEquipped(ch, "忍具");
            if (eq == null) return;

            // === フェーズ適合チェック ===
            bool isReservationPhase = _ctx.PlayerIsReservationSide;
            bool isPredictionPhase = !_ctx.PlayerIsReservationSide;

            if ((eq.UsePhase == NinguPhaseType.Reservation && !isReservationPhase) ||
                (eq.UsePhase == NinguPhaseType.Prediction && !isPredictionPhase))
            {
                // この忍具は今のフェーズでは非表示
                return;
            }

            var icon = new UIObject
            {
                Name = $"忍具アイコン_{ch.ObjectId}",
                PosX = 20,
                PosY = Common.CanvasHeight - 200,
                ZIndex = 16000,
                Opacity = _ninguUsed.Contains(ch.ObjectId) ? 0.3f : 0.9f,
                Enabled = !_ninguUsed.Contains(ch.ObjectId),
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            eq.SpriteSheet, 64, 64, 1, 1.0f,
                            eq.SrcX, eq.SrcY, 0.8f, 0.8f)
                    }}
                },
                CurrentAnimationName = "idle",
                OnClick = () => UseNingu(ch, eq)
            };

            AddUI(icon);
            _ninguIcons[ch.ObjectId] = icon;
        }

        /// <summary>
        /// プレイヤーが行動入力を開始したら忍具アイコンを消す（幻煙玉・先見丹用）
        /// </summary>
        private void CheckAndHideNinguIconIfUsed()
        {
            // 何もアイコンが出ていなければ無視
            if (_ninguIcons.Count == 0)
                return;

            foreach (var kv in _ninguIcons.ToList())
            {
                var icon = kv.Value;

                RemoveUI(icon);
                _ninguIcons.Remove(kv.Key);
            }
        }

        /// <summary>
        /// 回復ポップ（緑文字）
        /// </summary>
        private async Task ShowHealPopupAsync(Character target, int amount)
        {
            float baseX = target.PosX + (target.MirrorHorizontal ? 80 : 40);
            float baseY = target.PosY + 50;

            var effect = new UIObject
            {
                Name = $"HealEffect_{Guid.NewGuid()}",
                PosX = baseX - 20,
                PosY = baseY + 20,
                ZIndex = 40000,
                Opacity = 0.6f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "play", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png", 64, 64, 2, 0.3f,
                            offsetX: 64 * 10, offsetY: 64 * 2,
                            scaleX: 1.0f, scaleY: 1.0f)
                    }}
                },
                CurrentAnimationName = "play"
            };

            effect.OnAnimationCompleted += (obj, anim) =>
            {
                RemoveUI(effect);
            };

            AddUI(effect);

            var popup = new UIObject
            {
                Name = $"HealPopup_{Guid.NewGuid()}",
                PosX = baseX,
                PosY = baseY,
                ZIndex = 21000,
                Text = $"+{amount}",
                FontSize = 18,
                // 必要なら FontFamily = "\"Yu Mincho, serif\"",
                TextColor = "#66FF66", // ← 緑
                Opacity = 1.0f,
                StretchToText = true
            };

            AddUI(popup);

            // 上方向に浮かせつつフェードアウト（簡易アニメ）
            const int steps = 22;
            for (int i = 0; i < steps; i++)
            {
                popup.PosY -= 1.5f;
                popup.Opacity = 1.0f - (i / (float)steps);
                popup.MarkDirty();
                await Task.Delay(20);
            }

            RemoveUI(popup);
        }

        /// <summary>
        /// 煙玉エフェクト
        /// </summary>
        private void ShowSmokeEffect(float x, float y)
        {
            var smoke = new UIObject
            {
                Name = $"SmokeEffect_{Guid.NewGuid()}",
                PosX = x - 32,
                PosY = y - 32,
                ZIndex = 40000,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "play", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png", 64, 64, 2, 0.3f,
                            offsetX: 64 * 7, offsetY: 64 * 2,
                            scaleX: 1.0f, scaleY: 1.0f)
                    }}
                },
                CurrentAnimationName = "play"
            };

            smoke.OnAnimationCompleted += (obj, anim) =>
            {
                RemoveUI(smoke);
            };

            AddUI(smoke);
        }

        /// <summary>
        /// 先見丹使用時の目アイコン＋画面フラッシュ演出
        /// </summary>
        private async Task ShowEyeFlashEffectAsync()
        {
            // === 1. 白フラッシュ ===
            var flash = new UIObject
            {
                Name = "FlashEffect",
                PosX = 0,
                PosY = 0,
                ZIndex = 999999,
                Opacity = 0.9f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui06-02.png",
                                    new Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            AddUI(flash);

            // === 2. 目アイコン ===
            var eye = new UIObject
            {
                Name = "EyeEffect",
                CenterX = true,
                PosY = Common.CanvasHeight / 2 - 150,
                ZIndex = 900000,
                Opacity = 0.9f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png",
                            64, 64, 1, 1.0f,
                            offsetX: 64*4, offsetY: 64*3,
                            scaleX: 1.4f, scaleY: 1.4f)
                    }}
                },
                CurrentAnimationName = "idle"
            };
            AddUI(eye);

            // フェードアウト
            for (int i = 0; i < 5; i++)
            {
                flash.Opacity = 1f - i / 5f;
                flash.MarkDirty();
                await Task.Delay(15);
            }
            RemoveUI(flash);
            RemoveUI(eye);
        }

        /// <summary>
        /// 影分身使用時の演出
        /// </summary>
        private async Task ShowKageBunshinEffectAsync(float x, float y)
        {
            // === 1. 白フラッシュ ===
            var flash = new UIObject
            {
                Name = "FlashEffect",
                PosX = 0,
                PosY = 0,
                ZIndex = 999999,
                Opacity = 0.9f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui06-02.png",
                                    new Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            AddUI(flash);

            // === 2. 影分身エフェクト ===
            var effect = new UIObject
            {
                Name = $"KageEffect_{Guid.NewGuid()}",
                PosX = x,
                PosY = y,
                ZIndex = 40000,
                Opacity = 0.6f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "play", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png", 64, 64, 2, 0.2f,
                            offsetX: 64 * 11, offsetY: 64 * 0,
                            scaleX: 1.0f, scaleY: 1.0f)
                    }}
                },
                CurrentAnimationName = "play"
            };

            effect.OnAnimationCompleted += (obj, anim) =>
            {
                RemoveUI(effect);
            };

            AddUI(effect);

            // フェードアウト
            for (int i = 0; i < 5; i++)
            {
                flash.Opacity = 1f - i / 5f;
                flash.MarkDirty();
                await Task.Delay(15);
            }
            RemoveUI(flash);
        }

        /// <summary>
        /// 影縫い使用時の演出
        /// </summary>
        private async Task ShowKageNuiEffectAsync(float x, float y)
        {
            // === 1. 白フラッシュ ===
            var flash = new UIObject
            {
                Name = "FlashEffect",
                PosX = 0,
                PosY = 0,
                ZIndex = 999999,
                Opacity = 0.9f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui06-02.png",
                                    new Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            AddUI(flash);

            // === 2. 影縫いエフェクト ===
            var effect = new UIObject
            {
                Name = $"KageEffect_{Guid.NewGuid()}",
                PosX = x,
                PosY = y,
                ZIndex = 40000,
                Opacity = 0.6f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "play", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png", 64, 64, 2, 0.3f,
                            offsetX: 64 * 12, offsetY: 64 * 0,
                            scaleX: 1.3f, scaleY: 1.3f)
                    }}
                },
                CurrentAnimationName = "play"
            };

            effect.OnAnimationCompleted += (obj, anim) =>
            {
                RemoveUI(effect);
            };

            AddUI(effect);

            // フェードアウト
            for (int i = 0; i < 5; i++)
            {
                flash.Opacity = 1f - i / 5f;
                flash.MarkDirty();
                await Task.Delay(15);
            }
            RemoveUI(flash);
        }

        /// <summary>
        /// ステータスパネルの子UI参照まとめ
        /// </summary>
        private sealed class StatusPanelRefs
        {
            public UIObject Panel { get; set; }
            public UIObject HpBar { get; set; }        // 緑
            public UIObject HpBarFatal { get; set; }   // 赤 ←追加
            public UIObject HpText { get; set; }
            public UIObject AtbBar { get; set; }
            public UIObject NameText { get; set; }
            public UIObject Hands { get; set; }
        }

        /// <summary>
        /// 背景をフェードアウト→差し替え→フェードインする演出
        /// </summary>
        /// <param name="newBackground">切り替え先のBackground</param>
        /// <param name="fadeSeconds">フェード時間（秒）</param>
        public async Task FadeBackgroundAsync(Background newBackground, float fadeSeconds = 1.0f)
        {
            // --- フェード用オーバーレイ ---
            var fadeOverlay = new UIObject
            {
                Name = "BackgroundFadeOverlay",
                PosX = 0,
                PosY = 0,
                ZIndex = 999999,
                Opacity = 0f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui06-01.png",
                                    new System.Drawing.Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                            }
                        }
                    }}
                }
            };
            AddUI(fadeOverlay);

            // --- フェードアウト ---
            int steps = 10;
            for (int i = 0; i <= steps; i++)
            {
                fadeOverlay.Opacity = i / (float)steps;
                fadeOverlay.MarkDirty();
                await Task.Delay((int)(fadeSeconds * 1000 / steps));
            }

            // --- 背景を差し替え ---
            Background = newBackground;
            Characters.Clear();

            // --- フェードイン ---
            for (int i = 0; i <= steps; i++)
            {
                fadeOverlay.Opacity = 1f - (i / (float)steps);
                fadeOverlay.MarkDirty();
                await Task.Delay((int)(fadeSeconds * 1000 / steps));
            }

            RemoveUI(fadeOverlay);
        }
    }

    /// <summary>RGBユーティリティ構造体</summary>
    public readonly struct ColorRgb
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;

        public ColorRgb(byte r, byte g, byte b)
        {
            R = r; G = g; B = b;
        }

        public static ColorRgb FromHex(string hex)
        {
            // 形式 "#RRGGBB" 前提
            return new ColorRgb(
                Convert.ToByte(hex.Substring(1, 2), 16),
                Convert.ToByte(hex.Substring(3, 2), 16),
                Convert.ToByte(hex.Substring(5, 2), 16)
            );
        }

        public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";
    }
}



