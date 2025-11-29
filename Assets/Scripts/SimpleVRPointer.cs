using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Script simple para crear un rayo láser desde el mando derecho
/// y permitir hacer clic en botones UI con el trigger
/// Agregar este script al RightHandAnchor
/// </summary>
public class SimpleVRPointer : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private Color rayColor = Color.cyan;
    [SerializeField] private float rayWidth = 0.005f;
    [SerializeField] private OVRInput.Button clickButton = OVRInput.Button.PrimaryIndexTrigger; // Trigger derecho

    private LineRenderer lineRenderer;
    private EventSystem eventSystem;

    void Start()
    {
        // Crear o obtener Line Renderer
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            ConfigureLineRenderer();
        }

        // Buscar EventSystem
        eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("No hay EventSystem en la escena. Crea uno con OVRInputModule");
        }

        Debug.Log("✓ SimpleVRPointer iniciado en: " + gameObject.name);
    }

    void ConfigureLineRenderer()
    {
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth * 0.5f;
        lineRenderer.positionCount = 2;

        // Intentar múltiples shaders hasta encontrar uno válido
        Shader lineShader = Shader.Find("Unlit/Color");
        if (lineShader == null) lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null) lineShader = Shader.Find("UI/Default");
        if (lineShader == null) lineShader = Shader.Find("Standard");

        Material lineMat = new Material(lineShader);
        lineMat.color = rayColor;
        lineRenderer.material = lineMat;

        lineRenderer.startColor = rayColor;
        lineRenderer.endColor = rayColor;
        lineRenderer.enabled = true;

        // Configuraciones adicionales para que se vea como línea
        lineRenderer.useWorldSpace = true;
        lineRenderer.alignment = LineAlignment.View;

        Debug.Log("✓ Line Renderer configurado con shader: " + lineShader.name);
    }

    void Update()
    {
        UpdateLaser();
        CheckUIInteraction();
    }

    void UpdateLaser()
    {
        // Posición inicial del rayo
        Vector3 startPos = transform.position;
        Vector3 direction = transform.forward;
        Vector3 endPos = startPos + direction * rayLength;

        // Raycast para ver si golpea algo
        RaycastHit hit;
        if (Physics.Raycast(startPos, direction, out hit, rayLength))
        {
            endPos = hit.point;

            // Cambiar color si apunta a UI
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                lineRenderer.startColor = Color.green;
                lineRenderer.endColor = Color.green;
            }
            else
            {
                lineRenderer.startColor = rayColor;
                lineRenderer.endColor = rayColor;
            }
        }
        else
        {
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = rayColor;
        }

        // Actualizar posiciones del Line Renderer
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);
    }

    void CheckUIInteraction()
    {
        // Detectar si se presiona el trigger
        if (OVRInput.GetDown(clickButton, OVRInput.Controller.RTouch))
        {
            Debug.Log("🎯 Trigger presionado - Intentando hacer clic en UI");

            // Realizar raycast hacia UI
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayLength))
            {
                Debug.Log($"Rayo golpeó: {hit.collider.gameObject.name}");

                // Intentar ejecutar el botón
                ExecuteEvents.Execute(
                    hit.collider.gameObject,
                    new PointerEventData(eventSystem),
                    ExecuteEvents.pointerClickHandler
                );
            }
        }
    }

    // Para debug - mostrar el rayo en Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * rayLength);
    }
}