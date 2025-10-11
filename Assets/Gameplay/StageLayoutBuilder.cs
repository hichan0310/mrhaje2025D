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
            public Vector2 segmentSize;   // �� ���׸�Ʈ�� ��ǥ ũ��(���� ����)
            public Vector2 floorOffset;   // (x: �� ���� X, y: ���� Y ����)
        }

        [SerializeField] private GameObject platformPrefab = null;
        [SerializeField]
        private StageDefinition[] stages =
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
                Build();
        }

        [ContextMenu("Build Stage")]
        public void Build()
        {
            if (!platformPrefab)
            {
                Debug.LogWarning("StageLayoutBuilder requires a platform prefab.");
                return;
            }

            if (clearOnBuild) Clear();

            foreach (var stage in stages)
                BuildStage(stage);
        }

        // === �� ���� ������ 1���� ��Ƽ� '���' ����� ���� ===
        private void BuildStage(StageDefinition definition)
        {
            float targetH = definition.segmentSize.y;
            float targetWPerSeg = definition.segmentSize.x;
            float totalW = definition.segmentsPerFloor * targetWPerSeg;

            for (int floor = 0; floor < definition.floors; floor++)
            {
                // ���� ������(����)�� floorOffset.x�� �ΰ�,
                // SpriteRenderer�� '���� ����'�̶� �߾����� 0.5*totalW��ŭ �̵�
                Vector2 floorBase = new(definition.floorOffset.x, floor * definition.floorOffset.y);
                Vector3 centerPos = transform.position + new Vector3(floorBase.x + totalW * 0.5f, floorBase.y, 0f);

                var go = Instantiate(platformPrefab, centerPos, Quaternion.identity, transform);
                spawnedPlatforms.Add(go);

                // �������� 1�� �ΰ� SpriteRenderer�� drawMode/size�� ��Ȯ�� ä���
                go.transform.localScale = Vector3.one;

                var sr = go.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    // Ÿ��/�����̽��� �ƴϸ� ������ Ÿ�� ����
                    if (sr.drawMode == SpriteDrawMode.Simple)
                        sr.drawMode = SpriteDrawMode.Tiled;

                    // size�� '���� ����' ���� (Transform scale�� ���� �� �������� scale=1 ������ �ٽ�)
                    sr.size = new Vector2(totalW, targetH);
                }
                else
                {
                    // Sprite�� ���� ���(�޽� ��)�� ���������� '���� ������' ����
                    Vector2 baseSize = GetBaseBounds(go);
                    float sx = totalW / Mathf.Max(baseSize.x, 1e-6f);
                    float sy = targetH / Mathf.Max(baseSize.y, 1e-6f);
                    go.transform.localScale = new Vector3(sx, sy, 1f);
                }

                // �ݶ��̴��� SpriteRenderer�� ������ ũ��� ���� (���� ����)
                var box = go.GetComponentInChildren<BoxCollider2D>();
                if (box != null)
                {
                    box.size = new Vector2(totalW, targetH);
                    box.offset = Vector2.zero;
                }

                // �̼� ������ ����(�ȼ� ����)
                var p = go.transform.position;
                go.transform.position = new Vector3(
                    Mathf.Round(p.x * 1000f) / 1000f,
                    Mathf.Round(p.y * 1000f) / 1000f,
                    p.z
                );
            }
        }

        private static Vector2 GetBaseBounds(GameObject go)
        {
            // scale = 1�� ���� �뷫�� ���� ũ�� ����
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                return sr.sprite.bounds.size; // �̹� ���� ����

            var rend = go.GetComponentInChildren<Renderer>();
            if (rend != null) return rend.bounds.size;

            var col2d = go.GetComponentInChildren<Collider2D>();
            if (col2d != null) return col2d.bounds.size;

            return Vector2.one;
        }

        [ContextMenu("Clear Stage")]
        public void Clear()
        {
            for (int i = spawnedPlatforms.Count - 1; i >= 0; i--)
            {
                if (spawnedPlatforms[i])
                {
                    if (Application.isPlaying) Destroy(spawnedPlatforms[i]);
                    else DestroyImmediate(spawnedPlatforms[i]);
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
                float totalW = stage.segmentsPerFloor * stage.segmentSize.x;
                for (int floor = 0; floor < stage.floors; floor++)
                {
                    // �߾� ���� �ڽ��� �׸���
                    Vector3 center = transform.position + new Vector3(stage.floorOffset.x + totalW * 0.5f, floor * stage.floorOffset.y, 0f);
                    Vector3 size = new Vector3(totalW, stage.segmentSize.y, 1f);
                    Gizmos.DrawCube(center, size);
                }
            }
        }
#endif
    }
}
