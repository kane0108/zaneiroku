namespace BlazorApp.Game.UIObjectFactory
{
    public class UIObjectTelop : BaseUIObjectFactory
    {
        private float _x, _y;
        private bool _centerX;
        private string _text = "";
        private string _textColor = "#FFFFFF";
        private int _fontSize = 20;
        private string _fontFamily = "sans-serif";

        /// <summary>
        /// UIパラメータ設定
        /// </summary>
        public override void SetParameters(Dictionary<string, object> parameters)
        {
            _x = Get<float>(parameters, "x");
            _y = Get<float>(parameters, "y");
            _centerX = Get<bool>(parameters, "centerX");
            _text = Get<string>(parameters, "text");
            _textColor = Get<string>(parameters, "textColor", "#FFFFFF");
            _fontSize = Get<int>(parameters, "fontSize", 20);
            _fontFamily = Get<string>(parameters, "fontFamily", "sans-serif");
        }

        public override UIObject Create()
        {
            return new UIObject
            {
                PosX = _x,
                PosY = _y,
                CenterX= _centerX,
                Text = _text,
                TextColor = _textColor,
                FontSize = _fontSize,
                FontFamily = _fontFamily,
                StretchToText = false
            };
        }

        private T Get<T>(Dictionary<string, object> dict, string key, T fallback = default!)
        {
            return dict.TryGetValue(key, out var val) && val is T t ? t : fallback;
        }
    }
}
