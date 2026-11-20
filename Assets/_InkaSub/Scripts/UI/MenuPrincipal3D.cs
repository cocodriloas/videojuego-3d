using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Menú principal en 3D: Jugar, Controles, Opciones (volumen), Créditos y Salir.
// Detrás se ve el submarino flotando en el Mar de Grau. Arma su propia interfaz al empezar.
public class MenuPrincipal3D : MonoBehaviour
{
    [Header("Escena del juego (nombre EXACTO)")]
    public string escenaJuego = "Nivel_MarDeGrau";

    [Header("Estilo (lo asigna el constructor del menú)")]
    public Font fuente;
    public Sprite spriteRedondo;
    public Sprite logo;
    [Tooltip("Dibujos del 2D: botón normal / resaltado / presionado, marco de los paneles y volumen")]
    public Sprite botonNormal;
    public Sprite botonResaltado, botonPresionado, marcoPanel, rielDeslizador, perillaDeslizador;

    [Header("Escena de fondo")]
    public Transform submarino;
    public Transform camaraMenu;
    public float alturaFlotar = 0.22f;
    public float velocidadFlotar = 0.8f;

    [Header("Créditos")]
    [TextArea(4, 10)]
    public string creditos =
        "Inka Sub: Misión Mar de Grau\n" +
        "Desarrollado por Carlo Padilla y equipo\n" +
        "Universidad Continental\n\n" +
        "Fuente: Pixelify Sans (licencia OFL)\n" +
        "Modelos 3D generados con IA (Meshy)\n" +
        "Música generada con Gemini (Lyria)";

    private GameObject panelPrincipal, panelOpciones, panelCreditos, panelControles, panelConfirmar;
    private Button primeroPrincipal, primeroOpciones, primeroCreditos, primeroControles, primeroConfirmar;
    private RectTransform rtLogo;
    private Vector2 posLogo;
    private Image fundido;
    private Vector3 posSub, posCamara;
    private Quaternion rotSub, rotCamara;
    private bool cargando;

    void Awake()
    {
        Time.timeScale = 1f; // por si venimos de un juego pausado
        AudioListener.volume = UIInka.Volumen;
        UIInka.fuente = fuente;
        UIInka.spriteRedondo = spriteRedondo;
        UIInka.Estilo(botonNormal, botonResaltado, botonPresionado, marcoPanel, rielDeslizador, perillaDeslizador);
        Construir();
    }

    void Start()
    {
        if (submarino != null) { posSub = submarino.position; rotSub = submarino.rotation; }
        if (camaraMenu != null) { posCamara = camaraMenu.position; rotCamara = camaraMenu.rotation; }
        MostrarSolo(panelPrincipal, primeroPrincipal);
    }

    void Update()
    {
        float t = Time.unscaledTime;
        if (rtLogo != null) rtLogo.anchoredPosition = posLogo + new Vector2(0f, Mathf.Sin(t * 1.5f) * 10f);

        // El submarino se mece y gira despacio; la cámara se balancea como llevada por la corriente
        if (submarino != null)
        {
            // Flota suave en su sitio: sube y baja, se deja llevar un poco por la corriente y
            // se inclina con el vaivén (sin girar de más, así siempre se ve de 3/4)
            float sube = Mathf.Sin(t * velocidadFlotar);
            submarino.position = posSub + new Vector3(Mathf.Sin(t * 0.31f) * 0.12f, sube * alturaFlotar, Mathf.Sin(t * 0.23f) * 0.1f);
            submarino.rotation = rotSub * Quaternion.Euler(Mathf.Cos(t * velocidadFlotar) * 2.5f, Mathf.Sin(t * 0.2f) * 6f, Mathf.Sin(t * 0.55f) * 2f);
        }
        if (camaraMenu != null)
        {
            camaraMenu.position = posCamara + new Vector3(Mathf.Sin(t * 0.21f) * 0.6f, Mathf.Sin(t * 0.33f) * 0.3f, 0f);
            camaraMenu.rotation = rotCamara * Quaternion.Euler(Mathf.Sin(t * 0.27f) * 0.8f, Mathf.Sin(t * 0.19f) * 1.2f, 0f);
        }

        // Escape vuelve al panel principal
        Keyboard teclado = Keyboard.current;
        if (teclado != null && teclado.escapeKey.wasPressedThisFrame && !panelPrincipal.activeSelf)
            MostrarSolo(panelPrincipal, primeroPrincipal);
    }

    // ---------- Botones ----------

    // Con partida guardada, el nivel la carga solo (RestaurarProgreso3D); sin ella empieza desde el inicio
    public void NuevaPartida()
    {
        ProgresoJuego3D.Borrar();
        Jugar();
    }

    public void Jugar()
    {
        if (cargando) return;
        cargando = true;
        StartCoroutine(FundidoYCargar());
    }

    IEnumerator FundidoYCargar()
    {
        fundido.gameObject.SetActive(true);
        for (float t = 0f; t < 0.8f; t += Time.unscaledDeltaTime)
        {
            fundido.color = new Color(0f, 0.02f, 0.05f, t / 0.8f);
            yield return null;
        }
        fundido.color = new Color(0f, 0.02f, 0.05f, 1f);
        SceneManager.LoadScene(escenaJuego);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false; // en el editor detiene el Play
#else
        Application.Quit();                   // en el juego exportado cierra la ventana
#endif
    }

    void MostrarSolo(GameObject panel, Button primero)
    {
        panelPrincipal.SetActive(panel == panelPrincipal);
        panelOpciones.SetActive(panel == panelOpciones);
        panelCreditos.SetActive(panel == panelCreditos);
        panelControles.SetActive(panel == panelControles);
        panelConfirmar.SetActive(panel == panelConfirmar);
        if (rtLogo != null) rtLogo.gameObject.SetActive(panel == panelPrincipal);
        UIInka.Seleccionar(primero);
    }

    // ---------- Interfaz ----------

    void Construir()
    {
        Transform canvas = UIInka.CrearCanvas("Menu", 10).transform;

        // Degradado oscuro a la izquierda para que se lean los botones sobre el fondo 3D
        RectTransform sombra = UIInka.Panel(canvas, "Sombra", Color.clear, Vector2.zero);
        sombra.anchorMax = new Vector2(0.62f, 1f);
        Image imgSombra = sombra.gameObject.AddComponent<Image>();
        imgSombra.sprite = Degradado();
        imgSombra.color = new Color(0f, 0.03f, 0.07f, 0.75f);
        imgSombra.raycastTarget = false;

        if (logo != null)
        {
            GameObject go = new GameObject("Logo", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            Image img = go.AddComponent<Image>();
            img.sprite = logo;
            img.preserveAspect = true;
            img.raycastTarget = false;
            rtLogo = img.rectTransform;
            rtLogo.anchorMin = rtLogo.anchorMax = rtLogo.pivot = new Vector2(0.25f, 1f);
            rtLogo.sizeDelta = new Vector2(700, 320);
            rtLogo.anchoredPosition = posLogo = new Vector2(0, -36);
        }

        // Principal (columna de botones bajo el logo)
        panelPrincipal = Columna(canvas, "Principal");
        Transform p = panelPrincipal.transform;
        float y = 200f;
        if (ProgresoJuego3D.HayPartida)
        {
            // Hay partida guardada: Continuar desde el último cangrejo o empezar otra
            UIInka.Texto(p, "Partida guardada:  " + ProgresoJuego3D.Resumen(), 26, UIInka.Turquesa, new Vector2(0, y + 70), new Vector2(900, 40));
            primeroPrincipal = UIInka.Boton(p, "Continuar", new Vector2(0, y), Jugar);
            UIInka.Boton(p, "Nueva partida", new Vector2(0, y -= 100), () => MostrarSolo(panelConfirmar, primeroConfirmar));
        }
        else primeroPrincipal = UIInka.Boton(p, "Jugar", new Vector2(0, y), Jugar);
        UIInka.Boton(p, "Controles", new Vector2(0, y -= 100), () => MostrarSolo(panelControles, primeroControles));
        UIInka.Boton(p, "Opciones", new Vector2(0, y -= 100), () => MostrarSolo(panelOpciones, primeroOpciones));
        UIInka.Boton(p, "Créditos", new Vector2(0, y -= 100), () => MostrarSolo(panelCreditos, primeroCreditos));
#if !UNITY_WEBGL
        UIInka.Boton(p, "Salir", new Vector2(0, y -= 100), Salir);   // en el navegador no se puede cerrar la pestaña desde el juego
#endif

        // Confirmar "Nueva partida" (borra la guardada)
        panelConfirmar = Caja(canvas, "Confirmar", "¿NUEVA PARTIDA?", new Vector2(900, 480));
        Transform q = panelConfirmar.transform.GetChild(0);
        UIInka.Texto(q, "Se borrará la partida guardada y empezarás desde el inicio del Mar de Grau.", 32, Color.white, new Vector2(0, 40), new Vector2(800, 110));
        UIInka.Boton(q, "Sí, empezar", new Vector2(-180, -130), NuevaPartida, 320f);
        primeroConfirmar = UIInka.Boton(q, "No", new Vector2(180, -130), () => MostrarSolo(panelPrincipal, primeroPrincipal), 320f);

        // Opciones
        panelOpciones = Caja(canvas, "Opciones", "OPCIONES", new Vector2(760, 560));
        Transform o = panelOpciones.transform.GetChild(0);
        UIInka.Texto(o, "Volumen", 40, Color.white, new Vector2(0, 80), new Vector2(520, 60));
        UIInka.Deslizador(o, new Vector2(0, 10), UIInka.Volumen, UIInka.CambiarVolumen);
        primeroOpciones = UIInka.Boton(o, "Volver", new Vector2(0, -170), () => MostrarSolo(panelPrincipal, primeroPrincipal));

        // Créditos
        panelCreditos = Caja(canvas, "Creditos", "CRÉDITOS", new Vector2(1100, 720));
        Transform c = panelCreditos.transform.GetChild(0);
        UIInka.Texto(c, creditos, 36, Color.white, new Vector2(0, 30), new Vector2(1000, 380));
        primeroCreditos = UIInka.Boton(c, "Volver", new Vector2(0, -260), () => MostrarSolo(panelPrincipal, primeroPrincipal));

        // Controles
        panelControles = Caja(canvas, "Controles", "CONTROLES", new Vector2(1250, 860));
        Transform k = panelControles.transform.GetChild(0);
        UIInka.Texto(k,
            "WASD — mover (W y S suben y bajan)\n" +
            "Mouse — apuntar  ·  Clic izquierdo — disparar\n" +
            "R — recargar  ·  F — linterna  ·  E — hablar / usar\n" +
            "Túnel: rueda del mouse adelante avanza, atrás retrocede (o Espacio / Ctrl)\n" +
            "Poderes: 1 dash (o clic derecho) · 2 disparo doble (o K) · 3 escudo\n" +
            "(dash con 2 bacterias, disparo doble con 3, el escudo te lo da el Mero)\n" +
            "Esc o P — pausa", 34, Color.white, new Vector2(0, 50), new Vector2(1180, 400));
        UIInka.Texto(k, "Dispara a los OJOS ABIERTOS del Iceberg de Basura para dañarlo.", 30, UIInka.Turquesa,
            new Vector2(0, -200), new Vector2(1180, 60));
        primeroControles = UIInka.Boton(k, "Volver", new Vector2(0, -320), () => MostrarSolo(panelPrincipal, primeroPrincipal));

        // Pie de página
        RectTransform pie = UIInka.Texto(canvas, "ODS 14 · Vida submarina  —  Universidad Continental", 24,
            new Color(0.75f, 0.9f, 0.95f), Vector2.zero, new Vector2(1200, 40), TextAnchor.LowerRight).rectTransform;
        pie.anchorMin = pie.anchorMax = pie.pivot = new Vector2(1f, 0f);
        pie.anchoredPosition = new Vector2(-30, 24);

        // Cortina para pasar al juego
        fundido = UIInka.Panel(canvas, "Fundido", new Color(0f, 0.02f, 0.05f, 0f), Vector2.zero).gameObject.AddComponent<Image>();
        fundido.raycastTarget = true;
        fundido.gameObject.SetActive(false);
    }

    // Columna invisible a la izquierda, debajo del logo
    GameObject Columna(Transform canvas, string nombre)
    {
        RectTransform rt = UIInka.Panel(canvas, nombre, Color.clear, new Vector2(600, 600));
        rt.anchorMin = rt.anchorMax = new Vector2(0.25f, 0.5f);
        rt.anchoredPosition = new Vector2(0, -150);
        return rt.gameObject;
    }

    // Oscuro a la izquierda que se desvanece hacia la derecha (sin borde duro)
    static Sprite Degradado()
    {
        Texture2D t = new Texture2D(64, 1, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < 64; x++)
        {
            float k = x / 63f;
            t.SetPixel(x, 0, new Color(1f, 1f, 1f, 1f - k * k * (3f - 2f * k)));
        }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 64, 1), new Vector2(0.5f, 0.5f));
    }

    // Panel con fondo oscuro y título, centrado en la pantalla
    GameObject Caja(Transform canvas, string nombre, string titulo, Vector2 tamaño)
    {
        GameObject capa = UIInka.Panel(canvas, nombre, new Color(0f, 0.03f, 0.07f, 0.5f), Vector2.zero).gameObject;
        RectTransform caja = UIInka.Panel(capa.transform, "Caja", UIInka.FondoPanel, tamaño);
        // Con el marco inca el título baja un poco para no pisar el borde decorado
        float arriba = UIInka.marcoPanel != null ? 125f : 80f;
        UIInka.Texto(caja, titulo, 68, UIInka.Dorado, new Vector2(0, tamaño.y * 0.5f - arriba), new Vector2(tamaño.x - 220f, 100));
        return capa;
    }
}
