using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Events;

public class RadialSelection : MonoBehaviour
{
    [Header("Input Configuration")]
    public OVRInput.Button spawnButton = OVRInput.Button.PrimaryIndexTrigger;

    [Header("Radial Menu Settings")]
    [Range(2, 10)]
    public int numberOfRadialPart = 4;
    public GameObject radialPartPrefab;
    public Transform radialPartCanvas;
    public float angleBetweenPart = 10;
    public Transform handTransform;

    [Header("Menu Position Settings")]
    public float spawnDistance = 0.25f;
    public float menuScale = 0.0008f;
    public Transform playerCamera;

    [Header("Scene Names")]
    public string menuSceneName = "MainMenu";
    public string tutorialSceneName = "Tutorial";

    [Header("Events")]
    public UnityEvent<int> OnPartSelected;

    private List<GameObject> spawnedParts = new List<GameObject>();
    private int currentSelectedRadialPart = -1;
    private bool isMenuActive = false;
    private Vector3 menuOffsetFromPlayer;

    void Start()
    {
        if (handTransform == null)
        {
            Debug.LogError("⚠️ Hand Transform no está asignado!");
        }

        if (radialPartCanvas == null)
        {
            Debug.LogError("⚠️ Radial Part Canvas no está asignado!");
        }

        if (radialPartPrefab == null)
        {
            Debug.LogError("⚠️ Radial Part Prefab no está asignado!");
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }

        if (radialPartCanvas != null)
        {
            radialPartCanvas.gameObject.SetActive(false);
            radialPartCanvas.localScale = Vector3.one * menuScale;
        }

        Debug.Log("✓ Menú radial configurado");
    }

    void Update()
    {
        if (handTransform == null) return;

        if (OVRInput.GetDown(spawnButton))
        {
            SpawnRadialPart();
        }

        if (isMenuActive)
        {
            UpdateMenuPositionRelativeToPlayer();

            if (OVRInput.Get(spawnButton))
            {
                GetSelectedRadialPart();
            }
        }

        if (OVRInput.GetUp(spawnButton) && isMenuActive)
        {
            HideAndTriggerSelected();
        }
    }

    private void UpdateMenuPositionRelativeToPlayer()
    {
        if (radialPartCanvas == null || playerCamera == null) return;

        Vector3 rotatedOffset = playerCamera.rotation * menuOffsetFromPlayer;
        radialPartCanvas.position = playerCamera.position + rotatedOffset;

        Vector3 lookDirection = radialPartCanvas.position - playerCamera.position;
        lookDirection.y = 0;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            radialPartCanvas.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    public void SpawnRadialPart()
    {
        if (radialPartCanvas == null || handTransform == null || playerCamera == null) return;

        isMenuActive = true;
        radialPartCanvas.gameObject.SetActive(true);

        Vector3 spawnPosition = handTransform.position + handTransform.forward * spawnDistance;
        radialPartCanvas.position = spawnPosition;

        menuOffsetFromPlayer = Quaternion.Inverse(playerCamera.rotation) * (spawnPosition - playerCamera.position);

        Vector3 lookDirection = radialPartCanvas.position - playerCamera.position;
        lookDirection.y = 0;

        if (lookDirection != Vector3.zero)
        {
            radialPartCanvas.rotation = Quaternion.LookRotation(lookDirection);
        }
        else
        {
            radialPartCanvas.rotation = Quaternion.identity;
        }

        // Limpiar segmentos anteriores
        foreach (var item in spawnedParts)
        {
            Destroy(item);
        }
        spawnedParts.Clear();

        // Crear los segmentos del menú radial
        for (int i = 0; i < numberOfRadialPart; i++)
        {
            float angle = -i * 360f / numberOfRadialPart;

            GameObject spawnedRadialPart = Instantiate(radialPartPrefab, radialPartCanvas);

            RectTransform segmentRect = spawnedRadialPart.GetComponent<RectTransform>();
            if (segmentRect != null)
            {
                segmentRect.localPosition = Vector3.zero;
                segmentRect.localRotation = Quaternion.Euler(0, 0, angle);
                segmentRect.localScale = Vector3.one;
            }

            Image image = spawnedRadialPart.GetComponent<Image>();
            if (image != null)
            {
                image.fillAmount = (1f / numberOfRadialPart) - (angleBetweenPart / 360f);
            }

            spawnedParts.Add(spawnedRadialPart);
        }

        Debug.Log($"✓ Menú creado con {numberOfRadialPart} segmentos");
        Debug.Log("  0=Arriba (Reiniciar), 1=Derecha (Menú), 2=Abajo (Tutorial), 3=Izquierda (Volver)");
    }

    public void GetSelectedRadialPart()
    {
        if (handTransform == null || radialPartCanvas == null) return;

        Vector3 centerToHand = handTransform.position - radialPartCanvas.position;
        Vector3 centerToHandProjected = Vector3.ProjectOnPlane(centerToHand, radialPartCanvas.forward);
        float angle = Vector3.SignedAngle(radialPartCanvas.up, centerToHandProjected, -radialPartCanvas.forward);

        if (angle < 0)
            angle += 360;

        currentSelectedRadialPart = (int)(angle * numberOfRadialPart / 360);

        // Resaltar el segmento seleccionado
        for (int i = 0; i < spawnedParts.Count; i++)
        {
            Image image = spawnedParts[i].GetComponent<Image>();
            if (image != null)
            {
                if (i == currentSelectedRadialPart)
                {
                    image.color = Color.yellow;
                    spawnedParts[i].transform.localScale = 1.1f * Vector3.one;
                }
                else
                {
                    image.color = Color.white;
                    spawnedParts[i].transform.localScale = Vector3.one;
                }
            }
        }
    }

    public void CloseMenu()
    {
        if (radialPartCanvas == null) return;

        radialPartCanvas.gameObject.SetActive(false);
        isMenuActive = false;
        Debug.Log("✓ Menú cerrado");
    }

    public void HideAndTriggerSelected()
    {
        if (radialPartCanvas == null) return;

        Debug.Log($"✓ Opción seleccionada: {currentSelectedRadialPart}");

        ExecuteButtonAction(currentSelectedRadialPart);
        OnPartSelected.Invoke(currentSelectedRadialPart);

        radialPartCanvas.gameObject.SetActive(false);
        isMenuActive = false;
    }

    private void ExecuteButtonAction(int buttonIndex)
    {
        // Segmentos: 0=Arriba, 1=Derecha, 2=Abajo, 3=Izquierda
        switch (buttonIndex)
        {
            case 0: // Arriba - Reiniciar
                RestartLevel();
                break;

            case 1: // Derecha - Menú
                LoadMainMenu();
                break;

            case 2: // Abajo - Tutorial
                LoadTutorial();
                break;

            case 3: // Izquierda - Volver
                CloseMenu();
                break;

            default:
                Debug.LogWarning($"⚠️ Índice inválido: {buttonIndex}");
                break;
        }
    }

    public void RestartLevel()
    {
        Debug.Log("🔄 Reiniciando nivel...");
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void LoadMainMenu()
    {
        Debug.Log($"🏠 Cargando menú: {menuSceneName}");
        if (!string.IsNullOrEmpty(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            Debug.LogError("⚠️ Nombre de escena de menú no configurado!");
        }
    }

    public void LoadTutorial()
    {
        Debug.Log($"📚 Cargando tutorial: {tutorialSceneName}");
        if (!string.IsNullOrEmpty(tutorialSceneName))
        {
            SceneManager.LoadScene(tutorialSceneName);
        }
        else
        {
            Debug.LogError("⚠️ Nombre de escena de tutorial no configurado!");
        }
    }
}