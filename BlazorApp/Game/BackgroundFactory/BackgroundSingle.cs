using System.Drawing;

namespace BlazorApp.Game.BackgroundFactory
{
    /// <summary>
    /// 単一画像の背景を簡単に生成するファクトリ
    /// </summary>
    public static class BackgroundSingle
    {
        /// <summary>
        /// 単一画像から Background を生成
        /// </summary>
        /// <param name="imagePath">画像ファイルのパス</param>
        /// <returns>Background インスタンス</returns>
        public static Background Create(string imagePath)
        {
            return new Background
            {
                Layers = new List<BackgroundLayer>
                {
                    new BackgroundLayer
                    {
                        Sprite = new Sprite(
                            imagePath,
                            new Rectangle(0, 0, (int)Common.CanvasWidth, (int)Common.CanvasHeight)
                        ),
                    }
                }
            };
        }
    }
}
