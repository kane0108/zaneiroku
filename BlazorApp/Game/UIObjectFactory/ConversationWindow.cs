using BlazorApp.Game.Battle;
using BlazorApp.Game.UIObjectFactory;

namespace BlazorApp.Game
{
    /// <summary>
    /// 会話ウィンドウUI（戦闘・探索など共通で使用）
    /// </summary>
    public class ConversationWindow
    {
        private readonly Queue<string> _pages = new();

        private TaskCompletionSource<bool>? _tcs;
        
        private UIObject? _window, _text;
        private UIObject? _portrait, _namePlate;

        private Action? _onFinished;

        private float _windowHeight = 180f;
        private float _posY;

        private static UIObject? _mask;
        private static bool _isMaskFading = false;  // 暗幕フェード実行中フラグ
        private static CancellationTokenSource? _maskFadeCts;

        /// <summary>
        /// Character を渡すバージョン
        /// </summary>
        public Task ShowAsync(Scene scene, IEnumerable<string> lines, Character? speaker = null,
                              bool mirrorRight = false, float? posY = null)
        {
            return ShowAsync(scene, lines, isAlly: speaker.Type == "Player",
                             portraitPath: speaker.PortraitImagePath,
                             portraitId: speaker.PortraitId,
                             expressionId: speaker.CurrentExpressionId,
                             mirrorRight: mirrorRight,
                             charName: speaker.Name,
                             posY: posY);
        }

        /// <summary>
        /// 会話ウィンドウを表示する
        /// </summary>
        /// <param name="scene">対象シーン</param>
        /// <param name="lines">表示テキスト</param>
        /// <param name="isAlly">味方カラーか</param>
        /// <param name="portraitPath">立ち絵画像のパス（省略可）</param>
        /// <param name="mirrorRight">右側に配置するか</param>
        /// <param name="charName">キャラ名（省略可）</param>]
        public Task ShowAsync(Scene scene, IEnumerable<string> lines, bool isAlly,
                           string? portraitPath = null, int portraitId = 0, int expressionId = 0, bool mirrorRight = false,
                           string? charName = null, float? posY = null)
        {
            // ★ 前の暗幕フェードをキャンセル
            if (_maskFadeCts != null)
            {
                _maskFadeCts.Cancel();
                _maskFadeCts.Dispose();
                _maskFadeCts = null;
                _isMaskFading = false;
            }

            _tcs = new TaskCompletionSource<bool>();

            _pages.Clear();
            foreach (var line in lines) _pages.Enqueue(line);

            _windowHeight = 180f;
            _posY = posY ?? (Common.CanvasHeight - _windowHeight); // デフォルトは下寄せ

            // --- 背景マスク（暗幕＋誤入力ブロック） ---
            // ★ 既にシーンに追加されている場合は再Addしない
            if (_mask == null)
            {
                _mask = new UIObject
                {
                    Name = "ConversationMask",
                    PosX = 0,
                    PosY = 0,
                    ZIndex = 4000000,
                    Opacity = 0.6f,
                    TintColor = "#000000",
                    Enabled = true,
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        { "idle", new GameObjectAnimation {
                            Frames = new List<GameObjectAnimationFrame> {
                                new GameObjectAnimationFrame {
                                    Sprite = new Sprite("images/ui06-01.png",
                                        new System.Drawing.Rectangle(0,0,(int)Common.CanvasWidth,(int)Common.CanvasHeight))
                                }
                            }
                        }}
                    }
                };
                scene.AddUI(_mask);
            }
            else
            {
                // すでにある場合は透明度だけリセットして使い回す
                _mask.Opacity = 0.6f;
                _mask.MarkDirty();
            }

            // --- ウィンドウ画像 ---
            _window = new UIObject
            {
                Name = "ConversationWindow",
                PosX = 0,
                PosY = _posY,
                ZIndex = 4100000,
                Opacity = 0.8f,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-00.png",
                                    new System.Drawing.Rectangle(isAlly ? 0 : 360, 0, 360, 180))
                            }
                        }
                    }}
                },
                OnClick = () => NextPage(scene) // タップで進む
            };
            scene.AddUI(_window);

            // --- テキスト ---
            _text = new UIObjectMultilineText
            {
                Name = "ConversationText",
                PosX = 30,
                PosY = _posY + 30,
                ZIndex = 4200000,
                TextColor = "#FFFFFF",
                FontSize = 14,
                TextAlign = "left",
                Enabled = false,
                Opacity = 0.8f
            };
            scene.AddUI(_text);

            // --- 立ち絵（省略可能）---
            if (!string.IsNullOrEmpty(portraitPath))
            {
                _portrait = new UIObject
                {
                    Name = "ConversationPortrait",
                    ZIndex = 4050000,
                    PosX = mirrorRight ? Common.CanvasWidth - 160 : 0,
                    PosY = _posY - 160, // ウィンドウより上に
                    MirrorHorizontal = mirrorRight,
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        { "idle", new GameObjectAnimation {
                            Frames = new List<GameObjectAnimationFrame> {
                                new GameObjectAnimationFrame {
                                    Sprite = new Sprite(portraitPath,
                                        new System.Drawing.Rectangle(portraitId*180,expressionId*180,180,180)) {
                                        ScaleX = 1.0f, ScaleY = 1.0f
                                    }
                                }
                            }
                        }}
                    }
                };
                scene.AddUI(_portrait);
            }

            // --- キャラ名プレート（固定デザイン＋文字） ---
            if (!string.IsNullOrEmpty(charName))
            {
                _namePlate = new UIObject
                {
                    Name = "ConversationNamePlate",
                    ZIndex = 4250000,
                    PosX = mirrorRight ? Common.CanvasWidth - 110 : 20,
                    PosY = _posY - 10, // ウィンドウに少しかぶせる
                    Text = charName,
                    FontSize = 10,
                    TextOffsetX = 5f,
                    TextOffsetY = 2f,
                    TextColor = "#FFFFFF",
                    TextAlign = "center",
                    Animations = new Dictionary<string, GameObjectAnimation>
                    {
                        { "idle", new GameObjectAnimation {
                            Loop = false,
                            Frames = GameObjectAnimation.CreateFramesFromSheet(
                                "images/ui01-00.png", 360, 180, 1, 0f, scaleX:0.25f, scaleY:0.1f
                            )
                        }}
                    },
                };
                scene.AddUI(_namePlate);
            }

            // 最初のページを表示
            NextPage(scene);

            return _tcs.Task; // 完了待ちできる
        }

        /// <summary>
        /// ページ送り処理
        /// </summary>
        private void NextPage(Scene scene)
        {
            if (_pages.Count > 0)
            {
                if (_text != null)
                {
                    _text.Text = _pages.Dequeue();
                    _text.MarkDirty();
                }
            }
            else
            {
                _ = CloseWithFadeAsync(scene);
            }
        }

        /// <summary>
        /// フェードアウトして閉じる
        /// </summary>
        private async Task CloseWithFadeAsync(Scene scene)
        {
            const int steps = 3;
            const int delayMs = 30;

            for (int i = 0; i <= steps; i++)
            {
                float alpha = 1f - i / (float)steps;

                if (_window != null) { _window.Opacity = alpha; _window.MarkDirty(); }
                if (_text != null) { _text.Opacity = alpha; _text.MarkDirty(); }
                if (_portrait != null) { _portrait.Opacity = alpha; _portrait.MarkDirty(); }
                if (_namePlate != null) { _namePlate.Opacity = alpha; _namePlate.MarkDirty(); }

                await Task.Delay(delayMs);
            }

            // --- UI削除 ---
            if (_window != null) scene.RemoveUI(_window);
            if (_text != null) scene.RemoveUI(_text);
            if (_portrait != null) scene.RemoveUI(_portrait);
            if (_namePlate != null) scene.RemoveUI(_namePlate);

            _window = null;
            _text = null;
            _portrait = null;
            _namePlate = null;

            // 暗幕は遅延フェード → awaitの外で
            if (_mask != null && !_isMaskFading)
            {
                _isMaskFading = true;
                _maskFadeCts = new CancellationTokenSource();
                var token = _maskFadeCts.Token;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(200, token); // 遅延開始
                        for (int i = 0; i <= steps; i++)
                        {
                            token.ThrowIfCancellationRequested();

                            float alpha = 1f - i / (float)steps;
                            _mask.Opacity = 0.6f * alpha;
                            _mask.MarkDirty();
                            await Task.Delay(delayMs, token);
                        }

                        // フェードが終わったら削除
                        scene.RemoveUI(_mask);
                        _mask = null;
                    }
                    catch (OperationCanceledException)
                    {
                        // キャンセルされた場合は何もしない（新しい会話が始まった）
                    }
                    finally
                    {
                        _isMaskFading = false;
                    }
                }, token);
            }

            _tcs?.TrySetResult(true);
        }
    }
}
