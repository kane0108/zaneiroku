using BlazorApp.Game.BackgroundFactory;
using BlazorApp.Game.Battle;
using BlazorApp.Game.CharacterFactory;
using BlazorApp.Game.UIObjectFactory;
using System.Drawing;

namespace BlazorApp.Game.SceneFactory
{
    public class SceneCombat : BaseSceneFactory
    {
        public override GameState TargetState => GameState.Combat;

        public override Scene Create(object? payload)
        {
            if (payload is BattleSetup setup)
            {
                var scene = new BattleScene
                {
                    State = GameState.Combat,
                };

                scene.Initialize(setup);

                return scene;
            }

            // フォールバック（従来の固定生成）
            return Create();
        }

        public override Scene Create()
        {
            return Create();
        }
    }
}
