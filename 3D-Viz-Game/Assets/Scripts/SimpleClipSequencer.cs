using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.Collections;

public class SimpleClipSequencer : MonoBehaviour
{
    [Header("Target")]
    public Animator animator;                      // If null, grabs from children

    [Header("Sorting Clips (default)")]
    public AnimationClip[] clips;                  // keep this as your SORTING set

    [Header("Dishes Clips")]
    public AnimationClip[] clipsDishes;            // assign 3 dishes clips here

    [Header("Playback")]
    public bool playOnStart = true;
    public bool loop = true;                       // Restart after last clip
    [Range(0.1f, 3f)] public float playbackSpeed = 1f;
    public bool freezeLastPoseOnStop = false;      // Let controller retake pose on stop

    [Header("Blend Over Controller (movement-safe)")]
    [Tooltip("If true and the Animator has a controller, play sequence as an overlay on top of it.")]
    public bool blendOverController = true;
    [Tooltip("Mask for overlay (e.g., UpperBody). If null, overlay affects full body.")]
    public AvatarMask upperBodyMask;
    [Tooltip("Turn OFF root motion while sequence is playing (good if overlay affects hips).")]
    public bool disableRootMotionDuringSequence = true;

    // Playables
    private PlayableGraph graph;
    private AnimationPlayableOutput output;

    // Base controller playable + our clip mixer, optionally routed through a layer mixer
    private AnimatorControllerPlayable baseControllerPlayable; // only valid if Animator has a controller
    private AnimationMixerPlayable clipMixer;                  // we feed one clip at a time
    private AnimationLayerMixerPlayable layerMixer;            // [0]=base, [1]=overlay
    private bool usingOverlay = false;

    private Coroutine runCo;
    private bool overlayActive = false;
    private bool originalApplyRootMotion;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("[SimpleClipSequencer] No Animator found.");
            enabled = false;
            return;
        }

        originalApplyRootMotion = animator.applyRootMotion;

        graph = PlayableGraph.Create("SimpleClipSequencer");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        clipMixer = AnimationMixerPlayable.Create(graph, 1); // no obsolete arg

        // If we have a controller AND want to blend over it, build a 2-layer graph.
        var controller = animator.runtimeAnimatorController;
        if (blendOverController && controller != null)
        {
            baseControllerPlayable = AnimatorControllerPlayable.Create(graph, controller);

            layerMixer = AnimationLayerMixerPlayable.Create(graph, 2);
            graph.Connect(baseControllerPlayable, 0, layerMixer, 0);
            graph.Connect(clipMixer,             0, layerMixer, 1);

            layerMixer.SetInputWeight(0, 1f);   // base always on
            layerMixer.SetInputWeight(1, 0f);   // overlay off until we play

            if (upperBodyMask != null)
                layerMixer.SetLayerMaskFromAvatarMask(1, upperBodyMask);

            output = AnimationPlayableOutput.Create(graph, "AnimOut", animator);
            output.SetSourcePlayable(layerMixer);

            usingOverlay = true;
        }
        else
        {
            // No base controller to blend with → send our mixer directly to the Animator.
            output = AnimationPlayableOutput.Create(graph, "AnimOut", animator);
            output.SetSourcePlayable(clipMixer);
            usingOverlay = false;
        }

        graph.Play();
    }

    void Start()
    {
        if (playOnStart && clips != null && clips.Length > 0)
            StartSequence();
    }

    void OnDisable()
    {
        StopSequence();
    }

    void OnDestroy()
    {
        StopSequence();
        if (graph.IsValid()) graph.Destroy();
    }

    // ---------- Convenience API ----------
    public void PlaySorting(bool looping = true)  { PlaySequence(clips, looping); }
    public void PlayDishes (bool looping = true)  { PlaySequence(clipsDishes, looping); }

    /// Replace the current working set and (re)start playback.
    public void PlaySequence(AnimationClip[] newClips, bool loopOverride = true)
    {
        if (newClips == null || newClips.Length == 0)
        {
            Debug.LogWarning("[SimpleClipSequencer] Provided clip array is empty.");
            return;
        }
        clips = newClips;      // reuse runner’s working list
        loop  = loopOverride;
        StartSequence();
    }

    /// Starts (or restarts) playing the currently assigned 'clips'.
    public void StartSequence()
    {
        if (runCo != null) StopCoroutine(runCo);
        runCo = StartCoroutine(PlayClips());
    }

    /// Stops playback and clears overlay so controller regains full control.
    public void StopSequence()
    {
        if (runCo != null)
        {
            StopCoroutine(runCo);
            runCo = null;
        }

        // Clear mixer input
        if (clipMixer.IsValid() && clipMixer.GetInputCount() > 0)
        {
            clipMixer.SetInputWeight(0, 0f);
            if (graph.IsValid() && graph.IsPlaying()) graph.Evaluate(0f);
            if (graph.IsValid()) graph.Disconnect(clipMixer, 0);
        }

        // Turn off overlay layer weight if we created a layer mixer
        if (usingOverlay && layerMixer.IsValid())
        {
            layerMixer.SetInputWeight(1, 0f);
            overlayActive = false;
        }

        // Restore root motion
        if (disableRootMotionDuringSequence) animator.applyRootMotion = originalApplyRootMotion;

        // Optionally detach to let Animator resume immediately
        if (!freezeLastPoseOnStop && output.IsOutputValid())
            output.SetSourcePlayable(usingOverlay ? (Playable)layerMixer : (Playable)clipMixer);
    }

    private IEnumerator PlayClips()
    {
        if (clips == null || clips.Length == 0) yield break;

        var list = new System.Collections.Generic.List<AnimationClip>();
        foreach (var c in clips) if (c != null) list.Add(c);
        if (list.Count == 0) yield break;

        // Movement-safe: enable overlay and (optionally) disable root motion while playing
        if (usingOverlay && !overlayActive)
        {
            layerMixer.SetInputWeight(1, 1f);
            overlayActive = true;
        }
        if (disableRootMotionDuringSequence) animator.applyRootMotion = false;

        do
        {
            for (int i = 0; i < list.Count; i++)
            {
                var clip = list[i];
                if (clip == null) continue;

                var speed = Mathf.Max(0.0001f, playbackSpeed);

                var p = AnimationClipPlayable.Create(graph, clip);
                p.SetApplyFootIK(true);
                p.SetApplyPlayableIK(false);
                p.SetSpeed(speed);
                p.SetTime(0);

                EnsureMixerInputCount(clipMixer, 1);
                graph.Connect(p, 0, clipMixer, 0);
                clipMixer.SetInputWeight(0, 1f);

                // Speed-aware wait in real seconds
                float wait = clip.length / speed;
                float t = 0f;
                while (t < wait)
                {
                    t += Time.deltaTime;
                    yield return null;
                }

                // Cleanup this clip before next
                clipMixer.SetInputWeight(0, 0f);
                if (graph.IsValid()) graph.Disconnect(clipMixer, 0);
                p.Destroy();
            }
        }
        while (loop);

        // sequence finished (only when loop == false)
        if (usingOverlay && layerMixer.IsValid())
        {
            layerMixer.SetInputWeight(1, 0f);
            overlayActive = false;
        }
        if (disableRootMotionDuringSequence) animator.applyRootMotion = originalApplyRootMotion;

        runCo = null;
    }

    private void EnsureMixerInputCount(Playable mixerPlayable, int count)
    {
        if (!mixerPlayable.IsValid()) return;

        if (mixerPlayable.IsPlayableOfType<AnimationMixerPlayable>())
        {
            var m = (AnimationMixerPlayable)mixerPlayable;
            if (m.GetInputCount() < count)
                m.SetInputCount(count);
        }
    }
}
