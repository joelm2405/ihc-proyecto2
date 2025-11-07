using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialController : MonoBehaviour
{
    public void BackToMenu()
    {
        SceneManager.LoadScene("1 Start Scene");
    }
}
