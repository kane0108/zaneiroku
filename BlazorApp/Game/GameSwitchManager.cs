namespace BlazorApp.Game
{
    /// <summary>
    /// ゲームスイッチ状態の管理（ON/OFF）
    /// </summary>
    public class GameSwitchManager
    {
        public static GameSwitchManager Instance { get; } = new();

        private readonly Dictionary<string, bool> _switches = new();

        private GameSwitchManager() { }

        public void Set(string key, bool value) => _switches[key] = value;
        public void SetOn(string key) => Set(key, true);
        public void SetOff(string key) => Set(key, false);
        public bool IsOn(string key) => _switches.TryGetValue(key, out var v) && v;
        public void Clear() => _switches.Clear();

        public IReadOnlyDictionary<string, bool> Export() => _switches;
        public void Import(Dictionary<string, bool> data)
        {
            _switches.Clear();
            foreach (var kv in data) _switches[kv.Key] = kv.Value;
        }
    }
}
