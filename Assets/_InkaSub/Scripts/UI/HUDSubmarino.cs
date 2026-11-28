using UnityEngine;
using UnityEngine.UI;

// HUD arriba a la derecha (como el juego 2D): casco, munición, bacterias, dash (1), Disparo Doble (2) y Escudo (3).
// Los poderes muestran su recarga llenándose en círculo; bloqueados se ven oscuros y dicen
// cuántas bacterias faltan; listos brillan y laten. Va en el objeto HUD; arma su interfaz solo.
public class HUDSubmarino : MonoBehaviour
{
    [Header("Íconos del juego 2D (los asigna el constructor del nivel)")]
    public Sprite iconoMunicion;
    public Sprite iconoBacteria;
    public Sprite iconoDash;
    public Sprite iconoDoble;
    public Sprite iconoEscudo;
    [Tooltip("Submarinito del 2D: uno por punto de vida (si falta, se usan cuadrados)")]
    public Sprite iconoVida;
    public Font fuente;

    [Header("Bacterias que hacen falta (mismas reglas que Recolectable3D)")]
    public int bacteriasDash = 2;
    public int bacteriasDoble = 3;

    private class Poder
    {
        public RectTransform raiz;
        public Image gris, color, anillo;
        public Text etiqueta;
    }

    private Poder municion, bacterias, dash, doble, escudo;
    private Image[] bloquesCasco;
    private RectTransform filaCasco;
    private Text textoCasco;
    private int vidaAnterior = -1;
    private float sacudirHasta;
    private int totalBacterias;

    private SubmarinoSalud3D salud;
    private SubmarinoMovimiento3D mov;
    private SubmarinoDisparo3D disp;
    private EscudoSubmarino esc;

    static readonly Color Dorado = new Color(1f, 0.85f, 0.35f);

    static HUDSubmarino instancia;

    void Awake()
    {
        instancia = this;
        Anunciando = false;
        Construir();
    }

    void OnDestroy() { if (instancia == this) { instancia = null; if (Anunciando) Time.timeScale = 1f; Anunciando = false; } }

    // ---------- Poder nuevo: el ícono aparece GRANDE al centro, se explica y vuela a su lugar ----------

    public enum PoderNuevo { Dash, DisparoDoble, Escudo }
    // Mientras se presenta un poder el juego está en pausa (MenuJuego3D no abre la pausa encima)
    public static bool Anunciando { get; private set; }
    private readonly System.Collections.Generic.Queue<PoderNuevo> porAnunciar = new System.Collections.Generic.Queue<PoderNuevo>();
    private bool anunciando;

    public static void Anunciar(PoderNuevo p)
    {
        if (instancia == null) return;
        instancia.porAnunciar.Enqueue(p);
        if (!instancia.anunciando) instancia.StartCoroutine(instancia.Presentar());
    }

    System.Collections.IEnumerator Presentar()
    {
        anunciando = true;
        while (porAnunciar.Count > 0)
        {
            // Espera a que se cierre la ficha científica o el diálogo (no se pisan)
            while (FichaCientifica.Abierta || DialogoUI3D.Abierto) yield return null;
            yield return new WaitForSecondsRealtime(0.25f);
            PoderNuevo cual = porAnunciar.Dequeue();
            Poder destino = cual == PoderNuevo.Dash ? dash : cual == PoderNuevo.DisparoDoble ? doble : escudo;
            string titulo, tecla, texto;
            if (cual == PoderNuevo.Dash)
            { titulo = "DASH"; tecla = "Tecla 1  o  clic derecho"; texto = "Un impulso rápido hacia donde te mueves.\nMientras dura eres INMUNE al daño: úsalo para atravesar ataques."; }
            else if (cual == PoderNuevo.DisparoDoble)
            { titulo = "DISPARO DOBLE DISOLVENTE"; tecla = "Tecla 2  o  K"; texto = "Durante 5 segundos disparas por los dos cañones\nsin gastar munición (mantén el clic)."; }
            else
            { titulo = "ESCUDO DE BURBUJAS"; tecla = "Tecla 3"; texto = "Una burbuja que bloquea TODO el daño durante 4 segundos.\nÚsalo cuando no puedas esquivar."; }

            Anunciando = true;
            Time.timeScale = 0f;
            AudioManager.Sonar(Sonido.Desbloqueo);

            // Velo oscuro, rayos girando detrás del ícono, título, tecla y explicación
            RectTransform capa = Rect("Poder nuevo", transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            capa.anchorMin = Vector2.zero; capa.anchorMax = Vector2.one; capa.sizeDelta = Vector2.zero;
            Image velo = capa.gameObject.AddComponent<Image>();
            velo.color = new Color(0f, 0.02f, 0.05f, 0f);
            velo.raycastTarget = false;
            Image brillo = Imagen(capa, destino.color.sprite, new Vector2(420f, 420f));
            brillo.rectTransform.anchoredPosition = new Vector2(0f, 120f);
            brillo.color = new Color(1f, 0.85f, 0.35f, 0f);
            Image icono = Imagen(capa, destino.color.sprite, new Vector2(110f, 110f));
            icono.rectTransform.anchoredPosition = new Vector2(0f, 120f);
            Text tNuevo = Texto(capa, "¡NUEVO PODER!", 40, new Color(0.45f, 1f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1200f, 60f), TextAnchor.MiddleCenter);
            Text tTitulo = Texto(capa, titulo, 64, Dorado, new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(1400f, 80f), TextAnchor.MiddleCenter);
            Text tTecla = Texto(capa, tecla, 36, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0f, -135f), new Vector2(1200f, 50f), TextAnchor.MiddleCenter);
            Text tTexto = Texto(capa, texto, 30, new Color(0.8f, 0.92f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -215f), new Vector2(1300f, 100f), TextAnchor.MiddleCenter);
            Text tSeguir = Texto(capa, "[E] continuar", 28, Dorado, new Vector2(0.5f, 0.5f), new Vector2(0f, -320f), new Vector2(600f, 40f), TextAnchor.MiddleCenter);
            Text[] textos = { tNuevo, tTitulo, tTecla, tTexto };
            foreach (Text tx in textos) tx.color = new Color(tx.color.r, tx.color.g, tx.color.b, 0f);
            tSeguir.gameObject.SetActive(false);

            // 1) Entra: el velo se oscurece y el ícono crece con rebote
            for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.6f;
                velo.color = new Color(0f, 0.02f, 0.05f, 0.72f * k);
                float s = k < 0.7f ? Mathf.Lerp(0.2f, 3.1f, k / 0.7f) : Mathf.Lerp(3.1f, 2.6f, (k - 0.7f) / 0.3f);
                icono.rectTransform.localScale = Vector3.one * s;
                foreach (Text tx in textos) tx.color = new Color(tx.color.r, tx.color.g, tx.color.b, k);
                yield return null;
            }
            // 2) Se lee: el brillo late y gira; a los 1.2 s se puede seguir (o sigue solo a los 6 s)
            for (float t = 0f; t < 6f; t += Time.unscaledDeltaTime)
            {
                float u = Time.unscaledTime;
                brillo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, u * 25f);
                brillo.color = new Color(1f, 0.85f, 0.35f, 0.25f + Mathf.Sin(u * 3f) * 0.1f);
                icono.rectTransform.localScale = Vector3.one * (2.6f + Mathf.Sin(u * 2.5f) * 0.06f);
                if (t > 1.2f)
                {
                    tSeguir.gameObject.SetActive(true);
                    tSeguir.color = new Color(Dorado.r, Dorado.g, Dorado.b, 0.6f + Mathf.Sin(u * 4f) * 0.4f);
                    UnityEngine.InputSystem.Keyboard k = UnityEngine.InputSystem.Keyboard.current;
                    UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
                    UnityEngine.InputSystem.Gamepad g = UnityEngine.InputSystem.Gamepad.current;
                    if (pedirSeguir || (k != null && (k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame))
                        || (m != null && m.leftButton.wasPressedThisFrame) || (g != null && g.buttonSouth.wasPressedThisFrame)) break;
                }
                yield return null;
            }
            pedirSeguir = false;
            tSeguir.gameObject.SetActive(false);
            brillo.gameObject.SetActive(false);
            // 3) El juego sigue y el ícono VUELA a su lugar arriba a la derecha
            Time.timeScale = 1f;
            Anunciando = false;
            Vector3 desde = icono.rectTransform.position, hasta = destino.raiz.position;
            for (float t = 0f; t < 0.75f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.75f);
                icono.rectTransform.position = Vector3.Lerp(desde, hasta, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 60f;
                icono.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, k);
                velo.color = new Color(0f, 0.02f, 0.05f, 0.72f * (1f - k));
                foreach (Text tx in textos) tx.color = new Color(tx.color.r, tx.color.g, tx.color.b, 1f - k * 2f);
                yield return null;
            }
            Destroy(capa.gameObject);
            // Al llegar, su lugar en el HUD late unos segundos para que se note dónde quedó
            resaltarHasta = Time.unscaledTime + 2.5f;
            resaltado = destino;
        }
        anunciando = false;
    }

    private bool pedirSeguir;
    // Para pruebas o botones táctiles: lo mismo que apretar E
    public static void Seguir() { if (instancia != null) instancia.pedirSeguir = true; }
    private float resaltarHasta;
    private Poder resaltado;

    void Start()
    {
        foreach (Recolectable3D r in FindObjectsByType<Recolectable3D>(FindObjectsSortMode.None))
            if (r.tipo == TipoRecolectable.Bacteria) totalBacterias++;
    }

    void Update()
    {
        if (salud == null)
        {
            salud = SubmarinoSalud3D.instancia;
            if (salud == null) return;
            mov = salud.GetComponent<SubmarinoMovimiento3D>();
            disp = salud.GetComponent<SubmarinoDisparo3D>();
            esc = salud.GetComponent<EscudoSubmarino>();
        }

        ActualizarCasco();

        // Munición: el círculo se llena mientras recarga
        if (disp != null)
        {
            if (disp.HabilidadActiva) Pintar(municion, 1f, true, "MAX", Color.white, 1f);
            else if (disp.Recargando) Pintar(municion, disp.ProgresoRecarga, true, "Recargando", new Color(1f, 1f, 1f, 0.9f), 1f);
            else Pintar(municion, (float)disp.BalasActuales / Mathf.Max(1, disp.BalasMaximas), true, disp.BalasActuales + " / " + disp.BalasMaximas,
                disp.BalasActuales <= 5 ? new Color(1f, 0.55f, 0.45f) : Color.white, 1f);
        }

        int n = Recolectable3D.BacteriasRecogidas;
        Pintar(bacterias, 1f, true, n + " / " + Mathf.Max(totalBacterias, n), Color.white, 1f);

        // Dash (clic derecho)
        if (mov != null)
        {
            if (!mov.dashDesbloqueado) Bloqueado(dash, n, bacteriasDash);
            else EstadoPoder(dash, mov.ProgresoDash, mov.EstaHaciendoDash, mov.DashListo, "1");
        }

        // Disparo Doble (K)
        if (disp != null)
        {
            if (!disp.HabilidadDesbloqueada) Bloqueado(doble, n, bacteriasDoble);
            else EstadoPoder(doble, disp.ProgresoHabilidad, disp.HabilidadActiva, disp.HabilidadLista, "2");
        }

        // Escudo de Burbujas (3): lo da el Mero
        if (esc != null)
        {
            if (!esc.desbloqueado) { Bloqueado(escudo, 0, 0); escudo.etiqueta.text = "Mero"; }
            else EstadoPoder(escudo, esc.Progreso, esc.Activo, esc.Listo, "3");
        }
        else escudo.raiz.gameObject.SetActive(false);

        if (resaltado != null)
        {
            bool activo = Time.unscaledTime < resaltarHasta;
            resaltado.raiz.localScale = Vector3.one * (activo ? 1.15f + Mathf.Sin(Time.unscaledTime * 10f) * 0.1f : 1f);
            if (!activo) resaltado = null;
        }
    }

    // ---------- Estados ----------

    void Bloqueado(Poder p, int tiene, int necesita)
    {
        p.gris.color = new Color(0.12f, 0.12f, 0.16f, 0.85f);
        p.color.fillAmount = 0f;
        p.anillo.color = Color.clear;
        p.etiqueta.text = Mathf.Max(0, necesita - tiene) + " bact.";
        p.etiqueta.color = new Color(0.6f, 0.65f, 0.7f);
        p.raiz.localScale = Vector3.one;
    }

    void EstadoPoder(Poder p, float progreso, bool enUso, bool listo, string tecla)
    {
        p.gris.color = new Color(0.32f, 0.34f, 0.4f, 1f);
        p.color.fillAmount = progreso;
        float escala = 1f;
        if (enUso)
        {
            // En uso: late rápido y se va vaciando
            escala = 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 10f)) * 0.1f;
            p.color.color = Color.white;
            p.anillo.color = new Color(1f, 1f, 1f, 0.7f);
            p.etiqueta.text = "¡Activo!";
            p.etiqueta.color = Color.white;
        }
        else if (listo)
        {
            // Listo: brilla hacia turquesa y late suave
            float k = (Mathf.Sin(Time.unscaledTime * 3f) + 1f) * 0.5f;
            escala = 1f + k * 0.07f;
            p.color.color = Color.Lerp(Color.white, new Color(0.65f, 1f, 1f), k);
            p.anillo.color = new Color(0.4f, 1f, 1f, 0.35f + k * 0.45f);
            p.etiqueta.text = tecla;
            p.etiqueta.color = Dorado;
        }
        else
        {
            // Recargando: el color se va llenando en círculo
            p.color.color = new Color(1f, 1f, 1f, 0.9f);
            p.anillo.color = Color.clear;
            p.etiqueta.text = Mathf.RoundToInt(progreso * 100f) + "%";
            p.etiqueta.color = new Color(0.75f, 0.85f, 0.9f);
        }
        p.raiz.localScale = Vector3.one * escala;
    }

    void Pintar(Poder p, float relleno, bool activo, string texto, Color colorTexto, float escala)
    {
        p.gris.color = new Color(0.32f, 0.34f, 0.4f, 1f);
        p.color.fillAmount = relleno;
        p.color.color = Color.white;
        p.anillo.color = Color.clear;
        p.etiqueta.text = texto;
        p.etiqueta.color = colorTexto;
        p.raiz.localScale = Vector3.one * escala;
    }

    void ActualizarCasco()
    {
        int vida = salud.VidaActual, max = salud.vidaMaxima;
        if (bloquesCasco == null || bloquesCasco.Length != max) CrearBloques(max);
        if (vidaAnterior >= 0 && vida < vidaAnterior) sacudirHasta = Time.unscaledTime + 0.35f;   // golpe: tiembla
        vidaAnterior = vida;
        for (int i = 0; i < max; i++)
        {
            // Submarinitos: enteros a color; el último que queda se pone rojo y late; los perdidos, oscuros
            if (iconoVida != null)
            {
                bool ultimo = vida <= 1 && i < vida;
                bloquesCasco[i].color = i < vida ? (ultimo ? new Color(1f, 0.45f, 0.4f) : Color.white) : new Color(0.15f, 0.18f, 0.22f, 0.55f);
                bloquesCasco[i].rectTransform.localScale = Vector3.one * (ultimo ? 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.1f : 1f);
            }
            else bloquesCasco[i].color = i < vida ? (vida <= 1 ? new Color(1f, 0.35f, 0.3f) : Dorado) : new Color(1f, 1f, 1f, 0.15f);
        }
        filaCasco.anchoredPosition = new Vector2(-30f, -28f) + (Time.unscaledTime < sacudirHasta ? Random.insideUnitCircle * 6f : Vector2.zero);
    }

    // ---------- Interfaz ----------

    void Construir()
    {
        // Fila del casco: "CASCO" y un bloque por punto de vida
        filaCasco = Rect("Casco", transform, new Vector2(1f, 1f), new Vector2(-30f, -28f), new Vector2(520f, 50f));
        filaCasco.pivot = new Vector2(1f, 1f);
        textoCasco = Texto(filaCasco, "CASCO", 34, Dorado, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(200f, 50f), TextAnchor.MiddleRight);

        // Fila de íconos: munición, bacterias, dash, disparo doble
        Sprite[] iconos = { iconoMunicion, iconoBacteria, iconoDash, iconoDoble, iconoEscudo };
        Poder[] p = new Poder[iconos.Length];
        for (int i = 0; i < iconos.Length; i++)
        {
            float x = -30f - (iconos.Length - 1 - i) * 122f - 55f;
            p[i] = CrearPoder(iconos[i], new Vector2(x, -150f));
        }
        municion = p[0]; bacterias = p[1]; dash = p[2]; doble = p[3]; escudo = p[4];
    }

    float PasoVida => iconoVida != null ? 58f : 40f;

    void CrearBloques(int max)
    {
        if (bloquesCasco != null) foreach (Image b in bloquesCasco) if (b != null) Destroy(b.gameObject);
        bloquesCasco = new Image[max];
        for (int i = 0; i < max; i++)
        {
            Vector2 tam = iconoVida != null ? new Vector2(54f, 43f) : new Vector2(30f, 30f);
            RectTransform r = Rect("Bloque" + i, filaCasco, new Vector2(1f, 0.5f), new Vector2(-i * PasoVida - tam.x * 0.5f - 4f, 0f), tam);
            r.SetSiblingIndex(1);
            bloquesCasco[i] = r.gameObject.AddComponent<Image>();
            bloquesCasco[i].raycastTarget = false;
            if (iconoVida != null) { bloquesCasco[i].sprite = iconoVida; bloquesCasco[i].preserveAspect = true; }
            else r.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);
        }
        // Los bloques se leen de izquierda a derecha: el primero es el de más a la izquierda
        System.Array.Reverse(bloquesCasco);
        // "CASCO" justo a la izquierda del primer bloque
        textoCasco.rectTransform.anchoredPosition = new Vector2(520f - (max - 1) * PasoVida - (iconoVida != null ? 165f : 145f), 0f);
    }

    Poder CrearPoder(Sprite icono, Vector2 pos)
    {
        Poder p = new Poder();
        p.raiz = Rect("Poder", transform, new Vector2(1f, 1f), pos, new Vector2(110f, 110f));
        // Anillo que brilla cuando está listo (detrás del ícono)
        p.anillo = Imagen(p.raiz, icono, new Vector2(132f, 132f));
        p.anillo.color = Color.clear;
        p.gris = Imagen(p.raiz, icono, new Vector2(110f, 110f));
        p.color = Imagen(p.raiz, icono, new Vector2(110f, 110f));
        p.color.type = Image.Type.Filled;
        p.color.fillMethod = Image.FillMethod.Radial360;
        p.color.fillOrigin = (int)Image.Origin360.Top;
        p.color.fillClockwise = true;
        p.etiqueta = Texto(p.raiz, "", 24, Color.white, new Vector2(0.5f, 0f), new Vector2(0f, -22f), new Vector2(160f, 34f), TextAnchor.MiddleCenter);
        return p;
    }

    static RectTransform Rect(string nombre, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = ancla;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
        return r;
    }

    static Image Imagen(RectTransform padre, Sprite s, Vector2 tam)
    {
        RectTransform r = Rect("Imagen", padre, new Vector2(0.5f, 0.5f), Vector2.zero, tam);
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = s;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    Text Texto(RectTransform padre, string s, int tam, Color c, Vector2 ancla, Vector2 pos, Vector2 caja, TextAnchor alineado)
    {
        RectTransform r = Rect("Texto", padre, ancla, pos, caja);
        Text t = r.gameObject.AddComponent<Text>();
        t.font = fuente != null ? fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = tam;
        t.color = c;
        t.alignment = alineado;
        t.text = s;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        r.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
        return t;
    }
}
