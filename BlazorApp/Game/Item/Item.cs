namespace BlazorApp.Game
{
    public class Item : ISkillHolder
    {
        /// <summary>アイテムID兼名称（「鎚鉄」など）</summary>
        public string Id { get; set; } = "";

        /// <summary>説明文（改行対応）</summary>
        public string Description { get; set; } = "";

        /// <summary>所持数</summary>
        public int Count { get; set; } = 0;

        /// <summary>スプライトシート共通パス</summary>
        public string SpriteSheet { get; set; } = "images/ui04-00.png";

        /// <summary>スプライトの切り出し位置</summary>
        public int SrcX { get; set; }
        public int SrcY { get; set; }

        /// <summary>
        /// スキル
        /// </summary>
        public List<Skill> Skills { get; set; } = new();

        public Item(string id, string description, int gridX, int gridY, int count = 0)
        {
            Id = id;
            Description = description;
            SrcX = gridX * 64;
            SrcY = gridY * 64;
            Count = count;
        }
    }
}
