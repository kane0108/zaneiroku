using Microsoft.JSInterop;
using System.Drawing;

namespace BlazorApp.Game
{
    /// <summary>
    /// 描画対象となる画像スプライト情報を管理するクラス
    /// </summary>
    public class Sprite
    {
        /// <summary>画像ファイルのパス（wwwroot以下）</summary>
        public string ImagePath { get; set; }

        /// <summary>描画元画像の切り出し範囲</summary>
        public Rectangle SourceRect { get; set; }

        /// <summary>X方向の拡大縮小率</summary>
        public float ScaleX { get; set; } = 1.0f;

        /// <summary>Y方向の拡大縮小率</summary>
        public float ScaleY { get; set; } = 1.0f;

        /// <summary>透過色（指定色近傍を透明化）</summary>
        public Color? TransparentColor { get; set; }

        /// <summary>透過色の近似しきい値</summary>
        public int TransparentThreshold { get; set; } = 0;

        /// <summary>描画時の乗算カラー</summary>
        public string TintColor { get; set; } = "#FFFFFF";

        /// <summary>
        /// 初期化済みかどうか（画像サイズ取得済みかを示すフラグ）
        /// </summary>
        private bool _rectInitialized = false;

        /// <summary>
        /// プリロード済みサイズを保持する静的キャッシュ
        /// </summary>
        public static Dictionary<string, ImageSize> PreloadedSizes { get; set; }
            = new Dictionary<string, ImageSize>();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="imagePath">画像パス</param>
        /// <param name="sourceRect">切り出し範囲（省略時は未設定）</param>
        public Sprite(string imagePath, Rectangle? sourceRect = null)
        {
            ImagePath = imagePath;

            // バージョンを解決
            ImagePath = Common.VersionResolve(imagePath);

            //#if !DEBUG
            // リリース時はマゼンタ近傍を透過色として扱う
            TransparentColor = Color.Magenta;
            TransparentThreshold = 5;
//#endif

            if (sourceRect != null)
            {
                SourceRect = sourceRect.Value;
                _rectInitialized = true;
            }
            else if (PreloadedSizes.TryGetValue(imagePath, out var size))
            {
                // ★ プリロード済みサイズを即適用
                SourceRect = new Rectangle(0, 0, size.Width, size.Height);
                _rectInitialized = true;
            }
            else
            {
                // ★ プリロード漏れチェック
                GameMain.Instance.AddDebugMessage($"⚠ 未プリロード: {ImagePath}");

                // サイズ未定義。後で JS 経由で取得
                SourceRect = new Rectangle(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// JS経由で画像サイズを取得（非同期）
        /// </summary>
        public async Task EnsureImageSizeAsync(IJSRuntime js)
        {
            if (_rectInitialized) return;

            // まずプリロード済みキャッシュを参照
            if (PreloadedSizes.TryGetValue(ImagePath, out var size))
            {
                SourceRect = new Rectangle(0, 0, size.Width, size.Height);
                _rectInitialized = true;
                return;
            }

            // フォールバック: JS に問い合わせ
            var size2 = await js.InvokeAsync<ImageSize>("getImageSize", ImagePath);
            SourceRect = new Rectangle(0, 0, size2.Width, size2.Height);
            _rectInitialized = true;
        }

        /// <summary>
        /// JS経由で画像サイズを取得（同期的に呼び出し）
        /// </summary>
        public void EnsureImageSizeLazy(IJSRuntime js)
        {
            if (_rectInitialized) return;

            _ = EnsureImageSizeAsync(js); // 非同期呼び出しを無視して進行
        }

        /// <summary>
        /// 画像サイズ情報
        /// </summary>
        public class ImageSize
        {
            /// <summary>幅</summary>
            public int Width { get; set; }

            /// <summary>高さ</summary>
            public int Height { get; set; }
        }
    }
}
