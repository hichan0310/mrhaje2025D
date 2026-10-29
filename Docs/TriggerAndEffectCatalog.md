# 트리거·이펙트 종류 및 수치 현황

**2026-10-28 추가:** [6슬롯 장비 결합 화면](EquipmentTerminal.md)을 구현했다. ESC로 열고 슬롯마다 트리거 1개 + 효과 1개를 연결한다. 트리거 10종·기존 효과 19종을 새 카탈로그에 연결했으며 도형 배치 없이 파워를 전달한다.

기존 수치 조사 기준: 2026-10-23, 저장소의 C# 코드와 직렬화된 에셋·프리팹을 확인했다. 2026-10-28 전투 이벤트 연결 수정 및 격리 Unity 검증 결과는 [장비 화면 문서](EquipmentTerminal.md#전투-이벤트-연결-수정-2026-10-28)에 추가했다.

이 문서는 현재 구현을 새 장비 기획으로 옮기기 위한 목록이다. 기존 수치를 확정 밸런스로 간주하지 않는다. 코드의 설명문과 실제 구현이 다르면 **구현 수치**를 본문에 기록하고 차이를 별도로 표시했다. 코드 기본값은 에셋·프리팹·씬에서 덮어쓸 수 있다.

## 1. 개편 방향과 현재 구현의 구분

현재 합의된 개편 방향:

- 폴리오미노 배치를 없애고 일정 개수의 장비만 장착한다.
- 트리거와 이펙트를 각각 무작위로 획득하고 조합해서 장착한다.
- 트리거가 전달하는 파워를 기반으로 이펙트를 발동하고 밸런싱한다.
- 무작위 능력치 보너스는 트리거 쪽에 붙인다.

현재 구현은 **6슬롯, 슬롯당 트리거 1개 + 효과 1개**다. 같은 종류의 서로 다른 획득 개체는 중복 장착할 수 있다. 트리거별 무작위 보너스의 임시 범위·획득 규칙은 [장비 결합 가이드](EquipmentTerminal.md)에 정리했다. 아래 도형 기반 보너스 표는 구형 보드 기록이며 새 회로에서는 적용하지 않는다.

현재 활성 장착 경로는 `EquipmentLoadout → 트리거 RuntimeActivation → 효과`다. 아래 두 계열은 기존 콘텐츠의 출처이며 새 회로는 기존 보드의 셀 배치를 사용하지 않는다.

| 계열 | 연결 구조 | 현황 |
|---|---|---|
| 기존 보드 | `Trigger → Board → Polyomino` | 트리거 12종(예제 2종 포함), 장비 효과 19종. 보드의 모든 효과에 같은 파워를 전달 |
| 메모리 보드 | `PlayerMemoryBinder → MemoryBoard → MemoryPieceAsset → TriggerEffectAsset` | 에셋형 효과 5종. 보드의 이벤트 수신 함수가 비어 있어 기존 트리거의 자동 대체 경로로 볼 수 없음 |

관련 코드: [Trigger](../Assets/PlayerSystem/Trigger.cs), [Board](../Assets/PlayerSystem/Tiling/Board.cs), [ITriggerEffect](../Assets/PlayerSystem/ITriggerEffect.cs), [PlayerMemoryBinder](../Assets/PlayerSystem/PlayerMemoryBinder.cs), [MemoryBoard](../Assets/PlayerSystem/MemoryBoard.cs).

## 2. 수치 표기와 공통 계산

| 기호 | 의미 | 현재 기본값·주의점 |
|---|---|---|
| `P` | 효과가 전달받은 `power` | 기존 보드는 입력값 그대로 전달 |
| `A` | 계산된 공격력 | `int(baseAtk × (1 + increaseAtk / 100) + addAtk)` |
| `D` | `additionalDuration` | 기본값 **1**. 효과마다 초 단위로 더하거나 배율로 곱하는 등 해석이 다름 |
| `B` | `bulletRate` | 기본값 1, 실수형. 효과에 따라 정수로 잘라 사용 |
| `R` | `skillRange` | 기본값 1 |
| `F` | `fireSpeed` | 기본값 1 |
| `C` | 트리거 내부 축적값 | 트리거별 정의 |

일반 피해의 `피해 계수 X%`는 우선 `A × X / 100`으로 계산한다. 이후 가산 피해, 치명타, 태그별 피해 증가와 대상의 받는 피해 증가·방어력이 반영된다. 고정 피해 태그는 계수를 그대로 정수 피해로 사용하며 이 계산 경로에서 피해 증가·치명타·방어력 계산을 건너뛴다. 따라서 표의 계수는 최종 체력 감소량과 다를 수 있다.

공격력 증가 `increaseAtk +20`은 기초 공격력에 대한 +20%이고, `addAtk +100`은 공격력 고정 가산이다. 피해 증가 `dmgUp +20`과도 구분한다.

출처: [EntityStat](../Assets/EntitySystem/StatSystem/EntityStat.cs), [피해 태그](../Assets/EntitySystem/Tag.cs). 지속 피해는 [DamageOverTime](../Assets/EntitySystem/BuffTypes/DamageOverTime.cs)의 타이머가 간격을 **초과할 때** 1회 발생하므로 정확한 틱 수는 프레임·만료 순서의 영향을 받는다.

## 3. 트리거 목록

이름은 코드 식별자를 유지했다. 별도의 한글 정식 명칭은 아직 정하지 않았다. 내부 쿨다운이 없다는 것은 트리거 자체의 제한이 없다는 뜻이며, 원래 공격·스킬의 쿨다운은 별도다.

| 트리거 / 소스 | 실제 발동 조건 | 전달 파워 `P` | 간격·축적·제한 |
|---|---|---|---|
| [NeonHeartbeat](../Assets/PlayerSystem/Triggers/NeonHeartbeat.cs) | 소유자 시간 갱신 시 주기적으로 발동 | `0.3` | 1초 주기. 초기 타이머 0으로 첫 갱신에도 발동 |
| [RapidFirePulse](../Assets/PlayerSystem/Triggers/RapidFirePulse.cs) | 일반 공격 실행 이벤트 | `B / 10` | 내부 쿨다운 없음. 기본 `B=1`이면 `P=0.1` |
| [ModuleBoostCall](../Assets/PlayerSystem/Triggers/ModuleBoostCall.cs) | 스킬 실행 이벤트 | `2` | 내부 쿨다운 없음 |
| [EnergyFlareExpansion](../Assets/PlayerSystem/Triggers/EnergyFlareExpansion.cs) | 필살기 실행 이벤트 | `이벤트의 energy / 10` | 내부 쿨다운 없음. 설명은 사용 에너지 기준 |
| [SlipstreamEvasion](../Assets/PlayerSystem/Triggers/SlipstreamEvasion.cs) | `DodgeEvent` 수신 | `1.2` | `Player`는 회피 실행 시 이 이벤트를 보냄. 설명의 ‘회피 성공’과 구분 |
| [ReverseHackReflection](../Assets/PlayerSystem/Triggers/ReverseHackReflection.cs) | 저스트 회피 이벤트 | `2 + 회피한 공격의 trueDmg / 200` | 상한·내부 쿨다운 없음 |
| [OverchargeSurge](../Assets/PlayerSystem/Triggers/OverchargeSurge.cs) | 저스트 회피로 축적 후 필살기 실행 시 발동 | `3 + C` | 회피마다 `C += trueDmg / 300`, 최대 6. 파워 3~9. 발동 후 축적값을 소모하지 않음 |
| [ChainLoadingProtocol](../Assets/PlayerSystem/Triggers/ChainLoadingProtocol.cs) | 일반 공격 실행마다 축적, 스킬 피해 이벤트에서 발동 | `0.5 + 0.2 × C` | 일반 공격마다 `C += 1`. 축적 상한·소모 없음. 내부 쿨다운 `2/(F+1)`초(플레이어), 그 외 1초 |
| [ImpactFeedbackLoop](../Assets/PlayerSystem/Triggers/ImpactFeedbackLoop.cs) | 타이머가 0 이하일 때 소유자의 피해 주기 이벤트 | `1` | 내부 쿨다운 `2/(F+1)`초(플레이어), 그 외 1초. 효과 실행 전에 쿨다운 적용 |
| [RebootProtocol](../Assets/PlayerSystem/Triggers/RebootProtocol.cs) | 누적 가한 `trueDmg ≥ 캐시 최대 HP × 5`이고, 피격 이벤트에서 현재 HP가 0 이하 | `9` | 현재 최대 HP의 10%로 HP 직접 설정, 누적 피해 0으로 초기화. 이후 재충전 가능 |

`trueDmg`는 이벤트에 들어 있는 피해 값이다. 대상 방어력 적용 후 실제 HP 감소량과 같다고 보장되지 않는다.

### 3.1 예제 트리거

| 예제 / 소스 | 발동 조건 | 파워 | 간격 |
|---|---|---|---|
| [SimpleTriggerExample](../Assets/PlayerSystem/Triggers/SimpleTriggerExample.cs) | 피해 주기 이벤트 | 1 | 내부 쿨다운 0.5초. 타이머를 효과 실행 전에 설정 |
| [SimpleTriggerExample2](../Assets/PlayerSystem/Triggers/SimpleTriggerExample2.cs) | 주기 발동 | 1 | 1초 주기, 첫 시간 갱신에도 발동 |

### 3.2 트리거 구현 확인 사항

- `ChainLoadingProtocol`: 쿨다운 검사 누락을 수정했다. 효과 실행 전에 쿨다운을 적용해 동기 재진입도 막는다.
- `ImpactFeedbackLoop`: 피해 이벤트·공격자 필터와 발사 속도 연동을 적용하고, 효과 실행 전에 쿨다운을 설정하도록 수정했다.
- `OverchargeSurge`, `ChainLoadingProtocol`: 발동 후 축적값 초기화가 없다. 소모형 충전으로 변경할지는 기획 결정이 필요하다.
- `RebootProtocol`: 회복 이벤트가 아닌 HP 직접 설정이다. 장비 보너스를 포함한 캐시 최대 HP의 10%, 최소 1 HP로 복구한다. 충전 후 치명 피해에서 HP 복구와 파워 9 전달을 격리 Unity 검사로 확인했다.
- 행동형 트리거는 소유자 본인의 이벤트만 처리한다. 보류된 저스트 회피는 다음 행동·명중 전에 전달해 같은 프레임 후속 공격에도 효과를 적용한다. 회피된 공격은 공격자 명중 트리거를 발생시키지 않는다.
- 5종 무기 구현에서 일반 공격·스킬 실행 이벤트를 연결하고 Sniper 강화탄을 스킬 피해로 분리했다. 상세 내용은 [무기 구현 및 검증](WeaponImplementation.md)을 참조한다.

## 4. 기존 장비 이펙트 19종

다음은 `Polyomino`를 상속한 콘텐츠 효과다. 셀 모양에서 얻는 무작위 보너스는 제외하고 효과 자체의 수치를 적었다. ‘재발동 시 교체’는 같은 효과 인스턴스 기준이며, 다른 장비 개체 사이의 중첩 규칙을 보장하지 않는다.

### 4.1 공격력·방어력·피해 강화

| 이펙트 / 소스 | 구현 수치 | 지속시간·중첩 | 설명과의 차이·주의점 |
|---|---|---|---|
| [CurrentAmplifier](../Assets/PlayerSystem/Polyominoes/CurrentAmplifier/CurrentAmplifier.cs) | 기초 공격력 증가율 `+(P+1) × 10%` | `5 + D`초, 재발동 시 교체 | 설명은 공격력 수치 증가처럼 적혀 있으나 실제 필드는 `increaseAtk` |
| [NormalAttackCharger](../Assets/PlayerSystem/Polyominoes/NormalAttackCharger/NormalAttackCharger.cs) | 일반 공격 피해 증가 `+(P+1) × 15%` | `5 + D`초, 재발동 시 교체 | 일반 공격 태그에 적용 |
| [SkillCharger](../Assets/PlayerSystem/Polyominoes/SkillCharger/SkillCharger.cs) | 스킬 피해 증가 `+(P+1) × 15%` | `5 + D`초, 재발동 시 교체 | 스킬 태그에 적용 |
| [RiskAnalyzer](../Assets/PlayerSystem/Polyominoes/RiskAnalyzer/RiskAnalyzer.cs) | 기초 방어력 증가율 `+(P+1) × 15%` | `10 + D`초, 재발동 시 교체 | 설명에 맞춰 기본 시간 10초와 표시 이름을 수정 |
| [OverclockConverter](../Assets/PlayerSystem/Polyominoes/OverclockConverter/OverclockConverter.cs) | 모든 피해 증가 `+P × 15%`, 기초 방어력 증가율 `-20%` | `5 + D`초, 재발동 시 교체 | 현재 HP 5% 소모를 구현. HP를 1 미만으로 내리지 않음 |
| [OverclockUnlocker](../Assets/PlayerSystem/Polyominoes/OverclockUnlocker/OverclockUnlocker.cs) | 현재 HP 20% 소모, 기초 공격력 증가율 `+P × 20%` | `20 + D`초, 각 발동 독립 중첩, 상한 없음 | 현재 HP가 최대 HP의 10% 미만이면 발동 불가. HP는 `int(현재 HP × 0.8)`로 설정 |
| [AssaultStackBuffer](../Assets/PlayerSystem/Polyominoes/AssaultStackBuffer/AssaultStackBuffer.cs) | 공격력 고정 가산 +100씩 축적, 정상 경로 최대 +1000 | `3 × D`초. 피해 주기 이벤트로 지속시간 갱신 | **파워 미사용**. 새 회로에서는 장비 개체별 버프. 상한 초과 시 1000으로 제한하도록 수정 |
| [PowerInjectionCore](../Assets/PlayerSystem/Polyominoes/PowerInjectionCore/PowerInjectionCore.cs) | 공격력 고정 가산 `250 + 250P`, 최대 +2500 | 각 기록 `20 × D`초, 남아 있는 기록 중 가장 큰 값만 적용 | 새 회로에서는 장비 개체별 버프 |
| [NextShotAmplifier](../Assets/PlayerSystem/Polyominoes/NextShotAmplifier/NextShotAmplifier.cs) | 스택당 `max(0, 10 + 10P)%`, 합계 최대 150% | 높은 파워 3개만 보관. 시간 제한 없음. 다음 일반 공격 피해 이벤트 1회에 전부 소모 | 이벤트 피해에 직접 `1 + 합계/100` 배율 적용. 장비 개체별 버프에서 대상별로 스택 관리 |
| [NextSkillAmplifier](../Assets/PlayerSystem/Polyominoes/NestSkillAmplifier/NextSkillAmplifier.cs) | 스택당 `max(0, 10 + 10P)%`, 합계 최대 150% | 높은 파워 3개만 보관. 시간 제한 없음. 다음 스킬 피해 이벤트 1회에 전부 소모 | 폴더명은 `NestSkillAmplifier`. 다단히트 전체가 아닌 이벤트 1회 기준 |
| [UltimateCapacitor](../Assets/PlayerSystem/Polyominoes/UltimateCapacitor/UltimateCapacitor.cs) | `P≥1`일 때 내부 축적 `Q = min(20, Q+(P-1) × 1.3)`. 필살기 실행 시 필살기 피해 `+Q × 20%` | 강화 3초, 종료 시 축적 0 | 파워 하한·상한 처리 및 종료 시 버프·리스너 제거를 수정 |

### 4.2 공격·지속 피해·상태 효과

| 이펙트 / 소스 | 구현 수치 | 횟수·시간·범위 | 파워 반영·주의점 |
|---|---|---|---|
| [HeatEnergyRelease](../Assets/PlayerSystem/Polyominoes/HeatEnergyRelease/HeatEnergyRelease.cs) | 유도탄 1개당 `10 + 10P%` 계수, 열·전투 모듈 피해 | `5 + int(B)`개, 기본 6개 | 파워는 피해 계수에 반영. 개수는 총알 개수 스탯 기준 |
| [LaserBlaster](../Assets/PlayerSystem/Polyominoes/LaserBlaster/LaserBlaster.cs) | `P ≤ 1`: `50P%`, `P > 1`: `150P - 100%` 계수. 추가로 모든 피해 증가 `+20(B+R)%` | 가장 가까운 대상 탐색 기본 반경 15. 대상별 1회 관통 피해 | 물리·전투 모듈 태그. 추가 피해 증가는 해당 공격용 스탯 복사본에 적용 |
| [ElectricFieldGenerator](../Assets/PlayerSystem/Polyominoes/ElectricFieldGenerator/ElectricFieldGenerator.cs) | 최초 타격 `(P+1) × 50%` 계수. `P>2`이면 Shock 추가 | 전기장 1초, 대상별 최초 타격 1회. 크기 배율 `1 + (R-1)/3` | 전기·전투 모듈 피해. 마비 관련 수치는 아래 보조 효과 참조 |
| [ExecutionFollowupShot](../Assets/PlayerSystem/Polyominoes/ExecutionFollowupShot/ExecutionFollowupShot.cs) | 구체마다 고정 피해 500 | 일반 공격 피해 이벤트 후 0.5초 지연 공격. 의도된 보관 최대 20개 | **파워 미사용**. 보관 리스트 등록을 추가해 최대 20개 제한을 적용 |
| [FireKnife](../Assets/PlayerSystem/Polyominoes/FireKnife/FireKnife.cs) | 일반 공격·스킬·필살기가 실제 HP를 감소시킨 뒤 생존 대상에 화상 부여(`DamageDealtEvent`). 틱당 `(P+1) × 20%` 계수 | 부여 가능 상태 `10+D`초, 대상별 내부 쿨다운 0.5초. 화상 `8+D`초, 정확히 2초 누적마다 피해 | 열·지속 피해·치명타 불가. 재발동 시 이후 화상에 쓸 파워 교체. 기존 화상은 각 생성 시 피해 사용. 화상 틱은 새 화상을 부여하지 않음 |
| [FeatureExtractor](../Assets/PlayerSystem/Polyominoes/FeatureExtractor/FeatureExtractor.cs) | 주변 대상에 등록된 지속 피해를 각각 즉시 1회 추가 실행 | 생성 후 0.3초 지연. 범위는 실행체의 원형 콜라이더 기준 | `P ≥ 1`일 때만 발동. 그 이상 파워를 높여도 추가 틱 수는 늘지 않음 |

보조 실행체의 수치와 차이:

- [EnergyBullet](../Assets/PlayerSystem/Polyominoes/HeatEnergyRelease/EnergyBullet.cs): 이동 속도 6, 회전 속도 최대 1000도/초, 대상 재탐색 0.2초, 수명 6초. 충돌 또는 수명 만료 시 폭발 생성. [EnergyBulletHit](../Assets/PlayerSystem/Polyominoes/HeatEnergyRelease/EnergyBulletHit.cs)는 약 0.1초 후 콜라이더를 끄고 1초 후 제거하며 대상별 1회 피해를 준다.
- [Laser](../Assets/PlayerSystem/Polyominoes/LaserBlaster/Laser.cs): 0.2초 뒤 실행, 선 표시 길이 30, 판정 유지 0.5초, 소멸 연출 0.1초. 실제 충돌 범위는 프리팹 콜라이더 설정도 확인해야 한다.
- [ElectricField](../Assets/PlayerSystem/Polyominoes/ElectricFieldGenerator/ElectricField.cs) → [Shock](../Assets/PlayerSystem/Polyominoes/Shock.cs): 지속 피해 계수 `30P - 50%`, 1.5초 간격. 받는 지속 피해·전투 모듈 피해 증가는 **고정 20%**, 같은 대상에 중첩되지 않는다. 설명의 `30P-50%` 마비 수치와 다르다.
- `Shock`의 수명 감소 누락을 수정했다. 수명은 `10 × D`초이며 만료 시 지속 피해와 마비 피해 증가를 회수한다.
- `ExecutionFollowupShot`의 [Shot](../Assets/PlayerSystem/Polyominoes/ExecutionFollowupShot/Shot.cs)은 물리·전투 모듈·고정 피해 태그를 사용한다. 설명의 번개 연출과 전기 피해 태그를 혼동하지 않는다. 실행 전 중복 예약 방지를 추가했다.
- `FeatureExtractor`의 [실행체](../Assets/PlayerSystem/Polyominoes/FeatureExtractor/FeatureExtractionExecutor.cs)는 한 번에 콜라이더 최대 128개를 검사하고 같은 대상은 중복 처리하지 않는다. 범위 내 `DamageOverTime` 리스너마다 `giveDamage()`를 호출한다.

### 4.3 회복

| 이펙트 / 소스 | 구현 수치 | 축적·제한 | 설명과의 차이 |
|---|---|---|---|
| [SimpleCooler](../Assets/PlayerSystem/Polyominoes/SimpleCooler/SimpleCooler.cs) | HP `int(50P)` 회복 | 효과 자체의 내부 쿨다운 없음 | 최대 HP 초과 회복은 공통 회복 처리에서 제한 |
| [RecoveryManager](../Assets/PlayerSystem/Polyominoes/RecoveryManager/RecoveryManager.cs) | HP `int(1000P × E²)` 회복 | 활성 오브젝트의 `Update`마다 에너지 `E`가 초당 1 축적, 최대 2. 최대 축적 시 `4000P` 회복 | 발동 후 에너지 0으로 초기화, 1초 뒤 재충전. 새 회로에서는 UI 그래픽만 숨기고 실행체를 활성 상태로 유지해 충전이 계속됨 |

## 5. 에셋형 이펙트 5종

아래 기본값은 클래스 선언 기준이다. 실제 저장된 에셋 값은 다음 절에 별도로 기록한다.

| 이펙트 / 소스 | 코드 기본값 | 실행 방식·구현 제한 |
|---|---|---|
| [SpawnProjectileEffectAsset](../Assets/PlayerSystem/Effects/SpawnProjectileEffectAsset.cs) | 1발, 퍼짐각 5도, 생성 오프셋 `(0.5, 0.1)`, 방향 덮어쓰기 0, 크기 가산 0 | 투사체에 파워와 크기를 전달. 방향 0이면 좌우 방향 사용. 1발이어도 시작각 `-퍼짐각/2`가 적용됨 |
| [ApplyStatBuffEffectAsset](../Assets/PlayerSystem/Effects/ApplyStatBuffEffectAsset.cs) | 증가율 `15P%`, 지속시간 3초 | 메모리 컨텍스트가 있으면 해당 발동의 투사체 피해 보너스로 누적. 없으면 임시 기초 공격력 증가율 버프 생성. 경로별 의미가 다름 |
| [GainResourceEffectAsset](../Assets/PlayerSystem/Effects/GainResourceEffectAsset.cs) | Energy 자원 `5 × max(0,P)` | 현재 발동 보드 또는 활성 보드의 자원 풀에 추가. 플레이어 필살기 에너지와 같은 저장소라고 단정하지 않음 |
| [ProjectileKnockbackEffectAsset](../Assets/PlayerSystem/Effects/ProjectileKnockbackEffectAsset.cs) | 힘 5, 파워 배율 사용 | 컨텍스트에 `5 × max(0,P)` 누적. 컨텍스트가 없으면 효과 없음 |
| [ProjectileRecoilEffectAsset](../Assets/PlayerSystem/Effects/ProjectileRecoilEffectAsset.cs) | 힘 3, 파워 배율 미사용 | 컨텍스트에 기본 3 누적. 배율 옵션을 켜면 `3 × max(0,P)`. 컨텍스트가 없으면 효과 없음 |

**일반 Projectile 적용 보완:** [Projectile](../Assets/PlayerSystem/Weapons/Projectile.cs)의 충돌 피해 계수에 파워를 곱하고 피해 보너스를 반영하도록 수정했다. 넉백은 대상 Rigidbody2D에, 반동은 소유자 Rigidbody2D에 적용한다. 코드 기본값은 속도 12, 수명 3초, 피해 계수 10이다. 이 수정으로 과거 무시되던 값이 실제 성능에 영향을 주므로 기존 에셋의 큰 BasePower 수치는 재조정이 필요하다.

### 5.1 메모리 보드의 파워 계산

```text
효과 입력 파워 = max(0, 발동 기본 파워 × 전역 파워 배율)
               × 피스 BasePower
               × 획득/배치 PowerMultiplier
               × (1 + 겹치는 강화 구역 BonusPercent 합계 / 100)
```

[MemoryPieceAsset](../Assets/PlayerSystem/MemoryPieceAsset.cs)의 기본값은 BasePower 1, 쿨다운 0초, 자원 None, 비용 0, 코어 아님, 허용 트리거 All이다. 강화 구역의 코드 기본값은 +25%다. 보드는 능력치 강화 효과를 먼저 실행하고 나머지 효과를 실행한다. 코어는 자원 소모를 건너뛰지만 쿨다운 검사는 적용된다.

[ActionTriggerType](../Assets/PlayerSystem/ActionTriggerType.cs)의 분류는 Jump, DropDown, BasicAttack, HeavyAttack, Interact, Dodge, Dash, Hit, Ultimate이다. 이 분류 자체는 획득 가능한 트리거 아이템이나 조건·파워 수식을 구현한 것이 아니다. `MemoryBoard.recieveEvent()`는 비어 있다.

### 5.2 저장된 에셋·프리팹 수치

| 에셋 / 소스 | 저장된 값 | 연결·해석 |
|---|---|---|
| [SampleSpawnProjectileEffect](../Assets/Samples/ScriptableObjects/SampleSpawnProjectileEffect.asset) | 3발, 퍼짐각 8도, 오프셋 `(0.6, 0.1)`, 크기 가산 0 | SampleProjectile 연결 |
| [SpawnProjectileEffect](../Assets/Samples/ScriptableObjects/SpawnProjectileEffect.asset) | 1발, 퍼짐각 5도, 오프셋 `(0.5, 0.1)`, 크기 가산 3 | SampleProjectile 연결 |
| [StatBuffEffect](../Assets/StatBuffEffect.asset) | 증가율 계수 100, 지속시간 2초 | 효과 값은 `100P%`. 파일명이나 피스 표시 이름보다 저장값 우선 |
| [15%Dmgbuff](../Assets/PlayerSystem/Weapons/15%Dmgbuff.asset) | 증가율 계수 15, 지속시간 3초 | 효과 값은 `15P%` |
| [SampleMemoryPiece](../Assets/Samples/ScriptableObjects/SampleMemoryPiece.asset) | 표시 이름 `3 Bullets`, BasePower 1, 쿨다운 1초, Energy 비용 0, 3셀 | SampleSpawnProjectileEffect 연결, 허용 트리거 비트값 -1 |
| [Dmg2](../Assets/Samples/ScriptableObjects/Dmg2.asset) | 표시 이름 `Dmg*2`, BasePower 10, 쿨다운 0, 비용 0, 5셀 | StatBuffEffect 연결. 이름만으로 피해 2배를 보장하지 않음 |
| [Sizeup](../Assets/Samples/ScriptableObjects/Sizeup.asset) | 표시 이름 `Larger`, BasePower 30, 쿨다운 0, 비용 0, 1셀 | 크기 가산 3인 SpawnProjectileEffect 연결. BasePower 30은 크기 30을 뜻하지 않음 |
| [SampleReinforcementZone](../Assets/Samples/ScriptableObjects/SampleReinforcementZone.asset) | 강화 +30%, 4셀 | 코드 기본값 +25%를 덮어씀 |
| [SampleProjectile](../Assets/Samples/Prefabs/SampleProjectile.prefab) | 속도 14, 수명 3초, 피해 계수 100, 충돌 마스크 256, 모든 충돌 시 파괴 옵션 꺼짐 | 자기 외 엔티티 타격 시에는 1회 피해 후 파괴 |
| [LaserBlaster 프리팹](../Assets/PlayerSystem/Polyominoes/LaserBlaster/LaserBlaster.prefab) | 탐색 반경 15, 적 레이어 마스크 512 | 실제 피해 판정은 연결된 Laser 프리팹의 콜라이더에도 의존 |

자원 획득·넉백·반동 에셋형 클래스는 존재하지만 해당 스크립트를 참조하는 `.asset`·`.prefab` 설정은 이번 검색에서 발견하지 못했다. 저장된 샘플이 있다는 것과 실제 플레이 씬에서 사용 중이라는 것은 구분한다.

## 6. 추가 예제·프로토타입 효과

기존 장비 19종 및 에셋형 5종과 별도로 존재하는 예제다.

| 효과 / 소스 | 현재 수치 | 구현 상태 |
|---|---|---|
| [SimpleEffectExample1](../Assets/PlayerSystem/Effects/SimpleEffectExample1.cs) | 기초 공격력 증가율 +20%, 3초, 파워 무시 | 재발동 시 시간 갱신 |
| [SimpleEffectExample2](../Assets/PlayerSystem/Effects/SimpleEffectExample2.cs) | 화염구 피해 계수 `100P%` | 위치·진행 방향·충돌 실행 연결 미완성. `fireball=60`도 수용하도록 태그 배열 크기를 수정 |
| [EffectPowerExample](../Assets/PlayerSystem/Effects/EffectPowerExample.cs) | 하위 효과마다 입력 파워의 70% 전달 | 피해 효과가 아닌 파워 전달 예제 |
| [AtkUpByPower](../Assets/PlayerSystem/Effects/AtkUpEffect/AtkUpByPower.cs) | 기초 공격력 증가율 `+10P%`, 입력 파워 중 큰 값 보관 | 10초 만료 시 버프 제거. 효과 인스턴스별 상태로 분리하고 중복 등록을 방지 |
| [SimpleEnergyGunEffect](../Assets/PlayerSystem/Effects/EnergyGun/SimpleEnergyGunEffect.cs) | 60도 간격으로 투사체 6개 생성 | 파워 미사용. 투사체에 피해 이벤트를 설정하지 않는 미완성 예제 |

## 7. 기존 무작위 능력치 보너스

출처: [Polyomino](../Assets/PlayerSystem/Tiling/Polyomino.cs). 현재는 폴리오미노 `Start()`에서 셀 개수에 따라 추첨하고 보드 장착 시 적용, 해제 시 회수한다. 트리거에 보너스를 붙이는 개편은 아직 구현하지 않았다.

| 기존 셀 개수 | 추첨 횟수 |
|---|---:|
| 1~2 | 0 |
| 3~4 | 1 |
| 5~6 | 2 |
| 7~8 | 3 |
| 9~10 | 4 |
| 11~12 | 5 |
| 그 외 | 0 |

매 추첨마다 아래 9개 항목 중 하나를 동일 확률로 선택한다. 같은 항목의 중복 당첨이 가능하며 수치를 합산한다. +1이 최종 성능의 +1%라는 의미는 아니며 각 스탯의 소비 수식에 따른다.

| 보너스 | 1회 당첨 수치 | 적용 필드 |
|---|---:|---|
| 발사 속도 | +1 | `fireSpeed` |
| 총알 개수 | +1 | `bulletRate` |
| 총알 속도 | +1 | `bulletSpeed` |
| 추가 지속시간 | +1 | `additionalDuration` |
| 스킬 범위 | +1 | `skillRange` |
| 쿨다운 감소 | +1 | `skillCooldownDecrease` |
| 최대 체력 | +200 | `maxHp` |
| 기초 공격력 | +100 | `baseAtk` |
| 기초 방어력 | +100 | `baseDef` |

새 기획에서는 이 후보·수치를 이전 기준으로 검토할 수 있다. 셀 수에 의존하던 추첨 횟수의 대체 규칙은 새로 정해야 한다. 획득할 때 추첨해 트리거 개체에 저장하고 재장착·효과 교체 시 유지하는 방식은 권장 구현안이며, 위 현재 구현과 구분한다.

## 8. 밸런스 수정 전에 정할 사항

1. **파워 반응:** 파워 무시 효과, 기본값이 붙는 효과, 파워 문턱 효과를 유지할지 정한다. 낮은 파워를 자주 전달하는 트리거와 큰 파워를 드물게 전달하는 트리거의 차이에 직접 영향을 준다.
2. **시간 단위:** `5+D`와 `20×D`가 혼재한다. 추가 지속시간을 초 단위 가산으로 쓸지 배율로 쓸지 통일한다.
3. **효과별 상태:** 같은 부품을 여러 개 획득·장착했을 때 스택·충전·쿨다운을 개별 관리할지 공유할지 정한다. 현재 일부 효과는 정적 버프를 공유한다.
4. **연쇄 발동:** 장비 피해가 다른 트리거를 발동할 수 있는지, 발동 원인·태그·연쇄 횟수를 어떻게 제한할지 정한다.
5. **보너스 추첨:** 트리거에 붙는 능력치 종류·개수·중복·수치 범위·확률과 장착 시 적용 규칙을 정한다.
6. **구현 차이 처리:** 이 문서의 설명 불일치·누락 사항을 수정할지, 현 동작을 새 기획으로 채택할지 결정한 뒤 수치를 조정한다.

관련 기존 문서: [씬 셋업](SceneSetup.md), [샘플 프리팹](SamplePrefabs.md), [코드 리뷰](CodeReview.md). 이들 문서는 이전 구조의 설명을 포함하므로 현재 수치 확인 시 이 문서의 소스 링크와 함께 확인한다.
