namespace BlazorApp.Game
{
    public class EquipmentManager
    {
        public static EquipmentManager Instance { get; } = new EquipmentManager();

        private readonly Dictionary<string, Equipment> _allEquipments = new();
        private readonly HashSet<string> _unlocked = new();

        // キャラごとの装備（カテゴリ→装備ID）
        private readonly Dictionary<Character, Dictionary<string, string?>> _equipped = new();

        private EquipmentManager() { }

        public void Register(Equipment eq) => _allEquipments[eq.Id] = eq;

        public Equipment? Get(string id) =>
            _allEquipments.TryGetValue(id, out var eq) ? eq : null;

        public IEnumerable<Equipment> All => _allEquipments.Values;

        public void Unlock(string id) => _unlocked.Add(id);
        public bool IsUnlocked(string id) => _unlocked.Contains(id);

        public void InitializeDefaultEquipments()
        {
            _allEquipments.Clear();
            _unlocked.Clear();

            // === 忍具 ===
            Register(new Equipment("治痕薬",
                "忍びの間で用いられる妙薬。\n" +
                "一戦闘に一度、浅い傷（残痕）を完全治癒する。\n" +
                "致命に至った傷には効かない。",
                0, 4, "忍具")
            { UsePhase = NinguPhaseType.AnyTime });

            Register(new Equipment("治命水",
                "霊妙なる水薬。\n" +
                "一戦闘に一度、致命に至った傷を少し回復する。",
                0, 5, "忍具")
            { UsePhase = NinguPhaseType.AnyTime });

            Register(new Equipment("先見丹",
                "古来より「心眼を研ぎ澄ます」と伝わる秘薬。\n" +
                "戦闘中に服用すると、その手番だけ\n" +
                "相手のすべての手の内を見抜ける。",
                0, 6, "忍具")
            { UsePhase = NinguPhaseType.Prediction });

            Register(new Equipment("幻煙玉",
                "忍が姿を隠すために用いたとされる煙玉。\n" +
                "戦闘中に使用すると、濃煙が敵の眼を惑わせ、\n"+
                "その手番だけ、こちらの手の内を完全に隠す。",
                0, 7, "忍具")
            { UsePhase = NinguPhaseType.Reservation });

            Register(new Equipment("影分身の巻物",
                "己が影を分け、刹那に姿を惑わす忍の秘術。\n" +
                "戦闘中に使用すると、その手番だけ\n"+
                "敵の目を欺き回避率が上昇する。",
                9, 2, "忍具")
            { UsePhase = NinguPhaseType.Prediction });

            Register(new Equipment("影縫いの巻物",
                "暗き影を束ね、敵の足を縫うと伝わる秘術。\n" +
                "戦闘中に使用すると、その手番だけ\n"+
                "相手の身のこなしを奪い回避が鈍る。",
                9, 3, "忍具")
            { UsePhase = NinguPhaseType.Reservation });

            // === 武器 ===

            // -- 下位：合計 6～7 ---
            Register(new Equipment("無銘刀",
                "汎用的な打刀。\n" +
                "様々な局面で扱いやい。",
                1, 6, "武器")
            { HandsThrust = 2, HandsSlash = 2, HandsDown = 2, Trend = "剛特化" });

            Register(new Equipment("忍刀",
                "忍者が扱う基本の刀。\n" +
                "刀身が短く取り回しが良い。",
                2, 6, "武器")
            { HandsThrust = 2, HandsSlash = 3, HandsDown = 1, Trend = "迅特化"});

            Register(new Equipment("刺突刀",
                "細身の刀。\n" +
                "鋭い突きに適し、暗殺向き。",
                4, 6, "武器")
            { HandsThrust = 4, HandsSlash = 2, HandsDown = 1, Trend = "穿特化" });

            Register(new Equipment("小太刀",
                "刀身が短く軽快な刀。\n"+
                "軽量で速撃に向く。",
                3, 6, "武器")
            { HandsThrust = 1, HandsSlash = 4, HandsDown = 2, Trend = "迅特化"});

            Register(new Equipment("野太刀",
                "大ぶりの刀\n"+
                "重撃に秀でる。",
                5, 6, "武器")
            { HandsThrust = 1, HandsSlash = 2, HandsDown = 4, Trend = "剛特化"});

            // -- 中位：合計 9～10 ---

            Register(new Equipment("虎徹",
               "名を馳せた名工が鍛えた刀。\n" +
               "甲冑すら断つと伝わる。",
               10, 6, "武器")
            { HandsThrust = 3, HandsSlash = 3, HandsDown = 3, Trend = "剛特化" });

            Register(new Equipment("村正",
                "妖刀と恐れられる刀\n" +
                "切れ味は随一。素早く斬り伏せる。",
                6, 6, "武器")
            { HandsThrust = 2, HandsSlash = 5, HandsDown = 2, Trend = "迅特化" });

            Register(new Equipment("骨喰藤四郎",
                "骨すら断つとされる刀。\n" +
                "重い斬撃に強み。",
                8, 6, "武器")
            { HandsThrust = 2, HandsSlash = 2, HandsDown = 5, Trend = "剛特化"});

            Register(new Equipment("正宗",
                "天下五剣のひとつと伝わる名刀。\n"+
                "均整の取れた性能。",
                7, 6, "武器")
            { HandsThrust = 4, HandsSlash = 3, HandsDown = 3, Trend = "穿特化" });

            Register(new Equipment("三日月宗近",
                "最も美しい刀とされる天下五剣。\n" +
                "三日月の刃文が特徴。",
                5, 7, "武器")
            { HandsThrust = 3, HandsSlash = 4, HandsDown = 3, Trend = "迅特化" });

            Register(new Equipment("大典太光世",
               "天下五剣の一つ。\n" +
               "病を祓う霊力を宿すと伝わる大刀。",
               4, 7, "武器")
            { HandsThrust = 3, HandsSlash = 3, HandsDown = 4, Trend = "剛特化"});

            Register(new Equipment("忍刀・霞",
                "忍び専用に打たれた小型刀。\n"+
                "影のように素早い。",
                9, 6, "武器")
            { HandsThrust = 4, HandsSlash = 5, HandsDown = 1, Trend = "迅特化"});

            // -- 上位：合計 11～12 ---

            Register(new Equipment("七支刀",
                "神話に登場する枝分かれの刀。\n"+
                "特異な形状故に尖った性能を持つ。",
                1, 7, "武器")
            { HandsThrust = 5, HandsSlash = 1, HandsDown = 5, Trend = "穿特化" });

            Register(new Equipment("鬼丸国綱",
                "鬼を祓う霊刀。\n"+
                "重い一撃で怨敵を討つ。",
                3, 7, "武器")
            { HandsThrust = 1, HandsSlash = 5, HandsDown = 5, Trend = "剛特化"});

            Register(new Equipment("天叢雲剣",
                "三種の神器のひとつ。\n" +
                "古代より受け継がれる霊剣。",
                2, 7, "武器")
            { HandsThrust = 5, HandsSlash = 4, HandsDown = 3, Trend = "穿特化" });

            Register(new Equipment("布都御魂剣",
                "霊威を宿す剣。\n" +
                "神々が悪神を討つために用いたとされる。",
                6, 7, "武器")
            { HandsThrust = 3, HandsSlash = 5, HandsDown = 4, Trend = "迅特化" });

            Register(new Equipment("天羽々斬",
                "スサノオが八岐大蛇を斬ったと伝わる神剣。\n"+
                "力強い一閃であらゆる魔を祓う。",
                7, 7, "武器")
            { HandsThrust = 4, HandsSlash = 3, HandsDown = 5, Trend = "剛特化" });

            // -- 最強：合計 14～15 ---

            Register(new Equipment("影走り",
                "風魔一族に秘される幻の刀。\n" +
                "実体はなく、振るう者と共に影と同化する。",
                8, 7, "武器")
            { HandsThrust = 4, HandsSlash = 4, HandsDown = 4, Trend = "万能型" ,
                Skills = new List<Skill> { SkillDatabase.All["反撃無制限"] }
            });

            Register(new Equipment("無影刀・斬影",
                "風魔一族に伝わる究極の刀。\n"+
                "抜いた瞬間に斬撃が完了し、残影しか見えない。",
                9, 7, "武器")
            { HandsThrust = 5, HandsSlash = 5, HandsDown = 5, Trend = "万能型" ,
                Skills = new List<Skill> { SkillDatabase.All["不可避"] }
            });
        }

        /// <summary>
        /// 装備する（武器は必須、忍具はnullで外せる）
        /// </summary>
        public void Equip(Character ch, string category, string? id)
        {
            if (category == "武器")
            {
                // 武器は必須なので null は不可
                if (id == null || !_unlocked.Contains(id)) return;
            }
            else if (category == "忍具")
            {
                // 忍具はnull可
                if (id != null && !_unlocked.Contains(id)) return;
            }

            if (!_equipped.ContainsKey(ch))
                _equipped[ch] = new Dictionary<string, string?>();

            _equipped[ch][category] = id;

            ApplyToCharacter(ch);
        }

        public string? GetEquippedId(Character ch, string category)
        {
            if (_equipped.TryGetValue(ch, out var dict) &&
                dict.TryGetValue(category, out var id))
                return id;
            return null;
        }

        /// <summary>
        /// 指定キャラが装備中の装備オブジェクトを直接取得（存在しない場合 null）
        /// </summary>
        public Equipment? GetEquipped(Character ch, string category)
        {
            var id = GetEquippedId(ch, category);
            if (string.IsNullOrEmpty(id))
                return null;
            return Get(id); // ← Get(string id) は既に存在
        }

        private void ApplyToCharacter(Character ch)
        {
            // ベースからリセット
            ch.CurrentStats = ch.BaseStats.Clone();

            if (_equipped.TryGetValue(ch, out var dict))
            {
                foreach (var kv in dict)
                {
                    if (kv.Value != null && _allEquipments.TryGetValue(kv.Value, out var eq))
                    {
                        ch.CurrentStats.MaxHP += eq.Hp;
                        ch.CurrentStats.Attack += eq.Attack;
                        ch.CurrentStats.Defense += eq.Defense;
                        ch.CurrentStats.Speed += eq.Speed;
                        ch.CurrentStats.Insight += eq.Insight;
                        ch.CurrentStats.Confuse += eq.Confuse;
                    }
                }
            }

            // ★ 忍具未装備ボーナス適用
            var equippedNingu = GetEquipped(ch, "忍具");
            bool noNingu = (equippedNingu == null);
            ch.CurrentStats.ApplyNoNinguSpeedBonus(noNingu);

            // ★HPを装備後の最大値で初期化
            ch.CurrentStats.ResetHP();
            ch.CurrentStats.ResetHands();
        }

        /// <summary>
        /// 初期装備を設定する（必ず武器を持たせる）
        /// </summary>
        public void EnsureInitialEquip(Character ch, string defaultWeaponId)
        {
            if (!_unlocked.Contains(defaultWeaponId))
                _unlocked.Add(defaultWeaponId);

            if (!_equipped.ContainsKey(ch))
                _equipped[ch] = new Dictionary<string, string?>();

            if (!_equipped[ch].ContainsKey("武器") || _equipped[ch]["武器"] == null)
                _equipped[ch]["武器"] = defaultWeaponId;

            if (!_equipped[ch].ContainsKey("忍具"))
                _equipped[ch]["忍具"] = null;

            ApplyToCharacter(ch);
        }


        public void ApplyEquipmentAndSkillsToCharacter(Character ch)
        {
            // スキル集約リスト
            var allSkills = new List<Skill>();

            // === プレイヤーキャラ ===
            if (ch.Type == "Player")
            {
                ch.CurrentStats = ch.BaseStats.Clone();

                var eq = GetEquipped(ch, "武器");
                if (eq != null)
                {
                    // ステ補正
                    ch.CurrentStats.Attack += eq.Attack;
                    ch.CurrentStats.Defense += eq.Defense;
                    ch.CurrentStats.Speed += eq.Speed;
                    ch.CurrentStats.Insight += eq.Insight;
                    ch.CurrentStats.Confuse += eq.Confuse;

                    // 武器スキル
                    allSkills.AddRange(eq.Skills);
                }

                // キャラスキル
                allSkills.AddRange(ch.Skills);

                // 手数（武器基準）
                int thrust = eq?.HandsThrust ?? 0;
                int slash = eq?.HandsSlash ?? 0;
                int down = eq?.HandsDown ?? 0;

                // スキル適用
                ApplySkillEffects(ch, allSkills, ref thrust, ref slash, ref down);

                // 最終手数設定
                ch.CurrentStats.MaxHands = new Dictionary<AttackType, int>
                {
                    { AttackType.Thrust, thrust },
                    { AttackType.Slash,  slash  },
                    { AttackType.Down,   down   }
                };

                ch.CurrentStats.ResetHands();

                // ★ 忍具未装備ボーナス（スキル反映後に再度適用）
                var equippedNingu = GetEquipped(ch, "忍具");
                bool noNingu = (equippedNingu == null);
                ch.CurrentStats.ApplyNoNinguSpeedBonus(noNingu);
            }

            // === 敵キャラ ===
            else if (ch.Type == "Enemy")
            {
                ch.CurrentStats = ch.BaseStats.Clone();

                // キャラスキルのみ反映
                allSkills.AddRange(ch.Skills);

                // 現在の手数を取得（固定）
                int thrust = ch.BaseStats.MaxHands.TryGetValue(AttackType.Thrust, out var t) ? t : 0;
                int slash = ch.BaseStats.MaxHands.TryGetValue(AttackType.Slash, out var s) ? s : 0;
                int down = ch.BaseStats.MaxHands.TryGetValue(AttackType.Down, out var d) ? d : 0;

                // スキル適用
                ApplySkillEffects(ch, allSkills, ref thrust, ref slash, ref down);

                // 手数更新
                ch.CurrentStats.MaxHands = new Dictionary<AttackType, int>
                {
                    { AttackType.Thrust, thrust },
                    { AttackType.Slash,  slash  },
                    { AttackType.Down,   down   }
                };

                ch.CurrentStats.ResetHands();
            }

#if DEBUG
            Console.WriteLine($"[{ch.Name}] 手数→ 穿:{ch.CurrentStats.MaxHands[AttackType.Thrust]}, 迅:{ch.CurrentStats.MaxHands[AttackType.Slash]}, 剛:{ch.CurrentStats.MaxHands[AttackType.Down]}");
#endif
        }

        /// <summary>
        /// スキルのパッシブ効果をキャラに適用（ステータス・手数・予約数）
        /// </summary>
        private void ApplySkillEffects(Character ch, List<Skill> allSkills,
            ref int thrust, ref int slash, ref int down)
        {
            foreach (var sk in allSkills)
            {
                switch (sk.Id)
                {
                    // ステータス強化系
                    case "体力強化":
                        ch.CurrentStats.MaxHP += (int)(ch.CurrentStats.MaxHP * 0.01f * sk.Level);
                        break;
                    case "攻撃強化":
                        ch.CurrentStats.Attack += (int)(ch.CurrentStats.Attack * 0.01f * sk.Level);
                        break;
                    case "防御強化":
                        ch.CurrentStats.Defense += (int)(ch.CurrentStats.Defense * 0.01f * sk.Level);
                        break;
                    case "敏捷強化":
                        ch.CurrentStats.Speed += (int)(ch.CurrentStats.Speed * 0.01f * sk.Level);
                        break;
                    case "洞察強化":
                        ch.CurrentStats.Insight += (int)(ch.CurrentStats.Insight * 0.01f * sk.Level);
                        break;
                    case "翻弄強化":
                        ch.CurrentStats.Confuse += (int)(ch.CurrentStats.Confuse * 0.01f * sk.Level);
                        break;

                    // 手数増加系
                    case "体の極意":
                        thrust += 1;
                        slash += 1;
                        down += 1;
                        break;

                    // 予約数増加系
                    case "心の極意":
                        ch.CurrentStats.MaxReservationPerTurn += 1;
                        break;
                }
            }
        }

        /// <summary>
        /// セーブデータから反映
        /// </summary>
        public void ApplySaveData(Dictionary<string, EquipmentSave> eqs)
        {
            foreach (var kv in eqs)
            {
                var eq = FindById(kv.Key); // ★必要なら自作：Idで検索
                if (eq == null) continue;

                eq.ForgeWeight = kv.Value.ForgeWeight;
                eq.ForgeSharp = kv.Value.ForgeSharp;

                if (kv.Value.Unlocked)
                    _unlocked.Add(eq.Id);
                else
                    _unlocked.Remove(eq.Id);
            }
        }

        public Equipment? FindById(string id) => All.FirstOrDefault(e => e.Id == id);
    }
}
