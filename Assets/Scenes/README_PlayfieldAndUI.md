# 씬 구성 개요

## 플레이어가 움직이는 공간
- `Ground`와 `Platform` 오브젝트가 기본 충돌 지형을 담당합니다.
  - 두 오브젝트는 각각 `BoxCollider2D`를 가지고 있으며 `Ground` 레이어와 `Platform` 레이어에 속해 있습니다.
  - `PlayerActionController`가 자동으로 이 레이어들을 감지해 점프/드롭 판정을 수행하므로, 동일한 레이어를 사용하면 추가 플랫폼을 쉽게 늘릴 수 있습니다.
- `BattlePlayfieldGenerator` 컴포넌트는 배틀 맵 정의(`BattleMapDefinition`)를 읽어 동일한 충돌 규칙을 가진 타일을 자동으로 생성합니다.
  - `Battle Ui Root`와 동일한 맵을 공유하도록 `Map Source` 필드에 `BattleUIController`를 연결해 두었습니다.
  - 자동 생성된 타일은 `GeneratedPlayfield` 자식 오브젝트 아래에 배치되며, 레이어/태그가 규칙에 맞춰 설정됩니다.
- 플레이어(`Player` 오브젝트)는 `Rigidbody2D`와 `Collider2D`를 통해 위 지형과 직접 상호작용합니다.
  - 입력에 따라 이동/점프/드롭이 즉시 반영되며, 씬 내 월드 좌표 기준으로 움직입니다.
  - 자동 생성된 타일을 비활성화하고 싶다면 `BattlePlayfieldGenerator`의 `inheritDefinitionFromSource`를 끄고 `Rebuild Playfield Now`를 눌러 지우거나, 컴포넌트를 비활성화하세요.

## 전투 보드 UI
- `Battle Ui Root`에는 `BattleUIController`가 붙어 있으며, 런타임에 전용 캔버스를 생성합니다.
  - 생성되는 캔버스는 기본적으로 `ScreenSpaceOverlay` 모드라서 **씬 내 월드 오브젝트와 충돌하거나 겹치지 않습니다.** 필요 시 Render Mode를 `WorldSpace`로 조정할 수 있습니다.
  - 전투 맵 정의는 `BattlePlayfieldGenerator`가 참조해 월드 지형을 생성하므로, UI는 오버레이 역할에 집중합니다.
- `BattleMapRenderer`는 생성된 캔버스 안에서만 타일 버튼을 배치합니다.
  - 기본적으로 `Image.raycastTarget`을 사용해 UI 이벤트만 처리하므로, 보드 UI에는 별도의 Collider가 필요하지 않습니다.
  - 물리 지형은 `BattleUIController`가 별도로 생성하므로 UI 타일에는 Collider를 붙이지 마세요.

## 함께 사용할 때 주의할 점
1. 플레이어용 지형은 반드시 2D Collider를 가진 월드 오브젝트로 구성합니다. 자동 생성이 필요하면 `BattlePlayfieldGenerator`를 활성화한 뒤 `Rebuild Playfield Now`를 눌러 맵 데이터를 기반으로 Collider를 배치하세요.
2. 전투 맵 UI는 HUD와 같은 오버레이로 취급하세요. 만약 월드 공간 위에 띄우고 싶다면 `BattleUIController`의 `CreateCanvas` 부분을 수정해 `WorldSpace` 모드를 사용하도록 바꿔야 합니다.
3. 씬 작업 시 UI와 월드 오브젝트가 헷갈린다면, `Battle Ui Root`를 비활성화한 뒤 플레이어 환경을 먼저 정비하고, 이후 다시 켜서 UI 배치를 확인하는 방식이 좋습니다.

## 추가 지형을 배치하는 예시
1. Hierarchy에서 빈 게임오브젝트를 만들고 이름을 `Ground Extension`으로 설정합니다.
2. `SpriteRenderer`와 `BoxCollider2D`를 추가하고, 레이어를 `Ground`로 지정합니다.
3. 크기와 위치를 조절해 기존 Ground와 연결되도록 배치합니다. 플레이어는 별다른 스크립트 수정 없이 즉시 확장된 지형을 사용할 수 있습니다.

위 구성 덕분에 플레이어는 월드 공간에서 물리 충돌을 사용해 움직이고, 전투 보드는 UI 오버레이로 독립적으로 작동합니다. 자동 생성된 지형은 언제든지 `BattlePlayfieldGenerator`를 통해 갱신하거나 제거할 수 있습니다.
