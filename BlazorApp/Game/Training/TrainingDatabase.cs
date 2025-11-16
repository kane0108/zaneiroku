using BlazorApp.Game;

namespace BlazorApp.Game.Training
{
    public static class TrainingDatabase
    {
        public static readonly List<TrainingDefinition> All = new()
        {
            // === 基礎修練（Y=4*64固定） ===
            new TrainingDefinition {
                Name="体力修練", Category="基礎", ExpCost=10,
                Column=0, Row=0, IconSheetX=2*64, IconSheetY=4*64,
                ApplyEffect=(p1,p2)=>{ p1.BaseStats.MaxHP+=1; p2.BaseStats.MaxHP+=1; },
                PreviewEffect=(p1,p2)=> new(){ { "体力",(1,1) } }
            },
            new TrainingDefinition {
                Name="攻撃修練", Category="基礎", ExpCost=10,
                Column=1, Row=0, IconSheetX=3*64, IconSheetY=4*64,
                ApplyEffect=(p1,p2)=>{ p1.BaseStats.Attack+=1; p2.BaseStats.Attack+=1; },
                PreviewEffect=(p1,p2)=> new(){ { "攻撃",(1,1) } }
            },
            new TrainingDefinition {
                Name="防御修練", Category="基礎", ExpCost=10,
                Column=2, Row=0, IconSheetX=4*64, IconSheetY=4*64,
                ApplyEffect=(p1,p2)=>{ p1.BaseStats.Defense+=1; p2.BaseStats.Defense+=1; },
                PreviewEffect=(p1,p2)=> new(){ { "防御",(1,1) } }
            },
            new TrainingDefinition {
                Name="敏捷修練", Category="基礎", ExpCost=10,
                Column=0, Row=1, IconSheetX=5*64, IconSheetY=4*64,
                ApplyEffect=(p1,p2)=>{ p1.BaseStats.Speed+=1; p2.BaseStats.Speed+=1; },
                PreviewEffect=(p1,p2)=> new(){ { "敏捷",(1,1) } }
            },
            new TrainingDefinition {
                Name="洞察修練", Category="基礎", ExpCost=10,
                Column=1, Row=1, IconSheetX=6*64, IconSheetY=4*64,
                ApplyEffect=(p1,p2)=>{ p1.BaseStats.Insight+=1; p2.BaseStats.Insight+=1; },
                PreviewEffect=(p1,p2)=> new(){ { "洞察",(1,1) } }
            },
            new TrainingDefinition {
                Name="翻弄修練", Category="基礎", ExpCost=10,
                Column=2, Row=1, IconSheetX=7*64, IconSheetY=4*64,
                ApplyEffect=(p1,p2)=>{ p1.BaseStats.Confuse+=1; p2.BaseStats.Confuse+=1; },
                PreviewEffect=(p1,p2)=> new(){ { "翻弄",(1,1) } }
            },

            // === 複合修練（Y=4*64固定） ===
            new TrainingDefinition {
                Name="攻防修練", Category="複合", ExpCost=20,
                Column=0, Row=0, IconSheetX=2*64, IconSheetY=4*64,
                RequiredItem="攻防の印",
                ApplyEffect=(p1,p2)=> {
                    p1.BaseStats.Attack+=2; p1.BaseStats.Defense+=1;
                    p2.BaseStats.Defense+=2; p2.BaseStats.Insight+=1;
                },
                PreviewEffect=(p1,p2)=> new(){
                    { "攻撃",(2,0) }, { "防御",(1,2) }, { "洞察",(0,1) }
                }
            },
            new TrainingDefinition {
                Name="剣速修練", Category="複合", ExpCost=20,
                Column=1, Row=0, IconSheetX=3*64, IconSheetY=4*64,
                RequiredItem="剣速の印",
                ApplyEffect=(p1,p2)=> {
                    p2.BaseStats.Speed+=2; p2.BaseStats.Attack+=1;
                    p1.BaseStats.MaxHP+=1; p1.BaseStats.Speed+=2;
                },
                PreviewEffect=(p1,p2)=> new(){
                    { "体力",(1,0) }, { "攻撃",(0,1) }, { "敏捷",(2,2) }
                }
            },
            new TrainingDefinition {
                Name="心胆修練", Category="複合", ExpCost=20,
                Column=2, Row=0, IconSheetX=4*64, IconSheetY=4*64,
                RequiredItem="心胆の印",
                ApplyEffect=(p1,p2)=> {
                    p1.BaseStats.Defense+=2; p1.BaseStats.MaxHP+=1;
                    p2.BaseStats.Insight+=2; p2.BaseStats.Defense+=1;
                },
                PreviewEffect=(p1,p2)=> new(){
                    { "体力",(1,0) }, { "防御",(2,1) }, { "洞察",(0,2) }
                }
            },
            new TrainingDefinition {
                Name="攪乱修練", Category="複合", ExpCost=20,
                Column=0, Row=1, IconSheetX=5*64, IconSheetY=4*64,
                RequiredItem="攪乱の印",
                ApplyEffect=(p1,p2)=> {
                    p2.BaseStats.Speed+=2; p2.BaseStats.Confuse+=1;
                    p1.BaseStats.Insight+=2; p1.BaseStats.Confuse+=1;
                },
                PreviewEffect=(p1,p2)=> new(){
                    { "敏捷",(0,2) }, { "翻弄",(1,1) }, { "洞察",(2,0) }
                }
            },
            new TrainingDefinition {
                Name="虚心修練", Category="複合", ExpCost=20,
                Column=1, Row=1, IconSheetX=6*64, IconSheetY=4*64,
                RequiredItem="虚心の印",
                ApplyEffect=(p1,p2)=> {
                    p1.BaseStats.Attack+=1; p1.BaseStats.Defense+=1; p1.BaseStats.MaxHP+=1;
                    p2.BaseStats.Speed+=1; p2.BaseStats.Insight+=1; p2.BaseStats.Confuse+=1;
                },
                PreviewEffect=(p1,p2)=> new(){
                    { "体力",(1,0) }, { "攻撃",(1,0) }, { "防御",(1,0) },
                    { "敏捷",(0,1) }, { "洞察",(0,1) }, { "翻弄",(0,1) }
                }
            },
            new TrainingDefinition {
                Name="鍛身修練", Category="複合", ExpCost=20,
                Column=2, Row=1, IconSheetX=7*64, IconSheetY=4*64,
                RequiredItem="鍛身の印",
                ApplyEffect=(p1,p2)=> {
                    p1.BaseStats.MaxHP+=2; p1.BaseStats.Defense+=1;
                    p2.BaseStats.MaxHP+=1; p2.BaseStats.Speed+=2;
                },
                PreviewEffect=(p1,p2)=> new(){
                    { "体力",(2,1) }, { "防御",(1,0) }, { "敏捷",(0,2) }
                }
            },

            // === 皆伝（X=1*64, Y=5*64） ===
            new TrainingDefinition {
                Name="皆伝修練", Category="複合", ExpCost=30,
                Column=1, Row=2, IconSheetX=1*64, IconSheetY=5*64,
                RequiredItem="皆伝の印",
                ApplyEffect=(p1,p2)=> {
                    foreach (var s in new[]{p1.BaseStats, p2.BaseStats})
                    {
                        s.MaxHP+=1; s.Attack+=1; s.Defense+=1;
                        s.Speed+=1; s.Insight+=1; s.Confuse+=1;
                    }
                },
                PreviewEffect=(p1,p2)=> new(){
                    { "体力",(1,1) }, { "攻撃",(1,1) }, { "防御",(1,1) },
                    { "敏捷",(1,1) }, { "洞察",(1,1) }, { "翻弄",(1,1) }
                }
            }
        };

        public static IEnumerable<TrainingDefinition> ByCategory(string category)
            => All.Where(t => t.Category == category)
                 .OrderBy(t => t.Row)
                 .ThenBy(t => t.Column);
    }
}
