public class StealthRewardGenerator
{
    private readonly Random _rand = new();

    /// <summary>
    /// ゴールドを割合で算出（拾った枠数に応じて）
    /// </summary>
    public int GenerateGold(int totalGold, int totalSlots, int pickedSlots)
    {
        if (totalSlots <= 0 || pickedSlots <= 0) return 0;

        // 枠ごとの基準値
        int baseValue = totalGold / totalSlots;
        int remainder = totalGold % totalSlots;

        // 全枠分の配分リストを作成
        var slotValues = Enumerable.Repeat(baseValue, totalSlots).ToList();
        for (int i = 0; i < remainder; i++)
            slotValues[i] += 1; // 端数処理

        // 実際に拾った枠数ぶんだけ合計
        int pickedTotal = slotValues.Take(pickedSlots).Sum();
        return pickedTotal;
    }

    /// <summary>
    /// アイテム抽選
    /// </summary>
    public List<string> GenerateItems(
        Dictionary<string, int> normalTable,
        Dictionary<string, int> perfectTable,
        int normalCount,
        int perfectCount)
    {
        var results = new List<string>();

        // --- 通常分 ---
        results.AddRange(DrawFromTable(normalTable, normalCount));

        // --- パーフェクト枠 ---
        if (perfectCount > 0 && perfectTable.Count > 0)
        {
            results.AddRange(DrawFromTable(perfectTable, perfectCount));
        }


        return results;
    }

    private List<string> DrawFromTable(Dictionary<string, int> table, int count)
    {
        var results = new List<string>();
        if (table == null || table.Count == 0) return results;

        for (int i = 0; i < count; i++)
        {
            int roll = _rand.Next(0, 100);
            int accum = 0;
            foreach (var kv in table)
            {
                accum += kv.Value;
                if (roll < accum)
                {
                    results.Add(kv.Key);
                    break;
                }
            }
        }
        return results;
    }
}

/// <summary>
/// 戦闘フェーズに渡す報酬リスト
/// </summary>
public class StealthRewardResult
{
    public int Gold { get; set; }                 // 最終的に得られた金額
    public List<string> ItemIds { get; set; } = new(); // 抽選済みアイテムIDのリスト
}
