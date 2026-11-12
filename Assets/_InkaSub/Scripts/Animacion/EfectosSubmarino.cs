using System.Collections;
using UnityEngine;

// Efectos visuales del submarino (equivalen a las animaciones de Curación, Recarga y Daño del 2D):
//   Curacion()          -> destello verde, chispas que suben y un "latido" del casco
//   InicioRecarga(seg)  -> anillo de burbujas azules girando alrededor mientras recarga
//   FinRecarga()        -> pequeño salto y destello blanco ("¡clac!, listo")
//   Daño()              -> destello rojo, sacudida del modelo y de la cámara
//   Disparo(punto)      -> fogonazo verde en la boca del cañón
// Los llaman SubmarinoSalud3D y SubmarinoDisparo3D.
public class EfectosSubmarino : MonoBehaviour
{
    [Tooltip("Material de partículas (transparente). Si está vacío se crea uno")]
    public Material materialParticulas;

    private Transform modelo;
    private Vector3 escalaBase, posicionBase;
    private Light luz;
    private ParticleSystem chispasCuracion, anilloRecarga;
    private Coroutine rutinaCuerpo, rutinaLuz;
    private CamaraSeguimiento3D camara;
    private bool recargando;
    private float anguloAnillo, porEmitir;

    void Update()
    {
        if (!recargando) return;
        // Burbujas dando vueltas alrededor del submarino (como una barra de carga).
        // Se cuentan por segundo (no por cuadro) para que se vea igual en cualquier computadora.
        anguloAnillo += 540f * Time.deltaTime;
        porEmitir += 100f * Time.deltaTime;
        int cantidad = (int)porEmitir;
        porEmitir -= cantidad;
        for (int i = 0; i < cantidad; i++)
        {
            float a = (anguloAnillo + (i % 2) * 180f) * Mathf.Deg2Rad;
            var p = new ParticleSystem.EmitParams
            {
                position = transform.position + (Vector3.right * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * 1.6f,
                velocity = Vector3.zero,
                startSize = 0.28f,
                startLifetime = 0.45f,
                startColor = new Color(0.55f, 0.85f, 1f, 1f),
                applyShapeToPosition = false
            };
            anilloRecarga.Emit(p, 1);
        }
    }

    void Start()
    {
        modelo = transform.Find("Modelo");
        if (modelo == null) modelo = transform;
        escalaBase = modelo.localScale;
        posicionBase = modelo.localPosition;
        if (materialParticulas == null) materialParticulas = LinternaSubmarino.MaterialAditivo();

        GameObject l = new GameObject("LuzEfectos");
        l.transform.SetParent(transform, false);
        luz = l.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 6f;
        luz.intensity = 0f;

        chispasCuracion = CrearChispas();
        anilloRecarga = CrearAnillo();
        // Quedan "encendidos" pero sin emitir solos: así las partículas que se emiten a mano
        // con Emit() sí se mueven y se dibujan (con el sistema detenido no aparecen)
        chispasCuracion.Play();
        anilloRecarga.Play();
        if (Camera.main != null) camara = Camera.main.GetComponent<CamaraSeguimiento3D>();
    }

    // ---------------------------------------------------------------- Para llamar desde otros scripts

    public void Curacion()
    {
        chispasCuracion.Emit(30);
        Destello(new Color(0.3f, 1f, 0.5f), 6f, 0.6f);
        Cuerpo(Latido(1.18f, 0.35f));
    }

    // El dash esquivó un golpe: destello celeste (sin daño) para que se note que funcionó
    private float ultimoEsquive = -10f;
    public void Esquive()
    {
        if (Time.time - ultimoEsquive < 0.3f) return;
        ultimoEsquive = Time.time;
        Destello(new Color(0.4f, 0.95f, 1f), 7f, 0.35f);
        Cuerpo(Latido(1.12f, 0.25f));
    }

    public void InicioRecarga(float duracion)
    {
        // Las burbujas se generan en Update mientras dure la recarga
        recargando = true;
    }

    public void FinRecarga()
    {
        recargando = false;
        Destello(new Color(0.8f, 0.95f, 1f), 3f, 0.25f);
        Cuerpo(Salto());
    }

    public void Daño()
    {
        Destello(new Color(1f, 0.2f, 0.15f), 5f, 0.3f);
        Cuerpo(Sacudida(0.12f, 0.25f));
        if (camara != null) camara.Sacudir(0.25f, 0.2f);
    }

    public void Disparo(Vector3 punto)
    {
        StartCoroutine(Fogonazo(punto));
    }

    // ---------------------------------------------------------------- Animaciones

    void Cuerpo(IEnumerator rutina)
    {
        if (rutinaCuerpo != null) StopCoroutine(rutinaCuerpo);
        modelo.localScale = escalaBase;
        modelo.localPosition = posicionBase;
        rutinaCuerpo = StartCoroutine(rutina);
    }

    IEnumerator Latido(float escala, float duracion)
    {
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float k = Mathf.Sin(t / duracion * Mathf.PI);
            modelo.localScale = escalaBase * Mathf.Lerp(1f, escala, k);
            yield return null;
        }
        modelo.localScale = escalaBase;
    }

    IEnumerator Salto()
    {
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            modelo.localPosition = posicionBase + Vector3.up * Mathf.Sin(t / 0.25f * Mathf.PI) * 0.25f;
            yield return null;
        }
        modelo.localPosition = posicionBase;
    }

    IEnumerator Sacudida(float fuerza, float duracion)
    {
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            modelo.localPosition = posicionBase + Random.insideUnitSphere * fuerza * (1f - t / duracion);
            yield return null;
        }
        modelo.localPosition = posicionBase;
    }

    void Destello(Color color, float intensidad, float duracion)
    {
        if (rutinaLuz != null) StopCoroutine(rutinaLuz);
        rutinaLuz = StartCoroutine(RutinaDestello(color, intensidad, duracion));
    }

    IEnumerator RutinaDestello(Color color, float intensidad, float duracion)
    {
        luz.color = color;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            luz.intensity = intensidad * (1f - t / duracion);
            yield return null;
        }
        luz.intensity = 0f;
    }

    IEnumerator Fogonazo(Vector3 punto)
    {
        GameObject go = new GameObject("Fogonazo");
        go.transform.position = punto;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.6f, 1f, 0.5f);
        l.range = 4f;
        for (float t = 0f; t < 0.08f; t += Time.deltaTime)
        {
            l.intensity = 4f * (1f - t / 0.08f);
            yield return null;
        }
        Destroy(go);
    }

    // ---------------------------------------------------------------- Partículas

    ParticleSystem CrearChispas()
    {
        ParticleSystem ps = NuevoSistema("ChispasCuracion");
        var main = ps.main;
        main.startLifetime = 0.9f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new Color(0.4f, 1f, 0.55f, 1f);
        main.gravityModifier = -0.6f; // suben, como burbujas de oxígeno
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 1.2f;
        var emision = ps.emission;
        emision.rateOverTime = 0f;
        return ps;
    }

    ParticleSystem CrearAnillo()
    {
        ParticleSystem ps = NuevoSistema("AnilloRecarga");
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 0.6f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
        main.startColor = new Color(0.5f, 0.85f, 1f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emisionAnillo = ps.emission;
        emisionAnillo.rateOverTime = 0f; // se emite a mano en Update
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Circle;
        forma.radius = 1.5f;
        forma.radiusThickness = 0f;
        forma.arcMode = ParticleSystemShapeMultiModeValue.Loop; // las burbujas "dan la vuelta"
        forma.arcSpeed = 2f;
        var tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        return ps;
    }

    ParticleSystem NuevoSistema(string nombre)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.maxParticles = 300;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate; // que no se "congelen" si la cámara no los ve un momento
        var rp = go.GetComponent<ParticleSystemRenderer>();
        rp.sharedMaterial = materialParticulas;
        rp.maxParticleSize = 0.15f;   // con la cámara pegada al submarino no tapan la pantalla
        return ps;
    }
}
