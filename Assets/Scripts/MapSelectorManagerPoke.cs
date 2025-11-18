using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSelectorManagerPoke : MonoBehaviour
{
    // Función para cargar cualquier escena por su nombre
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
