namespace BlazorApp.Game.Stealth
{
    /// <summary>
    /// 隠密探索盤のレベル別テンプレート
    /// </summary>
    public static class StealthBoardTemplates
    {
        public static StealthBoardSetup Level1a()
        {
            return new StealthBoardSetup
            {
                Width = 5,
                Height = 5,
                StageName = "隠密任務・初級",
                Traps = new Dictionary<TrapType, int>
                {
                    { TrapType.PoisonDart, 3 }
                }
            };
        }

        public static StealthBoardSetup Level1z()
        {
            return new StealthBoardSetup
            {
                Width = 5,
                Height = 5,
                StageName = "隠密任務・初級",
                Traps = new Dictionary<TrapType, int>
                {
                    { TrapType.PoisonDart, 4 },
                    { TrapType.BearTrap,   2 }
                }
            };
        }

        public static StealthBoardSetup Level2a()
        {
            return new StealthBoardSetup
            {
                Width = 6,
                Height = 7,
                StageName = "隠密任務・中級",
                Traps = new Dictionary<TrapType, int>
                {
                    { TrapType.PoisonDart, 4 },
                    { TrapType.BearTrap,   2 }
                }
            };
        }

        public static StealthBoardSetup Level2z()
        {
            return new StealthBoardSetup
            {
                Width = 6,
                Height = 7,
                StageName = "隠密任務・中級",
                Traps = new Dictionary<TrapType, int>
                {
                    { TrapType.PoisonDart, 7 },
                    { TrapType.BearTrap,   3 }
                }
            };
        }

        public static StealthBoardSetup Level3a()
        {
            return new StealthBoardSetup
            {
                Width = 7,
                Height = 8,
                StageName = "隠密任務・上級",
                Traps = new Dictionary<TrapType, int>
                {
                    { TrapType.PoisonDart, 7 },
                    { TrapType.BearTrap,   3 }
                }
            };
        }

        public static StealthBoardSetup Level3z()
        {
            return new StealthBoardSetup
            {
                Width = 7,
                Height = 8,
                StageName = "隠密任務・上級",
                Traps = new Dictionary<TrapType, int>
                {
                    { TrapType.PoisonDart, 9 },
                    { TrapType.BearTrap,   4 }
                }
            };
        }

        public static StealthBoardSetup Level4a()
        {
            return new StealthBoardSetup
            {
                Width = 8,
                Height = 10,
                StageName = "隠密任務・極",
                Traps = new Dictionary<TrapType, int>
                {
                    { TrapType.PoisonDart, 9 },
                    { TrapType.BearTrap, 4 }
                }
            };
        }

        public static StealthBoardSetup Level4z()
        {
            return new StealthBoardSetup
            {
                Width = 8,
                Height = 10,
                StageName = "隠密任務・極",
                Traps = new Dictionary<TrapType, int>
                {
                    { TrapType.PoisonDart, 12 },
                    { TrapType.BearTrap, 5 }
                }
            };
        }

        public static StealthBoardSetup Level5a()
        {
            return new StealthBoardSetup
            {
                Width = 8,
                Height = 10,
                StageName = "隠密任務・特殊",
                Weather = WeatherType.Stormy,
                Traps = new Dictionary<TrapType, int>
                {
                    { TrapType.PoisonDart, 12 },
                    { TrapType.BearTrap, 5 }
                }
            };
        }
    }
}
