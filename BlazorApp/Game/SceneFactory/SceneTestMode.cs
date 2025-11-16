using BlazorApp.Game.BackgroundFactory;
using BlazorApp.Game.CharacterFactory;
using BlazorApp.Game.UIObjectFactory;
using System.Drawing;

namespace BlazorApp.Game.SceneFactory
{
    public class SceneTestMode : BaseSceneFactory
    {
        public override GameState TargetState => GameState.TestMode;

        /// <summary>
        /// シーン生成
        /// </summary>
        public override Scene Create()
        {
            // 経験値、お金、アイテムを補充
            GameMain.Instance.AddExp(10000);
            GameMain.Instance.AddMoney(10000);

            foreach (var i in ItemManager.Instance.GetAllItems())
                ItemManager.Instance.Add(i.Id, 100);

            return new Scene
            {
                State = GameState.TestMode,
                Background = GameInitializer.Create<BackgroundTestMode, Background>(),
                Characters = SetupCharacters(),
                UIObjects = SetupUIObjects(),
            };
        }

        /// <summary>
        /// キャラクタのセットアップ
        /// </summary>
        private Dictionary<int, Character> SetupCharacters()
        {
            var player = new Character[2];

            player[0] = GameInitializer.Create<CharacterPlayer00, Character>();
            player[0].ObjectId = 0;
            player[0].Name = "主人公";
            player[0].Type = "Player";
            player[0].PosX = 100;
            player[0].PosY = 100;

            player[1] = GameInitializer.Create<CharacterPlayer01, Character>();
            player[1].ObjectId = 1;
            player[1].Name = "相棒";
            player[1].Type = "Player";
            player[1].PosX = 100;
            player[1].PosY = 200;

            // ★ ここで全キャラの手数をリセット
            foreach (var ch in player)
            {
                ch.CurrentStats = ch.BaseStats.Clone();
                ch.CurrentStats.ResetHands();
                ch.CurrentStats.ResetHP();
            }

            return new Dictionary<int, Character>
            {
                { player[0].ObjectId, player[0] },
                { player[1].ObjectId, player[1] },
            };
        }

        /// <summary>
        /// UIオブジェクトのセットアップ
        /// </summary>
        private Dictionary<int, UIObject> SetupUIObjects()
        {
            var result = new Dictionary<int, UIObject>();
            int id = 100;

            // 穿攻撃ボタン
            result[id++] = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.PosX = 50f;
                ui.PosY = 500f;
                ui.Text = "穿";
                ui.FontSize = 20;
                ui.Opacity = 0.4f;
                ui.OnClick = () => {
#if DEBUG
                    Console.WriteLine("▶ 【穿】ボタンが押されました！");
#endif
                    var ch = GameMain.Instance.GetCharacter(1);
                    ch?.PlayAnimation("穿・攻撃");
                };
            });

            // 迅攻撃ボタン
            result[id++] = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.PosX = 120f;
                ui.PosY = 500f;
                ui.Text = "迅";
                ui.FontSize = 20;
                ui.Opacity = 0.4f;
                ui.OnClick = () => {
#if DEBUG
                Console.WriteLine("▶ 【迅】ボタンが押されました！");
#endif
                    var ch = GameMain.Instance.GetCharacter(1);
                    ch?.PlayAnimation("迅・攻撃");
                };
            });

            // 剛攻撃ボタン
            result[id++] = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.PosX = 190f;
                ui.PosY = 500f;
                ui.Text = "剛";
                ui.FontSize = 20;
                ui.Opacity = 0.4f;
                ui.OnClick = () => {
#if DEBUG
                    Console.WriteLine("▶ 【剛】ボタンが押されました！");
#endif
                    var ch = GameMain.Instance.GetCharacter(1);
                    ch?.PlayAnimation("剛・攻撃");
                };
            });

            // 回避ボタン
            result[id++] = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.PosX = 260f;
                ui.PosY = 500f;
                ui.Text = "回";
                ui.FontSize = 20;
                ui.Opacity = 0.4f;
                ui.OnClick = () => {
                    var ch = GameMain.Instance.GetCharacter(1);
                    ch?.PlayAnimation("回避");
                };
            });

            // ダウンボタン
            result[id++] = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.PosX = 330f;
                ui.PosY = 500f;
                ui.Text = "死";
                ui.FontSize = 20;
                ui.Opacity = 0.4f;
                ui.OnClick = () => {
                    var ch = GameMain.Instance.GetCharacter(1);
                    ch?.PlayAnimation("dead");
                };
            });

            // 穿NGボタン
            result[id++] = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.PosX = 50f;
                ui.PosY = 550f;
                ui.Text = "穿NG";
                ui.FontSize = 20;
                ui.Opacity = 0.4f;
                ui.OnClick = () => {
                    var ch = GameMain.Instance.GetCharacter(1);
                    ch?.PlayAnimation("穿失敗・ダメージ");
                };
            });

            // 迅NGボタン
            result[id++] = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.PosX = 120f;
                ui.PosY = 550f;
                ui.Text = "迅NG";
                ui.FontSize = 20;
                ui.Opacity = 0.4f;
                ui.OnClick = () => {
                    var ch = GameMain.Instance.GetCharacter(1);
                    ch?.PlayAnimation("迅失敗・ダメージ");
                };
            });

            // 剛NGボタン
            result[id++] = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.PosX = 190f;
                ui.PosY = 550f;
                ui.Text = "剛NG";
                ui.FontSize = 20;
                ui.Opacity = 0.4f;
                ui.OnClick = () => {
                    var ch = GameMain.Instance.GetCharacter(1);
                    ch?.PlayAnimation("剛失敗・ダメージ");
                };
            });

            // 回避NGボタン
            result[id++] = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.PosX = 260f;
                ui.PosY = 550f;
                ui.Text = "回NG";
                ui.FontSize = 20;
                ui.Opacity = 0.4f;
                ui.OnClick = () => {
                    var ch = GameMain.Instance.GetCharacter(1);
                    ch?.PlayAnimation("回避失敗・ダメージ");
                };
            });

            // 残像ON/OFFボタン
            result[id++] = GameInitializer.Create<UIObjectButton, UIObject>(new Dictionary<string, object>
            {
                { "x", 300f },
                { "y", 590f },
                { "text", "残像" },
                { "textColor", "#FF0000" },
                { "fontSize", 20 },
                { "fontFamily", "serif" },
                { "opacity", 0.4f },
                { "onClick", (Action)(() =>
                    {
#if DEBUG
                        Console.WriteLine("▶ 残像ON/OFFボタンが押されました！");
#endif
                        
                        var ch = GameMain.Instance.GetCharacter(1);
                        if (ch != null)
                        {
                            ch.EnableAfterImage = !ch.EnableAfterImage;
                        }
                    })
                }
            });

            // テロップテスト
            result[id++] = GameInitializer.Create<UIObjectTelop, UIObject>(ui =>
            {
                ui.CenterX = true;
                ui.TextAlign = "center";
                ui.PosY = 100f;
                ui.Text = "～テストモード～";
                ui.FontSize = 40;
                ui.TextColor = "#FFD700";
            });

            return result;
        }
    }
}
