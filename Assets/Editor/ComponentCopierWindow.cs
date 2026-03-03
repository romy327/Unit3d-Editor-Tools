using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ComponentCopierWindow : EditorWindow
{
    private GameObject sourceObject;
    private GameObject targetObject;
    private List<Component> sourceComponents;
    private List<Component> componentsToCopy;
    private Vector2 scrollPos;

    [MenuItem("Tools/Component Copier")]
    public static void ShowWindow()
    {
        GetWindow<ComponentCopierWindow>("Component Copier");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Component Copier", EditorStyles.boldLabel);
        GUILayout.Space(10);

        sourceObject = (GameObject)EditorGUILayout.ObjectField("Source Object", sourceObject, typeof(GameObject), true);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);

        GUILayout.Space(10);

        if (GUILayout.Button("Load Components"))
        {
            LoadComponents();
        }

        if (sourceComponents != null && sourceComponents.Count > 0)
        {
            GUILayout.Label("Select Components to Copy", EditorStyles.boldLabel);

            GUILayout.Space(10);

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            foreach (var component in sourceComponents)
            {
                bool isSelected = componentsToCopy.Contains(component);
                bool newIsSelected = EditorGUILayout.ToggleLeft(component.GetType().Name, isSelected);

                if (newIsSelected && !isSelected)
                {
                    componentsToCopy.Add(component);
                }
                else if (!newIsSelected && isSelected)
                {
                    componentsToCopy.Remove(component);
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Copy Selected Components"))
            {
                CopyComponents();
            }
        }
    }

    private void LoadComponents()
    {
        if (sourceObject == null)
        {
            Debug.LogWarning("Source object is not selected");
            return;
        }

        sourceComponents = new List<Component>();
        componentsToCopy = new List<Component>();

        // Load components from source object and its children
        LoadComponentsRecursive(sourceObject);
    }

    private void LoadComponentsRecursive(GameObject obj)
    {
        // Get all components except Transform, MeshFilter, and MeshRenderer
        var components = obj.GetComponents<Component>().Where(c => !(c is Transform || c is MeshFilter || c is MeshRenderer)).ToList();
        sourceComponents.AddRange(components);

        // Recursively load components from children
        foreach (Transform child in obj.transform)
        {
            LoadComponentsRecursive(child.gameObject);
        }
    }

    private void CopyComponents()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Target object is not selected");
            return;
        }

        // Copy components from source object and its children to target object and its children
        CopyComponentsRecursive(sourceObject, targetObject);
    }

    private void CopyComponentsRecursive(GameObject source, GameObject target)
    {
        if (source == null || target == null)
        {
            Debug.LogWarning("Source or target is null");
            return;
        }

        // Copy the selected components from the source to the target
        foreach (var component in source.GetComponents<Component>())
        {
            if (componentsToCopy.Contains(component))
            {
                CopyComponent(component, target);
            }
        }

        // Recursively copy components for each child
        for (int i = 0; i < source.transform.childCount; i++)
        {
            GameObject sourceChild = source.transform.GetChild(i).gameObject;
            GameObject targetChild = target.transform.childCount > i ? target.transform.GetChild(i).gameObject : null;

            if (targetChild != null)
            {
                CopyComponentsRecursive(sourceChild, targetChild);
            }
        }
    }

    private void CopyComponent(Component original, GameObject destination)
    {
        Type type = original.GetType();
        Component copy = destination.AddComponent(type);

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (field.Name != "name") // Exclude the name field
            {
                field.SetValue(copy, field.GetValue(original));
            }
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (prop.CanWrite && prop.Name != "name")// Exclude the name property
            {
                prop.SetValue(copy, prop.GetValue(original, null), null);
            }
        }
    }
}
