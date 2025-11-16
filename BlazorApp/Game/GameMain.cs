using BlazorApp.Game.Battle;
using BlazorApp.Game.CharacterFactory;
using BlazorApp.Game.SceneFactory;
using BlazorApp.Game.UIObjectFactory;
using Microsoft.JSInterop;

namespace BlazorApp.Game
{
    /// <summary>
    /// ゲームメイン処理クラス（シーン管理・フェード制御など）
    /// </summary>
    public class GameMain
    {
        // === シングルトン ===
        public static GameMain Instance { get; private set; } = new GameMain();

        // === プレイヤー管理 ===
        /// <summary>
        /// プレイヤーパーティ
        /// </summary>
        public List<Character> PlayerParty { get; } = new();

        /// <summary>
        /// 経験値
        /// </summary>
        public int PlayerExp { get; set; } = 0;
        private const int ExpMax = 999999;

        /// <summary>
        /// 所持金
        /// </summary>
        public int PlayerMoney { get; private set; } = 0;
        private const int MoneyMax = 999999;

        // === シーン管理 ===
        public Scene? CurrentScene { get; private set; }
        private SceneFactoryResolver? _resolver;

        // === フェード関連 ===
        public float FadeOpacity { get; set; } = 0f;
        public FadeState FadeState { get; set; } = FadeState.None;
        public float FadeSpeed { get; set; } = 2.0f;
        private GameState _nextState;
        private object? _nextPayload;
        private bool _pendingChange = false;
        public Action? OnFadeInCompleted;

        // === ATB / ゲージ関連 ===
        public int AtbMaxChars { get; private set; } = 50;  // 既定値
        public float GaugeTargetPx { get; set; } = 50f;     // 100%時の幅
        public float GaugePaddingPx { get; set; } = 8f;     // 内側余白
        public string GaugeFont { get; set; } = "1px monospace";

        // === イベント ===
        public event Action<GameState>? OnStateChanged;

        /// <summary>現在選択中のエリア</summary>
        public AreaId CurrentArea { get; set; } = AreaId.None;

        // デバッグメッセージ
        public List<string> DebugMessages { get; } = new();

        /// <summary>
        /// デバッグメッセージ追加
        /// </summary>
        public void AddDebugMessage(string msg)
        {
#if DEBUG
            DebugMessages.Add(msg);
            Console.WriteLine(msg);
#endif
        }

        /// <summary>
        /// デバッグメッセージクリア
        /// </summary>
        public void ClearDebugMessages()
        {
            DebugMessages.Clear();
        }

        /// <summary>
        /// ゲーム初期化（シーンファクトリ登録）
        /// </summary>
        public void Initialize()
        {
            FadeOpacity = 0f;
            FadeState = FadeState.None;

            // 各SceneFactoryを登録
            var factories = new List<BaseSceneFactory>
            {
                new SceneTitle(),
                new SceneHome(),
                new SceneStageSelectWorld(),
                new SceneStageSelectArea(),
                new SceneStealthBoard(),
                new SceneCombat(),
                new SceneStatus(),
                new SceneForge(),
                new SceneTestMode(),
            };
            _resolver = new SceneFactoryResolver(factories);

            // 初期シーンを生成
            var scene = _resolver.CreateScene(GameState.Title);
            ApplyScene(scene);
        }

        // === フェード付き状態遷移 ===

        public void StartFadeTransition(GameState toState) =>
            StartFadeTransition(toState, payload: null);

        public void StartFadeTransition(GameState toState, object? payload)
        {
            if (FadeState != FadeState.None || _pendingChange) return; // 二重開始防止

            _nextState = toState;
            _nextPayload = payload;
            FadeState = FadeState.FadingOut;
            FadeOpacity = 0f;
            _pendingChange = true;
        }

        // === 毎フレーム更新 ===

        public void Update(float deltaTime)
        {
            UpdateFade(deltaTime);

            // フェードアウト中はシーン更新を止める
            if (FadeState == FadeState.FadingOut)
                return;

            CurrentScene?.Update(deltaTime);
        }

        private void UpdateFade(float deltaTime)
        {
            switch (FadeState)
            {
                case FadeState.FadingOut:
                    FadeOpacity += FadeSpeed * deltaTime;
                    if (FadeOpacity >= 1f)
                    {
                        FadeOpacity = 1f;

                        // フェードアウト完了でシーン切替
                        ChangeState(_nextState, _nextPayload);
                        _nextPayload = null;

                        FadeState = FadeState.FadingIn;
                        _pendingChange = false;
                    }
                    break;

                case FadeState.FadingIn:
                    FadeOpacity -= FadeSpeed * deltaTime;
                    if (FadeOpacity <= 0f)
                    {
                        FadeOpacity = 0f;
                        FadeState = FadeState.None;
                        OnFadeInCompleted?.Invoke();
                    }
                    break;
            }
        }

        public bool IsFading => FadeState != FadeState.None;

        // === 状態遷移 ===

        public void ChangeState(GameState newState)
        {
            if (_resolver == null) throw new Exception("Resolver not initialized.");
            var scene = _resolver.CreateScene(newState);
            ApplyScene(scene);
            OnStateChanged?.Invoke(newState);
        }

        public void ChangeState(GameState newState, object? payload)
        {
            if (_resolver == null) throw new Exception("Resolver not initialized.");
            var scene = _resolver.CreateScene(newState, payload);
            ApplyScene(scene);
            OnStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// シーン切替処理
        /// </summary>
        private void ApplyScene(Scene scene)
        {
            CurrentScene = scene;

            // 切替直後に全オブジェクトをDirty化
            scene.Background?.MarkDirty();
            foreach (var ui in scene.UIObjects.Values) ui.MarkDirty();
            foreach (var ui in scene.CommonUIObjects.Values) ui.MarkDirty();
        }

        // === 入力系 ===

        /// <summary>
        /// スワイプ入力処理
        /// </summary>
        public void OnSwipe(string direction)
        {
#if DEBUG
            Console.WriteLine($"スワイプ方向: {direction}");
#endif
            if (IsFading) return;

            (CurrentScene as BattleScene)?.OnSwipeInBattle(direction);

            // テストモード限定のデバッグ操作
            if (CurrentScene?.State == GameState.TestMode)
            {
                var c = GetCharacter(1);
                if (c != null)
                {
                    switch (direction)
                    {
                        case "右": c.PosX += 10; break;
                        case "左": c.PosX -= 10; break;
                        case "下": c.PosY += 10; break;
                        case "上": c.PosY -= 10; break;
                    }
                }
            }
        }

        /// <summary>
        /// UIヒット判定（前面→背面）
        /// </summary>
        public static UIObject? GetHitUI(Scene? scene, float x, float y)
        {
            if (scene == null) return null;

            IEnumerable<UIObject> EnumerateAll()
            {
                foreach (var u in scene.UIObjects.Values) yield return u;

                var common = scene.GetType()
                                  .GetProperty("CommonUIObjects")
                                  ?.GetValue(scene) as Dictionary<int, UIObject>;
                if (common != null)
                    foreach (var u in common.Values) yield return u;
            }

            foreach (var ui in EnumerateAll().OrderByDescending(u => u.ZIndex).ToList())
            {
                var hit = ui.HitTestRecursive(x, y);
                if (hit != null) return hit;
            }

            return null;
        }

        /// <summary>
        /// キャラ矩形のラフヒット判定
        /// </summary>
        public static IEnumerable<Character> GetHitCharacterRough(Scene? scene, float x, float y)
        {
            if (scene == null) yield break;

            foreach (var ch in scene.Characters.Values.Reverse().ToList())
            {
                if (ch.HitTest(x, y))
                    yield return ch;
            }
        }

        /// <summary>
        /// キャラクタ取得
        /// </summary>
        public Character? GetCharacter(int id) =>
            CurrentScene?.Characters.TryGetValue(id, out var ch) == true ? ch : null;

        public void SetAtbMaxChars(int value) =>
            AtbMaxChars = Math.Max(1, value);

        /// <summary>
        /// 経験値加算
        /// </summary>
        public void AddExp(int amount, bool useEffect = true)
        {
            if (amount <= 0) return;

            var diff = PlayerExp;
            PlayerExp = Math.Min(ExpMax, PlayerExp + amount);
            diff = PlayerExp - diff;

            CurrentScene?.UpdateGlobalExpUI();

            if (useEffect)
            {
                CurrentScene?.ShowExpChange(diff);
            }
        }

        /// <summary>
        /// 経験値減算
        /// </summary>
        public void SubtractExp(int amount, bool useEffect = true)
        {
            if (amount <= 0) return;

            var diff = PlayerExp;
            PlayerExp = Math.Max(0, PlayerExp - amount);
            diff = PlayerExp - diff;

            CurrentScene?.UpdateGlobalExpUI();

            if (useEffect)
            {
                CurrentScene?.ShowExpChange(diff);
            }
        }

        /// <summary>
        /// 経験値セット
        /// </summary>
        public void SetExp(int value)
        {
            PlayerExp = Math.Clamp(value, 0, ExpMax);

            CurrentScene?.UpdateGlobalExpUI();
        }

        /// <summary>
        /// 所持金加算
        /// </summary>
        public void AddMoney(int amount, bool useEffect = true)
        {
            if (amount <= 0) return;

            var diff = PlayerMoney;
            PlayerMoney = Math.Min(MoneyMax, PlayerMoney + amount);
            diff = PlayerMoney - diff;

            CurrentScene?.UpdateGlobalMoneyUI();

            if (useEffect)
            {
                CurrentScene?.ShowMoneyChange(diff);
            }
        }

        /// <summary>
        /// 所持金減算
        /// </summary>
        public void SubtractMoney(int amount, bool useEffect = true)
        {
            if (amount <= 0) return;

            var diff = PlayerMoney;
            PlayerMoney = Math.Max(0, PlayerMoney - amount);
            diff = PlayerMoney - diff;

            CurrentScene?.UpdateGlobalMoneyUI();

            if (useEffect)
            {
                CurrentScene?.ShowMoneyChange(diff);
            }
        }

        /// <summary>
        /// 所持金セット
        /// </summary>
        public void SetMoney(int value)
        {
            PlayerMoney = Math.Clamp(value, 0, MoneyMax);
            CurrentScene?.UpdateGlobalMoneyUI();
        }
    }

    /// <summary>
    /// フェード状態
    /// </summary>
    public enum FadeState
    {
        None,
        FadingOut,
        FadingIn
    }

    /// <summary>
    /// エリア定義
    /// </summary>
    public enum AreaId
    {
        None,
        Village,
        Forest,
        Castle,
        Mountain,
        Snow,
    }
}
