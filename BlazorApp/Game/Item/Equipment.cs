namespace BlazorApp.Game
{
    public class Equipment : ISkillHolder
    {
        public string Id { get; set; } = "";       // 一意ID
        public string Category { get; set; } = ""; // "武器" or "忍具"
        public string Description { get; set; } = "";
        public string SpriteSheet { get; set; } = "images/ui04-00.png";
        public int SrcX { get; set; }
        public int SrcY { get; set; }

        // ステータス補正
        public int Hp { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        public int Insight { get; set; }
        public int Confuse { get; set; }

        // ★追加: 武器の手数
        public int HandsThrust { get; set; } // 穿
        public int HandsSlash { get; set; } // 迅
        public int HandsDown { get; set; } // 剛

        // ★追加: 傾向
        public string Trend { get; set; } = "";

        /// <summary>
        /// 重量
        /// </summary>
        public int ForgeWeight { get; set; } = 0; // -10～10
        /// <summary>
        /// 鋭さ
        /// </summary>
        public int ForgeSharp { get; set; } = 0; // -10～10

        /// <summary>
        /// スキル
        /// </summary>
        public List<Skill> Skills { get; set; } = new();

        public NinguPhaseType? UsePhase { get; set; } // 忍具以外は null

        public Equipment(string id, string desc, int srcX, int srcY, string category)
        {
            Id = id;
            Description = desc;
            Category = category;
            SrcX = srcX * 64;
            SrcY = srcY * 64;
        }
    }

    public enum NinguPhaseType
    {
        Reservation,
        Prediction,
        AnyTime,
    }
}
