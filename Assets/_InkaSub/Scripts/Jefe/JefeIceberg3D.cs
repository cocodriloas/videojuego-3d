using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// El Iceberg de Basura (jefe final), versión 3D del JefeIceberg del juego 2D.
// - Sube desde el fondo cuando se llama a IniciarPelea() (lo hace SalaJefeDisparador3D).
// - Tiene 4 ojos débiles en su lado izquierdo: SOLO los ojos ABIERTOS (rojos y brillantes) le hacen daño.
//   El cuerpo y los ojos cerrados detienen las balas sin daño.
// - Cambia de fase al 66% y al 33% de vida (ataques: JefeAtaques3D, refuerzos: JefeRefuerzos3D).
//   Al cambiar de fase ruge, cierra los ojos (no recibe daño unos segundos) y se le caen pedazos.
// - Se va DETERIORANDO: se oscurece, se encoge, le salen grietas rojas, pierde burbujas y trozos.
// - Si el submarino pierde durante la pelea, el jefe se esconde y la pelea se puede repetir.
// - Al llegar a 0 se desarma, se hunde y aparece la pantalla de victoria.
public class JefeIceberg3D : MonoBehaviour, IDañable
{
    enum Estado { Oculto, Apareciendo, Peleando, Muerto }

    [Header("Vida y fases")]
    public int vidaMaxima = 400;
    [Range(0f, 1f)] public float umbralFase2 = 0.66f;
    [Range(0f, 1f)] public float umbralFase3 = 0.33f;
    [Tooltip("Segundos que ruge y se protege al cambiar de fase (no recibe daño)")]
    public float duracionTransicion = 3.5f;

    [Header("Deterioro")]
    [Tooltip("Material de los trozos de basura que se le caen")]
    public Material materialTrozo;
    [Tooltip("Cada cuánta vida perdida (fracción) se le cae un trozo")]
    public float trozoCada = 0.03f;

    [Header("Barra de vida (dibujos del 2D)")]
    public Sprite barraFondo, barraRelleno, barraMarco;
    public Font fuenteBarra;

    [Header("Cuerpo y modelo")]
    [Tooltip("El modelo 3D del jefe (hijo). Se hunde, tiembla y se tiñe")]
    public Transform modelo;
    public float amplitudFlotar = 0.3f;
    public float velocidadFlotar = 1.2f;
    [Tooltip("Desde cuánto más abajo sube al aparecer")]
    public float profundidadAparicion = 22f;
    public float duracionAparicion = 3f;

    [Header("Ojos débiles (posición respecto al centro del jefe; su lado izquierdo mira al submarino)")]
    public Vector3[] posicionesOjos =
    {
        new Vector3(-4.3f, 5.5f, 0f), new Vector3(-4.3f, 1.8f, 0f), new Vector3(-4.3f, -2f, 0f), new Vector3(-4.3f, -5.8f, 0f)
    };
    public float radioOjo = 1.3f;
    [Tooltip("Cuántos ojos están abiertos a la vez en la fase 1, 2 y 3")]
    public int[] ojosAbiertosPorFase = { 2, 2, 3 };
    [Tooltip("Cada cuántos segundos cambian los ojos abiertos en la fase 1, 2 y 3")]
    public float[] segundosRotacionPorFase = { 5f, 4f, 3f };
    public Material materialOjo;

    [Header("Sonidos propios del jefe (Audio/Jefe)")]
    public AudioClip risa;
    public AudioClip cambioFase;
    public AudioClip rugidoMuerte;
    public AudioClip golpeOjo;
    public AudioClip latigazo;

    [Header("Victoria")]
    public float segundosHastaVictoria = 4f;
    public string textoDerrotado = "¡ICEBERG DE BASURA DERROTADO!";
    public string textoVictoria = "¡VICTORIA!\nSalvaste el Mar de Grau";
    [TextArea]
    public string textoVictoriaDetalle = "Gracias a las bacterias Ideonella, el plástico del Mar de Grau se está disolviendo.\nLos animales del mar peruano pueden volver a nadar tranquilos.";

    // ---------- Estado ----------
    private Estado estado = Estado.Oculto;
    private int fase = 1, vida;
    private Vector3 posicionBase;
    private readonly List<JefeOjo3D> ojos = new List<JefeOjo3D>();
    private Coroutine rotacionOjos, rutinaAparicion;
    private Collider[] colisionadoresCuerpo;
    private Renderer[] renderers;
    private AudioSource fuente;
    private BarraJefe3D barra;
    private Light luzJefe;
    private bool transicion;
    private float ultimoTrozo = 1f;
    private ParticleSystem fuga;
    private readonly List<Light> grietas = new List<Light>();
    private MaterialPropertyBlock bloqueTinte;

    public bool EstaPeleando => estado == Estado.Peleando;
    public bool EstaMuerto => estado == Estado.Muerto;
    public int FaseActual => fase;
    public int Vida => vida;
    // Cambiando de fase: no ataca ni recibe daño
    public bool EnTransicion => transicion;

    void Awake()
    {
        posicionBase = transform.position;
        if (modelo == null && transform.childCount > 0) modelo = transform.GetChild(0);
        colisionadoresCuerpo = GetComponents<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        luzJefe = GetComponentInChildren<Light>();
        if (luzJefe != null) { colorLuzBase = luzJefe.color; intensidadLuzBase = luzJefe.intensity; }
        if (modelo != null) escalaModeloBase = modelo.localScale.x;
        Esconder();
    }

    // ---------- Para llamar desde fuera ----------

    public void IniciarPelea()
    {
        if (estado != Estado.Oculto) return;
        rutinaAparicion = StartCoroutine(RutinaAparicion());
    }

    // Si el submarino pierde: el jefe se esconde y todo vuelve a como estaba antes de la pelea
    public void Reiniciar()
    {
        if (estado == Estado.Muerto) return;
        StopAllCoroutines();
        foreach (JefeOjo3D o in ojos) if (o != null) Destroy(o.gameObject);
        ojos.Clear();
        if (barra != null) barra.Mostrar(false);
        transform.position = posicionBase;
        estado = Estado.Oculto;
        transicion = false;
        LimpiarDeterioro();
        destellos = 0;
        if (luzJefe != null) { luzJefe.color = colorLuzBase; luzJefe.intensity = intensidadLuzBase; }
        Esconder();
    }

    // Lo llaman los ojos cuando una bala los toca
    public void GolpeEnOjo(JefeOjo3D ojo, int cantidad)
    {
        if (estado != Estado.Peleando || transicion || ojo == null || !ojo.Abierto) return;
        ojo.Destello();
        if (golpeOjo != null) fuente.PlayOneShot(golpeOjo, 0.7f);
        AplicarDaño(cantidad);
    }

    // Las balas que tocan el CUERPO no le hacen daño (solo los ojos abiertos)
    public void RecibirDaño(int cantidad) { }

    public void Sonar(AudioClip clip, float volumen = 1f)
    {
        if (clip != null) fuente.PlayOneShot(clip, volumen);
    }

    public void Sacudir(float duracion, float fuerza)
    {
        CamaraSeguimiento3D cam = Camera.main != null ? Camera.main.GetComponent<CamaraSeguimiento3D>() : null;
        if (cam != null) cam.Sacudir(fuerza, duracion);
    }

    // Brillo de aviso antes de un ataque (la luz del jefe se pone naranja)
    public void Avisar(float duracion)
    {
        StartCoroutine(Destello(new Color(1f, 0.6f, 0.2f), 6f, duracion));
    }

    // ---------- Aparición ----------

    IEnumerator RutinaAparicion()
    {
        estado = Estado.Apareciendo;
        vida = vidaMaxima;
        fase = 1;
        Mostrar(true);
        AudioManager.Sonar(Sonido.AvisoEnemigo);
        Sacudir(duracionAparicion, 0.25f);

        // Sube desde el fondo, temblando
        Vector3 abajo = posicionBase + Vector3.down * profundidadAparicion;
        for (float t = 0f; t < duracionAparicion; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duracionAparicion);
            transform.position = Vector3.Lerp(abajo, posicionBase, k) + Random.insideUnitSphere * 0.1f * (1f - k);
            yield return null;
        }
        transform.position = posicionBase;

        foreach (Collider c in colisionadoresCuerpo) c.enabled = true;
        CrearOjos();
        CrearBarra();
        ultimoTrozo = 1f;
        ActualizarDeterioro();
        Sonar(risa);
        MensajePantalla.Mostrar("¡EL ICEBERG DE BASURA!\nDispara a sus ojos rojos");

        estado = Estado.Peleando;
        rotacionOjos = StartCoroutine(RotarOjos());
    }

    void Update()
    {
        if (estado != Estado.Peleando) return;
        // Flota; en la fase final además se tambalea, ya muy dañado
        float tambaleo = fase == 3 ? Mathf.Sin(Time.time * 2.3f) * 0.25f : 0f;
        transform.position = posicionBase + Vector3.up * Mathf.Sin(Time.time * velocidadFlotar) * amplitudFlotar + Vector3.right * tambaleo;
        // Se encoge de a poco con cada fase
        if (modelo != null)
        {
            float objetivo = escalaModeloBase * (1f - 0.07f * (fase - 1));
            modelo.localScale = Vector3.one * Mathf.MoveTowards(modelo.localScale.x, objetivo, Time.deltaTime * 0.2f);
        }
        // Las grietas rojas parpadean como brasas
        for (int i = 0; i < grietas.Count; i++)
            if (grietas[i] != null) grietas[i].intensity = 2.5f + Mathf.PerlinNoise(Time.time * 4f, i * 3.1f) * 3f;
    }

    // ---------- Ojos ----------

    void CrearOjos()
    {
        ojos.Clear();
        for (int i = 0; i < posicionesOjos.Length; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Ojo_" + (i + 1);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = posicionesOjos[i];
            go.transform.localScale = Vector3.one * radioOjo * 2f;
            go.tag = "Enemigo";
            SphereCollider col = go.GetComponent<SphereCollider>();
            col.isTrigger = true;
            if (materialOjo != null) go.GetComponent<Renderer>().sharedMaterial = materialOjo;

            JefeOjo3D ojo = go.AddComponent<JefeOjo3D>();
            ojo.jefe = this;
            ojo.Cerrar();
            ojos.Add(ojo);
        }
    }

    IEnumerator RotarOjos()
    {
        while (estado == Estado.Peleando)
        {
            AbrirOjosAlAzar();
            yield return new WaitForSeconds(Mathf.Max(0.5f, Valor(segundosRotacionPorFase, 4f)));
        }
    }

    void AbrirOjosAlAzar()
    {
        if (ojos.Count == 0) return;
        List<int> indices = new List<int>();
        for (int i = 0; i < ojos.Count; i++) indices.Add(i);
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        int cuantos = Mathf.Clamp(Valor(ojosAbiertosPorFase, 2), 1, ojos.Count);
        for (int k = 0; k < indices.Count; k++)
            if (ojos[indices[k]] != null) { if (k < cuantos) ojos[indices[k]].Abrir(); else ojos[indices[k]].Cerrar(); }
    }

    // ---------- Daño y fases ----------

    void AplicarDaño(int cantidad)
    {
        vida = Mathf.Max(0, vida - cantidad);
        AudioManager.Sonar(Sonido.EnemigoGolpeado);
        StartCoroutine(Destello(new Color(1f, 0.3f, 0.3f), 4f, 0.08f));
        float p = (float)vida / vidaMaxima;
        if (barra != null) barra.Fijar(p, fase, transicion);

        // Cada tanto se le cae un trozo de basura
        if (ultimoTrozo - p >= trozoCada) { ultimoTrozo = p; Trozos(1); }
        ActualizarDeterioro();

        if (vida <= 0) { StartCoroutine(RutinaDerrota()); return; }

        if (fase == 1 && p <= umbralFase2) CambiarFase(2);
        else if (fase == 2 && p <= umbralFase3) CambiarFase(3);
    }

    void CambiarFase(int nueva)
    {
        fase = nueva;
        StartCoroutine(RutinaTransicion());
    }

    // Ruge, cierra los ojos (escudo: no recibe daño), se le desprenden pedazos y le salen grietas
    IEnumerator RutinaTransicion()
    {
        transicion = true;
        if (rotacionOjos != null) StopCoroutine(rotacionOjos);
        foreach (JefeOjo3D o in ojos) if (o != null) o.Cerrar();
        if (barra != null) barra.Fijar((float)vida / vidaMaxima, fase, true);
        MensajePantalla.Mostrar(fase == 3 ? "¡FASE FINAL!\nEl iceberg se está rompiendo" : "¡FASE " + fase + "!\nSe le caen pedazos");
        Sonar(cambioFase);
        if (risa != null) StartCoroutine(SonarEn(risa, 1.6f));
        Sacudir(duracionTransicion * 0.6f, 0.45f);
        StartCoroutine(Destello(new Color(1f, 0.2f, 0.2f), 9f, 0.6f));

        // Se le desprenden pedazos en dos tandas y aparecen grietas nuevas
        Trozos(10);
        AgregarGrietas(fase == 2 ? 2 : 3);
        ActualizarDeterioro();
        JefeAtaques3D.Burbujas(transform.position, 80, 5f);
        yield return new WaitForSeconds(duracionTransicion * 0.5f);
        if (estado != Estado.Peleando) yield break;
        Trozos(8);
        JefeAtaques3D.Burbujas(transform.position + Vector3.up * 3f, 50, 4f);
        yield return new WaitForSeconds(duracionTransicion * 0.5f);
        if (estado != Estado.Peleando) yield break;

        transicion = false;
        if (barra != null) barra.Fijar((float)vida / vidaMaxima, fase, false);
        rotacionOjos = StartCoroutine(RotarOjos());
    }

    IEnumerator SonarEn(AudioClip clip, float segundos)
    {
        yield return new WaitForSeconds(segundos);
        Sonar(clip);
    }

    // ---------- Derrota y victoria ----------

    IEnumerator RutinaDerrota()
    {
        estado = Estado.Muerto;
        if (rotacionOjos != null) StopCoroutine(rotacionOjos);
        foreach (JefeOjo3D o in ojos) if (o != null) Destroy(o.gameObject);
        ojos.Clear();
        foreach (Collider c in colisionadoresCuerpo) c.enabled = false;
        if (barra != null) barra.Mostrar(false);
        Trozos(25);

        if (rugidoMuerte != null) Sonar(rugidoMuerte, 1f); else AudioManager.Sonar(Sonido.EnemigoMuere);
        MensajePantalla.Mostrar(textoDerrotado);
        MatarEnemigosRestantes();
        Sacudir(2.5f, 0.4f);

        // Explota en burbujas y basura, y se hunde girando
        StartCoroutine(Explosiones());
        Vector3 inicio = transform.position;
        Quaternion rot = transform.rotation;
        for (float t = 0f; t < 3f; t += Time.deltaTime)
        {
            float k = t / 3f;
            transform.position = inicio + Vector3.down * k * k * 18f + Random.insideUnitSphere * 0.2f;
            transform.rotation = rot * Quaternion.Euler(0f, 0f, k * 25f);
            if (modelo != null) modelo.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, k) * escalaModeloBase;
            yield return null;
        }
        Mostrar(false);

        yield return new WaitForSeconds(Mathf.Max(0f, segundosHastaVictoria - 3f));
        AudioManager.TocarJingle(true);
        PantallaVictoria();
    }

    IEnumerator Explosiones()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 p = transform.position + new Vector3(Random.Range(-4f, 4f), Random.Range(-7f, 7f), -1f);
            JefeAtaques3D.Burbujas(p, 40, 4f);
            AudioManager.Sonar(Sonido.EnemigoMuere);
            StartCoroutine(Destello(Color.white, 10f, 0.15f));
            yield return new WaitForSeconds(0.28f);
        }
    }

    void MatarEnemigosRestantes()
    {
        foreach (EnemigoSalud3D e in FindObjectsByType<EnemigoSalud3D>(FindObjectsSortMode.None))
            e.RecibirDaño(9999);
        foreach (ProyectilEnemigo3D p in FindObjectsByType<ProyectilEnemigo3D>(FindObjectsSortMode.None))
            Destroy(p.gameObject);
    }

    void PantallaVictoria()
    {
        if (MenuJuego3D.instancia != null)
        {
            MenuJuego3D.instancia.MostrarVictoria(textoVictoria, textoVictoriaDetalle);
            return;
        }
        Canvas canvas = CanvasHUD();
        if (canvas == null) { Debug.Log(textoVictoria); return; }
        GameObject panel = new GameObject("Victoria", typeof(RectTransform));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0f, 0.08f, 0.14f, 0.75f);
        Text t = Texto(panel.transform, textoVictoria, 90, new Color(1f, 0.92f, 0.45f));
        t.rectTransform.anchoredPosition = new Vector2(0, 60);
        Text sub = Texto(panel.transform, textoVictoriaDetalle, 34, Color.white);
        sub.rectTransform.anchoredPosition = new Vector2(0, -110);
    }

    // El HUD del nivel (no el canvas de los menús de pausa)
    static Canvas CanvasHUD()
    {
        GameObject hud = GameObject.Find("HUD");
        return hud != null ? hud.GetComponent<Canvas>() : FindFirstObjectByType<Canvas>();
    }

    // ---------- Barra de vida (se crea sola en el HUD) ----------

    void CrearBarra()
    {
        if (barra == null)
        {
            Canvas canvas = CanvasHUD();
            if (canvas == null) return;
            barra = canvas.gameObject.AddComponent<BarraJefe3D>();
            barra.fondo = barraFondo; barra.relleno = barraRelleno; barra.marco = barraMarco;
            barra.fuente = fuenteBarra;
            barra.umbrales = new[] { umbralFase2, umbralFase3 };
            barra.Construir(canvas.transform);
        }
        barra.Fijar(1f, 1, false);
        barra.Mostrar(true);
    }

    // ---------- Deterioro (se va rompiendo) ----------

    // Pedazos de basura que se desprenden del cuerpo y caen al fondo
    void Trozos(int cantidad)
    {
        Bounds b = new Bounds(transform.position, new Vector3(6f, 14f, 4f));
        bool primero = true;
        foreach (Renderer r in renderers)
            if (r != null && r.enabled && r is MeshRenderer && r.GetComponent<JefeOjo3D>() == null) { if (primero) { b = r.bounds; primero = false; } else b.Encapsulate(r.bounds); }
        Collider sub = SubmarinoSalud3D.instancia != null ? SubmarinoSalud3D.instancia.GetComponent<Collider>() : null;
        for (int i = 0; i < cantidad; i++)
        {
            GameObject t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            t.name = "Trozo del iceberg";
            t.transform.position = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), transform.position.z - Random.Range(0.5f, 2f));
            t.transform.localScale = new Vector3(Random.Range(0.4f, 1.2f), Random.Range(0.3f, 0.9f), Random.Range(0.4f, 1f));
            t.transform.rotation = Random.rotation;
            if (materialTrozo != null) t.GetComponent<Renderer>().sharedMaterial = materialTrozo;
            Collider c = t.GetComponent<Collider>();
            if (sub != null) Physics.IgnoreCollision(c, sub);   // caen sin estorbar al submarino
            foreach (Collider cuerpo in colisionadoresCuerpo) Physics.IgnoreCollision(c, cuerpo);
            Rigidbody rb = t.AddComponent<Rigidbody>();
            rb.linearDamping = 1.5f; rb.angularDamping = 0.5f;   // bajo el agua caen despacio
            rb.linearVelocity = new Vector3(Random.Range(-4f, 1f), Random.Range(1f, 4f), Random.Range(-1f, 1f));
            rb.angularVelocity = Random.insideUnitSphere * 4f;
            Destroy(t, 9f);
        }
    }

    // Grietas: luces rojas que salen de adentro del cuerpo y parpadean como brasas
    void AgregarGrietas(int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            GameObject g = new GameObject("Grieta");
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(Random.Range(-2.5f, 1.5f), Random.Range(-6f, 6f), -2.5f);
            Light l = g.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.25f, 0.1f);
            l.range = 4f;
            l.renderingLayerMask = CapasLuz.Todas;
            grietas.Add(l);
        }
    }

    // Se oscurece y pierde más burbujas cuanto más dañado está
    void ActualizarDeterioro()
    {
        float d = 1f - (float)vida / Mathf.Max(1, vidaMaxima);
        if (bloqueTinte == null) bloqueTinte = new MaterialPropertyBlock();
        Color tinte = Color.Lerp(Color.white, new Color(0.55f, 0.42f, 0.45f), d);
        foreach (Renderer r in renderers)
        {
            if (r == null || r.GetComponent<JefeOjo3D>() != null || !(r is MeshRenderer)) continue;
            r.GetPropertyBlock(bloqueTinte);
            bloqueTinte.SetColor("_BaseColor", tinte);
            r.SetPropertyBlock(bloqueTinte);
        }
        if (fuga == null) fuga = CrearFuga();
        var em = fuga.emission;
        em.rateOverTime = Mathf.Lerp(2f, 40f, d);
    }

    ParticleSystem CrearFuga()
    {
        GameObject go = new GameObject("Fuga de burbujas");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(-1f, 0f, -1.5f);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = 2.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.gravityModifier = -0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = new Vector3(5f, 13f, 2f);
        if (JefeAtaques3D.MaterialBurbujas != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = JefeAtaques3D.MaterialBurbujas;
        ps.Play();
        return ps;
    }

    void LimpiarDeterioro()
    {
        foreach (Light l in grietas) if (l != null) Destroy(l.gameObject);
        grietas.Clear();
        if (fuga != null) { Destroy(fuga.gameObject); fuga = null; }
        if (bloqueTinte != null)
            foreach (Renderer r in renderers) if (r != null) r.SetPropertyBlock(null);
        if (modelo != null) modelo.localScale = Vector3.one * escalaModeloBase;
    }
    static Text Texto(Transform padre, string contenido, int tamaño, Color color)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1600, 300);
        Text t = go.AddComponent<Text>();
        t.text = contenido;
        t.fontSize = tamaño;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.raycastTarget = false;
        Text cualquiera = FindFirstObjectByType<Text>();
        t.font = cualquiera != null && cualquiera.font != null ? cualquiera.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);
        return t;
    }

    // ---------- Utilidades ----------

    private float escalaModeloBase = 1f;
    private Color colorLuzBase;
    private float intensidadLuzBase;
    private int destellos;

    void Esconder()
    {
        foreach (Collider c in colisionadoresCuerpo) c.enabled = false;
        Mostrar(false);
    }

    void Mostrar(bool visible)
    {
        foreach (Renderer r in renderers) if (r != null) r.enabled = visible;
        if (luzJefe != null) luzJefe.enabled = visible;
        if (visible && modelo != null) modelo.localScale = Vector3.one * escalaModeloBase;
    }

    IEnumerator Destello(Color color, float intensidad, float duracion)
    {
        if (luzJefe == null) yield break;
        // Varios destellos pueden pisarse: al terminar el último se vuelve SIEMPRE al color normal
        destellos++;
        luzJefe.color = color; luzJefe.intensity = intensidad;
        yield return new WaitForSeconds(duracion);
        destellos--;
        if (destellos <= 0) { destellos = 0; luzJefe.color = colorLuzBase; luzJefe.intensity = intensidadLuzBase; }
    }

    float Valor(float[] lista, float d) => lista == null || lista.Length == 0 ? d : lista[Mathf.Clamp(fase - 1, 0, lista.Length - 1)];
    int Valor(int[] lista, int d) => lista == null || lista.Length == 0 ? d : lista[Mathf.Clamp(fase - 1, 0, lista.Length - 1)];

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (posicionesOjos != null)
            foreach (Vector3 p in posicionesOjos) Gizmos.DrawWireSphere(transform.TransformPoint(p), radioOjo);
    }
}
