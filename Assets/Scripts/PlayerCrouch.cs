using UnityEngine;

public class PlayerCrouch : MonoBehaviour
{
    [Header("Configuración de Agacharse")]
    [Tooltip("Altura de la cámara cuando está de pie")]
    public float alturaStandard = 1.7f;

    [Tooltip("Altura de la cámara cuando está agachado")]
    public float alturaAgachado = 1.0f;

    [Tooltip("Velocidad de transición al agacharse")]
    public float velocidadTransicion = 5f;

    [Header("Referencias")]
    [Tooltip("Arrastra aquí el CenterEyeAnchor (la cámara)")]
    public Transform cameraTransform;

    [Tooltip("Arrastra aquí el CharacterController si lo tienes")]
    public CharacterController characterController;

    // Estado interno
    private bool estaAgachado = false;
    private float alturaObjetivo;
    private Vector3 posicionCamaraInicial;

    void Start()
    {
        // Si no asignaste la cámara manualmente, buscarla
        if (cameraTransform == null)
        {
            // Buscar el CenterEyeAnchor
            cameraTransform = transform.Find("TrackingSpace/CenterEyeAnchor");

            if (cameraTransform == null)
            {
                Debug.LogError("No se encontró CenterEyeAnchor. Asígnalo manualmente en el Inspector.");
            }
        }

        // Si no asignaste el CharacterController, buscarlo
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        // Guardar altura inicial
        if (cameraTransform != null)
        {
            posicionCamaraInicial = cameraTransform.localPosition;
            alturaObjetivo = alturaStandard;
        }
    }

    void Update()
    {
        // Detectar el botón B del mando derecho (OVRInput.Button.Two)
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            ToggleCrouch();
        }

        // Suavizar la transición de altura
        SuavizarAltura();
    }

    void ToggleCrouch()
    {
        estaAgachado = !estaAgachado;

        if (estaAgachado)
        {
            // Agacharse
            alturaObjetivo = alturaAgachado;
            Debug.Log("Agachándose");
        }
        else
        {
            // Levantarse
            alturaObjetivo = alturaStandard;
            Debug.Log("Levantándose");
        }
    }

    void SuavizarAltura()
    {
        if (cameraTransform == null) return;

        // Calcular la nueva altura con interpolación suave
        float alturaActual = cameraTransform.localPosition.y;
        float nuevaAltura = Mathf.Lerp(alturaActual, alturaObjetivo, Time.deltaTime * velocidadTransicion);

        // Aplicar la nueva altura
        Vector3 nuevaPosicion = cameraTransform.localPosition;
        nuevaPosicion.y = nuevaAltura;
        cameraTransform.localPosition = nuevaPosicion;

        // Si tienes CharacterController, ajustar su altura también
        if (characterController != null)
        {
            float diferencia = alturaStandard - alturaAgachado;
            characterController.height = alturaStandard - (alturaStandard - nuevaAltura);

            // Ajustar el centro del collider
            Vector3 centro = characterController.center;
            centro.y = nuevaAltura / 2;
            characterController.center = centro;
        }
    }

    // Método público por si quieres llamarlo desde otro script
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
}