using BlazorApp.Game.UIObjectFactory;

namespace BlazorApp.Game
{
    /// <summary>
    /// スキル説明ポップアップ（ItemPopupHelper準拠）
    /// </summary>
    public static class SkillPopupHelper
    {
        private static UIObject? _mask, _window, _text;

        private static CancellationTokenSource? _cts; // タイムアウト管理用

        /// <summary>
        /// スキル詳細ポップアップを表示
        /// </summary>
        public static void Show(Scene scene, Skill skill)
        {
            // === 既存ポップアップが出ていたら即キャンセル（再入保護） ===
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            // === 一旦クリア（念のため） ===
            if (_mask != null) scene.RemoveUI(_mask);
            if (_window != null) scene.RemoveUI(_window);
            if (_text != null) scene.RemoveUI(_text);

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

            // === 背景ウィンドウ ===
            _window = new UIObject
            {
                Name = "SkillPopupWindow",
                CenterX = true,
                PosY = 35,
                ZIndex = 1000200,
                Animations = new Dictionary<string, GameObjectAnimation>
                {
                    {
                        "idle", new GameObjectAnimation
                        {
                            Frames = new List<GameObjectAnimationFrame>
                            {
                                new GameObjectAnimationFrame
                                {
                                    Sprite = new Sprite("images/ui01-00.png",
                                        new System.Drawing.Rectangle(0, 0, 360, 180))
                                    {
                                        ScaleX = 1.0f,
                                        ScaleY = 0.4f,
                                    }
                                }
                            }
                        }
                    }
                }
            };
            scene.AddUI(_window);

            // === 説明文 ===
            _text = new UIObjectMultilineText
            {
                Name = "SkillPopupText",
                PosX = 20,
                PosY = 50,
                ZIndex = 1000300,
                FontSize = 11,
                TextColor = "#FFFFFF",
                Text = $"【{skill.DisplayName}】\n　{skill.GetDescription()}"
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

        /// <summary>
        /// スキルポップアップを閉じる
        /// </summary>
        public static void Close(Scene scene)
        {
            _cts?.Cancel();
            _cts = null;

            if (_mask != null) scene.RemoveUI(_mask);
            if (_window != null) scene.RemoveUI(_window);
            if (_text != null) scene.RemoveUI(_text);

            _mask = _window = _text = null;
        }
    }
}
