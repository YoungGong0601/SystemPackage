using System;
using UnityEngine;

// Tick을 안쓰기 위한 고군분투

// OnDestroy에서 Stop() 권장됨. (GC 문제 방지)
// <= Stop 안해도 GC 문제를 해결하고싶음. (콜백 있으면 Manager가 IsOver 되기 전까지 계속 참조함. 만약 Dur = 999... 라면 끝나기 전까지 GC 안됨.)

// [System.Serializable]
public class Timer
{

    // ── Data ──────────────
    public float Duration { get; private set; } = 0f;
    public float Speed { get; private set; } = 1f;

    public bool Unscaled { get; private set; } = false;

    bool _isPaused = false;
    public bool IsPaused => _isPaused;

    float _pauseElapsed = 0f;
    float _startTime = 0f;

    private float Now => Unscaled ? Time.unscaledTime : Time.time;

    public float Elapsed =>
        _isPaused ? _pauseElapsed
                  : (Now - _startTime) * Speed;

    public Action OnFinished { get; set; }

    //━━━━━━━━━━ Get ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    public bool IsOver => Elapsed >= Duration;
    public float Remaining => Mathf.Max(0f, Duration - Elapsed);
    public float Progress => Duration <= 0f ? 1f : Mathf.Clamp01(Elapsed / Duration);

    // ── 생성자 ──────────────

    public Timer(float duration, bool unscaled = false)
    {
        if (duration < 0f) throw new ArgumentException("Duration must be non-negative.");
        Duration = duration;
        Unscaled = unscaled;
    }

    //━━━━━━━━━━ Public methods ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public void Start(Action onFinished = null)
    {
        _startTime = Now;
        _isPaused = false;

        OnFinished = onFinished;

        if (OnFinished != null) TimerTickManager.Instance.RegisterTimer(this);
        else TimerTickManager.Instance.UnregisterTimer(this);
    }

    public void End()
    {
        SetElapsed(Duration);
        OnFinished?.Invoke();
        TimerTickManager.Instance.UnregisterTimer(this);
    }

    public void Pause()
    {
        if (_isPaused) return;

        _pauseElapsed = Elapsed;
        _isPaused = true;
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;
        SetElapsed(_pauseElapsed);
    }

    public Timer Shift(float dt)
    {
        SetElapsed(Elapsed + dt);
        return this;
    }

    public Timer SetSpeed(float s)
    {
        if (s <= 0f) { Pause(); return this; }

        float e = Elapsed;
        Speed = s;
        SetElapsed(e);
        return this;
    }

    public Timer SetDuration(float d)
    {
        if (d < 0f) throw new ArgumentException("Duration must be non-negative.");
        Duration = d;
        return this;
    }

    private void SetElapsed(float e)
    {
        e = Mathf.Clamp(e, 0f, Duration);
        if (_isPaused) _pauseElapsed = e;
        else _startTime = Now - e / Speed;
    }

    public void DebugLog()
    {
        Debug.Log($"[Timer] Elapsed: {Elapsed}, Remaining: {Remaining}, Progress: {Progress * 100f}%");
    }
}