# 샘플 프리팹 컬렉션

씬에서 시스템을 빠르게 검증할 수 있도록 `Assets/Samples` 폴더에 다음과 같은 샘플 에셋을 추가했습니다.

## 비주얼 스타일
샘플 프리팹은 모두 Unity 기본 제공 스프라이트(`UISprite`)와 머티리얼을 활용하며, 각 요소는 컬러 값만으로 식별할 수 있도록 구성되어 있습니다.
바이너리 텍스처 파일이 포함되지 않아도 즉시 확인할 수 있도록 설계되었습니다.

## 스크립터블 오브젝트
- `SampleSpawnProjectileEffect` : `SpawnProjectileEffectAsset` 기반의 3연발 사격 이펙트
- `SampleMemoryPiece` : 사격 트리거에 위 이펙트를 연결한 메모리 조각
- `SampleReinforcementZone` : 메모리 강화 영역 예시

## 프리팹
- `SampleProjectile` : 충돌 판정이 적용된 탄환 프리팹
- `SampleImpactEffect` : 간단한 타격 파티클 이펙트
- `SampleHpBar` : `SimpleFillBar` 스크립트를 사용해 왼쪽 정렬 게이지를 표현하는 HP 바
- `SampleMemoryTerminal` : 상호작용 시 메모리 조각을 지급하는 터미널
- `SamplePlatform` : `StageLayoutBuilder`와 함께 사용할 수 있는 기본 플랫폼 타일
- `SampleStageLayout` : 자동으로 다층 플랫폼을 생성하는 스테이지 빌더 세팅
- `SamplePlayer` : 이동/사격/메모리 연동이 모두 셋업된 플레이어

각 프리팹은 고유 색상으로 구분됩니다. 예를 들어 플레이어는 하늘색, 탄환은 황금색, 플랫폼은 회색, 터미널은 노란색 톤으로 채색되어 있어 외부 텍스처 없이도 식별이 가능합니다.

## 유틸리티 스크립트
- `SimpleFillBar` : 자식 트랜스폼의 X 스케일을 조절해 게이지 형태를 연출하는 샘플 컴포넌트

> **Tip.** `SamplePlayer` 프리팹을 씬에 배치하고 `SampleStageLayout`과 `SampleMemoryTerminal`, `SampleHpBar`를 함께 두면 전체 플레이 루프를 빠르게 확인할 수 있습니다.
