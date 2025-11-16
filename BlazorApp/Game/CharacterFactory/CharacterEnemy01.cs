namespace BlazorApp.Game.CharacterFactory
{
    public class CharacterEnemy01 : BaseCharacterFactory
    {
        public override Character Create()
        {
            var character = CreateCharacterBase(1, "役人", 100, 100);

            // 立ち絵と立ち絵番号
            character.PortraitImagePath = "images/ch00-01.png";
            character.PortraitId = 3;

            // 戦闘アニメーション画像
            var sheetPath = "images/ch05-00.png";

            var size = 128;

            // アイドル状態のアニメーション
            AddIdleFromSheet(character, sheetPath, size);

            // 穿攻撃のアニメーション
            AddAttackMotionThrustFromSheet(character, sheetPath, size);

            // 迅攻撃のアニメーション
            AddAttackMotionSlashFromSheet(character, sheetPath, size);

            // 剛攻撃のアニメーション
            AddAttackMotionDownFromSheet(character, sheetPath, size);

            // 穿失敗のアニメーション
            AddDamageMotionThrustFailedFromSheet(character, sheetPath, size);

            // 迅失敗のアニメーション
            AddDamageMotionSlashFailedFromSheet(character, sheetPath, size);

            // 剛失敗のアニメーション
            AddDamageMotionDownFailedFromSheet(character, sheetPath, size);

            // 回避失敗のアニメーション
            AddDamageMotionEvadeFailedFromSheet(character, sheetPath, size);

            // 回避のアニメーション
            AddEvadeMotionFromSheet(character, sheetPath, size);

            // ダウン状態のアニメーション
            AddLoseFromSheet(character, sheetPath, size);

            character.BaseStats.MaxHP = 100;
            character.BaseStats.Attack = 100;
            character.BaseStats.Defense = 100;
            character.BaseStats.Speed = 100;
            character.BaseStats.Insight = 100;
            character.BaseStats.Confuse = 100;
            character.BaseStats.Intelligence = 50;

            // CurrentStats は BaseStats のコピー
            character.CurrentStats = character.BaseStats.Clone();
            character.CurrentStats.ResetHands(); // 念のため初期化
            character.CurrentStats.ResetHP();

            character.Type = "Enemy";

            return character;
        }
    }
}
