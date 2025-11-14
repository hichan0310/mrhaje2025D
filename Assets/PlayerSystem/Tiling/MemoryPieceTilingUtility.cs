using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    /// <summary>
    /// Utility helpers that translate <see cref="MemoryPieceAsset"/> and related data into
    /// tiling-friendly <see cref="Cell"/> layouts.
    /// </summary>
    public static class MemoryPieceTilingUtility
    {
        private static readonly Cell[] EmptyCells = Array.Empty<Cell>();

        public static Cell[] CreateShapeSnapshot(MemoryPieceAsset asset)
        {
            return asset ? CreateShapeSnapshot(asset.ShapeCells) : EmptyCells;
        }

        public static Cell[] CreateShapeSnapshot(MemoryReinforcementZoneAsset asset)
        {
            return asset ? CreateShapeSnapshot(asset.ShapeCells) : EmptyCells;
        }

        public static Cell[] CreateShapeSnapshot(IReadOnlyList<Vector2Int> shape)
        {
            if (shape == null || shape.Count == 0)
            {
                return EmptyCells;
            }

            var result = new Cell[shape.Count];
            for (int i = 0; i < shape.Count; i++)
            {
                var offset = shape[i];
                result[i] = new Cell { x = offset.x, y = offset.y };
            }

            return result;
        }

        public static Cell[] CreateRotatedSnapshot(IReadOnlyList<Cell> localCells, int rotationSteps)
        {
            if (localCells == null || localCells.Count == 0)
            {
                return EmptyCells;
            }

            rotationSteps = NormalizeRotationSteps(rotationSteps);
            if (rotationSteps == 0)
            {
                var resultCopy = new Cell[localCells.Count];
                for (int i = 0; i < localCells.Count; i++)
                {
                    resultCopy[i] = localCells[i];
                }

                return resultCopy;
            }

            var result = new Cell[localCells.Count];
            for (int i = 0; i < localCells.Count; i++)
            {
                result[i] = Rotate(localCells[i], rotationSteps);
            }

            return result;
        }

        public static void CopyWorldCells(IReadOnlyList<Cell> localCells, Vector2Int origin, ISet<Vector2Int> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            CopyWorldCells(localCells, origin, (ICollection<Vector2Int>)destination);
        }

        public static void CopyWorldCells(IReadOnlyList<Cell> localCells, Vector2Int origin, List<Vector2Int> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            CopyWorldCells(localCells, origin, (ICollection<Vector2Int>)destination);
        }

        private static void CopyWorldCells(IReadOnlyList<Cell> localCells, Vector2Int origin,
            ICollection<Vector2Int> destination)
        {
            if (localCells == null || localCells.Count == 0)
            {
                return;
            }

            for (int i = 0; i < localCells.Count; i++)
            {
                var cell = localCells[i];
                destination.Add(new Vector2Int(origin.x + cell.x, origin.y + cell.y));
            }
        }

        public static bool FitsInsideBoard(IReadOnlyList<Cell> localCells, Vector2Int origin, Vector2Int boardSize)
        {
            if (localCells == null || boardSize.x <= 0 || boardSize.y <= 0)
            {
                return false;
            }

            for (int i = 0; i < localCells.Count; i++)
            {
                var cell = localCells[i];
                int x = origin.x + cell.x;
                int y = origin.y + cell.y;
                if (x < 0 || y < 0 || x >= boardSize.x || y >= boardSize.y)
                {
                    return false;
                }
            }

            return true;
        }

        public static int NormalizeRotationSteps(int rotationSteps)
        {
            if (rotationSteps == 0)
            {
                return 0;
            }

            int normalized = rotationSteps % 4;
            if (normalized < 0)
            {
                normalized += 4;
            }

            return normalized;
        }

        public static Cell Rotate(Cell cell, int rotationSteps)
        {
            rotationSteps = NormalizeRotationSteps(rotationSteps);
            return rotationSteps switch
            {
                1 => new Cell { x = -cell.y, y = cell.x },
                2 => new Cell { x = -cell.x, y = -cell.y },
                3 => new Cell { x = cell.y, y = -cell.x },
                _ => cell,
            };
        }
    }
}
