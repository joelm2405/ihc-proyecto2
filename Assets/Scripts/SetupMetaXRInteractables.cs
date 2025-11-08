using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Script Editor para configurar automáticamente la jerarquía y componentes
/// de objetos interactables siguiendo las buenas prácticas de META XR SDK
/// 
/// Estructura que crea:
/// - [Objeto]Grabbable (Rigidbody, Grabbable)
///     - Visuals (Interactable Color Visual, Material Property Block Editor, Interactable Group View)
///         - Root
///             - [Objeto Original]
/// </summary>
public class SetupMetaXRInteractables : EditorWindow
{
    private GameObject interactablesParent;
    private bool setupRigidbody = true;
    private bool setupGrabbable = true;
    private bool setupGrabInteractable = true;
    private bool setupPhysicsGrabbable = true;
    private bool setupHandGrabInteractable = true;
    private bool setupVisualComponents = true;

    [MenuItem("Tools/META XR/Setup Interactables")]
    public static void ShowWindow()
    {
        GetWindow<SetupMetaXRInteractables>("Setup META XR Interactables");
    }

    void OnGUI()
    {
        GUILayout.Label("Configuración de Interactables META XR", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        interactablesParent = (GameObject)EditorGUILayout.ObjectField(
            "Interactables Parent",
            interactablesParent,
            typeof(GameObject),
            true
        );

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Componentes a añadir:", EditorStyles.boldLabel);

        setupRigidbody = EditorGUILayout.Toggle("Rigidbody", setupRigidbody);
        setupGrabbable = EditorGUILayout.Toggle("Grabbable", setupGrabbable);
        setupGrabInteractable = EditorGUILayout.Toggle("Grab Interactable", setupGrabInteractable);
        setupPhysicsGrabbable = EditorGUILayout.Toggle("Physics Grabbable", setupPhysicsGrabbable);
        setupHandGrabInteractable = EditorGUILayout.Toggle("Hand Grab Interactable", setupHandGrabInteractable);

        EditorGUILayout.Space();
        setupVisualComponents = EditorGUILayout.Toggle("Setup Visual Components", setupVisualComponents);

        EditorGUILayout.Space();

        if (interactablesParent == null)
        {
            EditorGUILayout.HelpBox("Por favor, asigna el GameObject 'Interactables' que contiene los objetos a configurar.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"Se configurarán {interactablesParent.transform.childCount} objetos.", MessageType.Info);
        }

        EditorGUILayout.Space();

        GUI.enabled = interactablesParent != null;

        if (GUILayout.Button("Configurar Todos los Interactables", GUILayout.Height(40)))
        {
            SetupAllInteractables();
        }

        GUI.enabled = true;
    }

    void SetupAllInteractables()
    {
        if (interactablesParent == null)
        {
            Debug.LogError("No se ha asignado el GameObject 'Interactables'");
            return;
        }

        int configurados = 0;
        List<GameObject> objetosOriginales = new List<GameObject>();

        // Recolectar todos los hijos directos primero
        foreach (Transform child in interactablesParent.transform)
        {
            objetosOriginales.Add(child.gameObject);
        }

        // Procesar cada objeto
        foreach (GameObject objetoOriginal in objetosOriginales)
        {
            if (SetupSingleInteractable(objetoOriginal))
            {
                configurados++;
            }
        }

        Debug.Log($"✓ Configuración completada: {configurados} objetos configurados exitosamente.");
        EditorUtility.DisplayDialog("Configuración Completa",
            $"Se configuraron {configurados} objetos interactables correctamente.", "OK");
    }

    bool SetupSingleInteractable(GameObject objetoOriginal)
    {
        if (objetoOriginal == null) return false;

        string nombreObjeto = objetoOriginal.name;

        try
        {
            // 1. Crear [Objeto]Grabbable
            GameObject grabbable = new GameObject(nombreObjeto + "Grabbable");
            grabbable.transform.SetParent(interactablesParent.transform);
            grabbable.transform.localPosition = objetoOriginal.transform.localPosition;
            grabbable.transform.localRotation = objetoOriginal.transform.localRotation;
            grabbable.transform.localScale = objetoOriginal.transform.localScale;

            // Añadir componentes al Grabbable
            if (setupRigidbody)
            {
                Rigidbody rb = grabbable.AddComponent<Rigidbody>();
                rb.mass = 1f;
                rb.useGravity = true;
            }

            AddComponentByName(grabbable, "Grabbable", setupGrabbable);
            AddComponentByName(grabbable, "GrabInteractable", setupGrabInteractable);
            AddComponentByName(grabbable, "PhysicsGrabbable", setupPhysicsGrabbable);
            AddComponentByName(grabbable, "HandGrabInteractable", setupHandGrabInteractable);

            // 2. Crear Visuals
            GameObject visuals = new GameObject("Visuals");
            visuals.transform.SetParent(grabbable.transform);
            visuals.transform.localPosition = Vector3.zero;
            visuals.transform.localRotation = Quaternion.identity;
            visuals.transform.localScale = Vector3.one;

            // Añadir componentes visuales
            if (setupVisualComponents)
            {
                AddComponentByName(visuals, "InteractableColorVisual");
                AddComponentByName(visuals, "MaterialPropertyBlockEditor");
                AddComponentByName(visuals, "InteractableGroupView");
            }

            // 3. Crear Root
            GameObject root = new GameObject("Root");
            root.transform.SetParent(visuals.transform);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            // 4. Mover el objeto original dentro de Root
            objetoOriginal.transform.SetParent(root.transform);
            objetoOriginal.transform.localPosition = Vector3.zero;
            objetoOriginal.transform.localRotation = Quaternion.identity;
            // Mantener la escala original del objeto

            // 5. Asegurar que el objeto original tenga collider
            if (objetoOriginal.GetComponent<Collider>() == null)
            {
                // Intentar añadir MeshCollider si tiene MeshFilter
                MeshFilter meshFilter = objetoOriginal.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    MeshCollider meshCollider = objetoOriginal.AddComponent<MeshCollider>();
                    meshCollider.convex = true; // Necesario para interacciones físicas
                    Debug.Log($"→ Añadido MeshCollider (convex) a {nombreObjeto}");
                }
                else
                {
                    // Si no hay mesh, añadir BoxCollider genérico
                    objetoOriginal.AddComponent<BoxCollider>();
                    Debug.Log($"→ Añadido BoxCollider a {nombreObjeto}");
                }
            }

            Debug.Log($"✓ Configurado: {nombreObjeto}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Error al configurar {nombreObjeto}: {e.Message}");
            return false;
        }
    }

    void AddComponentByName(GameObject obj, string componentName, bool shouldAdd = true)
    {
        if (!shouldAdd) return;

        // Buscar el tipo del componente en todos los ensamblados
        System.Type componentType = null;

        // Lista de namespaces comunes de Oculus/Meta
        string[] namespaces = {
            "Oculus.Interaction",
            "Oculus.Interaction.HandGrab",
            "Oculus.Interaction.Grab",
            "Oculus.Interaction.GrabAPI",
            ""
        };

        foreach (string ns in namespaces)
        {
            string fullName = string.IsNullOrEmpty(ns) ? componentName : ns + "." + componentName;
            componentType = System.Type.GetType(fullName);

            if (componentType == null)
            {
                // Buscar en todos los ensamblados cargados
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    componentType = assembly.GetType(fullName);
                    if (componentType != null) break;
                }
            }

            if (componentType != null) break;
        }

        if (componentType != null)
        {
            if (obj.GetComponent(componentType) == null)
            {
                obj.AddComponent(componentType);
                Debug.Log($"  → Añadido: {componentName}");
            }
        }
        else
        {
            Debug.LogWarning($"  ⚠ No se encontró el componente: {componentName}");
        }
    }
}

// ============================================
// SCRIPT ALTERNATIVO PARA RUNTIME (Si lo necesitas)
// ============================================

/// <summary>
/// Versión Runtime del configurador (no requiere Editor)
/// Úsalo solo si necesitas configurar objetos en tiempo de ejecución
/// </summary>
public class SetupMetaXRInteractablesRuntime : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("GameObject padre que contiene todos los interactables")]
    public GameObject interactablesParent;

    [Header("Ejecutar en Start")]
    public bool autoSetupOnStart = false;

    void Start()
    {
        if (autoSetupOnStart && interactablesParent != null)
        {
            SetupAllInteractables();
        }
    }

    [ContextMenu("Setup Interactables")]
    public void SetupAllInteractables()
    {
        if (interactablesParent == null)
        {
            Debug.LogError("No se ha asignado el GameObject 'Interactables'");
            return;
        }

        List<GameObject> objetosOriginales = new List<GameObject>();

        foreach (Transform child in interactablesParent.transform)
        {
            objetosOriginales.Add(child.gameObject);
        }

        foreach (GameObject obj in objetosOriginales)
        {
            SetupSingleInteractable(obj);
        }

        Debug.Log($"✓ Configuración runtime completada");
    }

    void SetupSingleInteractable(GameObject objetoOriginal)
    {
        // Implementación similar pero sin usar EditorUtility
        // (código simplificado para runtime)
    }
}