using UnityEngine;

// Hélice animada del submarino: gira más rápido cuanto más rápido se mueve el submarino
// y suelta burbujas por detrás. Como el modelo de Meshy es de una sola pieza, la hélice
// es un objeto aparte (hijo de "Modelo", en la cola). Se crea sola si no existe.
public class HeliceSubmarino : MonoBehaviour
{
    [Tooltip("La hélice que gira (si está vacía se crea una)")]
    public Transform helice;
    [Tooltip("Posición de la hélice en el 'Modelo' (la cola está hacia -Z)")]
    public Vector3 posicionCola = new Vector3(0f, 0f, -1.35f);
    public float tamanio = 0.55f;
    public Color colorHelice = new Color(1f, 0.75f, 0.1f);

    [Header("Velocidad de giro (vueltas por segundo)")]
    public float giroQuieto = 0.6f;
    public float giroMaximo = 6f;

    [Header("Burbujas")]
    public bool burbujas = true;
    [Tooltip("Material de partículas con textura de burbuja (si está vacío se crea uno simple)")]
    public Material materialBurbujas;

    private Rigidbody rb;
    private Transform modelo;
    private ParticleSystem particulas;
    private float angulo;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        modelo = transform.Find("Modelo");
        if (modelo == null) modelo = transform;
        if (helice == null) helice = CrearHelice();
        if (burbujas) particulas = CrearBurbujas();
    }

    private bool pausada;

    // Mientras el submarino está "hundido" no suelta burbujas
    public void Pausar(bool p) { pausada = p; }

    void Update()
    {
        float rapidez = rb != null ? rb.linearVelocity.magnitude : 0f;
        float vueltas = Mathf.Lerp(giroQuieto, giroMaximo, Mathf.Clamp01(rapidez / 8f));
        angulo += vueltas * 360f * Time.deltaTime;
        helice.localRotation = Quaternion.Euler(0f, 0f, angulo);

        if (particulas != null)
        {
            var emision = particulas.emission;
            emision.rateOverTime = pausada ? 0f : Mathf.Lerp(2f, 30f, Mathf.Clamp01(rapidez / 6f));
        }
    }

    Transform CrearHelice()
    {
        Transform cola = new GameObject("Hélice").transform;
        cola.SetParent(modelo, false);
        cola.localPosition = posicionCola;

        Material mat = Material(colorHelice);
        GameObject eje = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(eje.GetComponent<Collider>());
        eje.transform.SetParent(cola, false);
        eje.transform.localScale = Vector3.one * tamanio * 0.35f;
        eje.GetComponent<Renderer>().sharedMaterial = mat;

        // Tres aspas repartidas en círculo
        for (int i = 0; i < 3; i++)
        {
            Transform brazo = new GameObject("Aspa").transform;
            brazo.SetParent(cola, false);
            brazo.localRotation = Quaternion.Euler(0f, 0f, i * 120f);
            GameObject aspa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(aspa.GetComponent<Collider>());
            aspa.transform.SetParent(brazo, false);
            aspa.transform.localPosition = new Vector3(0f, tamanio * 0.5f, 0f);
            aspa.transform.localRotation = Quaternion.Euler(0f, 25f, 0f); // inclinadas, como una hélice real
            aspa.transform.localScale = new Vector3(tamanio * 0.35f, tamanio, 0.04f);
            aspa.GetComponent<Renderer>().sharedMaterial = mat;
        }
        return cola;
    }

    ParticleSystem CrearBurbujas()
    {
        GameObject go = new GameObject("Burbujas");
        go.transform.SetParent(modelo, false);
        go.transform.localPosition = posicionCola + new Vector3(0f, 0f, -0.2f);
        go.transform.localRotation = Quaternion.LookRotation(Vector3.back);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = 1.2f;
        main.startSpeed = 1.2f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.startColor = new Color(0.8f, 0.95f, 1f, 0.7f);
        main.simulationSpace = ParticleSystemSimulationSpace.World; // las burbujas se quedan atrás
        main.gravityModifier = -0.15f;                               // suben un poco
        main.maxParticles = 200;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 18f;
        forma.radius = 0.15f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.maxParticleSize = 0.03f;   // con la cámara muy cerca (túnel, pegado al techo) no tapan la pantalla
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (materialBurbujas != null) r.sharedMaterial = materialBurbujas;
        else if (s != null)
        {
            Material m = new Material(s);
            m.SetFloat("_Surface", 1f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
            m.SetColor("_BaseColor", new Color(0.85f, 0.95f, 1f, 0.6f));
            r.sharedMaterial = m;
        }
        ps.Play();
        return ps;
    }

    static Material Material(Color c)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        Material m = new Material(s != null ? s : Shader.Find("Standard"));
        m.SetColor("_BaseColor", c);
        m.color = c;
        return m;
    }
}
