using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[AddComponentMenu("Debug/Debug Play Animation")]
public class DebugPlayAnimation : MonoBehaviour {
    public KeyCode triggerKey = KeyCode.Space;

    [Header("References (auto-filled on Reset)")]
    public Animator animator;

    [Header("Animation selection")]
    [Tooltip("If true the assigned AnimationClip will be played directly (via Playables). Otherwise the Animator state name will be used.")]
    public bool useClip = false;
    public bool playFollowUp = false;
    [Tooltip("Animation clip to play when 'useClip' is true.")]
    public AnimationClip clip;
    public AnimationClip followUpClip;
    [Tooltip("Animator state name (or full path like \"LayerName.StateName\") used when 'useClip' is false.")]
    public string stateName;
    [Tooltip("Normalized start time (0..1) when using stateName playback.")]
    [Range(0f, 1f)]
    public float normalizedStartTime = 0f;

    private Coroutine clipCoroutine;         
    private Coroutine followUpCoroutine;   

    public float followUpDelay = 0f;
    [Header("Crossfade (state-name playback)")]
    [Tooltip("Crossfade instead of hard play when playing by state name.")]
    public bool crossfade = true;
    [Tooltip("Crossfade duration in seconds.")]
    public float crossfadeDuration = 0.1f;

    // Playables
    private PlayableGraph playableGraph;
    private bool graphPlaying = false;

    void Reset() {
        // Helpful defaults when adding component in editor
        animator = GetComponent<Animator>();
    }

    void Update() {
        if (Input.GetKeyDown(triggerKey)) {
            PlayDebug();
        }
    }

    /// <summary>
    /// Public method to trigger the configured animation (callable from other scripts or buttons).
    /// </summary>
    public void PlayDebug() {
        if (animator == null) {
            Debug.LogWarning($"[{name}] DebugPlayAnimation: Animator is not assigned.");
            return;
        }

        if (useClip) {
            if (clip == null) {
                Debug.LogWarning($"[{name}] DebugPlayAnimation: useClip is true but no AnimationClip assigned.");
                return;
            }
            PlayClip(clip);
        } else {
            if (string.IsNullOrEmpty(stateName)) {
                Debug.LogWarning($"[{name}] DebugPlayAnimation: useClip is false but stateName is empty.");
                return;
            }
            PlayState(stateName);
        }
    }

    private void PlayState(string state) {
        // Play the state
        if (crossfade)
            animator.CrossFadeInFixedTime(state, crossfadeDuration, 0, normalizedStartTime);
        else
            animator.Play(state, 0, normalizedStartTime);

        // Cancel any previously scheduled follow-up
        if (followUpCoroutine != null) {
            StopCoroutine(followUpCoroutine);
            followUpCoroutine = null;
        }

        // If user requested a follow-up clip, try to wait for the state to become active, then wait for its remaining length
        if (playFollowUp && followUpClip != null) {
            followUpCoroutine = StartCoroutine(WaitForStateAndThenPlayFollowUp(state));
        }
    }

    private void PlayClip(AnimationClip clipToPlay, bool isFollowUp = false) {
        // stop any currently-playing playable graph first
        if (graphPlaying) {
            StopAndDestroyGraph();
            if (clipCoroutine != null) {
                StopCoroutine(clipCoroutine);
                clipCoroutine = null;
            }
        }

        // Build and play the PlayableGraph for the clip
        playableGraph = PlayableGraph.Create($"DebugPlay_{gameObject.name}_{clipToPlay.name}");
        var output = AnimationPlayableOutput.Create(playableGraph, "DebugAnimOutput", animator);
        var clipPlayable = AnimationClipPlayable.Create(playableGraph, clipToPlay);
        clipPlayable.SetApplyFootIK(false);
        output.SetSourcePlayable(clipPlayable);

        playableGraph.Play();
        graphPlaying = true;

        // ensure any existing follow-up scheduling is cancelled when starting a fresh clip
        if (followUpCoroutine != null) {
            StopCoroutine(followUpCoroutine);
            followUpCoroutine = null;
        }

        // start coroutine that will stop/destroy graph after clip length and optionally queue follow-up
        clipCoroutine = StartCoroutine(DestroyGraphAfterSeconds(clipToPlay.length, () =>
        {
            // if we're allowed to play a follow-up, and this clip isn't already the follow-up itself
            if (playFollowUp && !isFollowUp && followUpClip != null) {
                followUpCoroutine = StartCoroutine(PlayFollowUpAfter(followUpDelay));
            }
        }));
    }


    private System.Collections.IEnumerator DestroyGraphAfterSeconds(float seconds, System.Action onComplete = null) {
        yield return new WaitForSeconds(seconds);

        StopAndDestroyGraph();
        clipCoroutine = null;

        onComplete?.Invoke();
    }
    private System.Collections.IEnumerator PlayFollowUpAfter(float delay) {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        followUpCoroutine = null;
        // call PlayClip for the follow-up; mark it as 'isFollowUp' so we don't recursively schedule it again
        PlayClip(followUpClip, isFollowUp: true);
    }

    private System.Collections.IEnumerator WaitForStateAndThenPlayFollowUp(string stateNameToWaitFor) {
        const float timeout = 5f; // safety timeout in seconds to avoid infinite wait if state never appears
        float timer = 0f;
        int layer = 0;

        // Wait until the animator reports the target state (or timeout)
        while (timer < timeout) {
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName(stateNameToWaitFor)) {
                // Calculate remaining time in that state's current cycle (respect normalizedStartTime)
                float remaining = Mathf.Max(0f, (1f - normalizedStartTime) * info.length);
                // Wait for remaining + optional followUpDelay, then play the follow-up
                yield return new WaitForSeconds(remaining + followUpDelay);
                followUpCoroutine = null;
                PlayClip(followUpClip, isFollowUp: true);
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // If we hit the timeout, just attempt to play follow-up after the delay as a fallback
        followUpCoroutine = null;
        if (followUpDelay > 0f)
            yield return new WaitForSeconds(followUpDelay);

        PlayClip(followUpClip, isFollowUp: true);
    }
    private void StopAndDestroyGraph() {
        if (graphPlaying && playableGraph.IsValid()) {
            playableGraph.Stop();
            playableGraph.Destroy();
        }
        graphPlaying = false;
    }

    void OnDisable() {
        // ensure cleanup if object is disabled/destroyed
        if (clipCoroutine != null) {
            StopCoroutine(clipCoroutine);
            clipCoroutine = null;
        }
        StopAndDestroyGraph();
    }
}
