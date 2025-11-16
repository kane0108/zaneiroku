using BlazorApp.Game;
using BlazorApp.Game.UIObjectFactory;

namespace BlazorApp.Game.SceneFactory
{
    public abstract class BaseSceneFactory
    {
        public abstract GameState TargetState { get; }

        // ★ 追加: ペイロード付きCreate（既定は引数なし版を呼ぶ）
        public virtual Scene Create(object? payload) => Create();

        public abstract Scene Create();
    }
}
