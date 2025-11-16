using Microsoft.JSInterop;
using System.Drawing;

namespace BlazorApp.Game
{
    /// <summary>
    /// UI用オブジェクト（ボタンやテキストなど）
    /// </summary>
    public class UIObject : GameObjectBase
    {
        // === 表示テキスト関連 ===
        public string? Text { get; set; }
        public string? TextColor { get; set; } = "#FFFFFF";
        public int FontSize { get; set; } = 20;
        public string FontFamily { get; set; } = "sans-serif";
        public string Font => $"{FontSize}px {FontFamily}";

        // テキスト位置・配置
        public float TextOffsetX { get; set; } = 0f;
        public float TextOffsetY { get; set; } = 0f;
        public string TextAlign { get; set; } = "left";
        public bool CenterX { get; set; } = false;

        // 背景画像の伸縮設定
        public bool StretchToText { get; set; } = false;
        public float DrawnWidth { get; set; } = 0;
        public float DrawnHeight { get; set; } = 0;

        // === 子オブジェクト関連 ===
        public List<UIObject> Children { get; set; } = new();
        public UIObject? Parent { get; set; }

        // === 表示制御 ===
        public bool Visible { get; set; } = true;
        public bool Enabled { get; set; } = true;

        // === 補間関連 ===
        public float UiScaleX { get; set; } = 1.0f;
        public float UiScaleY { get; set; } = 1.0f;
        public float TargetUiScaleX { get; set; } = 1.0f;
        public float TargetUiScaleY { get; set; } = 1.0f;

        public float TargetPosX { get; set; }
        public float TargetPosY { get; set; }
        public float TargetScaleX { get; set; } = 1.0f;
        public float TargetScaleY { get; set; } = 1.0f;

        public float MoveLerpSpeed { get; set; } = 10f;
        public float ScaleLerpSpeed { get; set; } = 10f;
        public bool EnableLerp { get; set; } = false;

        // === フェードイン関連 ===
        public bool EnableFadeIn { get; private set; } = false;
        private float fadeTime = 0f;
        private float fadeDuration = 0.3f;

        // === ヒット判定 ===
        public RectangleF HitRect { get; private set; }

        // === 再描画管理 ===
        public bool IsDirty { get; private set; } = true;
        public void MarkDirty() => IsDirty = true;
        public void ClearDirty() => IsDirty = false;

        private float? cachedTextWidth;

        public float LongPressThreshold { get; set; } = 1.0f;
        public float _pressTimer = 0f;
        public bool _isPressing = false;
        public bool _longPressTriggered = false;

        // コールバック
        public Action? OnLongPressStart { get; set; }
        public Action? OnLongPressRelease { get; set; }

        /// <summary>
        /// アニメーション更新（補間・フェード含む）
        /// </summary>
        public virtual void UpdateRecursive(float deltaTime)
        {
            if (!Enabled) return;

            // アニメーション更新
            UpdateAnimation(deltaTime);

            // 補間処理
            if (EnableLerp)
            {
                PosX += (TargetPosX - PosX) * MoveLerpSpeed * deltaTime;
                PosY += (TargetPosY - PosY) * MoveLerpSpeed * deltaTime;
            }

            // フェードイン処理
            if (EnableFadeIn && Opacity < 1f)
            {
                fadeTime += deltaTime;
                float t = MathF.Min(1f, fadeTime / fadeDuration);
                Opacity = t;
                if (t >= 1f) EnableFadeIn = false;
            }

            // 子要素更新
            foreach (var child in Children.ToList())
            {
                child.UpdateRecursive(deltaTime);
            }
        }

        /// <summary>
        /// ヒット判定（子から優先的に判定）
        /// </summary>
        public UIObject? HitTestRecursive(float x, float y)
        {
            if (!Visible || !Enabled) return null;

            // 子要素をZIndex順に確認（前面優先）
            foreach (var child in Children.OrderByDescending(c => c.ZIndex))
            {
                var hit = child.HitTestRecursive(x, y);
                if (hit != null) return hit;
            }

            // 自身判定
            return IsHit(x, y) ? this : null;
        }

        public bool IsHit(float x, float y) => HitRect.Contains(x, y);

        /// <summary>
        /// フェードインを開始
        /// </summary>
        public void StartFadeIn(float duration = 0.3f)
        {
            fadeDuration = duration;
            fadeTime = 0f;
            Opacity = 0f;
            EnableFadeIn = true;
            Visible = true;
        }

        /// <summary>
        /// 描画コマンド収集
        /// </summary>
        public override void CollectDrawCommands(
            List<object> commands, IJSRuntime js, float fadeOpacity = 0f,
            float parentX = 0, float parentY = 0)
        {
            if (!Visible) return;

            float drawX = parentX + PosX;
            float drawY = parentY + PosY;
            float fadeAlpha = 1.0f - fadeOpacity;

            var sprite = GetCurrentFrameSprite();

            // === スプライトあり ===
            if (sprite != null)
            {
                sprite.EnsureImageSizeLazy(js);
                int sw = sprite.SourceRect.Width;
                int sh = sprite.SourceRect.Height;

                if (sw > 0 && sh > 0)
                {
                    // === スケール計算 ===
                    if (StretchToText && !string.IsNullOrEmpty(Text))
                    {
                        float baseSpriteWidth = 360f;
                        float baseSpriteHeight = 180f;

                        float textWidth = FontSize * Text.Length * 1.10f + 5.0f; // 全角文字用
                        float textHeight = FontSize * 1.6f;

                        sprite.ScaleX = textWidth / baseSpriteWidth;
                        sprite.ScaleY = textHeight / baseSpriteHeight;
                    }

                    // === 実際の描画サイズ ===
                    float drawW = sw * sprite.ScaleX;
                    float drawH = sh * sprite.ScaleY;

                    float finalX = parentX + PosX;
                    float finalY = parentY + PosY;

                    // フレームオフセット
                    var frame = GetCurrentAnimationFrame();
                    if (frame != null)
                    {
                        finalX += frame.OffsetX;
                        finalY += frame.OffsetY;
                    }

                    // ★ 最終サイズが確定したあとで CenterX を処理
                    if (CenterX)
                        finalX = (Common.CanvasWidth - drawW) / 2f;

                    // スプライト描画
                    commands.Add(new
                    {
                        type = "sprite",
                        canvasId = "gameCanvas",
                        imagePath = sprite.ImagePath,
                        x = finalX,
                        y = finalY,
                        sx = sprite.SourceRect.X,
                        sy = sprite.SourceRect.Y,
                        sw,
                        sh,
                        width = drawW,
                        height = drawH,
                        mirror = MirrorHorizontal,
                        tint = TintColor,
                        opacity = Opacity * fadeAlpha,
                        transparentColor = sprite.TransparentColor?.ToArgb(),
                        threshold = sprite.TransparentThreshold,
                        zIndex = this.ZIndex   // ★必ず追加
                    });

                    // テキスト描画
                    if (!string.IsNullOrEmpty(Text))
                    {
                        // テキストの論理的な幅と高さを推定
                        float textWidth = FontSize * Text.Length * 0.95f; // ← 係数を1.0～1.2で調整
                        float textHeight = FontSize * 1.4f;

                        // 左上基準の描画座標を計算
                        float textX = finalX + (drawW / 2f) - (textWidth / 2f) + TextOffsetX - 5.0f;
                        float textY = finalY + (drawH / 2f) - (textHeight / 2f) + TextOffsetY;

                        commands.Add(new
                        {
                            type = "text",
                            canvasId = "gameCanvas",
                            text = Text,
                            x = textX,  // 左端を指定
                            y = textY,  // 上端を指定
                            font = Font,
                            textColor = TextColor,
                            textAlign = "left", // ★ JSがleft/top固定なのでこのまま
                            opacity = Opacity * fadeAlpha,
                            zIndex = this.ZIndex   // ★必ず追加
                        });
                    }

                    // ヒット矩形更新
                    UpdateHitRect(finalX, finalY, drawW, drawH);
                }
            }
            // === スプライトなしでテキストのみ ===
            else if (!string.IsNullOrEmpty(Text))
            {
                float finalX = CenterX ? Common.CanvasWidth / 2f : drawX;

                commands.Add(new
                {
                    type = "text",
                    canvasId = "gameCanvas",
                    text = Text,
                    x = finalX + TextOffsetX,
                    y = drawY + TextOffsetY,
                    font = Font,
                    textColor = TextColor,
                    textAlign = TextAlign,
                    opacity = Opacity * fadeAlpha,
                    zIndex = this.ZIndex   // ★必ず追加
                });

                // 簡易ヒット矩形（テキスト幅推定）
                float approxWidth = FontSize * Text.Length * 0.6f;
                float approxHeight = FontSize * 1.4f;
                UpdateHitRect(finalX + TextOffsetX, drawY + TextOffsetY, approxWidth, approxHeight);
            }

            // === デバッグ枠 ===
            CollectDebugDrawCommands(commands);

        Children:
            foreach (var child in Children)
                child.CollectDrawCommands(commands, js, fadeOpacity, drawX, drawY);
        }

        /// <summary>
        /// ヒット矩形更新（ヒットテスト用に補正係数を掛ける）
        /// </summary>
        public void UpdateHitRect(float x, float y, float width, float height)
        {
            // 補正係数（必要に応じて調整）
            float hitWidthFactor = 1.0f;
            float hitHeightFactor = 1.0f;

            // 中心を維持するように補正
            float extraW = width * (hitWidthFactor - 1f);
            float extraH = height * (hitHeightFactor - 1f);

            float hitX = x - extraW / 2f;
            float hitY = y - extraH / 2f;
            float hitW = width * hitWidthFactor;
            float hitH = height * hitHeightFactor;

            HitRect = new RectangleF(hitX, hitY, hitW, hitH);
        }

        public virtual void CollectDebugDrawCommands(List<object> commands)
        {
#if false
            if (HitRect.Width > 0 && HitRect.Height > 0)
            {
                commands.Add(new
                {
                    type = "rect",
                    canvasId = "gameCanvas",
                    x = HitRect.X,
                    y = HitRect.Y,
                    width = HitRect.Width,
                    height = HitRect.Height,
                    strokeColor = "#FF0000",   // 赤い枠線
                    lineWidth = 1,
                    opacity = 0.8f,
                    zIndex = 999999            // 最前面
                });
            }
#endif
        }
    }
}
