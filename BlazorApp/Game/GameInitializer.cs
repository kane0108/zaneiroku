using BlazorApp.Game.UIObjectFactory;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;

namespace BlazorApp.Game
{
    /// <summary>
    /// ゲーム初期定義
    /// </summary>
    public static class GameInitializer
    {
        public static TProduct Create<TFactory, TProduct>()
        where TFactory : IFactory<TProduct>, new()
        {
            return new TFactory().Create();
        }

        public static TProduct Create<TFactory, TProduct>(Action<TProduct> init)
            where TFactory : IFactory<TProduct>, new()
        {
            var obj = Create<TFactory, TProduct>();
            init(obj);
            return obj;
        }

        public static UIObject Create<TFactory, TProduct>(Dictionary<string, object> parameters)
        where TFactory : BaseUIObjectFactory, IFactory<TProduct>, new()
        {
            var factory = new TFactory();
            factory.SetParameters(parameters);
            return factory.Create();
        }
    }

    /// <summary>
    /// ファクトリ用インタフェース
    /// </summary>
    public interface IFactory<T>
    {
        T Create();
    }
}
