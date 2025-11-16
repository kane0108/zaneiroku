// DamagePopup.cs
using BlazorApp.Game.UIObjectFactory;

namespace BlazorApp.Game.Battle
{
    /// <summary>
    /// ダメージ数値ポップアップ
    /// </summary>
    public class DamagePopup : UIObject
    {
        private float _lifetime = 1.0f; // 1秒で消える
        private float _elapsed = 0f;
        private float _riseSpeed = 30f; // 上昇速度(px/秒)

        public DamagePopup(int value, bool isFatal)
        {
            Name = $"DamagePopup_{Guid.NewGuid()}";
            Text = value.ToString();
            FontSize = 14;
            FontFamily = "monospace";
            TextAlign = "center";
            TextColor = isFatal ? "#FF0000" : "#FFFF00"; // 赤=致命 / 黄=残痕
            Opacity = 1.0f;
            ZIndex = 30000; // キャラより前面に
        }

        public override void UpdateRecursive(float deltaTime)
        {
            base.UpdateRecursive(deltaTime);

            _elapsed += deltaTime;
            PosY -= _riseSpeed * deltaTime; // 上に移動
            Opacity = MathF.Max(0f, 1f - _elapsed / _lifetime);

            if (_elapsed >= _lifetime)
            {
                // シーンから削除
                GameMain.Instance.CurrentScene.RemoveUI(this);
            }
        }
    }
}
