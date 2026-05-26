using UnityEditor;
using UnityEngine;

public class ReplaceMeshColliderWithBoxCollider : EditorWindow
{
    [MenuItem("Tools/Replace Mesh Collider with Box Collider")]
    private static void Init()
    {
        ReplaceMeshColliderWithBoxCollider window = (ReplaceMeshColliderWithBoxCollider)EditorWindow.GetWindow(typeof(ReplaceMeshColliderWithBoxCollider));
        window.Show();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Replace Mesh to Box Collider"))
        {
            ReplaceMeshToBoxCollider();
        }
        if (GUILayout.Button("Replace Box to Mesh Collider"))
        {
            ReplaceBoxToMeshCollider();
        }
        if (GUILayout.Button("Remove Colliders"))
        {
            RemoveSelectedColliders();
        }
    }

    private void ReplaceMeshToBoxCollider()
    {
        // Get selected game objects in the scene
        GameObject[] selectedObjects = Selection.gameObjects;

        foreach (GameObject selectedObject in selectedObjects)
        {
            // Remove existing MeshCollider if it exists
            MeshCollider meshCollider = selectedObject.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                DestroyImmediate(meshCollider);
            }

            // Add BoxCollider component
            BoxCollider boxCollider = selectedObject.AddComponent<BoxCollider>();

            // Optionally adjust the size of the BoxCollider to match the object's bounds
            Renderer renderer = selectedObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                boxCollider.size = renderer.bounds.size;
            }
        }

        Debug.Log("Collider replacement complete.");
    }
    private void ReplaceBoxToMeshCollider()
    {
        // Get selected game objects in the scene
        GameObject[] selectedObjects = Selection.gameObjects;

        foreach (GameObject selectedObject in selectedObjects)
        {
            // Remove existing BoxCollider if it exists
            BoxCollider boxCollider = selectedObject.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                DestroyImmediate(boxCollider);
            }

            // Add MeshCollider component
            MeshCollider meshCollider = selectedObject.AddComponent<MeshCollider>();

            // Optionally adjust the convex property of the MeshCollider
            // For complex meshes, set meshCollider.convex = false;

            // Optionally assign a custom mesh to the MeshCollider
            // meshCollider.sharedMesh = customMesh;
        }

        Debug.Log("Collider replacement complete.");
    }
    private void RemoveSelectedColliders()
    {
        // Get selected game objects in the scene
        GameObject[] selectedObjects = Selection.gameObjects;

        foreach (GameObject selectedObject in selectedObjects)
        {
            // Remove all collider components
            Collider[] colliders = selectedObject.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                DestroyImmediate(collider);
            }
        }

        Debug.Log("Colliders removed from selected objects.");
    }

}
