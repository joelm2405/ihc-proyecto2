using UnityEngine;
using TMPro;

public class ScoreTMPHUD : MonoBehaviour
{
    [Header("HUD Durante el Juego (Menu1)")]
    public TMP_Text labelHUD;

    [Header("Pantalla de Finalización (Menu2)")]
    public TMP_Text labelFinalizacion;

    [TextArea(3, 5)]
    public string mensajeFinalizacion = "Felicidades, haz completado el simulador con el siguiente puntaje: {puntaje} pts";

    void Reset()
    {
        if (!labelHUD) labelHUD = GetComponent<TMP_Text>();
    }

    void Start()
    {
        // Si hay un label de finalización, actualizarlo al iniciar
        if (labelFinalizacion != null)
        {
            MostrarPuntajeFinal();
        }
    }

    void OnEnable()
    {
        // Actualizar cuando se active el objeto
        if (labelFinalizacion != null)
        {
            MostrarPuntajeFinal();
        }
    }

    void LateUpdate()
    {
        var sm = ScoreManager.I;
        if (sm == null) return;

        // Actualizar el HUD durante el juego (Menu1)
        if (labelHUD != null)
        {
            labelHUD.text = $"Puntos: {sm.score:0}";
        }

        // Actualizar el texto de finalización (Menu2) en tiempo real
        if (labelFinalizacion != null)
        {
            string mensaje = mensajeFinalizacion.Replace("{puntaje}", sm.score.ToString("0"));
            labelFinalizacion.text = mensaje;
        }
    }

    // Método para actualizar el texto de finalización (Menu2)
    public void MostrarPuntajeFinal()
    {
        if (labelFinalizacion == null)
        {
            Debug.LogWarning("[ScoreTMPHUD] labelFinalizacion es NULL!");
            return;
        }

        var sm = ScoreManager.I;
        if (sm == null)
        {
            Debug.LogWarning("[ScoreTMPHUD] ScoreManager.I es NULL!");
            return;
        }

        Debug.Log($"[ScoreTMPHUD] Mostrando puntaje final: {sm.score}");

        // Reemplazar {puntaje} con el score actual
        string mensaje = mensajeFinalizacion.Replace("{puntaje}", sm.score.ToString("0"));
        labelFinalizacion.text = mensaje;

        Debug.Log($"[ScoreTMPHUD] Texto final: {mensaje}");
    }
}