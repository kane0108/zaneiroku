using BlazorApp.Game.SceneFactory;
using BlazorApp.Game.Training;
using BlazorApp.Game.UIObjectFactory;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Xml.Linq;

namespace BlazorApp.Game
{
    /// <summary>
    /// ステータス画面シーン
    /// </summary>
    public class SceneStatus : BaseSceneFactory
    {
        public override GameState TargetState => GameState.Status;

        // ★ 現在のタブ状態を保持
        private string _currentTab = "修練";

        // 直前にプレビュー中の修練名
        private string? _previewTraining = null;

        private UIObject? _trainingCursor;

        private bool _isTrainingExecuting = false;

        // フィールド追加
        private Equipment? _selectedEquipCandidate;
        private string? _selectedCategory;
        private Character? _selectedCharacter;

        private Dictionary<string, string?> _previewEquip = new(); // Key: $"{キャラ名}_{カテゴリ}", Value: 装備ID or null

        public override Scene Create(object? payload = null)
        {
            var scene = new Scene
            {
                State = GameState.Status
            };

            // === 背景 ===
            scene.Background = new Background
            {
                Layers = new List<BackgroundLayer>
                {
                    new BackgroundLayer
                    {
                        Sprite = new Sprite("images/bg012.png",   // ★専用背景を用意
                            new System.Drawing.Rectangle(0,0,360,640)),
                        LoopScroll = false
                    }
                }
            };

            // 共通：経験値描画
            scene.CreateGlobalExpUI();

            // 共通・所持金描画
            scene.CreateGlobalMoneyUI();

            // タブボタンを追加
            AddTabButtons(scene);

            _isTrainingExecuting = false;

            _currentTab = "修練";
            RefreshTabContents(scene);
            UpdateTabHighlight(scene);
            CreateTrainingCursor(scene);

            // 共通部分（立ち絵・ステータス表示など）
            AddCommonStatusUI(scene);

            // 初期タブを表示
            RefreshTabContents(scene);

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

            _ = CheckAndShowTrainingTutorialAsync(scene);

            return scene;
        }

        /// <summary>
        /// 修練チュートリアルを初回のみ表示
        /// </summary>
        private async Task CheckAndShowTrainingTutorialAsync(Scene scene)
        {
            // すでに完了済みならスキップ
            if (GameSwitchManager.Instance.IsOn(Common.GameSwitch.TrainingTutorial))
                return;

            // === チュートリアルTipsを表示 ===
            await scene.ShowTutorialTipsAsync(new[] {
                "こでは、二人の能力を確認することができる。\n"+
                "能力値には、体力・攻撃・防御・敏捷\n"+
                "そして、洞察・翻弄の六つがある。",
                "【体力】傷を受けても立ち続けられる力。\n"+
                "【攻撃】刃を振るったときの威力。\n"+
                "【防御】受けた斬撃や衝撃を和らげる力。\n"+
                "【敏捷】身のこなし。高いほど行動順が早く回る。",
                "【洞察】相手の動きを読み取る力。\n"+
                "【翻弄】自らの手の内を悟らせぬ技。\n"+
                "　※相対する者の洞察と翻弄の差が、どれだけ\n" +
                "    手の内を見抜けるか、隠せるかに関わる。"
            }, 13, offsetY: 50);

            await scene.ShowTutorialTipsAsync(new[] {
                "【基礎修練】では、任務で得た経験を使って\n" +
                "二人で修行を行い、お互いの能力を鍛える。\n" +
                "能力が高まるほど必要な経験は多くなるため、\n"+
                "偏り無く修練を積むことを勧める。"
            }, 14, offsetY: 50);

            // === スイッチON ===
            GameSwitchManager.Instance.SetOn(Common.GameSwitch.TrainingTutorial);
        }

        /// <summary>
        /// 装備チュートリアルを初回のみ表示
        /// </summary>
        private async Task CheckAndShowEquipmentTutorialAsync(Scene scene)
        {
            // すでに完了済みならスキップ
            if (GameSwitchManager.Instance.IsOn(Common.GameSwitch.EquipmentTutorial))
                return;

            // === チュートリアルTipsを表示 ===
            await scene.ShowTutorialTipsAsync(new[] {
                "ここでは、携える刀を選ぶことができる。\n"+
                "刀ごとに得意とする斬り筋が異なる。\n"+
                "迅に長けた刀で残痕を刻み、もう一方では\n"+
                "穿に長けた刀で致命の一撃を狙う、など、\n"+
                "長所を見極め使い分ける。",
                "尚、刀には「攻撃力」という概念は無く、\n"+
                "あるのは斬り筋など特性の違いだけである。"
            }, 15, offsetY: -100);

            await scene.ShowTutorialTipsAsync(new[] {
                "次に、任務に携える忍具を選ぶ。\n"+
                "忍具は任務に一つだけ持参でき、戦いの\n"+
                "局面を変える切り札となる。\n" +
                "任務から帰還すれば自動的に補充される。",
                "尚、何も持たぬことを選べば、その分だけ\n"+
                "身軽となり、敏捷が僅かに上昇する。"
            }, 16, offsetY: -100);

            // === スイッチON ===
            GameSwitchManager.Instance.SetOn(Common.GameSwitch.EquipmentTutorial);
        }

        /// <summary>
        /// タブボタン追加
        /// </summary>
        private void AddTabButtons(Scene scene)
        {
            // 修練タブ
            var btnTraining = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "Tab_Training";
                ui.PosX = 40f;
                ui.PosY = 30f;
                ui.Text = "　修　練　";
                ui.FontSize = 24;
                ui.OnClick = () =>
                {
                    _currentTab = "修練";
                    RefreshTabContents(scene);
                    UpdateTabHighlight(scene);
                    CreateTrainingCursor(scene);
                };
            });
            scene.AddUI(btnTraining);

            // 装備タブ
            var btnEquip = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "Tab_Equip";
                ui.PosX = 190f;
                ui.PosY = 30f;
                ui.Text = "　装　備　";
                ui.FontSize = 24;
                ui.OnClick = () =>
                {
                    if (_isTrainingExecuting) return; // ★ 実行中は無視

                    _currentTab = "装備";
                    RefreshTabContents(scene);
                    UpdateTabHighlight(scene);
                    ClearPreview(scene);
                };
            });
            scene.AddUI(btnEquip);
        }

        /// <summary>
        /// タブボタン更新
        /// </summary>
        /// <param name="scene"></param>
        private void UpdateTabHighlight(Scene scene)
        {
            if (scene.TryGetUI("Tab_Training", out var trainingBtn))
            {
                trainingBtn.TextColor = _currentTab == "修練" ? "#FFFF66" : "#FFFFFF"; // 選択中は黄色
                trainingBtn.Opacity = _currentTab == "修練" ? 1.0f : 0.6f;
                trainingBtn.MarkDirty();
            }

            if (scene.TryGetUI("Tab_Equip", out var equipBtn))
            {
                equipBtn.TextColor = _currentTab == "装備" ? "#FFFF66" : "#FFFFFF";
                equipBtn.Opacity = _currentTab == "装備" ? 1.0f : 0.6f;
                equipBtn.MarkDirty();
            }
        }

        /// <summary>
        /// 初期UIを作成する（Create専用）
        /// </summary>
        private void AddCommonStatusUI(Scene scene)
        {
            var party = GameMain.Instance.PlayerParty;

            // === 主人公の立ち絵 ===
            var portraitLeft = new UIObject
            {
                Name = "PortraitLeft",
                PosX = 0f,
                PosY = 55f,
                Opacity = 0.4f, // 半透明
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(party[0].PortraitImagePath,
                                    new System.Drawing.Rectangle(party[0].PortraitId*180,party[0].CurrentExpressionId*180,180,180)) {
                                    ScaleX = 1.0f, ScaleY = 1.0f
                                }
                            }
                        }
                    }}
                }
            };
            scene.AddUI(portraitLeft);

            // === 相棒の立ち絵 ===
            var portraitRight = new UIObject
            {
                Name = "PortraitRight",
                PosX = 200f,
                PosY = 55f,
                Opacity = 0.4f, // 半透明
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(party[1].PortraitImagePath,
                                    new System.Drawing.Rectangle(party[1].PortraitId*180,party[1].CurrentExpressionId*180,180,180)) {
                                    ScaleX = 1.0f, ScaleY = 1.0f
                                }
                            }
                        }
                    }}
                }
            };
            scene.AddUI(portraitRight);

            string[] labels = { "体力", "攻撃", "防御", "敏捷", "洞察", "翻弄" };
            float startY = 75f;
            float stepY = 30f;

            for (int i = 0; i < labels.Length; i++)
            {
                float y = startY + i * stepY;
                string statName = labels[i];

                // ステータス名
                var label = new UIObject {
                    Name = $"Label_{labels[i]}",
                    CenterX = true,
                    PosY = y,
                    Text = labels[i],
                    TextAlign = "center",
                    FontSize = 20,
                    TextColor = "#CCCCCC",
                    ZIndex = 10000
                };
                scene.AddUI(label);

                // 左側の数値（初期は0）
                var leftValue = new UIObject
                {
                    Name = $"ValueLeft_{statName}",
                    PosX = Common.CanvasWidth / 2 - 70,
                    PosY = y + 3,
                    Text = "0",
                    FontSize = 16,
                    TextColor = "#FFFFFF",
                    ZIndex = 10000,
                    TextAlign = "center"
                };
                scene.AddUI(leftValue);

                // 右側の数値（初期は0）
                var rightValue = new UIObject
                {
                    Name = $"ValueRight_{statName}",
                    PosX = Common.CanvasWidth / 2 + 70,
                    PosY = y + 3,
                    Text = "0",
                    FontSize = 16,
                    TextColor = "#FFFFFF",
                    ZIndex = 10000,
                    TextAlign = "center"
                };
                scene.AddUI(rightValue);
            }

            // 主人公ポリゴン
            var polyLeft = new UIObjectPolygon
            {
                Name = "PolygonLeft",
                FillColor = "#66AAFF",
                FillOpacity = 0.3f,
                Points = new List<(float, float)>(),
                ZIndex = 5000
            };
            scene.AddUI(polyLeft);

            // 相棒ポリゴン
            var polyRight = new UIObjectPolygon
            {
                Name = "PolygonRight",
                FillColor = "#FFAA66",
                FillOpacity = 0.3f,
                Points = new List<(float, float)>(),
                ZIndex = 5000
            };
            scene.AddUI(polyRight);

            // ★初回更新を呼んで数値を反映
            UpdateStatusUI(scene);
        }

        /// <summary>
        /// 修練選択カーソルアイコン
        /// </summary>
        /// <param name="scene"></param>
        private void CreateTrainingCursor(Scene scene)
        {
            _trainingCursor = new UIObject
            {
                Name = "TrainingCursor",
                PosX = 0,
                PosY = 0,
                ZIndex = 10001,
                Visible = false,
                Enabled = false,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-01.png",
                                    new System.Drawing.Rectangle(360*3, 0, 360, 180)) {
                                    ScaleX = 0.27f,
                                    ScaleY = 0.1f
                                }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(_trainingCursor);
        }

        /// <summary>
        /// 数値・ポリゴンの更新専用
        /// </summary>
        private void UpdateStatusUI(Scene scene)
        {
            var party = GameMain.Instance.PlayerParty;
            string[] labels = { "体力", "攻撃", "防御", "敏捷", "洞察", "翻弄" };

            bool isEquipTab = _currentTab == "装備";

            // --- （1）装備タブ時のみ安全に最終値を再計算 ---
            if (isEquipTab)
            {
                foreach (var ch in party)
                {
                    // BaseStats から再生成（内部で全ステータスを再構築）
                    EquipmentManager.Instance.ApplyEquipmentAndSkillsToCharacter(ch);

                    // 忍具未装備ボーナスを CurrentStats に反映（Speed +5%）
                    var equippedNingu = EquipmentManager.Instance.GetEquipped(ch, "忍具");
                    bool noNingu = (equippedNingu == null);
                    ch.CurrentStats.ApplyNoNinguSpeedBonus(noNingu);
                }
            }

            // --- （2）ステータス値・色更新 ---
            for (int i = 0; i < labels.Length; i++)
            {
                string statName = labels[i];

                int leftBase = GetStatValue(party[0].BaseStats, statName);
                int rightBase = GetStatValue(party[1].BaseStats, statName);

                int leftFinal = isEquipTab ? GetStatValue(party[0].CurrentStats, statName) : leftBase;
                int rightFinal = isEquipTab ? GetStatValue(party[1].CurrentStats, statName) : rightBase;

                // --- 左キャラ（烈火） ---
                if (scene.TryGetUI($"ValueLeft_{statName}", out var leftUI))
                {
                    leftUI.Text = leftFinal.ToString();
                    leftUI.TextColor = (isEquipTab && leftFinal > leftBase) ? "#66FF66" : "#FFFFFF";
                    leftUI.MarkDirty();
                }

                // --- 右キャラ（沙耶） ---
                if (scene.TryGetUI($"ValueRight_{statName}", out var rightUI))
                {
                    rightUI.Text = rightFinal.ToString();
                    rightUI.TextColor = (isEquipTab && rightFinal > rightBase) ? "#66FF66" : "#FFFFFF";
                    rightUI.MarkDirty();
                }
            }

            // --- （3）ポリゴン更新 ---
            if (scene.TryGetUI("PolygonLeft", out var polyLeft) && polyLeft is UIObjectPolygon p1)
            {
                // ベースポリゴン（青 or 橙）
                p1.Points = CalcPolygonPoints(party[0], useSkillBonus: false);
                p1.MarkDirty();

                // 差分ポリゴン（緑）
                UpdateBonusPolygon(scene, party[0], "Left", "#66FF66", 0.4f, isEquipTab);
            }

            if (scene.TryGetUI("PolygonRight", out var polyRight) && polyRight is UIObjectPolygon p2)
            {
                p2.Points = CalcPolygonPoints(party[1], useSkillBonus: false);
                p2.MarkDirty();

                UpdateBonusPolygon(scene, party[1], "Right", "#66FF66", 0.4f, isEquipTab);
            }
        }

        /// <summary>
        /// スキルによる補正値合算（攻撃強化、防御強化、敏捷強化など）
        /// </summary>
        private int GetSkillBonus(Character ch, string statName)
        {
            int sum = 0;
            foreach (var skill in ch.Skills)
            {
                if (statName == "体力" && skill.Id.Contains("体力強化")) sum += skill.Level;
                if (statName == "攻撃" && skill.Id.Contains("攻撃強化")) sum += skill.Level;
                if (statName == "防御" && skill.Id.Contains("防御強化")) sum += skill.Level;
                if (statName == "敏捷" && skill.Id.Contains("敏捷強化")) sum += skill.Level;
                if (statName == "洞察" && skill.Id.Contains("洞察強化")) sum += skill.Level;
                if (statName == "翻弄" && skill.Id.Contains("翻弄強化")) sum += skill.Level;
            }

            // 装備スキルも考慮
            var weapon = EquipmentManager.Instance.GetEquipped(ch, "武器");
            var tool = EquipmentManager.Instance.GetEquipped(ch, "忍具");

            foreach (var eq in new[] { weapon, tool })
            {
                if (eq == null) continue;
                foreach (var skill in eq.Skills)
                {
                    if (statName == "体力" && skill.Id.Contains("体力強化")) sum += skill.Level;
                    if (statName == "攻撃" && skill.Id.Contains("攻撃強化")) sum += skill.Level;
                    if (statName == "防御" && skill.Id.Contains("防御強化")) sum += skill.Level;
                    if (statName == "敏捷" && skill.Id.Contains("敏捷強化")) sum += skill.Level;
                    if (statName == "洞察" && skill.Id.Contains("洞察強化")) sum += skill.Level;
                    if (statName == "翻弄" && skill.Id.Contains("翻弄強化")) sum += skill.Level;
                }
            }

            return sum;
        }

        /// <summary>
        /// スキル補正および忍具未装備ボーナスによる差分ポリゴン（外側だけの帯状）
        /// </summary>
        private void UpdateBonusPolygon(Scene scene, Character ch, string sideKey, string color, float opacity, bool isEquipTab)
        {
            // 既存削除
            var old = scene.UIObjects.Values
                .Where(u => u.Name.StartsWith($"PolygonBonus_{sideKey}"))
                .ToList();
            foreach (var u in old)
                scene.RemoveUI(u);

            if (!isEquipTab)
                return;

            var basePoints = CalcPolygonPoints(ch, useSkillBonus: false);
            var bonusPoints = CalcPolygonPoints(ch, useSkillBonus: true);

            bool isLeft = (ch == GameMain.Instance.PlayerParty[0]); // 烈火

            // === 敏捷倍率 ===
            float totalRatio = 1.0f;
            var speedSkill = ch.Skills.FirstOrDefault(s => s.Id == "敏捷強化");
            if (speedSkill != null)
                totalRatio *= (1.0f + 0.01f * speedSkill.Level);
            if (ch.CurrentStats.NoNinguSpeedBonusApplied)
                totalRatio *= 1.05f;

            if (Math.Abs(totalRatio - 1f) < 0.001f)
                return;

            // === 敏捷軸だけ拡張 ===
            int agilityIndex = 3;
            var bp = bonusPoints[agilityIndex];
            var cx = basePoints.Average(p => p.x);
            var cy = basePoints.Average(p => p.y);
            float dx = bp.x - cx;
            float dy = bp.y - cy;

            // 拡張倍率
            float expand = totalRatio - 1f;

            // 左右方向補正：左キャラは X 減少が外方向
            if (isLeft)
                bonusPoints[agilityIndex] = (bp.x - Math.Abs(dx) * expand, bp.y);
            else
                bonusPoints[agilityIndex] = (bp.x + Math.Abs(dx) * expand, bp.y);

            // === 差分帯を構築 ===
            var bandPoints = new List<(float x, float y)>();
            for (int i = 0; i < basePoints.Count; i++)
            {
                var b = basePoints[i];
                var s = bonusPoints[i];

                // 🔸 左右で外側方向の判定を反転
                bool increased = isLeft ? (s.x < b.x) : (s.x > b.x);
                var outer = increased ? s : b;
                bandPoints.Add(outer);
            }

            for (int i = basePoints.Count - 1; i >= 0; i--)
                bandPoints.Add(basePoints[i]);

            var poly = new UIObjectPolygon
            {
                Name = $"PolygonBonus_{sideKey}",
                Points = bandPoints,
                FillColor = color,
                FillOpacity = opacity,
                ZIndex = 5001
            };
            scene.AddUI(poly);
        }

        /// <summary>
        /// UI表示更新
        /// </summary>
        private void RefreshTabContents(Scene scene)
        {
            // ★ 既存タブUIをクリア（ステータス共通UIは残す）
            var toRemove = scene.UIObjects.Values
               .Where(ui => ui.Name.StartsWith("Training_") ||
                            ui.Name.StartsWith("Equip_") ||
                            ui.Name.StartsWith("EquipSummary_") || 
                            ui.Name.StartsWith("EquipSummaryText_") ||
                            ui.Name.StartsWith("Label_Basic") ||
                            ui.Name.StartsWith("Label_Advanced") ||
                            ui.Name.StartsWith("InIcon_") ||
                            ui.Name.StartsWith("InCount_") ||
                            ui.Name.StartsWith("Cursor_Equip") ||
                            ui.Name.StartsWith("PopupEquip_") ||
                            ui.Name == "PopupEquip_Background" ||
                            ui.Name == "PopupEquip_Blocker" ||
                            ui.Name.StartsWith("PolygonBonus_") ||
                            ui.Name.StartsWith("ForgeGraph_") ||
                            ui.Name.StartsWith("ItemPopup_"))
               .ToList();
            foreach (var ui in toRemove) scene.RemoveUI(ui);

            scene.ClearAllSkillUI();

            var party = GameMain.Instance.PlayerParty;

            if (_currentTab == "修練")
            {
                // === 基礎修練 ===
                float baseYBasic = 240;
                float offsetYBasic = 40;
                scene.AddUI(new UIObject
                {
                    Name = "Label_Basic",
                    PosX = 10f,
                    PosY = baseYBasic + 15,
                    Text = "【基礎修練】",
                    FontSize = 18,
                    TextColor = "#EEEEEE",
                    ZIndex = 10000
                });

                foreach (var training in TrainingDatabase.ByCategory("基礎"))
                {
                    float posX = training.Column switch { 0 => 30f, 1 => Common.CanvasWidth / 2 - 47, 2 => Common.CanvasWidth - 123, _ => 30f };
                    float posY = baseYBasic + offsetYBasic * (training.Row + 1);

                    AddTrainingUI(scene, training, posX, posY, party);
                }

                // === 複合修練 ===
                float baseYAdv = baseYBasic + 40;
                float offsetYAdv = 60;
                scene.AddUI(new UIObject
                {
                    Name = "Label_Advanced",
                    PosX = 10f,
                    PosY = baseYAdv + offsetYAdv + 35,
                    Text = "【複合修練】",
                    FontSize = 18,
                    TextColor = "#EEEEEE",
                    ZIndex = 10000
                });

                foreach (var training in TrainingDatabase.ByCategory("複合"))
                {
                    float posX = training.Column switch { 0 => 30f, 1 => Common.CanvasWidth / 2 - 47, 2 => Common.CanvasWidth - 123, _ => 30f };
                    float posY = baseYAdv + offsetYAdv * (training.Row + 2);

                    AddTrainingUI(scene, training, posX, posY, party,
                        tint: training.Name == "皆伝修練" ? "#7777FF" : "#FF7777");
                }
            }
            else if (_currentTab == "装備")
            {
                AddEquipSummaryUI(scene, GameMain.Instance.PlayerParty[0], true);  // 左キャラ（烈火）
                AddEquipSummaryUI(scene, GameMain.Instance.PlayerParty[1], false); // 右キャラ（沙耶）
            }

            // 数値とポリゴンを更新
            UpdateStatusUI(scene);
        }

        private int GetStatValue(CharacterStats stats, string label)
        {
            return label switch
            {
                "体力" => stats.MaxHP,
                "攻撃" => stats.Attack,
                "防御" => stats.Defense,
                "敏捷" => stats.Speed,
                "洞察" => stats.Insight,
                "翻弄" => stats.Confuse,
                _ => 0
            };
        }

        /// <summary>
        /// ステータスポリゴン
        /// </summary>
        private List<(float x, float y)> CalcPolygonPoints(Character ch, bool useSkillBonus = false)
        {
            float startY = 85f;
            float stepY = 30f;
            float maxLen = 100f;
            float minFactor = 0.1f;

            string[] labels = { "体力", "攻撃", "防御", "敏捷", "洞察", "翻弄" };
            float globalMax = GameMain.Instance.PlayerParty
                .SelectMany(c => new[] {
            c.CurrentStats.MaxHP, c.CurrentStats.Attack, c.CurrentStats.Defense,
            c.CurrentStats.Speed, c.CurrentStats.Insight, c.CurrentStats.Confuse })
                .Max();

            List<(float x, float y)> pts = new();

            for (int i = 0; i < labels.Length; i++)
            {
                float y = startY + i * stepY;
                int val = GetStatValue(ch.CurrentStats, labels[i]);
                if (useSkillBonus) val += GetSkillBonus(ch, labels[i]);

                float norm = val / globalMax;
                float normClamped = minFactor + (1 - minFactor) * norm;

                float cx = Common.CanvasWidth / 2f;
                float baseX = (ch == GameMain.Instance.PlayerParty[0]) ? cx - 30 : cx + 30;
                float x = (ch == GameMain.Instance.PlayerParty[0])
                    ? baseX - (normClamped * maxLen)
                    : baseX + (normClamped * maxLen);

                pts.Add((x, y));
            }

            for (int i = labels.Length - 1; i >= 0; i--)
            {
                float y = startY + i * stepY;
                float cx = Common.CanvasWidth / 2f;
                float baseX = (ch == GameMain.Instance.PlayerParty[0]) ? cx - 30 : cx + 30;
                pts.Add((baseX, y));
            }

            return pts;
        }

        /// <summary>
        /// 修練UIを追加
        /// </summary>
        private void AddTrainingUI(Scene scene, TrainingDefinition training, float posX, float posY, List<Character> party, string? tint = null)
        {
            string key = $"{training.Category}_{training.Name}"; // ★カテゴリ付きでユニーク化

            int cost = training.CalcExpCost(party[0], party[1]);
            bool hasEnoughExp = GameMain.Instance.PlayerExp >= cost;
            bool hasRequiredItem = string.IsNullOrEmpty(training.RequiredItem) ||
                                   ItemManager.Instance.GetCount(training.RequiredItem) > 0;
            bool canExecute = hasEnoughExp && hasRequiredItem;

            // --- ボタン ---
            var btn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = $"Training_{key}";
                ui.PosX = posX;
                ui.PosY = posY;
                ui.Text = training.Name;
                ui.FontSize = 20;
                ui.TextColor = canExecute ? "#FFFFFF" : "#666666";
                if (tint != null) ui.TintColor = tint;
                ui.ZIndex = 10000;

                ui.OnClick = async () =>
                {
                    if (_isTrainingExecuting) return; // ★ 実行中は無視

                    // 実行開始
                    _isTrainingExecuting = true;

                    if (_previewTraining == key)
                    {
                        int execCost = training.CalcExpCost(party[0], party[1]);
                        if (GameMain.Instance.PlayerExp < execCost)
                        {
                            await scene.ShowErrorQuickAsync("経験値不足");
                            // 演出終了
                            _isTrainingExecuting = false;
                            return;
                        }
                        if (!string.IsNullOrEmpty(training.RequiredItem) &&
                            ItemManager.Instance.GetCount(training.RequiredItem) <= 0)
                        {
                            await scene.ShowErrorQuickAsync("印が足りない");
                            // 演出終了
                            _isTrainingExecuting = false;
                            return;
                        }

                        // 消費処理
                        GameMain.Instance.PlayerExp -= execCost;
                        scene.UpdateGlobalExpUI();
                        if (!string.IsNullOrEmpty(training.RequiredItem))
                            ItemManager.Instance.Consume(training.RequiredItem, 1);

                        // 効果反映
                        training.ApplyEffect(party[0], party[1]);

                        // ★ 修練は恒久育成なので BaseStats に加算済み。
                        //    ここで CurrentStats を再同期して UI に即反映させる。
                        foreach (var ch in party)
                        {
                            ch.CurrentStats = ch.BaseStats.Clone();
                            ch.CurrentStats.ResetHands(); // 手数リセット
                            ch.CurrentStats.ResetHP();    // HPもリセットしたい場合は残す
                        }

                        // 数値とポリゴンを更新
                        UpdateStatusUI(scene);

                        // 演出実行
                        await PlayTrainingEffectAsync(scene, training, party, execCost);

                        ClearPreview(scene);
                        _previewTraining = key;
                        ShowPreview(scene, training, party);
                        HighlightTrainingButton(scene, key);

                        UpdateTrainingIcon(scene, training, key);
                    }
                    else
                    {
                        ClearPreview(scene);
                        _previewTraining = key;
                        ShowPreview(scene, training, party);
                        HighlightTrainingButton(scene, key);
                    }

                    // 演出終了
                    _isTrainingExecuting = false;
                };
            });
            scene.AddUI(btn);

            // --- 印アイコン ---
            if (!string.IsNullOrEmpty(training.RequiredItem))
            {
                var icon = new UIObject
                {
                    Name = $"InIcon_{key}",
                    PosX = posX + 4,
                    PosY = posY + 32,
                    ZIndex = 10000,
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        { "idle", new GameObjectAnimation {
                            Frames = new List<GameObjectAnimationFrame> {
                                new GameObjectAnimationFrame {
                                    Sprite = new Sprite("images/ui04-00.png",
                                        new System.Drawing.Rectangle(training.IconSheetX, training.IconSheetY, 64, 64)) {
                                        ScaleX = 0.4f, ScaleY = 0.4f
                                    }
                                }
                            }
                        }}
                    },
                    CurrentAnimationName = "idle"
                };
                scene.AddUI(icon);

                int count = ItemManager.Instance.GetCount(training.RequiredItem);
                var countText = new UIObject
                {
                    Name = $"InCount_{key}",
                    PosX = posX + 30,
                    PosY = posY + 38,
                    Text = $"×{count}",
                    FontSize = 14,
                    TextColor = count <= 0 ? "#FF4444" : "#FFFFFF",
                    ZIndex = 10001
                };
                scene.AddUI(countText);
            }
        }

        /// <summary>
        /// 修練プレビューを表示
        /// </summary>
        private void ShowPreview(Scene scene, TrainingDefinition training, List<Character> party)
        {
            // --- ステータス上昇プレビュー ---
            var deltas = training.PreviewEffect?.Invoke(party[0], party[1]);
            if (deltas != null)
            {
                foreach (var kv in deltas)
                {
                    string statName = kv.Key;
                    int leftDelta = kv.Value.left;
                    int rightDelta = kv.Value.right;

                    if (leftDelta != 0 && scene.TryGetUI($"ValueLeft_{statName}", out var leftUI))
                    {
                        var preview = new UIObject
                        {
                            Name = $"PreviewLeft_{statName}",
                            PosX = leftUI.PosX - 50, // 数値の左に配置
                            PosY = leftUI.PosY + 1,
                            Text = $"{leftDelta} +",
                            TextAlign = "right",
                            FontSize = 14,
                            TextColor = "#66CCFF",
                            ZIndex = leftUI.ZIndex + 1
                        };
                        scene.AddUI(preview);
                    }

                    if (rightDelta != 0 && scene.TryGetUI($"ValueRight_{statName}", out var rightUI))
                    {
                        var preview = new UIObject
                        {
                            Name = $"PreviewRight_{statName}",
                            PosX = rightUI.PosX + 50, // 数値の右に配置
                            PosY = rightUI.PosY + 1,
                            Text = $"+ {rightDelta}",
                            FontSize = 14,
                            TextColor = "#66CCFF",
                            ZIndex = rightUI.ZIndex + 1
                        };
                        scene.AddUI(preview);
                    }
                }
            }

            // --- 経験値消費プレビュー ---
            int cost = training.CalcExpCost(party[0], party[1]);
            if (scene.TryGetUI("GlobalExpText", out var expUI))
            {
                var previewExp = new UIObject
                {
                    Name = "PreviewExp",
                    PosX = expUI.PosX,
                    PosY = expUI.PosY + 18,
                    Text = $"-{cost}",
                    TextAlign = "right",
                    FontSize = 14,
                    TextColor = "#FF4444",
                    ZIndex = expUI.ZIndex + 1
                };
                scene.AddUI(previewExp);
            }
        }

        /// <summary>
        /// プレビュ削除
        /// </summary>
        private void ClearPreview(Scene scene)
        {
            var toRemove = scene.UIObjects.Values
                .Where(ui => ui.Name.StartsWith("Preview"))
                .ToList();
            foreach (var ui in toRemove) scene.RemoveUI(ui);

            if (_trainingCursor != null)
            {
                _trainingCursor.Visible = false;
                _trainingCursor.MarkDirty();
            }

            _previewTraining = null;

            if (_currentTab == "装備")
            {
                _ = CheckAndShowEquipmentTutorialAsync(scene);
            }
        }

        /// <summary>
        /// プレビュー中のアイコン強調
        /// </summary>
        private void HighlightTrainingButton(Scene scene, string key)
        {
            if (_trainingCursor == null) return;

            if (scene.TryGetUI($"Training_{key}", out var btn))
            {
                _trainingCursor.PosX = btn.PosX;
                _trainingCursor.PosY = btn.PosY + 12;
                _trainingCursor.Visible = true;
                _trainingCursor.MarkDirty();
            }
        }

        /// <summary>
        /// /修練表示更新
        /// </summary>
        private void UpdateTrainingIcon(Scene scene, TrainingDefinition training, string key)
        {
            if (string.IsNullOrEmpty(training.RequiredItem)) return;

            int count = ItemManager.Instance.GetCount(training.RequiredItem);

            if (scene.TryGetUI($"InCount_{key}", out var ui))
            {
                ui.Text = $"×{count}";
                ui.TextColor = count <= 0 ? "#FF4444" : "#FFFFFF";
                ui.MarkDirty();
            }

            if (scene.TryGetUI($"Training_{key}", out var btn))
            {
                int cost = training.CalcExpCost(GameMain.Instance.PlayerParty[0], GameMain.Instance.PlayerParty[1]);
                bool canExecute = GameMain.Instance.PlayerExp >= cost && count > 0;
                btn.TextColor = canExecute ? "#FFFFFF" : "#666666";
                btn.MarkDirty();
            }
        }

        /// <summary>
        /// 修練実行演出
        /// </summary>
        private async Task PlayTrainingEffectAsync(Scene scene, TrainingDefinition training, List<Character> party, int cost)
        {
            // === フラッシュ画像 ===
            var flash = new UIObject
            {
                Name = "TrainingFlash",
                PosX = 0,
                PosY = 0,
                ZIndex = 999999,
                Opacity = 0,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui06-03.png",
                                    new System.Drawing.Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                            }
                        }
                    }}
                }
            };
            scene.AddUI(flash);

            // フラッシュ演出
            flash.Opacity = 0.5f; flash.MarkDirty();
            await Task.Delay(80);
            flash.Opacity = 0f; flash.MarkDirty();
            scene.RemoveUI(flash);

            // === 経験値減少演出 ===
            scene.ShowExpChange(-cost);

            // === ステータス値を一時青色に ===
            var deltas = training.PreviewEffect?.Invoke(party[0], party[1]);
            if (deltas != null)
            {
                foreach (var kv in deltas)
                {
                    if (scene.TryGetUI($"ValueLeft_{kv.Key}", out var leftUI))
                    {
                        leftUI.TextColor = "#66CCFF"; leftUI.MarkDirty();
                    }
                    if (scene.TryGetUI($"ValueRight_{kv.Key}", out var rightUI))
                    {
                        rightUI.TextColor = "#66CCFF"; rightUI.MarkDirty();
                    }
                }

                await Task.Delay(400);

                foreach (var kv in deltas)
                {
                    if (scene.TryGetUI($"ValueLeft_{kv.Key}", out var leftUI))
                    {
                        leftUI.TextColor = "#FFFFFF"; leftUI.MarkDirty();
                    }
                    if (scene.TryGetUI($"ValueRight_{kv.Key}", out var rightUI))
                    {
                        rightUI.TextColor = "#FFFFFF"; rightUI.MarkDirty();
                    }
                }
            }
        }

        /// <summary>
        /// キャラごとの装備サマリーUIを追加（左キャラ/右キャラ）
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="ch">対象キャラ</param>
        /// <param name="isLeft">左キャラかどうか</param>
        /// <param name="baseY">開始位置Y</param>
        private void AddEquipSummaryUI(Scene scene, Character ch, bool isLeft)
        {
            float baseX = isLeft ? 30f : 190f;
            float baseY = 480f;

            // === 武器 ===
            string? weaponId = EquipmentManager.Instance.GetEquippedId(ch, "武器");
            var weapon = weaponId != null ? EquipmentManager.Instance.Get(weaponId) : null;

            var weaponIcon = new UIObject
            {
                Name = $"EquipSummary_{ch.Name}_武器",
                PosX = baseX - 10,
                PosY = baseY,
                ZIndex = 1000,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = weapon != null
                                    ? new Sprite(weapon.SpriteSheet, new Rectangle(weapon.SrcX, weapon.SrcY,64,64)) { ScaleX=0.6f, ScaleY=0.6f }
                                    : new Sprite("images/ui04-00.png", new Rectangle(64*0,64*1,64,64)) { ScaleX=0.6f, ScaleY=0.6f }
                            }
                        }
                    }}
                }
            };
            scene.AddUI(weaponIcon);

            var weaponText = new UIObject
            {
                Name = $"EquipSummaryText_{ch.Name}_武器",
                PosX = baseX + 35,
                PosY = weaponIcon.PosY + 12,
                Text = weapon?.Id ?? "(未装備)",
                FontSize = 16,
                TextColor = "#FFFFFF",
                ZIndex = 1000,
            };
            scene.AddUI(weaponText);

            weaponIcon.OnClick = () => ShowEquipPopup(scene, ch, "武器");
            weaponText.OnClick = () => ShowEquipPopup(scene, ch, "武器");
            weaponIcon.OnLongPressStart = () => { if (weapon != null) ItemPopupHelper.Show(scene, new Item(weapon.Id, weapon.Description, weapon.SrcX / 64, weapon.SrcY / 64), weapon.HandsThrust, weapon.HandsSlash, weapon.HandsDown); };
            weaponIcon.OnLongPressRelease = () => { ItemPopupHelper.Close(scene); };

            // === 忍具 ===
            string? toolId = EquipmentManager.Instance.GetEquippedId(ch, "忍具");
            var tool = toolId != null ? EquipmentManager.Instance.Get(toolId) : null;

            var toolIcon = new UIObject
            {
                Name = $"EquipSummary_{ch.Name}_忍具",
                PosX = baseX - 10,
                PosY = weaponText.PosY + 40,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = tool != null
                                    ? new Sprite(tool.SpriteSheet, new Rectangle(tool.SrcX, tool.SrcY,64,64)) { ScaleX=0.6f, ScaleY=0.6f }
                                    : new Sprite("images/ui04-00.png", new Rectangle(64*0,64*1,64,64)) { ScaleX=0.6f, ScaleY=0.6f }
                            }
                        }
                    }}
                }
            };
            scene.AddUI(toolIcon);

            var toolText = new UIObject
            {
                Name = $"EquipSummaryText_{ch.Name}_忍具",
                PosX = baseX + 35,
                PosY = toolIcon.PosY + 12,
                Text = tool?.Id ?? "忍具装備無し",
                FontSize = 16,
                TextColor = "#FFFFFF"
            };
            scene.AddUI(toolText);

            toolIcon.OnClick = () => ShowEquipPopup(scene, ch, "忍具");
            toolText.OnClick = () => ShowEquipPopup(scene, ch, "忍具");
            toolIcon.OnLongPressStart = () => { if (tool != null) ItemPopupHelper.Show(scene, new Item(tool.Id, tool.Description, tool.SrcX / 64, tool.SrcY / 64)); };
            toolIcon.OnLongPressRelease = () => { ItemPopupHelper.Close(scene); };

            // === 傾向グラフ描画 ===
            scene.CreateWeaponPolygonGraph(weapon, isLeft ? 100 : Common.CanvasWidth - 100 , centerY: Common.CanvasHeight/2 + 115, radius: 60, prefix: $"ForgeGraph_{ch.Name}");

            // === スキル一覧描画 ===
            scene.ShowCharacterSkills(ch, isLeft ? 3 : Common.CanvasWidth / 2 + 3, 250, 2);
        }

        /// <summary>
        /// 装備選択ポップアップ表示
        /// </summary>
        private void ShowEquipPopup(Scene scene, Character ch, string category)
        {
            CloseEquipPopup(scene); // ★既存削除

            _selectedCharacter = ch;
            _selectedCategory = category;
            _selectedEquipCandidate = null;

            _previewEquip.Clear();
            foreach (var member in GameMain.Instance.PlayerParty)
            {
                var eqId = EquipmentManager.Instance.GetEquippedId(member, category);
                _previewEquip[$"{member.Name}_{category}"] = eqId;
            }

            float popupX = 0;
            float popupY = 45;

            // === ブロッカー ===
            var blocker = new UIObject
            {
                Name = "PopupEquip_Blocker",
                PosX = 0,
                PosY = 0,
                ZIndex = 500000,
                Opacity = 0.8f,
                Animations = new()
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new() {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui06-01.png",
                                    new Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                                { ScaleX = 1.0f, ScaleY = 1.0f }
                            }
                        }
                    }}
                },
                CurrentAnimationName = "idle"
            };
            scene.AddUI(blocker);

            // === 現在装備 ===
            var currentId = EquipmentManager.Instance.GetEquippedId(ch, category);
            var currentEq = currentId != null ? EquipmentManager.Instance.Get(currentId) : null;
            AddEquipDetailWindow(scene, "PopupEquip_Current", popupX, popupY, currentEq, "現在装備");

            // 矢印
            var arrow = new UIObject
            {
                Name = "PopupEquip_Arrow",
                CenterX = true,
                PosY = popupY + 115,
                ZIndex = 1001000,
                Animations = new()
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new() {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui04-00.png", new Rectangle(64*1,64*2,64,64))
                            }
                        }
                    }}
                }
            };
            scene.AddUI(arrow);

            // 変更後詳細
            AddEquipDetailWindow(scene, "PopupEquip_Candidate", popupX, popupY + 150, null, "変更後");

            // === 装備候補一覧 ===
            float listY = popupY + 320;
            var list = EquipmentManager.Instance.All
                .Where(e => e.Category == category && EquipmentManager.Instance.IsUnlocked(e.Id))
                .ToList();

            // 背景
            var listBg = new UIObject
            {
                Name = "PopupEquip_ListBG",
                CenterX = true,
                PosY = listY,
                ZIndex = 1000000,
                Animations = new()
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new() {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-00.png", new Rectangle(0,0,360,180))
                                { ScaleX = 1.0f, ScaleY = 1.5f }
                            }
                        }
                    }}
                }
            };
            scene.AddUI(listBg);

            var titleText = new UIObject
            {
                Name = $"PopupEquip_ListTitle",
                PosX = 10,
                PosY = listY,
                Text = "所持品一覧",
                FontSize = 14,
                TextColor = "#FFFF66",
                ZIndex = 1000002
            };
            scene.AddUI(titleText);

            int cols = 6;
            int cellSize = 48;
            for (int i = 0; i < list.Count; i++)
            {
                var eq = list[i];
                float posX = popupX + (i % cols) * cellSize + 40;
                float posY = listY + (i / cols) * cellSize + 40;

                // === 背景を追加（傾向・重量段階に応じて） ===
                if (category == "武器")
                {
                    var (sheet, rect) = scene.GetWeaponBackgroundSprite(eq);
                    var bg = new UIObject
                    {
                        Name = $"PopupEquipBg_{eq.Id}_{i}",
                        PosX = posX - 6,
                        PosY = posY - 6,
                        ZIndex = 1000099, // アイコンより一段下
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
                }

                // 武器本体
                var icon = new UIObject
                {
                    Name = $"PopupEquip_{category}_{eq.Id}_{i}", // ← 装備IDを埋め込む
                    PosX = posX,
                    PosY = posY,
                    ZIndex = 1000100,
                    Animations = new()
                    {
                        { "idle", new GameObjectAnimation {
                            Frames = new() {
                                new GameObjectAnimationFrame {
                                    Sprite = new Sprite(eq.SpriteSheet,new Rectangle(eq.SrcX,eq.SrcY,64,64))
                                    { ScaleX=0.6f, ScaleY=0.6f }
                                }
                            }
                        }}
                    }
                };
                scene.AddUI(icon);

                // === 初期表示: 装備中アイコンにマーカーを配置 ===
                foreach (var member in GameMain.Instance.PlayerParty)
                {
                    var equippedId = EquipmentManager.Instance.GetEquippedId(member, category);
                    if (equippedId == eq.Id)
                    {
                        AddMarker(scene, member, category, posX, posY);
                    }
                }

                // === クリック時処理 ===
                icon.OnClick = () =>
                {
                    _selectedEquipCandidate = eq;
                    UpdateEquipDetailWindow(scene, "PopupEquip_Candidate", popupX, popupY + 150, eq, "変更後");

                    var other = GameMain.Instance.PlayerParty.First(ch2 => ch2 != _selectedCharacter);
                    var myKey = $"{_selectedCharacter.Name}_{category}";
                    var otherKey = $"{other.Name}_{category}";

                    var myEqId = _previewEquip[myKey];
                    var otherEqId = _previewEquip[otherKey];

                    if (category == "武器")
                    {
                        if (otherEqId == eq.Id)
                        {
                            // 相方がこの武器を装備中 → 自分の元装備を相方へ
                            _previewEquip[otherKey] = myEqId;
                        }
                    }

                    // 自キャラは候補装備に更新
                    _previewEquip[myKey] = eq.Id;

                    // マーカーを再配置
                    UpdateMarkers(scene, category);
                };
            }

            // 装備ボタン
            var equipBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "PopupEquip_EquipBtn";
                ui.PosX = popupX + 80;
                ui.PosY = listY + 5 * cellSize;
                ui.Text = "装備する";
                ui.ZIndex = 1001000;
                ui.OnClick = () =>
                {
                    foreach (var member in GameMain.Instance.PlayerParty)
                    {
                        var key = $"{member.Name}_{_selectedCategory}";
                        var eqId = _previewEquip.ContainsKey(key) ? _previewEquip[key] : null;
                        EquipmentManager.Instance.Equip(member, _selectedCategory, eqId);
                    }
                    RefreshTabContents(scene);
                    CloseEquipPopup(scene);
                };
            });
            scene.AddUI(equipBtn);

            // 閉じるボタン
            var closeBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "PopupEquip_Close";
                ui.PosX = popupX + 200;
                ui.PosY = listY + 5 * cellSize;
                ui.Text = "閉じる";
                ui.ZIndex = 1001000;
                ui.OnClick = () => CloseEquipPopup(scene);
            });
            scene.AddUI(closeBtn);

            // 忍具用: 外す
            if (category == "忍具")
            {
                var noneBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
                {
                    ui.Name = "PopupEquip_NoneBtn";
                    ui.PosX = 300;
                    ui.PosY = 160;
                    ui.Text = "外す";
                    ui.ZIndex = 1003000;
                    ui.OnClick = () =>
                    {
                        _selectedEquipCandidate = null;
                        EquipmentManager.Instance.Equip(_selectedCharacter, _selectedCategory, null);
                        RefreshTabContents(scene);
                        CloseEquipPopup(scene);
                    };
                });
                scene.AddUI(noneBtn);
            }
        }

        /// <summary>
        /// マーカー再配置
        /// </summary>
        private void UpdateMarkers(Scene scene, string category)
        {
            // 既存マーカーを全削除
            var toRemove = scene.UIObjects.Values
                .Where(u => u.Name.StartsWith($"PopupEquip_Marker_{category}_"))
                .ToList();
            foreach (var u in toRemove) scene.RemoveUI(u);

            // プレビュー状態に基づいて全キャラ分マーカーを再描画
            foreach (var member in GameMain.Instance.PlayerParty)
            {
                var eqId = _previewEquip[$"{member.Name}_{category}"];
                if (eqId != null)
                {
                    var pos = GetEquipIconPos(scene, eqId, category);
                    if (pos != null)
                        AddMarker(scene, member, category, pos.Value.x, pos.Value.y);
                }
            }
        }

        /// <summary>
        /// 選択マーカー追加用ヘルパ
        /// </summary>
        private void AddMarker(Scene scene, Character ch, string category, float posX, float posY)
        {
            string markerName = $"PopupEquip_Marker_{category}_{ch.Name}";

            // このキャラ専用のマーカーだけ消す
            if (scene.TryGetUI(markerName, out var old))
                scene.RemoveUI(old);

            var marker = new UIObject
            {
                Name = markerName,
                PosX = posX - 5,
                PosY = posY - 5,
                ZIndex = 1005000,
                Opacity = 0.9f,
                Enabled = false,
                Animations = new()
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new() {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui04-00.png",
                                    new Rectangle(
                                        ch == GameMain.Instance.PlayerParty[0] ? 64*9 : 64*10,
                                        64*4, 64, 64)) { ScaleX=0.8f, ScaleY=0.8f }
                            }
                        }
                    }}
                }
            };
            scene.AddUI(marker);
        }

        /// <summary>
        /// アイコン座標取得関数
        /// </summary>
        private (float x, float y)? GetEquipIconPos(Scene scene, string equipId, string category)
        {
            var icon = scene.UIObjects.Values.FirstOrDefault(u => u.Name == $"PopupEquip_{category}_{equipId}_{u.Name.Split('_').Last()}");
            if (icon != null) return (icon.PosX, icon.PosY);
            return null;
        }

        /// <summary>
        /// 装備ポップアップ削除
        /// </summary>
        private void CloseEquipPopup(Scene scene)
        {
            var toRemove = scene.UIObjects.Values
                .Where(u => u.Name.StartsWith("PopupEquip"))
                .ToList();
            foreach (var ui in toRemove) scene.RemoveUI(ui);
        }

        private void AddEquipDetailWindow(Scene scene, string name, float x, float y, Equipment? eq, string title)
        {
            var bg = new UIObject
            {
                Name = $"{name}_BG",
                CenterX = true,
                PosY = y,
                ZIndex = 1000000,
                Animations = new Dictionary<string, GameObjectAnimation> {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-00.png",
                                    new System.Drawing.Rectangle(0, 0, 360, 180))
                                {
                                    ScaleX = 1.0f,
                                    ScaleY = 0.82f,
                                }
                            }
                        }
                    }}
                }
            };
            scene.AddUI(bg);

            var titleText = new UIObject
            {
                Name = $"{name}_Title",
                PosX = x + 10,
                PosY = y,
                Text = title,
                FontSize = 14,
                TextColor = "#FFFF66",
                ZIndex = 1000002
            };
            scene.AddUI(titleText);

            UpdateEquipDetailWindow(scene, name, x, y, eq, title);
        }

        /// <summary>
        /// 装備詳細ウィンドウ更新
        /// </summary>
        private void UpdateEquipDetailWindow(Scene scene, string name, float x, float y, Equipment? eq, string title)
        {
            // --- 既存削除 ---
            var toRemove = scene.UIObjects.Values
                .Where(u => u.Name.StartsWith(name + "_") &&
                           (u.Name.EndsWith("_InfoIcon") ||
                            u.Name.EndsWith("_InfoText") ||
                            u.Name.Contains("_Skill_")))
                .ToList();
            foreach (var ui in toRemove) scene.RemoveUI(ui);

            if (eq == null) return;

            // アイコン
            var icon = new UIObject
            {
                Name = $"{name}_InfoIcon",
                PosX = x + 20,
                PosY = y + 35,
                ZIndex = 1000200,
                Animations = new()
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new() {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(eq.SpriteSheet,new Rectangle(eq.SrcX,eq.SrcY,64,64)){ ScaleX=1.1f, ScaleY=1.1f }
                            }
                        }
                    }}
                }
            };
            scene.AddUI(icon);

            // ★ 強化段階を整形
            string weightStr = eq.ForgeWeight switch
            {
                > 0 => $"重量+{eq.ForgeWeight}",
                < 0 => $"重量{eq.ForgeWeight}",
                _ => "重量±0"
            };

            string sharpStr = eq.ForgeSharp switch
            {
                > 0 => $"鋭さ+{eq.ForgeSharp}",
                < 0 => $"鋭さ{eq.ForgeSharp}",
                _ => "鋭さ±0"
            };

            var text = $"【{eq.Id}】\n\n{eq.Description}";

            if (eq.Category == "武器")
            {
                text += $"\n　[穿×{eq.HandsThrust} 迅×{eq.HandsSlash} 剛×{eq.HandsDown}]　" +
                        $"　[{weightStr} {sharpStr}]";
            }

            // 説明文
            var desc = new UIObjectMultilineText
            {
                Name = $"{name}_InfoText",   // ←一意な名前
                PosX = x + 100,
                PosY = y + 25,
                ZIndex = 1000200,
                FontSize = 11,
                TextColor = "#FFFFFF",
                Text = text
            };
            scene.AddUI(desc);

            // --- ★スキル表示（ShowCharacterSkillsCore風） ---
            var skills = eq.Skills
                .Where(s => s.Category == "武器固定" || s.Category == "武器スロット")
                .Take(2)
                .ToList();

            if (skills.Count == 0)
            {
                return;
            }

            int offsetX = 86;
            int offsetY = 26;

            for (int i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];
                float px = x + 120 + (i % 2) * offsetX;
                float py = y + 100 + (i / 2) * offsetY;

                string bgColor = skill.Category switch
                {
                    "武器固定" => "#FF6666",
                    "武器スロット" => "#CC66FF",
                    _ => "#FFFFFF"
                };

                var ui = new UIObject
                {
                    Name = $"{name}_Skill_{skill.Id}_{i}",
                    PosX = px,
                    PosY = py,
                    ZIndex = 1000300,
                    Opacity = 0.95f,
                    Text = skill.DisplayName,
                    FontSize = 10,
                    TextAlign = "left",
                    TintColor = bgColor,
                    TextOffsetX = 3f,
                    TextOffsetY = 2f,
                    Animations = new()
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

                ui.OnLongPressStart = () => SkillPopupHelper.Show(scene, skill);
                ui.OnLongPressRelease = () => SkillPopupHelper.Close(scene);

                scene.AddUI(ui);
            }
        }

        public override Scene Create() => Create(null);
    }
}
