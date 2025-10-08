using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontend
{
    [Serializable]
    public enum BattleTileType
    {
        Empty,
        Obstacle,
        PlayerSpawn,
        EnemySpawn,
    }

    [Serializable]
    public class BattleMapDefinition
    {
        [SerializeField]
        private Vector2Int size = new Vector2Int(6, 4);

        [SerializeField]
        private List<string> rows = new List<string>
        {
            "P...E.",
            ".#....",
            ".#EE..",
            "P...#.",
        };

        public int Width
        {
            get => Mathf.Max(1, size.x);
            set => size.x = Mathf.Max(1, value);
        }

        public int Height
        {
            get => Mathf.Max(1, size.y);
            set => size.y = Mathf.Max(1, value);
        }

        public IReadOnlyList<string> Rows => rows;

        public static BattleMapDefinition Default()
        {
            var definition = new BattleMapDefinition();
            definition.EnsureValid();
            return definition;
        }

        public BattleMapDefinition Clone()
        {
            EnsureValid();

            var clone = new BattleMapDefinition
            {
                size = size,
                rows = new List<string>(rows.Count),
            };

            foreach (var row in rows)
            {
                var safeRow = row ?? string.Empty;
                clone.rows.Add(new string(safeRow.ToCharArray()));
            }

            return clone;
        }

        public void EnsureValid()
        {
            if (rows == null)
            {
                rows = new List<string>();
            }

            size.x = Mathf.Max(1, size.x);
            size.y = Mathf.Max(1, size.y);

            NormalizeRowCount();
            NormalizeRowWidth();
        }

        public void ForEachTile(Action<int, int, BattleTileType> callback)
        {
            if (callback == null)
            {
                return;
            }

            EnsureValid();
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    callback.Invoke(x, y, GetTile(x, y));
                }
            }
        }

        public BattleTileType GetTile(int x, int y)
        {
            if (rows == null || rows.Count == 0)
            {
                return BattleTileType.Empty;
            }

            x = Mathf.Clamp(x, 0, Width - 1);
            y = Mathf.Clamp(y, 0, Height - 1);

            var rowIndex = rows.Count - 1 - y;
            rowIndex = Mathf.Clamp(rowIndex, 0, rows.Count - 1);
            var row = rows[rowIndex] ?? string.Empty;

            if (row.Length <= x)
            {
                return BattleTileType.Empty;
            }

            return ParseSymbol(row[x]);
        }

        public void SetTile(int x, int y, BattleTileType type)
        {
            EnsureValid();

            x = Mathf.Clamp(x, 0, Width - 1);
            y = Mathf.Clamp(y, 0, Height - 1);

            var rowIndex = rows.Count - 1 - y;
            var row = rows[rowIndex];
            var characters = row.ToCharArray();
            characters[x] = ToSymbol(type);
            rows[rowIndex] = new string(characters);
        }

        public static BattleTileType ParseSymbol(char symbol)
        {
            return symbol switch
            {
                '#' => BattleTileType.Obstacle,
                'P' => BattleTileType.PlayerSpawn,
                'E' => BattleTileType.EnemySpawn,
                _ => BattleTileType.Empty,
            };
        }

        public static char ToSymbol(BattleTileType type)
        {
            return type switch
            {
                BattleTileType.Obstacle => '#',
                BattleTileType.PlayerSpawn => 'P',
                BattleTileType.EnemySpawn => 'E',
                _ => '.',
            };
        }

        private void NormalizeRowCount()
        {
            while (rows.Count < Height)
            {
                rows.Insert(0, new string('.', Width));
            }

            while (rows.Count > Height)
            {
                rows.RemoveAt(0);
            }
        }

        private void NormalizeRowWidth()
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i] ?? string.Empty;

                if (row.Length < Width)
                {
                    rows[i] = row.PadRight(Width, '.');
                }
                else if (row.Length > Width)
                {
                    rows[i] = row.Substring(0, Width);
                }
            }
        }
    }
}
