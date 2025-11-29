using UnityEngine;
using UnityEngine.SceneManagement;

public class WristMenuController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject wristMenuUI;
    [SerializeField] private Transform leftController;
    [SerializeField] private Vector3 menuOffset = new Vector3(0, 0.05f, 0.1f);
    [SerializeField] private Vector3 menuRotationOffset = new Vector3(0, 0, 0); // Ajustar según necesites

    [Header("Configuración")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private float smoothFollow = 10f;

    [Header("Botón de Activación")]
    [SerializeField] private OVRInput.Button activationButton = OVRInput.Button.Three;
    [SerializeField] private OVRInput.Controller controllerHand = OVRInput.Controller.LTouch;

    private bool isMenuActive = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private void Start()
    {
        Debug.Log("=== WristMenuController INICIADO ===");

        if (wristMenuUI != null)
        {
            wristMenuUI.SetActive(false);
            Debug.Log("✓ Menú UI encontrado y desactivado");
        }
        else
        {
            Debug.LogError("✗ ERROR: wristMenuUI NO está asignado en el Inspector!");
        }

        if (leftController == null)
        {
            Debug.Log("Buscando LeftHandAnchor automáticamente...");
            GameObject ovrRig = GameObject.Find("OVRCameraRig");
            if (ovrRig != null)
            {
                Transform trackingSpace = ovrRig.transform.Find("TrackingSpace");
                if (trackingSpace != null)
                {
                    leftController = trackingSpace.Find("LeftHandAnchor");
                    if (leftController != null)
                    {
                        Debug.Log("✓ LeftHandAnchor encontrado automáticamente");
                    }
                }
            }

            if (leftController == null)
            {
                Debug.LogError("✗ ERROR: No se encontró el LeftHandAnchor.");
            }
        }
    }

    private void Update()
    {
        // Detectar presión del botón
        if (OVRInput.GetDown(activationButton, controllerHand))
        {
            Debug.Log($"¡Botón {activationButton} presionado!");
            ToggleMenu();
        }

        // Seguir la posición del mando
        if (isMenuActive && wristMenuUI != null && leftController != null)
        {
            UpdateMenuPosition();
        }
    }

    public void ToggleMenu()
    {
        isMenuActive = !isMenuActive;
        Debug.Log($"Menú {(isMenuActive ? "ABIERTO" : "CERRADO")}");

        if (wristMenuUI != null)
        {
            wristMenuUI.SetActive(isMenuActive);
            if (isMenuActive)
            {
                UpdateMenuPosition();
            }
        }
    }

    private void UpdateMenuPosition()
    {
        if (leftController == null) return;

        // Calcular posición objetivo
        targetPosition = leftController.position + leftController.TransformDirection(menuOffset);

        // Calcular rotación mirando hacia la cámara
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 lookDirection = mainCam.transform.position - targetPosition;

            if (lookDirection != Vector3.zero)
            {
                // Rotación base mirando a la cámara
                targetRotation = Quaternion.LookRotation(lookDirection);

                // IMPORTANTE: Aplicar rotación adicional para corregir la inversión
                targetRotation *= Quaternion.Euler(menuRotationOffset);
            }
        }

        // Aplicar suavizado
        wristMenuUI.transform.position = Vector3.Lerp(
            wristMenuUI.transform.position,
            targetPosition,
            Time.deltaTime * smoothFollow
        );

        wristMenuUI.transform.rotation = Quaternion.Slerp(
            wristMenuUI.transform.rotation,
            targetRotation,
            Time.deltaTime * smoothFollow
        );
    }

    // Métodos para botones
    public void RestartScene()
    {
        Debug.Log("Reiniciando escena...");
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void ReturnToMainMenu()
    {
        Debug.Log($"Volviendo al menú: {menuSceneName}");
        SceneManager.LoadScene(menuSceneName);
    }

    public void ResumeGame()
    {
        Debug.Log("Reanudando juego...");
        ToggleMenu();
    }
}