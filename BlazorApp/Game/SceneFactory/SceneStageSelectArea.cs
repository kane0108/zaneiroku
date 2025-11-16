using BlazorApp.Game.BackgroundFactory;
using BlazorApp.Game.CharacterFactory;
using BlazorApp.Game.SceneFactory;
using BlazorApp.Game.Stealth;
using BlazorApp.Game.UIObjectFactory;

namespace BlazorApp.Game
{
    public class SceneStageSelectArea : BaseSceneFactory
    {
        public override GameState TargetState => GameState.StageSelectArea;

        public override Scene Create(object? payload = null)
        {
            var areaId = GameMain.Instance.CurrentArea;

            var scene = new Scene
            {
                State = GameState.StageSelectArea
            };

            // 背景（狭域マップ、エリアごとに差し替え）
            string bgPath = areaId switch
            {
                AreaId.Village => "images/bg010.png",
                _ => "images/bg010.png"
            };

            scene.Background = new Background
            {
                Layers = new List<BackgroundLayer>
                {
                    new BackgroundLayer
                    {
                        Sprite = new Sprite(bgPath,
                            new System.Drawing.Rectangle(0,0,360,640)),
                        LoopScroll = false
                    }
                }
            };

            // 共通：経験値描画
            scene.CreateGlobalExpUI();

            // 共通・所持金描画
            scene.CreateGlobalMoneyUI();

            // マップ選択へ戻るボタン
            var btnBack= GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "場所選択に戻る";
                ui.CenterX = true;
                ui.PosY = Common.CanvasHeight - 80;
                ui.Text = ui.Name;
                ui.FontSize = 17;
                ui.OnClick = () =>
                {
                    GameMain.Instance.StartFadeTransition(GameState.StageSelectWorld);
                };
            });
            scene.AddUI(btnBack);

            // ステージアイコン（タップで探索盤へ）
            var btnStage = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "廃村の影・壱";
                ui.CenterX = true;
                ui.PosY = 250f;
                ui.Text = ui.Name;
                ui.FontSize = 17;
                ui.OnClick = () =>
                {
                    // セットアップを取得
                    var setup = StealthBoardTemplates.Level1a();
                    setup.StageName = ui.Name;
                    setup.Background = BackgroundSingle.Create("images/bg007.png");
                    setup.BattleBackground = BackgroundSingle.Create("images/bg004.png");
                    setup.ItemDropTable = new Dictionary<string, int> {
                        { "鎚鉄", 100 },
                        { "名倉砥", 30 },
                    };
                    setup.PerfectItemDropTable = new Dictionary<string, int> {
                        { "鎚鉄", 70 },
                        { "名倉砥", 30 },
                    };
                    setup.MaxItemDrops = 1;
                    setup.BaseExpPerDig = 1;
                    setup.TotalMoney = 20;
                    setup.Allies = GameMain.Instance.PlayerParty;
                    setup.ForcedEnemies = new List<Character>
                    {
                        GameInitializer.Create<CharacterEnemy00, Character>(c => { c.ObjectId = 100; c.Name = "中忍";}),
                    };
                    setup.StealthEnemies = new List<Character>
                    {
                        GameInitializer.Create<CharacterEnemy00, Character>(c => { c.ObjectId = 101; c.Name = "下忍";}),
                    };

                    GameMain.Instance.StartFadeTransition(GameState.StealthBoard, setup);
                };
            });
            scene.AddUI(btnStage);

            return scene;
        }

        public override Scene Create() => Create(null);
    }
}
