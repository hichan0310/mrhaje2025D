# MemoryPiece 시스템 리팩터링 개요

본 문서는 `PlayerSystem.Tiling` 기반으로 진행된 최근 리팩터링의 모든 변경 사항을 코드 원리 중심으로 정리한 것입니다. 구형 `Polyomino`-중심 경로와 중복된 배치 계산을 제거하고, 메모리 보드/인벤토리/오버레이가 하나의 타일링 파이프라인을 공유하도록 한 내용을 빠짐없이 설명합니다.

## 1. 타일링 스냅샷 공통 유틸리티 (`MemoryPieceTilingUtility`)
* 새 정적 클래스(`Assets/PlayerSystem/Tiling/MemoryPieceTilingUtility.cs`)가 `MemoryPieceAsset`, `MemoryReinforcementZoneAsset` 등에서 정의한 로컬 좌표(`Vector2Int`)를 곧바로 `PlayerSystem.Tiling.Cell` 배열로 복제합니다. 이로써 MonoBehaviour 경로마다 따로 `Cell`을 생성하던 중복 코드가 사라졌습니다.
* `CreateRotatedSnapshot`/`Rotate`/`NormalizeRotationSteps`는 90° 단위 회전을 정규화하여 4개의 회전 상태(0~3)만 허용하며, `FitsInsideBoard`는 보드 경계 체크를 캡슐화합니다. 이 함수들을 통해 메모리 보드, 바인더, 오버레이에서 동일한 좌표계를 사용하게 됩니다.
* `CopyWorldCells`는 로컬 셀을 원점(`origin`) 기준 월드 좌표 리스트/집합으로 변환하면서 재사용 가능한 버퍼에 결과를 저장합니다. 이 방식은 `MemoryBoard`의 충돌 검사나 강화 구역 겹침 계산에서 곧바로 쓰입니다.

## 2. 에셋 수준의 회전 캐싱 (`MemoryPieceAsset`, `MemoryReinforcementZoneAsset`)
* `MemoryPieceAsset`에 `cachedRotationCells` 필드를 추가하고, `GetTilingCells(int rotationSteps)` 메서드가 `MemoryPieceTilingUtility`를 이용해 0~3 회전 각각의 `Cell[]`을 미리 계산합니다. OnEnable/OnValidate 시 캐시를 무효화하여 에디터에서 모양을 수정해도 즉시 반영됩니다.
* `MemoryReinforcementZoneAsset` 역시 `cachedTilingCells`를 보관하여, 강화 영역 배치가 매 프레임 리스트를 다시 생성하지 않고 동일 스냅샷을 재사용하도록 했습니다. 이는 강화 영역과 메모리 피스가 동일 좌표계를 쓴다는 점을 보장합니다.

## 3. `MemoryBoard` 런타임 구조 개편
* `MemoryPieceRuntime`가 생성될 때 `asset.GetTilingCells(rotationSteps)` 결과를 고정 스냅샷으로 저장하고, `MemoryPieceTilingUtility.CopyWorldCells`로 월드 좌표 집합을 만든 뒤 모든 배치 및 트리거 검증이 이 집합을 참조하게 했습니다.
* 신규 `rotationSteps` 필드가 배치·직렬화·트리거 경로 전반에 전파되어, 보드가 각 피스의 회전 상태를 기억하고 `MemoryPiecePlacementInfo` 로 UI에 돌려줍니다.
* `IsPlacementValid`가 `FitsInsideBoard` 및 공유 버퍼(`placementCellBuffer`)를 사용해 경계와 충돌을 한 번에 검사합니다. `MemoryBoard.CanPlacePiece` 공개 메서드는 이 로직을 외부(UI/바인더)가 재사용하도록 노출합니다.
* `TryAddPieceInternal`은 트리거 호환성, 중복 배치 여부, 회전 정규화를 검사한 뒤에만 `MemoryPieceRuntime`을 만들어 `startingPieces`에도 회전 정보를 기록합니다. 덕분에 저장/로드 시 정확한 회전 상태를 복원할 수 있습니다.
* 트리거 실행(`TriggerRuntimePiece`)은 강화 영역 보정, 자원 소비, 쿨다운 등의 기존 규칙을 유지하면서도 캐싱된 셀 정보를 바탕으로 `MemoryReinforcementRuntime`과 겹침을 계산하여 최종 배율을 구합니다.

## 4. `PlayerMemoryBinder`와 배치 질의 통합
* 바인더는 `board.CanPlacePiece`를 래핑하는 `CanPlacePiece` 메서드를 공개하여, UI나 다른 시스템이 특정 보드/회전에서 배치 가능 여부를 미리 검사할 수 있게 했습니다.
* `TryPlaceInventoryPiece`는 이제 보드별 회전/원점 정보를 전달하고, 성공 시 인벤토리에서 동일 항목을 제거한 뒤 보드가 발동하는 `OnPieceAdded` 이벤트와 연동됩니다. `pieceOwnership` 사전은 트리거별 소속을 추적하여 삭제 요청 시 정확한 보드를 찾아냅니다.

## 5. `MemoryBoardOverlay` UI/입력 경로 정비
* 오버레이는 `MemoryPieceTilingUtility`를 사용하여 선택된 인벤토리 항목의 회전을 관리하고(`selectedRotationSteps`), R 키 또는 회전 버튼 입력으로 `MemoryPieceTilingUtility.NormalizeRotationSteps`를 호출해 항상 0~3 범위를 유지합니다.
* `HandleCellClicked`는 셀이 이미 점유되었는지 확인 후, 잠금 여부를 고려하여 `boundBinder.RemovePiece` 또는 `TryPlaceInventoryPiece`를 호출합니다. 배치 전에 `CanPlaceSelectedItemAt`이 `PlayerMemoryBinder.CanPlacePiece`를 호출함으로써, UI 자체가 좌표 계산을 중복 수행하지 않습니다.
* 선택 라벨(`selectedPieceLabel`)은 현재 트리거 탭, 배율, 회전 각도를 모두 실시간 표시하여 플레이어가 “한 폴리오미노 = 한 효과”라는 모델을 시각적으로 이해할 수 있게 돕습니다.
* 보드 탭 레이아웃, 인벤토리 레이아웃, 스크롤 설정 등 UI 배치 속성도 코드 내에서 강제 설정하여 에디터 설정 실수로 인한 불일치를 방지했습니다.

## 6. 시스템 간 일관성 확보 효과
* 모든 배치 검증과 좌표 변환이 `MemoryPieceTilingUtility` → `MemoryPieceAsset/MemoryBoard` → `PlayerMemoryBinder` → `MemoryBoardOverlay` 순으로 공유되므로, 회전·경계·충돌 규칙이 단일 소스에서 결정됩니다.
* 메모리 피스가 보드에 한 번 배치되면 `MemoryPieceRuntime`과 `MemoryPiecePlacementInfo`가 동일 데이터를 참조하기 때문에, 트리거 실행·UI 표시·저장 데이터 간 불일치가 발생하지 않습니다.
* 강화 영역(`MemoryReinforcementZoneAsset`)도 동일 스냅샷 체계를 이용하여, 효과 배수 계산이 항상 UI의 하이라이트와 정확히 맞아떨어집니다.

위 항목들이 본 리팩터링에서 적용된 모든 코드 변경 사항입니다.
