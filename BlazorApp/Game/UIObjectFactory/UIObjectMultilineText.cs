using Microsoft.JSInterop;
using System.Drawing;

namespace BlazorApp.Game.UIObjectFactory
{
    /// <summary>
    /// 複数行テキスト専用UIオブジェクト
    /// </summary>
    public class UIObjectMultilineText : UIObject
    {
        /// <summary>
        /// 行間ピクセル
        /// </summary>
        public float LineSpacing { get; set; } = 4f;

        public override void CollectDrawCommands(
            List<object> commands, IJSRuntime js, float fadeOpacity = 0f,
            float parentX = 0, float parentY = 0)
        {
            if (!Visible) return;
            if (string.IsNullOrEmpty(Text)) return;

            float drawX = parentX + PosX;
            float drawY = parentY + PosY;
            float fadeAlpha = 1.0f - fadeOpacity;

            // 改行で分割
            var lines = Text.Split('\n');
            float lineHeight = FontSize + LineSpacing;

            float finalX = CenterX ? Common.CanvasWidth / 2f : drawX;

            for (int i = 0; i < lines.Length; i++)
            {
                commands.Add(new
                {
                    type = "text",
                    canvasId = "gameCanvas",
                    text = lines[i],
                    x = finalX + TextOffsetX,
                    y = drawY + TextOffsetY + i * lineHeight,
                    font = Font,
                    textColor = TextColor,
                    textAlign = TextAlign,
                    opacity = Opacity * fadeAlpha,
                    zIndex = this.ZIndex   // ★必ず追加
                });
            }

            // ヒット判定用に矩形を更新（高さを複数行ぶんに拡張）
            float totalHeight = lines.Length * lineHeight;
            UpdateHitRect(finalX, drawY, Common.CanvasWidth, totalHeight);
        }
    }
}
