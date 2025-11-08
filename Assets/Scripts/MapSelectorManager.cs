using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSelectorManager : MonoBehaviour
{   
    //reemplazar mapa1 y mapa2 por los nombres de las escenas reales
    
    [SerializeField] string sceneMap1 = "mapa1";
    [SerializeField] string sceneMap2 = "mapa2";
    [SerializeField] string sceneMenu = "1_Start_Menu";

    public void LoadMap1()
        => SceneManager.LoadScene(sceneMap1);

    public void LoadMap2()
        => SceneManager.LoadScene(sceneMap2);

    public void BackToMenu()
        => SceneManager.LoadScene(sceneMenu);
}
