using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Script simple para cambiar de escena cuando se hace Poke en un botón
/// Compatible con Meta XR Interaction SDK (PokeInteractable)
/// </summary>
public class SceneChangerPoke : MonoBehaviour
{
    [Header("🎬 Configuración de Escena")]
    [Tooltip("Nombre EXACTO de la escena a cargar (debe estar en Build Settings)")]
    public string nombreEscena = "";
    
    [Header("⚙️ Opciones")]
    [Tooltip("Activar para salir de la aplicación en lugar de cambiar escena")]
    public bool esSalir = false;
    
    [Tooltip("Tiempo de espera antes de cambiar (segundos)")]
    [Range(0f, 5f)]
    public float tiempoEspera = 0.5f;
    
    [Header("🔊 Audio (Opcional)")]
    [Tooltip("Sonido que se reproduce al hacer clic")]
    public AudioClip sonidoClick;
    
    [Tooltip("AudioSource para reproducir el sonido (se crea automáticamente si está vacío)")]
    public AudioSource audioSource;
    
    [Header("📊 Debug")]
    [SerializeField] private bool mostrarLogs = true;

    void Start()
    {
        // Crear AudioSource si no existe
        if (audioSource == null && sonidoClick != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // Audio 2D
        }
        
        // Validar configuración
        if (!esSalir && string.IsNullOrEmpty(nombreEscena))
        {
            Debug.LogError($"❌ [{gameObject.name}] No se ha configurado el nombre de la escena. Por favor, asigna 'nombreEscena' en el Inspector.");
        }
        else if (mostrarLogs)
        {
            if (esSalir)
            {
                Debug.Log($"✅ [{gameObject.name}] Configurado para SALIR de la aplicación");
            }
            else
            {
                Debug.Log($"✅ [{gameObject.name}] Configurado para cargar escena: '{nombreEscena}'");
            }
        }
    }

    /// <summary>
    /// Método público que se llama desde el PokeInteractable (When Select)
    /// </summary>
    public void CambiarEscena()
    {
        if (mostrarLogs)
        {
            Debug.Log($"🎯 [{gameObject.name}] Botón presionado!");
        }
        
        // Reproducir sonido si existe
        if (audioSource != null && sonidoClick != null)
        {
            audioSource.PlayOneShot(sonidoClick);
        }
        
        // Iniciar cambio de escena
        if (tiempoEspera > 0)
        {
            Invoke("EjecutarCambio", tiempoEspera);
        }
        else
        {
            EjecutarCambio();
        }
    }

    /// <summary>
    /// Ejecuta el cambio de escena o salida de la aplicación
    /// </summary>
    void EjecutarCambio()
    {
        if (esSalir)
        {
            SalirAplicacion();
        }
        else
        {
            CargarEscena();
        }
    }

    /// <summary>
    /// Carga la escena especificada
    /// </summary>
    void CargarEscena()
    {
        if (string.IsNullOrEmpty(nombreEscena))
        {
            Debug.LogError($"❌ [{gameObject.name}] No se puede cambiar de escena: nombreEscena está vacío");
            return;
        }
        
        // Verificar si la escena existe en Build Settings
        if (SceneExistsInBuildSettings(nombreEscena))
        {
            if (mostrarLogs)
            {
                Debug.Log($"🎬 [{gameObject.name}] Cambiando a escena: '{nombreEscena}'");
            }
            
            SceneManager.LoadScene(nombreEscena);
        }
        else
        {
            Debug.LogError($"❌ [{gameObject.name}] La escena '{nombreEscena}' NO existe en Build Settings. Agrega la escena en File > Build Settings.");
        }
    }

    /// <summary>
    /// Cierra la aplicación
    /// </summary>
    void SalirAplicacion()
    {
        if (mostrarLogs)
        {
            Debug.Log($"🚪 [{gameObject.name}] Saliendo de la aplicación...");
        }
        
        #if UNITY_EDITOR
        // En el editor, detiene el Play Mode
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("🛑 Play Mode detenido (Editor)");
        #else
        // En la build, cierra la aplicación
        Application.Quit();
        Debug.Log("🛑 Aplicación cerrada");
        #endif
    }

    /// <summary>
    /// Verifica si una escena existe en Build Settings
    /// </summary>
    bool SceneExistsInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            
            if (sceneNameInBuild == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Método alternativo por si usas un índice de escena en lugar de nombre
    /// </summary>
    public void CambiarEscenaPorIndice(int indiceEscena)
    {
        if (indiceEscena >= 0 && indiceEscena < SceneManager.sceneCountInBuildSettings)
        {
            if (mostrarLogs)
            {
                Debug.Log($"🎬 [{gameObject.name}] Cambiando a escena con índice: {indiceEscena}");
            }
            
            SceneManager.LoadScene(indiceEscena);
        }
        else
        {
            Debug.LogError($"❌ [{gameObject.name}] Índice de escena inválido: {indiceEscena}");
        }
    }

#if UNITY_EDITOR
    [Header("🛠️ Herramientas de Desarrollo")]
    [SerializeField] private bool mostrarInfoGUI = false;

    void OnGUI()
    {
        if (!mostrarInfoGUI) return;
        
        GUIStyle estilo = new GUIStyle(GUI.skin.box);
        estilo.fontSize = 10;
        estilo.normal.textColor = Color.white;
        estilo.alignment = TextAnchor.UpperLeft;
        
        string info = esSalir ? "Acción: SALIR" : $"Escena: {nombreEscena}";
        GUI.Box(new Rect(10, Screen.height - 60, 250, 50), $"Botón: {gameObject.name}\n{info}", estilo);
    }
#endif
}