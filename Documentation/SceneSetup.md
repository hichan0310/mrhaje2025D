# Scene Setup for Player Action Demo

이 프로젝트에서 플랫폼 액션과 UI를 테스트하려면 씬에 다음 오브젝트를 배치해야 합니다.

## 1. 플레이어 루트
- 빈 GameObject를 만들고 이름을 `Player`로 지정합니다.
- `Player` 컴포넌트를 추가합니다. 이 컴포넌트는 자동으로 `PlayerActionController`를 요구하므로, Unity가 해당 스크립트를 함께 추가하도록 합니다.【F:Assets/PlayerSystem/Player.cs†L6-L22】【F:Assets/PlayerSystem/PlayerActionController.cs†L10-L214】
- `Rigidbody2D`는 `PlayerActionController`가 이동과 점프를 처리할 때 필수이며 RequireComponent로 강제됩니다.【F:Assets/PlayerSystem/PlayerActionController.cs†L11-L235】
- 플레이어 오브젝트에는 `Collider2D`(Capsule, Box 등)를 반드시 추가해야 실제로 지면과 충돌하여 떨어지지 않습니다. 스크립트가 자동으로 요구하므로 빠뜨리면 Unity가 경고와 함께 Collider를 추가하도록 안내합니다.【F:Assets/PlayerSystem/PlayerActionController.cs†L10-L25】
- 땅 판정에 사용할 `groundCheck` 트랜스폼을 지정하지 않으면 기본적으로 플레이어 오브젝트의 트랜스폼을 사용합니다. 필요하다면 자식 오브젝트를 만들어 할당하세요.【F:Assets/PlayerSystem/PlayerActionController.cs†L22-L24】【F:Assets/PlayerSystem/PlayerActionController.cs†L166-L178】【F:Assets/PlayerSystem/PlayerActionController.cs†L434-L448】
- 이동/점프/공격 등에 대응하는 `InputActionReference` 필드에 프로젝트의 Input System 액션들을 연결해야 실제 입력이 동작합니다. 최소한 Move, Jump, Drop, Fire, Skill, Ultimate, Interact, Dodge, Aim 액션을 할당하세요.【F:Assets/PlayerSystem/PlayerActionController.cs†L99-L263】
- 드롭다운 플랫폼이나 지면에 사용할 레이어를 `groundLayers`와 `dropThroughLayers` 필드에 지정하면 점프/하강 판정이 정상 동작합니다. 기본값은 `Ground`, `Default`, `OneWayPlatform` 레이어를 자동으로 포함하며, 필요하면 직접 수정할 수 있습니다.【F:Assets/PlayerSystem/PlayerActionController.cs†L51-L82】【F:Assets/PlayerSystem/PlayerActionController.cs†L434-L674】
- 플레이어가 착지할 바닥에도 `Collider2D`가 붙어 있어야 하며, `groundLayers`에 해당 레이어를 포함시켜야 합니다. 프로젝트에는 `Ground`와 `OneWayPlatform` 레이어가 미리 정의돼 있으므로, 바닥과 일방 통과 플랫폼에 각각 지정하면 바로 동작합니다.【F:ProjectSettings/TagManager.asset†L1-L26】【F:Assets/PlayerSystem/PlayerActionController.cs†L51-L82】【F:Assets/PlayerSystem/PlayerActionController.cs†L434-L451】
- 트리거 효과를 쓰고 싶다면 `actionEffects` 리스트에 원하는 `ITriggerEffect` 구현체를 추가해 각 액션별 파워 조절과 버프 적용을 구성합니다.【F:Assets/PlayerSystem/PlayerActionController.cs†L127-L920】

## 2. 전투 UI 루트
- 빈 GameObject를 만들고 `Battle UI Root`처럼 구분 가능한 이름을 붙입니다.
- `BattleUIController` 스크립트를 추가하면 실행 시 자동으로 Canvas, 맵 타일, HP HUD가 생성됩니다.【F:Assets/UI/BattleUIController.cs†L8-L195】
- `mapDefinition` 필드에서 타일 문자열을 편집해 전투 격자를 원하는 형태로 설정할 수 있습니다. `EnsureValid()`가 호출되어 잘못된 크기는 자동 보정됩니다.【F:Assets/UI/BattleUIController.cs†L49-L87】【F:Assets/UI/BattleMapDefinition.cs†L17-L188】
- HUD에 표시할 엔티티를 `playerEntity`와 `enemyEntities` 필드에 드래그하여 연결하면, 각 엔티티의 체력이 실시간으로 반영됩니다.【F:Assets/UI/BattleUIController.cs†L31-L194】【F:Assets/UI/EntityHealthView.cs†L51-L205】

## 3. 참고
- 샘플 구성은 `Assets/Scenes/SampleScene.unity`에서 확인할 수 있으며, `Battle UI Root`에 `BattleUIController`가 붙어 있고 기본 맵 설정이 들어 있습니다. 씬에는 `Ground Platform`(Ground 레이어)과 `One Way Platform`(OneWayPlatform 레이어) 오브젝트가 미리 배치되어 있어, 추가 세팅 없이 점프와 드롭이 테스트 가능합니다.【F:Assets/Scenes/SampleScene.unity†L319-L428】
- UI는 런타임에 자동 생성되므로 추가 Canvas나 Event System이 필요하다면 별도로 배치하면 됩니다.
- 플레이어와 적 엔티티가 `Entity`를 상속하고 `EntityStat`을 보유해야 HP HUD가 정상적으로 업데이트됩니다.【F:Assets/EntitySystem/Entity.cs†L10-L62】【F:Assets/UI/EntityHealthView.cs†L188-L205】
