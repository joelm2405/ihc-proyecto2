using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Gestiona la UI del sistema de terremoto VR
/// UI fija en el visor del jugador (HUD)
/// </summary>
public class EarthquakeUIManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Referencia al script EarthquakeHybrid")]
    public EarthquakeHybrid earthquakeManager;

    [Header("UI Elements")]
    [Tooltip("Canvas principal")]
    public Canvas mainCanvas;

    [Tooltip("Panel de fondo")]
    public GameObject panelBackground;

    [Tooltip("Imagen del panel (para cambiar transparencia)")]
    public UnityEngine.UI.Image panelImage;

    [Tooltip("Texto del mensaje principal")]
    public TextMeshProUGUI textoMensaje;

    [Tooltip("Texto del contador")]
    public TextMeshProUGUI textoContador;

    [Tooltip("Texto de zona segura (se muestra durante terremoto)")]
    public TextMeshProUGUI textoZonaSegura;

    [Header("Configuración")]
    [Tooltip("Transform de CenterEyeAnchor para UI fija")]
    public Transform centerEyeTransform;

    [Header("Posición del Canvas")]
    [Tooltip("Altura del canvas (Y) - Positivo = arriba, Negativo = abajo")]
    [Range(-1f, 1f)]
    public float alturaCanvas = 0.2f;

    [Tooltip("Profundidad del canvas (Z) - Qué tan lejos está")]
    [Range(1f, 5f)]
    public float profundidadCanvas = 2f;

    [Tooltip("Desplazamiento horizontal (X)")]
    [Range(-1f, 1f)]
    public float desplazamientoHorizontal = 0f;

    [Tooltip("Escala del canvas - Ajusta el tamaño general")]
    [Range(0.0005f, 0.003f)]
    public float escalaCanvas = 0.001f;

    [Header("Colores")]
    public Color colorAdvertencia = new Color(1f, 0.8f, 0f, 1f); // Amarillo
    public Color colorPeligro = new Color(1f, 0.3f, 0.1f, 1f);   // Rojo-naranja
    public Color colorSeguro = new Color(0.2f, 0.8f, 1f, 1f);    // Azul claro
    public Color colorNormal = Color.white;

    [Header("Efectos")]
    [Tooltip("Activar parpadeo del texto durante terremoto")]
    public bool activarParpadeo = true;

    [Range(0.3f, 2f)]
    public float velocidadParpadeo = 0.8f;

    // Referencias privadas
    private bool uiInicializada = false;
    private bool mostrandoAdvertencia = false;
    private bool mostrandoTerremoto = false;
    private float tiempoInicioJuego = 0f;

    // Estados
    private enum EstadoUI
    {
        Oculto,
        Advertencia,
        Terremoto,
        Finalizado
    }

    private EstadoUI estadoActual = EstadoUI.Oculto;

    void Start()
    {
        InicializarUI();
        StartCoroutine(ActualizarUI());
    }

    void InicializarUI()
    {
        // Buscar CenterEyeAnchor si no está asignado
        if (centerEyeTransform == null)
        {
            GameObject ovrCameraRig = GameObject.Find("OVRCameraRig");
            if (ovrCameraRig != null)
            {
                Transform trackingSpace = ovrCameraRig.transform.Find("TrackingSpace");
                if (trackingSpace != null)
                {
                    centerEyeTransform = trackingSpace.Find("CenterEyeAnchor");
                    if (centerEyeTransform != null)
                    {
                        Debug.Log("CenterEyeAnchor encontrado automaticamente");
                    }
                }
            }
        }

        if (centerEyeTransform == null)
        {
            Debug.LogError("No se encontro CenterEyeAnchor! Asignalo manualmente");
            return;
        }

        // Buscar EarthquakeManager si no está asignado
        if (earthquakeManager == null)
        {
            earthquakeManager = FindObjectOfType<EarthquakeHybrid>();
            if (earthquakeManager == null)
            {
                Debug.LogError("No se encontro EarthquakeHybrid en la escena");
                return;
            }
        }

        // Hacer que el Canvas sea hijo de CenterEyeAnchor para UI fija
        if (mainCanvas != null)
        {
            mainCanvas.transform.SetParent(centerEyeTransform);
            mainCanvas.renderMode = RenderMode.WorldSpace;

            // Aplicar posición inicial
            ActualizarPosicionCanvas();
        }

        // Buscar el componente Image del panel si no está asignado
        if (panelBackground != null && panelImage == null)
        {
            panelImage = panelBackground.GetComponent<UnityEngine.UI.Image>();
        }

        // Ocultar todo al inicio
        OcultarTodo();

        // Guardar tiempo de inicio del juego
        tiempoInicioJuego = Time.time;

        uiInicializada = true;
        Debug.Log("EarthquakeUIManager inicializado - UI fija en visor");
    }

    IEnumerator ActualizarUI()
    {
        while (true)
        {
            if (!uiInicializada)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            // Actualizar posición del canvas cada frame
            ActualizarPosicionCanvas();

            // Determinar estado actual
            if (earthquakeManager.TerremotoHaFinalizado())
            {
                if (estadoActual != EstadoUI.Finalizado)
                {
                    MostrarFinalizacion();
                }
            }
            else if (earthquakeManager.EstaTerremotoActivo())
            {
                if (estadoActual != EstadoUI.Terremoto)
                {
                    MostrarTerremoto();
                }
                ActualizarContadorTerremoto();
            }
            else
            {
                if (estadoActual != EstadoUI.Advertencia)
                {
                    MostrarAdvertencia();
                }
                ActualizarContadorAdvertencia();
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    void ActualizarPosicionCanvas()
    {
        if (mainCanvas == null) return;

        // Aplicar posición y escala configurables
        mainCanvas.transform.localPosition = new Vector3(desplazamientoHorizontal, alturaCanvas, profundidadCanvas);
        mainCanvas.transform.localRotation = Quaternion.identity;
        mainCanvas.transform.localScale = Vector3.one * escalaCanvas;
    }

    void MostrarAdvertencia()
    {
        estadoActual = EstadoUI.Advertencia;
        mostrandoAdvertencia = true;
        mostrandoTerremoto = false;

        // Restaurar panel visible
        if (panelBackground != null) panelBackground.SetActive(true);
        if (panelImage != null)
        {
            Color colorVisible = panelImage.color;
            colorVisible.a = 0.7f; // Semi-transparente
            panelImage.color = colorVisible;
        }

        if (textoMensaje != null)
        {
            textoMensaje.gameObject.SetActive(true);
            textoMensaje.text = "ALERTA SISMICA\n\n" +
                               "Un terremoto esta por comenzar\n\n" +
                               "Dirijase a las ZONAS SEGURAS\n" +
                               "(Marcos de puertas o debajo de mesas resistentes)";
            textoMensaje.color = colorAdvertencia;
        }

        if (textoContador != null) textoContador.gameObject.SetActive(true);
        if (textoZonaSegura != null) textoZonaSegura.gameObject.SetActive(false);

        Debug.Log("Mostrando advertencia de terremoto");
    }

    void MostrarTerremoto()
    {
        estadoActual = EstadoUI.Terremoto;
        mostrandoAdvertencia = false;
        mostrandoTerremoto = true;

        // Hacer el panel TRANSPARENTE (no ocultarlo)
        if (panelImage != null)
        {
            Color colorTransparente = panelImage.color;
            colorTransparente.a = 0f; // Transparente
            panelImage.color = colorTransparente;
        }

        // Ocultar textos, dejar panel activo pero invisible
        if (panelBackground != null) panelBackground.SetActive(true);
        if (textoMensaje != null) textoMensaje.gameObject.SetActive(false);
        if (textoZonaSegura != null) textoZonaSegura.gameObject.SetActive(false);

        // SOLO el contador visible (sin fondo)
        if (textoContador != null)
        {
            textoContador.gameObject.SetActive(true);
            textoContador.color = colorPeligro;
        }

        Debug.Log("Mostrando UI de terremoto activo - Solo contador");
    }

    void MostrarFinalizacion()
    {
        estadoActual = EstadoUI.Finalizado;
        mostrandoTerremoto = false;
        mostrandoAdvertencia = false;

        // Restaurar panel visible
        if (panelBackground != null) panelBackground.SetActive(true);
        if (panelImage != null)
        {
            Color colorVisible = panelImage.color;
            colorVisible.a = 0.7f; // Semi-transparente
            panelImage.color = colorVisible;
        }

        if (textoMensaje != null)
        {
            textoMensaje.gameObject.SetActive(true);
            textoMensaje.text = "TERREMOTO FINALIZADO";
            textoMensaje.color = colorSeguro;
        }

        if (textoZonaSegura != null)
        {
            textoZonaSegura.gameObject.SetActive(true);
            textoZonaSegura.text = "Agarra la mochila de emergencia\ny dirijete a la salida\npara acabar la simulacion";
            textoZonaSegura.color = colorSeguro;
        }

        if (textoContador != null)
        {
            textoContador.gameObject.SetActive(false);
        }

        Debug.Log("Mostrando mensaje de finalizacion");

        // Opcional: ocultar después de un tiempo
        StartCoroutine(OcultarDespuesDeTiempo(10f));
    }

    void ActualizarContadorAdvertencia()
    {
        if (textoContador == null || !mostrandoAdvertencia) return;

        // Calcular tiempo desde que inicio el juego
        float tiempoTranscurridoDesdeInicio = Time.time - tiempoInicioJuego;

        // Tiempo que falta para que inicie el terremoto
        float tiempoRestante = earthquakeManager.tiempoEsperaInicio - tiempoTranscurridoDesdeInicio;

        if (tiempoRestante <= 0)
        {
            textoContador.text = "INICIANDO";
            textoContador.color = colorPeligro;
            return;
        }

        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);

        textoContador.text = string.Format("El terremoto comenzara en:\n\n<size=120>{0:00}:{1:00}</size>", minutos, segundos);
        textoContador.color = tiempoRestante <= 5f ? colorPeligro : colorAdvertencia;
    }

    void ActualizarContadorTerremoto()
    {
        if (textoContador == null || !mostrandoTerremoto) return;

        float tiempoRestante = earthquakeManager.GetTiempoRestante();

        if (tiempoRestante <= 0)
        {
            textoContador.text = "Finalizando...";
            return;
        }

        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);

        textoContador.text = string.Format("Duracion restante:\n\n<size=120>{0:00}:{1:00}</size>", minutos, segundos);
        textoContador.color = colorPeligro;
    }

    IEnumerator ParpadearTexto()
    {
        if (textoMensaje == null) yield break;

        while (mostrandoTerremoto && estadoActual == EstadoUI.Terremoto)
        {
            // Fade out
            float alpha = 1f;
            while (alpha > 0.4f && mostrandoTerremoto)
            {
                alpha -= Time.deltaTime * velocidadParpadeo;
                Color color = textoMensaje.color;
                color.a = alpha;
                textoMensaje.color = color;
                yield return null;
            }

            // Fade in
            while (alpha < 1f && mostrandoTerremoto)
            {
                alpha += Time.deltaTime * velocidadParpadeo;
                Color color = textoMensaje.color;
                color.a = alpha;
                textoMensaje.color = color;
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }

        // Restaurar alpha completo
        if (textoMensaje != null)
        {
            Color color = textoMensaje.color;
            color.a = 1f;
            textoMensaje.color = color;
        }
    }

    IEnumerator OcultarDespuesDeTiempo(float tiempo)
    {
        yield return new WaitForSeconds(tiempo);

        float duracion = 2f;
        float elapsed = 0f;

        CanvasGroup canvasGroup = mainCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = mainCanvas.gameObject.AddComponent<CanvasGroup>();
        }

        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duracion);
            yield return null;
        }

        OcultarTodo();
        canvasGroup.alpha = 1f;
    }

    void OcultarTodo()
    {
        if (panelBackground != null) panelBackground.SetActive(false);
        if (textoMensaje != null) textoMensaje.gameObject.SetActive(false);
        if (textoContador != null) textoContador.gameObject.SetActive(false);
        if (textoZonaSegura != null) textoZonaSegura.gameObject.SetActive(false);
    }

    // Métodos públicos para testing
    public void ForzarMostrarAdvertencia()
    {
        MostrarAdvertencia();
    }

    public void ForzarMostrarTerremoto()
    {
        MostrarTerremoto();
    }

    public void ForzarMostrarFinalizacion()
    {
        MostrarFinalizacion();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-buscar referencias en el editor
        if (mainCanvas == null)
        {
            mainCanvas = GetComponentInChildren<Canvas>();
        }
    }
#endif
}