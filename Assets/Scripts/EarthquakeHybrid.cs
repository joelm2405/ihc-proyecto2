using UnityEngine;
using System.Collections;

/// <summary>
/// Sistema híbrido de terremoto que combina:
/// - Perlin Noise para movimiento natural de cámara
/// - Oscilación sin/cos para efectos específicos
/// - Vibración de alta frecuencia para realismo
/// </summary>
public class EarthquakeHybrid : MonoBehaviour
{
    [Header("⏱️ CONFIGURACIÓN DE TIEMPO")]
    [Tooltip("Tiempo de espera antes de iniciar el terremoto (en segundos)")]
    [Range(0f, 60f)]
    public float tiempoEsperaInicio = 10f;
    
    [Tooltip("Duración total del terremoto (en segundos)")]
    [Range(5f, 120f)]
    public float duracionTotal = 30f;
    
    [Header("📈 Fases del Terremoto (RF15)")]
    [Tooltip("Duración de la fase de aumento (leve → fuerte)")]
    [Range(2f, 30f)]
    public float duracionAumento = 8f;
    
    [Tooltip("Duración de la fase de intensidad máxima")]
    [Range(2f, 30f)]
    public float duracionMaxima = 10f;
    // La fase de disminución se calcula automáticamente
    
    [Header("💪 Intensidades")]
    [Tooltip("Intensidad inicial (muy leve)")]
    [Range(0.001f, 0.1f)]
    public float intensidadInicial = 0.01f;
    
    [Tooltip("Intensidad máxima (más alto = más fuerte)")]
    [Range(0.05f, 0.3f)]
    public float intensidadMaxima = 0.12f;
    
    [Header("🌊 Movimiento Base (Perlin Noise - Suave)")]
    [Tooltip("Frecuencia del movimiento principal (recomendado: 15-25)")]
    [Range(10f, 40f)]
    public float frecuenciaBase = 20f;
    
    [Tooltip("Intensidad de rotación de cámara (0 = sin rotación)")]
    [Range(0f, 2f)]
    public float intensidadRotacion = 0.8f;
    
    [Header("⚡ Vibración Secundaria (Detalle Realista)")]
    [Tooltip("Activar vibración de alta frecuencia")]
    public bool usarVibracion = true;
    
    [Tooltip("Frecuencia de vibración (recomendado: 40-60)")]
    [Range(30f, 100f)]
    public float frecuenciaVibracion = 50f;
    
    [Tooltip("Intensidad de la vibración (más bajo = más sutil)")]
    [Range(0f, 0.5f)]
    public float intensidadVibracion = 0.15f;
    
    [Header("↔️ Oscilación Direccional (Opcional)")]
    [Tooltip("Agregar oscilación dominante en un eje")]
    public bool usarOscilacionDireccional = true;
    
    [Tooltip("Eje principal de oscilación")]
    public Vector3 direccionOscilacion = new Vector3(1f, 0.2f, 0.5f);
    
    [Tooltip("Frecuencia de oscilación (más bajo = más lento)")]
    [Range(0.5f, 5f)]
    public float frecuenciaOscilacion = 2f;
    
    [Range(0f, 1f)]
    public float intensidadOscilacion = 0.3f;
    
    [Header("🎯 Referencias")]
    [Tooltip("Transform de la cámara (CenterEyeAnchor). Si está vacío, se busca automáticamente")]
    public Transform cameraTransform;
    
    [Header("🔊 Audio (RNF04)")]
    [Tooltip("AudioSource para el sonido del terremoto")]
    public AudioSource audioSource;
    
    [Tooltip("Clip de audio del terremoto")]
    public AudioClip sonidoTerremoto;
    
    [Tooltip("Volumen inicial (fase de aumento)")]
    [Range(0f, 1f)]
    public float volumenInicial = 0.3f;
    
    [Tooltip("Volumen máximo (fase de máxima intensidad)")]
    [Range(0f, 1f)]
    public float volumenMaximo = 0.8f;
    
    // Variables privadas
    private Vector3 posicionOriginal;
    private Quaternion rotacionOriginal;
    private bool terremotoActivo = false;
    private float tiempoTranscurrido = 0f;
    private float intensidadActual = 0f;
    private float offsetPerlin; // Para evitar patrones repetitivos
    private bool terremotoCompletado = false;

    void Start()
    {
        // Buscar CenterEyeAnchor si no está asignado
        if (cameraTransform == null)
        {
            GameObject centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null)
            {
                cameraTransform = centerEye.transform;
                Debug.Log("✅ CenterEyeAnchor encontrado automáticamente");
            }
            else
            {
                Debug.LogError("❌ No se encontró CenterEyeAnchor. Por favor, asigna la cámara manualmente en el Inspector.");
                enabled = false;
                return;
            }
        }
        
        // Guardar estado original
        posicionOriginal = cameraTransform.localPosition;
        rotacionOriginal = cameraTransform.localRotation;
        
        // Offset aleatorio para Perlin Noise (hace que cada terremoto sea diferente)
        offsetPerlin = Random.Range(0f, 1000f);
        
        // Configurar audio
        ConfigurarAudio();
        
        // Programar inicio automático
        Invoke("IniciarTerremoto", tiempoEsperaInicio);
        
        Debug.Log($"🌋 Terremoto programado para iniciar automáticamente en {tiempoEsperaInicio} segundos");
        Debug.Log($"📊 Duración total: {duracionTotal}s (Aumento: {duracionAumento}s | Máxima: {duracionMaxima}s | Disminución: {duracionTotal - duracionAumento - duracionMaxima}s)");
    }

    void ConfigurarAudio()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.clip = sonidoTerremoto;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f; // Audio 3D
        audioSource.volume = 0f;
        audioSource.playOnAwake = false;
    }

    void IniciarTerremoto()
    {
        if (terremotoCompletado)
        {
            Debug.LogWarning("⚠️ El terremoto ya se ejecutó. No se puede iniciar nuevamente.");
            return;
        }
        
        terremotoActivo = true;
        tiempoTranscurrido = 0f;
        
        // Iniciar audio
        if (audioSource != null && sonidoTerremoto != null)
        {
            audioSource.Play();
        }
        
        Debug.Log("🌋 ¡TERREMOTO INICIADO AUTOMÁTICAMENTE!");
    }

    void Update()
    {
        if (!terremotoActivo) return;
        
        tiempoTranscurrido += Time.deltaTime;
        
        // Calcular intensidad progresiva (RF15)
        intensidadActual = CalcularIntensidad(tiempoTranscurrido);
        
        // Aplicar el efecto de terremoto
        AplicarTerremoto();
        
        // Actualizar volumen del audio según intensidad
        ActualizarAudio();
        
        // Verificar fin automático
        if (tiempoTranscurrido >= duracionTotal)
        {
            FinalizarTerremoto();
        }
    }

    float CalcularIntensidad(float tiempo)
    {
        // Fase 1: Aumento gradual
        if (tiempo < duracionAumento)
        {
            float progreso = tiempo / duracionAumento;
            // Usar curva suave (ease-in)
            progreso = progreso * progreso;
            return Mathf.Lerp(intensidadInicial, intensidadMaxima, progreso);
        }
        // Fase 2: Intensidad máxima
        else if (tiempo < duracionAumento + duracionMaxima)
        {
            // Agregar variación aleatoria en la fase máxima (más realista)
            float variacion = Mathf.PerlinNoise(tiempo * 0.5f, offsetPerlin) * 0.2f;
            return intensidadMaxima * (1f + variacion);
        }
        // Fase 3: Disminución gradual
        else
        {
            float tiempoDisminucion = tiempo - (duracionAumento + duracionMaxima);
            float duracionDisminucion = duracionTotal - (duracionAumento + duracionMaxima);
            float progreso = tiempoDisminucion / duracionDisminucion;
            // Usar curva suave (ease-out)
            progreso = 1f - (1f - progreso) * (1f - progreso);
            return Mathf.Lerp(intensidadMaxima, intensidadInicial, progreso);
        }
    }

    void AplicarTerremoto()
    {
        Vector3 desplazamiento = Vector3.zero;
        Quaternion rotacionExtra = Quaternion.identity;
        
        // 1. MOVIMIENTO BASE CON PERLIN NOISE (natural y suave)
        float time = Time.time + offsetPerlin;
        float x = (Mathf.PerlinNoise(time * frecuenciaBase, 0f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(0f, time * frecuenciaBase) - 0.5f) * 2f;
        float z = (Mathf.PerlinNoise(time * frecuenciaBase, time * frecuenciaBase) - 0.5f) * 2f;
        
        desplazamiento += new Vector3(x, y, z) * intensidadActual;
        
        // 2. VIBRACIÓN DE ALTA FRECUENCIA (detalle realista)
        if (usarVibracion)
        {
            float vx = (Mathf.PerlinNoise(time * frecuenciaVibracion, 500f) - 0.5f) * 2f;
            float vy = (Mathf.PerlinNoise(500f, time * frecuenciaVibracion) - 0.5f) * 2f;
            float vz = (Mathf.PerlinNoise(time * frecuenciaVibracion, time * frecuenciaVibracion + 500f) - 0.5f) * 2f;
            
            desplazamiento += new Vector3(vx, vy, vz) * intensidadActual * intensidadVibracion;
        }
        
        // 3. OSCILACIÓN DIRECCIONAL (simula onda sísmica dominante)
        if (usarOscilacionDireccional)
        {
            float onda = Mathf.Sin(time * frecuenciaOscilacion * Mathf.PI);
            desplazamiento += direccionOscilacion.normalized * onda * intensidadActual * intensidadOscilacion;
        }
        
        // 4. ROTACIÓN DE CÁMARA (simula pérdida de equilibrio)
        float rotX = (Mathf.PerlinNoise(time * frecuenciaBase * 0.4f, 100f) - 0.5f) * intensidadActual * intensidadRotacion * 15f;
        float rotZ = (Mathf.PerlinNoise(100f, time * frecuenciaBase * 0.4f) - 0.5f) * intensidadActual * intensidadRotacion * 15f;
        
        rotacionExtra = Quaternion.Euler(rotX, 0f, rotZ);
        
        // APLICAR TRANSFORMACIONES
        cameraTransform.localPosition = posicionOriginal + desplazamiento;
        cameraTransform.localRotation = rotacionOriginal * rotacionExtra;
    }

    void ActualizarAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            // Interpolar volumen según intensidad
            float volumenTarget = Mathf.Lerp(volumenInicial, volumenMaximo, 
                (intensidadActual - intensidadInicial) / (intensidadMaxima - intensidadInicial));
            audioSource.volume = Mathf.Lerp(audioSource.volume, volumenTarget, Time.deltaTime * 2f);
        }
    }

    void FinalizarTerremoto()
    {
        terremotoActivo = false;
        terremotoCompletado = true;
        
        // Fade out del audio
        if (audioSource != null && audioSource.isPlaying)
        {
            StartCoroutine(FadeOutAudio(1f));
        }
        
        // Volver suavemente a la posición original
        StartCoroutine(VolverAPosicionOriginal(2f));
        
        Debug.Log("✅ Terremoto finalizado automáticamente");
        
        // Aquí puedes disparar eventos para el siguiente paso (RF07)
        // Ejemplo: Activar la mochila de emergencia
        // EventManager.OnTerremotoFinalizado?.Invoke();
    }

    IEnumerator FadeOutAudio(float duracion)
    {
        if (audioSource == null) yield break;
        
        float volumeInicial = audioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(volumeInicial, 0f, elapsed / duracion);
            yield return null;
        }
        
        audioSource.Stop();
        audioSource.volume = volumenInicial;
    }

    IEnumerator VolverAPosicionOriginal(float duracion)
    {
        Vector3 posInicial = cameraTransform.localPosition;
        Quaternion rotInicial = cameraTransform.localRotation;
        float elapsed = 0f;
        
        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duracion;
            // Ease out
            t = 1f - (1f - t) * (1f - t);
            
            cameraTransform.localPosition = Vector3.Lerp(posInicial, posicionOriginal, t);
            cameraTransform.localRotation = Quaternion.Slerp(rotInicial, rotacionOriginal, t);
            yield return null;
        }
        
        cameraTransform.localPosition = posicionOriginal;
        cameraTransform.localRotation = rotacionOriginal;
    }

    // MÉTODOS PÚBLICOS (para que otros scripts puedan consultar el estado)

    /// <summary>
    /// Devuelve la intensidad actual del terremoto
    /// </summary>
    public float GetIntensidadActual()
    {
        return intensidadActual;
    }

    /// <summary>
    /// Devuelve si el terremoto está activo en este momento
    /// </summary>
    public bool EstaTerremotoActivo()
    {
        return terremotoActivo;
    }

    /// <summary>
    /// Devuelve si el terremoto ya finalizó
    /// </summary>
    public bool TerremotoHaFinalizado()
    {
        return terremotoCompletado;
    }

    /// <summary>
    /// Devuelve el tiempo transcurrido del terremoto
    /// </summary>
    public float GetTiempoTranscurrido()
    {
        return tiempoTranscurrido;
    }

    /// <summary>
    /// Devuelve el tiempo restante hasta que finalice el terremoto
    /// </summary>
    public float GetTiempoRestante()
    {
        if (!terremotoActivo) return 0f;
        return Mathf.Max(0f, duracionTotal - tiempoTranscurrido);
    }

    // INFORMACIÓN EN CONSOLA (Solo para desarrollo)
#if UNITY_EDITOR
    [Header("📊 Información en Tiempo Real (Solo Editor)")]
    [SerializeField] private bool mostrarInfoConsola = true;

    void OnGUI()
    {
        if (!mostrarInfoConsola) return;
        
        GUIStyle estiloLabel = new GUIStyle(GUI.skin.label);
        estiloLabel.fontSize = 12;
        estiloLabel.normal.textColor = Color.white;
        
        // Fondo semi-transparente
        GUI.Box(new Rect(10, 10, 320, 150), "");
        
        int yPos = 20;
        
        if (!terremotoActivo && !terremotoCompletado)
        {
            GUI.Label(new Rect(20, yPos, 300, 25), $"⏳ Esperando inicio: {tiempoEsperaInicio}s", estiloLabel);
        }
        else if (terremotoActivo)
        {
            GUI.Label(new Rect(20, yPos, 300, 25), $"🌋 TERREMOTO ACTIVO", estiloLabel);
            yPos += 25;
            GUI.Label(new Rect(20, yPos, 300, 25), $"Intensidad: {intensidadActual:F4}", estiloLabel);
            yPos += 25;
            GUI.Label(new Rect(20, yPos, 300, 25), $"Tiempo: {tiempoTranscurrido:F1}s / {duracionTotal:F0}s", estiloLabel);
            yPos += 25;
            GUI.Label(new Rect(20, yPos, 300, 25), $"Fase: {ObtenerFaseActual()}", estiloLabel);
            yPos += 25;
            
            if (audioSource != null)
            {
                GUI.Label(new Rect(20, yPos, 300, 25), $"Volumen: {audioSource.volume:F2}", estiloLabel);
            }
        }
        else if (terremotoCompletado)
        {
            GUI.Label(new Rect(20, yPos, 300, 25), $"✅ Terremoto Completado", estiloLabel);
        }
    }

    string ObtenerFaseActual()
    {
        if (!terremotoActivo) return "Inactivo";
        if (tiempoTranscurrido < duracionAumento) return "🔼 Aumentando";
        if (tiempoTranscurrido < duracionAumento + duracionMaxima) return "🔥 Máxima Intensidad";
        return "🔽 Disminuyendo";
    }
#endif
}