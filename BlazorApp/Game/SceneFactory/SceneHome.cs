using BlazorApp.Game.BackgroundFactory;
using BlazorApp.Game.CharacterFactory;
using BlazorApp.Game.SceneFactory;
using BlazorApp.Game.Stealth;
using BlazorApp.Game.UIObjectFactory;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace BlazorApp.Game
{
    /// <summary>
    /// ホーム画面
    /// </summary>
    public class SceneHome : BaseSceneFactory
    {
        public override GameState TargetState => GameState.Home;
        public override Scene Create()
        {
            return Create();
        }

        public override Scene Create(object? payload = null)
        {
            GameMain.Instance.CurrentArea = AreaId.None;

            var scene = new Scene
            {
                State = GameState.Home
            };

            // テスト用の敵キャラ
            var enemies = new List<Character>
            {
                GameInitializer.Create<CharacterEnemy00, Character>(c =>
                {
                    c.ObjectId = 100;
                    c.Name = "敵忍者1";
                    c.Type = "Enemy";

                    c.BaseStats = c.BaseStats.Clone();
                    c.CurrentStats = c.BaseStats.Clone();
                    c.CurrentStats.ResetHP();
                }),
                GameInitializer.Create<CharacterEnemy00, Character>(c =>
                {
                    c.ObjectId = 101;
                    c.Name = "敵忍者2";
                    c.Type = "Enemy";

                    c.BaseStats = c.BaseStats.Clone();
                    c.CurrentStats = c.BaseStats.Clone();
                    c.CurrentStats.ResetHP();
                }),
                //GameInitializer.Create<CharacterEnemy00, Character>(c =>
                //{
                //    c.ObjectId = 102;
                //    c.Name = "敵忍者3";
                //    c.Type = "Enemy";

                //    c.BaseStats = c.BaseStats.Clone();
                //    c.CurrentStats = c.BaseStats.Clone();
                //    c.CurrentStats.ResetHP();
                //}),
            };

            // ホーム画面の背景
            scene.Background = new Background
            {
                Layers = new List<BackgroundLayer>
                {
                    new BackgroundLayer
                    {
                        Sprite = new Sprite("images/bg001.png", new System.Drawing.Rectangle(0,0,360,640)),
                        LoopScroll = false,
                        IsForeground = false
                    }
                }
            };

            // 共通：経験値描画
            scene.CreateGlobalExpUI();

            // 共通・所持金描画
            scene.CreateGlobalMoneyUI();

            var nextPosY = 100f;

            // --- 隠密探索盤 ---

            var btnStory = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "物語を進める";
                ui.CenterX = true;
                ui.PosY = nextPosY;
                ui.Text = "物語を進める";
                ui.OnClick = () =>
                {
                    GameMain.Instance.StartFadeTransition(GameState.StageSelectWorld);
                };
            });
            scene.AddUI(btnStory);

            // --- 能力確認／修練 ---

            if (Common.CurrentProgress >= Common.StoryProgress.VillageMission1)
            {
                nextPosY += 100;

                var btnStatus = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
                {
                    ui.Name = "能力確認／修練";
                    ui.CenterX = true;
                    ui.PosY = nextPosY;
                    ui.Text = "能力確認／修練";
                    ui.OnClick = () =>
                    {
                        GameMain.Instance.StartFadeTransition(GameState.Status);
                    };
                });
                scene.AddUI(btnStatus);
            }

            // --- 武器鍛錬 ---

            if (Common.CurrentProgress >= Common.StoryProgress.VillageMission1)
            {
                nextPosY += 100;

                var btnForge = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
                {
                    ui.Name = "鍛錬ボタン";
                    ui.CenterX = true;
                    ui.PosY = nextPosY;
                    ui.Text = "武器鍛錬";
                    ui.OnClick = () =>
                    {
                        GameMain.Instance.StartFadeTransition(GameState.Forge);
                    };
                });
                scene.AddUI(btnForge);
            }

            // --- 隠密探索盤テスト ---
            var stealthSetup = StealthBoardTemplates.Level5a();
            stealthSetup.StageName = "隠密探索盤テスト";
            stealthSetup.Background = GameInitializer.Create<BackgroundSnowStorm, Background>();
            stealthSetup.BattleBackground = BackgroundSingle.Create("images/bg006.png");
            stealthSetup.Allies = GameMain.Instance.PlayerParty;
            //stealthSetup.StealthEnemies = enemies;
            stealthSetup.ItemDropTable = new Dictionary<string, int> {
                        { "鎚鉄", 70 },
                        { "名倉砥", 30 },
                    };
            stealthSetup.PerfectItemDropTable = new Dictionary<string, int> {
                        { "鎚鉄", 25 },
                        { "名倉砥", 15 },
                        { "羽鋼", 60 },
                    };
            stealthSetup.MaxItemDrops = 5;
            stealthSetup.BaseExpPerDig = 10;
            stealthSetup.TotalMoney = 10000;

            nextPosY += 100;

            var btnStealthTest = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "隠密探索盤テスト";
                ui.CenterX = true;
                ui.PosY = nextPosY;
                ui.Text = "隠密探索盤テスト";
                ui.OnClick = () =>
                {
                    GameMain.Instance.StartFadeTransition(GameState.StealthBoard, stealthSetup);
                };
            });
            scene.AddUI(btnStealthTest);

            // --- 戦闘テスト ---

            var setup = new Battle.BattleSetup
            {
                Allies = GameMain.Instance.PlayerParty,
                Enemies = enemies,
                Background = BackgroundSingle.Create("images/bg004.png")
            };

            nextPosY += 100;

            var btnBattleTest = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "戦闘テスト";
                ui.CenterX = true;
                ui.PosY = nextPosY;
                ui.Text = "戦闘テスト";
                ui.FontSize = 20;
                ui.OnClick = () =>
                {
                    // ★共通関数で装備・スキルを反映
                    foreach (var ch in GameMain.Instance.PlayerParty)
                    {
                        EquipmentManager.Instance.ApplyEquipmentAndSkillsToCharacter(ch);
                        ch.CurrentStats.ResetHP();
                    }

                    // 戦闘モードに遷移
                    GameMain.Instance.StartFadeTransition(GameState.Combat, setup);
                };
            });
            scene.AddUI(btnBattleTest);

            _ = SaveOnEnterAsync();

            return scene;
        }

        /// <summary>
        /// セーブ処理
        /// </summary>
        private async Task SaveOnEnterAsync()
        {
            await SaveManager.SaveAsync();
        }
    }
}
