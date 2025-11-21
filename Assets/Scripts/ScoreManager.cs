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
    public float safeRate    = 10f;   // + por segundo
    public float dangerRate  = -25f;  // - por segundo
    public float neutralRate = 0f;    // 0 (o -1 si quieres decay suave)

    public event Action<float> OnScoreChanged;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    public void AddOverTime(float ratePerSecond)
    {
        float prev = score;
        score += ratePerSecond * Time.deltaTime;
        score = Mathf.Clamp(score, minScore, maxScore);
        if (!Mathf.Approximately(score, prev))
            OnScoreChanged?.Invoke(score);
    }

    public void SetScore(float value)
    {
        float prev = score;
        score = Mathf.Clamp(value, minScore, maxScore);
        if (!Mathf.Approximately(score, prev))
            OnScoreChanged?.Invoke(score);
    }
}
