using System.Drawing;

namespace BlazorApp.Game.BackgroundFactory
{
    public class BackgroundSnowStorm : BaseBackgroundFactory
    {
        public override Background Create()
        {
            var background = CreateBackgroundBase(0);

            // 遠景レイヤー
            AddLayer(background, "images/bg008.png");

            // 前景レイヤー
            AddLayer(background, "images/bg005.png", scale: 1.0f, scrollSpeed: 180.0f, opacity: 0.3f, isForeground: true);

            return background;
        }
    }
}
