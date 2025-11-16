namespace BlazorApp.Game
{
    /// <summary>
    /// キャラクタ残像クラス
    /// </summary>
    public class CharacterGhost
    {
        public Sprite Sprite { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Opacity { get; set; } = 0.5f;
        public float LifeTime { get; set; } = 0.3f; // 秒
        public float Elapsed { get; set; } = 0f;
    }
}
