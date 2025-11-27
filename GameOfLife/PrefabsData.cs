using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GameOfLife
{
    public class PatternData
    {
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public bool[,] Cells { get; set; } = new bool[0, 0];
        public int Width => Cells?.GetLength(0) ?? 0;
        public int Height => Cells?.GetLength(1) ?? 0;
    }

    public static class PrefabsData
    {
        private const string PrefabFileName = "prefabs.json";

        private static readonly Lazy<IReadOnlyList<PatternData>> _patterns = new(() => LoadPatterns());

        public static IReadOnlyList<PatternData> Patterns => _patterns.Value;

        public static PatternData? GetPatternByName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return Patterns.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<PatternData> LoadPatterns()
        {
            try
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var filePath = Path.Combine(baseDirectory, PrefabFileName);

                if (!File.Exists(filePath))
                {
                    return Array.Empty<PatternData>();
                }

                using var stream = File.OpenRead(filePath);
                var config = JsonSerializer.Deserialize<PrefabConfig>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config?.Prefabs == null)
                {
                    return Array.Empty<PatternData>();
                }

                var patterns = new List<PatternData>();

                foreach (var prefab in config.Prefabs)
                {
                    var cells = ConvertToBoolArray(prefab);
                    if (cells == null)
                    {
                        continue;
                    }

                    patterns.Add(new PatternData
                    {
                        Name = prefab.Name ?? string.Empty,
                        Group = prefab.Group ?? string.Empty,
                        Cells = cells
                    });
                }

                return patterns;
            }
            catch
            {
                return Array.Empty<PatternData>();
            }
        }

        private static bool[,]? ConvertToBoolArray(PrefabEntry prefab)
        {
            if (prefab.Grid == null || prefab.Grid.Count == 0)
            {
                return null;
            }

            int height = prefab.Grid.Count;
            int width = prefab.Grid[0]?.Count ?? 0;

            if (width == 0)
            {
                return null;
            }

            var cells = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                var row = prefab.Grid[y];
                if (row == null || row.Count != width)
                {
                    return null;
                }

                for (int x = 0; x < width; x++)
                {
                    cells[x, y] = row[x] == 1;
                }
            }

            return cells;
        }

        private class PrefabConfig
        {
            public List<PrefabEntry>? Prefabs { get; set; }
        }

        private class PrefabEntry
        {
            public string? Name { get; set; }
            public string? Group { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public List<List<int>>? Grid { get; set; }
        }
    }
}
