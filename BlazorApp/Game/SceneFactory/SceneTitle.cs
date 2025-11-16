using BlazorApp.Game.BackgroundFactory;
using BlazorApp.Game.CharacterFactory;
using BlazorApp.Game.UIObjectFactory;
using System.Drawing;

namespace BlazorApp.Game.SceneFactory
{
    /// <summary>
    /// タイトル画面（はじめから／つづきから）
    /// </summary>
    public class SceneTitle : BaseSceneFactory
    {
        public override GameState TargetState => GameState.Title;

        public override Scene Create()
        {
            var scene = new Scene
            {
                State = GameState.Title,
                Background = GameInitializer.Create<BackgroundTitle, Background>()
            };

            // ★ タイトル UI 生成（セーブデータ有無で分岐）
            _ = SetupTitleUiAsync(scene);

            return scene;
        }

        // ------------------------------------------------------------
        // ★ タイトル UI セットアップ（はじめから／つづきから）
        // ------------------------------------------------------------
        private async Task SetupTitleUiAsync(Scene scene)
        {
            // ゲーム初期化（ロード前に必要）
            InitializeNewGame();

            // セーブデータが存在するか？
            bool hasSave = await SaveManager.ExistsAsync();

            float centerY = 200f;
            int fontSize = 40;

            if (!hasSave)
            {
                // ====================================================
                // ★ セーブ無し → 「はじめから」だけ表示
                // ====================================================
                var startBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
                {
                    ui.Name = "はじめから";
                    ui.CenterX = true;
                    ui.PosY = centerY;
                    ui.Text = "はじめから";
                    ui.FontSize = fontSize;

                    ui.OnClick = () =>
                    {
                        RunNewGamePrologue(scene);
                    };
                });

                scene.AddUI(startBtn);
            }
            else
            {
                // ====================================================
                // ★ セーブ有り → 「つづきから」だけ表示
                // ====================================================
                var continueBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
                {
                    ui.Name = "つづきから";
                    ui.CenterX = true;
                    ui.PosY = centerY;
                    ui.Text = "つづきから";
                    ui.FontSize = fontSize;

                    ui.OnClick = () =>
                    {
                        GameMain.Instance.StartFadeTransition(GameState.Home);
                    };
                });

                scene.AddUI(continueBtn);

                // ====================================================
                // ★ 画面下部に“データ初期化”ボタン
                // ====================================================
                var resetBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
                {
                    ui.Name = "データ初期化";
                    ui.CenterX = true;
                    ui.PosY = Common.CanvasHeight - 50;
                    ui.Text = "データ初期化";
                    ui.FontSize = 14;
                    ui.Opacity = 0.6f;

                    ui.OnClick = async () =>
                    {
                        await SaveManager.DeleteAsync();
                        GameMain.Instance.StartFadeTransition(GameState.Title);
                    };
                });

                scene.AddUI(resetBtn);

                // ★ ロード本体
                await SaveManager.LoadAsync();
            }

#if DEBUG
            // === 「デバッグ」ボタン ===
            var debugBtn = GameInitializer.Create<UIObjectButton, UIObject>(ui =>
            {
                ui.CenterX = true;
                ui.PosY = centerY + 100f;
                ui.Text = "デバッグ";
                ui.TextColor = "#FF0000";
                ui.FontSize = 40;
                ui.Visible = true;

                ui.OnClick = () =>
                {

                    Console.WriteLine("▶ デバッグボタンが押されました！");

                    Common.CurrentProgress = Common.StoryProgress.VillageMission1;

                    GameMain.Instance.StartFadeTransition(GameState.Home);
                };
            });

            scene.AddUI(debugBtn);
#endif
        }

        // ------------------------------------------------------------
        // ★ 「はじめから」 → プロローグ戦闘へ
        // ------------------------------------------------------------
        private async void RunNewGamePrologue(Scene scene)
        {
            // ログイン直後のボタンをフェードアウトしたい場合：
            foreach (var ui in scene.UIObjects.Values)
                ui.Opacity = 0f;

            // プロローグ敵セットアップ（あなたの実装そのまま）
            var pEnemies = CreatePrologueEnemies();

            var pSetup = new Battle.BattleSetup
            {
                Allies = GameMain.Instance.PlayerParty,
                Enemies = pEnemies,
                Background = BackgroundSingle.Create("images/bg014.png"),
                StageName = "プロローグ"
            };

            // プロローグテキスト（あなたの既存の chunks）
            var chunks = new List<string>
            {
                "……夜。\n\n　月が照らすは、闇の道。",
#if !DEVTOOLS
                "　時は明治。\n\n　世は変わり、人は進み、",
                "　だが、剣はなお語り続ける。\n\n　力ではなく、意志の刃として。",
                "　その意志を影に刻む者たち……\n\n　それが、風魔。",
                "　風をまとう影、血に生きる忍の一族。",
                "　彼らの刃は、時代の裏に在り続ける。",
#endif
            };

            await GameMain.Instance.CurrentScene.RunPrologueOverlayAsync(
                chunks,
                maskOpacity: 0.7f,
                fadeInSec: 0.8f,
                fadeOutSec: 0.4f,
                onCompleted: () =>
                {
                    GameMain.Instance.StartFadeTransition(GameState.Combat, pSetup);
                });
        }

        // ------------------------------------------------------------
        // ★ プロローグ敵生成（あなたの元コードを整理して移植）
        // ------------------------------------------------------------
        private List<Character> CreatePrologueEnemies()
        {
            var enemies = new List<Character>();

#if !DEVTOOLS
            enemies.Add(GameInitializer.Create<CharacterEnemy02, Character>(c =>
            {
                c.ObjectId = 100;
                c.Name = "巡回兵1";
                c.Type = "Enemy";
                c.BaseStats = new CharacterStats
                {
                    MaxHP = 140, Attack = 80, Defense = 60, Speed = 200,
                    Insight = 100, Confuse = 150, Intelligence = 50,
                    MaxReservationPerTurn = 2,
                    MaxHands = new Dictionary<AttackType, int> {
                        { AttackType.Thrust, 2 }, { AttackType.Slash, 0 }, { AttackType.Down, 0 }
                    }
                };
                c.CurrentStats = c.BaseStats.Clone();
                c.CurrentStats.ResetHP();
            }));
#endif

            enemies.Add(GameInitializer.Create<CharacterEnemy02, Character>(c =>
            {
                c.ObjectId = 101;
                c.Name = "巡回兵2";
                c.Type = "Enemy";
                c.BaseStats = new CharacterStats
                {
#if !DEVTOOLS
                    MaxHP = 80,
#else
                    MaxHP = 1,   // DEVTOOLS のときは1HP
#endif
                    Attack = 80,
                    Defense = 60,
                    Speed = 200,
                    Insight = 100,
                    Confuse = 150,
                    Intelligence = 50,
                    MaxReservationPerTurn = 2,
                    MaxHands = new Dictionary<AttackType, int> {
                        { AttackType.Thrust, 0 }, { AttackType.Slash, 2 }, { AttackType.Down, 0 }
                    }
                };
                c.CurrentStats = c.BaseStats.Clone();
                c.CurrentStats.ResetHP();
            }));

#if !DEVTOOLS
            enemies.Add(GameInitializer.Create<CharacterEnemy02, Character>(c =>
            {
                c.ObjectId = 102;
                c.Name = "巡回兵3";
                c.Type = "Enemy";
                c.BaseStats = new CharacterStats
                {
                    MaxHP = 80, Attack = 80, Defense = 60, Speed = 200,
                    Insight = 100, Confuse = 150, Intelligence = 50,
                    MaxReservationPerTurn = 2,
                    MaxHands = new Dictionary<AttackType, int> {
                        { AttackType.Thrust, 0 }, { AttackType.Slash, 0 }, { AttackType.Down, 2 }
                    }
                };
                c.CurrentStats = c.BaseStats.Clone();
                c.CurrentStats.ResetHP();
            }));
#endif

            return enemies;
        }

        // ------------------------------------------------------------
        // ★ ゲーム新規初期化（元コードの整理版）
        // ------------------------------------------------------------
        private void InitializeNewGame()
        {
            // アイテム／装備初期化
            ItemManager.Instance.InitializeDefaultItems();
            EquipmentManager.Instance.InitializeDefaultEquipments();

            // パーティ初期化
            GameMain.Instance.PlayerParty.Clear();
            GameMain.Instance.PlayerParty.Add(GameInitializer.Create<CharacterPlayer00, Character>());
            GameMain.Instance.PlayerParty.Add(GameInitializer.Create<CharacterPlayer01, Character>());

            // 進行度・スイッチ
            Common.CurrentProgress = 0;
            GameSwitchManager.Instance.Clear();

            // 初期経験値／所持金
            GameMain.Instance.SetExp(0);
            GameMain.Instance.SetMoney(0);
        }
    }
}
