using UnityEngine;
using System.Collections;

public class ClipSequenceLooper : MonoBehaviour
{
    [Header("Animator / Layer")]
    public Animator animator;          // Character's Animator
    public int layer = 0;              // Animator layer index

    [Header("States in order")]
    public string[] stateOrder = { "Sort_1", "Sort_2", "Sort_3", "Sort_4", "Sort_5" };

    [Header("Playback")]
    [Range(0f,1f)] public float crossFade = 0.15f;
    public bool restartFromFirstOnStart = true;

    [Header("Optional Trigger Start/Stop")]
    public bool useTrigger = true;     // Put this on a trigger collider zone
    public string playerTag = "Player";

    Coroutine loopRoutine;
    bool running;
    int nextIndex;

    public void StartSequence()
    {
        if (animator == null || stateOrder == null || stateOrder.Length == 0 || running) return;
        running = true;
        if (restartFromFirstOnStart) nextIndex = 0;
        loopRoutine = StartCoroutine(Loop());
    }

    public void StopSequence()
    {
        running = false;
        if (loopRoutine != null) { StopCoroutine(loopRoutine); loopRoutine = null; }
    }

    IEnumerator Loop()
    {
        while (running)
        {
            string state = stateOrder[nextIndex];
            animator.CrossFadeInFixedTime(state, crossFade, layer, 0f);

            // wait for current state and run to near end
            yield return null;
            while (running)
            {
                var info = animator.GetCurrentAnimatorStateInfo(layer);
                if (info.IsName(state) && info.normalizedTime >= 0.98f) break;
                yield return null;
            }
            nextIndex = (nextIndex + 1) % stateOrder.Length;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (useTrigger && other.CompareTag(playerTag)) StartSequence();
    }

    void OnTriggerExit(Collider other)
    {
        if (useTrigger && other.CompareTag(playerTag)) StopSequence();
    }
}
