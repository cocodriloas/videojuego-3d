using UnityEngine;
using UnityEngine.UI;

// Recordatorio de controles de los túneles (zonas de Profundidad). Aparece abajo al entrar al túnel
// y otra vez si el jugador se queda atorado (no avanza hacia el fondo en varios segundos) o se pierde
// (vuelve mucho hacia atrás). Se esconde solo cuando vuelve a avanzar. Va en el objeto HUD.
public class AyudaControles : MonoBehaviour
{
    public Font fuente;
    public Sprite spriteRedondo;
    [Tooltip("Segundos sin avanzar para volver a mostrar los controles")]
    public float segundosAtorado = 6f;
    [Tooltip("Metros que hay que avanzar para que cuente como progreso")]
    public float metrosProgreso = 1.5f;
    [Tooltip("Segundos que se ve al entrar a un túnel")]
    public float segundosAlEntrar = 5f;

    // Para las pruebas
    public static bool Visible { get; private set; }

    private CanvasGroup grupo;
    private RectTransform caja;
    private Transform jugador;
    private ZonaCamara zona;
    private float mejor, ultimoAvance, mostrarHasta, avanceAlMostrar;
    private bool porAtasco;

    void Start()
    {
        Visible = false;
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;
        Construir();
    }

    void Update()
    {
        bool mostrar = false;
        ZonaCamara z = ZonaCamara.Actual;
        if (jugador != null && z != null && z.modo == ModoZona.Profundidad && z.centroFijo == null)
        {
            float avance = Vector3.Dot(jugador.position - z.transform.position, z.Fondo);
            if (z != zona)
            {
                // Recién entra al túnel: se muestran los controles unos segundos
                zona = z;
                mejor = avance;
                ultimoAvance = Time.time;
                mostrarHasta = Time.time + segundosAlEntrar;
                porAtasco = false;
            }
            if (avance > mejor + metrosProgreso)
            {
                mejor = avance;
                ultimoAvance = Time.time;
                // Volvió a avanzar bien después del aviso: se esconde
                if (porAtasco && avance > avanceAlMostrar + 3f) porAtasco = false;
            }
            bool perdido = avance < mejor - 6f;   // se fue muy hacia atrás
            bool atorado = Time.timeScale > 0f && Time.time - ultimoAvance > segundosAtorado;
            if ((atorado || perdido) && !porAtasco)
            {
                porAtasco = true;
                avanceAlMostrar = Mathf.Max(avance, mejor - 6f);
                if (perdido) mejor = avance;   // desde aquí se vuelve a medir
            }
            mostrar = porAtasco || Time.time < mostrarHasta;
        }
        else zona = null;

        // No estorba a la ficha científica ni a la pausa
        if (FichaCientifica.Abierta || Time.timeScale == 0f) mostrar = false;
        Visible = mostrar;

        grupo.alpha = Mathf.MoveTowards(grupo.alpha, mostrar ? 1f : 0f, Time.unscaledDeltaTime * 3f);
        // Un pequeño latido cuando sale por atasco, para que se note
        float s = porAtasco ? 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.025f : 1f;
        caja.localScale = Vector3.one * s;
    }

    void Construir()
    {
        UIInka.fuente = fuente;
        UIInka.spriteRedondo = spriteRedondo;
        GameObject go = new GameObject("AyudaControles", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        caja = go.GetComponent<RectTransform>();
        caja.anchorMin = caja.anchorMax = caja.pivot = new Vector2(0.5f, 0f);
        caja.anchoredPosition = new Vector2(0f, 40f);
        caja.sizeDelta = new Vector2(1180f, 96f);
        grupo = go.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        Image fondo = go.AddComponent<Image>();
        fondo.color = new Color(0.01f, 0.06f, 0.1f, 0.78f);
        fondo.raycastTarget = false;
        if (spriteRedondo != null) { fondo.sprite = spriteRedondo; fondo.type = Image.Type.Sliced; }

        // Tecla + qué hace, de izquierda a derecha
        string[,] teclas = { { "RUEDA ADELANTE", "avanzar" }, { "RUEDA ATRÁS", "retroceder" }, { "W / S", "subir / bajar" }, { "A / D", "a los lados" } };
        float x = -560f;
        for (int i = 0; i < teclas.GetLength(0); i++)
        {
            float anchoTecla = teclas[i, 0].Length * 17f + 34f;
            RectTransform t = Tecla(caja, teclas[i, 0], new Vector2(x + anchoTecla * 0.5f, 0f), anchoTecla);
            Text d = UIInka.Texto(caja, teclas[i, 1], 28, Color.white, new Vector2(x + anchoTecla + 12f + 100f, 0f), new Vector2(200f, 50f), TextAnchor.MiddleLeft);
            d.horizontalOverflow = HorizontalWrapMode.Overflow;
            x += anchoTecla + 12f + d.preferredWidth + 46f;
        }
        // Centrar la fila según lo que ocupó
        float ocupado = x - 46f + 560f;
        foreach (RectTransform hijo in caja) hijo.anchoredPosition += new Vector2(-ocupado * 0.5f + 560f, 0f);
        caja.sizeDelta = new Vector2(ocupado + 70f, caja.sizeDelta.y);
    }

    RectTransform Tecla(RectTransform padre, string nombre, Vector2 pos, float ancho)
    {
        GameObject go = new GameObject("Tecla " + nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(ancho, 56f);
        Image img = go.AddComponent<Image>();
        img.color = new Color(1f, 0.88f, 0.45f);
        img.raycastTarget = false;
        if (spriteRedondo != null) { img.sprite = spriteRedondo; img.type = Image.Type.Sliced; }
        Text t = UIInka.Texto(rt, nombre, 26, new Color(0.08f, 0.12f, 0.16f), Vector2.zero, new Vector2(ancho, 56f));
        t.fontStyle = FontStyle.Bold;
        Destroy(t.GetComponent<Shadow>());
        return rt;
    }
}
