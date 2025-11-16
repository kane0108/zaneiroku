using Microsoft.JSInterop;

namespace BlazorApp.Game
{
    /// <summary>
    /// 背景（複数レイヤーで構成可能）
    /// </summary>
    public class Background
    {
        /// <summary>構成するレイヤー群</summary>
        public List<BackgroundLayer> Layers { get; set; } = new();

        public Background() { }

        /// <summary>
        /// レイヤーのオフセットを更新（スクロール処理など）
        /// </summary>
        public void Update(float deltaTime)
        {
            foreach (var layer in Layers)
            {
                var sprite = layer.Sprite;
                if (sprite.SourceRect.Width <= 0 || sprite.SourceRect.Height <= 0)
                    continue;

                // オフセット更新
                layer.OffsetX += layer.ScrollSpeedX * deltaTime;
                layer.OffsetY += layer.ScrollSpeedY * deltaTime;

                // ループスクロール処理
                if (layer.LoopScroll)
                {
                    float drawWidth = sprite.SourceRect.Width * sprite.ScaleX;
                    float drawHeight = sprite.SourceRect.Height * sprite.ScaleY;

                    layer.OffsetX = (layer.OffsetX % drawWidth + drawWidth) % drawWidth;
                    layer.OffsetY = (layer.OffsetY % drawHeight + drawHeight) % drawHeight;
                }
            }
        }

        /// <summary>
        /// 描画コマンドを収集
        /// </summary>
        public void CollectDrawCommands(
            List<object> commands, IJSRuntime js,
            bool foregroundOnly, float fadeOpacity = 0f)
        {
            if (Layers == null || Layers.Count == 0) return;

            float fadeAlpha = 1.0f - fadeOpacity;

            foreach (var layer in Layers)
            {
                // 前景/背景の選択
                if (foregroundOnly && !layer.IsForeground) continue;
                if (!foregroundOnly && layer.IsForeground) continue;

                var sprite = layer.Sprite;
                if (sprite == null) continue;

                sprite.EnsureImageSizeLazy(js);
                int sw = sprite.SourceRect.Width;
                int sh = sprite.SourceRect.Height;
                if (sw <= 0 || sh <= 0) continue;

                float drawW = sw * sprite.ScaleX;
                float drawH = sh * sprite.ScaleY;

                int canvasW = (int)Common.CanvasWidth;
                int canvasH = (int)Common.CanvasHeight;

                float offsetX = layer.OffsetX % drawW;
                if (offsetX < 0) offsetX += drawW;

                float offsetY = layer.OffsetY % drawH;
                if (offsetY < 0) offsetY += drawH;

                float startX = -offsetX;
                float startY = -offsetY;

                // タイル状に敷き詰めて描画
                for (float x = startX; x < canvasW; x += drawW)
                {
                    for (float y = startY; y < canvasH; y += drawH)
                    {
                        commands.Add(new
                        {
                            type = "sprite",
                            canvasId = "gameCanvas",
                            imagePath = sprite.ImagePath,
                            x,
                            y,
                            sx = sprite.SourceRect.X,
                            sy = sprite.SourceRect.Y,
                            sw,
                            sh,
                            width = drawW,
                            height = drawH,
                            mirror = false,
                            tint = "#FFFFFF",
                            opacity = layer.Opacity * fadeAlpha,
                            transparentColor = sprite.TransparentColor?.ToArgb(),
                            threshold = sprite.TransparentThreshold
                        });

                        if (!layer.LoopScroll) break;
                    }
                    if (!layer.LoopScroll) break;
                }
            }
        }

        /// <summary>
        /// 強制再描画フラグ（現状はダミー）
        /// </summary>
        public void MarkDirty() { }
    }
}
