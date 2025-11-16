using BlazorApp.Game.SceneFactory;
using BlazorApp.Game.UIObjectFactory;
using System.Collections.Generic;
using System.Drawing;
using static System.Formats.Asn1.AsnWriter;

namespace BlazorApp.Game.SceneFactory
{
    /// <summary>
    /// 武器鍛錬シーン
    /// </summary>
    public class SceneForge : BaseSceneFactory
    {
        public override GameState TargetState => GameState.Forge;

        // フィールドとして保持（選択中マーカー用）
        private UIObject? _selectedMarker;

        private Equipment? _selectedEquipment;

        // ★ 追加：スキル情報表示UIリスト
        private readonly List<UIObject> _skillIcons = new();

        public override Scene Create()
        {
            var scene = new Scene
            {
                State = GameState.Forge
            };

            // === 背景 ===
            scene.Background = new Background
            {
                Layers = new List<BackgroundLayer>
                {
                    new BackgroundLayer
                    {
                        Sprite = new Sprite("images/bg015.png", new Rectangle(0,0,360,640)),
                        LoopScroll = false,
                        IsForeground = false
                    }
                }
            };

            // 共通：経験値描画
            scene.CreateGlobalExpUI();

            // 共通・所持金描画
            scene.CreateGlobalMoneyUI();

            // 重量ゲージ枠
            var barWeight = new UIObject
            {
                Name = "ForgeBarWeightFrame",
                PosX = 15,
                PosY = 290,
                ZIndex = 300,
                Enabled = false,
                Animations = new Dictionary<string, GameObjectAnimation> {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-00.png", new Rectangle(1*360,2*180,360,180)) {
                                    ScaleX = 0.45f,
                                    ScaleY = 0.40f
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(barWeight);

            // 重量ゲージ本体
            var gaugeWeight = new UIObjectGauge
            {
                Name = "ForgeBarWeight",
                PosX = barWeight.PosX + 8,
                PosY = barWeight.PosY + 25,
                ZIndex = 301, // 枠より下
                DrawnWidth = 145,
                DrawnHeight = 20,
                LeftColor = "#FF0000", // 左側を赤に変更
                RightColor = "#0000FF" // 右側を青に変更
            };
            scene.AddUI(gaugeWeight);

            // 鋭さゲージ枠
            var barSharp = new UIObject
            {
                Name = "ForgeBarSharpFrame",
                PosX = 185,
                PosY = 290,
                ZIndex = 300,
                Enabled = false,
                Animations = new Dictionary<string, GameObjectAnimation> {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-00.png", new Rectangle(1*360,2*180,360,180)) {
                                    ScaleX = 0.45f,
                                    ScaleY = 0.40f
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(barSharp);

            // 鋭さゲージ本体
            var gaugeSharp = new UIObjectGauge
            {
                Name = "ForgeBarSharp",
                PosX = barSharp.PosX + 8,
                PosY = gaugeWeight.PosY,
                ZIndex = 301,
                DrawnWidth = 145,
                DrawnHeight = 20,
                LeftColor = "#FFFFFF", // 左側を白に変更
                RightColor = "#FFFF00" // 右側を黄に変更
            };
            scene.AddUI(gaugeSharp);

            // 重量ゲージ数値ラベル
            var labelWeight = new UIObject
            {
                Name = "ForgeBarWeightLabel",
                PosX = gaugeWeight.PosX + gaugeWeight.DrawnWidth / 2,
                PosY = gaugeWeight.PosY + 3,
                ZIndex = 400,
                Text = "0",
                TextAlign = "center",
                TextColor = "#888888", // グレー
                FontSize = 16
            };
            scene.AddUI(labelWeight);

            // 鋭さゲージ数値ラベル
            var labelSharp = new UIObject
            {
                Name = "ForgeBarSharpLabel",
                PosX = gaugeSharp.PosX + gaugeSharp.DrawnWidth / 2,
                PosY = gaugeSharp.PosY + 3,
                ZIndex = 400,
                Text = "0",
                TextAlign = "center",
                TextColor = "#888888",
                FontSize = 16
            };
            scene.AddUI(labelSharp);

            // === 選択中武器の詳細表示領域 ===
            var detailText = new UIObjectMultilineText
            {
                Name = "ForgeWeaponDetail",
                PosX = 20,
                PosY = 45,
                ZIndex = 100,
                FontSize = 14,
                TextColor = "#FFFFFF",
                Text = "武器を選択してください"
            };
            scene.AddUI(detailText);

            var polygon = new UIObjectPolygon
            {
                Name = "ForgeGraph",
                PosX = 260,   // 表示位置（調整可）
                PosY = 140,
                ZIndex = 500,
                FillColor = "#00FF00",
                FillOpacity = 0.4f
            };
            scene.AddUI(polygon);

            // 鍛錬ボタン群を生成
            CreateForgeButtons(scene, 150 + 65*2);

            // === 武器一覧（下部ウィンドウ） ===
            float listHeight = 260f;   // 装備選択と同じくらいの高さ
            float listBaseY = Common.CanvasHeight - listHeight - 10;

            // ウィンドウ背景（装備選択と同じUI画像を使用）
            var listWindow = new UIObject
            {
                Name = "ForgeWeaponListWindow",
                PosX = 0,
                PosY = listBaseY,
                ZIndex = 50,
                Opacity = 0.6f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-00.png",
                                    new Rectangle(0,0,360,180)) {
                                    ScaleX = 1.0f,
                                    ScaleY = listHeight / 180f   // 高さだけ伸縮
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(listWindow);

            // 武器アイコン群を生成
            CreateWeaponIcons(scene, listBaseY);

            // === 選択中武器の超拡大アイコン ===
            var enlargedIcon = new UIObject
            {
                Name = "ForgeSelectedWeaponBackground",
                CenterX = true,
                PosY = 90,
                ZIndex = 10, // 奥のレイヤー
                Opacity = 0.4f,
            };
            scene.AddUI(enlargedIcon);

            // === 拠点に戻るボタン ===
            var backBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "BackToHomeButton";
                ui.PosX = 10f;
                ui.PosY = Common.CanvasHeight - 40f;
                ui.Text = "拠点に戻る";
                ui.FontSize = 16;
                ui.TextColor = "#FFFFFF";
                ui.ZIndex = 20000;
                ui.OnClick = () =>
                {
                    GameMain.Instance.StartFadeTransition(GameState.Home);
                };
            });
            scene.AddUI(backBtn);

            // === 費用プレビュー ===
            var costPreview = new UIObject
            {
                Name = "ForgeCostPreview",
                PosX = 90,
                PosY = 8 + 18, // 所持金の下
                ZIndex = 30000,
                Text = "-0",
                TextAlign = "right",
                FontSize = 14,
                TextColor = "#FF6666",
            };
            scene.AddUI(costPreview);

            // === 烈火の初期装備武器を自動選択 ===
            var rekka = GameMain.Instance.PlayerParty.FirstOrDefault(c => c.Name == "烈火");
            if (rekka != null)
            {
                string? weaponId = EquipmentManager.Instance.GetEquippedId(rekka, "武器");
                if (!string.IsNullOrEmpty(weaponId))
                {
                    var eq = EquipmentManager.Instance.Get(weaponId);
                    if (eq != null)
                        UpdateSelectedWeaponUI(scene, eq); // ← 統一呼び出し
                }
            }

            return scene;
        }

        /// <summary>
        /// 選択中武器のUI更新を一括管理
        /// </summary>
        private void UpdateSelectedWeaponUI(Scene scene, Equipment eq)
        {
            // === 前回選択武器のスキルUIを削除 ===
            if (_selectedEquipment != null)
                scene.ClearSkillUI(_selectedEquipment.Id);

            _selectedEquipment = eq;

            // --- 詳細テキスト更新 ---
            if (scene.TryGetUI("ForgeWeaponDetail", out var uiDetail) && uiDetail is UIObjectMultilineText txt)
            {
                txt.Text = $"【{eq.Id}】\n{eq.Description}\n" +
                           $" 傾向: {eq.Trend}\n" +
                           $" 穿×{eq.HandsThrust} 迅×{eq.HandsSlash} 剛×{eq.HandsDown}";
                txt.MarkDirty();
            }

            // --- ゲージとラベル ---
            if (scene.TryGetUI("ForgeBarWeight", out var wui) && wui is UIObjectGauge wg)
            {
                wg.Value = eq.ForgeWeight;
                if (scene.TryGetUI("ForgeBarWeightLabel", out var lw) && lw is UIObject lblW)
                {
                    lblW.Text = eq.ForgeWeight.ToString("+#;-#;0");
                    lblW.MarkDirty();
                }
            }

            if (scene.TryGetUI("ForgeBarSharp", out var sui) && sui is UIObjectGauge sg)
            {
                sg.Value = eq.ForgeSharp;
                if (scene.TryGetUI("ForgeBarSharpLabel", out var ls) && ls is UIObject lblS)
                {
                    lblS.Text = eq.ForgeSharp.ToString("+#;-#;0");
                    lblS.MarkDirty();
                }
            }

            // --- 拡大アイコン、素材、費用 ---
            UpdateEnlargedIcon(scene, eq);
            UpdateForgeMaterialIcons(scene, eq);
            UpdateAllMaterialCounts(scene, eq);
            UpdateForgePreview(scene, "", eq);
            UpdateWeaponBackground(scene, eq);

            // --- 選択中マーカー ---
            if (_selectedMarker != null)
                scene.RemoveUI(_selectedMarker);

            if (scene.TryGetUI($"ForgeWeaponIcon_{eq.Id}", out var icon))
            {
                _selectedMarker = CreateMarker("SelectedMarker", "images/ui04-00.png",
                    64 * 5, 64 * 2, icon.PosX, icon.PosY);
                scene.AddUI(_selectedMarker);
            }

            // --- 武器特性グラフ ---
            scene.CreateWeaponPolygonGraph(eq, 80, 230, 60);

            // === 選択武器スキルリスト ===
            scene.ShowCharacterSkills(eq, 160, 130, 2);
         }

        /// <summary>
        /// 武器アイコン群を生成
        /// </summary>
        private void CreateWeaponIcons(Scene scene, float listBaseY)
        {
            int colMax = 6;
            float iconSize = 48f;
            float paddingX = 40f;
            float paddingY = 40f;

            int index = 0;
            foreach (var eq in EquipmentManager.Instance.All.Where(e => e.Category == "武器"))
            {
                int col = index % colMax;
                int row = index / colMax;

                float posX = paddingX + col * iconSize;
                float posY = paddingY + row * iconSize;

                // 背景スプライトを追加
                var (sheet, rect) = scene.GetWeaponBackgroundSprite(eq);
                var bg = new UIObject
                {
                    Name = $"ForgeWeaponBg_{eq.Id}",
                    PosX = posX - 6,   // ちょっとだけ広めに
                    PosY = listBaseY + posY - 6,
                    ZIndex = 90,       // 武器アイコンより下
                    Opacity = 0.5f,
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        { "idle", new GameObjectAnimation {
                            Frames = new List<GameObjectAnimationFrame> {
                                new GameObjectAnimationFrame {
                                    Sprite = new Sprite(sheet, rect)
                                    {
                                        ScaleX = 0.8f,
                                        ScaleY = 0.8f
                                    }
                                }
                            }
                        }}
                    },
                    CurrentAnimationName = "idle"
                };
                scene.AddUI(bg);

                var icon = new UIObject
                {
                    Name = $"ForgeWeaponIcon_{eq.Id}",
                    PosX = posX,
                    PosY = listBaseY + posY,
                    ZIndex = 100,
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        { "idle", new GameObjectAnimation {
                            Frames = new List<GameObjectAnimationFrame> {
                                new GameObjectAnimationFrame {
                                    Sprite = new Sprite(eq.SpriteSheet,
                                        new Rectangle(eq.SrcX, eq.SrcY, 64, 64)) {
                                        ScaleX = 0.6f,
                                        ScaleY = 0.6f
                                    }
                                }
                            }
                        }}
                    },
                    CurrentAnimationName = "idle"
                };

                icon.OnClick = () => UpdateSelectedWeaponUI(scene, eq); // ← 統一呼び出し

                scene.AddUI(icon);

                // --- 装備中マーカー（烈火/沙耶）を重ねる ---
                foreach (var ch in GameMain.Instance.PlayerParty)
                {
                    string? eqId = EquipmentManager.Instance.GetEquippedId(ch, "武器");
                    if (eqId == eq.Id)
                    {
                        // 烈火は青、沙耶はオレンジのマーカーを使う
                        var marker = ch.Name == "烈火"
                            ? CreateMarker("EquippedMarker_Rekka", "images/ui04-00.png",
                                           64 * 9, 64 * 4, icon.PosX, icon.PosY)
                            : CreateMarker("EquippedMarker_Saya", "images/ui04-00.png",
                                           64 * 10, 64 * 4, icon.PosX, icon.PosY);

                        scene.AddUI(marker);
                    }
                }

                index++;
            }
        }

        /// <summary>
        /// 武器の拡大背景（ゆらぎ付き）
        /// </summary>
        void UpdateEnlargedIcon(Scene scene, Equipment eq)
        {
            if (scene.TryGetUI("ForgeSelectedWeaponBackground", out var ui))
            {
                ui.Animations = new Dictionary<string, GameObjectAnimation> {
                    { "idle", new GameObjectAnimation {
                        Loop = true,
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(eq.SpriteSheet,
                                    new Rectangle(eq.SrcX, eq.SrcY, 64, 64)) {
                                    ScaleX = 3.45f, ScaleY = 3.45f
                                },
                                Duration = 0.5f
                            },
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(eq.SpriteSheet,
                                    new Rectangle(eq.SrcX, eq.SrcY, 64, 64)) {
                                    ScaleX = 3.50f, ScaleY = 3.50f
                                },
                                Duration = 0.5f
                            },
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(eq.SpriteSheet,
                                    new Rectangle(eq.SrcX, eq.SrcY, 64, 64)) {
                                    ScaleX = 3.55f, ScaleY = 3.55f
                                },
                                Duration = 0.5f
                            },
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(eq.SpriteSheet,
                                    new Rectangle(eq.SrcX, eq.SrcY, 64, 64)) {
                                    ScaleX = 3.50f, ScaleY = 3.50f
                                },
                                Duration = 0.5f
                            },
                        }
                    }}
                };
                ui.CurrentAnimationName = "idle";
                ui.MarkDirty();
            }
        }

        /// <summary>
        /// 共通マーカー生成
        /// </summary>
        private UIObject CreateMarker(string name, string sheet, int srcX, int srcY, float x, float y)
        {
            return new UIObject
            {
                Name = name + "_" + Guid.NewGuid(),
                PosX = x - 5,
                PosY = y - 5,
                ZIndex = 200, // アイコンより上
                Opacity = 0.8f,
                Enabled = false,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(sheet, new Rectangle(srcX, srcY, 64, 64)) {
                                    ScaleX = 0.8f,
                                    ScaleY = 0.8f
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
        }

        /// <summary>
        /// 鍛錬ボタン群を作成
        /// </summary>
        private void CreateForgeButtons(Scene scene, float baseY)
        {
            float startX = 20f;
            float startY = baseY;
            float stepX = 175f;
            float stepY = 65f;

            CreateForgeButtonWithItem(scene, "重量＋", "鎚鉄",   startX + stepX * 0, startY + stepY * 0, eq => { eq.ForgeWeight = Math.Min(10, eq.ForgeWeight + 1); });
            CreateForgeButtonWithItem(scene, "重量－", "羽鋼",   startX + stepX * 0, startY + stepY * 1, eq => { eq.ForgeWeight = Math.Max(-10, eq.ForgeWeight - 1); });
            CreateForgeButtonWithItem(scene, "鋭さ＋", "名倉砥", startX + stepX * 1, startY + stepY * 0, eq => { eq.ForgeSharp = Math.Min(10, eq.ForgeSharp + 1); });
            CreateForgeButtonWithItem(scene, "鋭さ－", "荒砥",   startX + stepX * 1, startY + stepY * 1, eq => { eq.ForgeSharp = Math.Max(-10, eq.ForgeSharp - 1); });
        }

        private void CreateForgeButtonWithItem(Scene scene, string label, string itemId, float x, float y, Action<Equipment> effect)
        {
            // ボタン本体
            var btn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = $"ForgeBtn_{label}";
                ui.PosX = x;
                ui.PosY = y;
                ui.Text = label;
                ui.FontSize = 15;
                ui.ZIndex = 20000;
                ui.OnClick = () => TryForge(scene, itemId, effect);
            });
            scene.AddUI(btn);

            // アイコン
            var item = ItemManager.Instance.Get(itemId);
            if (item != null)
            {
                var icon = new UIObject
                {
                    Name = $"ForgeBtnIcon_{label}",
                    PosX = x + 55,
                    PosY = y - 6,
                    ZIndex = 20001,
                    Animations = new Dictionary<string, GameObjectAnimation> {
                    { "idle", new GameObjectAnimation {
                            Frames = new List<GameObjectAnimationFrame> {
                                new GameObjectAnimationFrame {
                                    Sprite = new Sprite(item.SpriteSheet, new Rectangle(item.SrcX, item.SrcY, 64,64)) {
                                        ScaleX = 0.5f, ScaleY = 0.5f
                                    }
                                }
                            }
                        }}
                    },
                    CurrentAnimationName = "idle"
                };

                // ★長押しで詳細表示（ForgeWeight/ForgeSharpに応じて動的に素材を選択）
                icon.OnLongPressStart = () =>
                {
                    if (_selectedEquipment == null) return;

                    string actualId = itemId;
                    var eq = _selectedEquipment;

                    if (itemId == "鎚鉄" && eq.ForgeWeight >= +5) actualId = "玄鉄";
                    if (itemId == "羽鋼" && eq.ForgeWeight <= -5) actualId = "天羽布";
                    if (itemId == "名倉砥" && eq.ForgeSharp >= +5) actualId = "神砥";
                    if (itemId == "荒砥" && eq.ForgeSharp <= -5) actualId = "鬼砥";

                    var actualItem = ItemManager.Instance.Get(actualId);
                    if (actualItem != null)
                        ItemPopupHelper.Show(scene, actualItem);
                };
                icon.OnLongPressRelease = () => ItemPopupHelper.Close(scene);

                scene.AddUI(icon);

                // 所持数表示
                var text = new UIObject
                {
                    Name = $"ForgeBtnText_{label}",
                    PosX = x + 90,
                    PosY = y + 5,
                    FontSize = 12,
                    ZIndex = 20002,
                    TextColor = "#FFFFFF",
                    Text = $"×{item.Count}"
                };
                scene.AddUI(text);
            }
        }

        /// <summary>
        /// 素材消費と武器更新
        /// </summary>
        private async void TryForge(Scene scene, string itemId, Action<Equipment> applyEffect)
        {
            if (_selectedEquipment == null)
            {
                scene.ShowTelopAsync("武器を選択してください");
                return;
            }

            var eq = _selectedEquipment;
            var item = ItemManager.Instance.Get(itemId);
            if (item == null) return;

            // ★必要素材判定（上位素材チェック）
            string useItemId = itemId;
            if (eq.ForgeWeight >= +5 && itemId == "鎚鉄") useItemId = "玄鉄";
            if (eq.ForgeWeight <= -5 && itemId == "羽鋼") useItemId = "天羽布";
            if (eq.ForgeSharp >= +5 && itemId == "名倉砥") useItemId = "神砥";
            if (eq.ForgeSharp <= -5 && itemId == "荒砥") useItemId = "鬼砥";

            int haveCount = ItemManager.Instance.GetCount(useItemId);
            int cost = CalcForgeCost(eq);
            int haveMoney = GameMain.Instance.PlayerMoney;

            // ★素材・資金チェック
            if (haveCount < 1)
            {
                scene.ShowErrorQuickAsync($"{useItemId}が足りません");
                UpdateForgePreview(scene, useItemId, eq);
                return;
            }
            if (haveMoney < cost)
            {
                scene.ShowErrorQuickAsync("資金が足りません");
                UpdateForgePreview(scene, useItemId, eq);
                return;
            }

            // 消費・適用前に上限チェック
            bool canForge = true;
            switch (itemId)
            {
                case "鎚鉄":
                case "玄鉄":
                    if (eq.ForgeWeight >= 10) canForge = false;
                    break;
                case "羽鋼":
                case "天羽布":
                    if (eq.ForgeWeight <= -10) canForge = false;
                    break;
                case "名倉砥":
                case "神砥":
                    if (eq.ForgeSharp >= 10) canForge = false;
                    break;
                case "荒砥":
                case "鬼砥":
                    if (eq.ForgeSharp <= -10) canForge = false;
                    break;
            }

            if (!canForge)
            {
                scene.ShowErrorQuickAsync("これ以上強化できません");
                return;
            }

            // === 先に素材＆資金を減らす ===
            ItemManager.Instance.Consume(useItemId, 1);
            GameMain.Instance.SubtractMoney(cost, true);

            // 下位素材の残数を即時反映
            UpdateAllMaterialCounts(scene, eq); // ←この時点ではForge値は旧値
            UpdateForgePreview(scene, "", eq);

            // 最後に演出追加
            await PlayForgeEffectAsync(scene);

            // ✅ 一旦ここで1フレーム待って下位素材の減少を見せる
            await Task.Yield();
            await Task.Delay(250); // 0.25秒だけ間を置く

            // 🚨ここで武器が切り替わっていたら中止
            if (_selectedEquipment != eq)
                return;  // 現在の選択武器が違う → この処理を破棄

            // === Forge値を上げる（ここで上位素材へ移行） ===
            applyEffect(eq);

            // ✅ 追加: スキル再構築
            SkillHandler.ApplyForgeLevelChange(eq);

            // === 一括更新 ===
            UpdateSelectedWeaponUI(scene, eq);

            if (scene.TryGetUI("ForgeBarWeight", out var wui) && wui is UIObjectGauge wg)
            {
                wg.Value = eq.ForgeWeight;
                if (scene.TryGetUI("ForgeBarWeightLabel", out var lw) && lw is UIObject lblW)
                {
                    lblW.Text = eq.ForgeWeight.ToString("+#;-#;0"); // ±付きで表示
                    lblW.MarkDirty();
                }
            }

            if (scene.TryGetUI("ForgeBarSharp", out var sui) && sui is UIObjectGauge sg)
            {
                sg.Value = eq.ForgeSharp;
                if (scene.TryGetUI("ForgeBarSharpLabel", out var ls) && ls is UIObject lblS)
                {
                    lblS.Text = eq.ForgeSharp.ToString("+#;-#;0");
                    lblS.MarkDirty();
                }
            }

            UpdateWeaponBackground(scene, eq);
            // 素材消費後に全素材残数・費用を再描画
            UpdateForgeMaterialIcons(scene, eq);
            UpdateAllMaterialCounts(scene, eq);
            UpdateForgePreview(scene, "", eq); // 費用プレビュー再更新

        }

        /// <summary>
        /// 武器背景（傾向＋重量段階）を更新
        /// </summary>
        private void UpdateWeaponBackground(Scene scene, Equipment eq)
        {
            if (scene.TryGetUI($"ForgeWeaponBg_{eq.Id}", out var ui))
            {
                var (sheet, rect) = scene.GetWeaponBackgroundSprite(eq);
                ui.Animations = new Dictionary<string, GameObjectAnimation> {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(sheet, rect)
                                {
                                    ScaleX = 0.8f,
                                    ScaleY = 0.8f
                                }
                            }
                        }
                    }}
                };
                ui.CurrentAnimationName = "idle";
                ui.MarkDirty();
            }
        }

        /// <summary>
        /// 鍛錬費用を計算（重量・鋭さ合計に応じて）
        /// </summary>
        private int CalcForgeCost(Equipment eq)
        {
            int total = Math.Abs(eq.ForgeWeight) + Math.Abs(eq.ForgeSharp);
            return 100 + total * 50; // 例：固定100 + 段階ごとに50
        }

        /// <summary>
        /// 素材数・費用プレビューのUIを更新
        /// </summary>
        private void UpdateForgePreview(Scene scene, string itemId, Equipment eq)
        {
            // === 費用プレビュー更新 ===
            int cost = CalcForgeCost(eq);
            if (scene.TryGetUI("ForgeCostPreview", out var costUi))
            {
                costUi.Text = $"-{cost}";
                costUi.TextColor = GameMain.Instance.PlayerMoney < cost ? "#FF4444" : "#FF6666";
                costUi.MarkDirty();
            }

            // === 所持金UI更新 ===
            if (scene.TryGetUI("GlobalMoneyText", out var moneyUi))
            {
                moneyUi.TextColor = GameMain.Instance.PlayerMoney < cost ? "#FF4444" : "#FFFFFF";
                moneyUi.MarkDirty();
            }
        }

        /// <summary>
        /// 素材アイコンをForgeWeight/ForgeSharpに応じて差し替える
        /// </summary>
        private void UpdateForgeMaterialIcons(Scene scene, Equipment eq)
        {
            var replacements = new Dictionary<string, string>
            {
                ["ForgeBtnIcon_重量＋"] = eq.ForgeWeight >= +5 ? "玄鉄" : "鎚鉄",
                ["ForgeBtnIcon_重量－"] = eq.ForgeWeight <= -5 ? "天羽布" : "羽鋼",
                ["ForgeBtnIcon_鋭さ＋"] = eq.ForgeSharp >= +5 ? "神砥" : "名倉砥",
                ["ForgeBtnIcon_鋭さ－"] = eq.ForgeSharp <= -5 ? "鬼砥" : "荒砥"
            };

            foreach (var kv in replacements)
            {
                if (scene.TryGetUI(kv.Key, out var ui))
                {
                    var item = ItemManager.Instance.Get(kv.Value);
                    if (item == null) continue;

                    ui.Animations = new Dictionary<string, GameObjectAnimation> {
                        { "idle", new GameObjectAnimation {
                            Frames = new List<GameObjectAnimationFrame> {
                                new GameObjectAnimationFrame {
                                    Sprite = new Sprite(item.SpriteSheet,
                                        new Rectangle(item.SrcX, item.SrcY, 64, 64)) {
                                        ScaleX = 0.5f,
                                        ScaleY = 0.5f
                                    }
                                }
                            }
                        }}
                    };
                    ui.CurrentAnimationName = "idle";
                    ui.MarkDirty();
                }
            }
        }

        /// <summary>
        /// 現在の ForgeWeight/ForgeSharp に基づいて、全ボタンの所持素材数を最新反映
        /// </summary>
        private void UpdateAllMaterialCounts(Scene scene, Equipment eq)
        {
            var mapping = new Dictionary<string, string>
            {
                ["重量＋"] = eq.ForgeWeight >= +5 ? "玄鉄" : "鎚鉄",
                ["重量－"] = eq.ForgeWeight <= -5 ? "天羽布" : "羽鋼",
                ["鋭さ＋"] = eq.ForgeSharp >= +5 ? "神砥" : "名倉砥",
                ["鋭さ－"] = eq.ForgeSharp <= -5 ? "鬼砥" : "荒砥"
            };

            foreach (var kv in mapping)
            {
                string label = kv.Key;
                string material = kv.Value;
                int have = ItemManager.Instance.GetCount(material);
                int need = 1;

                if (scene.TryGetUI($"ForgeBtnText_{label}", out var ui) && ui is UIObject lbl)
                {
                    lbl.Text = $"×{have}";
                    lbl.TextColor = have < need ? "#FF4444" : "#FFFFFF";
                    lbl.MarkDirty();
                }
            }
        }

        /// <summary>
        /// 鍛冶演出（鍛冶師シルエットの一瞬フラッシュ）
        /// </summary>
        private async Task PlayForgeEffectAsync(Scene scene)
        {
            var flash = new UIObject
            {
                Name = "ForgeFlash",
                PosX = 0,
                PosY = 0,
                ZIndex = 999999,
                Opacity = 0,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                // 鍛冶師のシルエット画像
                                Sprite = new Sprite("images/ui06-04.png",
                                    new Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                            }
                        }
                    }}
                }
            };
            scene.AddUI(flash);

            flash.Opacity = 0.5f; // フラッシュ表示
            flash.MarkDirty();
            await Task.Delay(80);

            flash.Opacity = 0f;
            flash.MarkDirty();
            scene.RemoveUI(flash);
        }
    }
}
