using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Scene Names (tal como aparecen en Build Settings)")]
    public string mainSceneName = "3 MainGame";   // placeholder para el juego real
    public string tutorialSceneName = "2 Tutorial";

    public void Play()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    public void OpenTutorial()
    {
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
