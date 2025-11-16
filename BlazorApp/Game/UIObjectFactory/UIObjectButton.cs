namespace BlazorApp.Game.UIObjectFactory
{
    public class UIObjectButton : BaseUIObjectFactory
    {
        private float _x, _y;
        private bool _centerX;
        private string _text = "";
        private string _textColor = "#FFFFFF";
        private int _fontSize = 20;
        private int _textOffsetX = 5;
        private int _textOffsetY = 5;
        private string _fontFamily = "sans-serif";
        private float _opacity = 1.0f;
        private Action? _onClick;
        private Dictionary<string, GameObjectAnimation>? _animations;
        private string _defaultAnim = "idle";

        public override void SetParameters(Dictionary<string, object> parameters)
        {
            _x = Get<float>(parameters, "x");
            _y = Get<float>(parameters, "y");
            _centerX = Get<bool>(parameters, "centerX");
            _text = Get<string>(parameters, "text");
            _textColor = Get<string>(parameters, "textColor", "#FFFFFF");
            _fontSize = Get<int>(parameters, "fontSize", 20);
            _textOffsetX = Get<int>(parameters, "textOffsetX", 5);
            _textOffsetY = Get<int>(parameters, "textOffsetY", 5);
            _fontFamily = Get<string>(parameters, "fontFamily", "sans-serif");
            _opacity = Get<float>(parameters, "opacity", 1.0f);
            _onClick = Get<Action>(parameters, "onClick", null);
            _animations = Get<Dictionary<string, GameObjectAnimation>>(parameters, "animations", null);
            _defaultAnim = Get<string>(parameters, "defaultAnim", "idle");
        }

        public override UIObject Create()
        {
            var animations = _animations;

            // ✅ アニメーション未指定ならデフォルトセット
            if (animations == null || animations.Count == 0)
            {
                animations = new Dictionary<string, GameObjectAnimation>
                {
                    { "idle", GameObjectAnimation.CreateIdleButtonAnim() }
                };
                _defaultAnim = "idle";
            }

            return new UIObject
            {
                PosX = _x,
                PosY = _y,
                CenterX = _centerX,
                Text = _text,
                TextColor = _textColor,
                FontSize = _fontSize,
                FontFamily = _fontFamily,
                TextOffsetX = _textOffsetX,
                TextOffsetY = _textOffsetY,
                StretchToText = true,
                Opacity = _opacity,
                Animations = animations,
                CurrentAnimationName = _defaultAnim,
                OnClick = _onClick
            };
        }

        private T Get<T>(Dictionary<string, object> dict, string key, T fallback = default!)
        {
            return dict.TryGetValue(key, out var val) && val is T t ? t : fallback;
        }

    }
}
