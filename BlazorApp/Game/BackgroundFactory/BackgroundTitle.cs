using System.Drawing;

namespace BlazorApp.Game.BackgroundFactory
{
    public class BackgroundTitle : BaseBackgroundFactory
    {
        public override Background Create()
        {
            var background = CreateBackgroundBase(0);

            // 遠景レイヤー
            AddLayer(background, "images/bg002.png");

            // 前景レイヤー
            AddLayer(background, "images/bg003.png", scrollSpeed : 20.0f, opacity : 0.2f);

            return background;
        }
    }
}
