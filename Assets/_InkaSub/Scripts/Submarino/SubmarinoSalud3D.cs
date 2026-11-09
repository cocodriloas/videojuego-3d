using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Vida del submarino en 3D. Los enemigos y obstáculos llaman a RecibirDaño(cantidad).
// Al quedarse sin vida reaparece en el último cangrejo (PuntoControl3D) con la vida llena.
public class SubmarinoSalud3D : MonoBehaviour
{
    public static SubmarinoSalud3D instancia;

    [Header("Vida del submarino")]
    public int vidaMaxima = 6;
    private int vidaActual;

    [Header("Invulnerabilidad tras recibir daño")]
    public float tiempoInvulnerabilidad = 1f;
    [Tooltip("Qué tan rápido parpadea el modelo mientras es invulnerable")]
    public float velocidadParpadeo = 15f;

    [Header("UI (opcional)")]
    public Text textoVida;
    [Tooltip("Imagen roja a pantalla completa (transparente) que destella al recibir daño")]
    public Image imagenFlashDanio;
    public float duracionFlash = 0.15f;

    [Header("Derrota")]
    [Tooltip("Segundos antes de reaparecer en el último punto de control")]
    public float esperaReaparecer = 2f;

    private float tiempoRestanteInvulnerable = 0f;
    private EscudoSubmarino escudo;
    private bool estaDerrotado = false;
    private Renderer[] renderers;
    private Rigidbody rb;
    private EfectosSubmarino efectos;
    private Vector3 posicionInicial;

    public int VidaActual => vidaActual;
    public bool EstaDerrotado => estaDerrotado;

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        efectos = GetComponent<EfectosSubmarino>();
        renderers = GetComponentsInChildren<Renderer>();
        posicionInicial = transform.position;
        vidaActual = vidaMaxima;
        ActualizarUI();
    }

    void Update()
    {
        if (estaDerrotado) return; // no parpadear mientras está "hundido"
        if (tiempoRestanteInvulnerable > 0)
        {
            tiempoRestanteInvulnerable -= Time.deltaTime;
            bool visible = tiempoRestanteInvulnerable <= 0 || Mathf.Sin(Time.time * velocidadParpadeo) > 0f;
            MostrarModelo(visible);
        }
    }

    void OnCollisionEnter(Collision colision) { RevisarChoque(colision.gameObject); }
    void OnCollisionStay(Collision colision) { RevisarChoque(colision.gameObject); }

    void RevisarChoque(GameObject otro)
    {
        if (!otro.CompareTag("Enemigo")) return;
        // El Fantasma de Red daña con su garra (2); si el choque quitara 1 antes, la garra no haría nada
        if (otro.GetComponentInParent<EnemigoGarra3D>() != null) return;
        RecibirDaño(1);
    }

    private SubmarinoMovimiento3D movimiento;

    public void RecibirDaño(int cantidad)
    {
        if (estaDerrotado || tiempoRestanteInvulnerable > 0) return;
        // Dash: inmune mientras dura (se esquiva atravesando el ataque)
        if (movimiento == null) movimiento = GetComponent<SubmarinoMovimiento3D>();
        if (movimiento != null && movimiento.InmunePorDash) { if (efectos != null) efectos.Esquive(); return; }
        // Escudo de Burbujas activo: el golpe no hace nada
        if (escudo == null) escudo = GetComponent<EscudoSubmarino>();
        if (escudo != null && escudo.Absorber()) return;

        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        tiempoRestanteInvulnerable = tiempoInvulnerabilidad;
        ActualizarUI();
        if (imagenFlashDanio != null) StartCoroutine(FlashDaño());
        AudioManager.Sonar(Sonido.DanoSubmarino);
        if (efectos != null) efectos.Daño();

        if (vidaActual <= 0) StartCoroutine(Derrota());
    }

    public void Curar(int cantidad)
    {
        if (estaDerrotado) return;
        vidaActual = Mathf.Min(vidaActual + cantidad, vidaMaxima);
        ActualizarUI();
        AudioManager.Sonar(Sonido.Curacion);
        if (efectos != null) efectos.Curacion();
    }

    public void CurarCompleto()
    {
        Curar(vidaMaxima);
    }

    IEnumerator Derrota()
    {
        estaDerrotado = true;
        tiempoRestanteInvulnerable = 0f;
        MostrarModelo(false);
        if (rb != null) rb.linearVelocity = Vector3.zero;
        SetControles(false);
        // Si una red lo tenía atrapado, que lo suelte (si no, lo arrastraría al reaparecer)
        SubmarinoMovimiento3D mov = GetComponent<SubmarinoMovimiento3D>();
        if (mov != null) mov.Liberar();
        AudioManager.TocarJingle(false);

        yield return new WaitForSeconds(esperaReaparecer);

        // Pantalla de derrota: el juego espera a que elijas Continuar (Reiniciar o Menú cargan otra escena)
        if (MenuJuego3D.instancia != null)
        {
            bool continuar = false;
            MenuJuego3D.instancia.MostrarDerrota(() => continuar = true);
            yield return new WaitUntil(() => continuar);
        }

        // Reaparecer en el último cangrejo (o en el inicio)
        Vector3 destino = PuntoControl3D.Ultimo != null ? PuntoControl3D.Ultimo.PosicionReaparicion : posicionInicial;
        if (rb != null) { rb.position = destino; rb.linearVelocity = Vector3.zero; }
        transform.position = destino;

        vidaActual = vidaMaxima;
        estaDerrotado = false;
        tiempoRestanteInvulnerable = tiempoInvulnerabilidad * 2f;
        SetControles(true);
        AudioManager.ReanudarMusica();
        ActualizarUI();
    }

    void SetControles(bool activos)
    {
        // Linterna y burbujas: apagadas mientras está hundido
        LinternaSubmarino linterna = GetComponent<LinternaSubmarino>();
        HeliceSubmarino helice = GetComponent<HeliceSubmarino>();
        if (linterna != null) linterna.Suspender(!activos);
        if (helice != null) helice.Pausar(!activos);
        SubmarinoMovimiento3D mov = GetComponent<SubmarinoMovimiento3D>();
        SubmarinoDisparo3D disp = GetComponent<SubmarinoDisparo3D>();
        if (mov != null) mov.enabled = activos;
        if (disp != null) disp.enabled = activos;
    }

    void MostrarModelo(bool visible)
    {
        if (renderers == null) return;
        // Solo el casco y sus piezas: ni estelas, ni partículas, ni el haz de la linterna
        foreach (Renderer r in renderers)
            if (r != null && r is MeshRenderer && r.name != "Haz") r.enabled = visible;
    }

    IEnumerator FlashDaño()
    {
        Color c = imagenFlashDanio.color;
        c.a = 0.4f;
        imagenFlashDanio.color = c;
        yield return new WaitForSeconds(duracionFlash);
        c.a = 0f;
        imagenFlashDanio.color = c;
    }

    // Un bloque por punto de casco (con imágenes: la fuente pixel no trae ■ □ y en la web no se verían)
    private Image[] bloquesVida;

    void ActualizarUI()
    {
        if (textoVida == null) return;
        textoVida.text = "Casco";
        if (bloquesVida == null || bloquesVida.Length != vidaMaxima)
        {
            if (bloquesVida != null) foreach (Image b in bloquesVida) if (b != null) Destroy(b.gameObject);
            bloquesVida = new Image[vidaMaxima];
            for (int i = 0; i < vidaMaxima; i++)
            {
                GameObject go = new GameObject("BloqueVida" + i, typeof(RectTransform));
                go.transform.SetParent(textoVida.transform, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                // El texto está alineado abajo a la izquierda: los bloques van a la altura de sus letras
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(26f, 26f);
                rt.anchoredPosition = new Vector2(150f + i * 34f, 24f);
                bloquesVida[i] = go.AddComponent<Image>();
                bloquesVida[i].raycastTarget = false;
                go.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
            }
        }
        for (int i = 0; i < vidaMaxima; i++)
            bloquesVida[i].color = i < vidaActual ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 1f, 1f, 0.18f);
    }
}
