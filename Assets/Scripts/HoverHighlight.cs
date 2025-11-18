using UnityEngine;
using Oculus.Interaction;

public class HoverHighlight : MonoBehaviour
{
    [Header("Material de la imagen")]
    public Material targetMaterial;

    [Header("Configuración")]
    public Color hoverColor = Color.white; 
    public float highlightIntensity = 1.25f; // brillo extra
    public float transitionSpeed = 8f;

    private Color originalColor;
    private bool isHovering = false;

    private void Start()
    {
        if (targetMaterial != null)
        {
            originalColor = targetMaterial.color;
        }
    }

    public void OnHoverEnter()
    {
        isHovering = true;
    }

    public void OnHoverExit()
    {
        isHovering = false;
    }

    private void Update()
    {
        if (targetMaterial == null) return;

        Color targetColor = originalColor;

        if (isHovering)
        {
            targetColor = originalColor * highlightIntensity; 
        }

        targetMaterial.color = Color.Lerp(
            targetMaterial.color, 
            targetColor, 
            Time.deltaTime * transitionSpeed
        );
    }
}
