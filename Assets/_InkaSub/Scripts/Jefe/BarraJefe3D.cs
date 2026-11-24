using UnityEngine;
using UnityEngine.UI;

// Barra de vida del Iceberg de Basura, arriba al centro (con los dibujos de la barra del juego 2D):
//  - Marco dorado con la gema roja, relleno por segmentos que cambia de color en cada fase.
//  - Estela blanca de "vida perdida" que baja despacio (se nota cuánto le quitaste).
//  - Marcas donde cambia de fase, el nombre de la fase y "FASE x / 3".
//  - Tiembla y destella al recibir un golpe; un escudo celeste cuando es invulnerable.
// La crea JefeIceberg3D; se arma sola dentro del HUD.
public class BarraJefe3D : MonoBehaviour
{
    public Sprite fondo, relleno, marco;
    public Font fuente;
    public string nombre = "ICEBERG DE BASURA";
    public string[] nombresFase = { "Coloso de plástico", "Se está agrietando", "¡A punto de romperse!" };
    public float[] umbrales = { 0.66f, 0.33f };

    static readonly Color[] ColorFase = { new Color(1f, 1f, 1f), new Color(1f, 0.72f, 0.35f), new Color(0.95f, 0.4f, 1f) };

    private RectTransform raiz, zonaBarra;
    private Image imgRelleno, imgEstela, imgEscudo, imgDestello;
    private Text txtFase, txtSubtitulo;
    private float objetivo = 1f, estela = 1f, temblor, destello;
    private int fase = 1;
    private bool invulnerable;
    private Vector2 posBase;

    public void Construir(Transform hud)
    {
        raiz = Rect("BarraJefe", hud, new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(1100f, 120f));
        raiz.pivot = new Vector2(0.5f, 1f);
        posBase = raiz.anchoredPosition;

        Texto(raiz, nombre, 40, new Color(1f, 0.85f, 0.45f), new Vector2(0f, -20f), new Vector2(1000f, 50f), TextAnchor.MiddleCenter);
        txtSubtitulo = Texto(raiz, "", 24, new Color(1f, 0.7f, 0.6f), new Vector2(0f, -100f), new Vector2(1000f, 30f), TextAnchor.MiddleCenter);

        zonaBarra = Rect("Barra", raiz, new Vector2(0.5f, 1f), new Vector2(26f, -66f), new Vector2(920f, 36f));
        Imagen(zonaBarra, "Fondo", fondo, new Color(0.35f, 0.25f, 0.3f), Vector2.zero, Vector2.zero, true);
        imgEstela = Imagen(zonaBarra, "Estela", relleno, new Color(1f, 1f, 0.85f, 0.85f), Vector2.zero, Vector2.zero, true);
        Rellenar(imgEstela);
        imgRelleno = Imagen(zonaBarra, "Relleno", relleno, Color.white, Vector2.zero, Vector2.zero, true);
        Rellenar(imgRelleno);
        imgDestello = Imagen(zonaBarra, "Destello", null, new Color(1f, 1f, 1f, 0f), Vector2.zero, Vector2.zero, true);
        imgEscudo = Imagen(zonaBarra, "Escudo", null, new Color(0.4f, 0.9f, 1f, 0f), Vector2.zero, Vector2.zero, true);
        // Marcas de cambio de fase
        foreach (float u in umbrales)
        {
            RectTransform m = Rect("Marca fase", zonaBarra, new Vector2(u, 0.5f), Vector2.zero, new Vector2(4f, 46f));
            m.gameObject.AddComponent<Image>().color = new Color(1f, 0.85f, 0.4f, 0.9f);
        }
        // Marco del 2D por encima de todo (la gema roja queda a la izquierda)
        RectTransform rm = Imagen(raiz, "Marco", marco, Color.white, Vector2.zero, new Vector2(1010f, 79f), false).rectTransform;
        rm.anchorMin = rm.anchorMax = new Vector2(0.5f, 1f);
        rm.anchoredPosition = new Vector2(0f, -66f);
        txtFase = Texto(zonaBarra, "", 22, Color.white, new Vector2(0f, 0f), new Vector2(300f, 36f), TextAnchor.MiddleCenter);

        Fijar(1f, 1, false);
        raiz.gameObject.SetActive(false);
    }

    public void Mostrar(bool v)
    {
        if (raiz == null) return;
        raiz.gameObject.SetActive(v);
        if (v) { estela = objetivo; }
        // Durante la pelea se esconde la ayuda de controles de arriba (la barra la tapaba)
        Transform ayuda = raiz.parent != null ? raiz.parent.Find("Ayuda") : null;
        if (ayuda != null) ayuda.gameObject.SetActive(!v);
    }

    public void Fijar(float fraccion, int nuevaFase, bool esInvulnerable)
    {
        if (fraccion < objetivo - 0.0001f) { temblor = 0.2f; destello = 1f; }
        objetivo = Mathf.Clamp01(fraccion);
        if (nuevaFase != fase) temblor = 0.6f;
        fase = Mathf.Clamp(nuevaFase, 1, 3);
        invulnerable = esInvulnerable;
        txtFase.text = "FASE " + fase + " / 3";
        txtSubtitulo.text = nombresFase.Length >= fase ? nombresFase[fase - 1] : "";
    }

    void Update()
    {
        if (raiz == null || !raiz.gameObject.activeSelf) return;
        float dt = Time.unscaledDeltaTime;
        // La estela espera un momento y baja despacio hasta la vida real
        estela = estela > objetivo ? Mathf.MoveTowards(estela, objetivo, dt * 0.25f) : objetivo;
        imgRelleno.fillAmount = Mathf.Lerp(imgRelleno.fillAmount, objetivo, 1f - Mathf.Exp(-dt * 18f));
        imgEstela.fillAmount = estela;
        imgRelleno.color = Color.Lerp(imgRelleno.color, ColorFase[fase - 1], dt * 4f);

        destello = Mathf.MoveTowards(destello, 0f, dt * 6f);
        imgDestello.color = new Color(1f, 1f, 1f, destello * 0.35f);
        float k = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
        imgEscudo.color = new Color(0.4f, 0.9f, 1f, invulnerable ? 0.25f + k * 0.25f : 0f);

        temblor = Mathf.MoveTowards(temblor, 0f, dt);
        raiz.anchoredPosition = posBase + (temblor > 0f ? Random.insideUnitCircle * 8f * temblor / 0.6f : Vector2.zero);
        // En la fase final el nombre late en rojo
        if (fase == 3) txtSubtitulo.color = Color.Lerp(new Color(1f, 0.5f, 0.5f), new Color(1f, 0.2f, 0.4f), k);
    }

    // ---------- Piezas de interfaz ----------

    static RectTransform Rect(string n, Transform padre, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        GameObject go = new GameObject(n, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = ancla;
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
        return r;
    }

    static Image Imagen(RectTransform padre, string n, Sprite s, Color c, Vector2 pos, Vector2 tam, bool estirar)
    {
        RectTransform r = Rect(n, padre, new Vector2(0.5f, 0.5f), pos, tam);
        if (estirar) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = s;
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    static void Rellenar(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
    }

    Text Texto(RectTransform padre, string s, int tam, Color c, Vector2 pos, Vector2 caja, TextAnchor al)
    {
        RectTransform r = Rect("Texto", padre, new Vector2(0.5f, 1f), pos, caja);
        if (padre == zonaBarra) r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        Text t = r.gameObject.AddComponent<Text>();
        t.font = fuente != null ? fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = tam;
        t.color = c;
        t.alignment = al;
        t.text = s;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        r.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        return t;
    }
}
