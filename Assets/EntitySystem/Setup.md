# Scene Setup for Player Action Demo

이 프로젝트에서 플랫폼 액션과 UI를 테스트하려면 씬에 다음 오브젝트를 배치해야 합니다.

## 1. 플레이어 루트
- 빈 GameObject를 만들고 이름을 `Player`로 지정합니다.
- `Player` 컴포넌트를 추가합니다. 이 컴포넌트는 자동으로 `PlayerActionController`를 요구하므로, Unity가 해당 스크립트를 함께 추가하도록 합니다.【F:Assets/PlayerSystem/Player.cs†L6-L22】【F:Assets/PlayerSystem/PlayerActionController.cs†L10-L214】
- `Rigidbody2D`는 `PlayerActionController`가 이동과 점프를 처리할 때 필수이며 RequireComponent로 강제됩니다.【F:Assets/PlayerSystem/PlayerActionController.cs†L11-L235】
- Rigidbody2D의 Body Type을 **Dynamic**으로 두고 *Constraints → Freeze Rotation (Z)*를 체크하면 충돌 시 회전을 막을 수 있습니다. `PlayerActionController`가 실행 중 자동으로 동일한 설정을 적용하지만, 초기값을 맞춰 두면 경고 메시지를 피할 수 있습니다.【F:Assets/PlayerSystem/PlayerActionController.cs†L173-L220】【F:Assets/PlayerSystem/PlayerActionController.cs†L806-L846】
- 땅 판정에 사용할 `groundCheck` 트랜스폼을 지정하지 않으면 기본적으로 플레이어 오브젝트의 트랜스폼을 사용합니다. 필요하다면 자식 오브젝트를 만들어 할당하세요.【F:Assets/PlayerSystem/PlayerActionController.cs†L22-L24】【F:Assets/PlayerSystem/PlayerActionController.cs†L166-L178】【F:Assets/PlayerSystem/PlayerActionController.cs†L434-L448】
- 이동/점프/공격 등에 대응하는 `InputActionReference` 필드에 프로젝트의 Input System 액션들을 연결해야 실제 입력이 동작합니다. 최소한 Move, Jump, Drop, Fire, Skill, Ultimate, Interact, Dodge, Aim 액션을 할당하세요.【F:Assets/PlayerSystem/PlayerActionController.cs†L99-L263】
- 드롭다운 플랫폼이나 지면에 사용할 레이어를 `groundLayers`와 `dropThroughLayers` 필드에 지정하면 점프/하강 판정이 정상 동작합니다.【F:Assets/PlayerSystem/PlayerActionController.cs†L54-L75】【F:Assets/PlayerSystem/PlayerActionController.cs†L434-L674】
- 트리거 효과를 쓰고 싶다면 `actionEffects` 리스트에 원하는 `ITriggerEffect` 구현체를 추가해 각 액션별 파워 조절과 버프 적용을 구성합니다.【F:Assets/PlayerSystem/PlayerActionController.cs†L127-L920】

## 2. 전투 UI 루트
- 빈 GameObject를 만들고 `Battle UI Root`처럼 구분 가능한 이름을 붙입니다.
- `BattleUIController` 스크립트를 추가하면 실행 시 자동으로 Canvas, 맵 타일, HP HUD가 생성됩니다.【F:Assets/UI/BattleUIController.cs†L8-L195】
- `mapDefinition` 필드에서 타일 문자열을 편집해 전투 격자를 원하는 형태로 설정할 수 있습니다. `EnsureValid()`가 호출되어 잘못된 크기는 자동 보정됩니다.【F:Assets/UI/BattleUIController.cs†L49-L87】【F:Assets/UI/BattleMapDefinition.cs†L17-L188】
- HUD에 표시할 엔티티를 `playerEntity`와 `enemyEntities` 필드에 드래그하여 연결하면, 각 엔티티의 체력이 실시간으로 반영됩니다.【F:Assets/UI/BattleUIController.cs†L31-L194】【F:Assets/UI/EntityHealthView.cs†L51-L205】

## 3. 참고
- 샘플 구성은 `Assets/Scenes/SampleScene.unity`에서 확인할 수 있으며, `Battle UI Root`에 `BattleUIController`가 붙어 있고 기본 맵 설정이 들어 있습니다.【F:Assets/Scenes/SampleScene.unity†L319-L408】
- UI는 런타임에 자동 생성되므로 추가 Canvas나 Event System이 필요하다면 별도로 배치하면 됩니다.
- 플레이어와 적 엔티티가 `Entity`를 상속하고 `EntityStat`을 보유해야 HP HUD가 정상적으로 업데이트됩니다.【F:Assets/EntitySystem/Entity.cs†L10-L62】【F:Assets/UI/EntityHealthView.cs†L188-L205】