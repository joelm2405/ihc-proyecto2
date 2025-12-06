using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class ZoneArea : MonoBehaviour
{
    public enum ZoneType { Safe, Danger, Blue }

    [Header("Configuracion de Zona")]
    public ZoneType type = ZoneType.Safe;
    public string zoneName = "SafeZone_A";

    [Header("Referencias")]
    public EarthquakeHybrid earthquakeManager;

    [Header("Zona Azul - Objetos a mostrar")]
    [Tooltip("Objetos que apareceran cuando entres a la zona azul (solo si type = Blue)")]
    public GameObject[] objetosAMostrar = new GameObject[3];

    [Header("Zona Azul - Visualizacion")]
    [Tooltip("Renderer de la zona azul para ocultar/mostrar")]
    public Renderer rendererZonaAzul;

    [Header("Debug")]
    public bool mostrarDebug = true;

    // Variables privadas
    private bool jugadorDentro = false;
    private bool ultimoEstadoTerremoto = false;
    private bool puntosSumados = false;
    private bool objetosMostrados = false;
    private bool terremotoHaFinalizado = false;

    void Reset()
    {
        var c = GetComponent<BoxCollider>();
        c.isTrigger = true;
    }

    void Start()
    {
        // Buscar el EarthquakeManager
        if (earthquakeManager == null)
        {
            earthquakeManager = FindObjectOfType<EarthquakeHybrid>();
        }

        if (earthquakeManager == null)
        {
            Debug.LogError($"[{zoneName}] ERROR: No se encontro EarthquakeHybrid!");
            enabled = false;
            return;
        }

        // Buscar renderer automaticamente si es zona azul y no esta asignado
        if (type == ZoneType.Blue && rendererZonaAzul == null)
        {
            rendererZonaAzul = GetComponentInChildren<Renderer>();
            if (rendererZonaAzul != null && mostrarDebug)
            {
                Debug.Log($"[{zoneName}] Renderer encontrado automaticamente");
            }
        }

        // Si es zona azul, ocultar al inicio
        if (type == ZoneType.Blue)
        {
            OcultarZonaAzul();
            OcultarObjetosAMostrar();
        }

        if (mostrarDebug)
        {
            Debug.Log($"[{zoneName}] Inicializado. Tipo: {type}");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificar si el jugador o mochila entra en la zona
        if (EsJugador(other) || other.CompareTag("Mochila"))
        {
            jugadorDentro = true;

            if (mostrarDebug)
            {
                Debug.Log($"<color=cyan>[{zoneName}] >>> Objeto ENTRO <<<</color>");
            }

            // Si es zona azul y el terremoto ha finalizado, mostrar objetos
            if (type == ZoneType.Blue && terremotoHaFinalizado && !objetosMostrados)
            {
                MostrarObjetosAMostrar();
                objetosMostrados = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (EsJugador(other) || other.CompareTag("Mochila"))
        {
            jugadorDentro = false;

            if (mostrarDebug)
            {
                Debug.Log($"<color=cyan>[{zoneName}] <<< Objeto SALIO >>></color>");
            }
        }
    }

    bool EsJugador(Collider other)
    {
        // Multiples formas de detectar al jugador
        return other.CompareTag("Player") ||
               other.name.Contains("OVR") ||
               other.name.Contains("Camera") ||
               other.name.Contains("Player") ||
               other.GetComponent<WASDProxy>() != null ||
               other.GetComponentInParent<WASDProxy>() != null;
    }

    void Update()
    {
        if (earthquakeManager == null || ScoreManager.I == null)
            return;

        // Obtener estado actual del terremoto
        bool terremotoActivo = earthquakeManager.EstaTerremotoActivo();
        bool terremotoFinalizado = earthquakeManager.TerremotoHaFinalizado();

        // Log cuando cambia el estado del terremoto
        if (terremotoActivo != ultimoEstadoTerremoto && mostrarDebug)
        {
            Debug.Log($"<color=yellow>[{zoneName}] TERREMOTO cambio a: {(terremotoActivo ? "ACTIVO" : "INACTIVO")}</color>");
            ultimoEstadoTerremoto = terremotoActivo;
        }

        // Detectar cuando el terremoto finaliza (para zona azul)
        if (terremotoFinalizado && !terremotoHaFinalizado)
        {
            terremotoHaFinalizado = true;

            // Si es zona azul, mostrarla cuando termine el terremoto
            if (type == ZoneType.Blue)
            {
                MostrarZonaAzul();

                if (mostrarDebug)
                {
                    Debug.Log($"<color=blue>[{zoneName}] Zona azul VISIBLE - Terremoto finalizado</color>");
                }
            }
        }

        // === LOGICA DE PUNTOS ===

        // Zona azul: sumar 50 puntos solo una vez cuando entre
        if (jugadorDentro && terremotoFinalizado && type == ZoneType.Blue && !puntosSumados)
        {
            ScoreManager.I.SetScore(ScoreManager.I.score + 50f);
            puntosSumados = true;

            if (mostrarDebug)
            {
                Debug.Log($"<color=green>[{zoneName}] +50 puntos por entrar a zona azul!</color>");
            }
        }
        // Zonas normales: sumar/restar durante terremoto
        else if (jugadorDentro && terremotoActivo && type != ZoneType.Blue)
        {
            float tasa = type == ZoneType.Safe ? ScoreManager.I.safeRate : ScoreManager.I.dangerRate;
            ScoreManager.I.AddOverTime(tasa);

            if (mostrarDebug && Time.frameCount % 120 == 0)
            {
                string accion = type == ZoneType.Safe ? "SUMANDO" : "RESTANDO";
                Debug.Log($"<color=green>[{zoneName}] {accion} puntos</color>");
            }
        }
    }

    void OcultarZonaAzul()
    {
        if (rendererZonaAzul != null)
        {
            rendererZonaAzul.enabled = false;
        }
    }

    void MostrarZonaAzul()
    {
        if (rendererZonaAzul != null)
        {
            rendererZonaAzul.enabled = true;
        }
    }

    void OcultarObjetosAMostrar()
    {
        foreach (var obj in objetosAMostrar)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

    void MostrarObjetosAMostrar()
    {
        foreach (var obj in objetosAMostrar)
        {
            if (obj != null)
            {
                obj.SetActive(true);

                if (mostrarDebug)
                {
                    Debug.Log($"<color=blue>[{zoneName}] Mostrando objeto: {obj.name}</color>");
                }
            }
        }
    }

    // Metodos publicos
    public bool EstaJugadorDentro() => jugadorDentro;

    public bool EstaContandoPuntos()
    {
        if (earthquakeManager == null) return false;
        return jugadorDentro && earthquakeManager.EstaTerremotoActivo();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var boxCol = GetComponent<BoxCollider>();
        if (boxCol == null) return;
        
        bool terremotoActivo = false;
        bool terremotoFinalizado = false;
        
        if (Application.isPlaying && earthquakeManager != null)
        {
            terremotoActivo = earthquakeManager.EstaTerremotoActivo();
            terremotoFinalizado = earthquakeManager.TerremotoHaFinalizado();
        }
        
        Color color;
        
        // Si es zona azul y el terremoto no ha finalizado, hacerla muy transparente
        if (type == ZoneType.Blue && !terremotoFinalizado)
        {
            color = new Color(0f, 0f, 1f, 0.1f); // Casi invisible
        }
        // Contando puntos: muy visible
        else if (jugadorDentro && (terremotoActivo || (type == ZoneType.Blue && terremotoFinalizado)))
        {
            if (type == ZoneType.Safe)
                color = new Color(0f, 1f, 0f, 0.9f);
            else if (type == ZoneType.Danger)
                color = new Color(1f, 0f, 0f, 0.9f);
            else // Blue
                color = new Color(0f, 0.5f, 1f, 0.9f);
        }
        // Jugador dentro pero terremoto inactivo: amarillo
        else if (jugadorDentro)
        {
            color = new Color(1f, 1f, 0f, 0.5f);
        }
        // Fuera: transparente
        else
        {
            if (type == ZoneType.Safe)
                color = new Color(0f, 1f, 0f, 0.2f);
            else if (type == ZoneType.Danger)
                color = new Color(1f, 0f, 0f, 0.2f);
            else // Blue
                color = new Color(0f, 0.5f, 1f, 0.3f);
        }
        
        Gizmos.color = color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        
        // Dibujar un cubo sólido cuando este contando
        if (jugadorDentro && (terremotoActivo || (type == ZoneType.Blue && terremotoFinalizado)))
        {
            color.a = 0.3f;
            Gizmos.color = color;
            Gizmos.DrawCube(boxCol.center, boxCol.size);
        }
    }
#endif
}