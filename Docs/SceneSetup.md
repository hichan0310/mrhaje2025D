# 씬 셋업 가이드

이 문서는 `PlayerSystem`과 `Gameplay` 폴더에 포함된 플랫폼 슈터 프로토타입을 Unity 씬에서 구성하는 방법을 요약합니다. 기본 사용자는 Unity 2021 LTS 이상을 가정합니다.

## 1. 필수 에셋 준비

### 1.1 메모리 관련 스크립터블 오브젝트

1. **메모리 피스**: `Create > Player > Memory > Piece` 메뉴에서 `MemoryPieceAsset`을 생성합니다.
   - `Trigger Type`: 어떤 플레이어 동작이 이 피스를 발동시키는지 지정합니다.
   - `Effect`: `TriggerEffectAsset` 인스턴스를 연결합니다 (아래 참조).
   - `Shape Cells`: 격자 배치 시 차지하는 셀 오프셋을 정의합니다.
   - `Resource Type`/`Cost`: 발동에 필요한 자원 소비가 있다면 지정합니다.
2. **강화 구역**: `Create > Player > Memory > Reinforcement Zone`을 통해 `MemoryReinforcementZoneAsset`을 생성합니다. 강화 영역은 붙어있는 메모리 피스의 파워 배율을 증가시킵니다.
3. **트리거 효과**: 다음 메뉴에서 원하는 효과를 생성하고 매개변수를 설정합니다.
   - `Create > Player > Trigger Effects > Spawn Projectile`
   - `Create > Player > Trigger Effects > Gain Resource`
   - `Create > Player > Trigger Effects > Stat Buff`

### 1.2 무기 & 기타

- **Projectile 프리팹**: `Assets/PlayerSystem/Weapons/Projectile` 스크립트를 사용하는 프리팹을 준비합니다. `Rigidbody2D`, `Collider2D` 등을 연결하고 `Projectile` 컴포넌트의 속도를 설정합니다.
- **플랫폼 프리팹**: `StageLayoutBuilder`가 생성할 기본 발판 프리팹을 만들고, Collider2D와 필요한 Tile/Sprite Renderer를 추가합니다.

## 2. 플레이어 프리팹 구성

1. 씬에 빈 GameObject를 만들고 이름을 `Player`로 지정합니다.
2. 다음 컴포넌트를 추가합니다.
   - `Player` (자동으로 `Rigidbody2D`, `Collider2D`, `PlayerMemoryBinder`가 요구됩니다)
   - 필요 시 애니메이터나 비주얼 컴포넌트를 추가합니다.
3. `Player` 컴포넌트 필드를 설정합니다.
   - **Movement**: 속도, 점프 파라미터를 입력하고 `Ground Check` 트랜스폼과 `Ground Mask`를 지정합니다.
   - **Combat**: `Default Projectile` 프리팹, `Fire Point` 트랜스폼, `Fallback Skill/Ultimate Effect`를 각각 연결합니다.
   - **Mobility/Interaction/Input**: 프로젝트 입력에 맞추어 키 설정 및 상호작용 레이어를 조정합니다.
4. 자식 오브젝트로 `GroundCheck`와 `FirePoint`를 만들어 필드에 할당합니다.

## 3. 플레이어 메모리 보드 설정

`Player` 객체의 `PlayerMemoryBinder` 컴포넌트를 선택하여 아래 항목을 구성합니다.

1. **Board Settings**
   - `Grid Size`: 메모리 격자의 가로/세로 셀 수를 지정합니다.
   - `Resources`: `MemoryResourcePool` 목록을 필요 개수만큼 추가해 각 자원 타입, 최대치, 초당 회복량을 설정합니다.
   - `Starting Pieces`: 시작 시 자동으로 배치될 메모리 피스를 지정합니다.
     - `Piece`: `MemoryPieceAsset` 참조.
     - `Origin`: 격자 내 좌표 (왼쪽 아래가 `(0,0)`).
     - `Power Multiplier`: 해당 피스의 파워 배율.
     - `Locked`: `true`로 설정하면 런타임에 제거되지 않습니다.
   - `Reinforcement Zones`: 강화 구역 에셋과 배치 좌표를 지정합니다.
2. 플레이 중 메모리 구성을 테스트하려면 `PlayerMemoryBinder`의 `Trigger(...)`를 호출하거나 `MemoryTerminal`과 상호작용합니다.

### 3.1 메모리 보드 오버레이 UI 구성

메모리 보드와 인벤토리를 시각화하려면 `MemoryBoardOverlay`, `MemoryBoardCellView`, `MemoryPieceInventoryItemView`
스크립트를 기반으로 한 UI를 준비해야 합니다.

1. **캔버스 준비**
   - 씬에 `Canvas`를 추가하고 `Screen Space - Overlay` 모드를 사용합니다.
   - `Canvas`에 `CanvasGroup` 컴포넌트를 추가합니다.
   - 같은 오브젝트에 `MemoryBoardOverlay` 컴포넌트를 붙이고 아래 필드를 채울 수 있도록 빈 자식 오브젝트를 만들어 둡니다.

2. **보드 그리드 루트**
   - 캔버스의 자식으로 `BoardGrid`(임의 이름) `RectTransform`을 만들고 `GridLayoutGroup`을 추가합니다.
   - `Cell Size`는 메모리 셀 한 칸의 픽셀 크기로, `Spacing`으로 셀 간격을 조정합니다.
   - 이 `RectTransform`을 `MemoryBoardOverlay.boardGridRoot`에 할당합니다.

3. **보드 셀 프리팹**
   - `UI > Button`으로 새 프리팹을 만들고 `MemoryBoardCellView` 스크립트를 붙입니다.
   - 버튼 루트에 `Image`를 유지하여 배경으로 사용하고, `MemoryBoardCellView.backgroundImage`에 연결합니다.
   - 자식으로 다음 요소를 추가하고 스크립트 필드에 연결합니다.
     - `ReinforcementHighlight`: 투명도를 가진 `Image`, `MemoryBoardCellView.reinforcementHighlight`.
     - `PieceIcon`: `Image` 컴포넌트.
     - `PieceLabel`: `TextMeshProUGUI`.
     - `LockIndicator`: 잠금 상태를 표시할 아이콘 `GameObject`.
   - 준비된 프리팹을 `MemoryBoardOverlay.cellPrefab`에 지정합니다.

4. **인벤토리 스크롤 영역**
   - 캔버스에 `InventoryScroll`(임의 이름) 오브젝트를 만들고 `ScrollRect`와 `Image` 컴포넌트를 추가합니다. `Image`는 배경과 마스크 역할을 겸할 수 있습니다.
   - `ScrollRect`의 자식으로 `Viewport` `RectTransform`을 만들고 `Mask`(또는 `RectMask2D`)를 붙입니다. 그 안에 `InventoryContent` `RectTransform`을 추가해 스크롤되는 컨텐츠 루트를 구성합니다.
   - `InventoryContent`에는 `VerticalLayoutGroup`과 `ContentSizeFitter`(Vertical Fit을 `Preferred Size`)를 붙여 세로 방향으로 아이템이 자연스럽게 쌓이도록 합니다.
   - `ScrollRect`에서 `Horizontal` 체크는 끄고 `Vertical`만 활성화합니다. `Movement Type`은 `Clamped`를 권장합니다.
   - `InventoryScroll`의 `ScrollRect`를 `MemoryBoardOverlay.inventoryScrollRect` 필드에, `InventoryContent` `RectTransform`을 `inventoryContentRoot`에, `VerticalLayoutGroup`을 `inventoryLayoutGroup` 필드에 각각 연결합니다.

5. **인벤토리 아이템 프리팹**
   - `UI > Button`으로 새 프리팹을 만들고 `MemoryPieceInventoryItemView`를 붙입니다.
   - 프리팹 안에 다음 요소를 배치하고 필드에 연결합니다.
     - 아이콘 표시용 `Image` → `iconImage`.
     - 이름/배율 표기를 위한 `TextMeshProUGUI` → `nameLabel`.
     - 소지 개수 텍스트용 `TextMeshProUGUI` → `countLabel`.
     - 선택 시 강조할 `GameObject` → `selectionHighlight`.
   - 이 프리팹을 `MemoryBoardOverlay.inventoryItemPrefab`에 할당합니다.

6. **기타 UI 참조**
   - 닫기 버튼을 하나 배치하고 `MemoryBoardOverlay.closeButton`에 연결합니다.
   - 선택 중인 조각을 표시할 `TextMeshProUGUI`를 만들고 `selectedPieceLabel`에 연결합니다.
   - 인벤토리 영역의 간격과 여백은 `Inventory Item Spacing`, `Inventory Padding Top`, `Inventory Padding Bottom` 필드로 조정하며, 설정값은 연결된 `VerticalLayoutGroup`에 바로 반영됩니다.
   - 초기 상태에서 오버레이가 보이지 않도록 `CanvasGroup.alpha = 0`, `Interactable/Blocks Raycasts = false`로 두어도 됩니다.

## 4. 메모리 터미널 배치 (선택)

씬에 배치한 UI만으로는 플레이어가 상호작용할 수 있는 월드 오브젝트가 존재하지 않으므로, `MemoryTerminal`을 가시적인 형태로
직접 만들어 두어야 합니다.

1. 월드에 빈 GameObject를 만들고 `MemoryTerminal` 컴포넌트를 추가합니다.
2. **가시 요소 추가**
   - 같은 오브젝트에 `SpriteRenderer` 또는 `MeshRenderer`를 붙여 플레이어가 알아볼 수 있는 모델/스프라이트를 지정합니다.
   - 필요하다면 자식에 `Canvas`(World Space)와 상호작용 안내 텍스트를 배치해도 됩니다.
3. **충돌체/레이어 설정**
   - `CircleCollider2D`(또는 원하는 모양의 Collider)를 추가하고 `Is Trigger`를 켭니다.
   - 이 오브젝트의 레이어를 `Player`의 `Interact Mask`가 포함한 레이어(예: `Interactable`)로 지정합니다.
4. `Grants` 배열에 플레이어가 상호작용했을 때 부여할 메모리 피스를 설정합니다.
   - `Piece`: 지급할 `MemoryPieceAsset`.
   - `Position`: 보드 내 배치 좌표.
   - `Power Multiplier`: 적용할 배율.
5. `Player` 스크립트의 `Interact Key`로 터미널과 상호작용하면 인벤토리에 조각이 추가되고, `openOverlayOnInteract`가 켜져 있다면
   연결된 오버레이가 열립니다.
6. 씬에 배치한 `MemoryBoardOverlay` 인스턴스를 `MemoryTerminal.overlayReference` 필드에 연결하면 해당 오버레이를 사용하고,
   필드를 비워 두면 씬 내 첫 번째 `MemoryBoardOverlay`를 자동 탐색하여 사용합니다.

## 5. 스테이지 레이아웃 빌더 사용

1. 씬에 빈 GameObject를 만들고 `StageLayoutBuilder` 컴포넌트를 추가합니다.
2. `Platform Prefab` 필드에 준비한 플랫폼 프리팹을 할당합니다.
3. `Stages` 배열에서 각 맵의 층 수, 층 간 간격(`Floor Offset`), 층당 세그먼트 수 등을 설정합니다.
4. `Auto Build`가 켜져 있으면 플레이 시 자동으로 맵이 생성됩니다. 에디터에서 미리보기하려면 컴포넌트의 `Build Stage` 컨텍스트 메뉴를 사용하고, `Clear Stage`로 제거할 수 있습니다.

## 6. 입력 & 카메라 참고 사항

- `Player` 스크립트는 기본 Unity Input Manager 축(`Horizontal`)과 `KeyCode` 기반 입력을 사용합니다. 새로운 입력 시스템을 사용할 경우 스크립트를 수정하거나 키 설정을 변경하십시오.
- 카메라 추적이 필요하면 `Cinemachine` 등 별도 카메라 시스템을 추가하고 플레이어를 추적 대상으로 지정합니다.

## 7. 테스트 체크리스트

- 씬을 재생하여 이동/점프/대시/회피/발사가 정상 동작하는지 확인합니다.
- 각 동작이 대응하는 트리거를 호출해 메모리 피스가 발동하는지 콘솔 로그 또는 디버그 UI로 검증합니다.
- `MemoryTerminal` 상호작용으로 보드 구성이 변경되는지 확인합니다.
- `StageLayoutBuilder`가 원하는 맵 형태를 생성하는지 확인합니다.

위 절차를 통해 씬에서 메모리 기반 강화 시스템과 다층 맵을 갖춘 2D 횡스크롤 플랫포머를 빠르게 구성할 수 있습니다.
