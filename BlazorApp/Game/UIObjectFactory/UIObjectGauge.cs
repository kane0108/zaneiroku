using Microsoft.JSInterop;

namespace BlazorApp.Game.UIObjectFactory
{
    public class UIObjectGauge : UIObject
    {
        /// <summary>-10（全右色）～ +10（全左色）</summary>
        public float Value { get; set; } = 0f;
        public string LeftColor { get; set; } = "#FF0000"; // 赤
        public string RightColor { get; set; } = "#0000FF"; // 青

        public override void CollectDrawCommands(
            List<object> commands, IJSRuntime js,
            float fadeOpacity = 0f, float parentX = 0, float parentY = 0)
        {
            if (!Visible) return;

            float fadeAlpha = 1.0f - fadeOpacity;

            float drawX = parentX + PosX;
            float drawY = parentY + PosY;
            float w = DrawnWidth > 0 ? DrawnWidth : 200;
            float h = DrawnHeight > 0 ? DrawnHeight : 20;

            // -10～+10 を 0～1 に正規化
            // -10 → 0, 0 → 0.5, +10 → 1
            float ratio = (Value + 10f) / 20f;

            // 赤の描画幅（0～w）
            float redWidth = w * ratio;

            // === 赤（左側から広がる） ===
            if (redWidth > 0)
            {
                commands.Add(new
                {
                    type = "fillRect",
                    canvasId = "gameCanvas",
                    x = drawX,
                    y = drawY,
                    width = redWidth,
                    height = h,
                    fillColor = LeftColor,
                    opacity = fadeAlpha,
                    zIndex = this.ZIndex
                });
            }

            // === 青（残りを右から埋める） ===
            float blueWidth = w - redWidth;
            if (blueWidth > 0)
            {
                commands.Add(new
                {
                    type = "fillRect",
                    canvasId = "gameCanvas",
                    x = drawX + redWidth,
                    y = drawY,
                    width = blueWidth,
                    height = h,
                    fillColor = RightColor,
                    opacity = fadeAlpha,
                    zIndex = this.ZIndex
                });
            }
        }
    }
}
