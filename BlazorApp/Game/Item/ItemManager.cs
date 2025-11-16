namespace BlazorApp.Game
{
    public class ItemManager
    {
        public static ItemManager Instance { get; } = new ItemManager();

        private readonly Dictionary<string, Item> _items = new();

        private ItemManager() { }

        public void InitializeDefaultItems()
        {
            _items.Clear();

            // === 武器鍛錬素材 ===
            Register(new Item("鎚鉄",
                "自然に何度も打ち延ばされた鉄。\n" +
                "重みを増し、武器を重厚化する下地となる。\n" +
                "(武器を5段階まで「重く」するために1つ消費)",
                2, 5));

            Register(new Item("玄鉄",
                "黒々と輝く伝説的な鉄鉱。\n" +
                "極めて硬く重く、武器に圧倒的な重厚さを与える。\n" +
                "(武器を6段階以上「重く」するために1つ消費)",
                3, 5));

            Register(new Item("羽鋼",
                "羽のように軽いと伝えられる希少な鋼。\n" +
                "武器を軽快に扱えるようになる。\n" +
                "(武器を5段階まで「軽く」するために1つ消費)",
                4, 5));

            Register(new Item("天羽布",
                "神代の羽衣にたとえられる金属布。\n" +
                "鋼を超える軽さで、自在に振るえるようにする。\n" +
                "(武器を6段階以上「軽く」するために1つ消費)",
                5, 5));

            Register(new Item("名倉砥",
                "名工が用いる仕上げ砥石。\n" +
                "刀身を鋭く磨き上げ、切れ味を大きく高める。\n" +
                "(武器を5段階まで「鋭く」するために1つ消費)",
                6, 5));

            Register(new Item("神砥",
                "神域で産出したとされる神秘の砥石。\n" +
                "刃を極限まで鋭利に整える。\n" +
                "(武器を6段階以上「鋭く」するために1つ消費)",
                7, 5));

            Register(new Item("荒砥",
                "粗い粒子の砥石。\n" +
                "仕上げには向かないが、\n" +
                "刀身に荒々しい切り口を与える。\n" +
                "(武器を5段階まで「粗く」するために1つ消費)",
                8, 5));

            Register(new Item("鬼砥",
                "鬼が使ったと伝えられる伝説の荒砥。\n" +
                "粗雑だが強靭で、裂け目深い一撃を刻む。\n"+
                "(武器を6段階以上「粗く」するために1つ消費)",
                9, 5));

            // === 修練の印 ===
            Register(new Item("攻防の印",
                "攻防修練を行うための符。\n" + 
                "攻める力と守る力を共に鍛える修行に用いられる。\n"+
                "(「攻防修練」を1度実施するたびに1つ消費)",
                2, 4));

            Register(new Item("剣速の印",
                "剣速修練を行うための符。\n" +
                "素早い攻めと、それに耐える力を\n" +
                "鍛える修行に用いられる。\n" +
                "(「剣速修練」を1度実施するたびに1つ消費)",
                3, 4));

            Register(new Item("心胆の印",
                "心胆修練を行うための符。\n" +
                "胆力と洞察を磨く修行に用いられる。\n" +
                "(「心胆修練」を1度実施するたびに1つ消費)",
                4, 4));

            Register(new Item("攪乱の印",
                "攪乱修練を行うための符。\n" +
                "翻弄とそれを見破る眼を鍛える修行に用いられる。\n" +
                "(「攪乱修練」を1度実施するたびに1つ消費)",
                5, 4));

            Register(new Item("虚心の印",
                "虚心修練を行うための符。\n" +
                "心を空にし、偏りなく力を養う修行に用いられる。\n" +
                "(「虚心修練」を1度実施するたびに1つ消費)",
                6, 4));

            Register(new Item("鍛身の印",
                "鍛身修練を行うための符。\n" +
                "体力と基礎防御を高める修行に用いられる。\n" +
                "(「鍛身修練」を1度実施するたびに1つ消費)",
                7, 4));

            Register(new Item("皆伝の印",
                "皆伝修練を行うための符。\n"+
                "全ての力を効率的に高める。\n" +
                "(「皆伝修練」を1度実施するたびに1つ消費)",
                1, 5));            
        }

        // === 基本操作 ===

        public Item? Get(string id) =>
            _items.TryGetValue(id, out var item) ? item : null;

        public int GetCount(string id) => Get(id)?.Count ?? 0;

        private void Register(Item item)
        {
            item.Count = 0; // 初期所持数はゼロ
            _items[item.Id] = item;
        }

        public void Add(string id, int count = 1)
        {
            if (_items.TryGetValue(id, out var item))
            {
                item.Count += count;
            }
        }

        public bool Consume(string id, int amount)
        {
            if (!_items.ContainsKey(id)) return false;
            if (_items[id].Count < amount) return false;

            _items[id].Count -= amount;
            return true;
        }

        public IEnumerable<Item> GetAllItems() => _items.Values;

        /// <summary>
        /// セーブデータから反映
        /// </summary>
        public void ApplyCounts(Dictionary<string, int> counts)
        {
            // いったん全リセット（必要ならやり方変更）
            foreach (var item in GetAllItems())
                item.Count = 0;

            foreach (var kv in counts)
            {
                var itm = FindById(kv.Key); // ★必要なら自作：Idで検索
                if (itm != null) itm.Count = Math.Max(0, kv.Value);
            }
        }
        public Item? FindById(string id) => GetAllItems().FirstOrDefault(i => i.Id == id);

    }
}
