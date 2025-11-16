using BlazorApp.Game;

namespace BlazorApp.Game.UIObjectFactory
{
    public abstract class BaseUIObjectFactory : IFactory<UIObject>
    {
        /// <summary>
        /// UIObjectの生成用引数をセット（必要に応じてオプション）
        /// </summary>
        public virtual void SetParameters(Dictionary<string, object> parameters) { }

        /// <summary>
        /// 実際にUIObjectを生成
        /// </summary>
        public abstract UIObject Create();
    }
}
