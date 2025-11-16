using BlazorApp.Game;
using System.Drawing;
using System.IO;

namespace BlazorApp.Game.CharacterFactory
{
    /// <summary>
    /// ベースキャラクタファクトリ
    /// </summary>
    public abstract class BaseCharacterFactory : IFactory<Character>
    {
        public abstract Character Create();

        /// <summary>
        /// キャラクタベースの生成
        /// </summary>
        protected Character CreateCharacterBase(int id, string name, float posX, float posY)
        {
            var ch = new Character
            {
                ObjectId = id,
                Name = name,
                Type = "Player",
                PosX = posX,
                PosY = posY,
                DefaultPosX = posX,
                DefaultPosY = posY,
                BattlePosX = posX,
                BattlePosY = posY,
                BaseStats = new CharacterStats {
                    MaxHP = 100, Attack = 100, Defense = 100, Speed = 100, Insight = 100, Confuse = 100,
                    Intelligence = 50,
                    MaxReservationPerTurn = 4,
                    MaxHands = new Dictionary<AttackType, int> {
                        { AttackType.Thrust, 2 },
                        { AttackType.Slash,  2 },
                        { AttackType.Down,   2 }
                    }
                },
                CurrentStats = new CharacterStats(),
                Animations = new Dictionary<string, GameObjectAnimation>(),
                CurrentAnimationName = "idle"
            };

            // ★ 共通クリック処理
            ch.OnClick = () =>
            {
                var scene = GameMain.Instance.CurrentScene as BlazorApp.Game.Battle.BattleScene;
                scene?.SubmitIntent(new Battle.CharacterTapIntent(ch.ObjectId));
            };

            return ch;
        }

        /// <summary>
        /// アイドル状態アニメーションの定義
        /// </summary>
        protected void AddIdleFromSheet(Character character, string sheetPath, int size)
        {
            var frames = GameObjectAnimation.CreateFramesFromSheet(sheetPath, size, size, count: 2, duration: 1.0f);
            character.Animations["idle"] = new GameObjectAnimation
            {
                Loop = true,
                Frames = frames
            };
        }

        /// <summary>
        /// 穿攻撃アニメーションの定義
        /// </summary>
        protected void AddAttackMotionThrustFromSheet(Character character, string sheetPath, int size)
        {
            var frames = GameObjectAnimation.CreateFramesFromSheet(sheetPath, size, size, count: 3, duration: 0.2f, size * 3);

            frames[1].OffsetX = 20;  // 少し前進しながら
            frames[2].OffsetX = 15;

            character.Animations["穿・攻撃"] = new GameObjectAnimation
            {
                Loop = false,
                Frames = frames
            };
        }

        /// <summary>
        /// 迅攻撃アニメーションの定義
        /// </summary>
        protected void AddAttackMotionSlashFromSheet(Character character, string sheetPath, int size)
        {
            var frames = GameObjectAnimation.CreateFramesFromSheet(sheetPath, size, size, count: 3, duration: 0.2f, size * 1);

            frames[1].OffsetX = 20;  // 少し前進しながら
            frames[2].OffsetX = 15;

            character.Animations["迅・攻撃"] = new GameObjectAnimation
            {
                Loop = false,
                Frames = frames
            };
        }

        /// <summary>
        /// 剛攻撃アニメーションの定義
        /// </summary>
        protected void AddAttackMotionDownFromSheet(Character character, string sheetPath, int size)
        {
            var frames = GameObjectAnimation.CreateFramesFromSheet(sheetPath, size, size, count: 3, duration: 0.2f, size * 2);

            frames[1].OffsetX = 20;  // 少し前進しながら
            frames[2].OffsetX = 15;

            character.Animations["剛・攻撃"] = new GameObjectAnimation
            {
                Loop = false,
                Frames = frames
            };
        }

        /// <summary>
        /// 穿失敗ダメージアニメーションの定義
        /// </summary>
        protected void AddDamageMotionThrustFailedFromSheet(Character character, string sheetPath, int size)
        {
            var frames = new List<GameObjectAnimationFrame>()
            {
                new GameObjectAnimationFrame
                {
                    Sprite = new Sprite(sheetPath, new Rectangle(size * 3, size * 0, size, size)),
                    Duration = 0.2f,
                    OffsetX = 0,
                    OffsetY = 0,
                },
                new GameObjectAnimationFrame
                {
                    Sprite = new Sprite(sheetPath, new Rectangle(size * 4, size * 0, size, size)),
                    Duration = 0.4f,
                    OffsetX = -5,
                    OffsetY = -5,
                },
            };

            character.Animations["穿失敗・ダメージ"] = new GameObjectAnimation
            {
                Loop = false,
                Frames = frames
            };
        }

        /// <summary>
        /// 迅失敗ダメージアニメーションの定義
        /// </summary>
        protected void AddDamageMotionSlashFailedFromSheet(Character character, string sheetPath, int size)
        {
            var frames = new List<GameObjectAnimationFrame>()
            {
                new GameObjectAnimationFrame
                {
                    Sprite = new Sprite(sheetPath, new Rectangle(size * 1, size * 0, size, size)),
                    Duration = 0.2f,
                    OffsetX = 0,
                    OffsetY = 0,
                },
                new GameObjectAnimationFrame
                {
                    Sprite = new Sprite(sheetPath, new Rectangle(size * 4, size * 0, size, size)),
                    Duration = 0.4f,
                    OffsetX = -5,
                    OffsetY = -5,
                },
            };

            character.Animations["迅失敗・ダメージ"] = new GameObjectAnimation
            {
                Loop = false,
                Frames = frames
            };
        }

        /// <summary>
        /// 剛失敗ダメージアニメーションの定義
        /// </summary>
        protected void AddDamageMotionDownFailedFromSheet(Character character, string sheetPath, int size)
        {
            var frames = new List<GameObjectAnimationFrame>()
            {
                new GameObjectAnimationFrame
                {
                    Sprite = new Sprite(sheetPath, new Rectangle(size * 2, size * 0, size, size)),
                    Duration = 0.2f,
                    OffsetX = 0,
                    OffsetY = 0,
                },
                new GameObjectAnimationFrame
                {
                    Sprite = new Sprite(sheetPath, new Rectangle(size * 4, size * 0, size, size)),
                    Duration = 0.4f,
                    OffsetX = -5,
                    OffsetY = -5,
                },
            };

            character.Animations["剛失敗・ダメージ"] = new GameObjectAnimation
            {
                Loop = false,
                Frames = frames
            };


        }

        /// <summary>
        /// 回避成功アニメーションの定義
        /// </summary>
        protected void AddEvadeMotionFromSheet(Character character, string sheetPath, int size)
        {
            var frames = new List<GameObjectAnimationFrame>()
            {
                new GameObjectAnimationFrame
                {
                    Sprite = new Sprite(sheetPath, new Rectangle(size * 0, size * 2, size, size))
                    {
                        ScaleX = 0.96f,
                        ScaleY = 0.96f,
                    },
                    Duration = 0.2f,
                    OffsetX = -2,
                    OffsetY = -2,
                },
                new GameObjectAnimationFrame
                {
                    Sprite = new Sprite(sheetPath, new Rectangle(size * 0, size * 2, size, size))
                    {
                        ScaleX = 0.92f,
                        ScaleY = 0.92f,
                    },
                    Duration = 0.4f,
                    OffsetX = -5,
                    OffsetY = -5,
                },
            };

            character.Animations["回避"] = new GameObjectAnimation
            {
                Loop = false,
                Frames = frames
            };
        }

        /// <summary>
        /// 回避失敗ダメージアニメーションの定義
        /// </summary>
        protected void AddDamageMotionEvadeFailedFromSheet(Character character, string sheetPath, int size)
        {
            var frames = new List<GameObjectAnimationFrame>()
            {
                new GameObjectAnimationFrame
                {
                    Sprite = new Sprite(sheetPath, new Rectangle(size * 0, size * 2, size, size)),
                    Duration = 0.2f,
                    OffsetX = 0,
                    OffsetY = -5,
                },
                new GameObjectAnimationFrame
                {
                    Sprite = new Sprite(sheetPath, new Rectangle(size * 4, size * 0, size, size)),
                    Duration = 0.4f,
                    OffsetX = -5,
                    OffsetY = -5,
                },
            };

            character.Animations["回避失敗・ダメージ"] = new GameObjectAnimation
            {
                Loop = false,
                Frames = frames
            };
        }

        /// <summary>
        /// ダウン状態アニメーションの定義
        /// </summary>
        protected void AddLoseFromSheet(Character character, string sheetPath, int size)
        {
            var frames = GameObjectAnimation.CreateFramesFromSheet(sheetPath, size, size, count: 1, duration: 0f, offsetX:size*4, offsetY:size*1);
            character.Animations["dead"] = new GameObjectAnimation
            {
                Loop = false,
                Frames = frames
            };
        }
    }
}