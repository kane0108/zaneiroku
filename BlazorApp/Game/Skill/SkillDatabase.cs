namespace BlazorApp.Game
{
    /// <summary>
    /// 汎用スキル定義
    /// </summary>
    public class Skill
    {
        public string Id { get; set; } = "";

        public string DisplayName => (Level == 0 ? Id : $"{Id}＋{Level}");

        public int Level { get; set; } = 0;             // スキルレベル（鍛錬値）
        public string Category { get; set; } = "";     // 武器 / 忍具 / キャラ / 敵
        public SkillTrigger Trigger { get; set; }      // 発動タイミング
        public string Description { get; set; } = "";  // 効果説明文

        /// <summary>
        /// 表示用説明文を返す（Level反映）
        /// </summary>
        public string GetDescription()
        {
            // Levelが0ならマスタ記述をそのまま返す
            if (Level == 0) return Description;

            // 動的生成（マスタDescriptionをテンプレートとして利用）
            return Description
                .Replace("{Level}", Level.ToString())
                .Replace("{Percent}", $"{Level}%");
        }
    }

    /// <summary>
    /// スキル効果定義
    /// </summary>
    public static class SkillDatabase
    {
        public static readonly Dictionary<string, Skill> All = new()
        {
            // === 武器鍛錬スキル ===
            ["体力強化"] = new Skill
            {
                Id = "体力強化",
                Category = "武器鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "体力が{Percent}上昇する。"
            },
            ["攻撃強化"] = new Skill
            {
                Id = "攻撃強化",
                Category = "武器鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "攻撃が{Percent}上昇する。"
            },
            ["防御強化"] = new Skill
            {
                Id = "防御強化",
                Category = "武器鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "防御が{Percent}上昇する。"
            },
            ["敏捷強化"] = new Skill
            {
                Id = "敏捷強化",
                Category = "武器鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "敏捷が{Percent}上昇する。"
            },
            ["致命強化"] = new Skill
            {
                Id = "致命強化",
                Category = "武器鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "致命攻撃の威力が{Percent}上昇する。"
            },
            ["残痕強化"] = new Skill
            {
                Id = "残痕強化",
                Category = "武器鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "残痕攻撃の威力が{Percent}上昇する。"
            },
            ["体の極意"] = new Skill
            {
                Id = "体の極意",
                Category = "最大鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "「刀身が鍛え抜かれ、振るう者の力が途切れぬ」\n"+
                              "　穿/迅/剛の手数最大値+1"
            },
            ["技の極意(穿)"] = new Skill
            {
                Id = "技の極意(穿)",
                Category = "最大鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "「刃は息づき、斬撃は淀みなく巡る」\n"+
                              "　武器傾向(穿)の手数回復数+1"
            },
            ["技の極意(迅)"] = new Skill
            {
                Id = "技の極意(迅)",
                Category = "最大鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "「刃は息づき、斬撃は淀みなく巡る」\n" +
                              "　武器傾向(迅)の手数回復数+1"
            },
            ["技の極意(剛)"] = new Skill
            {
                Id = "技の極意(剛)",
                Category = "最大鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "「刃は息づき、斬撃は淀みなく巡る」\n" +
                              "　武器傾向(剛)の手数回復数+1"
            },
            ["技の極意(全)"] = new Skill
            {
                Id = "技の極意(全)",
                Category = "最大鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "「刃は息づき、斬撃は淀みなく巡る」\n" +
                              "　全手数回復数+1"
            },
            ["心の極意"] = new Skill
            {
                Id = "心の極意",
                Category = "最大鍛錬",
                Trigger = SkillTrigger.Passive,
                Description = "「体と技が調和し、刀に宿る心が覚醒する」\n" +
                              "　予約行動回数+1"
            },
            ["反撃無制限"] = new Skill
            {
                Id = "反撃無制限",
                Category = "武器固定",
                Trigger = SkillTrigger.Passive,
                Description = "反撃時に手数を消費しない。"
            },
            ["不可避"] = new Skill
            {
                Id = "不可避",
                Category = "武器固定",
                Trigger = SkillTrigger.Passive,
                Description = "迅/剛が必ず命中する。(穿は回避率半減)"
            },
            ["致命化"] = new Skill
            {
                Id = "致命化",
                Category = "武器スロット",
                Trigger = SkillTrigger.Passive,
                Description = "残痕ダメージをすべて致命ダメージとして計算する。"
            },
            ["穿・回復数+1"] = new Skill
            {
                Id = "穿・回復数+1",
                Category = "武器スロット",
                Trigger = SkillTrigger.Passive,
                Description = "穿の手数回復数が+1される。"
            },
            ["迅・回復数+1"] = new Skill
            {
                Id = "迅・回復数+1",
                Category = "武器スロット",
                Trigger = SkillTrigger.Passive,
                Description = "迅の手数回復数が+1される。"
            },
            ["剛・回復数+1"] = new Skill
            {
                Id = "剛・回復数+1",
                Category = "武器スロット",
                Trigger = SkillTrigger.Passive,
                Description = "剛の手数回復数が+1される。"
            },

            // === 忍具スキル例 ===
            ["洞察上昇"] = new Skill
            {
                Id = "洞察上昇",
                Category = "忍具",
                Trigger = SkillTrigger.OnBattle,
                Description = "戦闘開始時に洞察力＋10%。"
            },

            // === キャラ固有スキル例 ===
            ["鬼眼"] = new Skill
            {
                Id = "鬼眼",
                Category = "キャラ",
                Trigger = SkillTrigger.OnBattle,
                Description = "致命変換時、変換効率＋25%。沙耶専用。"
            },
        };
    }


    public interface ISkillHolder
    {
        List<Skill> Skills { get; set; }
    }

    /// <summary>
    /// スキル発動トリガー種別
    /// </summary>
    public enum SkillTrigger
    {
        Passive,        // 常時効果（装備・所持中に常に有効）
        OnForge,        // 武器鍛錬関連
        OnBattle,       // 戦闘中に特定条件で発動
    }
}
