using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontend
{
    [Serializable]
    /// <summary>
    /// 배틀 맵에서 사용되는 타일 유형. 각 항목은 <see cref="ParseSymbol"/>과 <see cref="ToSymbol"/>에서 문자 기호로 매핑됩니다.
    /// </summary>
    public enum BattleTileType
    {
        /// <summary>
        /// 비어 있는 땅 타일. 데이터 시트에서는 '.' 문자로 표현됩니다.
        /// </summary>
        Empty,

        /// <summary>
        /// 이동 불가 장애물 타일. 데이터 시트에서는 '#' 문자로 표현됩니다.
        /// </summary>
        Obstacle,

        /// <summary>
        /// 플레이어 유닛이 전투 시작 시 배치되는 스폰 지점. 데이터 시트에서는 'P' 문자로 표현됩니다.
        /// </summary>
        PlayerSpawn,

        /// <summary>
        /// 적 유닛이 전투 시작 시 배치되는 스폰 지점. 데이터 시트에서는 'E' 문자로 표현됩니다.
        /// </summary>
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

        /// <summary>
        /// 맵 정의 문자열에서 사용되는 문자 기호를 <see cref="BattleTileType"/>으로 변환합니다.
        /// '.'은 <see cref="BattleTileType.Empty"/>로, '#'은 장애물, 'P'는 플레이어 스폰, 'E'는 적 스폰으로 해석됩니다.
        /// </summary>
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

        /// <summary>
        /// <see cref="BattleTileType"/>를 맵 정의 문자열에서 사용하는 문자 기호로 변환합니다.
        /// </summary>
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
