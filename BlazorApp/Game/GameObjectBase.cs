using BlazorApp.Game.Battle;
using Microsoft.JSInterop;

namespace BlazorApp.Game
{
    /// <summary>
    /// 全てのゲームオブジェクトの基底クラス（キャラ/UI共通）
    /// </summary>
    public class GameObjectBase
    {
        private int _objectId;

        /// <summary>
        /// オブジェクトID
        /// 通常はコンストラクタで自動採番されるが、
        /// キャラクター生成時など必要に応じて手動で設定可能。
        /// </summary>
        public int ObjectId
        {
            get => _objectId;
            set => _objectId = value; // 手動指定も許可
        }

        /// <summary>オブジェクト名（任意）</summary>
        public string Name { get; set; } = "";

        /// <summary>画像セット</summary>
        public GameObjectImage ImageSet { get; set; } = new();

        /// <summary>X座標</summary>
        public float PosX { get; set; } = 100;

        /// <summary>Y座標</summary>
        public float PosY { get; set; } = 300;

        /// <summary>不透明度（0.0～1.0）</summary>
        public float Opacity { get; set; } = 1f;

        /// <summary>描画優先度（小さいほど奥）</summary>
        public int ZIndex { get; set; } = 0;

        /// <summary>左右反転描画するか</summary>
        public bool MirrorHorizontal { get; set; } = false;

        /// <summary>色の乗算（16進カラーコード）</summary>
        public string TintColor { get; set; } = "#FFFFFF";

        // === アニメーション関連 ===

        /// <summary>アニメーション定義群</summary>
        public Dictionary<string, GameObjectAnimation> Animations { get; set; } = new();

        /// <summary>現在再生中のアニメーション名</summary>
        public string CurrentAnimationName { get; set; } = "idle";

        /// <summary>アニメーション経過時間</summary>
        public float AnimationTime { get; set; } = 0f;

        // === 入力イベント ===

        /// <summary>クリックイベント</summary>
        public Action? OnClick { get; set; }

        /// <summary>スワイプイベント（方向文字列: "左","右","上","下"）</summary>
        public Action<string>? OnSwipe { get; set; }

        /// <summary>アニメーション完了イベント</summary>
        public event Action<GameObjectBase, string>? OnAnimationCompleted;

        /// <summary>
        /// アニメーション完了通知
        /// </summary>
        protected void NotifyAnimationCompleted(string animName)
        {
            OnAnimationCompleted?.Invoke(this, animName);
        }

        /// <summary>
        /// コンストラクタ（ID自動採番）
        /// </summary>
        public GameObjectBase()
        {
            _objectId = ObjectIdGenerator.Next();
        }

        /// <summary>
        /// 現在のアニメーションフレームを取得
        /// </summary>
        public GameObjectAnimationFrame? GetCurrentAnimationFrame()
        {
            if (!Animations.TryGetValue(CurrentAnimationName, out var anim) || anim.Frames.Count == 0)
                return null;

            float totalDuration = anim.Frames.Sum(f => f.Duration);
            float t = AnimationTime;

            if (!anim.Loop)
                t = MathF.Min(t, totalDuration - 0.0001f);

            float accum = 0;
            foreach (var frame in anim.Frames)
            {
                if (t < accum + frame.Duration)
                    return frame;
                accum += frame.Duration;
            }

            return anim.Frames.Last();
        }

        /// <summary>
        /// 現在のフレームのスプライトを取得
        /// </summary>
        public Sprite? GetCurrentFrameSprite()
        {
            return GetCurrentAnimationFrame()?.Sprite;
        }

        private bool _waitAfterNonLoop = false;
        private int _postAnimationWaitFrames = 0;

        /// <summary>
        /// アニメーションを更新
        /// </summary>
        public virtual void UpdateAnimation(float deltaTime)
        {
            if (!Animations.TryGetValue(CurrentAnimationName, out var anim))
                return;

            // ループしないアニメーションの終了待ち
            if (_waitAfterNonLoop)
            {
                _postAnimationWaitFrames--;
                if (_postAnimationWaitFrames <= 0)
                {
                    _waitAfterNonLoop = false;
                    CurrentAnimationName = "idle";
                    AnimationTime = 0;
                }
                return;
            }

            // 速度倍率を考慮して経過時間を進める
            float multiplier = BattleScene.GlobalAnimationSpeedMultiplier;
            AnimationTime += deltaTime * multiplier;

            float totalDuration = anim.Frames.Sum(f => f.Duration);
            if (AnimationTime >= totalDuration)
            {
                if (anim.Loop)
                {
                    AnimationTime %= totalDuration;
                }
                else
                {
                    AnimationTime = totalDuration - 0.0001f;
                    _waitAfterNonLoop = true;
                    _postAnimationWaitFrames = 2;
                }
            }

            // 非ループアニメ終了時 → コールバック発火
            if (!anim.Loop && AnimationTime >= totalDuration)
            {
                NotifyAnimationCompleted(CurrentAnimationName);
            }
        }

        /// <summary>
        /// アニメーションを再生開始
        /// </summary>
        public void PlayAnimation(string name)
        {
            if (Animations.ContainsKey(name))
            {
                CurrentAnimationName = name;
                AnimationTime = 0f;
                _waitAfterNonLoop = false;
                _postAnimationWaitFrames = 0;
            }
        }

        /// <summary>
        /// 描画コマンドを収集（キャラクター／UI用）
        /// </summary>
        public virtual void CollectDrawCommands(
            List<object> commands, IJSRuntime js,
            float fadeOpacity = 0f, float parentX = 0, float parentY = 0)
        {
            // サブクラスでオーバーライド実装
        }
    }

    /// <summary>
    /// オブジェクトIDを一意に採番するジェネレータ
    /// </summary>
    public static class ObjectIdGenerator
    {
        private static int _nextId = 1;

        /// <summary>次のIDを取得</summary>
        public static int Next()
        {
            return _nextId++;
        }

        /// <summary>採番をリセット</summary>
        public static void Reset(int start = 1)
        {
            _nextId = start;
        }
    }
}
