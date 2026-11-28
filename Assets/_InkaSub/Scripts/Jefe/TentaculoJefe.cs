using System.Collections;
using UnityEngine;

// Un tentáculo del Iceberg de Basura: una cadena de esferas que ondula como un pulpo.
//   1) Sale (del jefe o del piso), se enrosca y avisa con una marca roja donde está el submarino.
//   2) Latigazo: la punta va rápido a esa marca (solo daña en este momento).
//   3) Se retuerce un instante y vuelve a esconderse.
// La marca roja queda fija donde estaba el submarino y se intensifica hasta el golpe (hay tiempo para el dash).
// Si le disparas 4 veces mientras está afuera, se retira antes. Lo crea JefeAtaques3D.
public class TentaculoJefe : MonoBehaviour, IDañable
{
    public int segmentos = 24;
    public float grosorBase = 1.5f, grosorPunta = 0.35f;
    public float tiempoAviso = 1.5f, tiempoGolpe = 0.28f, tiempoQuieto = 0.55f, tiempoVuelta = 0.6f;
    public int golpesParaRetirarlo = 4;

    private Transform[] piezas;
    private SphereCollider[] zonas;
    private Vector3 baseT, afuera, punta;
    private float curva, fase;
    private int golpes;
    private bool retirarYa;
    private JefeIceberg3D jefe;
    private Transform marca, nucleo;
    private Renderer rMarca, rNucleo;
    private Light luzMarca;
    private MaterialPropertyBlock bloque;

    public bool Terminado { get; private set; }

    public static TentaculoJefe Crear(JefeIceberg3D jefe, Vector3 baseT, Vector3 haciaAfuera, Material material, Material aviso)
    {
        GameObject go = new GameObject("Tentaculo");
        go.tag = "Enemigo";
        go.transform.position = baseT;
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        TentaculoJefe t = go.AddComponent<TentaculoJefe>();
        t.jefe = jefe;
        t.baseT = baseT;
        t.afuera = haciaAfuera.normalized;
        t.curva = Random.Range(-1f, 1f) < 0f ? -1f : 1f;
        t.fase = Random.value * 6f;
        t.ArmarPiezas(material, aviso);
        return t;
    }

    void ArmarPiezas(Material material, Material aviso)
    {
        piezas = new Transform[segmentos];
        zonas = new SphereCollider[segmentos];
        for (int i = 0; i < segmentos; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = "Pieza" + i;
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(transform, false);
            if (material != null) p.GetComponent<Renderer>().sharedMaterial = material;
            piezas[i] = p.transform;
            SphereCollider z = gameObject.AddComponent<SphereCollider>();
            z.isTrigger = true;   // siempre activas: así se le puede disparar; solo daña durante el latigazo
            zonas[i] = z;
        }
        // Marca roja en el piso del ataque (un disco que late)
        GameObject m = GameObject.CreatePrimitive(PrimitiveType.Quad);
        m.name = "AvisoTentaculo";
        Destroy(m.GetComponent<Collider>());
        if (aviso != null) m.GetComponent<Renderer>().sharedMaterial = aviso;
        marca = m.transform;
        rMarca = m.GetComponent<Renderer>();
        // Núcleo que se llena: cuando cubre toda la marca, llega el latigazo (se ve cuánto falta)
        GameObject n = GameObject.CreatePrimitive(PrimitiveType.Quad);
        n.name = "Nucleo";
        Destroy(n.GetComponent<Collider>());
        if (aviso != null) n.GetComponent<Renderer>().sharedMaterial = aviso;
        n.transform.SetParent(marca, false);
        n.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        nucleo = n.transform;
        rNucleo = n.GetComponent<Renderer>();
        // Luz roja que se enciende cada vez más fuerte sobre la zona del golpe
        GameObject l = new GameObject("Luz aviso");
        l.transform.SetParent(marca, false);
        l.transform.localPosition = new Vector3(0f, 0f, -0.6f);
        luzMarca = l.AddComponent<Light>();
        luzMarca.type = LightType.Point;
        luzMarca.color = new Color(1f, 0.12f, 0.08f);
        luzMarca.range = 7f;
        luzMarca.intensity = 0f;
        luzMarca.renderingLayerMask = CapasLuz.Todas;
        bloque = new MaterialPropertyBlock();
        marca.gameObject.SetActive(false);
        punta = baseT;
        Dibujar(0f);
    }

    // Ataca al punto "objetivo" (posición del submarino). seguir = sigue al submarino durante parte del aviso
    public IEnumerator Atacar(Transform seguir, Vector3 desvio)
    {
        Vector3 objetivo = seguir != null ? seguir.position + desvio : baseT + afuera * 10f;
        Vector3 inicio = baseT + afuera * 0.5f;

        // 1) Sale y se enrosca apuntando, mientras la marca avisa dónde va a pegar
        marca.gameObject.SetActive(true);
        for (float t = 0f; t < tiempoAviso && !retirarYa; t += Time.deltaTime)
        {
            if (!jefe.EstaPeleando) { Fin(); yield break; }
            // La marca se FIJA casi al instante donde está el submarino y ya no lo sigue:
            // así se sabe dónde va a pegar y da tiempo de salir de ahí (o usar el dash)
            if (seguir != null && t < 0.12f) objetivo = seguir.position + desvio;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.45f));
            Vector3 preparado = baseT + (objetivo - baseT).normalized * 3.5f + Vector3.up * 1.5f;
            punta = Vector3.Lerp(inicio, preparado, k) + Random.insideUnitSphere * 0.08f;
            MoverMarca(objetivo, t);
            Dibujar(0.6f + Mathf.Sin(t * 10f) * 0.15f);
            yield return null;
        }
        marca.gameObject.SetActive(false);

        // 2) Latigazo hacia la marca (aquí sí daña)
        if (!retirarYa)
        {
            jefe.Sonar(jefe.latigazo);
            if (jefe.latigazo == null) AudioManager.Sonar(Sonido.Garra);
            Danino(true);
            Vector3 desde = punta;
            for (float t = 0f; t < tiempoGolpe && !retirarYa; t += Time.deltaTime)
            {
                if (!jefe.EstaPeleando) { Fin(); yield break; }
                float k = 1f - Mathf.Pow(1f - t / tiempoGolpe, 3f);
                punta = Vector3.Lerp(desde, objetivo, k);
                Dibujar(0.35f);
                yield return null;
            }
            jefe.Sacudir(0.3f, 0.2f);
            JefeAtaques3D.Burbujas(objetivo, 18, 0.8f);
            // Se retuerce un momento donde pegó
            for (float t = 0f; t < tiempoQuieto && !retirarYa; t += Time.deltaTime)
            {
                if (!jefe.EstaPeleando) { Fin(); yield break; }
                punta = objetivo + new Vector3(Mathf.Sin(t * 20f), Mathf.Cos(t * 17f), 0f) * 0.25f;
                Dibujar(0.5f);
                yield return null;
            }
        }
        Danino(false);

        // 3) Vuelve a esconderse
        Vector3 ultimo = punta;
        for (float t = 0f; t < tiempoVuelta; t += Time.deltaTime)
        {
            if (!jefe.EstaPeleando) break;
            punta = Vector3.Lerp(ultimo, baseT, Mathf.SmoothStep(0f, 1f, t / tiempoVuelta));
            Dibujar(0.8f);
            yield return null;
        }
        Fin();
    }

    void Fin()
    {
        Terminado = true;
        if (marca != null) Destroy(marca.gameObject);
        Destroy(gameObject);
    }

    private bool danino;
    void Danino(bool si) { danino = si; }

    void OnTriggerEnter(Collider otro) { if (danino) Golpear(otro); }
    void OnTriggerStay(Collider otro) { if (danino) Golpear(otro); }

    void Golpear(Collider otro)
    {
        if (!otro.CompareTag("Player")) return;
        SubmarinoSalud3D salud = otro.GetComponent<SubmarinoSalud3D>();
        if (salud != null) salud.RecibirDaño(1);   // (después de un golpe el submarino es invulnerable un momento)
    }

    // La marca roja se queda quieta y se vuelve cada vez más intensa: late más rápido, brilla más,
    // su núcleo se llena y la luz roja crece. Cuando está llena, llega el latigazo.
    void MoverMarca(Vector3 objetivo, float t)
    {
        Camera cam = Camera.main;
        marca.position = objetivo + (cam != null ? (cam.transform.position - objetivo).normalized * 0.5f : Vector3.back * 0.5f);
        if (cam != null) marca.rotation = cam.transform.rotation;
        float k = Mathf.Clamp01(t / tiempoAviso);
        float latido = Mathf.Sin(t * Mathf.Lerp(5f, 26f, k * k));   // late cada vez más rápido
        marca.localScale = Vector3.one * (3.4f + latido * 0.15f * (1f + k));
        nucleo.localScale = Vector3.one * Mathf.Lerp(0.1f, 0.95f, k);
        float brillo = Mathf.Lerp(0.35f, 1.6f, k) * (0.85f + latido * 0.15f);
        rMarca.GetPropertyBlock(bloque);
        bloque.SetColor("_BaseColor", new Color(1f * brillo, 0.15f * brillo, 0.1f * brillo, Mathf.Lerp(0.45f, 1f, k)));
        rMarca.SetPropertyBlock(bloque);
        bloque.SetColor("_BaseColor", new Color(1.2f * brillo, 0.25f * brillo, 0.12f * brillo, Mathf.Lerp(0.3f, 0.9f, k)));
        rNucleo.SetPropertyBlock(bloque);
        luzMarca.intensity = Mathf.Lerp(0.5f, 7f, k * k) * (0.8f + latido * 0.2f);
    }

    // Coloca las piezas a lo largo de una curva de la base a la punta, con una onda que recorre el tentáculo
    void Dibujar(float ondulacion)
    {
        Vector3 eje = punta - baseT;
        float largo = Mathf.Max(0.5f, eje.magnitude);
        Vector3 dir = eje / largo;
        Vector3 perp = Vector3.Cross(dir, Vector3.forward).normalized;
        if (perp.sqrMagnitude < 0.01f) perp = Vector3.up;
        Vector3 control = baseT + eje * 0.45f + perp * curva * largo * 0.25f;
        float tiempo = Time.time * 7f + fase;
        for (int i = 0; i < segmentos; i++)
        {
            float s = i / (float)(segmentos - 1);
            // Curva de Bézier (base -> control -> punta)
            Vector3 p = (1f - s) * (1f - s) * baseT + 2f * (1f - s) * s * control + s * s * punta;
            p += perp * Mathf.Sin(tiempo - s * 7f) * ondulacion * s;
            float grosor = Mathf.Lerp(grosorBase, grosorPunta, s) * (1f + Mathf.Sin(s * 40f) * 0.06f);
            piezas[i].position = p;
            piezas[i].localScale = Vector3.one * grosor;
            zonas[i].center = transform.InverseTransformPoint(p);
            zonas[i].radius = grosor * 0.45f;
        }
    }

    // Las balas del submarino: con varios golpes se retira antes
    public void RecibirDaño(int cantidad)
    {
        if (Terminado || retirarYa) return;
        golpes += cantidad;
        JefeAtaques3D.Burbujas(punta, 6, 0.4f);
        if (golpes >= golpesParaRetirarlo)
        {
            retirarYa = true;
            AudioManager.Sonar(Sonido.EnemigoGolpeado);
            JefeAtaques3D.Burbujas(punta, 25, 1f);
        }
    }

    void OnDestroy()
    {
        if (marca != null) Destroy(marca.gameObject);
    }
}
