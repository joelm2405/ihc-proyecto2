using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class ZoneArea : MonoBehaviour
{
    public enum ZoneType { Safe, Danger, Blue } // Añadido Blue para la zona azul

    [Header("Configuracion de Zona")]
    public ZoneType type = ZoneType.Safe;
    public string zoneName = "SafeZone_A";

    [Header("Referencias")]
    public EarthquakeHybrid earthquakeManager;

    [Header("Debug")]
    public bool mostrarDebug = true;

    // Variables privadas
    private bool jugadorDentro = false;
    private bool ultimoEstadoTerremoto = false;
    private bool puntosSumados = false; // Para asegurarse de que solo se sumen los puntos una vez

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

        if (mostrarDebug)
        {
            Debug.Log($"[{zoneName}] Inicializado. Tipo: {type}");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificar si la mochila entra en la zona
        if (EsJugador(other) || other.CompareTag("Mochila"))
        {
            jugadorDentro = true;

            if (mostrarDebug)
            {
                Debug.Log($"<color=cyan>[{zoneName}] >>> Objetos ENTRO <<<</color>");
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
                Debug.Log($"<color=cyan>[{zoneName}] <<< Objetos SALIERON >>></color>");
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

        // Log cuando cambia el estado del terremoto
        if (terremotoActivo != ultimoEstadoTerremoto && mostrarDebug)
        {
            Debug.Log($"<color=yellow>[{zoneName}] TERREMOTO cambio a: {(terremotoActivo ? "ACTIVO" : "INACTIVO")}</color>");
            ultimoEstadoTerremoto = terremotoActivo;
        }

        // Si es la zona azul, sumar 50 puntos solo una vez
        if (jugadorDentro && terremotoActivo && type == ZoneType.Blue && !puntosSumados)
        {
            ScoreManager.I.SetScore(ScoreManager.I.score + 50f);  // Sumar 50 puntos
            puntosSumados = true;  // Asegurar que solo se sumen una vez
            if (mostrarDebug)
            {
                Debug.Log($"<color=green>[{zoneName}] Suma de 50 puntos!</color>");
            }
        }
        else if (jugadorDentro && terremotoActivo && type != ZoneType.Blue)
        {
            // Aplicar puntos en zonas normales (Segura o de Peligro)
            float tasa = type == ZoneType.Safe ? ScoreManager.I.safeRate : ScoreManager.I.dangerRate;
            ScoreManager.I.AddOverTime(tasa);
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
        if (Application.isPlaying && earthquakeManager != null)
        {
            terremotoActivo = earthquakeManager.EstaTerremotoActivo();
        }
        
        Color color;
        
        // Contando puntos: muy visible
        if (jugadorDentro && terremotoActivo)
        {
            color = type == ZoneType.Safe ? new Color(0f, 1f, 0f, 0.9f) : type == ZoneType.Danger ? new Color(1f, 0f, 0f, 0.9f) : new Color(0f, 0f, 1f, 0.9f); // Zona Azul es azul
        }
        // Jugador dentro pero terremoto inactivo: amarillo
        else if (jugadorDentro)
        {
            color = new Color(1f, 1f, 0f, 0.5f);
        }
        // Fuera: transparente
        else
        {
            color = type == ZoneType.Safe ? new Color(0f, 1f, 0f, 0.2f) : type == ZoneType.Danger ? new Color(1f, 0f, 0f, 0.2f) : new Color(0f, 0f, 1f, 0.2f); // Zona Azul es azul
        }
        
        Gizmos.color = color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        
        // Dibujar un cubo sólido cuando esté contando
        if (jugadorDentro && terremotoActivo)
        {
            color.a = 0.3f;
            Gizmos.color = color;
            Gizmos.DrawCube(boxCol.center, boxCol.size);
        }
    }
#endif
}
