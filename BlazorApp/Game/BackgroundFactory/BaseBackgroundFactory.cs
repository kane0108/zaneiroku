using BlazorApp.Game;
using System.Drawing;

namespace BlazorApp.Game.BackgroundFactory
{
    /// <summary>
    /// ベース背景ファクトリ
    /// </summary>
    public abstract class BaseBackgroundFactory : IFactory<Background>
    {
        public abstract Background Create();

        /// <summary>
        /// 背景ベースの生成
        /// </summary>
        protected Background CreateBackgroundBase(int id)
        {
            return new Background();
        }

        /// <summary>
        /// レイヤーの追加
        /// </summary>
        protected void AddLayer(Background background, string imagePath,
            float scale = 1.0f, float scrollSpeed = 0f, float opacity = 1.0f, bool isForeground = false)
        {
            background.Layers.Add(
                new BackgroundLayer()
                {
                    Sprite = new Sprite(imagePath)
                    {
                        ScaleX = scale,
                        ScaleY = scale,
                        TransparentColor = Color.Magenta,
                        TransparentThreshold = 0
                    },
                    ScrollSpeedX = scrollSpeed,
                    LoopScroll =  (scrollSpeed == 0f ? false : true),
                    Opacity = opacity,
                    IsForeground = isForeground,
                }
            );
        }
    }
}