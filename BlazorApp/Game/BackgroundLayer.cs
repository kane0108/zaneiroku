namespace BlazorApp.Game
{
    /// <summary>
    /// 背景レイヤークラス
    /// </summary>
    public class BackgroundLayer
    {
        /// <summary>使用するスプライト</summary>
        public Sprite Sprite { get; set; }

        /// <summary>スクロール速度（X, Y）</summary>
        public float ScrollSpeedX { get; set; } = 0f;
        public float ScrollSpeedY { get; set; } = 0f;

        /// <summary>スクロールオフセット</summary>
        public float OffsetX { get; set; } = 0f;
        public float OffsetY { get; set; } = 0f;

        /// <summary>レイヤーの透明度（0.0〜1.0）</summary>
        public float Opacity { get; set; } = 1.0f;

        /// <summary>ループスクロールするかどうか</summary>
        public bool LoopScroll { get; set; } = true;

        /// <summary>
        /// 前景（キャラより前・UIより後）として描画するか
        /// </summary>
        public bool IsForeground { get; set; } = false;
    }
}
