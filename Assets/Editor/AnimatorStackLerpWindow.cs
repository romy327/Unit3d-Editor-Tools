using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class AnimatorStackLerpWindow : EditorWindow
{
    private List<AnimatorEntry> animators = new List<AnimatorEntry>();
    private float animationDuration = 1.0f;

    [MenuItem("Tools/Animator Stack Lerp")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorStackLerpWindow>("Animator Stack Lerp");
    }

    private void OnGUI()
    {
        GUILayout.Label("Animator Stack Animation", EditorStyles.boldLabel);

        animationDuration = EditorGUILayout.FloatField("Animation Duration", animationDuration);

        if (GUILayout.Button("Add Selected Animator"))
        {
            foreach (GameObject obj in Selection.gameObjects)
            {
                Animator animator = obj.GetComponent<Animator>();
                if (animator != null && !animators.Exists(a => a.animator == animator))
                {
                    string blendParameter = FindBlendParameter(animator);
                    if (!string.IsNullOrEmpty(blendParameter))
                    {
                        animators.Add(new AnimatorEntry { animator = animator, blendParameter = blendParameter });
                    }
                    else
                    {
                        Debug.LogWarning($"No float blend parameter found in {animator.name}.");
                    }
                }
            }
        }

        if (GUILayout.Button("Clear Animators"))
        {
            animators.Clear();
        }

        GUILayout.Label("Animators in Stack:");
        foreach (var entry in animators)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(entry.animator.name);
            entry.blendParameter = EditorGUILayout.TextField("Blend Parameter", entry.blendParameter);
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Start Animation Sequence"))
        {
            if (Application.isPlaying)
            {
                AnimatorStackRunner.RunAnimationSequence(animators, animationDuration);
            }
            else
            {
                Debug.LogWarning("You need to be in Play mode to run the animation sequence.");
            }
        }
    }

    private string FindBlendParameter(Animator animator)
    {
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Float)
            {
                return param.name;
            }
        }
        return null;
    }

    [System.Serializable]
    public class AnimatorEntry
    {
        public Animator animator;
        public string blendParameter;
    }
}

public class AnimatorStackRunner : MonoBehaviour
{
    public static void RunAnimationSequence(List<AnimatorStackLerpWindow.AnimatorEntry> animators, float animationDuration)
    {
        GameObject runnerObject = new GameObject("AnimatorStackRunner");
        AnimatorStackRunner runner = runnerObject.AddComponent<AnimatorStackRunner>();
        runner.StartCoroutine(runner.AnimateSequence(animators, animationDuration));
    }

    private IEnumerator AnimateSequence(List<AnimatorStackLerpWindow.AnimatorEntry> animators, float animationDuration)
    {
        foreach (var entry in animators)
        {
            if (entry.animator != null && entry.animator.HasParameter(entry.blendParameter))
            {
                Debug.Log($"Animating {entry.animator.name} with parameter '{entry.blendParameter}'");

                float elapsedTime = 0f;
                while (elapsedTime < animationDuration)
                {
                    float blendValue = Mathf.Lerp(0f, 1f, elapsedTime / animationDuration);
                    entry.animator.SetFloat(entry.blendParameter, blendValue);
                    entry.animator.Update(0);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                // Ensure the final value is set to 1
                entry.animator.SetFloat(entry.blendParameter, 1f);
                entry.animator.Update(0);

                yield return new WaitForSeconds(0.1f); // Short delay to ensure transition completes
            }
            else
            {
                Debug.LogWarning($"Animator '{entry.animator?.name}' does not have the parameter '{entry.blendParameter}'.");
            }
        }
        Debug.Log("Animation sequence completed.");
        Destroy(gameObject);
    }
}

public static class AnimatorExtensions
{
    public static bool HasParameter(this Animator animator, string paramName)
    {
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
}
