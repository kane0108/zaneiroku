namespace BlazorApp.Game.Training
{
    /// <summary>
    /// 修練定義
    /// </summary>
    public class TrainingDefinition
    {
        public string Name { get; init; } = "";
        public string Category { get; init; } = "基礎"; // 基礎 / 複合
        public int ExpCost { get; init; }
        public string? RequiredItem { get; init; } // null = 印不要
        public int Column { get; init; }   // 0=左, 1=中央, 2=右
        public int Row { get; init; }      // カテゴリ内の行番号
        public int IconSheetX { get; init; }        // アイコンのスプライトシート位置

        public int IconSheetY { get; init; } = 64 * 4;   // アイコンのスプライトシート位置
                                                         // 実処理（反映用）
        public Action<Character, Character> ApplyEffect { get; init; } = (_, _) => { };

        public Func<Character, Character, Dictionary<string, (int left, int right)>>? PreviewEffect { get; init; }

        /// <summary>
        /// 実際の必要経験値を計算する
        /// </summary>
        public int CalcExpCost(Character p1, Character p2)
        {
            int total = p1.BaseStats.MaxHP + p1.BaseStats.Attack + p1.BaseStats.Defense
                      + p1.BaseStats.Speed + p1.BaseStats.Insight + p1.BaseStats.Confuse
                      + p2.BaseStats.MaxHP + p2.BaseStats.Attack + p2.BaseStats.Defense
                      + p2.BaseStats.Speed + p2.BaseStats.Insight + p2.BaseStats.Confuse;

            float cost = ExpCost * (1 + total / 200f);
            return (int)MathF.Ceiling(cost); // 切り上げ
        }
    }
}
