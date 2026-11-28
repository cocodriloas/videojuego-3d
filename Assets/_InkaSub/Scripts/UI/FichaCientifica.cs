using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Ficha de "DATO CIENTÍFICO" que entra por la derecha al recoger una bacteria (pausa el juego y oscurece el resto)
// y contador "Bacterias x / total" en el HUD. Va en el objeto HUD; arma su interfaz al empezar.
public class FichaCientifica : MonoBehaviour
{
    static FichaCientifica instancia;
    public static bool Existe => instancia != null;

    public Font fuente;
    public Sprite spriteRedondo;
    [Tooltip("Marco de la ficha (la caja dorada del diálogo del 2D)")]
    public Sprite marco;
    [Tooltip("Hoja de la bacteria del 2D (4 dibujos en fila): se anima en la ficha")]
    public Texture2D hojaBacteria;
    [Tooltip("Contador 'Bacterias x / total' abajo a la izquierda (apagado si el HUD ya lo muestra)")]
    public bool mostrarContador = true;

    private Sprite[] cuadrosBacteria;
    private Image imagenBacteria;
    private Text subtitulo;

    [Header("Datos (uno por bacteria, en el orden en que se recogen)")]
    [TextArea(3, 6)]
    public string[] datos =
    {
        "Esta es Ideonella sakaiensis, una bacteria descubierta en 2016 en Japón, cerca de una planta de reciclaje de botellas. Es capaz de degradar el PET, el plástico de las botellas de bebidas.",
        "Para comerse el PET usa dos enzimas: la PETasa lo corta en trozos (MHET) y la MHETasa los separa en ácido tereftálico y etilenglicol, que la bacteria usa como alimento.",
        "PET significa tereftalato de polietileno. En la naturaleza tarda cientos de años en degradarse; mientras tanto se rompe en microplásticos que comen peces, aves y tortugas.",
        "Desde 2018, científicos mejoran la PETasa en el laboratorio para que trabaje más rápido. Una versión de 2022, la FAST-PETasa, logra degradar algunos plásticos en cuestión de días.",
        "El Mar de Grau es parte de la corriente de Humboldt, uno de los ecosistemas marinos más productivos del mundo. Las bacterias ayudan, pero la mejor solución es usar menos plástico y reciclar (ODS 14)."
    };

    [Header("Tiempo en pantalla")]
    public float segundosMinimos = 10f;
    public float letrasPorSegundo = 12f;   // más texto = más tiempo para leer
    [Tooltip("Después de estos segundos aparece \"[E] continuar\" (antes no, para que no se cierre sin querer)")]
    public float segundosAntesDeCerrar = 1.5f;
    // Mientras la ficha se puede cerrar con E, esa tecla es suya (NPCDialogo3D no la usa para hablar)
    public static bool PuedeCerrarse { get; private set; }
    private Text txtCerrar;
    private Image velo;

    private RectTransform ficha;
    private Text titulo, texto, desbloqueo;
    private Image barraTiempo;
    private Text contador;
    private int total;
    private Coroutine mostrando;

    void Awake()
    {
        instancia = this;
        PuedeCerrarse = false;
        Abierta = false;
        Construir();
    }

    void Start()
    {
        // Cuántas bacterias tiene el nivel (se cuentan al empezar)
        foreach (Recolectable3D r in FindObjectsByType<Recolectable3D>(FindObjectsSortMode.None))
            if (r.tipo == TipoRecolectable.Bacteria) total++;
        ActualizarContador(0);
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    void Update()
    {
        // La bacteria de la ficha se mueve como en el 2D y flota un poco
        if (imagenBacteria == null || !ficha.gameObject.activeSelf) return;
        float t = Time.unscaledTime;
        if (cuadrosBacteria != null && cuadrosBacteria.Length > 1)
            imagenBacteria.sprite = cuadrosBacteria[Mathf.FloorToInt(t * 6f) % cuadrosBacteria.Length];
        imagenBacteria.rectTransform.anchoredPosition = new Vector2(-262f, 150f + Mathf.Sin(t * 2.5f) * 5f);
    }

    // Lo llama Recolectable3D al recoger la bacteria número "n" (1, 2, 3...).
    // "habilidad" es el aviso de lo que se desbloqueó (o null).
    public static void BacteriaRecogida(int n, string habilidad = null)
    {
        if (instancia == null) return;
        ActualizarContador(n);
        if (instancia.datos.Length == 0) return;
        string dato = instancia.datos[Mathf.Clamp(n - 1, 0, instancia.datos.Length - 1)];
        instancia.desbloqueo.text = habilidad ?? "";
        if (instancia.mostrando != null) instancia.StopCoroutine(instancia.mostrando);
        PuedeCerrarse = false;
        if (Abierta) { Abierta = false; Time.timeScale = 1f; }
        instancia.mostrando = instancia.StartCoroutine(instancia.Mostrar(n, dato));
    }

    // También lo usa RestaurarProgreso3D al cargar una partida
    public static void ActualizarContador(int n)
    {
        if (instancia != null && instancia.contador != null)
            instancia.contador.text = "Bacterias  " + n + " / " + Mathf.Max(instancia.total, n);
    }

    // El juego se PAUSA mientras se lee: la pantalla se oscurece (como una pausa) y la ficha queda al costado.
    // Después de un momento aparece "[E] continuar" y el jugador sigue cuando quiere.
    public static bool Abierta { get; private set; }
    private bool pedirContinuar;

    // Sigue jugando (lo mismo que apretar E). Sirve para botones táctiles y para las pruebas
    public static void Continuar() { if (instancia != null && PuedeCerrarse) instancia.pedirContinuar = true; }

    IEnumerator Mostrar(int n, string dato)
    {
        subtitulo.text = "Ideonella sakaiensis  ·  " + n + " de " + Mathf.Max(total, n);
        texto.text = dato;
        ficha.gameObject.SetActive(true);
        barraTiempo.gameObject.SetActive(false);
        txtCerrar.gameObject.SetActive(false);
        Abierta = true;
        pedirContinuar = false;
        Time.timeScale = 0f;
        velo.gameObject.SetActive(true);

        // Se oscurece el resto y la ficha entra deslizándose con un pequeño rebote
        StartCoroutine(Velo(0f, 0.62f, 0.3f));
        yield return Deslizar(ficha.anchoredPosition.x, -30f, 0.35f);
        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            ficha.localScale = Vector3.one * (1f + Mathf.Sin(t / 0.25f * Mathf.PI) * 0.05f);
            yield return null;
        }
        ficha.localScale = Vector3.one;

        // Lee tranquilo: sin límite de tiempo. A los segundos se puede continuar con E (o Enter)
        for (float t = 0f; ; t += Time.unscaledDeltaTime)
        {
            if (t >= segundosAntesDeCerrar)
            {
                txtCerrar.gameObject.SetActive(true);
                PuedeCerrarse = true;
                txtCerrar.color = new Color(1f, 0.9f, 0.5f, 0.6f + Mathf.Sin(Time.unscaledTime * 4f) * 0.4f);
                // E, Enter, Espacio, clic o el botón A del mando: cualquiera sirve para seguir
                UnityEngine.InputSystem.Keyboard k = UnityEngine.InputSystem.Keyboard.current;
                UnityEngine.InputSystem.Mouse m = UnityEngine.InputSystem.Mouse.current;
                UnityEngine.InputSystem.Gamepad g = UnityEngine.InputSystem.Gamepad.current;
                if (pedirContinuar
                    || (k != null && (k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame))
                    || (m != null && m.leftButton.wasPressedThisFrame)
                    || (g != null && g.buttonSouth.wasPressedThisFrame)) break;
            }
            yield return null;
        }
        txtCerrar.gameObject.SetActive(false);
        AudioManager.Sonar(Sonido.BotonUI);

        StartCoroutine(Velo(0.62f, 0f, 0.25f));
        yield return Deslizar(-30f, ficha.sizeDelta.x + 60f, 0.3f);
        ficha.gameObject.SetActive(false);
        velo.gameObject.SetActive(false);
        PuedeCerrarse = false;
        Abierta = false;
        Time.timeScale = 1f;
        mostrando = null;
    }

    IEnumerator Velo(float desde, float hasta, float segundos)
    {
        for (float t = 0f; t < segundos; t += Time.unscaledDeltaTime)
        {
            velo.color = new Color(0f, 0.02f, 0.05f, Mathf.Lerp(desde, hasta, t / segundos));
            yield return null;
        }
        velo.color = new Color(0f, 0.02f, 0.05f, hasta);
    }
    IEnumerator Deslizar(float desde, float hasta, float segundos)
    {
        for (float t = 0f; t < segundos; t += Time.unscaledDeltaTime)
        {
            float k = t / segundos;
            k = 1f - (1f - k) * (1f - k);   // frena al llegar
            ficha.anchoredPosition = new Vector2(Mathf.Lerp(desde, hasta, k), ficha.anchoredPosition.y);
            yield return null;
        }
        ficha.anchoredPosition = new Vector2(hasta, ficha.anchoredPosition.y);
    }

    // ---------- Interfaz ----------

    void Construir()
    {
        UIInka.fuente = fuente;
        UIInka.spriteRedondo = spriteRedondo;
        Color verde = new Color(0.45f, 1f, 0.55f);

        // Contador abajo a la izquierda (si el HUD de arriba no lo muestra)
        contador = UIInka.Texto(transform, "", 32, verde, Vector2.zero, new Vector2(500, 50), TextAnchor.LowerLeft);
        contador.name = "ContadorBacterias";
        RectTransform rc = contador.rectTransform;
        rc.anchorMin = rc.anchorMax = rc.pivot = Vector2.zero;
        rc.anchoredPosition = new Vector2(30, 170);
        contador.gameObject.SetActive(mostrarContador);

        // Velo oscuro de pantalla completa detrás de la ficha (el juego queda en pausa mientras se lee)
        GameObject gv = new GameObject("Velo ficha", typeof(RectTransform));
        gv.transform.SetParent(transform, false);
        RectTransform rv = gv.GetComponent<RectTransform>();
        rv.anchorMin = Vector2.zero; rv.anchorMax = Vector2.one; rv.offsetMin = rv.offsetMax = Vector2.zero;
        velo = gv.AddComponent<Image>();
        velo.color = new Color(0f, 0.02f, 0.05f, 0f);
        velo.raycastTarget = false;
        gv.SetActive(false);

        // Ficha a la derecha, con el marco dorado del juego 2D (o un panel simple si falta la imagen)
        if (marco != null)
        {
            GameObject go = new GameObject("FichaCientifica", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            ficha = go.GetComponent<RectTransform>();
            ficha.sizeDelta = new Vector2(680, 470);
            Image fondo = go.AddComponent<Image>();
            fondo.sprite = marco;
            fondo.type = Image.Type.Sliced;
            fondo.pixelsPerUnitMultiplier = 1.8f;
        }
        else ficha = UIInka.Panel(transform, "FichaCientifica", UIInka.FondoPanel, new Vector2(680, 470));
        ficha.anchorMin = ficha.anchorMax = ficha.pivot = new Vector2(1f, 0.5f);
        ficha.anchoredPosition = new Vector2(ficha.sizeDelta.x + 60f, 30f);

        // La bacteria del juego 2D, animada (o una cápsula verde si falta la imagen)
        if (hojaBacteria != null)
        {
            int cuadros = Mathf.Max(1, Mathf.RoundToInt((float)hojaBacteria.width / hojaBacteria.height * 1.2f));
            cuadros = Mathf.Clamp(cuadros, 1, 4);
            float w = hojaBacteria.width / (float)cuadros;
            cuadrosBacteria = new Sprite[cuadros];
            for (int i = 0; i < cuadros; i++)
                cuadrosBacteria[i] = Sprite.Create(hojaBacteria, new Rect(i * w, 0f, w, hojaBacteria.height), new Vector2(0.5f, 0.5f), 100f);
            imagenBacteria = Imagen(ficha, "Bacteria", Color.white, new Vector2(-262, 150), new Vector2(110, 132));
            imagenBacteria.sprite = cuadrosBacteria[0];
            imagenBacteria.type = Image.Type.Simple;
            imagenBacteria.preserveAspect = true;
        }
        else
        {
            imagenBacteria = Imagen(ficha, "Bacteria", verde, new Vector2(-262, 150), new Vector2(86, 44));
        }

        titulo = UIInka.Texto(ficha, "DATO CIENTÍFICO", 38, UIInka.Dorado, new Vector2(50, 168), new Vector2(480, 50), TextAnchor.MiddleLeft);
        subtitulo = UIInka.Texto(ficha, "", 22, verde, new Vector2(60, 128), new Vector2(500, 32), TextAnchor.MiddleLeft);
        subtitulo.horizontalOverflow = HorizontalWrapMode.Overflow;
        subtitulo.fontStyle = FontStyle.Italic;
        Imagen(ficha, "Linea", new Color(1f, 0.85f, 0.35f, 0.5f), new Vector2(0, 96), new Vector2(580, 3)).sprite = null;
        texto = UIInka.Texto(ficha, "", 27, Color.white, new Vector2(0, -12), new Vector2(580, 190), TextAnchor.UpperLeft);
        texto.lineSpacing = 1.12f;
        // Habilidad nueva (dash o disparo doble), dentro de la ficha para no tapar nada
        desbloqueo = UIInka.Texto(ficha, "", 30, new Color(1f, 0.95f, 0.5f), new Vector2(0, -150), new Vector2(580, 56));

        barraTiempo = Imagen(ficha, "Tiempo", new Color(verde.r, verde.g, verde.b, 0.8f), new Vector2(0, -192), new Vector2(560, 7));
        txtCerrar = UIInka.Texto(ficha, "[E] continuar", 30, UIInka.Dorado, new Vector2(0, -192), new Vector2(560, 40), TextAnchor.MiddleCenter);
        txtCerrar.gameObject.SetActive(false);
        barraTiempo.sprite = null;
        barraTiempo.type = Image.Type.Filled;
        barraTiempo.fillMethod = Image.FillMethod.Horizontal;

        ficha.gameObject.SetActive(false);
    }

    static Image Imagen(RectTransform padre, string nombre, Color color, Vector2 pos, Vector2 tamaño)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamaño;
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        if (UIInka.spriteRedondo != null) { img.sprite = UIInka.spriteRedondo; img.type = Image.Type.Sliced; }
        return img;
    }
}
