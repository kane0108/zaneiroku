using System.Linq;
using System.Threading.Tasks;
using BlazorApp.Game.UIObjectFactory;

namespace BlazorApp.Game.Stealth
{
    /// <summary>
    /// 隠密探索盤のイベントスクリプト管理
    /// BattleScene.EventScripts と同様に
    /// トリガーごとの分岐 + ステージ名で判定
    /// </summary>
    public class StealthBoardEventScripts
    {
        private readonly Scene _scene;
        private readonly StealthBoardSetup _setup;

        public StealthBoardEventScripts(Scene scene, StealthBoardSetup setup)
        {
            _scene = scene;
            _setup = setup;
        }

        /// <summary>
        /// 探索盤イベントスクリプト再生
        /// </summary>
        public async Task PlayEventScriptAsync(string trigger)
        {
            string stageName = _setup.StageName ?? string.Empty;

            switch (stageName)
            {
                case "廃村の影・壱":
                    await Stage_Village01Async(trigger);
                    break;

                default:
                    // ステージ名が未設定または対応外
                    break;
            }
        }

        /// <summary>
        /// ステージごとの個別メソッド群
        /// </summary>
        private async Task Stage_Village01Async(string trigger)
        {
            var conv = new ConversationWindow();
            var rekka = _setup.Allies.FirstOrDefault(a => a.Name == "烈火") ?? _setup.Allies.First();
            var saya = _setup.Allies.FirstOrDefault(a => a.Name == "沙夜") ?? _setup.Allies.ElementAtOrDefault(1);

            switch (trigger)
            {
                case "Prologue":
                    if (Common.CurrentProgress <= Common.StoryProgress.VillageMission0)
                    {
                        await Task.Delay(1000);

                        await conv.ShowAsync(_scene, new[]
                        {
                            "……ここが廃村。\n"+
                            "敵の気配が濃い。"
                        }, saya);

                        await conv.ShowAsync(_scene, new[]
                        {
                            "気づかれぬよう影に潜み、敵を削る。\n"+
                            "……それが今回の任。"
                        }, rekka);

                        await conv.ShowAsync(_scene, new[]
                        {
                            "了解。"
                        }, saya);

                        Common.CurrentProgress = Common.StoryProgress.VillageMissionT;
                    }
                    break;

                case "Tutorial":
                    await Task.Delay(1000);

                    // 探索盤チュートリアルは初期任務で必ず表示する

                    await _scene.ShowTutorialTipsAsync(new[] {
                        "■敵地への潜入\n"+
                        " この盤面は「隠密探索盤」。\n"+
                        " 最下段から縦横斜めの隣接マスを一つずつ開き、\n"+
                        " 最上部（扉）から敵地を突破することが目的。",
                        "\n 盤上のどこかに隠された鍵を入手すると\n"+
                        " 真の扉が判明し、目的地への道が開ける。\n"+
                        " 「慎重に進め。焦れば足跡が敵の目に映る。」"
                        }, 6, offsetY: -50
                    );

                    await _scene.ShowTutorialTipsAsync(new[] {
                        "■罠および敵の潜伏\n"+
                        " この地には、さまざまな罠が仕掛けられている。\n"+
                        " 【毒矢】烈火にダメージ\n"+
                        " 【鋏罠】沙耶にダメージ\n"+
                        " 【発覚】敵に見つかり増援を呼ばれ、任務失敗"
                        }, 7, offsetY: -50
                    );

                    await _scene.ShowTutorialTipsAsync(new[] {
                        "■周囲の気配による予知\n"+
                        " 開いたマスの色は周囲に潜む罠や敵の数を示す。\n"+
                        " 【無色】…0、【水色】…1、【黄色】…2\n"+
                        " 【橙色】…3、【赤色】…それ以上\n"+
                        " 「時には回り道、慎重に観察し推理せよ。」"
                        }, 12, offsetY: -50
                    );

                    await _scene.ShowTutorialTipsAsync(new[] {
                        "■足跡と警戒度\n"+
                        " マスをタップして探索すれば、足跡が残る。\n"+
                        " その数が敵の警戒度として積み上がっていく。\n"+
                        " 警戒度が最大に達すれば任務は続行不可となる。\n"+
                        " 「不用意な探索は、敵を呼ぶ。」"
                        }, 8, offsetY: -50
                    );

                    await _scene.ShowTutorialTipsAsync(new[] {
                        "■集中状態\n"+
                        " 探索中、「集中状態」を発動可能。\n"+
                        " この状態で、罠や敵の位置を予想し敵中すれば\n"+
                        "【罠なら解除】【敵(発覚マス)なら暗殺】できる。",
                        "\n 成功すれば警戒は緩み、古い足跡も消えていく…\n"+
                        " 「痕跡を残さぬことこそ、忍びの極意。」"
                        }, 9, offsetY: -50
                    );

                    await _scene.ShowTutorialTipsAsync(new[] {
                        "■消えぬ足跡\n"+
                        " ただし、集中状態で罠のない場所を選んだり、\n"+
                        " 探索状態で罠を踏んだ箇所は赤く染まり、\n"+
                        " 二度と痕跡は消せず、警戒は残り続ける。\n"+
                        " 「忍びの油断は、永遠に刻まれる。」"
                        }, 10, offsetY: -50
                    );

                    await _scene.ShowTutorialTipsAsync(new[] {
                        "■探索の報酬\n"+
                        " 探索の途中では銭や拾い物をすることがある。\n"+
                        " また能力強化に必要な経験も積むことができる。\n"+
                        " 拾い物は無事任務を達成できたとき鑑定される。",
                        "■特別報酬\n"+
                        " 足跡・痕跡を一切残さず警戒度０で突破すれば\n"+
                        " 特別な報酬を得られる。さらに潜む敵も全滅し\n"+
                        " 突破できれば、さらなる報酬を得る。\n" +
                        " 「真の忍びは、静かに、そして完全に終える。」"
                        }, 11, offsetY: -50
                    );

                    if (Common.CurrentProgress <= Common.StoryProgress.VillageMissionT)
                    {
                        await conv.ShowAsync(_scene, new[]
                        {
                            "警戒を怠るな。\n"+
                            "…だができるだけ数を減らして突破する。"
                        }, rekka);
                    }
                    break;

                case "Epilogue":
                    if(Common.CurrentProgress <= Common.StoryProgress.VillageMissionT)
                    {
                        await Task.Delay(1000);

                        await conv.ShowAsync(_scene, new[]
                        {
                            "……動く影。\nまだ終わっていない。"
                        }, rekka);

                        await conv.ShowAsync(_scene, new[]
                        {
                            "奥の納屋から気配がする。"
                        }, saya);

                        await conv.ShowAsync(_scene, new[]
                        {
                            "残党か。痕跡を残すな。"
                        }, rekka);
                    }

                    break;
            }
        }
    }
}
