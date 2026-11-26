using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Piezas de interfaz con el estilo del Inka Sub (paneles, botones, textos y deslizadores).
// Los menús se arman solos desde código, así no hay que conectar nada a mano en el Inspector.
public static class UIInka
{
    public static Font fuente;
    public static Sprite spriteRedondo;   // borde redondeado (UISprite de Unity)
    // Estilo inca del juego 2D (si están, los botones, paneles y el volumen los usan)
    public static Sprite botonNormal, botonResaltado, botonPresionado, marcoPanel, rielDeslizador, perillaDeslizador;

    public static void Estilo(Sprite normal, Sprite resaltado, Sprite presionado, Sprite marco, Sprite riel, Sprite perilla)
    {
        botonNormal = normal; botonResaltado = resaltado; botonPresionado = presionado;
        marcoPanel = marco; rielDeslizador = riel; perillaDeslizador = perilla;
    }

    public static readonly Color Dorado = new Color(1f, 0.85f, 0.35f);
    public static readonly Color Turquesa = new Color(0.45f, 0.95f, 1f);
    public static readonly Color FondoPanel = new Color(0.02f, 0.1f, 0.16f, 0.88f);
    static readonly Color ColorBoton = new Color(0.05f, 0.27f, 0.36f, 0.95f);

    public static Canvas CrearCanvas(string nombre, int orden)
    {
        GameObject go = new GameObject(nombre);
        Canvas c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = orden;
        CanvasScaler escala = go.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920, 1080);
        escala.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        AsegurarEventSystem();
        return c;
    }

    // Sin EventSystem los botones no reciben clics (con el Input System nuevo)
    public static void AsegurarEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // Panel centrado; con tamaño cero cubre toda la pantalla
    public static RectTransform Panel(Transform padre, string nombre, Color fondo, Vector2 tamaño, Vector2 pos = default)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (tamaño == Vector2.zero)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        else
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = tamaño;
            rt.anchoredPosition = pos;
        }
        if (fondo.a > 0f && tamaño == Vector2.zero)
        {
            go.AddComponent<Image>().color = fondo;
        }
        else if (fondo.a > 0f && marcoPanel != null)
        {
            Image marco = go.AddComponent<Image>();
            marco.sprite = marcoPanel;
            marco.type = Image.Type.Sliced;
            marco.pixelsPerUnitMultiplier = 1.25f;   // marco un poco más fino en cajas grandes
            marco.color = Color.white;
            // Hijo "Interior" vacío: ahí van los textos y botones (igual que la caja simple)
            GameObject interior = new GameObject("Interior", typeof(RectTransform));
            interior.transform.SetParent(go.transform, false);
            RectTransform ri = interior.GetComponent<RectTransform>();
            ri.anchorMin = Vector2.zero; ri.anchorMax = Vector2.one;
            ri.offsetMin = ri.offsetMax = Vector2.zero;
        }
        else if (fondo.a > 0f)
        {
            // Caja: el Image de la raíz es el BORDE y "Interior" el relleno (un Outline teñiría todo el fondo)
            Image borde = go.AddComponent<Image>();
            borde.color = new Color(Turquesa.r, Turquesa.g, Turquesa.b, 0.7f);
            if (spriteRedondo != null) { borde.sprite = spriteRedondo; borde.type = Image.Type.Sliced; }
            GameObject interior = new GameObject("Interior", typeof(RectTransform));
            interior.transform.SetParent(go.transform, false);
            RectTransform ri = interior.GetComponent<RectTransform>();
            ri.anchorMin = Vector2.zero; ri.anchorMax = Vector2.one;
            ri.offsetMin = new Vector2(3f, 3f); ri.offsetMax = new Vector2(-3f, -3f);
            Image img = interior.AddComponent<Image>();
            img.color = new Color(fondo.r, fondo.g, fondo.b, 1f);
            img.raycastTarget = false;
            if (spriteRedondo != null) { img.sprite = spriteRedondo; img.type = Image.Type.Sliced; }
        }
        return rt;
    }

    public static Text Texto(Transform padre, string contenido, int tamaño, Color color, Vector2 pos, Vector2 caja, TextAnchor ancla = TextAnchor.MiddleCenter)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = caja;
        Text t = go.AddComponent<Text>();
        t.font = fuente != null ? fuente : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = tamaño;
        t.color = color;
        t.alignment = ancla;
        t.text = contenido;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        go.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);
        return t;
    }

    public static Button Boton(Transform padre, string texto, Vector2 pos, UnityAction accion, float ancho = 520f)
    {
        GameObject go = new GameObject("Boton " + texto, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(ancho, 84f);

        Image img = go.AddComponent<Image>();
        img.color = Color.white;
        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;
        if (botonNormal != null)
        {
            // Botón del 2D (barra con los adornos incas): cambia de dibujo al pasar el mouse o al presionarlo
            img.sprite = botonNormal;
            img.type = Image.Type.Sliced;
            b.transition = Selectable.Transition.SpriteSwap;
            SpriteState estado = new SpriteState
            {
                highlightedSprite = botonResaltado,
                selectedSprite = botonResaltado,
                pressedSprite = botonPresionado != null ? botonPresionado : botonResaltado
            };
            b.spriteState = estado;
            b.onClick.AddListener(accion);
            Text tx = Texto(go.transform, texto, 38, Color.white, new Vector2(0f, 2f), rt.sizeDelta - new Vector2(130f, 0f));
            tx.GetComponent<Shadow>().effectColor = new Color(0f, 0.05f, 0.1f, 0.9f);
            tx.resizeTextForBestFit = true; tx.resizeTextMinSize = 22; tx.resizeTextMaxSize = 38;
            tx.horizontalOverflow = HorizontalWrapMode.Wrap; tx.verticalOverflow = VerticalWrapMode.Truncate;   // así "Best Fit" achica el texto largo
            tx.rectTransform.sizeDelta = new Vector2(rt.sizeDelta.x - 130f, 48f);   // una sola línea
            go.AddComponent<BotonAnimado>().texto = tx;
            return b;
        }
        if (spriteRedondo != null) { img.sprite = spriteRedondo; img.type = Image.Type.Sliced; }
        go.AddComponent<Outline>().effectColor = new Color(Dorado.r, Dorado.g, Dorado.b, 0.6f);
        // El color cambia al pasar el mouse o al elegirlo con el teclado
        ColorBlock colores = b.colors;
        colores.normalColor = ColorBoton;
        colores.highlightedColor = new Color(0.1f, 0.5f, 0.6f, 1f);
        colores.selectedColor = new Color(0.1f, 0.5f, 0.6f, 1f);
        colores.pressedColor = new Color(0.75f, 0.6f, 0.2f, 1f);
        colores.fadeDuration = 0.08f;
        b.colors = colores;
        b.onClick.AddListener(accion);

        Text t = Texto(go.transform, texto, 40, Color.white, Vector2.zero, rt.sizeDelta);
        t.color = Dorado;
        return b;
    }

    public static Slider Deslizador(Transform padre, Vector2 pos, float valor, UnityAction<float> alCambiar)
    {
        GameObject go = new GameObject("Deslizador", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(520f, 36f);

        Image fondo = Hijo(go.transform, "Fondo", new Color(0.02f, 0.06f, 0.1f, 1f), 0f);
        RectTransform area = Hijo(go.transform, "Area", Color.clear, 0f).rectTransform;
        Image relleno = Hijo(area, "Relleno", Turquesa, 0f);
        RectTransform areaMango = Hijo(go.transform, "AreaMango", Color.clear, 18f).rectTransform;
        Image mango = Hijo(areaMango, "Mango", Dorado, 0f);
        mango.rectTransform.sizeDelta = new Vector2(36f, 52f);
        mango.rectTransform.anchorMin = mango.rectTransform.anchorMax = new Vector2(0f, 0.5f);

        Slider s = go.AddComponent<Slider>();
        s.fillRect = relleno.rectTransform;
        s.handleRect = mango.rectTransform;
        s.targetGraphic = mango;
        s.minValue = 0f; s.maxValue = 1f;
        s.value = valor;
        s.onValueChanged.AddListener(alCambiar);
        fondo.raycastTarget = true;
        if (rielDeslizador != null)
        {
            fondo.sprite = rielDeslizador; fondo.type = Image.Type.Simple; fondo.color = Color.white;
            rt.sizeDelta = new Vector2(560f, 34f);
            area.offsetMin = new Vector2(30f, 10f); area.offsetMax = new Vector2(-30f, -10f);
            relleno.color = new Color(0.45f, 1f, 0.9f, 0.55f);
        }
        if (perillaDeslizador != null)
        {
            mango.sprite = perillaDeslizador; mango.type = Image.Type.Simple; mango.preserveAspect = true;
            mango.color = Color.white;
            mango.rectTransform.sizeDelta = new Vector2(64f, 56f);
            ColorBlock cm = s.colors; cm.highlightedColor = cm.selectedColor = new Color(1f, 0.95f, 0.75f); s.colors = cm;
        }
        return s;
    }

    static Image Hijo(Transform padre, string nombre, Color color, float margenX)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margenX, 0f); rt.offsetMax = new Vector2(-margenX, 0f);
        Image img = go.AddComponent<Image>();
        img.color = color;
        if (spriteRedondo != null) { img.sprite = spriteRedondo; img.type = Image.Type.Sliced; }
        img.raycastTarget = color.a > 0f;
        return img;
    }

    // Deja elegido un botón: así también se puede usar el menú con flechas + Enter
    public static void Seleccionar(Selectable s)
    {
        if (EventSystem.current == null || s == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    // Volumen general del juego (se guarda entre partidas)
    public static float Volumen => PlayerPrefs.GetFloat("volumen", 0.8f);
    public static void CambiarVolumen(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat("volumen", v);
    }
}
