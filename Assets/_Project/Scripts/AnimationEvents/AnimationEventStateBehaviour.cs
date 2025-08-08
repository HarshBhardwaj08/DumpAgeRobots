using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Triggers specific UnityEvents based on normalized animation time and current blend value.
/// Useful for Blend Trees where different motions correspond to different events.
/// </summary>
public class AnimationEventStateBehaviour : StateMachineBehaviour
{
    [Serializable]
    public class ClipEvent
    {
        public string clipName;  // Optional label for clarity
        public float threshold;  // Blend parameter threshold
        public string eventName; // Event to trigger
    }

    [Header("Blend Tree Settings")]
    public string blendParameter = "Attacks";
    public List<ClipEvent> clipEvents = new();

    [Header("Trigger Timing")]
    [Tooltip("Time (normalized 0-1) when the event should trigger.")]
    [Range(0f, 1f)] public float triggerTime = 0.3f;

    [Header("Editor Preview Only")]
    [Tooltip("Used for previewing BlendTree weights in Editor only.")]
    public float blendParameterValue = 0f;

    [Tooltip("Used for previewing time (0-1) in Editor only.")]
    [Range(0f, 1f)] public float clipTime = 0f;

    bool hasTriggered;
    AnimationEventReceiver receiver;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hasTriggered = false;
        receiver = animator.GetComponent<AnimationEventReceiver>();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float currentTime = stateInfo.normalizedTime % 1f;

        if (!hasTriggered && currentTime >= triggerTime)
        {
            TriggerEvent(animator);
            hasTriggered = true;
        }
    }

    void TriggerEvent(Animator animator)
    {
        if (receiver == null || clipEvents.Count == 0) return;

        float currentBlendValue = animator.GetFloat(blendParameter);
        ClipEvent closest = null;
        float smallestDiff = float.MaxValue;

        foreach (var clipEvent in clipEvents)
        {
            float diff = Mathf.Abs(clipEvent.threshold - currentBlendValue);
            if (diff < smallestDiff)
            {
                smallestDiff = diff;
                closest = clipEvent;
            }
        }

        if (closest != null)
        {
            receiver.OnAnimationEventTriggered(closest.eventName);
#if UNITY_EDITOR
            Debug.Log($"[AnimationEvent] Triggered '{closest.eventName}' at blend value {currentBlendValue:F2}");
#endif
        }
    }
}
