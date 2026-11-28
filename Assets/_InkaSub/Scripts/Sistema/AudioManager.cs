using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Lista de todos los efectos de sonido del juego
public enum Sonido
{
    Disparo, DisparoDoble, Recarga, Dash, DanoSubmarino, Curacion, Estacion,
    RecogerBacteria, Desbloqueo, EnemigoGolpeado, EnemigoMuere, AvisoEnemigo,
    Garra, DisparoEnemigo, BotonUI, TextoDialogo
}

// Controla TODA la música y los efectos de sonido de una escena.
// Los demás scripts solo dicen: AudioManager.Sonar(Sonido.Disparo);
public class AudioManager : MonoBehaviour
{
    public static AudioManager instancia;

    [Header("Música")]
    [Tooltip("En el menú: Musica_Menu. En el juego: Musica_Exploracion")]
    public AudioClip musicaPrincipal;
    public AudioClip musicaProfundidad;
    public AudioClip musicaCombate;
    [Tooltip("Música de la pelea contra el Iceberg de Basura (opcional hasta que la tengas)")]
    public AudioClip musicaJefe;   // NUEVO
    [Tooltip("Segundo de la canción del jefe donde empieza la parte fuerte (se salta la intro tranquila)")]
    public float inicioMusicaJefe = 0f;
    [Range(0f, 1f)] public float volumenJefe = 0.85f;
    [Tooltip("Golpe que suena justo cuando empieza la pelea (para que se sienta la llegada del jefe)")]
    public AudioClip golpeJefe;
    public AudioClip ambiente;
    public AudioClip jingleVictoria;
    public AudioClip jingleDerrota;

    [Header("Volúmenes")]
    [Range(0f, 1f)] public float volumenMusica = 0.5f;
    [Range(0f, 1f)] public float volumenAmbiente = 0.25f;
    [Range(0f, 1f)] public float volumenEfectos = 0.8f;

    [Header("Cambio automático de música")]
    [Tooltip("Pone la música de combate si hay enemigos cerca del submarino")]
    public bool cambiarPorCombate = true;
    public float radioCombate = 10f;
    [Tooltip("Pone la música de profundidad si el submarino baja de esta altura (Y)")]
    public bool cambiarPorProfundidad = true;
    public float alturaZonaProfunda = -20f;
    public float duracionTransicion = 1.5f;

    [Header("Efectos de sonido")]
    public AudioClip disparo;
    public AudioClip disparoDoble;
    public AudioClip recarga;
    public AudioClip dash;
    public AudioClip danoSubmarino;
    public AudioClip curacion;
    public AudioClip estacion;
    public AudioClip recogerBacteria;
    public AudioClip desbloqueo;
    public AudioClip enemigoGolpeado;
    public AudioClip enemigoMuere;
    public AudioClip avisoEnemigo;
    public AudioClip garra;
    public AudioClip disparoEnemigo;
    public AudioClip botonUI;
    public AudioClip textoDialogo;

    private AudioSource musicaA, musicaB, fuenteAmbiente, fuenteEfectos;
    private bool usandoA = true;
    private AudioClip musicaActual;
    private Transform jugador;
    private bool musicaDetenida = false;
    private bool musicaForzada = false;   // NUEVO: true mientras dura la pelea contra el jefe
    private Coroutine transicion;

    void Awake()
    {
        instancia = this;

        musicaA = CrearFuente(true);
        musicaB = CrearFuente(true);
        fuenteAmbiente = CrearFuente(true);
        fuenteEfectos = CrearFuente(false);

        // Volumen general guardado desde el menú de opciones
        AudioListener.volume = PlayerPrefs.GetFloat("volumen", 0.8f);
    }

    void Start()
    {
        if (ambiente != null)
        {
            fuenteAmbiente.clip = ambiente;
            fuenteAmbiente.volume = volumenAmbiente;
            fuenteAmbiente.Play();
        }

        CambiarMusica(musicaPrincipal);

        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;

        // Todos los botones de la escena suenan al hacer clic (sin configurar uno por uno)
        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            b.onClick.AddListener(() => Sonar(Sonido.BotonUI));

        InvokeRepeating(nameof(RevisarMusica), 1f, 0.5f);
    }

    AudioSource CrearFuente(bool enBucle)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.loop = enBucle;
        s.playOnAwake = false;
        s.ignoreListenerPause = true;
        return s;
    }

    // ---------- Para llamar desde cualquier script ----------

    public static void Sonar(Sonido sonido)
    {
        if (instancia == null) return;
        AudioClip clip = instancia.Obtener(sonido);
        if (clip == null) return;

        float vol = instancia.volumenEfectos;
        if (sonido == Sonido.TextoDialogo) vol *= 0.35f;   // el tic del texto va bajito
        if (sonido == Sonido.Disparo) vol *= 0.7f;          // se repite mucho, mejor más suave
        instancia.fuenteEfectos.PlayOneShot(clip, vol);
    }

    // Victoria o derrota: se apaga la música y suena el jingle
    public static void TocarJingle(bool victoria)
    {
        if (instancia == null) return;
        instancia.musicaDetenida = true;
        instancia.musicaA.Stop();
        instancia.musicaB.Stop();
        AudioClip clip = victoria ? instancia.jingleVictoria : instancia.jingleDerrota;
        if (clip != null) instancia.fuenteEfectos.PlayOneShot(clip, instancia.volumenMusica + 0.2f);
    }

    // Después de un jingle de derrota (al reaparecer), la música vuelve a sonar
    public static void ReanudarMusica()
    {
        if (instancia == null) return;
        instancia.musicaDetenida = false;
        instancia.musicaActual = null; // así RevisarMusica vuelve a ponerla
    }

    // NUEVO: empieza la música del jefe y evita que el cambio automático la pise.
    // Si todavía no asignaste "Musica Jefe", no hace nada y sigue la música normal.
    public static void IniciarMusicaJefe()
    {
        if (instancia == null) return;

        if (instancia.musicaJefe == null)
        {
            Debug.LogWarning("AudioManager: falta asignar Musica Jefe. Sigue la música normal.");
            return;
        }

        instancia.musicaForzada = true;
        instancia.musicaDetenida = false;
        if (instancia.golpeJefe != null) instancia.fuenteEfectos.PlayOneShot(instancia.golpeJefe, 1f);
        // Corte casi directo (sin el fundido largo) y desde la parte fuerte de la canción
        instancia.CambiarMusica(instancia.musicaJefe, 0.35f, instancia.volumenJefe, instancia.inicioMusicaJefe);
    }

    // NUEVO: termina el modo jefe. Vuelve el cambio automático de música.
    public static void TerminarMusicaJefe()
    {
        if (instancia == null) return;
        instancia.musicaForzada = false;
        instancia.musicaActual = null;
    }

    // Para las pruebas: qué canción suena ahora y a qué volumen
    public static string Estado()
    {
        if (instancia == null) return "sin AudioManager";
        AudioSource s = instancia.usandoA ? instancia.musicaA : instancia.musicaB;
        return (s.clip != null ? s.clip.name : "nada") + " · sonando=" + s.isPlaying + " · volumen=" + s.volume.ToString("0.00") + " · segundo=" + s.time.ToString("0.0");
    }

    // ---------- Interno ----------

    void RevisarMusica()
    {
        if (musicaDetenida || musicaForzada) return;   // CAMBIADO: también se detiene en modo jefe

        AudioClip elegida = musicaPrincipal;

        if (jugador != null)
        {
            if (cambiarPorProfundidad && musicaProfundidad != null && jugador.position.y < alturaZonaProfunda)
                elegida = musicaProfundidad;

            if (cambiarPorCombate && musicaCombate != null && HayEnemigosCerca())
                elegida = musicaCombate;
        }

        CambiarMusica(elegida);
    }

    bool HayEnemigosCerca()
    {
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag("Enemigo");
        foreach (GameObject e in enemigos)
        {
            if (Vector3.Distance(e.transform.position, jugador.position) < radioCombate) return true;
        }
        return false;
    }

    void CambiarMusica(AudioClip nueva) { CambiarMusica(nueva, duracionTransicion, volumenMusica, 0f); }

    void CambiarMusica(AudioClip nueva, float segundos, float volumen, float desde)
    {
        if (nueva == null || nueva == musicaActual) return;
        musicaActual = nueva;
        if (transicion != null) StopCoroutine(transicion);
        transicion = StartCoroutine(Transicion(nueva, Mathf.Max(0.05f, segundos), volumen, desde));
    }

    IEnumerator Transicion(AudioClip nueva, float duracion, float volumenFinal, float desde)
    {
        AudioSource sale = usandoA ? musicaA : musicaB;
        AudioSource entra = usandoA ? musicaB : musicaA;
        usandoA = !usandoA;

        entra.clip = nueva;
        entra.volume = 0f;
        entra.time = Mathf.Clamp(desde, 0f, Mathf.Max(0f, nueva.length - 1f));
        entra.Play();

        float t = 0f;
        float volSale = sale.volume;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float p = t / duracion;
            entra.volume = volumenFinal * p;
            sale.volume = volSale * (1f - p);
            yield return null;
        }
        entra.volume = volumenFinal;
        sale.Stop();
    }

    AudioClip Obtener(Sonido s)
    {
        switch (s)
        {
            case Sonido.Disparo: return disparo;
            case Sonido.DisparoDoble: return disparoDoble;
            case Sonido.Recarga: return recarga;
            case Sonido.Dash: return dash;
            case Sonido.DanoSubmarino: return danoSubmarino;
            case Sonido.Curacion: return curacion;
            case Sonido.Estacion: return estacion;
            case Sonido.RecogerBacteria: return recogerBacteria;
            case Sonido.Desbloqueo: return desbloqueo;
            case Sonido.EnemigoGolpeado: return enemigoGolpeado;
            case Sonido.EnemigoMuere: return enemigoMuere;
            case Sonido.AvisoEnemigo: return avisoEnemigo;
            case Sonido.Garra: return garra;
            case Sonido.DisparoEnemigo: return disparoEnemigo;
            case Sonido.BotonUI: return botonUI;
            case Sonido.TextoDialogo: return textoDialogo;
        }
        return null;
    }
}