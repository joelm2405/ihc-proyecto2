using UnityEngine;

/// <summary>
/// Sistema completo de movimiento y agacharse para OVRCameraRig con interacciones
/// Compatible con el sistema de interacción de Meta
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class OVRPlayerMovementAndCrouch : MonoBehaviour
{
    [Header("=== MOVIMIENTO ===")]
    [Tooltip("Velocidad de movimiento en m/s")]
    public float velocidadMovimiento = 3f;

    [Tooltip("Velocidad de rotación en grados/s")]
    public float velocidadRotacion = 90f;

    [Tooltip("Usar rotación snap (30 grados) en vez de suave")]
    public bool rotacionSnap = true;

    [Tooltip("Ángulo de rotación snap")]
    public float anguloSnap = 30f;

    [Header("=== AGACHARSE ===")]
    [Tooltip("Cuánto bajar cuando se agacha (en metros)")]
    public float cantidadAgacharse = 0.7f;

    [Tooltip("Velocidad de transición al agacharse")]
    public float velocidadTransicion = 5f;

    [Header("=== ALTURA BASE ===")]
    [Tooltip("Offset de altura base del jugador")]
    public float alturaBase = 0f;

    [Header("=== GRAVEDAD ===")]
    [Tooltip("Aplicar gravedad al jugador")]
    public bool aplicarGravedad = true;

    [Tooltip("Fuerza de gravedad")]
    public float fuerzaGravedad = 9.81f;

    [Header("=== REFERENCIAS ===")]
    public Transform trackingSpace;
    public Transform centerEyeAnchor;

    // Componentes
    private CharacterController characterController;

    // Estado de agacharse
    private bool estaAgachado = false;
    private float offsetObjetivo;
    private float offsetActual;
    private float alturaCharacterControllerOriginal;
    private Vector3 centroCharacterControllerOriginal;

    // Estado de movimiento
    private float rotacionPendiente = 0f;
    private bool rotacionEnProgreso = false;
    private Vector3 velocidadVertical = Vector3.zero;

    void Start()
    {
        // Obtener CharacterController
        characterController = GetComponent<CharacterController>();

        // Buscar OVRCameraRig automáticamente
        OVRCameraRig cameraRig = GetComponentInChildren<OVRCameraRig>();

        if (cameraRig == null)
        {
            Debug.LogError("No se encontró OVRCameraRig como hijo. Este script debe estar en el padre del OVRCameraRig.");
            enabled = false;
            return;
        }

        // Asignar referencias automáticamente
        if (trackingSpace == null)
            trackingSpace = cameraRig.trackingSpace;

        if (centerEyeAnchor == null)
            centerEyeAnchor = cameraRig.centerEyeAnchor;

        // Guardar valores originales del CharacterController
        alturaCharacterControllerOriginal = characterController.height;
        centroCharacterControllerOriginal = characterController.center;

        // Establecer altura inicial
        offsetActual = alturaBase;
        offsetObjetivo = alturaBase;

        AplicarAltura(offsetActual);

        Debug.Log($"OVRPlayerMovementAndCrouch inicializado. Altura base: {alturaBase}m");
    }

    void Update()
    {
        // === AGACHARSE ===
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            ToggleCrouch();
        }

        // Suavizar transición de agacharse
        if (Mathf.Abs(offsetActual - offsetObjetivo) > 0.001f)
        {
            offsetActual = Mathf.Lerp(offsetActual, offsetObjetivo, Time.deltaTime * velocidadTransicion);
            AplicarAltura(offsetActual);
        }

        // === MOVIMIENTO ===
        ProcesarMovimiento();

        // === ROTACIÓN ===
        ProcesarRotacion();

        // === GRAVEDAD ===
        if (aplicarGravedad)
        {
            AplicarGravedad();
        }
    }

    void ProcesarMovimiento()
    {
        // Obtener input del joystick izquierdo
        Vector2 input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        if (input.magnitude > 0.1f)
        {
            // Dirección basada en donde mira la cámara (solo rotación Y)
            Vector3 direccionMirada = centerEyeAnchor.forward;
            direccionMirada.y = 0;
            direccionMirada.Normalize();

            Vector3 direccionDerecha = centerEyeAnchor.right;
            direccionDerecha.y = 0;
            direccionDerecha.Normalize();

            // Calcular movimiento
            Vector3 movimiento = (direccionMirada * input.y + direccionDerecha * input.x);
            movimiento *= velocidadMovimiento * Time.deltaTime;

            // Aplicar movimiento
            characterController.Move(movimiento);
        }
    }

    void ProcesarRotacion()
    {
        // Obtener input del joystick derecho (eje X para rotación)
        float inputRotacion = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x;

        if (rotacionSnap)
        {
            // Rotación snap (como la mayoría de juegos VR)
            if (!rotacionEnProgreso && Mathf.Abs(inputRotacion) > 0.75f)
            {
                rotacionEnProgreso = true;
                float rotacion = inputRotacion > 0 ? anguloSnap : -anguloSnap;
                transform.RotateAround(centerEyeAnchor.position, Vector3.up, rotacion);
            }
            else if (Mathf.Abs(inputRotacion) < 0.3f)
            {
                rotacionEnProgreso = false;
            }
        }
        else
        {
            // Rotación suave
            if (Mathf.Abs(inputRotacion) > 0.1f)
            {
                float rotacion = inputRotacion * velocidadRotacion * Time.deltaTime;
                transform.RotateAround(centerEyeAnchor.position, Vector3.up, rotacion);
            }
        }
    }

    void AplicarGravedad()
    {
        if (characterController.isGrounded)
        {
            velocidadVertical.y = -0.5f; // Pequeña fuerza para mantener en el suelo
        }
        else
        {
            velocidadVertical.y -= fuerzaGravedad * Time.deltaTime;
        }

        characterController.Move(velocidadVertical * Time.deltaTime);
    }

    void ToggleCrouch()
    {
        estaAgachado = !estaAgachado;

        if (estaAgachado)
        {
            offsetObjetivo = alturaBase - cantidadAgacharse;
            Debug.Log($"Agachándose - Nueva altura: {offsetObjetivo}m");
        }
        else
        {
            offsetObjetivo = alturaBase;
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

        // Ajustar CharacterController
        float nuevaAltura = alturaCharacterControllerOriginal + offset;
        characterController.height = nuevaAltura;

        Vector3 nuevoCentro = centroCharacterControllerOriginal;
        nuevoCentro.y = nuevaAltura / 2f;
        characterController.center = nuevoCentro;
    }

    // === MÉTODOS PÚBLICOS ===
    public bool EstaAgachado() => estaAgachado;

    public void Agacharse()
    {
        if (!estaAgachado) ToggleCrouch();
    }

    public void Levantarse()
    {
        if (estaAgachado) ToggleCrouch();
    }

    public void SetVelocidadMovimiento(float velocidad)
    {
        velocidadMovimiento = velocidad;
    }

    // === VISUALIZACIÓN ===
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || trackingSpace == null) return;

        Vector3 posicion = transform.position;
        Gizmos.color = estaAgachado ? Color.yellow : Color.green;
        Gizmos.DrawLine(posicion, posicion + Vector3.up * (offsetActual + 1.7f));

        // Visualizar dirección de movimiento
        if (centerEyeAnchor != null)
        {
            Gizmos.color = Color.blue;
            Vector3 direccion = centerEyeAnchor.forward;
            direccion.y = 0;
            Gizmos.DrawRay(centerEyeAnchor.position, direccion * 0.5f);
        }
    }
}