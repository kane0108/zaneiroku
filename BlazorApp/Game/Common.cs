namespace BlazorApp.Game
{
    /// <summary>
    /// ゲーム全体で共通利用する定数やユーティリティ
    /// </summary>
    public static class Common
    {
        public const int GlobalUiBaseId = 900000;
        public const float CanvasWidth = 360f;
        public const float CanvasHeight = 640f;

        public const float ReserveIconHeight = 20f;
        public const float AttackButtonHeight = 460f;

        // 論理パス → バージョン文字列
        public static readonly Dictionary<string, string> Map = new()
        {
            { "images/bg001.png" , "" },
            { "images/bg002.png" , "" },
            { "images/bg003.png" , "" },
            { "images/bg004.png" , "" },
            { "images/bg005.png" , "" },
            { "images/bg006.png" , "" },
            { "images/bg007.png" , "" },
            { "images/bg008.png" , "" },
            { "images/bg009.png" , "" },
            { "images/bg010.png" , "" },
            { "images/bg011.png" , "" },
            { "images/bg012.png" , "" },
            { "images/bg013.png" , "?v=20251026a" },
            { "images/bg014.png" , "" },
            { "images/bg015.png" , "" },
            { "images/bg016.png" , "" },

            { "images/ch00-00.png" , "?v=20251009a" },
            { "images/ch00-01.png" , "?v=20251106a" },
            { "images/ch01-00.png" , "" },
            { "images/ch02-00.png" , "" },
            { "images/ch03-00.png" , "" },
            { "images/ch04-00.png" , "?v=20251018a" },
            { "images/ch05-00.png" , "?v=20251101a" },
            { "images/ch06-00.png" , "?v=20251101a" },

            { "images/ui00-00.png" , "" },
            { "images/ui00-01.png" , "?v=20250928a" },
            { "images/ui01-00.png" , "?v=20251003a" },
            { "images/ui01-01.png" , "" },
            { "images/ui02-00.png" , "" },
            { "images/ui03-00.png" , "" },
            { "images/ui04-00.png" , "?v=20251022a" },
            { "images/ui05-01.png" , "" },
            { "images/ui05-02.png" , "" },
            { "images/ui06-00.png" , "" },
            { "images/ui06-01.png" , "" },
            { "images/ui06-02.png" , "" },
            { "images/ui06-03.png" , "?v=20250930a" },
            { "images/ui06-04.png" , "" },
            { "images/ui07-01.png" , "?v=20251110a" },
            { "images/ui07-02.png" , "?v=20251110a" },
            { "images/ui07-03.png" , "?v=20251110a" },
            { "images/ui07-04.png" , "?v=20251110a" },
            { "images/ui07-05.png" , "?v=20251110a" },
            { "images/ui07-06.png" , "?v=20251110a" },
            { "images/ui07-07.png" , "?v=20251110a" },
            { "images/ui07-08.png" , "?v=20251110a" },
            { "images/ui07-09.png" , "?v=20251110a" },
            { "images/ui07-10.png" , "?v=20251110a" },
            { "images/ui07-11.png" , "?v=20251110a" },
            { "images/ui07-12.png" , "?v=20251110a" },
            { "images/ui07-13.png" , "?v=20251110a" },
            { "images/ui07-14.png" , "?v=20251110a" },
            { "images/ui07-15.png" , "?v=20251110a" },
            { "images/ui07-16.png" , "?v=20251110a" },
        };

        /// <summary>
        /// バージョン解決して画像パスを返す
        /// </summary>
        public static string VersionResolve(string logicalPath)
            => Map.TryGetValue(logicalPath, out var version)
                ? logicalPath + version
                : logicalPath;

        /// <summary>
        /// プリロード対象の全画像パスを返す
        /// </summary>
        public static IEnumerable<string> GetAllImages()
            => Map.Keys.Select(VersionResolve);

        /// <summary>
        /// ストーリー進行度
        /// </summary>
        public enum StoryProgress
        {
            Prologue = 0,                 // 序章
            VillageMission0 = 100,        // 廃村の掃討任務・開始
            VillageMissionT = 101,        // 廃村の掃討任務・チュートリアル
            VillageMission1 = 111,        // 廃村の掃討任務・進行度1
            VillageMission2 = 112,        // 廃村の掃討任務・進行度2
            VillageMission3 = 113,        // 廃村の掃討任務・進行度3
            VillageMission4 = 114,        // 廃村の掃討任務・進行度4
            VillageMission5 = 115,        // 廃村の掃討任務・進行度5

            ReturnToVillage = 200,        // 風魔の里に帰還、小太郎との会話
            BanditHideout = 300,          // 夜盗拠点潜入
            ShadowTrial = 700,            // 冥府入口
            HellChapter = 800,            // 冥府編
            Ending = 900                  // エンディング
        }

        /// <summary>
        /// 現在のストーリー進行度
        /// </summary>
        public static StoryProgress CurrentProgress = StoryProgress.Prologue;

        /// <summary>
        /// ゲームスイッチ定義（キー一覧）
        /// </summary>
        public static class GameSwitch
        {
            public const string TrainingTutorial = "修練チュートリアル完了";
            public const string TrainingMix = "複合修練解放";
            public const string TrainingSp = "皆伝修練解放";
            public const string EquipmentTutorial = "装備チュートリアル完了";
        }

    }

    public static class KanjiNumber
    {
        private static readonly string[] KanjiDigits =
            { "〇", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
        private static readonly string[] KanjiUnits =
            { "", "十", "百", "千" };

        /// <summary>
        /// 整数を漢数字に変換（1～99まで対応）
        /// </summary>
        public static string ToKanji(int num)
        {
            if (num == 0) return KanjiDigits[0];
            if (num < 0 || num > 99) return num.ToString();

            int tens = num / 10;
            int ones = num % 10;
            string result = "";

            if (tens > 0)
            {
                if (tens > 1) result += KanjiDigits[tens];
                result += "十";
            }

            if (ones > 0) result += KanjiDigits[ones];
            return result;
        }
    }

    /// <summary>
    /// ゲーム状態
    /// </summary>
    public enum GameState
    {
        None,
        Title,
        Home, 
        StageSelectWorld,
        StageSelectArea,
        StealthBoard,
        Combat,
        Status,
        Forge,
        TestMode,
    }

    /// <summary>
    /// 戦闘画面の状態
    /// </summary>
    public enum BattlePhase
    {
        None,
        Test,
        PrologueScript,
        StartOfBattle,
        StartOfTurn,
        ChooseActor,
        Reservation,
        Reservation_SelectTarget,   // まず敵を1体選ぶ
        Reservation_SelectAttack,   // 攻撃種別を選ぶ（最大回数まで繰り返し）
        Prediction,
        Prediction_SelectAttack,
        Prediction_Confirm,
        Resolution,
        Results,
        EndTurn,
        EndBattle,
    }

    /// <summary>
    /// 戦闘結果
    /// </summary>
    public enum BattleResult
    {
        None,
        Victory,
        Defeat
    }

    /// <summary>
    /// 攻撃行動の種別
    /// </summary>
    public enum AttackType {
        /// <summary>穿</summary>
        Thrust,
        /// <summary>迅</summary>
        Slash,
        /// <summary>剛</summary>
        Down
    }

    /// <summary>
    /// 対応行動の種別
    /// </summary>
    public enum ResponseType
    {
        None,
        /// <summary>かばう</summary>
        Cover,
        /// <summary>回避</summary>
        Evade, 
        /// <summary>迅カウンター</summary>
        CounterSlash,
        /// <summary>剛カウンター</summary>
        CounterDown,
    }

    public record ActionOutcome
    {
        public string OutcomeType { get; set; } = "";

        // アニメーション指定
        public string? AttackerAnimOnHit { get; set; }
        public string? DefenderAnimOnHit { get; set; }
        public string? AttackerAnimOnEvade { get; set; }
        public string? DefenderAnimOnEvade { get; set; }

        // ★ 残痕式ダメージ
        public float AttackerResidualRatio { get; set; } = 0f; // 攻撃者が受ける残痕ダメージ比率
        public float AttackerFatalRatio { get; set; } = 0f; // 攻撃者が受ける致命ダメージ比率
        public float DefenderResidualRatio { get; set; } = 0f; // 防御者が受ける残痕ダメージ比率
        public float DefenderFatalRatio { get; set; } = 0f; // 防御者が受ける致命ダメージ比率

        // 回避用
        public float EvadeRate { get; set; } = 0f;
    }


    public record PlannedAction
    {
        public int AttackerId { get; init; }
        public int TargetId { get; set; }   // 予約時点の単一ターゲット
        public AttackType Attack { get; init; }
        public int SlotIndex { get; init; }   // 0..4
        public bool IsRevealedToDefender { get; set; } // 洞察差で可視
    }

    public record PredictedResponse
    {
        public int ResponderId { get; set; }          // 実際対応するキャラ（かばうで差替え可）
        public ResponseType Response { get; set; }
        public int SlotIndex { get; init; }           // 対応する予約スロット
        public ActionOutcome Outcome { get; set; }
    }

    public class BattleContext
    {
        public int TurnNumber { get; set; } = 1;

        public int CurrentActorId;  // 行動者のID
        public int CurrentTargetId { get; set; } = -1; // 選択中ターゲット

        public bool PlayerIsReservationSide { get; set; } // 今ターンの予約側

        public int CurrentMaxReservations { get; set; } = 6;

        public List<PlannedAction> ReserveQueue { get; } = new();
        public List<PredictedResponse> PredictQueue { get; } = new();

        // 使用上限・洞察差・かばう回数など
        public int RevealCount { get; set; }
        public int CoverRemaining { get; set; } = 2; // 例
                                                     // キャラ一覧・前衛/後衛情報・ABA禁止管理などもここで
    }
}
