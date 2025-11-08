using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialReturn : MonoBehaviour
{
    [SerializeField] string sceneMenu = "1_Start_Menu";

    public void BackToMenu()
    {
        SceneManager.LoadScene(sceneMenu);
    }
}
