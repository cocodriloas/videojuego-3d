using System.Collections;
using UnityEngine;

// Versión 3D de EnemigoAnimaciones: como los modelos de Meshy no traen animaciones,
// "animamos" al enemigo cambiando su color y su tamaño desde código.
// Funciona con cualquier modelo 3D: tiñe todos los Renderer que tenga dentro.
// Los otros scripts del enemigo llaman a: IniciarAviso(), Disparar(), RecibirGolpe() y Morir().
public class EnemigoAnimaciones3D : MonoBehaviour
{
    [Header("Modelo (si se deja vacío, busca el hijo llamado \"Modelo\")")]
    public Transform modelo;

    [Header("Golpe recibido")]
    public Color colorGolpe = new Color(1f, 0.25f, 0.25f);
    public float duracionDestello = 0.1f;
    [Tooltip("Cuánto se infla el modelo al recibir un golpe (1 = nada)")]
    public float escalaGolpe = 1.2f;
    public float duracionGolpe = 0.15f;

    [Header("Aviso antes de atacar")]
    [Tooltip("Color al que se va tiñendo mientras avisa (como el brillo amarillo del 2D)")]
    public Color colorAviso = Color.yellow;
    [Tooltip("Cuánto crece mientras avisa (1 = nada)")]
    public float escalaAviso = 1.25f;

    [Header("Disparo / zarpazo")]
    [Tooltip("Pequeño 'apretón' del modelo al atacar")]
    public float escalaDisparo = 0.85f;
    public float duracionDisparo = 0.15f;

    [Header("Muerte")]
    public float duracionMuerte = 0.35f;

    // ---------- Estado interno ----------
    private Renderer[] renderers;
    private Color[] coloresOriginales;
    private MaterialPropertyBlock bloque;
    private Vector3 escalaBase;

    private float finDestello = -1f;       // hasta cuándo dura el destello rojo
    private float inicioAviso, duracionAviso;
    private bool avisando = false;
    private float finAviso = -1f;          // por seguridad: el aviso no dura para siempre
    private float inicioPulso = -10f;      // golpe o disparo (cambio de tamaño rápido)
    private float duracionPulso = 0.1f, escalaPulso = 1f;
    private bool muriendo = false;
    private bool teñido = false;

    public bool Muriendo => muriendo;

    void Awake()
    {
        if (modelo == null) modelo = BuscarModelo(transform);
        escalaBase = modelo.localScale;
        bloque = new MaterialPropertyBlock();

        renderers = GetComponentsInChildren<Renderer>();
        coloresOriginales = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material m = renderers[i].sharedMaterial;
            coloresOriginales[i] = Color.white;
            if (m != null && m.HasProperty("_BaseColor")) coloresOriginales[i] = m.GetColor("_BaseColor");
            else if (m != null && m.HasProperty("_Color")) coloresOriginales[i] = m.GetColor("_Color");
        }
    }

    // Busca el hijo "Modelo"; si no hay, el primer hijo; si no hay hijos, el propio enemigo
    public static Transform BuscarModelo(Transform raiz)
    {
        Transform m = raiz.Find("Modelo");
        if (m == null && raiz.childCount > 0) m = raiz.GetChild(0);
        return m != null ? m : raiz;
    }

    void LateUpdate()
    {
        if (muriendo) return;

        if (avisando && Time.time > finAviso) avisando = false;

        // ----- Color: el destello del golpe tiene prioridad sobre el aviso -----
        if (Time.time < finDestello) Teñir(colorGolpe, 1f);
        else if (avisando) Teñir(colorAviso, ProgresoAviso());
        else if (teñido) QuitarTinte();

        // ----- Tamaño: aviso (crece poco a poco) por un pulso rápido (golpe o disparo) -----
        float escala = 1f;
        if (avisando) escala = Mathf.Lerp(1f, escalaAviso, ProgresoAviso());

        float t = (Time.time - inicioPulso) / duracionPulso;
        if (t < 1f) escala *= Mathf.Lerp(escalaPulso, 1f, t); // vuelve suave a su tamaño

        if (modelo != transform) modelo.localScale = escalaBase * escala;
    }

    float ProgresoAviso()
    {
        return Mathf.Clamp01((Time.time - inicioAviso) / Mathf.Max(0.05f, duracionAviso));
    }

    // ---------- Para llamar desde los otros scripts ----------

    // Se va poniendo amarillo y creciendo durante "duracion" segundos
    public void IniciarAviso(float duracion)
    {
        if (muriendo) return;
        avisando = true;
        inicioAviso = Time.time;
        duracionAviso = duracion;
        finAviso = Time.time + duracion + 1f;
    }

    // Termina el aviso con un "apretón" rápido
    public void Disparar()
    {
        if (muriendo) return;
        avisando = false;
        Pulso(escalaDisparo, duracionDisparo);
    }

    public void RecibirGolpe()
    {
        if (muriendo) return;
        finDestello = Time.time + duracionDestello;
        Pulso(escalaGolpe, duracionGolpe);
    }

    // Apaga todo, se encoge y se borra
    public void Morir()
    {
        if (muriendo) return;
        muriendo = true;

        // Ya no choca ni hace daño mientras desaparece
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
            if (script != this) script.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic) rb.linearVelocity = Vector3.zero; // a uno cinemático no se le puede poner velocidad
            rb.isKinematic = true;
        }

        StartCoroutine(RutinaMuerte());
    }

    // ---------- Funcionamiento interno ----------

    void Pulso(float escala, float duracion)
    {
        inicioPulso = Time.time;
        escalaPulso = escala;
        duracionPulso = Mathf.Max(0.01f, duracion);
    }

    void Teñir(Color color, float cantidad)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Color c = Color.Lerp(coloresOriginales[i], color, cantidad);
            renderers[i].GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", c); // URP Lit
            bloque.SetColor("_Color", c);     // por si algún material no es URP
            renderers[i].SetPropertyBlock(bloque);
        }
        teñido = true;
    }

    void QuitarTinte()
    {
        bloque.Clear();
        foreach (Renderer r in renderers) if (r != null) r.SetPropertyBlock(bloque);
        teñido = false;
    }

    IEnumerator RutinaMuerte()
    {
        // Un último destello y se encoge hasta desaparecer
        Teñir(colorGolpe, 1f);
        Vector3 escalaInicial = transform.localScale;
        float tiempo = 0f;
        while (tiempo < duracionMuerte)
        {
            tiempo += Time.deltaTime;
            float p = tiempo / duracionMuerte;
            // Primero se infla un poquito y luego se encoge (se ve más "blando")
            float escala = p < 0.3f ? Mathf.Lerp(1f, 1.2f, p / 0.3f) : Mathf.Lerp(1.2f, 0f, (p - 0.3f) / 0.7f);
            transform.localScale = escalaInicial * escala;
            yield return null;
        }
        Destroy(gameObject);
    }
}
