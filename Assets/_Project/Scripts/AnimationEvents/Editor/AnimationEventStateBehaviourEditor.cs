#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections.Generic;

[CustomEditor(typeof(AnimationEventStateBehaviour))]
public class AnimationEventStateBehaviourEditor : Editor
{
    Motion previewClip;
    bool isPreviewing;
    PlayableGraph playableGraph;
    AnimationMixerPlayable mixer;

    SerializedProperty clipEventsProp;
    SerializedProperty blendParameterValueProp;
    SerializedProperty clipTimeProp;

    void OnEnable()
    {
        clipEventsProp = serializedObject.FindProperty("clipEvents");
        blendParameterValueProp = serializedObject.FindProperty("blendParameterValue");
        clipTimeProp = serializedObject.FindProperty("clipTime");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        AnimationEventStateBehaviour stateBehaviour = (AnimationEventStateBehaviour)target;
        GameObject selected = Selection.activeGameObject;
        Animator animator = null;
        if (Selection.activeObject != null)
        {
            EditorGUILayout.HelpBox("You selected: " + Selection.activeObject, MessageType.Info);
        }
        if (selected != null)
        {
            animator = selected.GetComponent<Animator>();
        }
      
        if (animator != null)
        {
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller != null)
            {
                int motionCount = BlendTreeUtils.GetBlendTreeMotionCount(controller, "Attack Tree");
                EditorGUILayout.Slider(blendParameterValueProp, 0f, Mathf.Max(1, motionCount - 1));
            }
            else
            {
                EditorGUILayout.HelpBox("Selected object does not have a valid Animator Controller.", MessageType.Warning);
            }
        }


        //  EditorGUILayout.Slider(blendParameterValueProp, 0f, 5f);
        EditorGUILayout.Slider(clipTimeProp, 0f, 1f);

        GUILayout.Space(5);

        // Show ClipEvents as reorderable list
        EditorGUILayout.LabelField("Clip Events", EditorStyles.boldLabel);
        if (clipEventsProp != null)
        {
            for (int i = 0; i < clipEventsProp.arraySize; i++)
            {
                SerializedProperty element = clipEventsProp.GetArrayElementAtIndex(i);
                SerializedProperty clipName = element.FindPropertyRelative("clipName");
                SerializedProperty threshold = element.FindPropertyRelative("threshold");
                SerializedProperty eventName = element.FindPropertyRelative("eventName");

                EditorGUILayout.BeginVertical("box");
                clipName.stringValue = EditorGUILayout.TextField("Clip Name", clipName.stringValue);
                threshold.floatValue = EditorGUILayout.FloatField("Threshold", threshold.floatValue);
                eventName.stringValue = EditorGUILayout.TextField("Event Name", eventName.stringValue);

                if (GUILayout.Button("Remove"))
                {
                    clipEventsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Clip Event"))
            {
                clipEventsProp.arraySize++;
            }
        }

        GUILayout.Space(10);

        if (Validate(stateBehaviour, out string errorMessage))
        {
            if (isPreviewing)
            {
                if (GUILayout.Button("Stop Preview"))
                {
                    StopPreview();
                }
                else
                {
                    PreviewAnimationClip(stateBehaviour);
                }

                GUILayout.Label($"Previewing at {stateBehaviour.clipTime:F2}s", EditorStyles.helpBox);
            }
            else
            {
                if (GUILayout.Button("Preview"))
                {
                    isPreviewing = true;
                    AnimationMode.StartAnimationMode();
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox(errorMessage, MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
    public static class BlendTreeUtils
    {
        public static int GetBlendTreeMotionCount(AnimatorController controller, string blendTreeStateName)
        {
            if (controller == null)
                return 0;

            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.name == blendTreeStateName && state.state.motion is BlendTree blendTree)
                    {
                        return blendTree.children.Length;
                    }
                }
            }

            return 0;
        }
    }
    void StopPreview()
    {
        EnforceTPose();
        isPreviewing = false;
        AnimationMode.StopAnimationMode();
        if (playableGraph.IsValid()) playableGraph.Destroy();
    }

    void PreviewAnimationClip(AnimationEventStateBehaviour stateBehaviour)
    {
        AnimatorController animatorController = GetValidAnimatorController(out _);
        if (animatorController == null) return;

        ChildAnimatorState matchingState = animatorController.layers
            .Select(layer => FindMatchingState(layer.stateMachine, stateBehaviour))
            .FirstOrDefault(state => state.state != null);

        if (matchingState.state == null) return;

        Motion motion = matchingState.state.motion;

        if (motion is BlendTree blendTree)
        {
            SampleBlendTreeAnimation(stateBehaviour, blendTree);
        }
        else if (motion is AnimationClip clip)
        {
            float previewTime = stateBehaviour.clipTime * clip.length;
            AnimationMode.SampleAnimationClip(Selection.activeGameObject, clip, previewTime);
        }
    }

    float GetBlendedClipLength(AnimationClipPlayable[] clips, float[] weights)
    {
        float length = 0f;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i].GetAnimationClip();
            if (clip != null)
            {
                length += clip.length * weights[i];
            }
        }
        return Mathf.Max(0.01f, length);
    }

    void SampleBlendTreeAnimation(AnimationEventStateBehaviour stateBehaviour, BlendTree blendTree)
    {
        Animator animator = Selection.activeGameObject.GetComponent<Animator>();
        if (!animator) return;

        if (playableGraph.IsValid()) playableGraph.Destroy();

        playableGraph = PlayableGraph.Create("BlendTreePreviewGraph");
        mixer = AnimationMixerPlayable.Create(playableGraph, blendTree.children.Length);
        Transform root = animator.transform;
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        var output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
        output.SetSourcePlayable(mixer);

        float maxThreshold = blendTree.children.Max(child => child.threshold);
        float targetWeight = Mathf.Clamp(stateBehaviour.blendParameterValue, blendTree.minThreshold, maxThreshold);

        AnimationClipPlayable[] clipPlayables = new AnimationClipPlayable[blendTree.children.Length];
        float[] weights = new float[blendTree.children.Length];
        float totalWeight = 0f;

        for (int i = 0; i < blendTree.children.Length; i++)
        {
            ChildMotion child = blendTree.children[i];
            float weight = CalculateWeightForChild(blendTree, child, targetWeight);
            weights[i] = weight;
            totalWeight += weight;

            AnimationClip clip = GetAnimationClipFromMotion(child.motion);
            clipPlayables[i] = AnimationClipPlayable.Create(playableGraph, clip);
        }

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] /= totalWeight;
            mixer.ConnectInput(i, clipPlayables[i], 0);
            mixer.SetInputWeight(i, weights[i]);
        }

        float finalClipLength = GetBlendedClipLength(clipPlayables, weights);
        float finalTime = stateBehaviour.clipTime * finalClipLength;
        AnimationMode.SamplePlayableGraph(playableGraph, 0, finalTime);
    }

    float CalculateWeightForChild(BlendTree blendTree, ChildMotion child, float targetWeight)
    {
        float weight = 0f;

        if (blendTree.blendType == BlendTreeType.Simple1D)
        {
            ChildMotion? lower = null, upper = null;

            foreach (var motion in blendTree.children)
            {
                if (motion.threshold <= targetWeight && (lower == null || motion.threshold > lower.Value.threshold))
                    lower = motion;
                if (motion.threshold >= targetWeight && (upper == null || motion.threshold < upper.Value.threshold))
                    upper = motion;
            }

            if (lower.HasValue && upper.HasValue)
            {
                if (Mathf.Approximately(child.threshold, lower.Value.threshold))
                    weight = 1f - Mathf.InverseLerp(lower.Value.threshold, upper.Value.threshold, targetWeight);
                else if (Mathf.Approximately(child.threshold, upper.Value.threshold))
                    weight = Mathf.InverseLerp(lower.Value.threshold, upper.Value.threshold, targetWeight);
            }
            else
            {
                weight = Mathf.Approximately(targetWeight, child.threshold) ? 1f : 0f;
            }
        }

        return weight;
    }

    AnimationClip GetAnimationClipFromMotion(Motion motion)
    {
        if (motion is AnimationClip clip) return clip;
        if (motion is BlendTree tree)
            return tree.children.Select(child => GetAnimationClipFromMotion(child.motion)).FirstOrDefault(c => c != null);
        return null;
    }

    ChildAnimatorState FindMatchingState(AnimatorStateMachine stateMachine, AnimationEventStateBehaviour stateBehaviour)
    {
        foreach (var state in stateMachine.states)
        {
            if (state.state.behaviours.Contains(stateBehaviour)) return state;
        }

        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            var result = FindMatchingState(subStateMachine.stateMachine, stateBehaviour);
            if (result.state != null) return result;
        }

        return default;
    }

    AnimatorController GetValidAnimatorController(out string errorMessage)
    {
        errorMessage = string.Empty;
        GameObject selected = Selection.activeGameObject;
        if (!selected)
        {
            errorMessage = "Select a GameObject with an Animator.";
            return null;
        }

        var animator = selected.GetComponent<Animator>();
        if (!animator)
        {
            errorMessage = "Selected GameObject lacks Animator.";
            return null;
        }

        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (!controller)
        {
            errorMessage = "AnimatorController is missing or invalid.";
            return null;
        }

        return controller;
    }

    bool Validate(AnimationEventStateBehaviour stateBehaviour, out string errorMessage)
    {
        AnimatorController controller = GetValidAnimatorController(out errorMessage);
        if (controller == null) return false;

        var matchingState = controller.layers
            .Select(layer => FindMatchingState(layer.stateMachine, stateBehaviour))
            .FirstOrDefault(state => state.state != null);

        previewClip = GetAnimationClipFromMotion(matchingState.state?.motion);
        return previewClip != null;
    }

    [MenuItem("GameObject/Enforce T-Pose", false, 0)]
    static void EnforceTPose()
    {
        GameObject selected = Selection.activeGameObject;
        if (!selected || !selected.TryGetComponent(out Animator animator) || !animator.avatar) return;

        SkeletonBone[] skeletonBones = animator.avatar.humanDescription.skeleton;

        foreach (HumanBodyBones hbb in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (hbb == HumanBodyBones.LastBone) continue;

            Transform bone = animator.GetBoneTransform(hbb);
            if (!bone) continue;

            SkeletonBone skelBone = skeletonBones.FirstOrDefault(sb => sb.name == bone.name);
            if (string.IsNullOrEmpty(skelBone.name)) continue;

            if (hbb == HumanBodyBones.Hips) bone.localPosition = skelBone.position;
            bone.localRotation = skelBone.rotation;
        }
    }
}
#endif
