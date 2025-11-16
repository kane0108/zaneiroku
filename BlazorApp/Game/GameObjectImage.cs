namespace BlazorApp.Game
{
    public class GameObjectImage
    {
        /// <summary>状態に応じたスプライトマップ</summary>
        public Dictionary<string, Sprite> Sprites { get; set; } = new();

        /// <summary>状態タグからスプライトを取得</summary>
        public Sprite? GetSprite(string tag)
        {
            return Sprites.TryGetValue(tag, out var sprite) ? sprite : null;
        }
    }
}
