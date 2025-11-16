using Microsoft.JSInterop;

namespace BlazorApp.Game
{
    /// <summary>
    /// キャラクター（敵/味方共通）
    /// </summary>
    public class Character : GameObjectBase,  ISkillHolder
    {
        // === 基本情報 ===

        /// <summary>敵/味方などの分類</summary>
        public string Type { get; set; } = "Player";

        /// <summary>基礎ステータス</summary>
        public CharacterStats BaseStats { get; set; } = new();

        /// <summary>現在のステータス</summary>
        public CharacterStats CurrentStats { get; set; } = new();

        /// <summary>回避時固定再生するアニメーション名</summary>
        public string FixedEvadeAnim { get; set; } = "回避";

        /// <summary>通常時の描画優先度</summary>
        public int BaseZIndex { get; set; } = 0;

        /// <summary>攻撃アニメーション再生中かどうか</summary>
        public bool IsAnimatingAttack { get; private set; } = false;

        // === 立ち絵管理 ===
        /// <summary>会話用立ち絵画像</summary>
        public string? PortraitImagePath { get; set; }

        /// <summary>立ち絵画像番号（切取座標X）</summary>
        public int PortraitId { get; set; } = 0;

        /// <summary>立ち絵表情番号（切取座標Y）</summary>
        public int CurrentExpressionId { get; set; } = 0;

        // === 座標管理 ===

        /// <summary>初期位置（座標）</summary>
        public float DefaultPosX { get; set; }
        public float DefaultPosY { get; set; }

        /// <summary>陣形移動の目標位置</summary>
        public float BattlePosX { get; set; }
        public float BattlePosY { get; set; }

        /// <summary>陣形補間を有効化するか</summary>
        public bool EnableFormationLerp { get; set; } = false;

        // === 残像制御 ===

        /// <summary>残像を有効化するか</summary>
        public bool EnableAfterImage { get; set; } = false;

        private readonly Queue<CharacterGhost> _afterImages = new();
        private float _afterImageInterval = 0.05f;
        private float _afterImageTimer = 0f;
        private const int MaxAfterImages = 20; // 残像の最大数

        /// <summary>
        /// スキル
        /// </summary>
        public List<Skill> Skills { get; set; } = new();

        // === 更新処理 ===

        /// <summary>
        /// アニメーション更新（残像や死亡時アニメも含む）
        /// </summary>
        public override void UpdateAnimation(float deltaTime)
        {
            base.UpdateAnimation(deltaTime);

            // 死亡時は強制的に "dead" 再生
            if (CurrentStats.IsDead)
            {
                if (CurrentAnimationName != "dead" && Animations.ContainsKey("dead"))
                {
                    PlayAnimation("dead");
                    ZIndex = BaseZIndex; // ★ここで元の値に戻す
                }
            }

            // 陣形補間処理
            if (EnableFormationLerp)
            {
                float speed = 5f;
                PosX += (BattlePosX - PosX) * speed * deltaTime;
                PosY += (BattlePosY - PosY) * speed * deltaTime;
            }

            // 残像処理
            if (EnableAfterImage)
            {
                _afterImageTimer += deltaTime;
                if (_afterImageTimer >= _afterImageInterval)
                {
                    var sprite = GetCurrentFrameSprite();
                    if (sprite != null)
                    {
                        _afterImages.Enqueue(new CharacterGhost
                        {
                            Sprite = sprite,
                            X = PosX,
                            Y = PosY,
                            Opacity = 0.6f
                        });

                        // 古い残像を削除して数を制限
                        while (_afterImages.Count > MaxAfterImages)
                            _ = _afterImages.Dequeue();
                    }
                    _afterImageTimer = 0f;
                }

                foreach (var ghost in _afterImages.ToList())
                {
                    ghost.Elapsed += deltaTime;
                    ghost.Opacity = MathF.Max(0f, ghost.Opacity - deltaTime * 1.5f);
                }

                while (_afterImages.Count > 0 &&
                       (_afterImages.Peek().Elapsed >= _afterImages.Peek().LifeTime ||
                        _afterImages.Peek().Opacity <= 0f))
                {
                    _ = _afterImages.Dequeue();
                }
            }

            // idle に戻ったら ZIndex をリセット
            if (IsAnimatingAttack && CurrentAnimationName == "idle")
            {
                ZIndex = BaseZIndex;
                IsAnimatingAttack = false;
            }
        }

        /// <summary>
        /// 描画コマンドを収集
        /// </summary>
        public override void CollectDrawCommands(
            List<object> commands, IJSRuntime js, float fadeOpacity = 0f,
            float parentX = 0, float parentY = 0)
        {
            // 残像を先に描画
            if (EnableAfterImage)
            {
                foreach (var ghost in _afterImages)
                {
                    var sp = ghost.Sprite;
                    if (sp == null) continue;

                    sp.EnsureImageSizeLazy(js);
                    int sw = sp.SourceRect.Width;
                    int sh = sp.SourceRect.Height;
                    if (sw <= 0 || sh <= 0) continue;

                    commands.Add(new
                    {
                        type = "sprite",
                        canvasId = "gameCanvas",
                        imagePath = sp.ImagePath,
                        x = ghost.X,
                        y = ghost.Y,
                        sx = sp.SourceRect.X,
                        sy = sp.SourceRect.Y,
                        sw,
                        sh,
                        width = sw * sp.ScaleX,
                        height = sh * sp.ScaleY,
                        mirror = MirrorHorizontal,
                        tint = "#FFFFFF",
                        opacity = ghost.Opacity * (1.0f - fadeOpacity),
                        transparentColor = sp.TransparentColor?.ToArgb(),
                        threshold = sp.TransparentThreshold,
                        zIndex = ZIndex
                    });
                }
            }

            // 本体キャラ描画
            var sprite = GetCurrentFrameSprite();
            if (sprite == null) return;

            sprite.EnsureImageSizeLazy(js);
            int swMain = sprite.SourceRect.Width;
            int shMain = sprite.SourceRect.Height;
            if (swMain <= 0 || shMain <= 0) return;

            float drawX = parentX + PosX;
            float drawY = parentY + PosY;

            var frame = GetCurrentAnimationFrame();
            if (frame != null)
            {
                drawX += MirrorHorizontal ? -frame.OffsetX : frame.OffsetX;
                drawY += frame.OffsetY;
            }

            float drawW = swMain * sprite.ScaleX;
            float drawH = shMain * sprite.ScaleY;

            commands.Add(new
            {
                type = "sprite",
                canvasId = "gameCanvas",
                imagePath = sprite.ImagePath,
                x = drawX,
                y = drawY,
                sx = sprite.SourceRect.X,
                sy = sprite.SourceRect.Y,
                sw = swMain,
                sh = shMain,
                width = drawW,
                height = drawH,
                mirror = MirrorHorizontal,
                tint = "#FFFFFF",
                opacity = Opacity * (1.0f - fadeOpacity),
                transparentColor = sprite.TransparentColor?.ToArgb(),
                threshold = sprite.TransparentThreshold,
                zIndex = ZIndex
            });
        }

        /// <summary>
        /// 攻撃アニメーションを再生し、ZIndexを前面化
        /// </summary>
        public void PlayAttackAnimation(string animName)
        {
            PlayAnimation(animName);
            ZIndex = 200;  // 攻撃中は最前面
            IsAnimatingAttack = true;
        }

        /// <summary>
        /// 粗い矩形ベースのタップ判定
        /// </summary>
        public bool HitTest(float x, float y)
        {
            var sp = GetCurrentFrameSprite();
            if (sp == null) return false;

            float w = sp.SourceRect.Width * (sp.ScaleX == 0 ? 1f : sp.ScaleX);
            float h = sp.SourceRect.Height * (sp.ScaleY == 0 ? 1f : sp.ScaleY);

            float left = PosX;
            float top = PosY;

            return (x >= left && x <= left + w &&
                    y >= top && y <= top + h);
        }

        /// <summary>
        /// 厳密なピクセル単位ヒット判定（透過色考慮）
        /// </summary>
        public async Task<bool> IsHitPreciseAsync(float x, float y, IJSRuntime js)
        {
            var sprite = GetCurrentFrameSprite();
            if (sprite == null) return false;

            var frame = GetCurrentAnimationFrame();

            // 1. 描画開始位置（左上基準）
            float drawX = PosX;
            float drawY = PosY;
            if (frame != null)
            {
                drawX += frame.OffsetX;
                drawY += frame.OffsetY;
            }

            // 2. ローカル座標に変換
            float localX = (x - drawX) / sprite.ScaleX;
            float localY = (y - drawY) / sprite.ScaleY;

            // 3. ミラー反転
            if (MirrorHorizontal)
                localX = sprite.SourceRect.Width - localX;

            // 4. 範囲外判定
            if (localX < 0 || localY < 0 ||
                localX >= sprite.SourceRect.Width ||
                localY >= sprite.SourceRect.Height)
            {
                return false;
            }

            // 5. JSに渡して透過チェック
            return await js.InvokeAsync<bool>("checkPixelHit", new
            {
                imagePath = sprite.ImagePath,
                sx = sprite.SourceRect.X,
                sy = sprite.SourceRect.Y,
                sw = sprite.SourceRect.Width,
                sh = sprite.SourceRect.Height,
                scaleX = sprite.ScaleX,
                scaleY = sprite.ScaleY,
                px = localX,
                py = localY,
                transparentColor = sprite.TransparentColor?.ToArgb(),
                threshold = sprite.TransparentThreshold
            });
        }
    }
}
