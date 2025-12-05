using UnityEngine;
using System.Collections;

/// <summary>
/// Sistema de terremoto para VR - MUEVE EL ESCENARIO, NO EL JUGADOR
/// El jugador VR mantiene total libertad de movimiento
/// Solo el entorno (casa/mundo) se mueve
/// </summary>
public class EarthquakeHybrid : MonoBehaviour
{
    [Header("⏱ CONFIGURACIÓN DE TIEMPO")]
    [Tooltip("Tiempo de espera antes de iniciar el terremoto (en segundos)")]
    [Range(0f, 60f)]
    public float tiempoEsperaInicio = 10f;

    [Tooltip("Duración total del terremoto (en segundos)")]
    [Range(5f, 120f)]
    public float duracionTotal = 30f;

    [Header("📈 Fases del Terremoto")]
    [Tooltip("Duración de la fase de aumento (leve → fuerte)")]
    [Range(2f, 30f)]
    public float duracionAumento = 8f;

    [Tooltip("Duración de la fase de intensidad máxima")]
    [Range(2f, 30f)]
    public float duracionMaxima = 10f;

    [Header("💪 Intensidades")]
    [Tooltip("Intensidad inicial (muy leve)")]
    [Range(0.0001f, 0.01f)]
    public float intensidadInicial = 0.003f;

    [Tooltip("Intensidad máxima")]
    [Range(0.005f, 0.1f)]
    public float intensidadMaxima = 0.045f;

    [Header("🏠 CONFIGURACIÓN DEL ESCENARIO")]
    [Tooltip("Transform del escenario/casa que se va a mover (NO el jugador)")]
    public Transform escenarioTransform;

    [Tooltip("Nombre del GameObject del escenario (si no está asignado manualmente)")]
    public string nombreEscenario = "Escenario";

    [Header("🌊 Movimiento (MUY SUAVE)")]
    [Tooltip("Frecuencia del movimiento principal")]
    [Range(0.5f, 5f)]
    public float frecuenciaBase = 1.8f;

    [Tooltip("Multiplicador de movimiento vertical (arriba/abajo)")]
    [Range(0.5f, 3f)]
    public float multiplicadorVertical = 1.8f;

    [Tooltip("Multiplicador de movimiento horizontal")]
    [Range(0.05f, 1f)]
    public float multiplicadorHorizontal = 0.4f;

    [Tooltip("Intensidad de rotación del escenario")]
    [Range(0f, 1f)]
    public float intensidadRotacion = 0.3f;

    [Header("⚡ Vibración Secundaria")]
    [Tooltip("Activar vibración de alta frecuencia")]
    public bool usarVibracion = true;

    [Tooltip("Frecuencia de vibración")]
    [Range(5f, 30f)]
    public float frecuenciaVibracion = 16f;

    [Tooltip("Intensidad de la vibración")]
    [Range(0f, 0.15f)]
    public float intensidadVibracion = 0.08f;

    [Header("↔ Oscilación Direccional")]
    [Tooltip("Agregar oscilación dominante")]
    public bool usarOscilacionDireccional = true;

    [Tooltip("Eje principal (énfasis vertical)")]
    public Vector3 direccionOscilacion = new Vector3(0.4f, 1.2f, 0.3f);

    [Tooltip("Frecuencia de oscilación")]
    [Range(0.3f, 2f)]
    public float frecuenciaOscilacion = 0.9f;

    [Range(0f, 0.5f)]
    public float intensidadOscilacion = 0.28f;

    [Header("🔊 Audio")]
    [Tooltip("AudioSource para el sonido del terremoto")]
    public AudioSource audioSource;

    [Tooltip("Clip de audio del terremoto")]
    public AudioClip sonidoTerremoto;

    [Tooltip("Volumen inicial")]
    [Range(0f, 1f)]
    public float volumenInicial = 0.3f;

    [Tooltip("Volumen máximo")]
    [Range(0f, 1f)]
    public float volumenMaximo = 0.8f;

    [Header("🔧 Suavizado")]
    [Tooltip("Velocidad de suavizado del movimiento")]
    [Range(3f, 20f)]
    public float velocidadSuavizado = 12f;

    // Variables privadas
    private Vector3 posicionOriginalEscenario;
    private Quaternion rotacionOriginalEscenario;
    private bool terremotoActivo = false;
    private float tiempoTranscurrido = 0f;
    private float intensidadActual = 0f;
    private float offsetPerlin;
    private bool terremotoCompletado = false;

    // Para suavizado
    private Vector3 desplazamientoActual;
    private Quaternion rotacionActual;

    void Start()
    {
        // Buscar el escenario si no está asignado
        if (escenarioTransform == null)
        {
            GameObject escenario = GameObject.Find(nombreEscenario);

            if (escenario == null)
            {
                // Intentar otros nombres comunes
                string[] nombresComunes = { "Casa", "House", "Edificio", "Building", "Environment", "World", "Map", "Scene", "Apartment" };

                foreach (string nombre in nombresComunes)
                {
                    escenario = GameObject.Find(nombre);
                    if (escenario != null)
                    {
                        Debug.Log($"✅ Escenario encontrado: {nombre}");
                        break;
                    }
                }
            }

            if (escenario != null)
            {
                escenarioTransform = escenario.transform;
            }
            else
            {
                Debug.LogError("❌ ERROR: No se encontró el escenario.");
                Debug.LogError("Por favor, asigna manualmente el GameObject de tu casa/escenario en el Inspector.");
                Debug.LogError("O asegúrate que el objeto se llame 'Escenario', 'Casa', 'House', o 'Apartment'");
                enabled = false;
                return;
            }
        }

        // CRÍTICO: Guardar la posición ACTUAL del escenario (no modificarla)
        posicionOriginalEscenario = escenarioTransform.position;
        rotacionOriginalEscenario = escenarioTransform.rotation;

        // Inicializar variables de suavizado
        desplazamientoActual = Vector3.zero;
        rotacionActual = Quaternion.identity;

        // Offset aleatorio
        offsetPerlin = Random.Range(0f, 1000f);

        // Configurar audio
        ConfigurarAudio();

        // Programar inicio
        Invoke("IniciarTerremoto", tiempoEsperaInicio);

        Debug.Log($"🌋 Terremoto programado para {tiempoEsperaInicio}s");
        Debug.Log($"🏠 Escenario a mover: {escenarioTransform.name}");
        Debug.Log($"📍 Posición original guardada: {posicionOriginalEscenario}");
        Debug.Log($"👤 El jugador VR mantendrá total libertad de movimiento");
    }

    void ConfigurarAudio()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = sonidoTerremoto;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.volume = 0f;
        audioSource.playOnAwake = false;
    }

    void IniciarTerremoto()
    {
        if (terremotoCompletado)
        {
            Debug.LogWarning("⚠ El terremoto ya se ejecutó.");
            return;
        }

        terremotoActivo = true;
        tiempoTranscurrido = 0f;

        if (audioSource != null && sonidoTerremoto != null)
        {
            audioSource.Play();
        }

        Debug.Log("🌋 ¡TERREMOTO INICIADO! Moviendo escenario...");
    }

    void Update()
    {
        if (!terremotoActivo) return;

        tiempoTranscurrido += Time.deltaTime;
        intensidadActual = CalcularIntensidad(tiempoTranscurrido);

        AplicarMovimientoEscenario();
        ActualizarAudio();

        if (tiempoTranscurrido >= duracionTotal)
        {
            FinalizarTerremoto();
        }
    }

    float CalcularIntensidad(float tiempo)
    {
        if (tiempo < duracionAumento)
        {
            float progreso = tiempo / duracionAumento;
            progreso = Mathf.SmoothStep(0f, 1f, progreso);
            return Mathf.Lerp(intensidadInicial, intensidadMaxima, progreso);
        }
        else if (tiempo < duracionAumento + duracionMaxima)
        {
            float variacion = Mathf.PerlinNoise(tiempo * 0.2f, offsetPerlin) * 0.12f;
            return intensidadMaxima * (1f + variacion);
        }
        else
        {
            float tiempoDisminucion = tiempo - (duracionAumento + duracionMaxima);
            float duracionDisminucion = duracionTotal - (duracionAumento + duracionMaxima);
            float progreso = tiempoDisminucion / duracionDisminucion;
            progreso = Mathf.SmoothStep(0f, 1f, progreso);
            return Mathf.Lerp(intensidadMaxima, intensidadInicial, progreso);
        }
    }

    void AplicarMovimientoEscenario()
    {
        Vector3 desplazamientoTarget = Vector3.zero;
        float time = Time.time + offsetPerlin;

        // 1. MOVIMIENTO BASE CON PERLIN NOISE
        float x = (Mathf.PerlinNoise(time * frecuenciaBase, 0f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(0f, time * frecuenciaBase) - 0.5f) * 2f;
        float z = (Mathf.PerlinNoise(time * frecuenciaBase, time * frecuenciaBase) - 0.5f) * 2f;

        desplazamientoTarget += new Vector3(
            x * multiplicadorHorizontal,
            y * multiplicadorVertical,
            z * multiplicadorHorizontal
        ) * intensidadActual;

        // 2. VIBRACIÓN SUTIL
        if (usarVibracion)
        {
            float vx = (Mathf.PerlinNoise(time * frecuenciaVibracion, 500f) - 0.5f) * 2f;
            float vy = (Mathf.PerlinNoise(500f, time * frecuenciaVibracion) - 0.5f) * 2f;
            float vz = (Mathf.PerlinNoise(time * frecuenciaVibracion, time * frecuenciaVibracion + 500f) - 0.5f) * 2f;

            desplazamientoTarget += new Vector3(
                vx * multiplicadorHorizontal * 0.4f,
                vy * multiplicadorVertical * 0.4f,
                vz * multiplicadorHorizontal * 0.4f
            ) * intensidadActual * intensidadVibracion;
        }

        // 3. OSCILACIÓN DIRECCIONAL
        if (usarOscilacionDireccional)
        {
            float onda = Mathf.Sin(time * frecuenciaOscilacion * Mathf.PI);
            Vector3 oscilacion = direccionOscilacion.normalized * onda * intensidadActual * intensidadOscilacion;

            oscilacion.x *= multiplicadorHorizontal;
            oscilacion.y *= multiplicadorVertical;
            oscilacion.z *= multiplicadorHorizontal;

            desplazamientoTarget += oscilacion;
        }

        // SUAVIZAR el desplazamiento
        desplazamientoActual = Vector3.Lerp(
            desplazamientoActual,
            desplazamientoTarget,
            Time.deltaTime * velocidadSuavizado
        );

        // 4. ROTACIÓN DEL ESCENARIO (muy sutil)
        float rotX = (Mathf.PerlinNoise(time * frecuenciaBase * 0.3f, 100f) - 0.5f) * intensidadActual * intensidadRotacion * 1.5f;
        float rotZ = (Mathf.PerlinNoise(100f, time * frecuenciaBase * 0.3f) - 0.5f) * intensidadActual * intensidadRotacion * 1.5f;

        Quaternion rotacionTarget = Quaternion.Euler(rotX, 0f, rotZ);

        // SUAVIZAR la rotación
        rotacionActual = Quaternion.Slerp(
            rotacionActual,
            rotacionTarget,
            Time.deltaTime * velocidadSuavizado * 0.7f
        );

        // APLICAR AL ESCENARIO (no al jugador)
        escenarioTransform.position = posicionOriginalEscenario + desplazamientoActual;
        escenarioTransform.rotation = rotacionOriginalEscenario * rotacionActual;
    }

    void ActualizarAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            float volumenTarget = Mathf.Lerp(volumenInicial, volumenMaximo,
                (intensidadActual - intensidadInicial) / (intensidadMaxima - intensidadInicial));
            audioSource.volume = Mathf.Lerp(audioSource.volume, volumenTarget, Time.deltaTime * 2f);
        }
    }

    void FinalizarTerremoto()
    {
        terremotoActivo = false;
        terremotoCompletado = true;

        if (audioSource != null && audioSource.isPlaying)
        {
            StartCoroutine(FadeOutAudio(1.5f));
        }

        StartCoroutine(VolverEscenarioAPosicionOriginal(3f));

        Debug.Log("✅ Terremoto finalizado - Escenario volviendo a posición original");
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

    IEnumerator VolverEscenarioAPosicionOriginal(float duracion)
    {
        Vector3 posInicial = escenarioTransform.position;
        Quaternion rotInicial = escenarioTransform.rotation;
        float elapsed = 0f;

        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duracion);

            escenarioTransform.position = Vector3.Lerp(posInicial, posicionOriginalEscenario, t);
            escenarioTransform.rotation = Quaternion.Slerp(rotInicial, rotacionOriginalEscenario, t);
            yield return null;
        }

        escenarioTransform.position = posicionOriginalEscenario;
        escenarioTransform.rotation = rotacionOriginalEscenario;

        Debug.Log("🏠 Escenario restaurado a posición original");
    }

    // ==================== MÉTODOS PÚBLICOS ====================
    public float GetIntensidadActual() => intensidadActual;
    public bool EstaTerremotoActivo() => terremotoActivo;
    public bool TerremotoHaFinalizado() => terremotoCompletado;
    public float GetTiempoTranscurrido() => tiempoTranscurrido;
    public float GetTiempoRestante() => terremotoActivo ? Mathf.Max(0f, duracionTotal - tiempoTranscurrido) : 0f;

#if UNITY_EDITOR
    [Header("📊 Debug")]
    [SerializeField] private bool mostrarInfoConsola = true;

    void OnGUI()
    {
        if (!mostrarInfoConsola) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 12;
        style.normal.textColor = Color.white;
        
        GUI.Box(new Rect(10, 10, 400, 200), "");
        
        int y = 20;
        
        GUI.Label(new Rect(20, y, 380, 25), $"🏠 Escenario: {(escenarioTransform != null ? escenarioTransform.name : "NO ASIGNADO")}", style);
        y += 25;
        
        if (escenarioTransform != null)
        {
            GUI.Label(new Rect(20, y, 380, 25), $"📍 Pos Original: {posicionOriginalEscenario}", style);
            y += 25;
            GUI.Label(new Rect(20, y, 380, 25), $"📍 Pos Actual: {escenarioTransform.position}", style);
            y += 25;
        }
        
        if (!terremotoActivo && !terremotoCompletado)
        {
            GUI.Label(new Rect(20, y, 380, 25), $"⏳ Inicio en: {tiempoEsperaInicio}s", style);
        }
        else if (terremotoActivo)
        {
            GUI.Label(new Rect(20, y, 380, 25), "🌋 TERREMOTO ACTIVO - ESCENARIO MOVIÉNDOSE", style);
            y += 25;
            GUI.Label(new Rect(20, y, 380, 25), $"Intensidad: {intensidadActual:F5}", style);
            y += 25;
            GUI.Label(new Rect(20, y, 380, 25), $"Desplazamiento: {desplazamientoActual.magnitude:F4}m", style);
            y += 25;
            GUI.Label(new Rect(20, y, 380, 25), $"Tiempo: {tiempoTranscurrido:F1}s / {duracionTotal:F0}s", style);
            y += 25;
            
            if (audioSource != null)
            {
                GUI.Label(new Rect(20, y, 380, 25), $"Audio: {audioSource.volume:F2}", style);
            }
        }
        else if (terremotoCompletado)
        {
            GUI.Label(new Rect(20, y, 380, 25), "✅ Completado - Jugador tiene control total", style);
        }
    }
#endif
}