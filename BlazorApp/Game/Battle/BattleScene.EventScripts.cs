using BlazorApp.Game.BackgroundFactory;
using BlazorApp.Game.CharacterFactory;
using BlazorApp.Game.UIObjectFactory;
using System.Drawing;
using System.Threading.Tasks;
using static BlazorApp.Game.Common;

namespace BlazorApp.Game.Battle
{
    /// <summary>
    /// バトル中イベント（勝利・敗北・掛け合いなど）
    /// </summary>
    public partial class BattleScene
    {
        /// <summary>
        /// イベントスクリプトを発火
        /// </summary>
        /// <param name="trigger">"Start", "Mid", "Victory", "Defeat" など</param>
        public async Task PlayEventScriptAsync(string trigger)
        {
            switch (_setup.StageName)
            {
                case "プロローグ":
                    await PlayEvent_PrologueAsync(trigger);
                    break;

                case "廃村の影・壱":
                    await PlayEvent_Village01Async(trigger);
                    break;

                default:
                    await PlayEvent_DefaultAsync(trigger);
                    break;
            }
        }

        // === プロローグ戦闘中・勝利後演出 ===
        private async Task PlayEvent_PrologueAsync(string trigger)
        {
            var conv = new ConversationWindow();
            var rekka = _setup.Allies[0];
            var saya = _setup.Allies[1];

            switch (trigger)
            {
                // タイトル表示
                case "Title":
                    _ = ShowTelopAsync("「密命の影」", "序章");
                    break;

                case "Prologue":
                    var enemy1 = GameInitializer.Create<CharacterEnemy02, Character>();
                    var enemy2 = GameInitializer.Create<CharacterEnemy01, Character>();
                    var enemy3 = GameInitializer.Create<CharacterEnemy02, Character>();
                    var enemy4 = GameInitializer.Create<CharacterEnemy02, Character>();
                    var enemy5 = GameInitializer.Create<CharacterEnemy02, Character>();

                    Characters.Clear();

                    rekka.ObjectId = 0;
                    rekka.Type = "Player";
                    rekka.MirrorHorizontal = false;
                    rekka.PosX = -200;
                    rekka.PosY = 120;
                    rekka.BattlePosX = rekka.PosX;
                    rekka.BattlePosY = rekka.PosY;
                    rekka.EnableFormationLerp = true;
                    Characters[rekka.ObjectId] = rekka;

                    saya.ObjectId = 1;
                    saya.Type = "Player";
                    saya.MirrorHorizontal = false;
                    saya.PosX = -200;
                    saya.PosY = 160;
                    saya.BattlePosX = saya.PosX;
                    saya.BattlePosY = saya.PosY;
                    saya.EnableFormationLerp = true;
                    Characters[saya.ObjectId] = saya;

                    enemy1.ObjectId = 2;
                    enemy1.Type = "Enemy";
                    enemy1.Name = "護衛";
                    enemy1.MirrorHorizontal = true;
                    enemy1.PosX = 340;
                    enemy1.PosY = 120;
                    enemy1.BattlePosX = enemy1.PosX;
                    enemy1.BattlePosY = enemy1.PosY;
                    enemy1.EnableFormationLerp = true;
                    Characters[enemy1.ObjectId] = enemy1;

                    enemy2.ObjectId = 3;
                    enemy2.Type = "Enemy";
                    enemy2.Name = "役人";
                    enemy2.MirrorHorizontal = true;
                    enemy2.PosX = 320;
                    enemy2.PosY = 170;
                    enemy2.BattlePosX = enemy2.PosX;
                    enemy2.BattlePosY = enemy2.PosY;
                    enemy2.EnableFormationLerp = true;
                    Characters[enemy2.ObjectId] = enemy2;

                    enemy3.ObjectId = 4;
                    enemy3.Type = "Enemy";
                    enemy3.Name = "巡回兵1";
                    enemy3.MirrorHorizontal = true;
                    enemy3.PosX = 320;
                    enemy3.PosY = 80;
                    enemy3.BattlePosX = enemy3.PosX;
                    enemy3.BattlePosY = enemy3.PosY;
                    enemy3.EnableFormationLerp = true;
                    Characters[enemy3.ObjectId] = enemy3;

                    enemy4.ObjectId = 5;
                    enemy4.Type = "Enemy";
                    enemy4.Name = "巡回兵2";
                    enemy4.MirrorHorizontal = true;
                    enemy4.PosX = 320;
                    enemy4.PosY = 200;
                    enemy4.BattlePosX = enemy4.PosX;
                    enemy4.BattlePosY = enemy4.PosY;
                    enemy4.EnableFormationLerp = true;
                    Characters[enemy4.ObjectId] = enemy4;

                    enemy5.ObjectId = 6;
                    enemy5.Type = "Enemy";
                    enemy5.Name = "巡回兵3";
                    enemy5.MirrorHorizontal = true;
                    enemy5.PosX = 320;
                    enemy5.PosY = 320;
                    enemy5.BattlePosX = enemy5.PosX;
                    enemy5.BattlePosY = enemy5.PosY;
                    enemy5.EnableFormationLerp = true;
                    Characters[enemy5.ObjectId] = enemy5;

#if !DEVTOOLS
                    await Task.Delay(1500);

                    rekka.BattlePosX = 90;
                    saya.BattlePosX = 40;

                    await Task.Delay(1500);

                    await conv.ShowAsync(this, new[] {
                        "標的はあの役人だ。\n密書を持ち、官軍に通じている。"
                    }, rekka, mirrorRight: false);

                    await conv.ShowAsync(this, new[] {
                        "護衛は一人。……巡回に隙あり。"
                    }, saya, mirrorRight: false);

                    await conv.ShowAsync(this, new[] {
                        "首尾よくやれ。風魔の刃に、無駄は要らぬ。"
                    }, rekka, mirrorRight: false);

                    // 同時に「回避」アニメ＋フェードアウト
                    rekka.PlayAnimation("回避");
                    saya.PlayAnimation("回避");

                    rekka.BattlePosX -= 20;
                    rekka.BattlePosY -= 20;
                    saya.BattlePosX -= 20;
                    saya.BattlePosY -= 20;

                    // 徐々に透明にする
                    float fadeDuration = 0.5f;
                    int steps = 5;
                    for (int i = 0; i <= steps; i++)
                    {
                        float t = i / (float)steps;
                        float opacity = 1f - t;
                        rekka.Opacity = opacity;
                        saya.Opacity = opacity;
                        await Task.Delay((int)(fadeDuration * 1000 / steps));
                    }

                    await Task.Delay(1000);

                    for (int i = 0; i <= 15; i++)
                    {
                        enemy1.BattlePosX -= 10;
                        enemy2.BattlePosX -= 12;
                        await Task.Delay(200);
                    }

                    rekka.BattlePosX = 250;
                    rekka.BattlePosY = 120;
                    rekka.MirrorHorizontal = true;
                    saya.BattlePosX = 80;
                    saya.BattlePosY = 170;

                    await Task.Delay(500);

                    await conv.ShowAsync(this, new[] {
                        "…この村も、いずれ官軍の手に落ちよう。",
                        "忍どもに縋（すが）るなど、時代錯誤よ。"
                    }, enemy2, mirrorRight: true);

                    // 烈火・徐々に透明解除する
                    for (int i = 0; i <= 10; i++)
                    {
                        rekka.Opacity = 0.1f * i;
                        await Task.Delay(100);
                    }

                    await conv.ShowAsync(this, new[] {
                        "……口が過ぎるな。風魔の務めだ。……闇へ還れ。"
                    }, rekka, mirrorRight: true);

                    // 突きアニメ
                    rekka.PlayAnimation("穿・攻撃");

                    await Task.Delay(500);

                    // 強制死亡
                    enemy1.CurrentStats.KillInstantly();

                    enemy2.PosX += 20;
                    enemy2.MirrorHorizontal = false;

                    await conv.ShowAsync(this, new[] {
                        "な、何者だっ！"
                    }, enemy2, mirrorRight: false);

                    // 沙耶・徐々に透明解除する
                    for (int i = 0; i <= 10; i++)
                    {
                        saya.Opacity = 0.1f * i;
                        await Task.Delay(100);
                    }

                    await conv.ShowAsync(this, new[] {
                        "……声を立てたら、ここで終わる。"
                    }, saya, mirrorRight: false);

                    // 突きアニメ
                    saya.PlayAnimation("穿・攻撃");

                    await Task.Delay(300);

                    // 強制死亡
                    enemy2.CurrentStats.KillInstantly();

                    await Task.Delay(500);

                    await conv.ShowAsync(this, new[] {
                        "……任務完了。"
                    }, saya, mirrorRight: false);

                    rekka.PosX += 20;
                    rekka.MirrorHorizontal = false;

                    await conv.ShowAsync(this, new[] {
                        "いや……まだ終わっていない。"
                    }, rekka, mirrorRight: false);

                    // 同時に「回避」アニメ＋フェードアウト
                    rekka.PlayAnimation("回避");
                    saya.PlayAnimation("回避");

                    rekka.BattlePosX -= 20;
                    rekka.BattlePosY -= 20;
                    saya.BattlePosX -= 20;
                    saya.BattlePosY -= 20;

                    // 徐々に透明にする
                    fadeDuration = 0.5f;
                    steps = 5;
                    for (int i = 0; i <= steps; i++)
                    {
                        float t = i / (float)steps;
                        float opacity = 1f - t;
                        rekka.Opacity = opacity;
                        saya.Opacity = opacity;
                        // 敵も透明にする
                        enemy1.Opacity = opacity;
                        enemy2.Opacity = opacity;
                        await Task.Delay((int)(fadeDuration * 1000 / steps));
                    }

                    rekka.BattlePosX = 10;
                    rekka.BattlePosY = 80;
                    saya.BattlePosX = 10;
                    saya.BattlePosY = 200;

                    rekka.PosX = rekka.BattlePosX;
                    rekka.PosY = rekka.BattlePosY;
                    saya.PosX = saya.BattlePosX;
                    saya.PosY = saya.BattlePosY;

                    // 徐々に透明解除する
                    for (int i = 0; i <= 10; i++)
                    {
                        rekka.Opacity = 0.1f * i;
                        saya.Opacity = 0.1f * i;
                        await Task.Delay(100);
                    }

                    enemy3.BattlePosX = 360 - 128 - 10;
                    await Task.Delay(200);
                    enemy4.BattlePosX = 360 - 128 - 10;
                    await Task.Delay(200);
                    enemy5.BattlePosX = 360 - 128 - 10;

                    await conv.ShowAsync(this, new[] {
                        "追手？……三人。"
                    }, saya, mirrorRight: false);

                            await conv.ShowAsync(this, new[] {
                        "官軍の巡回だな。\n型の違い、見極めて斬れ。"
                    }, rekka, mirrorRight: false);

                    await Task.Delay(2000);
#endif
                    Characters.Clear();
                    OneTimeSetup();
                    break;

                // 勝利時
                case "Victory":
                    await Task.Delay(800);

                    // UI削除
                    ClearAllBattleUI();

                    await conv.ShowAsync(this, new[] {
                        "……片付いた。"
                    }, saya, mirrorRight: false);

#if !DEVTOOLS
                    await conv.ShowAsync(this, new[] {
                        "動きを読むとは、型を奪うことだ。\n"+
                        "だが忘れるな。相手もまた、お前を見ている。"
                    }, rekka, mirrorRight: false);

                    await conv.ShowAsync(this, new[] {
                        "…なら、迷いは捨てる。斬るだけ。"
                    }, saya, mirrorRight: false);

                    await conv.ShowAsync(this, new[] {
                        "それでいい。迷いは影を濁らせる。",
                        "任務完了。月が沈む前に撤収する。",
                    }, rekka, mirrorRight: false);
#endif

                    await Task.Delay(800);

                    var chunks = new List<string>
                    {
                        "　　　　――月が沈んでも、影は消えぬ。\n\n"+
                        "　　　　　　影を斬り、痕を絶つ。\n\n"+
                        "　　　　　　「風魔」に夜明けはない…",
                    };

                    await GameMain.Instance.CurrentScene.RunPrologueOverlayAsync(
                        chunks,
                        maskOpacity: 0.7f,
                        fadeInSec: 0.8f,
                        fadeOutSec: 0.4f);

                    var kotaro = GameInitializer.Create<CharacterEnemyKotaro, Character>();
                    kotaro.Type = "Player";

                    // 背景フェードアウト→切り替え→フェードイン
                    await FadeBackgroundAsync(
                        BackgroundSingle.Create("images/bg001.png"),
                        fadeSeconds: 1.5f
                    );

                    await Task.Delay(1000);

                    await conv.ShowAsync(this, new[] {
                        "報告は、俺がする。お前は休め。"
                    }, rekka, mirrorRight: false);

                    await conv.ShowAsync(this, new[] {
                        "命令なら、従う。"
                    }, saya, mirrorRight: true);

                    // 屋敷に移動
                    await FadeBackgroundAsync(
                        BackgroundSingle.Create("images/bg016.png"),
                        fadeSeconds: 1.5f
                    );

                    await Task.Delay(1000);

                    await conv.ShowAsync(this, new[] {
                        "戻ったか、烈火。……任は果たしたな。"
                    }, kotaro, mirrorRight: true);

                    await conv.ShowAsync(this, new[] {
                        "密書を奪取。役人と護衛は全滅。"
                    }, rekka, mirrorRight: false);

                    await conv.ShowAsync(this, new[] {
                        "よくやった。風魔の刃に乱れなし。\n……沙耶も初陣にしては、よく動いた。"
                    }, kotaro, mirrorRight: true);

                    await Task.Delay(1000);

                    await conv.ShowAsync(this, new[] {
                        "次の命だ。",
                        "南にある無名の廃村……\nその地が、最近になって火を上げた。",
                        "調べでは、正体不明の一団が占拠している。\n夜な夜な物資を奪い、人を攫っていると聞く。"
                    }, kotaro, mirrorRight: true);

                    await conv.ShowAsync(this, new[] {
                        "官軍ではない……？"
                    }, rekka, mirrorRight: false);

                    await conv.ShowAsync(this, new[] {
                        "違う。掟を持たぬ者ども――野盗か、\nあるいは別の“影”かもしれぬ。\n"+
                        "…だが、どちらにせよ邪魔者は斬れ。"
                    }, kotaro, mirrorRight: true);

                    await conv.ShowAsync(this, new[] {
                        "了。\n出立は、明け方に。"
                    }, rekka, mirrorRight: false);

                    await conv.ShowAsync(this, new[] {
                        "それでよい。\nどうやら敵の数はかなり多いようだ。\n真正面から挑めば、多勢に無勢だ。",
                        "目立つ動きは禁物。\n一人でも気づけば、全てが台無しだ。\n"+
                        "我らを警戒して罠も仕掛けられている可能性がある。"
                    }, kotaro, mirrorRight: true);

                    await conv.ShowAsync(this, new[] {
                        "潜入……殲滅、了解。"
                    }, rekka, mirrorRight: false);

                    // ストーリー進行
                    Common.CurrentProgress = StoryProgress.VillageMission0;

                    break;

                // 敗北時
                case "Defeat":
                    await ShowTelopAsync("敗北", holdSeconds: 3.0f, maskOpacity: 0.7f);
                    break;
            }
        }

        // === 村ステージ戦闘イベント ===
        private async Task PlayEvent_Village01Async(string trigger)
        {
            var conv = new ConversationWindow();
            var rekka = _setup.Allies[0];
            var saya = _setup.Allies[1];

            switch (trigger)
            {
                // タイトル表示
                case "Title":
                    _ = ShowTelopAsync("仕合開始");
                    OneTimeSetup();
                    break;

                case "Victory":
                    if (Common.CurrentProgress < Common.StoryProgress.VillageMission1)
                    {
                        await conv.ShowAsync(this, new[] {
                            "……すべて、倒した。"
                        }, saya);

                        await conv.ShowAsync(this, new[] {
                            "ああ。だが、妙だな。\nこれほど荒らされているのに、金品は手つかずだ。"
                        }, rekka);

                        await conv.ShowAsync(this, new[] {
                            "盗みに来た者たちではなかった、ということ？"
                        }, saya);

                        await conv.ShowAsync(this, new[] {
                            "そうなる。野盗どもは……“使われた”側。\n背後に、手を引く者がいる。"
                        }, rekka);

                        await conv.ShowAsync(this, new[] {
                            "風の流れに、違う匂い。刃を扱う者の気配…"
                        }, saya);

                        await conv.ShowAsync(this, new[] {
                            "忍びか。"
                        }, rekka);

                        await conv.ShowAsync(this, new[] {
                            "……かもしれない。"
                        }, saya);

                        await conv.ShowAsync(this, new[] {
                            "確かめるしかない。\n体制を整え奥へ進む。"
                        }, rekka);

                        await conv.ShowAsync(this, new[] {
                            "了解。"
                        }, saya);

                        Common.CurrentProgress = Common.StoryProgress.VillageMission1;
                    }
                    break;

                // 敗北時
                case "Defeat":
                    await ShowTelopAsync("敗北", holdSeconds: 3.0f, maskOpacity: 0.7f);
                    break;
            }
        }

        // === デフォルト ===
        private async Task PlayEvent_DefaultAsync(string trigger)
        {
            switch (trigger)
            {
                // タイトル表示
                case "Title":
                    _ = ShowTelopAsync("仕合開始");
                    OneTimeSetup();
                    break;

                // 敗北時
                case "Defeat":
                    await ShowTelopAsync("敗北", holdSeconds: 3.0f, maskOpacity: 0.7f);
                    break;
            }
        }
    }
}
