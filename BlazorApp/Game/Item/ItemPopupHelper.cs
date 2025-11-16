using BlazorApp.Game.UIObjectFactory;

namespace BlazorApp.Game
{
    public static class ItemPopupHelper
    {
        private static UIObject? _mask, _window, _icon, _text;

        private static CancellationTokenSource? _cts; // タイムアウト管理用

        public static void Show(Scene scene, Item item, int handsThrust = 0, int handsSlash = 0, int handsDown = 0)
        {
            // 既存ポップアップを閉じる
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            // === マスク ===
            _mask = new UIObject
            {
                Name = "SkillPopupMask",
                PosX = 0,
                PosY = 0,
                ZIndex = 1000000,
                Opacity = 0.6f,
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

            // 背景ウィンドウ
            _window = new UIObject
            {
                Name = "ItemPopupWindow",
                CenterX = true,
                PosY = 45,
                ZIndex = 1000100,
                Animations = new Dictionary<string, GameObjectAnimation> {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite("images/ui01-00.png",
                                    new System.Drawing.Rectangle(0, 0, 360, 180))
                                {
                                    ScaleX = 1.0f,
                                    ScaleY = 0.82f,
                                }
                            }
                        }
                    }}
                }
            };
            scene.AddUI(_window);

            // 拡大アイコン
            _icon = new UIObject
            {
                Name = "ItemPopupIcon",
                PosX = 20,
                PosY = 80,
                ZIndex = 1000200,
                Animations = new Dictionary<string, GameObjectAnimation> {
                    { "idle", new GameObjectAnimation {
                        Frames = new List<GameObjectAnimationFrame> {
                            new GameObjectAnimationFrame {
                                Sprite = new Sprite(item.SpriteSheet,
                                    new System.Drawing.Rectangle(item.SrcX, item.SrcY, 64,64)) {
                                    ScaleX = 1.1f,
                                    ScaleY = 1.1f
                                }
                            }
                        }
                    }}
                }
            };
            scene.AddUI(_icon);

            var text = $"【{item.Id}】\n\n{item.Description}";

            if (handsThrust != 0 || handsSlash != 0 || handsDown != 0)
            {
                text += $"\n　穿×{handsThrust} 迅×{handsSlash} 剛×{handsDown}";
            }

            // 説明文
            _text = new UIObjectMultilineText
            {
                Name = "ItemPopupText",
                PosX = 100,
                PosY = 70,
                ZIndex = 1000200,
                FontSize = 11,
                TextColor = "#FFFFFF",
                Text = text
            };
            scene.AddUI(_text);

            // === タイムアウト自動閉じ ===
            _ = AutoCloseAsync(scene, _cts.Token);
        }

        private static async Task AutoCloseAsync(Scene scene, CancellationToken token)
        {
            try
            {
                await Task.Delay(4000, token);
                if (!token.IsCancellationRequested)
                    Close(scene);
            }
            catch (TaskCanceledException) { /* 安全に無視 */ }
        }

        public static void Close(Scene scene)
        {
            _cts?.Cancel();
            _cts = null;

            if (_mask != null) scene.RemoveUI(_mask);
            if (_window != null) scene.RemoveUI(_window);
            if (_icon != null) scene.RemoveUI(_icon);
            if (_text != null) scene.RemoveUI(_text);

            _mask = _window = _icon = _text = null;
        }
    }
}
