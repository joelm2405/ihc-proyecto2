using UnityEngine;
using TMPro;

public class ScoreTMPHUD : MonoBehaviour
{
    public TMP_Text label;

    void Reset()
    {
        if (!label) label = GetComponent<TMP_Text>();
    }

    void LateUpdate()
    {
        var sm = ScoreManager.I;
        if (!sm || !label) return;
        label.text = $"Puntos: {sm.score:0}";
    }
}
