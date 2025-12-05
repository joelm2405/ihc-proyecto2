using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class ZoneArea : MonoBehaviour
{
    public enum ZoneType { Safe, Danger }

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
        // Buscar por multiples formas
        if (EsJugador(other))
        {
            jugadorDentro = true;

            if (mostrarDebug)
            {
                Debug.Log($"<color=cyan>[{zoneName}] >>> Jugador ENTRO <<<</color>");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (EsJugador(other))
        {
            jugadorDentro = false;

            if (mostrarDebug)
            {
                Debug.Log($"<color=cyan>[{zoneName}] <<< Jugador SALIO >>></color>");
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

        // REGLA SIMPLE: Solo aplicar puntos si AMBAS condiciones son verdaderas
        if (jugadorDentro && terremotoActivo)
        {
            // Aplicar puntos
            float tasa = type == ZoneType.Safe ? ScoreManager.I.safeRate : ScoreManager.I.dangerRate;
            ScoreManager.I.AddOverTime(tasa);

            // Log ocasional (cada 2 segundos aprox)
            if (mostrarDebug && Time.frameCount % 120 == 0)
            {
                string accion = type == ZoneType.Safe ? "SUMANDO" : "RESTANDO";
                Debug.Log($"<color=green>[{zoneName}] {accion} puntos (tasa: {tasa})</color>");
            }
        }
        else
        {
            // NO hacer nada - no aplicar tasas
            // Log solo cuando hay jugador pero terremoto no está activo
            if (jugadorDentro && !terremotoActivo && mostrarDebug && Time.frameCount % 120 == 0)
            {
                Debug.Log($"<color=orange>[{zoneName}] Jugador dentro pero terremoto NO activo - NO se cuentan puntos</color>");
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
        if (Application.isPlaying && earthquakeManager != null)
        {
            terremotoActivo = earthquakeManager.EstaTerremotoActivo();
        }
        
        Color color;
        
        // Contando puntos: muy visible
        if (jugadorDentro && terremotoActivo)
        {
            color = type == ZoneType.Safe ? new Color(0f, 1f, 0f, 0.9f) : new Color(1f, 0f, 0f, 0.9f);
        }
        // Jugador dentro pero terremoto inactivo: amarillo
        else if (jugadorDentro)
        {
            color = new Color(1f, 1f, 0f, 0.5f);
        }
        // Fuera: transparente
        else
        {
            color = type == ZoneType.Safe ? new Color(0f, 1f, 0f, 0.2f) : new Color(1f, 0f, 0f, 0.2f);
        }
        
        Gizmos.color = color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        
        // Dibujar un cubo sólido cuando está contando
        if (jugadorDentro && terremotoActivo)
        {
            color.a = 0.3f;
            Gizmos.color = color;
            Gizmos.DrawCube(boxCol.center, boxCol.size);
        }
    }
#endif
}