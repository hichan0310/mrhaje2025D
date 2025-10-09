using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Simple stage generator that creates multi-floor arenas described in the design document.
    /// </summary>
    public class StageLayoutBuilder : MonoBehaviour
    {
        [System.Serializable]
        private struct StageDefinition
        {
            public string name;
            [Range(1, 20)] public int floors;
            [Range(1, 10)] public int segmentsPerFloor;
            public Vector2 segmentSize;
            public Vector2 floorOffset;
        }

        [SerializeField] private GameObject platformPrefab = null;
        [SerializeField] private StageDefinition[] stages =
        {
            new StageDefinition { name = "Stage A", floors = 10, segmentsPerFloor = 6, segmentSize = new Vector2(3f, 1f), floorOffset = new Vector2(0f, 3f) },
            new StageDefinition { name = "Stage B", floors = 10, segmentsPerFloor = 6, segmentSize = new Vector2(3f, 1f), floorOffset = new Vector2(24f, 3f) },
            new StageDefinition { name = "Stage C", floors = 10, segmentsPerFloor = 6, segmentSize = new Vector2(3f, 1f), floorOffset = new Vector2(48f, 3f) },
            new StageDefinition { name = "Stage D", floors = 10, segmentsPerFloor = 6, segmentSize = new Vector2(3f, 1f), floorOffset = new Vector2(72f, 3f) },
        };
        [SerializeField] private bool autoBuild = true;
        [SerializeField] private bool clearOnBuild = true;

        private readonly List<GameObject> spawnedPlatforms = new();

        private void Start()
        {
            if (autoBuild)
            {
                Build();
            }
        }

        [ContextMenu("Build Stage")]
        public void Build()
        {
            if (!platformPrefab)
            {
                Debug.LogWarning("StageLayoutBuilder requires a platform prefab.");
                return;
            }

            if (clearOnBuild)
            {
                Clear();
            }

            foreach (var stage in stages)
            {
                BuildStage(stage);
            }
        }

        private void BuildStage(StageDefinition definition)
        {
            for (int floor = 0; floor < definition.floors; floor++)
            {
                Vector2 floorBase = new(definition.floorOffset.x, floor * definition.floorOffset.y);
                for (int segment = 0; segment < definition.segmentsPerFloor; segment++)
                {
                    Vector3 position = transform.position + new Vector3(
                        floorBase.x + segment * definition.segmentSize.x,
                        floorBase.y,
                        0f);

                    var platform = Instantiate(platformPrefab, position, Quaternion.identity, transform);
                    platform.transform.localScale = new Vector3(definition.segmentSize.x, definition.segmentSize.y, 1f);
                    spawnedPlatforms.Add(platform);
                }
            }
        }

        [ContextMenu("Clear Stage")]
        public void Clear()
        {
            for (int i = spawnedPlatforms.Count - 1; i >= 0; i--)
            {
                if (spawnedPlatforms[i])
                {
                    if (Application.isPlaying)
                    {
                        Destroy(spawnedPlatforms[i]);
                    }
                    else
                    {
                        DestroyImmediate(spawnedPlatforms[i]);
                    }
                }
            }

            spawnedPlatforms.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.35f);
            foreach (var stage in stages)
            {
                for (int floor = 0; floor < stage.floors; floor++)
                {
                    Vector3 center = transform.position + new Vector3(stage.floorOffset.x, floor * stage.floorOffset.y, 0f);
                    Vector3 size = new Vector3(stage.segmentSize.x * stage.segmentsPerFloor, stage.segmentSize.y, 1f);
                    Gizmos.DrawCube(center + new Vector3((stage.segmentsPerFloor - 1) * stage.segmentSize.x * 0.5f, 0f, 0f), size);
                }
            }
        }
#endif
    }
}
