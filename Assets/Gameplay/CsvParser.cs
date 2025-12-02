using System;
using System.Collections.Generic;
using UnityEngine;

public class GridLevelLoader : MonoBehaviour
{
    [Header("CSV 파일")]
    public TextAsset csvFile;

    [Serializable]
    public class TokenPrefab
    {
        public string token;      // 예: "W", "주", "전투로봇", "깡패"
        public GameObject prefab;
    }

    [Header("타입별 프리팹 매핑")]
    public List<TokenPrefab> tokenPrefabs = new List<TokenPrefab>();

    // 내부용 딕셔너리
    private Dictionary<string, GameObject> prefabMap;

    [Header("좌표 설정")]
    public float cellSize = 1f;          // 한 칸당 유니티 단위 크기
    public Vector2 originOffset = Vector2.zero; // 맵 전체 오프셋

    [Header("플레이어 시작점 처리")]
    public bool spawnPlayerFromToken = true;
    public string playerToken = "주";    // CSV에서 플레이어 위치 표시용
    public GameObject playerPrefab;      // 플레이어 프리팹

    private void Awake()
    {
        BuildPrefabMap();
        LoadGridFromCsv();
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

    private void LoadGridFromCsv()
    {
        if (csvFile == null)
        {
            Debug.LogError("csvFile이 설정되지 않았습니다.");
            return;
        }

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
        // 실제 데이터는 1번 줄부터
        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] cells = line.Split(',');

            if (cells.Length == 0)
                continue;

            string yLabel = cells[0].Trim(); // 예: "y15", "y0", 또는 빈칸

            // y행이 아닌 설명 줄이면 스킵 (예: "폐기된 로봇들의 산 느낌" 있는 줄)
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

                // 플레이어 토큰이면 따로 처리
                if (spawnPlayerFromToken && rawToken == playerToken)
                {
                    if (playerPrefab != null)
                    {
                        SpawnAtGrid(playerPrefab, xCol - 1, yIndex); // xCol-1 == x번호
                    }
                    continue;
                }

                // 나머지는 타입별 프리팹 매핑에서 찾기
                if (!prefabMap.TryGetValue(rawToken, out GameObject prefab))
                {
                    Debug.LogWarning($"알 수 없는 토큰: {rawToken} (line {lineIndex}, col {xCol})");
                    continue;
                }

                SpawnAtGrid(prefab, xCol - 1, yIndex);
            }
        }

        Debug.Log("그리드 맵 로드 완료");
    }

    private void SpawnAtGrid(GameObject prefab, int gridX, int gridY)
    {
        Vector3 worldPos = new Vector3(
            gridX * cellSize + originOffset.x,
            gridY * cellSize + originOffset.y,
            0f
        );

        Instantiate(prefab, worldPos, Quaternion.identity, this.transform);
    }
}
