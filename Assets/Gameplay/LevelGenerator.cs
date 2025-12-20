using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GridLevelBuilderWindow : EditorWindow
{
    [Serializable]
    public class TokenPrefab
    {
        public string token;      // 예: "W", "주", "전투로봇", "깡패"
        public GameObject prefab;
    }

    private TextAsset csvFile;

    private List<TokenPrefab> tokenPrefabs = new List<TokenPrefab>();
    private Dictionary<string, GameObject> prefabMap;

    private float cellSize = 1f;
    private Vector2 originOffset = Vector2.zero;

    private bool spawnPlayerFromToken = true;
    private string playerToken = "주";
    private GameObject playerPrefab;

    private Transform parentTransform;
    private bool clearChildrenBeforeBuild = true;

    // 메뉴에서 열기
    [MenuItem("Tools/Grid Level Builder")]
    public static void ShowWindow()
    {
        GetWindow<GridLevelBuilderWindow>("Grid Level Builder");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("CSV 기반 그리드 맵 생성", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV 파일", csvFile, typeof(TextAsset), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("타입별 프리팹 매핑", EditorStyles.boldLabel);

        // 토큰-프리팹 리스트 그리기
        int removeIndex = -1;
        for (int i = 0; i < tokenPrefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            tokenPrefabs[i].token = EditorGUILayout.TextField(tokenPrefabs[i].token, GUILayout.Width(80));
            tokenPrefabs[i].prefab = (GameObject)EditorGUILayout.ObjectField(tokenPrefabs[i].prefab, typeof(GameObject), false);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
        {
            tokenPrefabs.RemoveAt(removeIndex);
        }

        if (GUILayout.Button("+ 토큰 추가"))
        {
            tokenPrefabs.Add(new TokenPrefab());
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("좌표 설정", EditorStyles.boldLabel);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
        originOffset = EditorGUILayout.Vector2Field("Origin Offset", originOffset);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("플레이어 토큰 처리", EditorStyles.boldLabel);
        spawnPlayerFromToken = EditorGUILayout.Toggle("Spawn Player From Token", spawnPlayerFromToken);
        if (spawnPlayerFromToken)
        {
            playerToken = EditorGUILayout.TextField("Player Token", playerToken);
            playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player Prefab", playerPrefab, typeof(GameObject), false);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("생성 위치", EditorStyles.boldLabel);
        parentTransform = (Transform)EditorGUILayout.ObjectField("Parent Transform", parentTransform, typeof(Transform), true);
        clearChildrenBeforeBuild = EditorGUILayout.Toggle("기존 자식 삭제", clearChildrenBeforeBuild);

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(csvFile == null);
        if (GUILayout.Button("Build Grid"))
        {
            BuildGrid();
        }
        EditorGUI.EndDisabledGroup();
    }

    private void BuildGrid()
    {
        if (csvFile == null)
        {
            Debug.LogError("CSV 파일이 설정되지 않았습니다.");
            return;
        }

        // 부모 없으면 새로 생성
        if (parentTransform == null)
        {
            GameObject parentGO = new GameObject("GridRoot");
            Undo.RegisterCreatedObjectUndo(parentGO, "Create GridRoot");
            parentTransform = parentGO.transform;
        }
        else
        {
            // Undo 등록 (되돌리기 가능)
            Undo.RegisterFullObjectHierarchyUndo(parentTransform.gameObject, "Build Grid From CSV");
        }

        // 기존 자식 삭제 옵션
        if (clearChildrenBeforeBuild)
        {
            ClearChildrenInEditor(parentTransform);
        }

        BuildPrefabMap();
        LoadGridFromCsvAndSpawn();

        Debug.Log("에디터에서 그리드 생성 완료", parentTransform);
    }

    private void BuildPrefabMap()
    {
        prefabMap = new Dictionary<string, GameObject>();

        foreach (var entry in tokenPrefabs)
        {
            if (entry.prefab == null || string.IsNullOrEmpty(entry.token))
                continue;

            if (!prefabMap.ContainsKey(entry.token))
            {
                prefabMap.Add(entry.token, entry.prefab);
            }
            else
            {
                Debug.LogWarning($"중복 token: {entry.token}");
            }
        }
    }

    private void LoadGridFromCsvAndSpawn()
    {
        string[] lines = csvFile.text.Split(
            new[] { '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries
        );

        if (lines.Length <= 1)
        {
            Debug.LogError("CSV 내용이 비어 있거나 헤더만 있습니다.");
            return;
        }

        // 0번 줄은 헤더: y\x,x0,x1,...
        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] cells = line.Split(',');

            if (cells.Length == 0)
                continue;

            string yLabel = cells[0].Trim(); // 예: "y15", "y0", 또는 빈칸

            // y행이 아닌 설명 줄이면 스킵
            if (string.IsNullOrEmpty(yLabel) || !yLabel.StartsWith("y"))
                continue;

            // y값 파싱 (y15 -> 15)
            if (!int.TryParse(yLabel.Substring(1), out int yIndex))
            {
                Debug.LogWarning($"y 인덱스 파싱 실패: {yLabel}");
                continue;
            }

            // x0 ~ xN 순회 (cells[1]부터)
            for (int xCol = 1; xCol < cells.Length; xCol++)
            {
                string rawToken = cells[xCol].Trim();

                // 빈칸 / . / \ 이런 건 무시
                if (string.IsNullOrEmpty(rawToken) || rawToken == "." || rawToken == "\\")
                    continue;

                // 플레이어 토큰 처리
                if (spawnPlayerFromToken && rawToken == playerToken)
                {
                    if (playerPrefab != null)
                    {
                        SpawnAtGrid(playerPrefab, xCol - 1, yIndex);
                    }
                    continue;
                }

                // 일반 토큰
                if (!prefabMap.TryGetValue(rawToken, out GameObject prefab))
                {
                    Debug.LogWarning($"알 수 없는 토큰: {rawToken} (line {lineIndex}, col {xCol})");
                    continue;
                }

                SpawnAtGrid(prefab, xCol - 1, yIndex);
            }
        }
    }

    private void SpawnAtGrid(GameObject prefab, int gridX, int gridY)
    {
        Vector3 worldPos = new Vector3(
            gridX * cellSize + originOffset.x,
            gridY * cellSize + originOffset.y,
            0f
        );

        // Prefab 연결 유지하면서 에디터에 생성
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parentTransform);
        if (instance != null)
        {
            instance.transform.position = worldPos;
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Grid Cell");
        }
    }

    private void ClearChildrenInEditor(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }
}

