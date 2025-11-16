namespace BlazorApp.Game
{
    /// <summary>
    /// キャラクターの戦闘用ステータス
    /// </summary>
    public class CharacterStats
    {
        // === HP関連 ===

        /// <summary>最大HP</summary>
        public int MaxHP { get; set; }

        /// <summary>残痕HP（緑ゲージ部分）</summary>
        public int ResidualHP { get; set; }

        /// <summary>致命HP（赤ゲージ部分）</summary>
        public int FatalHP { get; set; }

        /// <summary>
        /// 表示用の補間値（緑HP）
        /// 0.0～1.0 の比率で管理し、UI更新で滑らかに変化させる
        /// </summary>
        public float DisplayResidual { get; set; }

        /// <summary>
        /// 表示用の補間値（赤HP）
        /// 0.0～1.0 の比率で管理し、UI更新で滑らかに変化させる
        /// </summary>
        public float DisplayFatal { get; set; }

        /// <summary>戦闘不能かどうか（残痕+致命の両方が0以下）</summary>
        public bool IsDead => ResidualHP <= 0 && FatalHP <= 0;

        // === 能力値関連 ===

        /// <summary>攻撃力</summary>
        public int Attack { get; set; }

        /// <summary>防御力</summary>
        public int Defense { get; set; }

        /// <summary>行動速度（ATB用）</summary>
        public int Speed { get; set; }

        /// <summary>洞察（予約側の手を読みやすさ）</summary>
        public int Insight { get; set; }

        /// <summary>翻弄（相手に読まれにくさ）</summary>
        public int Confuse { get; set; }

        /// <summary>
        /// 賢さ（0～100程度）
        /// 高いほど敵AIが最適手を選びやすい
        /// </summary>
        public int Intelligence { get; set; } = 80;

        /// <summary>
        /// 1ターンで予約できる行動数の上限
        /// </summary>
        public int MaxReservationPerTurn { get; set; } = 6;

        // === 補助フラグ ===

        /// <summary>忍具未装備時の敏捷補正適用済みかどうか</summary>
        public bool NoNinguSpeedBonusApplied { get; private set; } = false;

        /// <summary>忍具未装備による敏捷ボーナス値</summary>
        public int NoNinguSpeedBonusValue { get; private set; } = 0;

        // === 行動回数管理 ===

        /// <summary>各攻撃種別ごとの最大手数</summary>
        public Dictionary<AttackType, int> MaxHands { get; set; } = new()
        {
            { AttackType.Thrust, 0 },
            { AttackType.Slash,  0 },
            { AttackType.Down,   0 }
        };

        /// <summary>現在残っている手数</summary>
        public Dictionary<AttackType, int> RemainingHands { get; set; } = new()
        {
            { AttackType.Thrust, 0 },
            { AttackType.Slash,  0 },
            { AttackType.Down,   0 }
        };

        /// <summary>
        /// ターン終了時の手数（UI表示用）
        /// </summary>
        public Dictionary<AttackType, int> EndOfTurnHands { get; set; } = new();

        public Dictionary<string, float> TempBuffs { get; set; } = new();

        public void ClearTempBuffs()
        {
            TempBuffs.Clear();
        }

        // === メソッド ===

        /// <summary>
        /// HPを初期化（全回復）
        /// </summary>
        public void ResetHP()
        {
            ResidualHP = MaxHP;
            FatalHP = 0;
            DisplayResidual = 1f;
            DisplayFatal = 0f;
        }

        /// <summary>
        /// 残り手数をリセット（MaxHandsに戻す）
        /// </summary>
        public void ResetHands()
        {
            foreach (var atk in MaxHands.Keys.ToList())
                RemainingHands[atk] = MaxHands[atk];
        }

        /// <summary>
        /// 全攻撃種に指定数の手数を回復
        /// </summary>
        public void AddHands(int count)
        {
            foreach (var atk in MaxHands.Keys.ToList())
            {
                RemainingHands[atk] = Math.Min(RemainingHands[atk] + count, MaxHands[atk]);

                if (EndOfTurnHands.ContainsKey(atk))
                    EndOfTurnHands[atk] = Math.Min(RemainingHands[atk], MaxHands[atk]);
            }
        }

        /// <summary>
        /// ステータスのディープコピーを生成
        /// </summary>
        public CharacterStats Clone()
        {
            return new CharacterStats
            {
                MaxHP = this.MaxHP,
                ResidualHP = this.ResidualHP,
                FatalHP = this.FatalHP,
                DisplayResidual = this.DisplayResidual,
                DisplayFatal = this.DisplayFatal,
                Attack = this.Attack,
                Defense = this.Defense,
                Speed = this.Speed,
                Insight = this.Insight,
                Confuse = this.Confuse,
                Intelligence = this.Intelligence,
                MaxReservationPerTurn = this.MaxReservationPerTurn,
                MaxHands = new Dictionary<AttackType, int>(this.MaxHands),
                RemainingHands = new Dictionary<AttackType, int>(this.RemainingHands),
                EndOfTurnHands = new Dictionary<AttackType, int>(this.EndOfTurnHands),
            };
        }

        // === ダメージ処理 ===

        /// <summary>
        /// 残痕ダメージ（緑HPを削り、その分赤HPに変換）
        /// </summary>
        public void ApplyResidualDamage(int amount)
        {
            int consume = Math.Min(ResidualHP, amount);
            ResidualHP -= consume;
            FatalHP += consume;
        }

        /// <summary>
        /// 致命ダメージ（赤HPのみを削る）
        /// </summary>
        public void ApplyFatalDamage(int amount)
        {
            int consume = Math.Min(FatalHP, amount);
            FatalHP -= consume;
        }

        /// <summary>
        /// 残痕/致命の混合ダメージを与える
        /// </summary>
        public void ApplyMixedDamage(int residualAmount, int fatalAmount)
        {
            ApplyResidualDamage(residualAmount);
            ApplyFatalDamage(fatalAmount);
        }

        /// <summary>
        /// 強制死亡
        /// </summary>
        public void KillInstantly()
        {
            ResidualHP = 0;
            FatalHP = 0;
        }

        /// <summary>
        /// 忍具未装備時の敏捷ボーナスを適用または解除
        /// </summary>
        /// <param name="noNinguEquipped">忍具が装備されていないかどうか</param>
        public void ApplyNoNinguSpeedBonus(bool noNinguEquipped)
        {
            if (noNinguEquipped && !NoNinguSpeedBonusApplied)
            {
                // 例：敏捷 +5%
                NoNinguSpeedBonusValue = (int)MathF.Ceiling(Speed * 0.05f);
                Speed += NoNinguSpeedBonusValue;
                NoNinguSpeedBonusApplied = true;
            }
            else if (!noNinguEquipped && NoNinguSpeedBonusApplied)
            {
                // 元に戻す
                Speed -= NoNinguSpeedBonusValue;
                NoNinguSpeedBonusApplied = false;
                NoNinguSpeedBonusValue = 0;
            }
        }
    }
}
