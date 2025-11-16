using BlazorApp.Game.CharacterFactory;
using Microsoft.JSInterop;
using System.Text.Json;

namespace BlazorApp.Game
{
    public static class SaveManager
    {
        // ★単一データ制：キーは1つ
        private const string STORAGE_KEY = "FZ_SaveData_Encrypted_v1";

        // Program.cs でセットする
        public static IJSRuntime? JS { get; set; }

        // JSONオプション（循環など避ける用）
        private static readonly JsonSerializerOptions JsonOpt = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = true
        };

        public static async Task<bool> ExistsAsync()
        {
            string? v = await GetItemAsync(STORAGE_KEY);
            return !string.IsNullOrEmpty(v);
        }

        public static async Task SaveAsync()
        {
            var data = BuildSaveDataFromRuntime();
            string json = JsonSerializer.Serialize(data, JsonOpt);
            string payload = await SaveCrypto.EncryptAndSignAsync(json);
            await SetItemAsync(STORAGE_KEY, payload);
#if DEBUG
            Console.WriteLine("Save completed.");
#endif
        }

        public static async Task<bool> LoadAsync()
        {
            string? payload = await GetItemAsync(STORAGE_KEY);
            if (string.IsNullOrEmpty(payload)) return false;

            string json = await SaveCrypto.DecryptAndVerifyAsync(payload);
            var data = JsonSerializer.Deserialize<SaveData>(json, JsonOpt);
            if (data == null) return false;

            SaveDataMigration.MigrateIfNeeded(data);
            ApplySaveDataToRuntime(data);
            return true;
        }

        public static async Task DeleteAsync()
        {
            await RemoveItemAsync(STORAGE_KEY);
        }

        // ===== バックアップ入出力 =====

        /// <summary>暗号化済み文字列（Base64＋署名付き）をそのままエクスポート</summary>
        public static async Task<string> ExportBase64Async()
        {
            // 保存を最新化してから出力しても良い
            await SaveAsync();
            string? payload = await GetItemAsync(STORAGE_KEY);
            return payload ?? "";
        }

        /// <summary>エクスポート文字列をインポート（署名検証→復号→適用→保存）</summary>
        public static async Task<bool> ImportBase64Async(string payload)
        {
            try
            {
                string json = await SaveCrypto.DecryptAndVerifyAsync(payload);
                var data = JsonSerializer.Deserialize<SaveData>(json, JsonOpt);
                if (data == null) return false;

                SaveDataMigration.MigrateIfNeeded(data);
                ApplySaveDataToRuntime(data);

                // 正常に適用できたのでそのまま保存
                await SetItemAsync(STORAGE_KEY, payload);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ===== 実体 -> SaveData へ詰める =====
        private static SaveData BuildSaveDataFromRuntime()
        {
            var sd = new SaveData
            {
                Version = 1,
                Progress = (int)Common.CurrentProgress,
                PlayerExp = GameMain.Instance.PlayerExp,
                PlayerMoney = GameMain.Instance.PlayerMoney,
                Switches = new(GameSwitchManager.Instance.Export())
            };

            // キャラ
            foreach (var ch in GameMain.Instance.PlayerParty)
            {
                var c = new CharacterSave
                {
                    Name = ch.Name,
                    Stats = ch.BaseStats.Clone(),
                    WeaponId = EquipmentManager.Instance.GetEquippedId(ch, "武器"),
                    NinguId = EquipmentManager.Instance.GetEquippedId(ch, "忍具"),
                };
                sd.Characters.Add(c);
            }

            // アイテム
            foreach (var item in ItemManager.Instance.GetAllItems())
                sd.Items[item.Id] = item.Count;

            // 装備
            foreach (var eq in EquipmentManager.Instance.All)
            {
                sd.Equipments[eq.Id] = new EquipmentSave
                {
                    ForgeWeight = eq.ForgeWeight,
                    ForgeSharp = eq.ForgeSharp,
                    Unlocked = EquipmentManager.Instance.IsUnlocked(eq.Id)
                };
            }

            return sd;
        }

        // ===== SaveData -> 実体へ反映 =====
        private static void ApplySaveDataToRuntime(SaveData sd)
        {
            // --- 基本情報 ---
            Common.CurrentProgress = (Common.StoryProgress)sd.Progress;
            GameMain.Instance.SetExp(sd.PlayerExp);
            GameMain.Instance.SetMoney(sd.PlayerMoney);
            GameSwitchManager.Instance.Import(sd.Switches);

            // --- 1) アイテム・装備全体を先に復元 ---
            ItemManager.Instance.ApplyCounts(sd.Items);
            EquipmentManager.Instance.ApplySaveData(sd.Equipments);

            // --- 2) キャラ情報取得 ---
            var rekkaSave = sd.Characters.FirstOrDefault(c => c.Name == "烈火");
            var sayaSave = sd.Characters.FirstOrDefault(c => c.Name == "沙耶");

            // --- 3) パーティ再構築 ---
            GameMain.Instance.PlayerParty.Clear();

            // ===== 烈火 =====
            var rekka = GameInitializer.Create<CharacterPlayer00, Character>();
            if (rekkaSave != null)
            {
                ApplyStatsFromSave(rekka, rekkaSave);

                if (!string.IsNullOrEmpty(rekkaSave.WeaponId))
                    EquipmentManager.Instance.Equip(rekka, "武器", rekkaSave.WeaponId);

                if (!string.IsNullOrEmpty(rekkaSave.NinguId))
                    EquipmentManager.Instance.Equip(rekka, "忍具", rekkaSave.NinguId);
            }
            GameMain.Instance.PlayerParty.Add(rekka);

            // ===== 沙耶 =====
            var saya = GameInitializer.Create<CharacterPlayer01, Character>();
            if (sayaSave != null)
            {
                ApplyStatsFromSave(saya, sayaSave);

                if (!string.IsNullOrEmpty(sayaSave.WeaponId))
                    EquipmentManager.Instance.Equip(saya, "武器", sayaSave.WeaponId);

                if (!string.IsNullOrEmpty(sayaSave.NinguId))
                    EquipmentManager.Instance.Equip(saya, "忍具", sayaSave.NinguId);
            }
            GameMain.Instance.PlayerParty.Add(saya);
        }

        /// <summary>
        /// ファクトリで生成されたキャラに、セーブ済みステータスだけを上書きする
        /// （画像・アニメ・表情などはキャラクラス側の初期値を尊重）
        /// </summary>
        private static void ApplyStatsFromSave(Character ch, CharacterSave cs)
        {
            var s = cs.Stats;
            var bs = ch.BaseStats;

            bs.MaxHP = s.MaxHP;
            bs.ResidualHP = s.ResidualHP;
            bs.FatalHP = s.FatalHP;

            bs.Attack = s.Attack;
            bs.Defense = s.Defense;
            bs.Speed = s.Speed;
            bs.Insight = s.Insight;
            bs.Confuse = s.Confuse;
            bs.Intelligence = s.Intelligence;
            bs.MaxReservationPerTurn = s.MaxReservationPerTurn;

            bs.MaxHands = new Dictionary<AttackType, int>(s.MaxHands);
            bs.RemainingHands = new Dictionary<AttackType, int>(s.RemainingHands);

            // UI用HP補間値はここで再計算しておく
            if (bs.MaxHP > 0)
            {
                bs.DisplayResidual = (float)bs.ResidualHP / bs.MaxHP;
                bs.DisplayFatal = (float)bs.FatalHP / bs.MaxHP;
            }
            else
            {
                bs.DisplayResidual = 0f;
                bs.DisplayFatal = 0f;
            }

            ch.CurrentStats = bs.Clone();
        }

        // ===== LocalStorage（JS） =====
        private static ValueTask<string?> GetItemAsync(string key)
            => JS!.InvokeAsync<string?>("localStorage.getItem", key);

        private static ValueTask<object?> SetItemAsync(string key, string value)
            => JS!.InvokeAsync<object?>("localStorage.setItem", key, value);

        private static ValueTask<object?> RemoveItemAsync(string key)
            => JS!.InvokeAsync<object?>("localStorage.removeItem", key);
    }
}
