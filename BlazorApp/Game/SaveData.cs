using System.Text.Json.Serialization;

namespace BlazorApp.Game
{
    /// <summary>将来の互換維持のためバージョンを必ず持つ</summary>
    public class SaveData
    {
        public int Version { get; set; } = 1;

        public int Progress { get; set; }
        public int PlayerExp { get; set; }
        public int PlayerMoney { get; set; }

        public List<CharacterSave> Characters { get; set; } = new();

        // アイテム所持数（Id -> Count）
        public Dictionary<string, int> Items { get; set; } = new();

        // 装備の鍛錬・アンロック状態（Id -> Save）
        public Dictionary<string, EquipmentSave> Equipments { get; set; } = new();

        // スイッチ（ON/OFF）
        public Dictionary<string, bool> Switches { get; set; } = new();

        // 進行に関わる拡張（例：クリア済みステージ）
        public List<string>? ClearedStages { get; set; } = new();
    }

    public class CharacterSave
    {
        public string Name { get; set; } = "";
        public CharacterStats Stats { get; set; } = new();
        public string? WeaponId { get; set; }
        public string? NinguId { get; set; }
    }

    public class EquipmentSave
    {
        public int ForgeWeight { get; set; }
        public int ForgeSharp { get; set; }
        public bool Unlocked { get; set; }
    }

    public static class SaveDataMigration
    {
        /// <summary>古いSaveDataを新バージョンへマイグレーション</summary>
        public static SaveData MigrateIfNeeded(SaveData data)
        {
            if (data.Version < 1)
            {
                data.Version = 1;
            }

            // v2以降でフィールド追加したら↓のように初期化
            // if (data.Version < 2) { data.ClearedStages ??= new(); data.Version = 2; }

            return data;
        }
    }
}
