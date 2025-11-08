using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] string sceneSelector = "2_MapSelector";
    [SerializeField] string sceneTutorial = "3_Tutorial";

    public void LoadPlay()       
        => SceneManager.LoadScene(sceneSelector);

    public void LoadTutorial()  
        => SceneManager.LoadScene(sceneTutorial);

    public void QuitApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
