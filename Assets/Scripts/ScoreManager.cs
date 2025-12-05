using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager I { get; private set; }

    [Header("Puntaje")]
    public float score = 0f;
    public float minScore = 0f;
    public float maxScore = 100f;

    [Header("Tasas (puntos/segundo)")]
    public float safeRate = 10f;      // + por segundo
    public float dangerRate = -25f;   // - por segundo
    public float neutralRate = 0f;    // 0 (o -1 si quieres decay suave)

    [Header("Proteccion de Terremoto")]
    [Tooltip("Solo permitir cambios de score durante el terremoto")]
    public bool soloContarDuranteTerremoto = true;

    [Tooltip("Referencia al EarthquakeManager")]
    public EarthquakeHybrid earthquakeManager;

    [Header("Debug")]
    public bool mostrarDebug = true;

    public event Action<float> OnScoreChanged;

    private bool ultimoEstadoPermitido = false;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    void Start()
    {
        // Buscar EarthquakeManager si no esta asignado
        if (earthquakeManager == null)
        {
            earthquakeManager = FindObjectOfType<EarthquakeHybrid>();

            if (earthquakeManager != null && mostrarDebug)
            {
                Debug.Log("[ScoreManager] EarthquakeManager encontrado automaticamente");
            }
        }
    }

    void Update()
    {
        // Log cuando cambia el estado de permiso
        bool permitido = PuedeContarPuntos();

        if (permitido != ultimoEstadoPermitido && mostrarDebug)
        {
            Debug.Log($"<color=yellow>[ScoreManager] Contar puntos: {(permitido ? "PERMITIDO" : "BLOQUEADO")}</color>");
            ultimoEstadoPermitido = permitido;
        }
    }

    bool PuedeContarPuntos()
    {
        // Si no esta activada la proteccion, siempre permitir
        if (!soloContarDuranteTerremoto)
            return true;

        // Si no hay earthquakeManager, no permitir
        if (earthquakeManager == null)
            return false;

        // Solo permitir si el terremoto esta activo
        return earthquakeManager.EstaTerremotoActivo();
    }

    public void AddOverTime(float ratePerSecond)
    {
        // PROTECCION: Solo contar si el terremoto esta activo
        if (!PuedeContarPuntos())
        {
            if (mostrarDebug && Time.frameCount % 120 == 0)
            {
                Debug.Log($"<color=red>[ScoreManager] BLOQUEADO: Intento de sumar {ratePerSecond} puntos/seg pero terremoto NO esta activo</color>");
            }
            return;
        }

        float prev = score;
        score += ratePerSecond * Time.deltaTime;
        score = Mathf.Clamp(score, minScore, maxScore);

        if (!Mathf.Approximately(score, prev))
        {
            OnScoreChanged?.Invoke(score);

            // Log ocasional
            if (mostrarDebug && Time.frameCount % 120 == 0)
            {
                string accion = ratePerSecond > 0 ? "SUMANDO" : "RESTANDO";
                Debug.Log($"<color=green>[ScoreManager] {accion} {Mathf.Abs(ratePerSecond)} pts/seg | Score: {score:F1}</color>");
            }
        }
    }

    public void SetScore(float value)
    {
        float prev = score;
        score = Mathf.Clamp(value, minScore, maxScore);

        if (!Mathf.Approximately(score, prev))
        {
            OnScoreChanged?.Invoke(score);
        }
    }

    // Metodo para forzar reset
    public void ResetScore()
    {
        SetScore(0f);
        if (mostrarDebug)
        {
            Debug.Log("[ScoreManager] Score reseteado a 0");
        }
    }

    public void AddPoints(float points)
{
    // Sumamos los puntos manualmente y aseguramos que el puntaje no exceda el rango máximo.
    score += points;
    score = Mathf.Clamp(score, minScore, maxScore);

    // Notificamos el cambio de puntaje.
    OnScoreChanged?.Invoke(score);

    if (mostrarDebug)
    {
        Debug.Log($"<color=green>[ScoreManager] Puntos sumados: {points} | Score: {score:F1}</color>");
    }
}

}