using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AddMeshCollidersToSceneObjects : EditorWindow
{
    private bool convexEnabled = false;
    private bool showLog = true;

    private string[] targetObjectNames = new string[]
    {
        "Global Wall Plugs",
        "Bathrooms",
        "Hallways",
        "Laundry Room",
        "Bedrooms",
        "Global Light Switches",
        "Study",
        "Living Area"
    };

    [MenuItem("Tools/Add Mesh Colliders to Scene Objects")]
    public static void ShowWindow()
    {
        GetWindow<AddMeshCollidersToSceneObjects>("Add Mesh Colliders");
    }

    void OnGUI()
    {
        GUILayout.Label("Configuración de Mesh Colliders", EditorStyles.boldLabel);

        convexEnabled = EditorGUILayout.Toggle("Convex", convexEnabled);
        showLog = EditorGUILayout.Toggle("Mostrar Log Detallado", showLog);

        EditorGUILayout.Space(10);

        GUILayout.Label("Objetos de la escena a procesar:", EditorStyles.boldLabel);
        foreach (var objName in targetObjectNames)
        {
            EditorGUILayout.LabelField("• " + objName);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Añadir Mesh Colliders a Todos los Objetos", GUILayout.Height(40)))
        {
            ProcessAllSceneObjects();
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Este script añadirá Mesh Colliders a todos los GameObjects con MeshFilter/MeshRenderer " +
            "dentro de los objetos especificados en la escena actual.\n\n" +
            "CONVEX: Marca esta opción si los objetos necesitan física (Rigidbody). " +
            "Para objetos estáticos, déjalo desmarcado.",
            MessageType.Info
        );
    }

    void ProcessAllSceneObjects()
    {
        int totalProcessed = 0;
        int totalModified = 0;

        foreach (var objectName in targetObjectNames)
        {
            // Buscar todos los GameObjects con ese nombre en la escena
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj.name == objectName)
                {
                    if (showLog)
                    {
                        Debug.Log($"Procesando objeto padre: {obj.name}");
                    }

                    // Procesar este objeto y todos sus hijos
                    int modified = ProcessObjectAndChildren(obj);
                    totalModified += modified;
                    totalProcessed++;
                }
            }
        }

        string message = $"Proceso completado!\n\n" +
                        $"Objetos padres procesados: {totalProcessed}\n" +
                        $"GameObjects con MeshCollider añadido/actualizado: {totalModified}";

        EditorUtility.DisplayDialog("Completado", message, "OK");
        Debug.Log(message);
    }

    int ProcessObjectAndChildren(GameObject parentObject)
    {
        int modifiedCount = 0;

        // Obtener todos los MeshFilters del objeto y sus hijos
        MeshFilter[] meshFilters = parentObject.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter meshFilter in meshFilters)
        {
            GameObject obj = meshFilter.gameObject;

            // Verificar si ya tiene un MeshCollider
            MeshCollider existingCollider = obj.GetComponent<MeshCollider>();

            if (existingCollider == null)
            {
                // Añadir MeshCollider
                MeshCollider newCollider = Undo.AddComponent<MeshCollider>(obj);
                newCollider.convex = convexEnabled;

                modifiedCount++;

                if (showLog)
                {
                    Debug.Log($"✓ MeshCollider añadido a: {GetGameObjectPath(obj)}");
                }
            }
            else
            {
                // Actualizar convex si ya existe
                if (existingCollider.convex != convexEnabled)
                {
                    Undo.RecordObject(existingCollider, "Update MeshCollider Convex");
                    existingCollider.convex = convexEnabled;
                    modifiedCount++;

                    if (showLog)
                    {
                        Debug.Log($"↻ MeshCollider actualizado en: {GetGameObjectPath(obj)}");
                    }
                }
            }
        }

        return modifiedCount;
    }

    string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}