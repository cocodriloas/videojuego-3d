using System.Collections;
using UnityEngine;

// Ataques del Iceberg de Basura (versión 3D de JefeAtaques del juego 2D):
//   LLUVIA de bolas de basura que caen del techo, ABANICO de microplásticos y TENTÁCULOS que salen del
//   jefe o del piso y le pegan al submarino donde está (1, 2 y 3 a la vez en cada fase; avisan con una marca roja).
//   Cada fase ataca más rápido. Mientras cambia de fase no ataca.
// Antes de cada ataque el jefe avisa (brilla naranja y suena una alarma). Nunca repite el mismo
// ataque dos veces seguidas. Va en el mismo objeto que JefeIceberg3D.
[RequireComponent(typeof(JefeIceberg3D))]
public class JefeAtaques3D : MonoBehaviour
{
    [Header("Ritmo")]
    public float pausaInicial = 2f;
    public float[] pausaEntreAtaquesPorFase = { 3.5f, 2.8f, 2f };
    public float tiempoAviso = 1f;

    [Header("Lluvia de bolas (en filas, con un hueco seguro)")]
    public int[] filasLluviaPorFase = { 2, 3, 3 };
    [Tooltip("Entre qué x cae la lluvia (el interior de la sala)")]
    public Vector2 rangoLluviaMundo = new Vector2(189.5f, 225f);
    [Tooltip("Metros entre una bola y la siguiente de la misma fila")]
    public float separacionBolas = 3.2f;   // con bolas de 1.4 m queda 1.8 m entre ellas
    [Tooltip("Ancho del hueco seguro que queda cerca del submarino en cada fila")]
    public float anchoHueco = 4.2f;
    public float segundosEntreFilas = 1.6f;
    public float alturaLluvia = 11f;
    public float altoSuelo = -12f;
    public float velocidadLluvia = 4f;
    [Tooltip("Segundos que cuelga cada bola del techo, temblando, antes de caer")]
    public float avisoBola = 0.7f;
    public float diametroBola = 1.4f;
    public Material materialBola;

    [Header("Abanico de microplásticos")]
    public int[] cantidadAbanicoPorFase = { 3, 5, 7 };
    public float anguloAbanico = 60f;
    public GameObject prefabMicroplastico;
    public Vector3 origenAbanico = new Vector3(-5f, 0f, 0f);

    [Header("Tentáculos (1, 2 y 3 a la vez según la fase)")]
    public int[] tentaculosPorFase = { 1, 2, 3 };
    [Tooltip("Entre qué x pueden salir del piso (el interior de la sala)")]
    public Vector2 limitesSalaX = new Vector2(189f, 199f);
    public bool tentaculoActivo = true;
    [Tooltip("Alturas posibles del latigazo (respecto al jefe): elige la más cercana al submarino")]
    public float[] alturasTentaculo = { -6f, -2f, 2f, 6f };
    public Material materialTentaculo;
    public Material materialAviso;

    private JefeIceberg3D jefe;
    private Transform jugador;
    private int ultimoAtaque = -1;

    void Start()
    {
        jefe = GetComponent<JefeIceberg3D>();
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;
    }

    // JefeIceberg3D arranca y para los ataques (así se reinician bien si el submarino pierde)
    private Coroutine rutina;
    void Update()
    {
        if (jefe.EstaPeleando && rutina == null) rutina = StartCoroutine(RutinaAtaques());
        if (!jefe.EstaPeleando && rutina != null) { StopCoroutine(rutina); rutina = null; }
    }

    IEnumerator RutinaAtaques()
    {
        yield return new WaitForSeconds(pausaInicial);
        while (jefe.EstaPeleando)
        {
            // Mientras cambia de fase (ruge, se rompe y está protegido) no ataca
            while (jefe.EnTransicion && jefe.EstaPeleando) yield return null;
            if (!jefe.EstaPeleando) break;
            int ataque = ElegirAtaque();
            ultimoAtaque = ataque;
            if (ataque == 0) yield return StartCoroutine(Lluvia());
            else if (ataque == 1) yield return StartCoroutine(Abanico());
            else yield return StartCoroutine(Tentaculo());
            yield return new WaitForSeconds(Mathf.Max(0.5f, Valor(pausaEntreAtaquesPorFase, 3f)));
        }
        rutina = null;
    }

    // Fase 1: lluvia o abanico. Fase 2: + tentáculo. Fase 3: los tres, con el tentáculo más seguido.
    // Nunca repite el mismo ataque dos veces seguidas.
    int ElegirAtaque()
    {
        int fase = Mathf.Clamp(jefe.FaseActual, 1, 3);
        bool conTentaculo = tentaculoActivo;
        int a;
        if (!conTentaculo) a = Random.Range(0, 2);
        else if (fase == 3) a = Random.value < 0.5f ? 2 : Random.Range(0, 2);
        else if (fase == 2) a = Random.Range(0, 3);
        else a = Random.value < 0.25f ? 2 : Random.Range(0, 2);   // fase 1: el tentáculo de vez en cuando
        if (a == ultimoAtaque) a = conTentaculo ? (a + 1) % 3 : 1 - Mathf.Min(a, 1);
        return a;
    }

    IEnumerator Avisar()
    {
        jefe.Avisar(tiempoAviso);
        AudioManager.Sonar(Sonido.AvisoEnemigo);
        yield return new WaitForSeconds(tiempoAviso);
    }

    // ---------------------------------------------------------------- 1) Lluvia

    // Cada "ola" de la lluvia elige al azar una de 3 formas (nunca la misma dos veces seguidas):
    //   0) FILA con un hueco seguro (el hueco cae a un lado u otro del submarino, no siempre encima)
    //   1) CASCADA: las bolas caen una tras otra barriendo la sala de izquierda a derecha o al revés
    //   2) DISPERSA: bolas sueltas en sitios al azar, algunas apuntadas cerca del submarino
    // Solo cae DELANTE del jefe (entre la pared izquierda y su cara), nunca detrás de él.
    // Cada bola cuelga un momento del techo temblando (aviso) antes de caer: se ve venir y se esquiva.
    private int ultimaForma = -1;

    IEnumerator Lluvia()
    {
        yield return StartCoroutine(Avisar());
        int olas = Mathf.Max(1, Valor(filasLluviaPorFase, 2));
        AudioManager.Sonar(Sonido.DisparoEnemigo);
        jefe.Sacudir(olas * segundosEntreFilas + 0.6f, 0.08f);
        for (int f = 0; f < olas && jefe.EstaPeleando; f++)
        {
            int forma = Random.Range(0, 3);
            if (forma == ultimaForma) forma = (forma + Random.Range(1, 3)) % 3;
            ultimaForma = forma;
            if (forma == 0) Fila();
            else if (forma == 1) StartCoroutine(Cascada());
            else Dispersa();
            yield return new WaitForSeconds(segundosEntreFilas * Random.Range(0.85f, 1.25f));
        }
    }

    // Borde derecho de la lluvia: la cara del jefe (lo que hay detrás no se ve ni importa)
    float BordeDerecho()
    {
        float derecha = rangoLluviaMundo.y;
        // Con la forma de la caja del cuerpo (sirve aunque el collider esté apagado al cambiar de fase)
        BoxCollider c = GetComponent<BoxCollider>();
        if (c != null) derecha = Mathf.Min(derecha, transform.TransformPoint(c.center).x - c.size.x * 0.5f * transform.lossyScale.x - 0.6f);
        return Mathf.Max(rangoLluviaMundo.x + 6f, derecha);
    }

    float XJugador => jugador != null ? jugador.position.x : (rangoLluviaMundo.x + BordeDerecho()) * 0.5f;

    void Fila()
    {
        float izq = rangoLluviaMundo.x, der = BordeDerecho();
        // El hueco queda cerca del submarino pero corrido a un lado: hay que moverse un poco
        float lado = Random.value < 0.5f ? -1f : 1f;
        float hueco = Mathf.Clamp(XJugador + lado * Random.Range(0.8f, 3.2f), izq + 1.5f, der - 1.5f);
        float ancho = anchoHueco * Random.Range(0.9f, 1.15f);
        float x = izq + Random.Range(0f, separacionBolas * 0.6f);
        while (x <= der)
        {
            if (Mathf.Abs(x - hueco) >= ancho * 0.5f)
                LanzarBola(new Vector3(x, transform.position.y + alturaLluvia, transform.position.z), Random.Range(0f, 0.3f));
            x += separacionBolas * Random.Range(0.85f, 1.25f);   // separación irregular: no parece una regla
        }
    }

    IEnumerator Cascada()
    {
        float izq = rangoLluviaMundo.x, der = BordeDerecho();
        bool haciaDerecha = Random.value < 0.5f;
        float paso = separacionBolas * Random.Range(0.75f, 1f);
        int n = Mathf.CeilToInt((der - izq) / paso);
        for (int i = 0; i <= n && jefe.EstaPeleando; i++)
        {
            float x = haciaDerecha ? izq + i * paso : der - i * paso;
            LanzarBola(new Vector3(Mathf.Clamp(x + Random.Range(-0.4f, 0.4f), izq, der), transform.position.y + alturaLluvia, transform.position.z), 0f);
            yield return new WaitForSeconds(Random.Range(0.1f, 0.18f));
        }
    }

    void Dispersa()
    {
        float izq = rangoLluviaMundo.x, der = BordeDerecho();
        int cantidad = Random.Range(6, 10) + jefe.FaseActual * 2;
        var usadas = new System.Collections.Generic.List<float>();
        for (int intento = 0; usadas.Count < cantidad && intento < cantidad * 8; intento++)
        {
            // Un tercio apuntadas cerca del submarino, el resto por toda la sala
            float x = usadas.Count < cantidad / 3 ? XJugador + Random.Range(-3.5f, 3.5f) : Random.Range(izq, der);
            x = Mathf.Clamp(x, izq, der);
            if (usadas.Exists(u => Mathf.Abs(u - x) < diametroBola * 1.6f)) continue;
            usadas.Add(x);
            LanzarBola(new Vector3(x, transform.position.y + alturaLluvia + Random.Range(-0.5f, 0.5f), transform.position.z), Random.Range(0f, 1.1f));
        }
    }

    void LanzarBola(Vector3 pos, float retraso)
    {
        GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        b.name = "BolaBasura";
        b.transform.position = pos;
        b.transform.localScale = Vector3.one * diametroBola * Random.Range(0.8f, 1.2f);
        b.transform.rotation = Random.rotation;
        if (materialBola != null) b.GetComponent<Renderer>().sharedMaterial = materialBola;
        b.GetComponent<SphereCollider>().isTrigger = true;
        Rigidbody rb = b.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        AtaqueQueDania d = b.AddComponent<AtaqueQueDania>();
        d.velocidad = Vector3.zero;   // primero cuelga del techo (aviso)
        d.giro = Random.onUnitSphere * 120f;
        d.alturaSuelo = transform.position.y + altoSuelo;
        d.burbujasAlTocarSuelo = true;
        StartCoroutine(Soltar(d, retraso));
    }

    IEnumerator Soltar(AtaqueQueDania d, float retraso)
    {
        Vector3 inicio = d.transform.position;
        // Cada bola tiembla un tiempo distinto: no caen todas a la vez
        float espera = avisoBola + retraso;
        for (float t = 0f; t < espera && d != null; t += Time.deltaTime)
        {
            d.transform.position = inicio + Random.insideUnitSphere * 0.08f;   // tiembla: está por caer
            yield return null;
        }
        if (d != null) d.velocidad = Vector3.down * velocidadLluvia * Random.Range(0.85f, 1.3f);
    }

    // ---------------------------------------------------------------- 2) Abanico

    IEnumerator Abanico()
    {
        yield return StartCoroutine(Avisar());
        if (!jefe.EstaPeleando || jugador == null || prefabMicroplastico == null) yield break;

        int cantidad = Mathf.Max(1, Valor(cantidadAbanicoPorFase, 3));
        Vector3 origen = transform.TransformPoint(origenAbanico);
        Vector3 hacia = jugador.position - origen;
        hacia.z = 0f; // en el plano del juego
        float central = Mathf.Atan2(hacia.y, hacia.x) * Mathf.Rad2Deg;

        AudioManager.Sonar(Sonido.DisparoEnemigo);
        for (int i = 0; i < cantidad; i++)
        {
            float reparto = cantidad == 1 ? 0f : (i / (float)(cantidad - 1)) - 0.5f;
            float rad = (central + reparto * anguloAbanico) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Instantiate(prefabMicroplastico, origen, Quaternion.LookRotation(dir, Vector3.up));
        }
    }

    // ---------------------------------------------------------------- 3) Tentáculo

    // Uno, dos o tres tentáculos (según la fase) que salen del jefe o del piso cerca del submarino
    // y le pegan donde está (cada uno con un pequeño desvío, para que haya que moverse)
    IEnumerator Tentaculo()
    {
        if (jugador == null) yield break;
        jefe.Avisar(1f);
        AudioManager.Sonar(Sonido.AvisoEnemigo);
        int cuantos = Mathf.Clamp(Valor(tentaculosPorFase, 1), 1, 4);
        var activos = new System.Collections.Generic.List<TentaculoJefe>();
        for (int i = 0; i < cuantos; i++)
        {
            bool delPiso = i % 2 == 1;
            Vector3 baseT, afuera;
            if (delPiso)
            {
                // Sale del piso, cerca (pero no debajo) del submarino
                float x = Mathf.Clamp(jugador.position.x + (Random.value < 0.5f ? -1f : 1f) * Random.Range(3f, 6f), limitesSalaX.x, limitesSalaX.y);
                baseT = new Vector3(x, transform.position.y + altoSuelo - 1f, transform.position.z);
                afuera = Vector3.up;
            }
            else
            {
                float a = alturasTentaculo[Random.Range(0, alturasTentaculo.Length)];
                baseT = transform.position + new Vector3(-3.5f, a, 0f);
                afuera = Vector3.left;
            }
            TentaculoJefe t = TentaculoJefe.Crear(jefe, baseT, afuera, materialTentaculo, materialAviso);
            Vector3 desvio = i == 0 ? Vector3.zero : (Vector3)(Random.insideUnitCircle * 1.6f);
            desvio.z = 0f;
            t.StartCoroutine(t.Atacar(jugador, desvio));
            activos.Add(t);
            yield return new WaitForSeconds(0.35f);   // no todos a la vez: se pueden esquivar uno por uno
        }
        // Espera a que vuelvan todos (o a que la pelea se corte)
        while (jefe.EstaPeleando && activos.Exists(t => t != null && !t.Terminado)) yield return null;
    }

    // ---------------------------------------------------------------- Utilidades

    // Nube de burbujas (también la usa el jefe al morir)
    public static void Burbujas(Vector3 pos, int cantidad, float radio)
    {
        GameObject go = new GameObject("Burbujas");
        go.transform.position = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false;
        main.duration = 0.3f;
        main.startLifetime = 1.2f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
        main.gravityModifier = -0.4f;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)cantidad) });
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = radio;
        Material m = MaterialBurbujas;
        if (m != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = m;
        ps.Play();
        Object.Destroy(go, 2.5f);
    }

    // Lo asigna el constructor del nivel (material de partículas con textura de burbuja)
    public static Material MaterialBurbujas;
    [Tooltip("Material de partículas para las burbujas (se comparte con todos los ataques)")]
    public Material materialBurbujas;
    void Awake() { if (materialBurbujas != null) MaterialBurbujas = materialBurbujas; }

    float Valor(float[] l, float d) => l == null || l.Length == 0 ? d : l[Mathf.Clamp(jefe.FaseActual - 1, 0, l.Length - 1)];
    int Valor(int[] l, int d) => l == null || l.Length == 0 ? d : l[Mathf.Clamp(jefe.FaseActual - 1, 0, l.Length - 1)];
}
