namespace BlazorApp.Game
{
    public static class SkillHandler
    {
        /// <summary>
        /// 鍛錬値変更に応じてスキルを更新
        /// </summary>
        public static void ApplyForgeLevelChange(Equipment eq)
        {
            /// === 武器鍛錬スキル以外を退避 ===
            var fixedSkills = eq.Skills
                .Where(s =>
                    (s.Category != "武器鍛錬" && s.Category != "最大鍛錬"))
                .Select(CloneSkill)
                .ToList();

            // === 武器鍛錬由来スキルをリセット ===
            eq.Skills.Clear();

            // === 固定スキルを再登録 ===
            eq.Skills.AddRange(fixedSkills);

            // === 重量＋方向：攻撃強化 ===
            if (eq.ForgeWeight > 0)
            {
                var baseSkill = SkillDatabase.All["攻撃強化"];
                var copy = CloneSkill(baseSkill);
                copy.Level = eq.ForgeWeight;
                eq.Skills.Add(copy);
            }

            // === 重量－方向：防御・敏捷強化 ===
            if (eq.ForgeWeight < 0)
            {
                int lv = Math.Abs(eq.ForgeWeight);

                var defSkill = CloneSkill(SkillDatabase.All["防御強化"]);
                defSkill.Level = lv;
                eq.Skills.Add(defSkill);
            }
            if (eq.ForgeWeight < 0)
            {
                int lv = Math.Abs(eq.ForgeWeight);

                var defSkill = CloneSkill(SkillDatabase.All["敏捷強化"]);
                defSkill.Level = lv;
                eq.Skills.Add(defSkill);
            }

            // === 鋭さ＋方向：致命強化 ===
            if (eq.ForgeSharp > 0)
            {
                var baseSkill = CloneSkill(SkillDatabase.All["致命強化"]);
                baseSkill.Level = eq.ForgeSharp;
                eq.Skills.Add(baseSkill);
            }

            // === 鋭さ－方向：残痕強化 ===
            if (eq.ForgeSharp < 0)
            {
                int lv = Math.Abs(eq.ForgeSharp);
                var baseSkill = CloneSkill(SkillDatabase.All["残痕強化"]);
                baseSkill.Level = lv;
                eq.Skills.Add(baseSkill);
            }

            // === 最大鍛錬到達時（重量±10／鋭さ±10） ===
            if ((Math.Abs(eq.ForgeWeight) >= 10) && (Math.Abs(eq.ForgeSharp) >= 10))
            {
                eq.Skills.Add(CloneSkill(SkillDatabase.All["心の極意"]));
            }
            
            if (Math.Abs(eq.ForgeWeight) >= 10)
            {
                eq.Skills.Add(CloneSkill(SkillDatabase.All["体の極意"]));
            }

            if (Math.Abs(eq.ForgeSharp) >= 10)
            {
                switch (eq.Trend)
                {
                    case "穿特化":
                        eq.Skills.Add(CloneSkill(SkillDatabase.All["技の極意(穿)"]));
                        break;
                    case "迅特化":
                        eq.Skills.Add(CloneSkill(SkillDatabase.All["技の極意(迅)"]));
                        break;
                    case "剛特化":
                        eq.Skills.Add(CloneSkill(SkillDatabase.All["技の極意(剛)"]));
                        break;
                    case "万能型":
                        eq.Skills.Add(CloneSkill(SkillDatabase.All["技の極意(全)"]));
                        break;
                }
            }
        }

        /// <summary>
        /// マスタスキルをディープコピー（参照汚染防止）
        /// </summary>
        private static Skill CloneSkill(Skill src)
        {
            return new Skill
            {
                Id = src.Id,
                Level = src.Level,
                Category = src.Category,
                Trigger = src.Trigger,
                Description = src.Description
            };
        }
    }
}
