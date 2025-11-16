using BlazorApp.Game.BackgroundFactory;

namespace BlazorApp.Game.Battle
{
    /// <summary>
    /// バトルシーン：フェーズごとの処理
    /// </summary>
    public partial class BattleScene
    {
        // === 初期セットアップ ===

        /// <summary>
        /// バトル開始時に一度だけ行う初期設定
        /// </summary>
        private void OneTimeSetup()
        {
            int id = 0;
            float xMargin = 10f;
            float yStart = 80f;
            float step = 120f;

            _ninguUsed.Clear();
            _ninguIcons.Clear();

            // 味方配置
            float y = yStart;
            foreach (var a in _setup.Allies)
            {
                a.ObjectId = id++;
                a.Type = "Player";
                a.MirrorHorizontal = false;
                a.PosX = xMargin;
                a.PosY = y;
                InitDefaultAndBattlePos(a);   // 初期位置を登録
                a.EnableFormationLerp = true;
                Characters[a.ObjectId] = a;

                y += step;

                Console.WriteLine($"[DEBUG] {a.Name} Speed={a.CurrentStats.Speed}");
            }

            // 敵配置
            y = yStart;
            foreach (var e in _setup.Enemies)
            {
                e.ObjectId = id++;
                e.Type = "Enemy";
                e.MirrorHorizontal = true;
                e.PosX = RightSideX(e) - xMargin;
                e.PosY = y;
                InitDefaultAndBattlePos(e);   // 初期位置を登録
                e.EnableFormationLerp = true;
                Characters[e.ObjectId] = e;

                y += step;
            }

            // 各キャラのステータス初期化
            foreach (var ch in Characters.Values)
            {
                // どちらの場合も手数はリセット
                ch.CurrentStats.ResetHands();

                // ターン終了時表示用にコピー
                ch.CurrentStats.EndOfTurnHands =
                    new Dictionary<AttackType, int>(ch.CurrentStats.RemainingHands);

                // ★ 表示用 HP を実値に即時同期
                ch.CurrentStats.DisplayResidual =
                    (float)ch.CurrentStats.ResidualHP / Math.Max(1, ch.CurrentStats.MaxHP);
                ch.CurrentStats.DisplayFatal =
                    (float)ch.CurrentStats.FatalHP / Math.Max(1, ch.CurrentStats.MaxHP);
            }

            // ATB初期化
            var actors = Characters.Values
                .Select(c => (c.ObjectId, speed: (float)c.CurrentStats.Speed))
                .ToList();
            if (actors.Any())
                _atb = new AtbTracker(actors, threshold: 100f);

            // UI生成
            CreateTestButton();
            CreateReservationSelectButton();
            CreatePredictionSelectButton();
            CreateConfirmReservationButton();
            CreateShobuButton();
            CreateKetchakuButton();
            CreateAllStatusPanels();

            // コンテキスト初期化
            _ctx.TurnNumber = 1;
            _ctx.CoverRemaining = 2;

            // UIリセット
            ClearTurnIcons();
        }

        /// <summary>
        /// キャラのデフォルト位置と初期バトル位置を初期化
        /// </summary>
        private void InitDefaultAndBattlePos(Character ch)
        {
            ch.DefaultPosX = ch.PosX;
            ch.DefaultPosY = ch.PosY;
            ch.BattlePosX = ch.PosX;
            ch.BattlePosY = ch.PosY;
        }

        // === ターン管理 ===

        /// <summary>
        /// ターン開始時の処理
        /// </summary>
        private void StartOfTurn()
        {
            _ctx.ReserveQueue.Clear();
            _ctx.PredictQueue.Clear();
            _ctx.CoverRemaining = 2;
            _evadeCount.Clear(); // 回避疲労リセット

            // ★ 今ターンの基準手数をスナップショット
            foreach (var ch in Characters.Values)
            {
                ch.CurrentStats.EndOfTurnHands =
                    new Dictionary<AttackType, int>(ch.CurrentStats.RemainingHands);
            }

            // ターン数表示
            ShowTurnTelop(_ctx.TurnNumber);
        }

        /// <summary>
        /// ターン終了後のクリーニング処理
        /// </summary>
        private void CleanupForNextTurn()
        {
            ClearTurnIcons(); // UIリセット

            // --- 一時バフリセット ---
            foreach (var ch in Characters.Values)
            {
                ch.CurrentStats.ClearTempBuffs();
            }

            _ctx.TurnNumber++;
        }

        // === 勝敗判定 ===

        /// <summary>
        /// 戦闘終了判定
        /// </summary>
        private BattleResult CheckBattleResult()
        {
            var players = Characters.Values.Where(c => c.Type == "Player").ToList();
            var enemies = Characters.Values.Where(c => c.Type == "Enemy").ToList();

            bool allPlayersDead = players.Any() && players.All(c => c.CurrentStats.IsDead);
            bool allEnemiesDead = enemies.Any() && enemies.All(c => c.CurrentStats.IsDead);

            if (allPlayersDead && allEnemiesDead)
                return BattleResult.Defeat; // 引き分けは暫定で敗北扱い

            if (allEnemiesDead)
                return BattleResult.Victory;

            if (allPlayersDead)
                return BattleResult.Defeat;

            return BattleResult.None;
        }

        // === 補助ユーティリティ ===

        /// <summary>
        /// キャラの右側X座標（配置計算用）
        /// </summary>
        private static float RightSideX(Character ch)
        {
            var sp = ch.GetCurrentFrameSprite();
            var w = (sp?.SourceRect.Width ?? 0) * (sp?.ScaleX ?? 1f);
            return Common.CanvasWidth - w;
        }
    }
}
