using UnityEngine;

public class PlayerCrouch : MonoBehaviour
{
    [Header("Configuración de Agacharse")]
    [Tooltip("Offset de altura cuando está de pie (0 = altura real del jugador)")]
    public float offsetAlturaStandard = 0f;

    [Tooltip("Cuánto bajar cuando se agacha (en metros)")]
    public float cantidadAgacharse = 0.7f;

    [Tooltip("Velocidad de transición al agacharse")]
    public float velocidadTransicion = 5f;

    [Header("Configuración de Altura Base")]
    [Tooltip("Altura base del jugador (ajusta esto para ser más bajo/alto)")]
    public float alturaBase = 0f;

    [Header("Referencias (Opcional - se asignan automáticamente)")]
    public Transform trackingSpace;
    public CharacterController characterController;

    // Estado interno
    private bool estaAgachado = false;
    private float offsetObjetivo;
    private float offsetActual;
    private float alturaCharacterControllerOriginal;
    private Vector3 centroCharacterControllerOriginal;

    void Start()
    {
        // Buscar TrackingSpace si no está asignado
        if (trackingSpace == null)
        {
            trackingSpace = transform.Find("TrackingSpace");

            if (trackingSpace == null)
            {
                // Intentar buscar por tipo
                OVRCameraRig cameraRig = GetComponentInChildren<OVRCameraRig>();
                if (cameraRig != null)
                {
                    trackingSpace = cameraRig.trackingSpace;
                }

                if (trackingSpace == null)
                {
                    Debug.LogError("No se encontró TrackingSpace. Asígnalo manualmente.");
                    enabled = false;
                    return;
                }
            }
        }

        // Buscar CharacterController
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        // Guardar valores originales del CharacterController
        if (characterController != null)
        {
            alturaCharacterControllerOriginal = characterController.height;
            centroCharacterControllerOriginal = characterController.center;
        }

        // Establecer altura inicial
        offsetActual = offsetAlturaStandard + alturaBase;
        offsetObjetivo = offsetAlturaStandard + alturaBase;

        // Aplicar altura base inmediatamente
        AplicarAltura(offsetActual);

        Debug.Log($"PlayerCrouch inicializado. Altura base: {alturaBase}m, Agacharse: {cantidadAgacharse}m");
    }

    void Update()
    {
        // Detectar el botón B del mando derecho
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            ToggleCrouch();
        }

        // Suavizar la transición
        if (Mathf.Abs(offsetActual - offsetObjetivo) > 0.001f)
        {
            offsetActual = Mathf.Lerp(offsetActual, offsetObjetivo, Time.deltaTime * velocidadTransicion);
            AplicarAltura(offsetActual);
        }
    }

    void ToggleCrouch()
    {
        estaAgachado = !estaAgachado;

        if (estaAgachado)
        {
            // Agacharse
            offsetObjetivo = offsetAlturaStandard + alturaBase - cantidadAgacharse;
            Debug.Log($"Agachándose - Nueva altura: {offsetObjetivo}m");
        }
        else
        {
            // Levantarse
            offsetObjetivo = offsetAlturaStandard + alturaBase;
            Debug.Log($"Levantándose - Nueva altura: {offsetObjetivo}m");
        }
    }

    void AplicarAltura(float offset)
    {
        if (trackingSpace == null) return;

        // Mover el TrackingSpace hacia arriba/abajo
        Vector3 nuevaPosicion = trackingSpace.localPosition;
        nuevaPosicion.y = offset;
        trackingSpace.localPosition = nuevaPosicion;

        // Ajustar CharacterController si existe
        if (characterController != null)
        {
            // Calcular nueva altura del collider
            float nuevaAltura = alturaCharacterControllerOriginal + offset;
            characterController.height = nuevaAltura;

            // Ajustar el centro del collider para que la base quede en el suelo
            Vector3 nuevoCentro = centroCharacterControllerOriginal;
            nuevoCentro.y = nuevaAltura / 2f;
            characterController.center = nuevoCentro;
        }
    }

    // Métodos públicos
    public bool EstaAgachado()
    {
        return estaAgachado;
    }

    public void Agacharse()
    {
        if (!estaAgachado)
        {
            ToggleCrouch();
        }
    }

    public void Levantarse()
    {
        if (estaAgachado)
        {
            ToggleCrouch();
        }
    }

    // Para visualizar en el editor
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || trackingSpace == null) return;

        // Dibujar línea de altura actual
        Vector3 posicion = transform.position;
        Gizmos.color = estaAgachado ? Color.yellow : Color.green;
        Gizmos.DrawLine(posicion, posicion + Vector3.up * (offsetActual + 1.7f));
    }
}

