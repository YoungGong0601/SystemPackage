using System.Collections.Generic;
using UnityEngine;

// 문제점:
// - Timer 미해지 시 GC 문제 야기됨.
// - Timer 종료까지 순서없이 모두 체크함.  남은 시간으로 미리 정렬해서 최적화 가능할듯...? (특정 Timer부터 시간이 남으면 그 뒤는 체크 안해도 됨)
//   하지만 오히려 비용이 더 클 수도 있음 (재배열 비용)
//   지금으로선 오버엔지니어링

// 아얘 CallBackTickManager로 포괄적인 System 만들면 좋을듯. (아이디어)

[UnityEngine.DefaultExecutionOrder(-100)]
public class TimerTickManager : MonoBehaviour
{
    public static TimerTickManager Instance { get; private set; }

    // private readonly List<Timer> _actionTimer = new();
    // 오버엔지니어링 인듯 n < 1000 일텐데. 성능 문제 시 inner indexing로 바꿔보자. (idx를 timer가 직접 보유)
    // HashSet 순회 비용이 걸렸었다.

    private readonly HashSet<Timer> _actionTimer = new();

    private readonly HashSet<Timer> _toRemove = new();

    private readonly List<Timer> _buffer = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Init()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("TimerTickManager");
        go.AddComponent<TimerTickManager>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        Tick();
    }

    public void Tick()
    {
        _buffer.Clear();
        _buffer.AddRange(_actionTimer);

        foreach (var timer in _buffer)
        {
            if (timer == null)
            {
                _toRemove.Add(timer);
                continue;
            }
            if (timer.IsOver)
            {
                _toRemove.Add(timer);
            }
        }

        // Remove timers
        if (_toRemove.Count == 0) return;
        foreach (var timer in _toRemove)
        {
            _actionTimer.Remove(timer);
            timer.OnFinished?.Invoke();
        }
        _toRemove.Clear();
    }

    //━━━━━━━━━━ Public Methods ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public void RegisterTimer(Timer timer)
    {
        _actionTimer.Add(timer);
    }

    public void UnregisterTimer(Timer timer)
    {
        _actionTimer.Remove(timer);
    }
}