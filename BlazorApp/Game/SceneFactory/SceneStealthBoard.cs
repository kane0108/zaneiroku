using BlazorApp.Game.Battle;
using BlazorApp.Game.CharacterFactory;
using BlazorApp.Game.Stealth;
using BlazorApp.Game.UIObjectFactory;
using static System.Formats.Asn1.AsnWriter;

namespace BlazorApp.Game.SceneFactory
{
    /// <summary>
    /// 隠密探索盤シーン
    /// </summary>
    public class SceneStealthBoard : BaseSceneFactory
    {
        public override GameState TargetState => GameState.StealthBoard;

        private StealthBoardCell[,] _cells = new StealthBoardCell[0, 0];
        private Random _rand = new Random();

        private StealthMode _currentMode = StealthMode.Explore;

        private readonly List<UIObject> _footsteps = new(); // 足跡を古い順に記録

        private UIObject? _stealthMask = null;

        private (int x, int y)? _keyPos = null;
        private int _goalCols;
        private int _correctDoorIndex;

        private bool _missionFailed = false;

        private Character _player;                // 探索中のプレイヤーキャラ
        private UIObject _hpBarResidual;          // 緑
        private UIObject _hpBarFatal;             // 赤
        private UIObject _hpText;                 // 数値表示

        private UIObject _alertBar;

        private UIObject _itemCountText;
        private UIObject _moneyCountText;

        private int _extraAlertPoints = 0;

        private int _revealStepCounter = 0;

        // 取得回数
        private int _itemPickupCount = 0;
        private int _moneyPickupCount = 0;

        private CancellationTokenSource? _flashCtsItem;
        private CancellationTokenSource? _flashCtsMoney;

        private StealthBoardEventScripts _eventScripts;

        public override Scene Create(object? payload = null)
        {
            _missionFailed = false; // ★リセット
            _extraAlertPoints = 0;
            _revealStepCounter = 0;
            _itemPickupCount = 0;
            _moneyPickupCount = 0;

            // 警戒度リセット
            _footsteps.Clear();

            var setup = payload as StealthBoardSetup
                        ?? new StealthBoardSetup(); // デフォルトステージ

            var scene = new Scene
            {
                State = GameState.StealthBoard
            };

            // 背景
            scene.Background = setup.Background;

            // イベントスクリプト初期化
            _eventScripts = new StealthBoardEventScripts(scene, setup);

            // --- シーン初期化処理開始 ---
            _ = InitializeAsync(scene, setup);

            return scene;
        }
        public override Scene Create() => Create(null);

        /// <summary>
        /// シーン初期化処理
        /// </summary>
        private async Task InitializeAsync(Scene scene, StealthBoardSetup setup)
        {
            // 1. 背景を先に表示
            await Task.Delay(50); // 描画反映待ち（描画系のタイミング確保）

            var blocker = AddInputBlocker(scene);

            // 2. プロローグ再生            
            await _eventScripts.PlayEventScriptAsync("Prologue");

            // 3. プロローグ後にUI生成
            await CreateStealthUIAsync(scene, setup);

            // 4. チュートリアル再生            
            await _eventScripts.PlayEventScriptAsync("Tutorial");

            scene.RemoveUI(blocker);
        }

        /// <summary>
        /// UI生成
        /// </summary>
        private async Task CreateStealthUIAsync(Scene scene, StealthBoardSetup setup)
        {
            CreateStealthMask(scene);

            // --- プレイヤー初期化 ---

            // ★共通関数で装備・スキルを反映
            foreach (var ch in GameMain.Instance.PlayerParty)
            {
                EquipmentManager.Instance.ApplyEquipmentAndSkillsToCharacter(ch);
                ch.CurrentStats.ResetHP();             // 探索前はHP全回復
            }

            _player = GameMain.Instance.PlayerParty[0]; // 仮にプレイヤー１（探索者で特徴を持たせたい）

            // --- HPバーUI生成 ---
            CreateStealthHpUI(scene);

            // === タイルの自動配置 ===
            int cols = setup.Width;
            int rows = setup.Height;
            _cells = new StealthBoardCell[cols, rows];

            int marginX = 4, spacing = 2;
            float boardWidth = Common.CanvasWidth - marginX * 2;
            float tileSize = (boardWidth - (cols - 1) * spacing) / cols;
            float totalHeight = rows * tileSize + (rows - 1) * spacing;

            float startX = marginX;
            float startY = (Common.CanvasHeight - totalHeight) / 2f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    _cells[x, y] = new StealthBoardCell
                    {
                        X = x,
                        Y = y,
                        IsRevealed = false,
                        Trap = null,
                        EnemyInstance = null
                    };

                    var tile = new UIObject
                    {
                        Name = $"Tile_{x}_{y}",
                        PosX = startX + x * (tileSize + spacing),
                        PosY = startY + y * (tileSize + spacing),
                        Opacity = 0.6f,
                        ZIndex = 500,
                        TintColor = "#FFFFFF",
                        Animations = new Dictionary<string, GameObjectAnimation>
                        {
                            { "idle", new GameObjectAnimation {
                                Loop = false,
                                Frames = GameObjectAnimation.CreateFramesFromSheet(
                                    "images/ui04-00.png",
                                    64, 64, 1, 1.0f,
                                    offsetX:64*6, offsetY:64*2,
                                    scaleX: tileSize / 64f,
                                    scaleY: tileSize / 64f
                                )
                            }}
                        },
                        CurrentAnimationName = "idle"
                    };

                    int cx = x, cy = y;
                    tile.OnClick = () => RevealTile(scene, setup, cx, cy);
                    scene.AddUI(tile);
                }
            }

            // 共通：経験値描画
            scene.CreateGlobalExpUI();

            // ★ 罠と敵と鍵をランダム配置
            PlaceTrapsRandomly(setup);
            PlaceStealthEnemyRandomly(setup);
            PlaceKeyRandomly();
            PlaceRewardsRandomly(setup);
            CreateRewardListUI(scene);

            // === スタートライン（盤面の下） ===
            for (int x = 0; x < cols; x++)
            {
                var startTile = new UIObject
                {
                    Name = $"StartTile_{x}",
                    PosX = startX + x * (tileSize + spacing),
                    PosY = startY + rows * (tileSize + spacing), // ★最下行のさらに下
                    Opacity = 0.9f,
                    ZIndex = 10000,
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        { "idle", new GameObjectAnimation {
                            Loop = false,
                            Frames = GameObjectAnimation.CreateFramesFromSheet(
                                "images/ui04-00.png", // スタート用スプライト
                                64, 64, 1, 1.0f,
                                offsetX: 64 * 0, offsetY: 64 * 0, // ←適当な枠を割当
                                scaleX: tileSize / 64f,
                                scaleY: tileSize / 64f
                            )
                        }}
                    },
                    CurrentAnimationName = "idle",
                    Enabled = false // ★クリック無効化
                };
                scene.AddUI(startTile);
            }

            // === ゴールライン（盤面の上） ===
            _goalCols = cols;
            _correctDoorIndex = _rand.Next(cols);

            for (int x = 0; x < cols; x++)
            {
                int gx = x;

                var goalTile = new UIObject
                {
                    Name = $"GoalDoor_{x}",
                    PosX = startX + x * (tileSize + spacing),
                    PosY = startY - (tileSize + spacing),
                    ZIndex = 10000,
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        { "idle", new GameObjectAnimation {
                            Loop = false,
                            Frames = GameObjectAnimation.CreateFramesFromSheet(
                                "images/ui04-00.png", // 施錠扉
                                64, 64, 1, 1.0f,
                                offsetX: 64*7, offsetY: 64*0,
                                scaleX: tileSize / 64f,
                                scaleY: tileSize / 64f
                            )
                        }}
                    },
                    CurrentAnimationName = "idle",
                    OnClick = () => {
                        TryEnterGoal(scene, setup, gx);
                    },
                };
                scene.AddUI(goalTile);
            }

            var btnModeToggle = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "ModeToggle";
                ui.CenterX = true;
                ui.PosY = Common.CanvasHeight - 50f;
                ui.Text = "　探 索　"; // 初期モード
                ui.FontSize = 24;
                ui.ZIndex = 10000;
                ui.TextColor = "#DDDDDD"; // デフォルト白文字
                ui.OnClick = () =>
                {
                    if (_currentMode == StealthMode.Explore)
                    {
                        _currentMode = StealthMode.Focus; // ★ 集中モード
                        ui.Text = "　集 中　";
                        ui.TextColor = "#FFFF00";
                        if (_stealthMask != null) _stealthMask.Opacity = 0.5f;
                    }
                    else
                    {
                        _currentMode = StealthMode.Explore;
                        ui.Text = "　探 索　";
                        ui.TextColor = "#DDDDDD";
                        if (_stealthMask != null) _stealthMask.Opacity = 0f;
                    }
                    ui.MarkDirty();
                };
            });
            scene.AddUI(btnModeToggle);

            HighlightAvailableTiles(scene);

#if DEBUG
            // === テスト用：強制探索成功ボタン ===
            var btnTestBattle = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "TestBattleButton";
                ui.PosX = Common.CanvasWidth - 70f;   // 右下
                ui.PosY = Common.CanvasHeight - 40f;
                ui.Text = "TEST";
                ui.FontSize = 14;
                ui.Opacity = 0.2f;
                ui.ZIndex = 20000;
                ui.TextColor = "#FFFFFF";

                ui.OnClick = async () =>
                {
                    Console.WriteLine("▶ テストボタン押下");

                    // 戦闘開始前に会話 → 会話終了後にフェーズ進行
                    await new ConversationWindow().ShowAsync(
                        scene,
                        new[]
                        {
                            "……ここが敵の根城か。",
                            "（辺りを見渡す）"
                        },
                        speaker: setup.Allies[0],
                        mirrorRight: false
                    );
                    await new ConversationWindow().ShowAsync(
                        scene,
                        new[]
                        {
                            "気を引き締めて。\n油断は禁物よ。"
                        },
                        speaker: setup.Allies[1],
                        mirrorRight: true
                    );

                    TryEnterGoal(scene, setup, 0, true);
                };
            });
            scene.AddUI(btnTestBattle);
#endif
        }

        /// <summary>
        /// 入力ブロッカー
        /// </summary>
        private UIObject AddInputBlocker(Scene scene)
        {
            var blocker = new UIObject
            {
                Name = $"InputBlocker_{Guid.NewGuid()}",
                PosX = 0,
                PosY = 0,
                ZIndex = 50000, // 最前面
                Opacity = 0.0f, // 完全透明
                Enabled = true, // ★入力を受けるので下のUIをブロックできる
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui06-01.png",
                                    new System.Drawing.Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                                {
                                    TintColor = "#000000",
                                },
                                Duration = 1.0f
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(blocker);
            return blocker;
        }

        /// <summary>
        /// 隠蔽モード中のマスク画像生成
        /// </summary>
        private void CreateStealthMask(Scene scene)
        {
            _stealthMask = new UIObject
            {
                Name = "StealthMask",
                PosX = 0,
                PosY = 0,
                ZIndex = 100, // 背景より上、ボタンより下
                Opacity = 0f,
                Enabled = false, // ★入力無効化
                TintColor = "#9933CC", // 紫色
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui06-01.png",
                                    new System.Drawing.Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight)),
                                Duration = 1.0f
                            }
                        }
                    }}
                }
            };
            scene.AddUI(_stealthMask);
        }

        /// <summary>
        /// HPバー生成
        /// </summary>
        private void CreateStealthHpUI(Scene scene)
        {
            float posX = 10f, posY = 20f;
            float barWidth = 80f;

            // === 背景バー（黒） ===
            var bgBar = new UIObject
            {
                Name = "StealthHpBackground",
                PosX = posX,
                PosY = posY + 5f,
                ZIndex = 9999,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui02-00.png", new System.Drawing.Rectangle(0,0,1,1)) {
                                    ScaleX = barWidth,
                                    ScaleY = 6f
                                }
                            }
                        }
                    }}
                },
                TintColor = "#333333"
            };
            scene.AddUI(bgBar);

            // === キャラ名 ===
            var nameText = new UIObject
            {
                Name = "StealthHpName",
                PosX = posX,
                PosY = posY - 10f,
                ZIndex = 9500,
                FontSize = 12,
                TextColor = "#FFFFFF",
                Text = _player.Name
            };
            scene.AddUI(nameText);

            // === 緑バー ===
            _hpBarResidual = new UIObject
            {
                Name = "StealthHpResidual",
                PosX = posX,
                PosY = posY + 5f,
                ZIndex = 10000,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui02-00.png", new System.Drawing.Rectangle(0,0,1,1)) {
                                    ScaleX = 80f, ScaleY = 6f
                                }
                            }
                        }
                    }}
                },
                TintColor = "#009900"
            };

            // === 赤バー ===
            _hpBarFatal = new UIObject
            {
                Name = "StealthHpFatal",
                PosX = posX,
                PosY = posY + 5f,
                ZIndex = 10000,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui02-00.png", new System.Drawing.Rectangle(0,0,1,1)) {
                                    ScaleX = 80f, ScaleY = 6f
                                }
                            }
                        }
                    }}
                },
                TintColor = "#CC0000"
            };

            // === 数値 ===
            _hpText = new UIObject
            {
                Name = "StealthHpText",
                PosX = posX + 80,
                PosY = posY + 0f,
                ZIndex = 10000,
                FontSize = 12,
                TextColor = "#FFFFFF",
                TextAlign = "right"
            };

            // 追加順に注意（背景 → 名称 → バー → テキスト）
            scene.AddUI(_hpBarResidual);
            scene.AddUI(_hpBarFatal);
            scene.AddUI(_hpText);

            // === 警戒度バー ===
            var alertLabel = new UIObject
            {
                Name = "AlertLabel",
                PosX = posX + 120,
                PosY = posY - 10f,
                ZIndex = 9500,
                FontSize = 12,
                TextColor = "#FFFFFF",
                Text = "警戒度"
            };
            scene.AddUI(alertLabel);

            // === 警戒度バー背景（黒） ===
            var alertBarBackground = new UIObject
            {
                Name = "AlertBarBackground",
                PosX = posX + 120,
                PosY = posY + 5f,
                ZIndex = 9999,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui02-00.png",
                                    new System.Drawing.Rectangle(0,0,1,1)) {
                                    ScaleX = barWidth,
                                    ScaleY = 12f
                                }
                            }
                        }
                    }}
                },
                TintColor = "#000000"
            };
            scene.AddUI(alertBarBackground);

            // === 警戒度バー（赤） ===
            _alertBar = new UIObject
            {
                Name = "AlertBar",
                PosX = alertBarBackground.PosX,
                PosY = alertBarBackground.PosY,
                ZIndex = 10000,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui02-00.png",
                                    new System.Drawing.Rectangle(0,0,1,1)) {
                                    ScaleX = 0f,   // 初期ゼロ
                                    ScaleY = 12f
                                }
                            }
                        }
                    }}
                },
                TintColor = "#FF0000"
            };
            scene.AddUI(_alertBar);

            UpdateHpUI();
            UpdateAlertBar();
        }

        /// <summary>
        /// HPバー更新
        /// </summary>
        private void UpdateHpUI()
        {
            float totalWidth = 80f;
            float greenWidth = totalWidth * _player.CurrentStats.ResidualHP / _player.BaseStats.MaxHP;
            float redWidth = totalWidth * _player.CurrentStats.FatalHP / _player.BaseStats.MaxHP;

            var gFrame = _hpBarResidual.GetCurrentAnimationFrame();
            var rFrame = _hpBarFatal.GetCurrentAnimationFrame();

            if (gFrame?.Sprite != null) gFrame.Sprite.ScaleX = MathF.Max(1f, greenWidth);
            if (rFrame?.Sprite != null)
            {
                rFrame.Sprite.ScaleX = MathF.Max(1f, redWidth);
                _hpBarFatal.PosX = _hpBarResidual.PosX + greenWidth;
            }

            _hpText.Text = $"{_player.CurrentStats.ResidualHP + _player.CurrentStats.FatalHP}/{_player.BaseStats.MaxHP}";

            _hpBarResidual.MarkDirty();
            _hpBarFatal.MarkDirty();
            _hpText.MarkDirty();
        }

        /// <summary>
        /// 警戒度バーの更新
        /// </summary>
        private void UpdateAlertBar()
        {
            float totalWidth = 80f;
            float alert = CalculateAlertLevel(); // 0.0〜1.0
            float redWidth = totalWidth * alert;

            var frame = _alertBar.GetCurrentAnimationFrame();
            if (frame?.Sprite != null)
                frame.Sprite.ScaleX = MathF.Max(0f, redWidth);

            _alertBar.MarkDirty();
        }

        /// <summary>罠をランダムに配置</summary>
        private void PlaceTrapsRandomly(StealthBoardSetup setup)
        {
            int cols = _cells.GetLength(0);
            int rows = _cells.GetLength(1);

            var all = Enumerable.Range(0, cols * rows)
                .Select(i => (x: i % cols, y: i / cols))
                 .Where(pos => pos.y < rows - 1) // ★ 最下行は除外
                .ToList();

            foreach (var kv in setup.Traps)
            {
                for (int i = 0; i < kv.Value && all.Count > 0; i++)
                {
                    int idx = _rand.Next(all.Count);
                    var pos = all[idx];
                    all.RemoveAt(idx);

                    _cells[pos.x, pos.y].Trap = kv.Key;
                }
            }
        }

        /// <summary>潜伏敵をランダムに配置</summary>
        private void PlaceStealthEnemyRandomly(StealthBoardSetup setup)
        {
            if (setup.StealthEnemies == null || setup.StealthEnemies.Count == 0) return;

            var cols = _cells.GetLength(0);
            var rows = _cells.GetLength(1);

            var all = Enumerable.Range(0, cols * rows)
                .Select(i => (x: i % cols, y: i / cols))
                .Where(pos => _cells[pos.x, pos.y].Trap == null)
                .Where(pos => pos.y < rows - 1)
                .ToList();

            foreach (var enemy in setup.StealthEnemies)
            {
                if (all.Count == 0) break;
                var pos = all[_rand.Next(all.Count)];
                all.Remove(pos);

                _cells[pos.x, pos.y].EnemyInstance = enemy; // ★直接キャラを置く
            }
        }

        /// <summary>
        /// 安全マスに鍵を配置
        /// </summary>
        private void PlaceKeyRandomly()
        {
            int cols = _cells.GetLength(0);
            int rows = _cells.GetLength(1);

            var candidates = Enumerable.Range(0, cols * rows)
                .Select(i => (x: i % cols, y: i / cols))
                .Where(pos => _cells[pos.x, pos.y].Trap == null && _cells[pos.x, pos.y].EnemyInstance == null)
                .Where(pos => pos.y < rows - 1)
                .ToList();

            if (candidates.Count > 0)
            {
                _keyPos = candidates[_rand.Next(candidates.Count)];
            }
        }

        /// <summary>マスを開く処理</summary>
        private void RevealTile(Scene scene, StealthBoardSetup setup, int x, int y)
        {
            if (!CanReveal(x, y)) return;

            var cell = _cells[x, y];
            if (cell.IsRevealed) return;
            cell.IsRevealed = true;
            cell.RevealedStep = ++_revealStepCounter;

            Console.WriteLine($"▶ Tile {x},{y} を開いた");

            if (_currentMode == StealthMode.Explore)
            {
                if (cell.EnemyInstance != null)
                {
                    var hazard = PlaceHazardIcon(scene, x, y, danger: true);
                    // 経験値減少
                    GameMain.Instance.SubtractExp(setup.BaseExpPerDig * 1);
                    PlayHazardShockEffect(scene, hazard);
                    _ = TriggerMissionFail(scene, "敵に発見された！");
                    return;
                }
                else if (cell.Trap.HasValue)
                {
                    // 罠は通常通り演出 → 失敗ではない
                    var hazard = PlaceHazardIcon(scene, x, y, danger: true);
                    // 経験値減少
                    GameMain.Instance.SubtractExp(setup.BaseExpPerDig * 1);

                    PlayHazardShockEffect(scene, hazard);

                    ApplyTrapDamage(cell.Trap.Value);

                    cell.TrapTriggered = true; // ★罠を踏んだマスは記録
                }
                else
                {
                    PlaceFootstep(scene, x, y);

                    // 経験値加算
                    GameMain.Instance.AddExp(setup.BaseExpPerDig * 1);

                    // 報酬がある場合
                    if (cell.IsReward || cell.IsItemReward)
                    {
                        ShowRewardPopup(scene, cell);
                    }

                    if (cell.IsItemReward)
                    {
                        _itemPickupCount++;
                        _itemCountText.Text = $"×{_itemPickupCount}";
                        _itemCountText.MarkDirty();

                        FlashItemCountText(_itemCountText, "#66CCFF");
                    }
                    else if (cell.IsReward)
                    {
                        _moneyPickupCount++;
                        _moneyCountText.Text = $"×{_moneyPickupCount}";
                        _moneyCountText.MarkDirty();

                        FlashMoneyCountText(_moneyCountText, "#66CCFF");
                    }
                }
            }
            else if (_currentMode == StealthMode.Focus)
            {
                if (cell.Trap.HasValue || cell.EnemyInstance != null)
                {
                    PlaceHazardIcon(scene, x, y, danger: false);

                    // 経験値加算
                    GameMain.Instance.AddExp(setup.BaseExpPerDig * 2);

                    RemoveOldSafeFootsteps(scene, 4);
                    cell.Trap = null;
                    cell.EnemyInstance = null;
                }
                else
                {
                    // ★ 安全マスなのに集中モード → 赤足跡を残す
                    PlaceDangerFootstep(scene, x, y);
                    // 削除対象に入らない（警戒度下げられない）

                    // 経験値減少
                    GameMain.Instance.SubtractExp(setup.BaseExpPerDig * 1);
                }

                //  集中操作終了後は探索モードに戻る
                _currentMode = StealthMode.Explore;
                if (scene.TryGetUI("ModeToggle", out var toggleBtn))
                {
                    toggleBtn.Text = "　探 索　";
                    toggleBtn.TextColor = "#DDDDDD"; // デフォルト白文字
                    if (_stealthMask != null) _stealthMask.Opacity = 0f; // ★マスクを消す
                    toggleBtn.MarkDirty();
                }
            }

            if (setup.Weather == WeatherType.Stormy)
            {
                CloseOldReveals(scene, setup.WeatherRevealThreshold);
            }

            // ★ 安全化
            cell.Trap = null;
            cell.EnemyInstance = null;

            UpdateDangerTint(scene, x, y);
            UpdateNeighborsTint(scene, x, y);
            _ = UpdateFootstepColors(scene);

            HighlightAvailableTiles(scene);

            if (_keyPos.HasValue && _keyPos.Value.x == x && _keyPos.Value.y == y)
            {
                ShowKeyAndOpenDoor(scene, x, y);
                _keyPos = null; // 一度だけ
            }
        }

        /// <summary>
        /// ダメージ反映処理
        /// </summary>
        private void ApplyTrapDamage(TrapType trap)
        {
            int maxHp = _player.BaseStats.MaxHP;

            switch (trap)
            {
                case TrapType.PoisonDart:
                    _player.CurrentStats.ApplyResidualDamage((int)(maxHp * 0.20f));
                    _extraAlertPoints++;
                    break;

                case TrapType.BearTrap:
                    _player.CurrentStats.ApplyResidualDamage((int)(maxHp * 0.15f));
                    _player.CurrentStats.ApplyFatalDamage((int)(maxHp * 0.10f));
                    _extraAlertPoints += 2; // トラバサミは動作音で警戒度2倍上昇とする
                    break;
            }

            UpdateHpUI();
            UpdateAlertBar(); // 即時更新

            // ★ HPチェック：死亡していたら即任務失敗
            if (_player.CurrentStats.IsDead)
            {
                _ = TriggerMissionFail(
                    GameMain.Instance.CurrentScene,
                    "致命傷を負った！"
                );
            }
        }

        /// <summary>
        /// 鍵演出処理
        /// </summary>
        private async void ShowKeyAndOpenDoor(Scene scene, int x, int y)
        {
            // 鍵アイコン生成
            var tile = scene.UIObjects.Values.First(u => u.Name == $"Tile_{x}_{y}");
            var keyIcon = new UIObject
            {
                Name = "KeyIcon",
                PosX = tile.PosX,
                PosY = tile.PosY,
                ZIndex = 20000,
                EnableLerp = true,
                MoveLerpSpeed = 4f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = true,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png", 64, 64, 1, 1.0f,
                            offsetX: 64*8, offsetY: 64*0, // 鍵の絵
                            scaleX: tile.Animations["idle"].Frames[0].Sprite.ScaleX,
                            scaleY: tile.Animations["idle"].Frames[0].Sprite.ScaleY
                        )
                    }}
                },
                CurrentAnimationName = "idle"
            };

            // 扉位置へターゲット設定
            if (scene.TryGetUI($"GoalDoor_{_correctDoorIndex}", out var door))
            {
                keyIcon.TargetPosX = door.PosX;
                keyIcon.TargetPosY = door.PosY;
            }
            scene.AddUI(keyIcon);

            // 少し待ってから処理
            await Task.Delay(1500);

            // 扉を開錠済みに差し替え
            if (scene.TryGetUI($"GoalDoor_{_correctDoorIndex}", out var correctDoor))
            {
                float scaleX = correctDoor.Animations["idle"].Frames[0].Sprite.ScaleX;
                float scaleY = correctDoor.Animations["idle"].Frames[0].Sprite.ScaleY;

                correctDoor.Animations["idle"] = new GameObjectAnimation
                {
                    Loop = false,
                    Frames = GameObjectAnimation.CreateFramesFromSheet(
                        "images/ui04-00.png", 64, 64, 1, 1.0f,
                        offsetX: 64 * 7, offsetY: 64 * 1, // 開いた扉
                        scaleX: scaleX, scaleY: scaleY
                    )
                };
                correctDoor.MarkDirty();
            }

            // 外れ扉は煙エフェクトを出して消す
            for (int i = 0; i < _goalCols; i++)
            {
                if (i == _correctDoorIndex) continue;
                if (scene.TryGetUI($"GoalDoor_{i}", out var doorb))
                {
                    ShowSmokeEffect(scene, doorb.PosX, doorb.PosY);
                    scene.RemoveUI(doorb);
                }
            }

            // 鍵アイコンを消す
            scene.RemoveUI(keyIcon);
        }

        /// <summary>
        /// 煙演出例：
        /// </summary>
        private void ShowSmokeEffect(Scene scene, float x, float y)
        {
            // 扉と同じスケールを取得
            float scaleX = 1f, scaleY = 1f;
            if (scene.UIObjects.Values.FirstOrDefault(u => u.PosX == x && u.PosY == y) is UIObject door)
            {
                var frame = door.Animations["idle"].Frames[0];
                scaleX = frame.Sprite.ScaleX;
                scaleY = frame.Sprite.ScaleY;
            }

            var smoke = new UIObject
            {
                Name = $"Smoke_{Guid.NewGuid()}",
                PosX = x,
                PosY = y,
                ZIndex = 25000,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "play", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png", 64, 64, 2, 0.3f,
                            offsetX: 64*7, offsetY: 64*2, // 煙アニメ
                            scaleX: scaleX, scaleY: scaleY
                        )
                    }}
                },
                CurrentAnimationName = "play"
            };
            smoke.OnAnimationCompleted += (obj, anim) => scene.RemoveUI(smoke);
            scene.AddUI(smoke);
        }

        /// <summary>
        /// 足跡生成
        /// </summary>
        private void PlaceFootstep(Scene scene, int x, int y)
        {
            var tile = scene.UIObjects.Values.First(u => u.Name == $"Tile_{x}_{y}");
            var frame = tile.Animations["idle"].Frames[0];
            float baseW = frame.Sprite.SourceRect.Width;
            float scaleX = frame.Sprite.ScaleX;
            float tileSize = baseW * scaleX;
            float scale = tileSize / 64f;

            var stepIcon = new UIObject
            {
                Name = $"Icon_{x}_{y}_Step",
                PosX = tile.PosX,
                PosY = tile.PosY,
                ZIndex = 10000,
                Opacity = 0.7f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png",
                            64, 64, 1, 1.0f,
                            offsetX: 64*6, offsetY: 64*3,
                            scaleX: scale, scaleY: scale
                        )
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(stepIcon);
            _footsteps.Add(stepIcon);
        }

        /// <summary>
        /// 危険足跡（集中モードで安全マスを叩いたとき）
        /// </summary>
        private void PlaceDangerFootstep(Scene scene, int x, int y)
        {
            var tile = scene.UIObjects.Values.First(u => u.Name == $"Tile_{x}_{y}");
            var frame = tile.Animations["idle"].Frames[0];
            float baseW = frame.Sprite.SourceRect.Width;
            float scaleX = frame.Sprite.ScaleX;
            float tileSize = baseW * scaleX;
            float scale = tileSize / 64f;

            var stepIcon = new UIObject
            {
                Name = $"Icon_{x}_{y}_DangerStep", // ★ 通常足跡と区別
                PosX = tile.PosX,
                PosY = tile.PosY,
                ZIndex = 10000,
                Opacity = 0.9f,
                TintColor = "#FF0000", // 赤固定
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png",
                            64, 64, 1, 1.0f,
                            offsetX: 64*6, offsetY: 64*3,
                            scaleX: scale, scaleY: scale
                        )
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(stepIcon);
            _footsteps.Add(stepIcon);
        }

        /// <summary>
        /// 罠/敵アイコン
        /// </summary>
        private UIObject PlaceHazardIcon(Scene scene, int x, int y, bool danger)
        {
            var cell = _cells[x, y];
            int offsetX = 0, offsetY = 0;

            if (cell.Trap.HasValue)
            {
                switch (cell.Trap.Value)
                {
                    case TrapType.PoisonDart: offsetX = 64 * 4; offsetY = 64 * 2; break;
                    case TrapType.BearTrap: offsetX = 64 * 5; offsetY = 64 * 3; break;
                }
            }
            else if (cell.EnemyInstance != null)
            {
                offsetX = 64 * 4; offsetY = 64 * 3;
            }

            var tile = scene.UIObjects.Values.First(u => u.Name == $"Tile_{x}_{y}");
            var frame = tile.Animations["idle"].Frames[0];
            float baseW = frame.Sprite.SourceRect.Width;
            float scaleX = frame.Sprite.ScaleX;
            float tileSize = baseW * scaleX;
            float scale = tileSize / 64f;

            var icon = new UIObject
            {
                Name = $"Icon_{x}_{y}_Hazard",
                PosX = tile.PosX,
                PosY = tile.PosY,
                ZIndex = 10001,
                Opacity = 0.9f,
                TintColor = danger ? "#FF0000" : "#000000",
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png",
                            64, 64, 1, 1.0f,
                            offsetX: offsetX, offsetY: offsetY,
                            scaleX: scale, scaleY: scale
                        )
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(icon);

            return icon; // ★返す
        }

        /// <summary>
        /// 拡大演出処理
        /// </summary>
        private async void PlayHazardShockEffect(Scene scene, UIObject hazard)
        {
            if (hazard == null) return;

            var blocker = AddInputBlocker(scene);

            // === 中央に大きな罠アイコンを生成 ===
            var frame = hazard.Animations["idle"].Frames[0];
            var bigIcon = new UIObject
            {
                Name = $"ShockIcon_{Guid.NewGuid()}",
                CenterX = true,
                PosY = 200,
                ZIndex = 30000,
                Opacity = 1.0f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(frame.Sprite.ImagePath, frame.Sprite.SourceRect)
                                {
                                    ScaleX = frame.Sprite.ScaleX * 3.5f,
                                    ScaleY = frame.Sprite.ScaleY * 3.5f
                                },
                                Duration = 1.0f
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(bigIcon);

            // === 画面全体を赤くフラッシュ ===
            var flash = new UIObject
            {
                Name = $"RedFlash_{Guid.NewGuid()}",
                PosX = 0,
                PosY = 0,
                ZIndex = 29000,
                Opacity = 0.0f,
                TintColor = "#FF0000",
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui06-02.png",
                                    new System.Drawing.Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight)),
                                Duration = 1.0f
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(flash);

            // フラッシュのアニメーション（フェードイン→フェードアウト）
            for (int i = 0; i < 5; i++)
            {
                flash.Opacity = i / 5f * 0.6f;
                flash.MarkDirty();
                await Task.Delay(30);
            }
            for (int i = 0; i < 5; i++)
            {
                flash.Opacity = 0.6f - i / 5f * 0.6f;
                flash.MarkDirty();
                await Task.Delay(30);
            }
            scene.RemoveUI(flash);

            // アイコンを少し待ってから消す
            await Task.Delay(500);

            // 後始末
            scene.RemoveUI(blocker);
            scene.RemoveUI(bigIcon);
        }

        /// <summary>
        /// タイルの ZIndex 制御
        /// </summary>
        private void HighlightAvailableTiles(Scene scene)
        {
            int cols = _cells.GetLength(0);
            int rows = _cells.GetLength(1);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (scene.TryGetUI($"Tile_{x}_{y}", out var tile))
                    {
                        if (!_cells[x, y].IsRevealed && CanReveal(x, y))
                        {
                            tile.ZIndex = 6000; // 暗幕より上
                            tile.Opacity = 1.0f;
                        }
                        else
                        {
                            tile.ZIndex = 4000; // 暗幕より下
                            tile.Opacity = 0.5f;
                        }
                        tile.MarkDirty();
                    }
                }
            }
        }

        /// <summary>
        /// 古い足跡を削除
        /// </summary>
        private void RemoveOldSafeFootsteps(Scene scene, int count)
        {
            // Danger マークがついていない足跡だけを候補にする
            var candidates = _footsteps
                .Where(s =>
                {
                    if (string.IsNullOrEmpty(s.Name)) return false;
                    if (s.Name.EndsWith("_DangerStep")) return false;
                    if (!s.Name.EndsWith("_Step")) return false;

                    // Danger名を生成
                    var dangerName = s.Name.Substring(0, s.Name.Length - "_Step".Length) + "_Danger";

                    // scene.UIObjects に Danger が存在しなければ「安全足跡」
                    bool hasDanger = scene.UIObjects.Values.Any(u => u.Name == dangerName);
                    return !hasDanger;
                })
                .ToList();

            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                var oldest = candidates[0];         // 一番古い候補
                candidates.RemoveAt(0);             // 候補リストから削除
                _footsteps.Remove(oldest);          // 足跡リストから削除
                scene.RemoveUI(oldest);             // 画面から消す
            }
        }

        /// <summary>
        /// 古いマスを閉じる処理(荒天時)
        /// </summary>
        private void CloseOldReveals(Scene scene, int maxThreshold)
        {
            int threshold = _revealStepCounter - maxThreshold;
            if (threshold <= 0) return;

            foreach (var cell in _cells)
            {
                if (cell.IsRevealed
                    && cell.RevealedStep > 0
                    && cell.RevealedStep <= threshold
                    && !cell.TrapTriggered) // ★罠を踏んだマスは戻さない
                {
                    // 閉じ直し
                    cell.IsRevealed = false;
                    cell.RevealedStep = -1;

                    if (scene.TryGetUI($"Tile_{cell.X}_{cell.Y}", out var tile))
                    {
                        tile.TintColor = "#FFFFFF"; // 未開封色
                        tile.MarkDirty();
                    }

                    // 足跡や危険アイコンを消す
                    if (scene.TryGetUI($"Icon_{cell.X}_{cell.Y}_Step", out var step))
                        scene.RemoveUI(step);
                    if (scene.TryGetUI($"Icon_{cell.X}_{cell.Y}_DangerStep", out var dstep))
                        scene.RemoveUI(dstep);
                    if (scene.TryGetUI($"Icon_{cell.X}_{cell.Y}_Hazard", out var hazard))
                        scene.RemoveUI(hazard);
                }
            }
        }

        /// <summary>
        /// 警戒度半減
        /// </summary>
        private void HalveAlertLevel(Scene scene)
        {
            int removeCount = _footsteps.Count / 2;
            for (int i = 0; i < removeCount; i++)
            {
                var oldest = _footsteps[0];
                _footsteps.RemoveAt(0);
                scene.RemoveUI(oldest);
            }

            // 色を再計算
            _ = UpdateFootstepColors(scene);
        }

        /// <summary>
        /// ひらけるマスのチェック
        /// </summary>
        private bool CanReveal(int x, int y)
        {
            if (_cells[x, y].IsRevealed) return false;

            int cols = _cells.GetLength(0);
            int rows = _cells.GetLength(1);

            // ★ 最下行は常にスタート可能
            if (y == rows - 1) return true;

            // ★ 8方向チェック（上下左右＋斜め）
            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int i = 0; i < dx.Length; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (nx >= 0 && nx < cols && ny >= 0 && ny < rows)
                {
                    if (_cells[nx, ny].IsRevealed)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 危険数カウント処理
        /// </summary>
        private int CountAdjacentDanger(int x, int y)
        {
            int cols = _cells.GetLength(0);
            int rows = _cells.GetLength(1);
            int count = 0;

            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int i = 0; i < dx.Length; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (nx >= 0 && nx < cols && ny >= 0 && ny < rows)
                {
                    var cell = _cells[nx, ny];
                    if (cell.Trap.HasValue) count++;
                    if (cell.EnemyInstance != null) count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 危険度色更新処理
        /// </summary>
        private void UpdateDangerTint(Scene scene, int x, int y)
        {
            int dangerCount = CountAdjacentDanger(x, y);

            string color = dangerCount switch
            {
                0 => "#000000",   // 黒
                1 => "#66CCFF",   // 水色
                2 => "#FFFF66",   // 黄色
                3 => "#FF9933",   // オレンジ
                4 => "#FF3333",   // 赤
                _ => "#CC33CC"    // 紫
            };

            if (scene.TryGetUI($"Tile_{x}_{y}", out var tile))
            {
                tile.TintColor = color;
                tile.MarkDirty();
            }
        }

        /// <summary>
        /// 周囲8マスをまとめて更新
        /// </summary>
        private void UpdateNeighborsTint(Scene scene, int x, int y)
        {
            int cols = _cells.GetLength(0);
            int rows = _cells.GetLength(1);

            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int i = 0; i < dx.Length; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (nx >= 0 && nx < cols && ny >= 0 && ny < rows)
                {
                    if (_cells[nx, ny].IsRevealed)
                        UpdateDangerTint(scene, nx, ny);
                }
            }
        }

        /// <summary>
        /// 警戒度の計算
        /// </summary>
        private float CalculateAlertLevel()
        {
            int baseCount = _footsteps.Count;
            return Math.Min(1.0f, (baseCount + _extraAlertPoints) * 0.10f);
        }

        /// <summary>
        /// 警戒度をもとに足跡色を更新
        /// </summary>
        private async Task UpdateFootstepColors(Scene scene)
        {
            float alert = CalculateAlertLevel();

            foreach (var step in _footsteps.ToList())
            {
                string dangerName = step.Name.Replace("_Step", "_Danger");
                bool isDanger = scene.UIObjects.Values.Any(u => u.Name == dangerName);

                if (isDanger)
                {
                    step.TintColor = "#FF0000"; // 危険足跡は常に赤
                }
                else
                {
                    int r = 255;
                    int g = (int)(255 * (1 - alert));
                    int b = (int)(255 * (1 - alert));
                    step.TintColor = $"#{r:X2}{g:X2}{b:X2}";
                }
                step.MarkDirty();
            }

            // ★ 警戒度バー更新
            UpdateAlertBar();

            // ★ 警戒度チェック
            await CheckAlertLevelAsync(scene);
        }

        /// <summary>
        /// 警戒度チェック
        /// </summary>
        private async Task CheckAlertLevelAsync(Scene scene)
        {
            float alert = CalculateAlertLevel();

            if (alert >= 1.0f) // 100% 到達
            {
                _ = TriggerMissionFail(scene, "警戒が限界に達した！");
            }
        }

        /// <summary>
        /// 共通の失敗演出
        /// </summary>
        private async Task TriggerMissionFail(Scene scene, string reason)
        {
            if (_missionFailed) return; // ★既に失敗処理中なら無視
            _missionFailed = true;

            var blocker = AddInputBlocker(scene);

            // 一定時間待機
            await Task.Delay(2000);

            _ = scene.ShowTelopAsync("任務失敗", holdSeconds: 3.0f, maskOpacity: 0.7f);

            // === 下段：理由 ===
            var telopReason = new UIObject
            {
                Name = $"TelopFailReason_{Guid.NewGuid()}",
                CenterX = true,
                PosY = Common.CanvasHeight / 2 - 120,
                Text = reason,
                FontSize = 16, // 小さめ
                FontFamily = "\"Yu Mincho, serif\"",
                TextColor = "#FFAAAA",
                TextAlign = "center",
                Opacity = 0f,
                ZIndex = 390001,
                StretchToText = true,
                TextOffsetX = 10f,
                TextOffsetY = 5f,
            };
            scene.AddUI(telopReason);

            // === 補足説明 ===
            var telopSub = new UIObject
            {
                Name = $"TelopFailSub_{Guid.NewGuid()}",
                CenterX = true,
                PosY = Common.CanvasHeight / 2 - 90,
                Text = "入手物を失った...",
                FontSize = 16, // 小さめ
                FontFamily = "\"Yu Mincho, serif\"",
                TextColor = "#FFAAAA",
                TextAlign = "center",
                Opacity = 0f,
                ZIndex = 390002,
                StretchToText = true,
                TextOffsetX = 10f,
                TextOffsetY = 5f,
            };
            scene.AddUI(telopSub);

            // フェードイン
            for (int i = 0; i <= 5; i++)
            {
                float alpha = i / 5f;
                telopReason.Opacity = alpha;
                telopSub.Opacity = alpha;
                telopReason.MarkDirty();
                telopSub.MarkDirty();
                await Task.Delay(160);
            }

            // 一定時間表示
            await Task.Delay(2000);

            // フェードアウト
            for (int i = 0; i <= 5; i++)
            {
                float alpha = 1f - (i / 5f);
                telopReason.Opacity = alpha;
                telopSub.Opacity = alpha;
                telopReason.MarkDirty();
                telopSub.MarkDirty();
                await Task.Delay(160);
            }

            scene.RemoveUI(telopReason);
            scene.RemoveUI(telopSub);

            // ホーム画面に戻る
            GameMain.Instance.StartFadeTransition(GameState.Home);

            // 一定時間表示
            await Task.Delay(2000);

            scene.RemoveUI(blocker);
        }

        /// <summary>
        /// 安全マスの30%にアイテム枠とお金枠を割り当てる（非表示）
        /// </summary>
        private void PlaceRewardsRandomly(StealthBoardSetup setup)
        {
            int cols = _cells.GetLength(0);
            int rows = _cells.GetLength(1);

            // 安全マス候補（罠なし、敵なし、最下行以外）
            var safeCells = Enumerable.Range(0, cols * rows)
                .Select(i => (x: i % cols, y: i / cols))
                .Where(pos => _cells[pos.x, pos.y].Trap == null && _cells[pos.x, pos.y].EnemyInstance == null)
                .Where(pos => pos.y < rows - 1) // 最下行は除外
                .Select(pos => _cells[pos.x, pos.y])
                .ToList();

            if (safeCells.Count == 0) return;

            // 配置スロット数 = 安全マスの30%
            int rewardSlots = (int)(safeCells.Count * 0.3f);
            if (rewardSlots <= 0) return;

            // シャッフル
            safeCells = safeCells.OrderBy(_ => _rand.Next()).ToList();

            // --- アイテム枠 ---
            int itemSlots = Math.Min(setup.MaxItemDrops, rewardSlots);
            for (int i = 0; i < itemSlots; i++)
            {
                var cell = safeCells[i];
                cell.IsReward = true;
                cell.IsItemReward = true;
            }

            // --- 残りをお金枠 ---
            for (int i = itemSlots; i < rewardSlots; i++)
            {
                var cell = safeCells[i];
                cell.IsReward = true;
                cell.IsItemReward = false;
            }
        }

        /// <summary>
        /// 報酬入手アニメーション
        /// </summary>
        private async void ShowRewardPopup(Scene scene, StealthBoardCell cell)
        {
            string iconPath = "images/ui04-00.png"; // 汎用アイコンシート
            int offsetX = 0, offsetY = 0;

            if (cell.IsItemReward) { offsetX = 64 * 8; offsetY = 64 * 3; }
            else if (cell.IsReward) { offsetX = 64 * 8; offsetY = 64 * 2; }

            var tile = scene.UIObjects.Values.First(u => u.Name == $"Tile_{cell.X}_{cell.Y}");
            var frame = tile.Animations["idle"].Frames[0];

            var icon = new UIObject
            {
                Name = $"Reward_{Guid.NewGuid()}",
                PosX = tile.PosX,
                PosY = tile.PosY,
                ZIndex = 20000,
                Opacity = 1.0f,
                Enabled = false,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(iconPath, new System.Drawing.Rectangle(offsetX, offsetY, 64, 64))
                                {
                                    ScaleX = frame.Sprite.ScaleX,
                                    ScaleY = frame.Sprite.ScaleY
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(icon);

            // 上に移動しつつフェードアウト
            for (int i = 0; i < 20; i++)
            {
                icon.PosY -= 2f;
                icon.Opacity = 1.0f - i / 20f;
                icon.MarkDirty();
                await Task.Delay(30);
            }
            scene.RemoveUI(icon);
        }

        /// <summary>
        /// 報酬入手状況アイコン生成
        /// </summary>
        /// <param name="scene"></param>
        private void CreateRewardListUI(Scene scene)
        {
            float baseX = Common.CanvasWidth - 120;
            float baseY = 30f;

            // --- お金アイコン ---
            var moneyIcon = new UIObject
            {
                Name = "RewardIcon_Money",
                PosX = baseX,
                PosY = baseY,
                ZIndex = 9000,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui04-00.png",
                                    new System.Drawing.Rectangle(64*8, 64*2, 64, 64)) {
                                    ScaleX = 0.3f,
                                    ScaleY = 0.3f
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(moneyIcon);

            _moneyCountText = new UIObject
            {
                Name = "RewardText_Money",
                PosX = baseX + 25,
                PosY = baseY + 5,
                ZIndex = 9000,
                FontSize = 14,
                TextColor = "#FFFFFF",
                Text = "×0"
            };
            scene.AddUI(_moneyCountText);

            // --- アイテムアイコン ---
            var itemIcon = new UIObject
            {
                Name = "RewardIcon_Item",
                PosX = baseX + 60,
                PosY = baseY,
                ZIndex = 9000,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui04-00.png",
                                    new System.Drawing.Rectangle(64*8, 64*3, 64, 64)) {
                                    ScaleX = 0.3f,
                                    ScaleY = 0.3f
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(itemIcon);

            _itemCountText = new UIObject
            {
                Name = "RewardText_Item",
                PosX = baseX + 60 + 25,
                PosY = baseY + 5,
                ZIndex = 9000,
                FontSize = 14,
                TextColor = "#FFFFFF",
                Text = "×0"
            };
            scene.AddUI(_itemCountText);
        }

        /// <summary>
        /// 一時的にアイテム入手数の色を変える
        /// </summary>
        private async void FlashItemCountText(UIObject textUi, string colorHex)
        {
            _flashCtsItem?.Cancel();
            _flashCtsItem = new CancellationTokenSource();
            var token = _flashCtsItem.Token;

            textUi.TextColor = colorHex;
            textUi.MarkDirty();

            try
            {
                await Task.Delay(500, token);
                textUi.TextColor = "#FFFFFF";
                textUi.MarkDirty();
            }
            catch (TaskCanceledException) { }
        }

        /// <summary>
        /// 一時的にお金入手数の色を変える
        /// </summary>
        private async void FlashMoneyCountText(UIObject textUi, string colorHex)
        {
            _flashCtsMoney?.Cancel();
            _flashCtsMoney = new CancellationTokenSource();
            var token = _flashCtsMoney.Token;

            textUi.TextColor = colorHex;
            textUi.MarkDirty();

            try
            {
                await Task.Delay(500, token);
                textUi.TextColor = "#FFFFFF";
                textUi.MarkDirty();
            }
            catch (TaskCanceledException) { }
        }

        /// <summary>
        /// ゴール処理
        /// </summary>
        private async void TryEnterGoal(Scene scene, StealthBoardSetup setup, int doorIndex, bool isTest=false)
        {
            if (!isTest)
            {
                // 正しい扉でなければ無視
                if (doorIndex != _correctDoorIndex) return;

                // まだ開いていなければ無視
                if (!scene.TryGetUI($"GoalDoor_{doorIndex}", out var door)) return;

                int cols = _cells.GetLength(0);
                int rows = _cells.GetLength(1);

                // 扉の真下は盤面の最上段 (y=0)
                int belowY = 0;
                bool adjacent = false;

                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = doorIndex + dx;
                    if (nx >= 0 && nx < cols)
                    {
                        if (_cells[nx, belowY].IsRevealed)
                        {
                            adjacent = true;
                            break;
                        }
                    }
                }

                if (!adjacent) return;
            }

            var blocker = AddInputBlocker(scene);

            var isPerfect = (CalculateAlertLevel() <= 0f);
            var isStealthKillPerfect = CheckStealthPerfect();

            // リザルト表示 (OK後に戦闘へ)
            await ShowStealthResultAsync(
                scene,
                success: true,
                itemCount: _itemPickupCount,
                moneyCount: _moneyPickupCount,
                bonusZeroAlert: isPerfect,
                bonusStealthPerfect: isStealthKillPerfect,
                onOk: async () => 
                {
                    // 探索盤エピローグ再生            
                    await _eventScripts.PlayEventScriptAsync("Epilogue");

                    var enemies = new List<Character>();

                    if (setup.ForcedEnemies != null)
                        enemies.AddRange(setup.ForcedEnemies);

                    foreach (var enemy in setup.StealthEnemies)
                    {
                        if (_cells.Cast<StealthBoardCell>().Any(c => c.EnemyInstance == enemy))
                            enemies.Add(enemy);
                    }

                    // 報酬抽選の実行
                    var rewards = GenerateRewards(setup, isPerfect, isStealthKillPerfect);

                    if (enemies.Count > 0)
                    {
                        var setupBattle = new BattleSetup
                        {
                            Allies = setup.Allies,
                            Enemies = enemies,
                            Background = setup.BattleBackground,
                            Rewards = rewards,
                            StageName = setup.StageName,
                        };
                        setupBattle.Allies[0].BaseStats = _player.BaseStats.Clone();
                        setupBattle.Allies[0].CurrentStats = _player.CurrentStats.Clone();

                        GameMain.Instance.StartFadeTransition(GameState.Combat, setupBattle);
                    }
                    else
                    {
                        // 敵がいないので、最終リザルト表示して終了
                        await SceneResultHelper.ShowStageResultAsync(scene, rewards, true, async () =>
                        {
                            GameMain.Instance.StartFadeTransition(GameState.Home);
                        });
                    }
                }
            );

            scene.RemoveUI(blocker);
        }

        /// <summary>
        /// 完全ステルス条件チェック
        /// </summary>
        private bool CheckStealthPerfect()
        {
            // 警戒度がゼロ
            bool zeroAlert = (CalculateAlertLevel() <= 0f);

            // 敵をすべて暗殺（= StealthEnemies が盤上に残っていない）
            bool allAssassinated = !_cells.Cast<StealthBoardCell>().Any(c => c.EnemyInstance != null);

            return zeroAlert && allAssassinated;
        }

        /// <summary>
        /// リザルト表示
        /// </summary>
        private async Task ShowStealthResultAsync(
            Scene scene,
            bool success,
            int itemCount,
            int moneyCount,
            bool bonusZeroAlert,
            bool bonusStealthPerfect,
            Func<Task>? onOk = null)
        {
            // === 暗転マスク ===
            var mask = new UIObject
            {
                Name = "ResultMask",
                PosX = 0,
                PosY = 0,
                ZIndex = 400000,
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
            scene.AddUI(mask);

            // フェードイン
            for (int i = 0; i <= 10; i++)
            {
                mask.Opacity = i / 10f * 0.7f;
                mask.MarkDirty();
                await Task.Delay(40);
            }

            float centerX = Common.CanvasWidth / 2;
            float posY = Common.CanvasHeight / 2 - 160;

            // === 1. 探索完了テキスト ===
            var completeText = new UIObject
            {
                Name = "ResultCompleteText",
                CenterX = true,
                PosY = posY,
                ZIndex = 410000,
                FontSize = 28,
                TextColor = "#FFFFFF",
                Text = success ? "探索完了！" : "探索失敗…",
                TextAlign = "center",
                StretchToText = true,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Loop = false,
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-01.png",
                                    new System.Drawing.Rectangle(0,0,360,180))
                                {
                                    ScaleX = 1.0f,
                                    ScaleY = 1.0f
                                }
                            }
                        }
                    }}
                }
            };
            scene.AddUI(completeText);

            completeText.StartFadeIn(0.5f);
            await Task.Delay(1500);

            if (success)
            {
                // === 2. お金 ===
                var moneyIcon = new UIObject
                {
                    Name = "ResultMoneyIcon",
                    PosX = centerX - 80,
                    PosY = posY + 60,
                    ZIndex = 420000,
                    Opacity = 0f,
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        { "idle", new GameObjectAnimation {
                            Frames = new List<GameObjectAnimationFrame> {
                                new GameObjectAnimationFrame {
                                    Sprite = new Sprite("images/ui04-00.png",
                                        new System.Drawing.Rectangle(64*8, 64*2, 64, 64)) {
                                        ScaleX = 0.6f,
                                        ScaleY = 0.6f
                                    }
                                }
                            }
                        }}
                    }
                };
                scene.AddUI(moneyIcon);

                var moneyText = new UIObject
                {
                    Name = "ResultMoneyText",
                    PosX = centerX,
                    PosY = posY + 70,
                    ZIndex = 420000,
                    FontSize = 18,
                    TextColor = "#FFFFFF",
                    Text = $"×{moneyCount}",
                    Opacity = 0f
                };
                scene.AddUI(moneyText);

                moneyIcon.StartFadeIn(0.5f);
                moneyText.StartFadeIn(0.5f);
                await Task.Delay(800);

                // === 3. アイテム ===
                var itemIcon = new UIObject
                {
                    Name = "ResultItemIcon",
                    PosX = centerX - 80,
                    PosY = posY + 110,
                    ZIndex = 420000,
                    Opacity = 0f,
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        { "idle", new GameObjectAnimation {
                            Frames = new List<GameObjectAnimationFrame> {
                                new GameObjectAnimationFrame {
                                    Sprite = new Sprite("images/ui04-00.png",
                                        new System.Drawing.Rectangle(64*8, 64*3, 64, 64)) {
                                        ScaleX = 0.6f,
                                        ScaleY = 0.6f
                                    }
                                }
                            }
                        }}
                    }
                };
                scene.AddUI(itemIcon);

                var itemText = new UIObject
                {
                    Name = "ResultItemText",
                    PosX = centerX,
                    PosY = posY + 120,
                    ZIndex = 420000,
                    FontSize = 18,
                    TextColor = "#FFFFFF",
                    Text = $"×{itemCount}",
                    Opacity = 0f
                };
                scene.AddUI(itemText);

                itemIcon.StartFadeIn(0.5f);
                itemText.StartFadeIn(0.5f);
                await Task.Delay(800);

                // === 4. パーフェクトボーナス ===
                if (bonusStealthPerfect)
                {
                    var bonusText = new UIObject
                    {
                        Name = "ResultBonusText",
                        CenterX = true,
                        PosY = posY + 170,
                        ZIndex = 420000,
                        FontSize = 18,
                        TextColor = "#FFD700", // 金色で特別感
                        Text = "【痕跡も敵影も、この地に残さず】",
                        TextAlign = "center",
                        Opacity = 0f
                    };
                    scene.AddUI(bonusText);
                    bonusText.StartFadeIn(0.8f);

                    // アイテムに +2 表示（金）
                    var plusOneText = new UIObject
                    {
                        Name = "ResultItemBonusText",
                        PosX = itemText.PosX + 60,
                        PosY = itemText.PosY,
                        ZIndex = 420000,
                        FontSize = 18,
                        TextColor = "#FFD700",
                        Text = "+2",
                        Opacity = 0f
                    };
                    scene.AddUI(plusOneText);
                    plusOneText.StartFadeIn(0.5f);

                    await Task.Delay(1000);
                }
                else if (bonusZeroAlert)
                {
                    var bonusText = new UIObject
                    {
                        Name = "ResultBonusText",
                        CenterX = true,
                        PosY = posY + 170,
                        ZIndex = 420000,
                        FontSize = 18,
                        TextColor = "#00FF00",
                        Text = "【痕跡を残さず完遂】",
                        TextAlign = "center",
                        Opacity = 0f
                    };
                    scene.AddUI(bonusText);
                    bonusText.StartFadeIn(0.8f);

                    // アイテムに +1 表示（緑）
                    var plusOneText = new UIObject
                    {
                        Name = "ResultItemBonusText",
                        PosX = itemText.PosX + 60,
                        PosY = itemText.PosY,
                        ZIndex = 420000,
                        FontSize = 18,
                        TextColor = "#00FF00",
                        Text = "+1",
                        Opacity = 0f
                    };
                    scene.AddUI(plusOneText);
                    plusOneText.StartFadeIn(0.5f);

                    await Task.Delay(1000);
                }
            }

            // === 5. 確認ボタン ===
            var okBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "ResultOkButton";
                ui.CenterX = true;
                ui.PosY = posY + 210;
                ui.ZIndex = 430000;
                ui.Text = "確認";
                ui.FontSize = 24;
                ui.TextColor = "#FFFFFF";
                ui.Opacity = 0f;
                ui.TextAlign = "center";
                ui.OnClick = async () =>
                {
                    // フェードアウト対象を収集
                    var fadeTargets = scene.UIObjects.Values
                        .Where(o => o.Name.StartsWith("Result") && o != ui)
                        .ToList();

                    fadeTargets.Add(ui); // OKボタン自身も含める

                    // 順次フェードアウト
                    int steps = 10;
                    for (int i = 0; i <= steps; i++)
                    {
                        float alpha = 1f - (i / (float)steps);
                        foreach (var target in fadeTargets)
                        {
                            target.Opacity = alpha;
                            target.MarkDirty();
                        }
                        await Task.Delay(40);
                    }

                    // 後始末
                    foreach (var target in fadeTargets)
                        scene.RemoveUI(target);

                    if (onOk != null)
                        await onOk();

                    // マスクをフェードアウト
                    for (int i = 0; i <= 10; i++)
                    {
                        mask.Opacity = 0.7f - (i / 10f * 0.7f);
                        mask.MarkDirty();
                        await Task.Delay(40);
                    }
                    scene.RemoveUI(mask);
                };
            });
            scene.AddUI(okBtn);

            okBtn.StartFadeIn(0.5f);
        }

        /// <summary>
        /// リザルト抽選
        /// </summary>
        private StealthRewardResult GenerateRewards(StealthBoardSetup setup, bool isPerfect, bool isStealthKillPerfect = false)
        {
            var rewardGen = new StealthRewardGenerator();
            var result = new StealthRewardResult();

            // --- ゴールド ---
            int moneyCellCount = _cells.Cast<StealthBoardCell>()
                .Count(c => c.IsReward && !c.IsItemReward); // 金枠だけ数える

            result.Gold = rewardGen.GenerateGold(
                setup.TotalMoney,
                moneyCellCount,
                _moneyPickupCount   // 実際に拾った金枠数
            );

            // --- アイテム ---
            int normalCount = _itemPickupCount;   // 拾ったアイテム枠数
            int perfectCount = 0;

            if (isPerfect)
            {
                perfectCount = isStealthKillPerfect ? 2 : 1;
            }

            result.ItemIds = rewardGen.GenerateItems(
                setup.ItemDropTable,
                setup.PerfectItemDropTable,
                normalCount,
                perfectCount
            );

            return result;
        }
    }

    public class StealthBoardCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsRevealed { get; set; }
        public TrapType? Trap { get; set; }
        public Character? EnemyInstance { get; set; }
        public int RevealedStep { get; set; } = -1; // 何回目の開封ステップで開けたか
        public bool TrapTriggered { get; set; } = false; // ★罠を踏んだことがあるか

        // 報酬管理（抽選は戦闘勝利時）
        public bool IsReward { get; set; } = false;     // 報酬枠かどうか
        public bool IsItemReward { get; set; } = false; // アイテム報酬枠かどうか
    }

    public enum StealthMode
    {
        Explore, // 探索（通常の足跡進行）
        Focus    // 集中（罠解除や暗殺）
    }
}
