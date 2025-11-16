namespace BlazorApp.Game.Stealth
{
    /// <summary>
    /// 隠密探索盤ステージ設定
    /// </summary>
    public sealed class StealthBoardSetup
    {
        /// <summary>盤の横マス数</summary>
        public int Width { get; init; } = 10;

        /// <summary>盤の縦マス数</summary>
        public int Height { get; init; } = 8;

        /// <summary>罠の種類と数</summary>
        public Dictionary<TrapType, int> Traps { get; init; } = new();

        /// <summary>
        /// Allies を渡す
        /// </summary>
        public List<Character> Allies { get; set; } = new();

        /// <summary>探索盤に配置される敵（暗殺や回避が可能）</summary>
        public List<Character> StealthEnemies { get; set; } = new();

        /// <summary>必ず戦闘に登場する敵（探索で除外できない）</summary>
        public List<Character> ForcedEnemies { get; set; } = new();

        /// <summary>探索背景</summary>
        public Background Background { get; set; } = new();

        /// <summary>戦闘背景</summary>
        public Background BattleBackground { get; set; } = new();

        /// <summary>このステージの表示名</summary>
        public string StageName { get; set; } = "隠密任務・初陣";

        // ★追加: 天候
        public WeatherType Weather { get; init; } = WeatherType.Normal;

        // ★追加: 何マスごとに古いマスをリセットするか
        public int WeatherRevealThreshold { get; init; } = 10;

        // --- 報酬関連 ---
        public int TotalMoney { get; set; } = 0;                   // 総額
        public Dictionary<string, int> ItemDropTable { get; set; } = new();
        public Dictionary<string, int> PerfectItemDropTable { get; set; } = new();
        public int MaxItemDrops { get; set; } = 1;


        public int BaseExpPerDig { get; set; } = 5; // 1マス掘ったときの基礎経験値
    }

    /// <summary>
    /// 罠の種類
    /// </summary>
    public enum TrapType
    {
        PoisonDart, // 毒矢
        BearTrap,   // トラバサミ
    }

    /// <summary>
    /// 天候
    /// </summary>
    public enum WeatherType
    {
        Normal,
        Stormy,
    }
}
