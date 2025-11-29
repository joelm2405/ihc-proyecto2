using UnityEngine;

public class SetSkyboxPerScene : MonoBehaviour
{
    [Header("Skybox Material for this Scene")]
    public Material skybox;

    void Start()
    {
        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
            DynamicGI.UpdateEnvironment();
        }
    }
}
