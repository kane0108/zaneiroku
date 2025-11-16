namespace BlazorApp.Game.Battle
{
    public sealed class AtbTracker
    {
        private readonly float _threshold;
        private readonly Dictionary<int, float> _gauge = new();
        private readonly Queue<int> _turnQueue = new();

        public Dictionary<int, float> Speed { get; } = new();
        private float _scalePerSec = 1f; // ★ 追加：1秒あたりスケール

        // ★ 満タンでキューに積まれている回数（表示ラッチ用）
        private readonly Dictionary<int, int> _queuedCount = new();

        public AtbTracker(IEnumerable<(int id, float speed)> actors, float threshold = 100f, float targetSecondsForAvg = 1.5f)
        {
            _threshold = threshold;
            foreach (var (id, spd) in actors)
            {
                var s = MathF.Max(spd, 0.01f);
                Speed[id] = s;
                _gauge[id] = 0f;
            }

            // ★ 平均速度をもとにスケーリング：平均キャラが targetSecondsForAvg 秒で満タン
            var avg = Speed.Values.Average();
            _scalePerSec = (_threshold / MathF.Max(0.01f, avg)) / MathF.Max(0.1f, targetSecondsForAvg);
            // 例）avg=20, threshold=100, T=3 → scalePerSec ≈ 1.6667
        }

        /// <summary>
        /// ATBゲージを進める（死亡者はゼロ固定）
        /// </summary>
        public void Tick(float dt, Dictionary<int, Character> characters)
        {
            foreach (var id in characters.Keys)
            {
                var ch = characters[id];

                if (ch.CurrentStats.IsDead)
                {
                    _gauge[id] = 0f;
                    continue;
                }

                if (!_gauge.ContainsKey(id))
                    _gauge[id] = 0f;

                // ✅ 常に最新の CurrentStats.Speed を参照
                float currentSpeed = MathF.Max(ch.CurrentStats.Speed, 0.01f);
                _gauge[id] += currentSpeed * _scalePerSec * dt;

                while (_gauge[id] >= _threshold)
                {
                    _gauge[id] -= _threshold;
                    _turnQueue.Enqueue(id);
                    if (!_queuedCount.ContainsKey(id)) _queuedCount[id] = 0;
                    _queuedCount[id]++;
                }
            }
        }

        public bool TryPop(out int actorId)
        {
            if (_turnQueue.Count > 0) { actorId = _turnQueue.Dequeue(); return true; }
            actorId = -1; return false;
        }

        // ★ 表示は、キューに溜まっていれば 1.0 を返す（ピタ止め）
        public float GetFill01(int id)
        {
            if (_queuedCount.TryGetValue(id, out var n) && n > 0) return 1f;
            return MathF.Min(1f, _gauge[id] / _threshold);
        }

        // ★ そのキャラの “1回ぶんのラッチ” を消費（表示を解除）
        public void ConsumeLatchedFull(int id)
        {
            if (_queuedCount.TryGetValue(id, out var n) && n > 0)
                _queuedCount[id] = n - 1;

            // ★ 追加: 既に積まれている turnQueue も 1 回ぶん消費
            if (_turnQueue.Count > 0 && _turnQueue.Peek() == id)
                _turnQueue.Dequeue();
        }
    }

}
