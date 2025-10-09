# Battle Map Tiles

이 문서는 `BattleMapRenderer`와 `BattleMapDefinition`이 생성하는 UI 전투 맵 타일의 의미와 상호작용 방식을 정리합니다.

## 타일 유형과 문자 기호
- `.` (`Empty`): 이동 가능한 일반 지형.
- `#` (`Obstacle`): 이동 및 배치가 불가능한 장애물.
- `P` (`PlayerSpawn`): 전투 시작 시 플레이어 유닛이 배치되는 스폰 지점.
- `E` (`EnemySpawn`): 전투 시작 시 적 유닛이 배치되는 스폰 지점.

맵 데이터는 문자열 행(row)으로 구성되며, 위의 문자 기호를 사용해 각 좌표의 지형을 표현합니다. `BattleMapDefinition.ParseSymbol`과 `BattleMapDefinition.ToSymbol`이 이 문자-타입 매핑을 처리합니다.

## Collider 필요 여부
`BattleMapRenderer`는 전투 맵을 UI(Canvas) 위에 그리기 때문에, 물리 충돌을 위한 Collider가 필요하지 않습니다. 각 타일은 `UnityEngine.UI.Image` 컴포넌트를 가지고 있으며, `Image.raycastTarget` 옵션을 통해 그래픽 레이캐스트(UI 이벤트 시스템)로 클릭/터치를 감지합니다. Inspector에서 "Interaction" 섹션의 **Enable Tile Raycasts** 옵션이 켜져 있으면 Collider 없이도 Pointer 이벤트를 받을 수 있습니다.

Physics(2D/3D) 기반 상호작용이 필요한 경우 `BattlePlayfieldGenerator` 컴포넌트를 사용해 동일한 맵 정의로 Collider를 자동 생성하거나, 별도의 월드 오브젝트에 Collider 컴포넌트를 직접 추가해야 합니다.
