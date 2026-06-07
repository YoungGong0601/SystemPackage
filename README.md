# SystemPackage

Unity 게임 개발에 필요한 핵심 시스템 모음입니다. 이벤트 통신, 오브젝트 관리, 스탯 계산, 타이머, 시드 랜덤 등 게임 로직에서 반복적으로 쓰이는 기능들을 의존성 없이 가볍게 제공합니다.

## 구성

| 시스템 | 위치 | 설명 |
| --- | --- | --- |
| **EventBus** | `System/EventBus/EventBus.cs` | 타입 기반 전역 이벤트 발행/구독 시스템 |
| **ObjectManager** | `System/ObjectManager.cs` | 타입별 GameObject 등록/조회용 정적 매니저 |
| **Stat** | `System/Stat.cs` | 레이어 기반 스탯 계산 (Observer + Dirty Flag) |
| **Timer** | `System/Timer/` | Tick 비의존 경량 타이머 |
| **RandomSeed** | `System/RandomSeed.cs` | 시드 고정형 랜덤 호출기 |

---

## EventBus

`struct` 이벤트 타입을 키로 사용하는 전역 발행/구독 시스템입니다. 핸들러 예외가 발생해도 나머지 핸들러는 안전하게 실행됩니다.

```csharp
// 이벤트 정의
public struct OnPlayerDashed { public int continuousCount; }

// 구독
EventBus.Subscribe<OnPlayerDashed>(e => Debug.Log(e.continuousCount));

// 발행
EventBus.Publish(new OnPlayerDashed { continuousCount = 3 });

// 해제
EventBus.Unsubscribe<OnPlayerDashed>(handler);
```

- `Sub` / `Unsub` / `Pub` 축약 메서드 제공
- `Clear<T>()` 특정 이벤트 정리, `ClearAll()` 전체 정리
- 씬 로드 전(`BeforeSceneLoad`) 자동 초기화

## ObjectManager

타입(문자열 키)별로 `GameObject` 집합을 관리합니다. 오브젝트의 `OnEnable` / `OnDisable`에서 등록·해제하는 것을 권장합니다.

```csharp
void OnEnable()  => ObjectManager.RegisterObject(ObjectManager.ENEMY, gameObject);
void OnDisable() => ObjectManager.UnregisterObject(ObjectManager.ENEMY, gameObject);

// 조회
if (ObjectManager.TryGetObjects(ObjectManager.ENEMY, out var enemies))
{
    foreach (var enemy in enemies) { /* ... */ }
}
```

- 등록/해제 시 `OnObjectsRegistered` / `OnObjectsUnregistered` 이벤트를 EventBus로 발행
- 중복 등록·null·미등록 해제는 경고 로그 후 무시

## Stat

레이어 기반 스탯 계산 시스템입니다. Observer와 Dirty Flag 패턴으로 값 변경을 추적하고 캐싱합니다.

**계산 공식:** `((value + Flat) + (value * PercentAdd)) * Multiplier`

```csharp
var attack = new Stat(10f);

// 모디파이어 추가 (sourceKey로 식별)
attack.AddModifier(new StatModifier(5f, ModifierType.Flat, "weapon"));
attack.AddModifier(new StatModifier(0.2f, ModifierType.PercentAdd, "buff"), layer: (int)StatLayerType.Buff);

float result = attack.Value;      // 자동 캐싱 계산
float raw    = attack;            // float 암시적 변환 지원
```

- **레이어**: `StatLayerType` (Default / Buff / Debuff / FinalCalculation)로 계산 순서 분리
- **ModifierType**: `Flat`(합) / `PercentAdd`(비율 합) / `Multiplier`(곱)
- **DynamicModifier**: 다른 `Stat` 값을 비율로 참조하며 원본 변경 시 자동 갱신
- `ValueExcluding(keys)` / `ValueWithOutFinalCalculation`로 부분 기여분 분리 계산
- `OnValueChanged` 콜백, `DebugModifierList()` 디버그 출력 제공

## Timer

`Update` Tick에 매번 의존하지 않는 경량 타이머입니다. 경과 시간을 `Time.time` 기반으로 즉석 계산합니다.

```csharp
var timer = new Timer(3f);
timer.Start(() => Debug.Log("완료!"));   // 콜백 등록 시에만 TickManager에 등록

if (timer.IsOver) { /* ... */ }
float p = timer.Progress;                // 0~1

timer.Pause();
timer.Resume();
timer.SetSpeed(2f).Shift(0.5f);          // 배속·시간 이동 체이닝
```

- `Elapsed` / `Remaining` / `Progress` / `IsOver` 조회
- `Unscaled` 옵션으로 `Time.unscaledTime` 사용 가능
- 콜백이 필요한 경우에만 `TimerTickManager`가 완료를 감지·실행 (자동 생성, `DontDestroyOnLoad`)

## RandomSeed

스테이지마다 동일한 랜덤 결과를 재현하기 위한 시드 고정형 랜덤입니다.

```csharp
RandomSeed.SetSeed(12345);

int n = RandomSeed.Random(0, 100);       // 고정 시드 기반
var shuffled = RandomSeed.Shuffle(list); // Fisher–Yates 셔플

int once = RandomSeed.Random(0, 10, seed: 777); // 일회성 시드 지정
```

---

## 설치

`System/` 폴더를 Unity 프로젝트의 `Assets` 하위에 복사합니다. 외부 의존성은 없으며 UnityEngine만 사용합니다.
