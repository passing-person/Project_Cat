using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class NpcTimer : MonoBehaviour
{
    // templates
    public NpcTimerTemplate timerTemplate;

    // cache
    private readonly Dictionary<NpcTimerType, Timer> timers = new();

    private void Awake()
    {
        InitializeTimers();
    }

    private void Update()
    {
        TimeTick();
    }

    private void TimeTick()
    {
        foreach (var timer in timers.Values)
        {
            timer.Tick(Time.deltaTime);
        }
    }

    public void InitializeTimer(NpcTimerType timerType)
    {
        if (timerTemplate == null)
        {
            Debug.LogWarning("NpcTimerTemplate is not assigned. Cannot initialize timer.");
            return;
        }
        var values = timerTemplate.timerValues.Find(tv => tv.timerType == timerType);

        if (values == null)
        {
            Debug.LogWarning($"Timer {timerType} does not exist in the template.");
            return;
        }

        timers[timerType] = new Timer(
            timerType,
            values.duration,
            values.isOneShot);
    }

    public void InitializeTimers()
    {
        if (timerTemplate == null)
        {
            Debug.LogWarning("NpcTimerTemplate is not assigned. Cannot initialize timers.");
            return;
        }
        foreach (var timerValue in timerTemplate.timerValues)
        {
            timers[timerValue.timerType] = new Timer(timerValue.timerType, timerValue.duration, timerValue.isOneShot, null);
        }
    }

    public void StartTimer(NpcTimerType timerType, Action callback)
    {
        // timer of the specified type doesn't exist, create a new one with timer template values
        if (!timers.TryGetValue(timerType, out var timer))
        {
            InitializeTimer(timerType);
            timer = timers[timerType];
        }
        timer.Start(callback);
    }

    public void StartTimer(NpcTimerType timerType, float duration, Action callback = null)
    {
        timers[timerType] = new Timer(timerType, duration, true, callback);
        timers[timerType].Start();
    }

    public void StopTimer(NpcTimerType timerType)
    {
        if (!timers.TryGetValue(timerType, out var timer))
        {
            Debug.LogWarning($"Timer {timerType} does not exist. Cannot stop timer.");
            return;
        }
        timer.Stop();
    }

    public void ResetTimer(NpcTimerType timerType)
    {
        if (!timers.TryGetValue(timerType, out var timer))
        {
            Debug.LogWarning($"Timer {timerType} does not exist. Cannot reset timer.");
            return;
        }
        timer.Reset();
    }

    public bool IsRunning(NpcTimerType timerType)
    {
        if (!timers.TryGetValue(timerType, out var timer))
        {
            Debug.LogWarning($"Timer {timerType} does not exist. Cannot check if timer is running.");
            return false;
        }
        return timer.IsRunning();
    }

    public float Remaining(NpcTimerType timerType)
    {
        if (!timers.TryGetValue(timerType, out var timer))
        {
            Debug.LogWarning($"Timer {timerType} does not exist. Cannot get remaining time.");
            return 0f;
        }
        return timer.GetRemainingTime();
    }

    [System.Serializable]
    private class Timer
    {
        public readonly NpcTimerType timerType;
        public readonly float duration;
        private float remainingTime;
        private float elapsedTime;
        private bool isRunning;
        private bool IsFinished => remainingTime <= 0f;
        private bool isOneShot = true;
        private Action callback = null;

        public Timer(NpcTimerType timerType,
            float duration, 
            bool isOneShot = true,
            Action callback = null)
        {
            this.timerType = timerType;
            this.duration = duration;
            this.remainingTime = duration;
            this.elapsedTime = 0f;
            this.isRunning = false;
            this.isOneShot = isOneShot;
            this.callback = callback;
        }

        public void Start(Action callback = null)
        {
            this.callback = callback;
            remainingTime = duration;
            elapsedTime = 0f;
            isRunning = true;
        }

        public void Stop()
        {
            isRunning = false;
        }

        public void Tick(float dt)
        {
            if (!isRunning) return;

            elapsedTime += dt;
            remainingTime -= dt;

            if (!IsFinished) return;

            callback?.Invoke();

            if (isOneShot) Reset();
            else Start();
        }

        public void Reset()
        {
            remainingTime = duration;
            elapsedTime = 0f;
            isRunning = false;
        }

        public bool IsRunning()
        {
            return isRunning;
        }

        public bool IsOneShot()
        {
            return isOneShot;
        }

        public void SetIsOneShot(bool isOneShot)
        {
            this.isOneShot = isOneShot;
        }

        public Action GetCallback()
        {
            return callback;
        }

        public float GetRemainingTime()
        {
            return remainingTime;
        }

        public float GetElapsedTime()
        {
            return elapsedTime;
        }
    }
}

public enum NpcTimerType
{
    Chase,
    ChaseCooldown,
    DiveWindUp,
    Dive,
    DiveCooldown,
    Search
}