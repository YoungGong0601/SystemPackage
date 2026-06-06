using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    // ── Fields ──────────────────────────────────────────────────────────────
    Queue<char> inputQ = new();

    // ── 트리거 시퀀스 정의 ────────────────────────────────────────────────
    //const string SEQ_MAIN_MENU = "menu"; // 예시 시퀀스
    // ───────────────────────────────────────────────────────────────────


    const int MAX_QUEUE = 12; // 가장 긴 시퀀스 길이

    // ── Unity Lifecycle ──────────────────────────────────────────────────────
    void Awake()
    {
    }

    void Update()
    {
        EnqueueInput();
        CheckTriggers();
        HandleCheats();
    }

    // ── Queue 입력 ────────────────────────────────────────────────────────
    void EnqueueInput()
    {
        foreach (char c in Input.inputString)
        {
            inputQ.Enqueue(c);
            if (inputQ.Count > MAX_QUEUE)
                inputQ.Dequeue();
        }
    }

    // ── 트리거 체크 ──────────────────────────────────────────────────────
    void CheckTriggers()
    {
        // if (CheckSequence(SEQ_MAIN_MENU))
        // {
        //     return;
        // }
    }

    // ── 시퀀스 일치 확인 ─────────────────────────────────────────────────
    bool CheckSequence(string sequence)
    {
        if (inputQ.Count < sequence.Length) return false;

        bool match = inputQ.TakeLast(sequence.Length).SequenceEqual(sequence);
        if (match) inputQ.Clear();
        return match;
    }

    // ── Cheats ───────────────────────────────────────────────────────────────
    void HandleCheats()
    {
        if (infinityHealth)
            GM.GameHealth = 6;
    }
}