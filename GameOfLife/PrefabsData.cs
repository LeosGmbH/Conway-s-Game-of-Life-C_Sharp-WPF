using System.Collections.Generic;

namespace GameOfLife
{
    public class PatternData
    {
        public string Name { get; set; }
        public bool[,] Cells { get; set; }
        public int Width => Cells?.GetLength(0) ?? 0;
        public int Height => Cells?.GetLength(1) ?? 0;
    }

    public static class PrefabsData
    {
        public static readonly List<PatternData> Patterns = new List<PatternData>
        {
            new PatternData 
            { 
                Name = "Glider",
                Cells = new bool[3, 3]
                {
                    { false, true, false },
                    { false, false, true },
                    { true, true, true }
                }
            },
            new PatternData 
            { 
                Name = "LWSS",
                Cells = new bool[4, 5]
                {
                    { false, true, true, true, true },
                    { true, false, false, false, true },
                    { false, false, false, false, true },
                    { true, false, false, true, false }
                }
            },
            new PatternData 
            { 
                Name = "Pulsar",
                Cells = new bool[13, 13]
                {
                    { false, false, true, true, true, false, false, false, true, true, true, false, false },
                    { false, false, false, false, false, false, false, false, false, false, false, false, false },
                    { true, false, false, false, false, true, false, true, false, false, false, false, true },
                    { true, false, false, false, false, true, false, true, false, false, false, false, true },
                    { true, false, false, false, false, true, false, true, false, false, false, false, true },
                    { false, false, true, true, true, false, false, false, true, true, true, false, false },
                    { false, false, false, false, false, false, false, false, false, false, false, false, false },
                    { false, false, true, true, true, false, false, false, true, true, true, false, false },
                    { true, false, false, false, false, true, false, true, false, false, false, false, true },
                    { true, false, false, false, false, true, false, true, false, false, false, false, true },
                    { true, false, false, false, false, true, false, true, false, false, false, false, true },
                    { false, false, false, false, false, false, false, false, false, false, false, false, false },
                    { false, false, true, true, true, false, false, false, true, true, true, false, false }
                }
            },
            new PatternData 
            { 
                Name = "Gosper Gun",
                Cells = new bool[9,36]
                {
                    { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false },
                    { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, false, false },
                    { false, false, false, false, false, false, false, false, false, false, false, false, true, true, false, false, false, false, false, false, true, true, false, false, false, false, false, false, false, false, false, false, false, false, true, true },
                    { false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, true, false, false, false, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, true, true },
                    { false, true, true, false, false, false, false, false, false, false, true, false, false, false, false, false, true, false, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
                    { false, true, true, false, false, false, false, false, false, false, true, false, false, false, true, false, true, true, false, false, false, false, true, false, true, false, false, false, false, false, false, false, false, false, false, false },
                    { false, false, false, false, false, false, false, false, false, false, true, false, false, false, false, false, true, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false },
                    { false, false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false },
                    { false, false, false, false, false, false, false, false, false, false, false, false, false, true, true, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }
                }
            }
        };
    }
}
