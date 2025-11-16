using BlazorApp.Game.SceneFactory;
using BlazorApp.Game.UIObjectFactory;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace BlazorApp.Game
{
    /// <summary>
    /// 基本シーン（バトル/ホーム/タイトル等の共通基盤）
    /// </summary>
    public class Scene
    {
        /// <summary>シーンの状態種別</summary>
        public GameState State { get; set; }

        /// <summary>背景情報</summary>
        public Background Background { get; set; } = new();

        /// <summary>シーン固有のキャラクター一覧</summary>
        public Dictionary<int, Character> Characters { get; set; } = new();

        /// <summary>シーン固有のUIオブジェクト一覧</summary>
        public Dictionary<int, UIObject> UIObjects { get; set; } = new();

        /// <summary>共通UIオブジェクト一覧（タイトル・ホーム遷移ボタン等）</summary>
        public Dictionary<int, UIObject> CommonUIObjects { get; set; } = new();

        // === 内部キャッシュ ===
        private List<UIObject> _sortedUI = new();
        private List<UIObject> _sortedCommonUI = new();
        private bool _uiDirty = true;
        private bool _commonUiDirty = true;

        private UIObject _expIcon;
        private UIObject _expText;
        private UIObject _expBg;
        private int _expPopupToken = 0;

        private UIObject _moneyIcon;
        private UIObject _moneyText;
        private UIObject _moneyBg;
        private int _moneyPopupToken = 0;

        private readonly Dictionary<string, List<UIObject>> _skillUiGroups = new();

        // === スキル説明ポップアップ ===
        private UIObject? _popupMask, _popupWindow, _popupText;

        private static bool _isPrologueRunning = false;

        private TaskCompletionSource<bool>? _tipsTcs;

        /// <summary>
        /// 毎フレーム更新処理
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            Background?.Update(deltaTime);

            // キャラ更新
            foreach (var ch in Characters.Values.ToList())
                ch.UpdateAnimation(deltaTime);

            // UI更新（ソートキャッシュ利用）
            if (_uiDirty)
            {
                _sortedUI = UIObjects.Values.OrderBy(u => u.ZIndex).ToList();
                _uiDirty = false;
            }
            foreach (var ui in _sortedUI.ToList())
            {
                // 長押し判定
                if (ui._isPressing && !ui._longPressTriggered)
                {
                    ui._pressTimer += deltaTime;
                    if (ui._pressTimer >= ui.LongPressThreshold)
                    {
                        ui._longPressTriggered = true;

                        Console.WriteLine($"DEBUG: Invoke {ui.Name} {ui.OnLongPressStart != null}");
                        ui.OnLongPressStart?.Invoke();
                    }
                }

                ui.UpdateRecursive(deltaTime);
            }

            if (_commonUiDirty)
            {
                _sortedCommonUI = CommonUIObjects.Values.OrderBy(u => u.ZIndex).ToList();
                _commonUiDirty = false;
            }
            foreach (var ui in _sortedCommonUI.ToList())
                ui.UpdateRecursive(deltaTime);
        }

        /// <summary>
        /// UIオブジェクトを追加
        /// </summary>
        public void AddUI(UIObject ui)
        {
            UIObjects[ui.ObjectId] = ui;
            _uiDirty = true;
        }

        /// <summary>
        /// UIオブジェクトを削除
        /// </summary>
        public void RemoveUI(UIObject ui)
        {
            if (UIObjects.Remove(ui.ObjectId))
                _uiDirty = true;   // 実際に削除があった場合のみ Dirty
        }

        /// <summary>
        /// 共通UIオブジェクトを追加
        /// </summary>
        public void AddCommonUI(UIObject ui)
        {
            CommonUIObjects[ui.ObjectId] = ui;
            _commonUiDirty = true;
        }

        /// <summary>
        /// 共通UIオブジェクトを全削除
        /// </summary>
        public void ClearCommonUI()
        {
            CommonUIObjects.Clear();
            _commonUiDirty = true;
        }

        /// <summary>
        /// 共通UIオブジェクトを生成（タイトル遷移ボタン等）
        /// </summary>
        public void SetupCommonUIObjects()
        {
            ClearCommonUI();
            int id = Common.GlobalUiBaseId;

            // タイトルへ戻るボタン
            var btn1 = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "タイトルボタン";
                ui.PosX = 5f;
                ui.PosY = Common.CanvasHeight - 60f;
                ui.Text = "タイトルへ";
                ui.FontSize = 10;
                ui.Opacity = 0.2f;
                ui.OnClick = () => GameMain.Instance.StartFadeTransition(GameState.Title);
            });
            AddCommonUI(btn1);

            // テストモードボタン
            var btn2 = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "テストモードボタン";
                ui.PosX = 5f;
                ui.PosY = Common.CanvasHeight - 30f;
                ui.Text = "テストモード";
                ui.FontSize = 10;
                ui.Opacity = 0.2f;
                ui.OnClick = () => GameMain.Instance.ChangeState(GameState.TestMode);
            });
            AddCommonUI(btn2);
        }

        /// <summary>
        /// 名前でUIを検索
        /// </summary>
        public bool TryGetUI(string name, [MaybeNullWhen(false)] out UIObject ui)
        {
            ui = UIObjects.Values.FirstOrDefault(u => u.Name == name);
            return ui != null;
        }

        /// <summary>
        /// テロップを表示（フェードイン→保持→フェードアウト）
        /// </summary>
        public async Task ShowTelopAsync(
            string text,
            string subText = null,
            float holdSeconds = 2.0f,
            float fadeSeconds = 0.8f,
            float maskOpacity = 0.6f)
        {
            // --- マスク作成 ---
            var mask = new UIObject
            {
                Name = $"TelopMask_{Guid.NewGuid()}",
                PosX = 0,
                PosY = 0,
                ZIndex = 300000,
                Opacity = 0f,
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
            AddUI(mask);

            var subTelop = null as UIObject;

            if (subText != null)
            {
                // --- サブテロップ ---
                subTelop = new UIObject
                {
                    Name = $"SubTelopText_{Guid.NewGuid()}",
                    CenterX = true,
                    PosY = Common.CanvasHeight / 2 - 210,
                    Text = subText,
                    FontSize = 20,
                    FontFamily = "\"Yu Mincho, serif\"",
                    TextColor = "#FFFFFF",
                    TextAlign = "center",
                    Opacity = 0f,
                    ZIndex = 390000,
                };
                AddUI(subTelop);
            }

            // --- テロップ本体（背景つき） ---
            var telop = new UIObject
            {
                Name = $"TelopText_{Guid.NewGuid()}",
                CenterX = true,
                PosY = Common.CanvasHeight / 2 - 180,
                Text = text,
                FontSize = 32,
                FontFamily = "\"Yu Mincho, serif\"",
                TextColor = "#FFFFFF",
                TextAlign = "center",
                Opacity = 0f,
                ZIndex = 390000,
                StretchToText = true,
                TextOffsetY = 10f,
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
            AddUI(telop);

            // --- 演出シーケンス ---
            // マスクフェードイン
            for (int i = 0; i <= 5; i++)
            {
                mask.Opacity = i / 5f * maskOpacity;
                mask.MarkDirty();
                await Task.Delay((int)(fadeSeconds * 100 / 20));
            }

            // テロップフェードイン
            telop.StartFadeIn(fadeSeconds);
            if (subTelop != null)
            {
                subTelop.StartFadeIn(fadeSeconds);
            }
            await Task.Delay((int)((fadeSeconds + holdSeconds) * 1000));

            // テロップフェードアウト
            for (int i = 0; i <= 5; i++)
            {
                telop.Opacity = 1f - i / 5f;
                telop.MarkDirty();
                if (subTelop != null)
                {
                    subTelop.Opacity = 1f - i / 5f;
                    subTelop.MarkDirty();
                }
                await Task.Delay((int)(fadeSeconds * 100 / 20));
            }
            RemoveUI(telop);
            if (subTelop != null)
            {
                RemoveUI(subTelop);
            }

            // マスクフェードアウト
            for (int i = 0; i <= 5; i++)
            {
                mask.Opacity = maskOpacity - (i / 5f * maskOpacity);
                mask.MarkDirty();
                await Task.Delay((int)(fadeSeconds * 100 / 20));
            }
            RemoveUI(mask);
        }

        /// <summary>
        /// 経験値表示
        /// </summary>
        public void CreateGlobalExpUI()
        {
            float baseX = Common.CanvasWidth - 120;
            float baseY = 5f;

            // 背景
            _expBg = new UIObject
            {
                Name = "GlobalExpBg",
                PosX = baseX,
                PosY = baseY,
                ZIndex = 900000,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-01.png",
                                    new System.Drawing.Rectangle(0, 0, 360, 180)) {
                                    ScaleX = 0.3f,
                                    ScaleY = 0.12f
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            AddUI(_expBg);

            // アイコン
            _expIcon = new UIObject
            {
                Name = "GlobalExpIcon",
                PosX = baseX,
                PosY = baseY,
                ZIndex = 999999,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui04-00.png",
                                    new System.Drawing.Rectangle(64*8, 64*1, 64, 64)) {
                                    ScaleX = 0.3f,
                                    ScaleY = 0.3f
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            AddUI(_expIcon);

            // テキスト
            _expText = new UIObject
            {
                Name = "GlobalExpText",
                PosX = baseX + 80,
                PosY = baseY + 3,
                ZIndex = 999999,
                FontSize = 16,
                TextColor = "#FFFFFF",
                Text = $"{GameMain.Instance.PlayerExp}",
                TextAlign = "right",
            };
            AddUI(_expText);
        }

        public void UpdateGlobalExpUI()
        {
            if (_expText != null)
            {
                _expText.Text = $"{GameMain.Instance.PlayerExp}";
                _expText.MarkDirty();
            }
        }

        /// <summary>
        /// 経験値獲得演出
        /// </summary>
        public async void ShowExpChange(int amount)
        {
            if (_expText == null) return;

            // 新しいトークンを発行
            int myToken = ++_expPopupToken;

            // 表示色（プラスは青、マイナスは赤）
            string color = amount >= 0 ? "#66CCFF" : "#FF4444";
            string sign = amount >= 0 ? "+" : "-";

            // --- 獲得テキストオブジェクト ---
            var popup = new UIObject
            {
                Name = $"ExpPopup_{Guid.NewGuid()}",
                PosX = _expText.PosX + 3, // 本体の右側
                PosY = _expText.PosY,
                ZIndex = _expText.ZIndex + 1,
                FontSize = 14,
                TextColor = color,
                Text = $"{sign}{Math.Abs(amount)}",
                Opacity = 1.0f
            };
            AddUI(popup);

            // --- 本体の色を一時変更 ---
            var originalColor = _expText.TextColor;
            _expText.TextColor = color;
            _expText.MarkDirty();

            // フェードアウト＆上移動
            int steps = 20;
            for (int i = 0; i < steps; i++)
            {
                popup.PosY -= 1.5f;                 // 上に移動
                popup.Opacity = 1.0f - (i / (float)steps); // 徐々に消える
                popup.MarkDirty();
                await Task.Delay(30);
            }

            // 後始末
            RemoveUI(popup);

            // ★ 最新トークンだけが色を戻せる
            if (myToken == _expPopupToken)
            {
                _expText.TextColor = "#FFFFFF"; // デフォルト色に戻す
                _expText.MarkDirty();
            }
        }

        /// <summary>
        /// グローバル所持金表示
        /// </summary>
        public void CreateGlobalMoneyUI()
        {
            float baseX = 10f;
            float baseY = 5f;

            // 背景
            _moneyBg = new UIObject
            {
                Name = "GlobalMoneyBg",
                PosX = baseX,
                PosY = baseY,
                ZIndex = 900000,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-01.png",
                                    new System.Drawing.Rectangle(0, 0, 360, 180)) {
                                    ScaleX = 0.3f,
                                    ScaleY = 0.12f
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            AddUI(_moneyBg);

            // アイコン
            _moneyIcon = new UIObject
            {
                Name = "GlobalMoneyIcon",
                PosX = baseX,
                PosY = baseY,
                ZIndex = 999999,
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
            AddUI(_moneyIcon);

            // テキスト
            _moneyText = new UIObject
            {
                Name = "GlobalMoneyText",
                PosX = baseX + 80,
                PosY = baseY + 3,
                ZIndex = 999999,
                FontSize = 16,
                TextColor = "#FFFFFF",
                Text = $"{GameMain.Instance.PlayerMoney}",
                TextAlign = "right",
            };
            AddUI(_moneyText);
        }

        public void UpdateGlobalMoneyUI()
        {
            if (_moneyText != null)
            {
                _moneyText.Text = $"{GameMain.Instance.PlayerMoney}";
                _moneyText.MarkDirty();
            }
        }

        /// <summary>
        /// 所持金増減演出
        /// </summary>
        public async void ShowMoneyChange(int amount)
        {
            if (_moneyText == null) return;

            // トークン発行
            int myToken = ++_moneyPopupToken;

            // 色と符号
            string color = amount >= 0 ? "#66CCFF" : "#FF4444";
            string sign = amount >= 0 ? "+" : "-";

            // ポップアップ
            var popup = new UIObject
            {
                Name = $"MoneyPopup_{Guid.NewGuid()}",
                PosX = _moneyText.PosX + 3,
                PosY = _moneyText.PosY,
                ZIndex = _moneyText.ZIndex + 1,
                FontSize = 14,
                TextColor = color,
                Text = $"{sign}{Math.Abs(amount)}",
                Opacity = 1.0f
            };
            AddUI(popup);

            // 本体の色を一時変更
            var originalColor = _moneyText.TextColor;
            _moneyText.TextColor = color;
            _moneyText.MarkDirty();

            // アニメーション
            int steps = 20;
            for (int i = 0; i < steps; i++)
            {
                popup.PosY -= 1.5f;
                popup.Opacity = 1.0f - (i / (float)steps);
                popup.MarkDirty();
                await Task.Delay(30);
            }

            RemoveUI(popup);

            // 最新トークンのみが色を戻せる
            if (myToken == _moneyPopupToken)
            {
                _moneyText.TextColor = "#FFFFFF";
                _moneyText.MarkDirty();
            }
        }

        /// <summary>
        /// 簡易エラー表示
        /// </summary>
        public async Task ShowErrorQuickAsync(string text)
        {
            // 既存メッセージを探して削除
            var existing = UIObjects.Values
                .FirstOrDefault(u => u.Name.StartsWith("ErrorMsg_"));
            if (existing != null)
                RemoveUI(existing);

            var msg = new UIObject
            {
                Name = $"ErrorMsg_{Guid.NewGuid()}",
                PosX = Common.CanvasWidth - 10,
                PosY = Common.CanvasHeight - 30,
                Text = text,
                FontSize = 14,
                TextColor = "#FF4444",
                TextAlign = "right",
                ZIndex = 500000,
                Opacity = 1f
            };
            AddUI(msg);

            await Task.Delay(1200); // 1.2秒表示
            RemoveUI(msg);
        }

        /// <summary>
        /// 武器傾向と重量段階に応じた背景スプライトを返す
        /// </summary>
        public (string sheet, Rectangle rect) GetWeaponBackgroundSprite(Equipment eq)
        {
            string basePath = "images/ui04-00.png"; // あなたが用意する背景スプライト集
            int sx = 0, sy = 8;

            // 傾向別ベース位置
            switch (eq.Trend)
            {
                case "穿特化": sx = 0; break;
                case "迅特化": sx = 3; break;
                case "剛特化": sx = 6; break;
                case "万能型": sx = 9; break;
                default: sx = 9; break;
            }

            // 重量段階で差し替え（左から順にノーマル・強化1・強化2）
            if (Math.Abs(eq.ForgeWeight) >= 10 && Math.Abs(eq.ForgeSharp) >= 10)
                sx += 2;
            else if (Math.Abs(eq.ForgeWeight) >= 10 || Math.Abs(eq.ForgeSharp) >= 10)
                sx += 1;

            // 各タイルは 64×64 想定
            return (basePath, new Rectangle(sx * 64, sy * 64, 64, 64));
        }

        /// <summary>
        /// 三軸ポリゴン背景と塗りつぶしグラフを追加または更新
        /// （最大回数＋スキル反映＆増加箇所は緑文字）
        /// </summary>
        public void CreateWeaponPolygonGraph(Equipment eq, float centerX, float centerY, float radius, string prefix = "ForgeGraph")
        {
            // --- 傾向色決定 ---
            string fillColor = eq.Trend switch
            {
                "穿特化" => "#FFD700", // 黄
                "迅特化" => "#00AAFF", // 青
                "剛特化" => "#FF3333", // 赤
                "万能" => "#FFFFFF",   // 白
                _ => "#FFFFFF"
            };

            // --- 古いグラフUIを削除 ---
            var oldObjs = UIObjects.Values
                .Where(u => u.Name.StartsWith(prefix + "_"))
                .ToList();
            foreach (var obj in oldObjs)
                RemoveUI(obj);

            // === 背景 ===
            var bg = new UIObject
            {
                Name = $"{prefix}_Bg",
                PosX = centerX - radius - 10,
                PosY = centerY - radius - 22,
                ZIndex = 499,
                Opacity = 0.5f,
                Animations = new()
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new() {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui04-00.png",
                                    new Rectangle(64 * 9, 0, 64 * 2, 64 * 2))
                                {
                                    ScaleX = 1.1f,
                                    ScaleY = 1.1f
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            AddUI(bg);

            // === スキル反映済みの手数算出 ===
            int thrust = eq.HandsThrust;
            int slash = eq.HandsSlash;
            int down = eq.HandsDown;

            bool plusThrust = eq.Skills.Any(s => s.Id.Contains("穿・最大値"));
            bool plusSlash = eq.Skills.Any(s => s.Id.Contains("迅・最大値"));
            bool plusDown = eq.Skills.Any(s => s.Id.Contains("剛・最大値"));

            if (plusThrust) thrust++;
            if (plusSlash) slash++;
            if (plusDown) down++;

            bool plusAll = eq.Skills.Any(s => s.Id.Contains("体の極意"));

            if (plusAll)
            {
                thrust++;
                slash++;
                down++;
            }

            // === ポリゴン算出 ===
            float maxVal = 8f; // 手数最大想定
            float pPierce = thrust / maxVal;
            float pSlash = slash / maxVal;
            float pDown = down / maxVal;

            float radPierce = -90f * (float)Math.PI / 180f;
            float radSlash = 150f * (float)Math.PI / 180f;
            float radDown = 30f * (float)Math.PI / 180f;

            var pts = new List<(float x, float y)>
            {
                (centerX + radius * pPierce * (float)Math.Cos(radPierce),
                 centerY + radius * pPierce * (float)Math.Sin(radPierce)),
                (centerX + radius * pSlash  * (float)Math.Cos(radSlash),
                 centerY + radius * pSlash  * (float)Math.Sin(radSlash)),
                (centerX + radius * pDown   * (float)Math.Cos(radDown),
                 centerY + radius * pDown   * (float)Math.Sin(radDown))
            };

            // === ポリゴン塗りつぶし ===
            var poly = new UIObjectPolygon
            {
                Name = $"{prefix}_Polygon",
                Points = pts,
                FillColor = fillColor,
                FillOpacity = 0.5f,
                ZIndex = 500
            };
            AddUI(poly);

            // === 各軸ラベル（増加した箇所は緑） ===
            CreateGraphLabel($"{prefix}_Label_Pierce",
                thrust.ToString(),
                pts[0].x - 1, pts[0].y - 10,
                (plusThrust || plusAll) ? "#66FF66" : "#FFFF66");

            CreateGraphLabel($"{prefix}_Label_Slash",
                slash.ToString(),
                pts[1].x - 3, pts[1].y - 4,
                (plusSlash || plusAll) ? "#66FF66" : "#FFFF66");

            CreateGraphLabel($"{prefix}_Label_Down",
                down.ToString(),
                pts[2].x + 3, pts[2].y - 4,
                (plusDown || plusAll) ? "#66FF66" : "#FFFF66");
        }

        /// <summary>
        /// グラフ数値ラベル生成（色指定対応）
        /// </summary>
        private void CreateGraphLabel(string name, string text, float x, float y, string color = "#FFFF66")
        {
            var label = new UIObject
            {
                Name = name,
                PosX = x,
                PosY = y,
                ZIndex = 510,
                FontSize = 12,
                Text = text,
                TextColor = color,
                TextAlign = "center"
            };
            AddUI(label);
        }

        /// <summary>
        /// 既存グラフの更新（装備変更や強化時に呼ぶ）
        /// </summary>
        public void UpdateWeaponPolygonGraph(Equipment eq)
        {
            if (!TryGetUI($"WeaponGraphPolygon_{eq.Id}", out var ui) || ui is not UIObjectPolygon poly) return;

            float centerX = poly.Points.Average(p => p.x);
            float centerY = poly.Points.Average(p => p.y);
            float radius = 80f; // 同値を再使用

            float maxVal = 6f;
            float pPierce = eq.HandsThrust / maxVal;
            float pSlash = eq.HandsSlash / maxVal;
            float pDown = eq.HandsDown / maxVal;

            float radPierce = -90f * (float)Math.PI / 180f;
            float radSlash = 150f * (float)Math.PI / 180f;
            float radDown = 30f * (float)Math.PI / 180f;

            poly.Points = new List<(float, float)>
            {
                (centerX + radius * pPierce * (float)Math.Cos(radPierce),
                 centerY + radius * pPierce * (float)Math.Sin(radPierce)),
                (centerX + radius * pSlash  * (float)Math.Cos(radSlash),
                 centerY + radius * pSlash  * (float)Math.Sin(radSlash)),
                (centerX + radius * pDown   * (float)Math.Cos(radDown),
                 centerY + radius * pDown   * (float)Math.Sin(radDown))
            };
            poly.MarkDirty();
        }

        public void ShowCharacterSkills(Character ch, float baseX = 230, float baseY = 80, int columns = 1)
        {
            var skills = new List<Skill>();
            if (ch.Skills?.Count > 0) skills.AddRange(ch.Skills);

            var ninjaTool = EquipmentManager.Instance.GetEquipped(ch, "忍具");
            if (ninjaTool?.Skills?.Count > 0) skills.AddRange(ninjaTool.Skills);

            var weapon = EquipmentManager.Instance.GetEquipped(ch, "武器");
            if (weapon?.Skills?.Count > 0) skills.AddRange(weapon.Skills);

            ShowCharacterSkillsCore(skills, baseX, baseY, columns, ch.Name);
        }

        public void ShowCharacterSkills(Equipment eq, float baseX = 230f, float baseY = 80f, int columns = 1)
        {
            var skills = eq.Skills?.ToList() ?? new List<Skill>();
            ShowCharacterSkillsCore(skills, baseX, baseY, columns, eq.Id);
        }


        private void ShowCharacterSkillsCore(List<Skill> skills, float baseX, float baseY, int columns, string groupKey)
        {
            // 既存グループの削除
            if (_skillUiGroups.TryGetValue(groupKey, out var oldList))
            {
                foreach (var u in oldList)
                    RemoveUI(u);
                _skillUiGroups.Remove(groupKey);
            }

            var newList = new List<UIObject>();

            // === ★ スキルが1つもない場合 ===
            if (skills == null || skills.Count == 0)
            {
                // 想定するスキル表示エリアのサイズ
                // columns と 3行程度を想定して中央揃え（必要なら調整）
                float areaWidth = columns * 86f;
                float areaHeight = 3 * 26f;  // スキル3行分の高さを想定

                // 中心座標を算出
                float centerX = baseX + areaWidth / 2f;
                float centerY = baseY + areaHeight / 2f;

                var noSkill = new UIObject
                {
                    Name = $"SkillEmpty_{groupKey}",
                    CenterX = false,
                    PosX = centerX,
                    PosY = centerY,
                    ZIndex = 3500,
                    Text = "発動能力無し",
                    FontSize = 14,
                    TextColor = "#AAAAAA",
                    TextAlign = "center", // 中央寄せに変更
                };
                AddUI(noSkill);
                newList.Add(noSkill);
                _skillUiGroups[groupKey] = newList;
                return;
            }

            string[] categoryOrder =
            {
                "キャラ",
                "忍具",
                "武器固定",
                "武器スロット",
                "最大鍛錬",
                "武器鍛錬",
            };

            var orderedSkills = skills
                .OrderBy(s => Array.IndexOf(categoryOrder, s.Category))
                .ThenBy(s => s.Id)
                .ToList();

            int offsetX = 86;
            int offsetY = 26;

            for (int i = 0; i < orderedSkills.Count; i++)
            {
                var skill = orderedSkills[i];
                int col = i % columns;
                int row = i / columns;

                string bgColor = skill.Category switch
                {
                    "キャラ" => "#FFD700",
                    "忍具" => "#66FF66",
                    "武器固定" => "#FF6666",
                    "武器スロット" => "#CC66FF",
                    "武器鍛錬" => "#33AAEE",
                    "最大鍛錬" => "#2277FF",
                    _ => "#FFFFFF"
                };

                var ui = new UIObject
                {
                    Name = $"SkillIcon_{groupKey}_{skill.Id}_{i}",
                    PosX = baseX + col * offsetX,
                    PosY = baseY + row * offsetY,
                    ZIndex = 3500,
                    Opacity = 0.95f,
                    Text = skill.DisplayName,
                    FontSize = 10,
                    TextAlign = "left",
                    TintColor = bgColor,
                    TextOffsetX = 3f,
                    TextOffsetY = 2f,
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        {
                            "idle", new GameObjectAnimation
                            {
                                Frames = new List<GameObjectAnimationFrame>
                                {
                                    new GameObjectAnimationFrame
                                    {
                                        Sprite = new Sprite("images/ui01-00.png",
                                            new Rectangle(360*2, 0, 360, 180))
                                        {
                                            ScaleX = 0.25f,
                                            ScaleY = 0.14f,
                                        }
                                    }
                                }
                            }
                        }
                    },
                    CurrentAnimationName = "idle"
                };

                ui.OnLongPressStart = () => SkillPopupHelper.Show(this, skill);
                ui.OnLongPressRelease = () => SkillPopupHelper.Close(this);

                AddUI(ui);
                newList.Add(ui);
            }

            // 新しいグループを登録
            _skillUiGroups[groupKey] = newList;
        }

        /// <summary>
        /// すべてのスキルUIグループを削除（シーン切り替え・タブ変更時など）
        /// </summary>
        public void ClearAllSkillUI()
        {
            foreach (var kv in _skillUiGroups)
            {
                foreach (var ui in kv.Value)
                    RemoveUI(ui);
            }
            _skillUiGroups.Clear();
        }

        /// <summary>
        /// 特定キャラまたは装備IDのスキルUIを削除
        /// </summary>
        public void ClearSkillUI(string groupKey)
        {
            if (_skillUiGroups.TryGetValue(groupKey, out var list))
            {
                foreach (var u in list)
                    RemoveUI(u);
                _skillUiGroups.Remove(groupKey);
            }
        }

        /// <summary>
        /// プロローグ用オーバレイ
        /// </summary>

        public async Task RunPrologueOverlayAsync(
             IReadOnlyList<string> chunks,
             float maskOpacity = 0.65f,
             float fadeInSec = 0.8f,
             float fadeOutSec = 0.4f,
             Action? onCompleted = null)
        {
            if (_isPrologueRunning)
                return; // ← すでに実行中なら無視

            _isPrologueRunning = true;

            // --- 1. 背景マスク（暗転） ---
            var mask = new UIObject
            {
                Name = $"PrologueMask_{Guid.NewGuid()}",
                PosX = 0,
                PosY = 0,
                ZIndex = 300000,
                Opacity = 0f,
                Animations = new()
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new() {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui06-01.png",
                                    new System.Drawing.Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            AddUI(mask);

            int steps = 8;
            for (int i = 0; i <= steps; i++)
            {
                mask.Opacity = maskOpacity * (i / (float)steps);
                mask.MarkDirty();
                await Task.Delay((int)(fadeInSec * 1000 / steps));
            }

            // --- 2. クリックキャッチャ ---
            var clickCatcher = new UIObject
            {
                Name = $"PrologueClickCatcher_{Guid.NewGuid()}",
                PosX = 0,
                PosY = 0,
                ZIndex = 350000,
                Opacity = 0.01f,
                Animations = new()
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new() {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui06-01.png",
                                    new System.Drawing.Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            AddUI(clickCatcher);

            Task WaitClickAsync()
            {
                var tcs = new TaskCompletionSource();
                clickCatcher.OnClick = () =>
                {
                    clickCatcher.OnClick = null; // ←二重クリック防止
                    tcs.TrySetResult();
                };
                return tcs.Task;
            }

            // --- 3. テキストUI (UIObjectMultilineText) ---
            UIObjectMultilineText? textUi = null;

            foreach (var block in chunks)
            {
                if (textUi != null) RemoveUI(textUi);

                textUi = new UIObjectMultilineText
                {
                    Name = $"PrologueText_{Guid.NewGuid()}",
                    PosX = 30f,
                    PosY = 200f,
                    ZIndex = 360000,
                    FontSize = 15,
                    FontFamily = "\"Yu Mincho, serif\"",
                    TextColor = "#FFFFFF",
                    LineSpacing = 6f,
                    TextAlign = "left",
                    Text = block,
                    Opacity = 0f,
                };
                AddUI(textUi);

                // フェードイン
                textUi.StartFadeIn(fadeInSec);

                // クリック待ち
                await WaitClickAsync();

                // フェードアウト
                int outSteps = 10;
                for (int i = 0; i <= outSteps; i++)
                {
                    textUi.Opacity = 1f - (i / (float)outSteps);
                    textUi.MarkDirty();
                    await Task.Delay((int)(fadeOutSec * 1000 / outSteps));
                }

                RemoveUI(textUi);
                textUi = null;
            }

            // --- 4. 終了処理 ---
            RemoveUI(clickCatcher);

            for (int i = 0; i <= steps; i++)
            {
                mask.Opacity = maskOpacity * (1f - i / (float)steps);
                mask.MarkDirty();
                await Task.Delay((int)(fadeOutSec * 1000 / steps));
            }
            RemoveUI(mask);

            onCompleted?.Invoke();

            _isPrologueRunning = false;
        }

        /// <summary>
        /// チュートリアルTipsを表示（画像番号＋複数ページメッセージ対応）
        /// </summary>
        public async Task ShowTutorialTipsAsync(IEnumerable<string> messages, int imageNumber, int offsetY = 0)
        {
            // ★二重起動防止ではなく「起動中は待機」に変更
            while (_tipsTcs != null)
            {
                await Task.Delay(100); // 他のTipsが閉じるのを待つ
            }

            _tipsTcs = new TaskCompletionSource<bool>();

            // === 1. マスク ===
            var mask = new UIObject
            {
                Name = "TutorialTipsMask",
                PosX = 0,
                PosY = 0,
                ZIndex = 900000,
                Opacity = 0.4f,
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
                },
            };
            AddUI(mask);

            // 軽い演出用の遅延
            await Task.Delay(800);

            // === 2. 背景ウィンドウ ===
            var window = new UIObject
            {
                Name = "TutorialTipsWindow",
                CenterX = true,
                PosY = Common.CanvasHeight / 2 + offsetY,
                ZIndex = 900030,
                Opacity = 0.8f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-00.png",
                                    new System.Drawing.Rectangle(0, 0, 360, 180)) {
                                    ScaleX = 0.95f,
                                    ScaleY = 0.7f
                                }
                            }
                        }
                    }}
                }
            };
            AddUI(window);

            // === 3. 画像 ===
            string imagePath = $"images/ui07-{imageNumber:D2}.png";

            var image = new UIObject
            {
                Name = $"TutorialTipsImage_{imageNumber}",
                PosX = 0,
                PosY = 0,
                ZIndex = 900020,
                Opacity = 0.8f,
                Enabled = false,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(imagePath,
                                    new System.Drawing.Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                            }
                        }
                    }}
                },
            };
            AddUI(image);

            // === 4. テキスト ===
            var text = new UIObjectMultilineText
            {
                Name = "TutorialTipsText",
                PosX = 32,
                PosY = window.PosY + 20,
                ZIndex = 900040,
                FontSize = 14,
                TextColor = "#FFFFFF",
                TextAlign = "left",
                Enabled = false,
            };
            AddUI(text);

            // === 5. ページ送りロジック ===
            var pages = messages.ToList();
            int currentPage = 0;

            void UpdatePageText()
            {
                text.Text = pages[currentPage];
                text.MarkDirty();
            }

            UpdatePageText();

            // === 6. 共通閉じ処理 ===
            Action closeAll = () =>
            {
                RemoveUI(mask);
                RemoveUI(window);
                RemoveUI(image);
                RemoveUI(text);
                _tipsTcs?.TrySetResult(true);
                _tipsTcs = null;
            };

            // === 7. ページ送り処理 ===
            void HandleTap()
            {
                if (currentPage < pages.Count - 1)
                {
                    currentPage++;
                    UpdatePageText(); // ★ ページ送り時にテキスト更新
                }
                else
                {
                    closeAll();
                }
            }

            // マスク・ウィンドウどちらでもタップで進行
            mask.OnClick = HandleTap;
            window.OnClick = HandleTap;

            await _tipsTcs.Task;
        }
    }

    /// <summary>
    /// シーンファクトリの解決クラス
    /// </summary>
    public class SceneFactoryResolver
    {
        private readonly Dictionary<GameState, BaseSceneFactory> _factories = new();

        /// <summary>コンストラクタでファクトリ群を登録</summary>
        public SceneFactoryResolver(IEnumerable<BaseSceneFactory> factories)
        {
            foreach (var factory in factories)
                _factories[factory.TargetState] = factory;
        }

        /// <summary>シーンを生成</summary>
        public Scene CreateScene(GameState state)
        {
            if (_factories.TryGetValue(state, out var factory))
            {
                var scene = factory.Create();
                scene.SetupCommonUIObjects();
                return scene;
            }
            throw new ArgumentException($"SceneFactory for {state} not registered.");
        }

        /// <summary>ペイロード付きシーンを生成</summary>
        public Scene CreateScene(GameState state, object? payload)
        {
            if (_factories.TryGetValue(state, out var factory))
            {
                var scene = factory.Create(payload);
                scene.SetupCommonUIObjects();
                return scene;
            }
            throw new ArgumentException($"SceneFactory for {state} not registered.");
        }
    }

    /// <summary>
    /// リザルト表示ヘルパ
    /// </summary>
    public static class SceneResultHelper
    {
        /// <summary>
        /// ステージリザルト表示
        /// </summary>
        public static async Task ShowStageResultAsync(
            Scene scene,
            StealthRewardResult rewards,
            bool isSuccess,
            Func<Task>? onOk = null)
        {
            float centerX = Common.CanvasWidth / 2;
            float posY = Common.CanvasHeight / 2 - 160;

            // === 暗転マスク ===
            var mask = new UIObject
            {
                Name = "StageResultMask",
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
                mask.Opacity = i / 10f * 0.8f;
                mask.MarkDirty();
                await Task.Delay(40);
            }

            // 共通・所持金描画
            scene.CreateGlobalMoneyUI();

            // === 任務完了タイトル ===
            var title = new UIObject
            {
                Name = "StageResultTitle",
                CenterX = true,
                PosY = posY,
                ZIndex = 410000,
                FontSize = 28,
                TextColor = "#FFFFFF",
                Text = isSuccess ? "任務完了" : "任務失敗…",
                TextAlign = "center",
                StretchToText = true,
                TextOffsetY = 10f,
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
            scene.AddUI(title);
            title.StartFadeIn(0.5f);
            await Task.Delay(1000);

            if (isSuccess)
            {
                // --- お金アイコン ---
                var moneyIcon = new UIObject
                {
                    Name = "StageResultMoneyIcon",
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
                moneyIcon.StartFadeIn(0.5f);

                // --- 金額換算を表示 ---
                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    var eqText = new UIObject
                    {
                        Name = "StageResultMoneyEq",
                        PosX = centerX -40,
                        PosY = posY + 70,
                        ZIndex = 420000,
                        FontSize = 18,
                        TextColor = "#FFFF66",
                        Text = $"＝ {rewards.Gold}",
                        Opacity = 0f
                    };
                    scene.AddUI(eqText);
                    eqText.StartFadeIn(0.5f);
                });

                // 所持金加算
                GameMain.Instance.AddMoney(rewards.Gold);

                // --- アイテム（縦並びにして名称付きで表示） ---
                float baseItemX = centerX - 80f;       // 左寄せ基準（中央よりやや左）
                float baseItemY = posY + 110f;         // 最初のY位置
                float itemSpacingY = 40f;              // アイテム同士の縦間隔

                for (int i = 0; i < rewards.ItemIds.Count; i++)
                {
                    float itemX = baseItemX;
                    float itemY = baseItemY + i * itemSpacingY;

                    // --- 仮アイコン ---
                    var dummyIcon = new UIObject
                    {
                        Name = $"StageResultItemDummy_{i}",
                        PosX = itemX,
                        PosY = itemY,
                        ZIndex = 420000,
                        Animations = new Dictionary<string, GameObjectAnimation>
                        {
                            { "idle", new GameObjectAnimation {
                                Frames = new List<GameObjectAnimationFrame> {
                                    new GameObjectAnimationFrame {
                                        Sprite = new Sprite("images/ui04-00.png",
                                            new System.Drawing.Rectangle(64*8,64*3,64,64)) {
                                            ScaleX = 0.6f,
                                            ScaleY = 0.6f
                                        },
                                        Duration = 1.0f
                                    }
                                }
                            }}
                        },
                        CurrentAnimationName = "idle",
                        Opacity = 0f
                    };
                    scene.AddUI(dummyIcon);
                    dummyIcon.StartFadeIn(0.5f);
                }

                await Task.Delay(500);

                // --- 遅延で1つずつ出現 ---
                _ = Task.Run(async () =>
                {
                    for (int i = 0; i < rewards.ItemIds.Count; i++)
                    {
                        var item = ItemManager.Instance.Get(rewards.ItemIds[i]);
                        if (item == null) continue;

                        ItemManager.Instance.Add(item.Id);
                        await Task.Delay(500);

                        float itemX = baseItemX;
                        float itemY = baseItemY + i * itemSpacingY;

                        // 煙エフェクト
                        ShowSmokeForItem(scene, itemX, itemY);
                        await Task.Delay(300);

                        // 仮アイコン削除
                        if (scene.TryGetUI($"StageResultItemDummy_{i}", out var dummy))
                        {
                            scene.RemoveUI(dummy);
                            dummy.MarkDirty();
                        }

                        // --- 本アイコン ---
                        var realIcon = new UIObject
                        {
                            Name = $"StageResultItemReal_{i}",
                            PosX = itemX,
                            PosY = itemY,
                            ZIndex = 450010,
                            Animations = new Dictionary<string, GameObjectAnimation>
                            {
                                { "idle", new GameObjectAnimation {
                                    Frames = new List<GameObjectAnimationFrame> {
                                        new GameObjectAnimationFrame {
                                            Sprite = new Sprite(item.SpriteSheet,
                                                new System.Drawing.Rectangle(item.SrcX, item.SrcY, 64, 64)) {
                                                ScaleX = 0.6f,
                                                ScaleY = 0.6f
                                            },
                                            Duration = 1.0f
                                        }
                                    }
                                }}
                            },
                            CurrentAnimationName = "idle"
                        };
                        realIcon.OnLongPressStart = () => ItemPopupHelper.Show(scene, item);
                        realIcon.OnLongPressRelease = () => ItemPopupHelper.Close(scene);
                        scene.AddUI(realIcon);
                        realIcon.StartFadeIn(0.5f);

                        // --- アイテム名称（右側に表示） ---
                        var nameText = new UIObject
                        {
                            Name = $"StageResultItemName_{i}",
                            PosX = itemX + 60f,  // アイコンの右側
                            PosY = itemY + 12f,
                            ZIndex = 450020,
                            FontSize = 15,
                            TextColor = "#FFFFFF",
                            Text = item.Id,
                            Opacity = 0f
                        };
                        scene.AddUI(nameText);
                        nameText.StartFadeIn(0.6f);
                    }
                });
            }

            // === OKボタン（待機可能）===
            var tcs = new TaskCompletionSource<bool>();

            var okBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "StageResultOk";
                ui.CenterX = true;
                ui.PosY = posY + 280;
                ui.ZIndex = 430000;
                ui.Text = "確認";
                ui.FontSize = 24;
                ui.TextColor = "#FFFFFF";
                ui.Opacity = 0f;
                ui.OnClick = () =>
                {
                    tcs.TrySetResult(true); // ボタン押下で完了
                };
            });
            scene.AddUI(okBtn);
            okBtn.StartFadeIn(0.5f);

            // 🔸 ここでユーザーのクリックを待つ
            await tcs.Task;

            // クリック後の後処理
            if (onOk != null) await onOk();

            var removeList = scene.UIObjects.Values
                .Where(o => o.Name.StartsWith("StageResult")).ToList();
            foreach (var u in removeList) scene.RemoveUI(u);

            scene.RemoveUI(mask);
            scene.RemoveUI(title);
            scene.RemoveUI(okBtn);
        }

        /// <summary>
        /// アイテム変化用の煙エフェクト
        /// </summary>
        private static void ShowSmokeForItem(Scene scene, float x, float y)
        {
            var smoke = new UIObject
            {
                Name = $"Smoke_{Guid.NewGuid()}",
                PosX = x,
                PosY = y,
                ZIndex = 450000,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "play", new GameObjectAnimation {
                        Loop = false,
                        Frames = GameObjectAnimation.CreateFramesFromSheet(
                            "images/ui04-00.png", 64, 64, 2, 0.3f,
                            offsetX: 64*7, offsetY: 64*2,
                            scaleX: 0.6f, scaleY: 0.6f
                        )
                    }}
                },
                CurrentAnimationName = "play"
            };
            smoke.OnAnimationCompleted += (obj, anim) => scene.RemoveUI(smoke);
            scene.AddUI(smoke);
        }
    }
}
