using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menús DENTRO del nivel (como el GameManager del 2D): pausa (Esc o P), derrota y victoria.
// Arma su propia interfaz al empezar. Va en un objeto "MenuJuego" de la escena del nivel.
public class MenuJuego3D : MonoBehaviour
{
    public static MenuJuego3D instancia;

    [Header("Escena del menú principal (nombre EXACTO)")]
    public string escenaMenu = "MenuPrincipal";

    [Header("Estilo (lo asigna el constructor del nivel)")]
    public Font fuente;
    public Sprite spriteRedondo;
    [Tooltip("Dibujos del 2D para botones, paneles y volumen (los mismos del menú principal)")]
    public Sprite botonNormal;
    public Sprite botonResaltado, botonPresionado, marcoPanel, rielDeslizador, perillaDeslizador;

    [Header("Datos que se muestran al perder")]
    [TextArea]
    public string[] datosAlPerder =
    {
        "Una botella de plástico puede tardar unos 450 años en degradarse en el mar.",
        "La bacteria Ideonella sakaiensis, descubierta en Japón en 2016, puede descomponer el plástico PET.",
        "Las redes de pesca abandonadas (redes fantasma) siguen atrapando animales durante años.",
        "El ODS 14 busca conservar y usar de forma sostenible los océanos y los recursos marinos.",
        "Los microplásticos ya se encuentran en peces, aves y hasta en el agua que bebemos."
    };

    private GameObject panelPausa, panelDerrota, panelVictoria;
    private Button primeroPausa, primeroDerrota, primeroVictoria;
    private Text textoDato, tituloVictoria, subtituloVictoria;
    private Slider volumenPausa;
    private bool pausado, terminado;
    private Action alContinuar;

    public bool EstaPausado => pausado;
    // Pausa, derrota o victoria en pantalla
    public bool HayMenuAbierto => pausado || terminado;

    void Awake()
    {
        instancia = this;
        Time.timeScale = 1f;
        UIInka.fuente = fuente;
        UIInka.spriteRedondo = spriteRedondo;
        UIInka.Estilo(botonNormal, botonResaltado, botonPresionado, marcoPanel, rielDeslizador, perillaDeslizador);
        Construir();
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null || terminado || FichaCientifica.Abierta || HUDSubmarino.Anunciando) return;   // la ficha científica ya pausa el juego
        if (teclado.escapeKey.wasPressedThisFrame || teclado.pKey.wasPressedThisFrame)
        {
            if (pausado) Reanudar();
            else Pausar();
        }
    }

    // ---------- Pausa ----------

    public void Pausar()
    {
        if (terminado) return;
        pausado = true;
        volumenPausa.SetValueWithoutNotify(UIInka.Volumen);
        panelPausa.SetActive(true);
        Time.timeScale = 0f;
        UIInka.Seleccionar(primeroPausa);
    }

    public void Reanudar()
    {
        pausado = false;
        panelPausa.SetActive(false);
        Time.timeScale = 1f;
    }

    // ---------- Derrota y victoria ----------

    // El submarino se hundió: el juego se congela hasta que el jugador elija qué hacer.
    // "continuar" se llama al elegir Continuar (reaparece en el último cangrejo).
    public void MostrarDerrota(Action continuar)
    {
        if (pausado) Reanudar();
        terminado = true;
        alContinuar = continuar;
        if (datosAlPerder.Length > 0)
            textoDato.text = "¿Sabías que...?\n" + datosAlPerder[UnityEngine.Random.Range(0, datosAlPerder.Length)];
        panelDerrota.SetActive(true);
        Time.timeScale = 0f;
        UIInka.Seleccionar(primeroDerrota);
    }

    public void MostrarVictoria(string titulo, string subtitulo)
    {
        if (pausado) Reanudar();
        terminado = true;
        ProgresoJuego3D.Borrar();   // juego terminado: la próxima vez se empieza de cero
        tituloVictoria.text = titulo;
        subtituloVictoria.text = subtitulo;
        panelVictoria.SetActive(true);
        Time.timeScale = 0f;
        UIInka.Seleccionar(primeroVictoria);
    }

    void Continuar()
    {
        terminado = false;
        panelDerrota.SetActive(false);
        Time.timeScale = 1f;
        Action a = alContinuar;
        alContinuar = null;
        a?.Invoke();
    }

    // ---------- Botones ----------

    // Empieza el nivel desde el inicio: borra la partida guardada (si no, cargaría el último cangrejo)
    public void ReiniciarNivel()
    {
        ProgresoJuego3D.Borrar();
        Time.timeScale = 1f; // importante: restaurar el tiempo antes de recargar
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(escenaMenu)) SceneManager.LoadScene(escenaMenu);
        else Debug.LogWarning("Falta la escena '" + escenaMenu + "' en Build Settings (menú Inka Sub → Crear Menú Principal).");
    }

    // ---------- Interfaz ----------

    void Construir()
    {
        Transform canvas = UIInka.CrearCanvas("Menus del juego", 50).transform;
        Color velo = new Color(0f, 0.04f, 0.08f, 0.7f);

        // Pausa
        panelPausa = UIInka.Panel(canvas, "Pausa", velo, Vector2.zero).gameObject;
        Transform p = UIInka.Panel(panelPausa.transform, "Caja", UIInka.FondoPanel, new Vector2(780, 900));
        UIInka.Texto(p, "PAUSA", 84, UIInka.Dorado, new Vector2(0, 320), new Vector2(700, 110));
        primeroPausa = UIInka.Boton(p, "Continuar", new Vector2(0, 180), Reanudar);
        UIInka.Boton(p, "Empezar desde el inicio", new Vector2(0, 80), ReiniciarNivel);
        UIInka.Boton(p, "Menú principal", new Vector2(0, -20), IrAlMenu);
        UIInka.Texto(p, "Volumen", 34, Color.white, new Vector2(0, -115), new Vector2(520, 50));
        volumenPausa = UIInka.Deslizador(p, new Vector2(0, -165), UIInka.Volumen, UIInka.CambiarVolumen);
        UIInka.Texto(p, "WASD mover · Mouse apuntar · Clic disparar · R recargar · Rueda: túnel\n1 dash · 2 disparo doble · 3 escudo · F linterna · E hablar · Esc pausa", 24,
            new Color(0.8f, 0.9f, 0.95f), new Vector2(0, -290), new Vector2(700, 90));
        panelPausa.SetActive(false);

        // Derrota
        panelDerrota = UIInka.Panel(canvas, "Derrota", new Color(0.12f, 0.02f, 0.03f, 0.72f), Vector2.zero).gameObject;
        Transform d = UIInka.Panel(panelDerrota.transform, "Caja", UIInka.FondoPanel, new Vector2(1040, 840));
        UIInka.Texto(d, "¡EL INKA SUB SE HUNDIÓ!", 72, new Color(1f, 0.45f, 0.4f), new Vector2(0, 275), new Vector2(820, 110));
        textoDato = UIInka.Texto(d, "", 30, UIInka.Turquesa, new Vector2(0, 150), new Vector2(820, 140));
        primeroDerrota = UIInka.Boton(d, "Continuar desde el cangrejo", new Vector2(0, 10), Continuar, 640f);
        UIInka.Boton(d, "Empezar desde el inicio", new Vector2(0, -90), ReiniciarNivel, 640f);
        UIInka.Boton(d, "Menú principal", new Vector2(0, -190), IrAlMenu, 640f);
        panelDerrota.SetActive(false);

        // Victoria
        panelVictoria = UIInka.Panel(canvas, "Victoria", velo, Vector2.zero).gameObject;
        Transform v = UIInka.Panel(panelVictoria.transform, "Caja", UIInka.FondoPanel, new Vector2(1320, 820));
        tituloVictoria = UIInka.Texto(v, "", 84, UIInka.Dorado, new Vector2(0, 200), new Vector2(1100, 200));
        subtituloVictoria = UIInka.Texto(v, "", 34, Color.white, new Vector2(0, 30), new Vector2(1080, 140));
        primeroVictoria = UIInka.Boton(v, "Jugar de nuevo", new Vector2(0, -130), ReiniciarNivel, 600f);
        UIInka.Boton(v, "Menú principal", new Vector2(0, -230), IrAlMenu, 600f);
        panelVictoria.SetActive(false);
    }
}
