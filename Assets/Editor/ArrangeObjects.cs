using UnityEngine;
using UnityEditor;
public class ArrangeObjects : EditorWindow
{
    public GameObject mySelection;

    // For Arange Objectes for Editing
    [MenuItem("Tools/ArrangeObjects")]//By Romy R Michael
    public static void ShowWindow() {
        GetWindow<ArrangeObjects>("Arrange Order");
    }
    private void OnGUI()
    {
        GUILayout.Label("Select Object and Click Order buttons!", EditorStyles.boldLabel);
        if (GUILayout.Button("Send to Top"))
        {
            SendToTop();
        }

        if (GUILayout.Button("Send to Bottom"))
        {
            SendToBottom();
        }
    }
    private static void SendToTop()
    {
        //Debug.Log("You clicked on " + Selection.activeGameObject.transform.GetSiblingIndex());
        Selection.activeGameObject.transform.parent = null;
        Selection.activeGameObject.transform.SetAsFirstSibling();
    }private static void SendToBottom()
    {
        //Debug.Log("You clicked on " + Selection.activeGameObject.transform.GetSiblingIndex());
        Selection.activeGameObject.transform.parent = null;
        Selection.activeGameObject.transform.SetAsLastSibling();
    }
}
