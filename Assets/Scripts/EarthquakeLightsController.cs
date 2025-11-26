using System.Collections.Generic;
using UnityEngine;

public class EarthquakeLightsController : MonoBehaviour
{
    [Header("References")]
    public EarthquakeHybrid earthquake; // Si lo dejas vacío intenta encontrarlo en Start

    [Header("Apagado por intensidad")]
    [Range(0f, 1f)] public float startOffPercent = 0.25f; // % de luces apagadas al comenzar el sismo
    [Range(0f, 1f)] public float peakOffPercent  = 0.85f; // % de luces apagadas en el pico

    [Header("Parpadeo (solo luces encendidas)")]
    [Range(0f, 1f)] public float flickerAmplitude = 0.3f; // 0–1 (proporción de la intensidad base)
    [Range(0.1f, 40f)] public float flickerSpeed = 16f;

    [Header("Emisión del cover (opcional)")]
    public bool affectEmission = false;
    public Color emissionOn = new Color(1f, 1f, 1f, 1f) * 2f; // color/valor alto
    public Color emissionOff = Color.black;

    // Internos
    class LightSlot
    {
        public Light light;
        public float baseIntensity;
        public Renderer coverRenderer;
        public Color baseEmission;
        public float noiseSeed;
    }

    readonly List<LightSlot> _slots = new();
    int[] _shuffleOrder; // orden estable para ir apagando
    bool _restored = true;

    void Start()
    {
        if (!earthquake)
            earthquake = FindObjectOfType<EarthquakeHybrid>();

        CollectLights();
        MakeShuffleOrder();
    }

    void CollectLights()
    {
        _slots.Clear();

        // Busca TODAS las luces bajo este objeto
        var lights = GetComponentsInChildren<Light>(true);
        foreach (var l in lights)
        {
            if (!l) continue;

            var slot = new LightSlot
            {
                light = l,
                baseIntensity = l.intensity,
                noiseSeed = Random.Range(0f, 1000f)
            };

            // Intenta encontrar un renderer de cubierta en el mismo padre
            // PFB_LightCover suele tener un MeshRenderer
            Renderer cover = l.GetComponentInParent<Renderer>();
            if (!cover)
            {
                // alternativa: buscar en el mismo GO
                cover = l.GetComponent<Renderer>();
            }

            slot.coverRenderer = cover;

            if (cover && affectEmission)
            {
                // Guarda emisión actual si existe
                slot.baseEmission = GetEmissionColor(cover);
                EnableEmissionKeyword(cover, true);
            }

            _slots.Add(slot);
        }
    }

    void MakeShuffleOrder()
    {
        int n = _slots.Count;
        _shuffleOrder = new int[n];
        for (int i = 0; i < n; i++) _shuffleOrder[i] = i;

        // Fisher–Yates
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_shuffleOrder[i], _shuffleOrder[j]) = (_shuffleOrder[j], _shuffleOrder[i]);
        }
    }

    void Update()
    {
        if (_slots.Count == 0) return;

        float t = 0f; // 0 al inicio, 1 en el pico
        bool active = false;

        if (earthquake)
        {
            active = earthquake.EstaTerremotoActivo();
            float i0 = earthquake.intensidadInicial;
            float i1 = earthquake.intensidadMaxima;
            float ia = Mathf.Clamp(earthquake.GetIntensidadActual(), i0, i1);
            t = Mathf.InverseLerp(i0, i1, ia);
        }

        if (!active && !_restored)
        {
            RestoreAll();
            return;
        }

        if (active)
        {
            _restored = false;

            // ¿Cuántas luces apagar según t?
            int total = _slots.Count;
            int offStart = Mathf.RoundToInt(total * startOffPercent);
            int offPeak  = Mathf.RoundToInt(total * peakOffPercent);
            int offNow   = Mathf.RoundToInt(Mathf.Lerp(offStart, offPeak, t));

            // Apaga primeras 'offNow' del orden barajado, el resto enciende con flicker
            HashSet<int> offIdx = new HashSet<int>();
            for (int i = 0; i < offNow && i < _shuffleOrder.Length; i++)
                offIdx.Add(_shuffleOrder[i]);

            float time = Time.time;

            for (int i = 0; i < total; i++)
            {
                var s = _slots[i];
                if (!s.light) continue;

                bool shouldOff = offIdx.Contains(i);

                if (shouldOff)
                {
                    if (s.light.enabled) s.light.enabled = false;
                    if (affectEmission && s.coverRenderer)
                        SetEmissionColor(s.coverRenderer, emissionOff);
                }
                else
                {
                    if (!s.light.enabled) s.light.enabled = true;

                    // parpadeo suave
                    float n = Mathf.PerlinNoise(time * flickerSpeed, s.noiseSeed);
                    float factor = 1f - (flickerAmplitude * n); // [1-amp, 1]
                    s.light.intensity = s.baseIntensity * factor;

                    if (affectEmission && s.coverRenderer)
                    {
                        // Subir/bajar un poco la emisión con el mismo factor
                        Color target = emissionOn * factor;
                        SetEmissionColor(s.coverRenderer, target);
                    }
                }
            }
        }
    }

    void RestoreAll()
    {
        foreach (var s in _slots)
        {
            if (!s.light) continue;
            s.light.enabled = true;
            s.light.intensity = s.baseIntensity;

            if (affectEmission && s.coverRenderer)
                SetEmissionColor(s.coverRenderer, s.baseEmission);
        }
        _restored = true;
    }

    // ==== Helpers Emission (URP/Built-in) ====
    static void EnableEmissionKeyword(Renderer r, bool on)
    {
        if (!r) return;
        foreach (var m in r.sharedMaterials)
        {
            if (!m) continue;
            if (on) m.EnableKeyword("_EMISSION");
            else    m.DisableKeyword("_EMISSION");
        }
    }

    static Color GetEmissionColor(Renderer r)
    {
        if (!r) return Color.black;
        var m = r.material; // instancia segura
        return m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
    }

    static void SetEmissionColor(Renderer r, Color c)
    {
        if (!r) return;
        var m = r.material; // instancia (no shared) para no tocar el asset
        if (m.HasProperty("_EmissionColor"))
        {
            m.SetColor("_EmissionColor", c);
            m.EnableKeyword("_EMISSION");
        }
    }
}
