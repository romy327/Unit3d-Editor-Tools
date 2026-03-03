using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ComponentLister : EditorWindow
{
    [MenuItem("Tools/List Components on Selected GameObject")]
    public static void ListComponent()
    {
        GameObject selected = Selection.activeGameObject;

        if(selected == null)
        {
            Debug.LogWarning("No GameObject Selected");
            return;
        }

        Component[] components =  selected.GetComponents<Component>();
        List<string> componentsNames = new List<string>();

        foreach(Component comp in components)
        {
            if (comp == null) continue;

            componentsNames.Add(comp.GetType().ToString());
        }

        Debug.Log($"Components on '{selected.name}':\n" + string.Join("\n", componentsNames));

    }

}
