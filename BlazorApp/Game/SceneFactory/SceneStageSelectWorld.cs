using BlazorApp.Game.SceneFactory;
using BlazorApp.Game.UIObjectFactory;

namespace BlazorApp.Game
{
    public class SceneStageSelectWorld : BaseSceneFactory
    {
        public override GameState TargetState => GameState.StageSelectWorld;

        public override Scene Create(object? payload = null)
        {
            var scene = new Scene
            {
                State = GameState.StageSelectWorld
            };

            // 背景（広域マップ）
            scene.Background = new Background
            {
                Layers = new List<BackgroundLayer>
                {
                    new BackgroundLayer
                    {
                        Sprite = new Sprite("images/bg011.png",
                            new System.Drawing.Rectangle(0,0,360,640)),
                        LoopScroll = false
                    }
                }
            };

            // --- 目的地アイコン ---

            var btnHome = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "拠点に戻る";
                ui.PosX = 35f;
                ui.PosY = 531f;
                ui.Text = "拠点に戻る";
                ui.FontSize = 17;
                ui.OnClick = () =>
                {
                    GameMain.Instance.StartFadeTransition(GameState.Home);
                };
            });
            scene.AddUI(btnHome);

            var btnArea01 = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "廃村";
                ui.PosX = 42f;
                ui.PosY = 296f;
                ui.Text = "廃村";
                ui.TextAlign = "center";
                ui.FontSize = 17;
                ui.OnClick = () =>
                {
                    GameMain.Instance.CurrentArea = AreaId.Village;
                    GameMain.Instance.StartFadeTransition(GameState.StageSelectArea);
                };
            });
            scene.AddUI(btnArea01);

            var btnArea02 = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "森林地帯";
                ui.PosX = 238f;
                ui.PosY = 249f;
                ui.Text = "森林地帯";
                ui.TextAlign = "center";
                ui.FontSize = 17;
                ui.OnClick = () =>
                {
                    GameMain.Instance.CurrentArea = AreaId.Forest;
                    GameMain.Instance.StartFadeTransition(GameState.StageSelectArea);
                };
            });
            scene.AddUI(btnArea02);

            var btnArea03 = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "港町";
                ui.PosX = 290f;
                ui.PosY = 92f;
                ui.Text = "港町";
                ui.TextAlign = "center";
                ui.FontSize = 17;
                ui.OnClick = () =>
                {
                    GameMain.Instance.CurrentArea = AreaId.Castle;
                    GameMain.Instance.StartFadeTransition(GameState.StageSelectArea);
                };
            });
            scene.AddUI(btnArea03);

            var btnArea04 = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "火山";
                ui.PosX = 60f;
                ui.PosY = 68f;
                ui.Text = "火山";
                ui.TextAlign = "center";
                ui.FontSize = 17;
                ui.OnClick = () =>
                {
                    GameMain.Instance.CurrentArea = AreaId.Mountain;
                    GameMain.Instance.StartFadeTransition(GameState.StageSelectArea);
                };
            });
            scene.AddUI(btnArea04);

            var btnArea05 = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.Name = "雪山";
                ui.PosX = 182f;
                ui.PosY = 11f;
                ui.Text = "雪山";
                ui.TextAlign = "center";
                ui.FontSize = 17;
                ui.OnClick = () =>
                {
                    GameMain.Instance.CurrentArea = AreaId.Snow;
                    GameMain.Instance.StartFadeTransition(GameState.StageSelectArea);
                };
            });
            scene.AddUI(btnArea05);

            return scene;
        }

        public override Scene Create() => Create(null);
    }
}
