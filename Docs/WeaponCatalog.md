# 무기 종류·수치 및 구현 현황

5종 무기의 목표 기획은 [WeaponDesign.md](WeaponDesign.md), 최신 구현과 임시 수치·검증 결과는 [WeaponImplementation.md](WeaponImplementation.md)를 참조한다. **아래 내용은 개편 전 구현 조사 기록**으로 보존한다. 아래의 미구현·문제 사항 중 수정 완료된 항목은 최신 구현 문서에서 구분한다.

작성 기준: 2026-10-23. 코드, 무기·투사체 프리팹, 플레이어·씬 참조를 정적으로 확인했다. Unity에서 실제 조작·전투를 실행한 결과는 아니다.

## 1. 확인 결과

`Weapon`을 상속한 무기는 **Sniper와 GunAndKnife 두 종류**다. Sniper는 일반 공격·스킬·필살기·조준·UI까지 주요 흐름이 구현돼 있고, GunAndKnife는 일반 공격과 표식 스킬의 일부가 구현된 상태다. 따라서 ‘주요 기능이 갖춰진 무기 1개, 제작 중인 무기 1개’라는 구분은 코드 구조와 일치한다. 다만 Sniper도 이벤트 연동 등 보완할 부분이 있어 무결한 완성품으로 판정하지 않는다.

| 항목 | Sniper | GunAndKnife |
|---|---|---|
| 일반 공격 | 조준 후 발사, 차지·치명타 보정·피해 처리 구현 | 다발 발사·피해 처리 구현. 각도 계산 확인 필요 |
| 스킬 | 스택 소모, 강제 회피, 다음 탄 강화·폭발 구현 | 주변 대상에 칼 투척·표식 부여 구현. 스택 소모 누락 |
| 필살기 | 에너지 소모, 슬로모션 조준, 다발 투사체·반복 타격·마무리 폭발 구현 | 함수 본문과 전용 실행체가 비어 있음 |
| 조준 | 조준선·대상 보정·슬로모션 구현 | 조준선·대상 보정 구현. 슬로모션 플래그만 변경 |
| UI | 스킬 스택·쿨다운·에너지 표시 연결 | 무기 전용 UI 처리 없음 |
| 저장된 설정 | 쿨다운·에너지·탄환·UI 참조 설정 | 쿨다운·에너지 관련 필드 다수가 0 |
| 플레이어 연결 | 플레이용 플레이어 프리팹에 지정 | 씬·다른 프리팹에서 무기 프리팹을 참조하는 기록을 찾지 못함 |

공통 기반: [Weapon](../Assets/PlayerSystem/Weapons/Weapon.cs), [AimSupport](../Assets/PlayerSystem/AimSupport.cs), [Player](../Assets/PlayerSystem/Player.cs).

### 1.1 현재 연결 경로

- [Play 씬](../Assets/Scenes/Play.unity)과 [SampleScene](../Assets/Scenes/SampleScene.unity)은 [Assets/PlayerSystem/SamplePlayer.prefab](../Assets/PlayerSystem/SamplePlayer.prefab)을 참조한다.
- 해당 플레이어의 `weapon` 필드는 [Sniper.prefab](../Assets/PlayerSystem/Weapons/Sniper/Sniper.prefab)이다. `Player.Start()`에서 무기를 생성하고 플레이어를 등록한다.
- [gun_blue.prefab](../Assets/PlayerSystem/Weapons/GunAndKnife/gun_blue.prefab)은 존재하지만 이번 `.unity`·`.prefab` GUID 검색에서는 외부 참조가 발견되지 않았다. 런타임의 별도 로딩 여부까지 확정하는 결과는 아니다.
- `Assets/Samples/Prefabs/SamplePlayer.prefab`은 위 플레이용 플레이어와 다른 샘플 파일이다. 이름이 같으므로 혼동하지 않는다.

## 2. 공통 수치 표기

| 기호 | 스탯 | 기본값 |
|---|---|---:|
| `B` | 총알 개수 `bulletRate` | 1 |
| `V` | 총알 속도 `bulletSpeed` | 1 |
| `F` | 발사 속도 `fireSpeed` | 1 |
| `R` | 스킬 범위 `skillRange` | 1 |
| `K` | 쿨다운 감소 `skillCooldownDecrease` | 1 |
| `E` | 에너지 충전 배율 `energyRecharge` | 1 |
| `t` | 조준 누적 시간 `aimDuration` | `Time.deltaTime`으로 누적 |

피해 계수 `X%`는 공격력에 대한 비율이다. 피해 증가·치명타·대상 방어력에 따라 실제 피해가 달라진다. 계산은 [트리거·이펙트 현황](TriggerAndEffectCatalog.md#2-수치-표기와-공통-계산)을 참조한다.

시간은 게임 내 타이머 기준이다. 조준 중 시간 배율이 바뀌므로 현실 시간과 구분한다. [TimeScaler](../Assets/TimeScaler.cs)의 시간 변경은 절대값 설정이 아니라 현재 `Time.timeScale`에 배율을 곱하는 방식이다.

## 3. Sniper

소스: [Sniper.cs](../Assets/PlayerSystem/Weapons/Sniper/Sniper.cs), [SniperAim.cs](../Assets/PlayerSystem/Weapons/Sniper/SniperAim.cs). 설정값은 별도 표시가 없으면 **Sniper.prefab에 저장된 값**이다.

### 3.1 조작·자원·쿨다운

| 항목 | 현재 값·동작 |
|---|---|
| 일반 공격 | 마우스 왼쪽 버튼을 누르며 조준, 놓을 때 발사. 별도 일반 공격 쿨다운 없음 |
| 조준 범위 | 조준 중 `59.9 × exp(-7t)`. 해제 시 공통 기본값 10으로 초기화 |
| 조준 보정 | 마우스 월드 좌표 주변 반경 1 안의 가장 가까운 `EnemyLockOnPoint` 선택 |
| 슬로모션 | 누적 조준 시간 0.3초 이상에서 시간 배율 ×1/3. 발사 시 ×3 |
| 스킬 키 | E. 조준 중·필살기 중에는 키 입력으로 사용 불가 |
| 스킬 보관 | 최대 4회. 초기 보유 0, 첫 갱신에서 충전 진행 |
| 스킬 충전 | 기본 쿨다운 5초, 실제 `5 × 4 / (K+4)`초. 기본 `K=1`이면 4초에 1회 |
| 필살기 키 | Q. 조준 중·필살기 중에는 키 입력으로 사용 불가 |
| 필살기 비용 | 에너지 60 |
| 에너지 상한 | 100, 초기 0 |
| 에너지 획득 | 피해 주기 이벤트의 `energeRecharge × E`만큼 증가, 상한 100 |
| UI | 보유 스킬 수, 스킬 충전 진행도, 현재 에너지/100, 에너지 진행도 |

### 3.2 일반 공격

일반 탄환 **1발**을 생성한다. 총알 개수·총알 속도·발사 속도 스탯은 아래 피해 계수에 반영된다.

```text
기본 계수 C = 100 × (B+1) × (V+1) × (F+1) / 8
차지 계수 N = C × min(0.5+t, 1) / 2
t ≥ 0.3초이면 N에 5를 곱하고 치명타 확률에 50%p 가산
최종 피해 계수 = N%
```

| 항목 | 값 |
|---|---|
| 기본 스탯, 조준 0초의 계수 | 25% |
| 기본 스탯, 조준 0.3초의 계수 | 200%, 치명타 확률 +50%p |
| 기본 스탯, 조준 0.5초 이상의 계수 | 250%, 치명타 확률 +50%p |
| 발사각 오차 | 조준 범위 × 무작위 `[-0.3, 0.3]`도 |
| 피해 태그 | 전기·일반 공격 |
| 명중 에너지 값 | `3 × min(t,1)` |
| 투사체 속도·최대 수명 | 프리팹 값 50, 최대 10초 |
| 명중 | 플레이어를 무시하고 다른 콜라이더에 닿으면 정지·잔상 정리. 엔티티이면 피해 전달 |

소스: [NormalBullet](../Assets/PlayerSystem/Weapons/Sniper/NormalBullet.cs), [NormalBullet.prefab](../Assets/PlayerSystem/Weapons/Sniper/NormalBullet.prefab).

### 3.3 스킬: 회피 후 다음 탄 강화

E 입력 처리에서 스택 1개를 소모하고 `skill()`을 호출한다. `skill()`은 다음 사격을 강화탄으로 전환하고 `Player.TryDodge(true)`로 회피 타이머 검사를 우회한다. 강화탄 대기 시간 제한은 없다.

| 항목 | 값 |
|---|---|
| 강화탄 피해 계수 | 위 일반 공격 계수 `N`에 대해 `(0.8N + 100)%` |
| 기본 스탯, 조준 0초 | 120% |
| 기본 스탯, 조준 0.3초 | 260%, 치명타 확률 +50%p |
| 기본 스탯, 조준 0.5초 이상 | 300%, 치명타 확률 +50%p |
| 에너지 값 | `5 × min(t,1)` |
| 투사체 속도·최대 수명 | 프리팹 값 50, 최대 10초 |
| 명중 효과 | 충돌 지점에 범위 타격 생성, 크기 배율 `sqrt(R)` |
| 폭발 판정·연출 | 약 0.1초 후 판정 비활성화, 오브젝트 1초 후 제거 |
| 실제 피해 태그 | **전기·일반 공격**. 스킬 피해 태그가 아님 |

소스: [SkillBullet](../Assets/PlayerSystem/Weapons/Sniper/SkillBullet.cs), [SkillHit](../Assets/PlayerSystem/Weapons/Sniper/SkillHit.cs). 폭발 중복 방지는 엔티티가 아닌 콜라이더 단위이므로 여러 콜라이더를 가진 대상은 중복 피해 가능성을 확인해야 한다.

### 3.4 필살기

에너지 60을 소모해 시간 배율을 ×1/3으로 변경하고 필살기 조준 상태에 들어간다. 마우스 왼쪽 버튼을 놓으면 ×3으로 복원하고 투사체를 발사한다. 제한 시간·취소 입력은 별도로 구현돼 있지 않다.

| 항목 | 값 |
|---|---|
| 발사 개수 | `int(B)+2`, 기본 3발 |
| 투사체 속도 | 40 |
| 도착 지점 | 조준 거리 기준 ±약 3.33% 무작위 거리 보정 |
| 도착 시간 | 발사 위치부터 도착점까지 거리 / 40 |
| 피해 태그 | 전기·필살기 |
| 반복 타격 | 1회당 30% 계수, 약 0.2초 주기의 10회 구간 |
| 반복 판정 | 주기 내 처음 약 0.1초 활성, 다음 약 0.1초 비활성 |
| 마무리 타격 | 700% 계수 |
| 한 영역의 명목 합계 | `30% × 10 + 700% = 1000%` 계수. 전부 명중한 경우이며 중복 판정·프레임·대상 이동에 따라 달라짐 |
| 영역 크기 | 초기 스케일 0.3에서 `1.5 × sqrt(R)`까지 3초 동안 확대하도록 설정. 반복 타격 종료 시 마무리로 전환 |
| 마무리 판정 | 0.5초 후 비활성, 1초 후 오브젝트 제거 |
| 이벤트의 에너지 값 | **`UltimateExecuteEvent(player, 50)` 고정**, 실제 소모 60과 다름 |
| 타격 에너지 값 | 반복 타격 2, 마무리 타격 3 |

분산각도는 발사 수 `n=int(B)+2`, `q=(n이 짝수이면 2n+1, 홀수이면 2n-1)`에 대해 아래 코드식을 사용한다.

```text
각도 간격 h = atan(sqrt(R) × n) / π × 60 / (q-1) × sqrt(n/3)
i번째 투사체 각도 = 조준각 + (2i-n+1) × h
```

소스: [UltimateBullet](../Assets/PlayerSystem/Weapons/Sniper/UltimateBullet.cs), [UltimateHit](../Assets/PlayerSystem/Weapons/Sniper/UltimateHit.cs), [UltimateFinished](../Assets/PlayerSystem/Weapons/Sniper/UltimateFinished.cs).

### 3.5 Sniper 보완·검증 사항

- **트리거 연결:** 일반 사격 시 `BasicAttackExecuteEvent`, 스킬 사용 시 `SkillExecuteEvent`를 보내지 않는다. 해당 이벤트에 의존하는 장비 트리거는 연결이 필요하다.
- **스킬 분류:** 강화탄은 일반 공격 태그이므로 스킬 피해 증가와 스킬 명중 트리거의 대상이 되지 않는다. 의도인지 결정해야 한다.
- **필살기 에너지 불일치:** 실제 비용은 60, 이벤트 값은 50. 현재 `EnergyFlareExpansion`은 이 필살기에서 파워 5를 받는다.
- **다단히트 중복:** 스킬 폭발·필살기 판정은 콜라이더 단위로 중복 방지한다. 엔티티당 1회인지 확인이 필요하다.
- **에너지 이벤트 재사용:** 같은 피해 이벤트를 다수 대상·틱에서 재사용한다. 공통 `DamageGiveEvent.trigger()`가 에너지 값을 0으로 바꾸므로 표의 값이 매 명중마다 반복 충전된다고 볼 수 없다.
- **시간과 종료 처리:** 조준·필살기 도중 무기 해제·파괴 시 시간 배율 복원 처리가 없다. 무기 교체를 도입할 때 정리해야 한다.
- **직접 호출 계약:** 스택·에너지 검사는 입력을 처리하는 `Update()`에 있다. `skill()`·`ultimate()`를 외부에서 직접 호출하면 비용 검사를 우회한다.
- **UI 초기값:** `skillCooltimeNow`가 0일 때 진행도 계산의 0 나눗셈 가능성이 있다. 실제 초기 화면을 확인해야 한다.

## 4. GunAndKnife

소스: [GunAndKnife.cs](../Assets/PlayerSystem/Weapons/GunAndKnife/GunAndKnife.cs), [GunAndKnifeAim.cs](../Assets/PlayerSystem/Weapons/GunAndKnife/GunAndKnifeAim.cs), [gun_blue.prefab](../Assets/PlayerSystem/Weapons/GunAndKnife/gun_blue.prefab).

### 4.1 저장된 설정

| 항목 | 프리팹 값 | 실제 사용 상태 |
|---|---:|---|
| 일반 공격 쿨다운 | 0.2초 | 발사 후 적용 |
| 필살기 중 공격 쿨다운 | 0초 | 미사용 |
| 스킬 충전 쿨다운 | 0초 | 타이머가 음수가 되면 스택 +1, 결과적으로 빠르게 충전 |
| 최대 스킬 스택 | 2 | 충전 상한은 적용, 사용 시 소모 없음 |
| 필살기 지속시간 | 0초 | 상태 전환 구현 없음 |
| 필살기 요구 에너지 | 0 | 조건 검사만 있고 본문 없음 |
| 최대 에너지 | 0 | 충전 상한 처리에 사용하지 않음 |
| 추가 총알 개수 | 4 | 일반 공격 발사 수에 가산 |
| 조준 보정 반경 | 1 | 공통 조준 보정 사용 |
| 슬로모션 문턱 필드 | 0.3초 | 플래그만 변경하고 시간 배율은 바꾸지 않음 |

### 4.2 일반 공격

| 항목 | 값 |
|---|---|
| 조작 | 마우스 왼쪽 버튼을 누르며 조준, 놓을 때 쿨다운이 끝났으면 발사 |
| 발사 개수 | `int(B)+4`, 기본 5발 |
| 1발 피해 계수 | `200 / (int(B)+3)%`, 기본 50% |
| 기본 스탯 명목 합계 | 5발이 모두 명중하면 250% 계수 |
| 피해 태그 | 물리·일반 공격 |
| 에너지 값 | 1발의 이벤트에 10 지정 |
| 투사체 속도·최대 수명 | 프리팹 값 20, 최대 0.4초 |
| 조준 범위 | `5 + 34.9 × exp(-7t)` |

소스: [Bullet](../Assets/PlayerSystem/Weapons/GunAndKnife/Bullet.cs), [일반 탄환 프리팹](../Assets/PlayerSystem/Weapons/GunAndKnife/NormalBullet.prefab).

발사각 계산에서 `atan2`의 라디안 값에 도 단위로 쓰이는 분산각을 더한 뒤 다시 도 단위로 해석한다. 방향·분산이 의도와 다를 수 있어 수정 또는 플레이 확인이 필요하다. 최종 발사 수가 1인 경우 분산각 분모가 0이 되는 경로도 있다.

### 4.3 스킬: 칼 투척과 표식

E 입력 시 보유 스택이 1 이상이면 무기 오브젝트 위치 반경 10 안의 자기 외 모든 `Entity`를 검색하고 각 대상 방향으로 칼을 1개씩 생성한다. 적 전용 레이어·진영 필터는 없다.

| 항목 | 값 |
|---|---|
| 칼의 피해 계수 | 50% |
| 칼의 실제 피해 태그 | 물리·**일반 공격** |
| 칼의 에너지 값 | 5 |
| 칼 속도·최대 수명 | 프리팹 값 15, 최대 0.5초 |
| 표식 지속시간 | 프리팹 값 20초 |
| 재표식 | 남은 시간에 20초 추가. 필요 명중 수는 초기화하지 않음 |
| 표식 해제 필요 명중 | `int((B × F + 2) × 3)`, 기본 9회 |
| 명중 카운트 | 대상이 받는 일반 공격 피해 이벤트마다 -1. 공격자가 표식 소유자인지는 검사하지 않음 |
| 표식 완성 피해 | 600% 계수, 물리·스킬 피해 |
| 표식 완성 에너지 값 | 15 |

소스: [KnifeSkill](../Assets/PlayerSystem/Weapons/GunAndKnife/KnifeSkill.cs), [Mark](../Assets/PlayerSystem/Weapons/GunAndKnife/Mark.cs), [Mark.prefab](../Assets/PlayerSystem/Weapons/GunAndKnife/Mark.prefab).

현재 `marking`은 외부 `Mark.prefab` 참조에 직접 플레이어와 표식 상태를 설정한다. 실행용 표식 관리자를 `Instantiate`해서 소유하는 경로가 없으므로 런타임 인스턴스 생성·정리를 보완해야 한다. 스킬을 사용해도 스택이 줄지 않고, 검색 반경 10보다 칼의 속도×수명으로 계산한 명목 이동 거리 7.5가 짧다. 먼 대상에게 명중 가능한지도 확인이 필요하다.

### 4.4 필살기·에너지 구현 상태

- `ultimate()`는 `energy >= requireEnergy` 조건 내부가 비어 있다. 에너지 소모·모드 전환·발사 로직이 없다.
- [KnifeUltimate](../Assets/PlayerSystem/Weapons/GunAndKnife/KnifeUltimate.cs) 클래스도 비어 있다.
- `isUltimate`는 기본 false에서 true로 바뀌는 코드가 없으며, `skill()`의 필살기 상태 분기도 비어 있다.
- `fireCooldownUltimate`, `ultimateDuration`, `maxEnergy`는 실제 필살기 기능에 연결되지 않았다.
- 에너지는 피해 주기 이벤트 값 × `E`, 저스트 회피 시 `5E`, 피격 시 `1E`만큼 증가한다. 상한 제한은 없다.
- 일반 공격·스킬 실행 이벤트를 보내지 않고 필살기 실행 이벤트도 없다.

## 5. 장비 개편과 연결할 작업

1. 무기 일반 공격·스킬·필살기 사용 시점과 피해 태그를 확정하고 트리거가 받는 이벤트를 연결한다.
2. Sniper의 필살기 실제 비용과 이벤트 에너지 값을 일치시키거나 서로 다른 값인 이유를 명시한다.
3. GunAndKnife의 표식 관리자를 실행 인스턴스로 분리하고 스킬 소모·충전·필살기를 완성한다.
4. 무기 교체·해제 시 이벤트 등록, 조준선, UI, 시간 배율, 표식의 정리 규칙을 마련한다.
5. 현재 수치를 기준으로 조준 시간·스탯·다단히트에 따른 피해 및 초당 파워 공급량을 검증한다.

`Assets/PlayerSystem/Skills`와 `UltimateSkills`에는 별도의 스킬·필살기 클래스도 있다. 이들은 현재 `Weapon` 하위 무기 종류 수에 포함하지 않았다. 일반 [Projectile](../Assets/PlayerSystem/Weapons/Projectile.cs) 역시 독립 무기 클래스가 아닌 공용 투사체다.
