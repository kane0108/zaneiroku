using System.Drawing;

namespace BlazorApp.Game
{
    /// <summary>
    /// ゲームオブジェクトアニメーションクラス
    /// </summary>
    public class GameObjectAnimation
    {
        public bool Loop { get; set; } = true;

        public List<GameObjectAnimationFrame> Frames { get; set; } = new();

        public float TotalTime => Frames.Sum(f => f.Duration);

        /// <summary>
        /// アニメーション・スプライトシートからの取り込み
        /// </summary>
        public static List<GameObjectAnimationFrame> CreateFramesFromSheet(
        string path, int frameWidth, int frameHeight,
        int count, float duration, int offsetX = 0, int offsetY = 0, float scaleX = 1f, float scaleY = 1f)
        {
            var frames = new List<GameObjectAnimationFrame>();

            for (int i = 0; i < count; i++)
            {
                var rect = new Rectangle(offsetX, offsetY + i * frameHeight, frameWidth, frameHeight);
                frames.Add(new GameObjectAnimationFrame
                {
                    Sprite = new Sprite(path, rect)
                    {
                        ScaleX = scaleX,
                        ScaleY = scaleY,
                    },
                    Duration = duration
                });
            }

            return frames;
        }

        /// <summary>
        /// ボタン待機アニメ初期値の設定
        /// </summary>
        public static GameObjectAnimation CreateIdleButtonAnim()
        {
            return new GameObjectAnimation
            {
                Loop = true,
                Frames = CreateFramesFromSheet("images/ui01-00.png", 360, 180, count: 1, duration: 0.0f)
            };
        }
    }

    /// <summary>
    /// アニメーションフレームクラス
    /// </summary>
    public class GameObjectAnimationFrame
    {
        /// <summary>
        /// 使用するスプライト
        /// </summary>
        public Sprite Sprite { get; set; }

        /// <summary>
        /// 秒数
        /// </summary>
        public float Duration { get; set; } = 0.2f;

        /// <summary>
        /// 行動時の移動オフセットX
        /// </summary>
        public float OffsetX { get; set; } = 0;     // 攻撃の移動など

        /// <summary>
        /// 行動時の移動オフセットY
        /// </summary>
        public float OffsetY { get; set; } = 0;
    }
}
