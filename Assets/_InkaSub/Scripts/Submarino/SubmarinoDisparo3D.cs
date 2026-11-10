using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Disparo de bacterias en 3D (misma lógica que el juego 2D):
// clic izquierdo = disparar, R = recargar, K = Disparo Doble Disolvente (se desbloquea con 3 bacterias).
public class SubmarinoDisparo3D : MonoBehaviour
{
    [Header("Configuración de disparo")]
    public GameObject prefabProyectil;
    [Tooltip("Solo se usa si no asignas las bocas de abajo")]
    public float distanciaSpawn = 1f;

    [Header("Bocas de los cañones (objetos vacíos HIJOS de 'Modelo')")]
    [Tooltip("Punta del cañón normal")]
    public Transform bocaCanon;
    [Tooltip("Punta del cañón de arriba del Doble Cañón")]
    public Transform bocaDobleArriba;
    [Tooltip("Punta del cañón de abajo del Doble Cañón")]
    public Transform bocaDobleAbajo;

    [Header("Munición (bacterias)")]
    public int balasMaximas = 20;
    public float tiempoRecarga = 1.5f;
    private int balasActuales;
    private bool recargando = false;
    private Coroutine rutinaRecarga;

    [Header("UI de munición (opcional)")]
    public Text textoMunicion;
    public GameObject objetoRecargando;

    [Header("Habilidad: Disparo Doble Disolvente (se desbloquea con 3 bacterias)")]
    public bool habilidadDesbloqueada = false;
    public float duracionHabilidad = 5f;
    public float cooldownHabilidad = 15f;
    public float separacionDisparoDoble = 0.3f;
    [Tooltip("Durante la habilidad: munición infinita y disparo automático manteniendo el clic")]
    public float cadenciaDisparoDoble = 0.12f;

    [Header("Pieza visual del doble cañón (opcional, se muestra durante la habilidad)")]
    public GameObject visualDobleCanon;

    private bool dobleDisparoActivo = false;
    private float cooldownRestante = 0f;
    private float activoRestante = 0f;
    private float siguienteDisparoAuto = 0f;
    private SubmarinoApuntar3D apuntado;
    private Collider[] misColliders;
    private EfectosSubmarino efectos;

    // ---------- Datos para el HUD ----------
    public int BalasActuales => balasActuales;
    public bool Recargando => recargando;
    public int BalasMaximas => balasMaximas;
    // Mientras recarga va de 0 a 1 (para el ícono del HUD)
    public float ProgresoRecarga => recargando ? Mathf.Clamp01((Time.time - inicioRecarga) / Mathf.Max(0.01f, tiempoRecarga)) : 1f;
    private float inicioRecarga;
    public bool HabilidadDesbloqueada => habilidadDesbloqueada;
    public bool HabilidadActiva => dobleDisparoActivo;
    public bool HabilidadLista => habilidadDesbloqueada && !dobleDisparoActivo && cooldownRestante <= 0f;
    // 1 = llena, 0 = vacía (mientras está activa baja; mientras recarga sube)
    public float ProgresoHabilidad
    {
        get
        {
            if (!habilidadDesbloqueada) return 0f;
            if (dobleDisparoActivo) return Mathf.Clamp01(activoRestante / Mathf.Max(0.01f, duracionHabilidad));
            return 1f - Mathf.Clamp01(cooldownRestante / Mathf.Max(0.01f, cooldownHabilidad));
        }
    }

    void Start()
    {
        apuntado = GetComponent<SubmarinoApuntar3D>();
        efectos = GetComponent<EfectosSubmarino>();
        misColliders = GetComponentsInChildren<Collider>();

        balasActuales = balasMaximas;
        ActualizarUIMunicion();
        if (objetoRecargando != null) objetoRecargando.SetActive(false);
        if (visualDobleCanon != null) visualDobleCanon.SetActive(false);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // juego en pausa: no disparar
        Mouse mouse = Mouse.current;
        Keyboard teclado = Keyboard.current;
        if (mouse == null || teclado == null) return;

        if (cooldownRestante > 0) cooldownRestante -= Time.deltaTime;
        if (dobleDisparoActivo) activoRestante -= Time.deltaTime;

        if (dobleDisparoActivo)
        {
            // Munición infinita: basta con mantener el clic
            if (mouse.leftButton.isPressed && Time.time >= siguienteDisparoAuto)
            {
                Disparar();
                siguienteDisparoAuto = Time.time + cadenciaDisparoDoble;
            }
        }
        else if (mouse.leftButton.wasPressedThisFrame && !recargando)
        {
            Disparar();
        }

        // Recargar a mano con R
        if (teclado.rKey.wasPressedThisFrame && !recargando && !dobleDisparoActivo && balasActuales < balasMaximas)
            rutinaRecarga = StartCoroutine(Recargar());

        // Activar la habilidad con K o la tecla 2
        if (teclado.kKey.wasPressedThisFrame || teclado.digit2Key.wasPressedThisFrame)
        {
            if (HabilidadLista) StartCoroutine(ActivarDobleDisparoTemporal());
            else if (!habilidadDesbloqueada) Debug.Log("Disparo doble bloqueado: se desbloquea con 3 bacterias (o marca 'Habilidad Desbloqueada' en el Submarino).");
        }
    }

    void Disparar()
    {
        if (prefabProyectil == null) { Debug.LogWarning("SubmarinoDisparo3D: falta asignar el prefab del proyectil."); return; }
        if (!dobleDisparoActivo && balasActuales <= 0) return;

        Vector3 direccion = apuntado != null ? apuntado.DireccionApuntada : transform.right;

        if (!dobleDisparoActivo)
        {
            Vector3 puntoDeSpawn = bocaCanon != null ? bocaCanon.position : transform.position + direccion * distanciaSpawn;

            // Desde la boca del cañón, directo hacia el punto del mouse
            CrearProyectil(puntoDeSpawn, DireccionDesde(puntoDeSpawn, direccion));
            balasActuales -= 1;
            AudioManager.Sonar(Sonido.Disparo);
            if (efectos != null) efectos.Disparo(puntoDeSpawn);
        }
        else
        {
            Vector3 arriba, abajo;
            if (bocaDobleArriba != null && bocaDobleAbajo != null)
            {
                arriba = bocaDobleArriba.position;
                abajo = bocaDobleAbajo.position;
            }
            else
            {
                // Perpendicular a la dirección, dentro del plano que ve la cámara
                Vector3 lado = Vector3.Cross(direccion, Camera.main != null ? Camera.main.transform.forward : Vector3.forward).normalized;
                Vector3 centro = transform.position + direccion * distanciaSpawn;
                arriba = centro + lado * separacionDisparoDoble;
                abajo = centro - lado * separacionDisparoDoble;
            }

            // Las dos balas salen paralelas, en la dirección del mouse.
            // Durante la habilidad NO se gastan bacterias.
            CrearProyectil(arriba, direccion);
            CrearProyectil(abajo, direccion);
            AudioManager.Sonar(Sonido.DisparoDoble);
            if (efectos != null) efectos.Disparo((arriba + abajo) * 0.5f);
        }

        if (balasActuales < 0) balasActuales = 0;
        ActualizarUIMunicion();

        if (!dobleDisparoActivo && balasActuales <= 0 && !recargando)
            rutinaRecarga = StartCoroutine(Recargar());
    }

    void CrearProyectil(Vector3 posicion, Vector3 direccion)
    {
        GameObject p = Instantiate(prefabProyectil, posicion, Quaternion.LookRotation(direccion, Vector3.up));

        // Que el proyectil no choque con el propio submarino
        Collider cp = p.GetComponent<Collider>();
        if (cp != null)
            foreach (Collider c in misColliders)
                if (c != null) Physics.IgnoreCollision(cp, c);   // algunos colliders de los modelos se quitan al empezar: saltarlos (antes cortaba la 2.ª bala del doble cañón)
    }

    Vector3 DireccionDesde(Vector3 desde, Vector3 porDefecto)
    {
        if (apuntado == null) return porDefecto;
        Vector3 dir = apuntado.PuntoApuntado - desde;
        // Si el mouse está casi encima de la boca, usamos la dirección normal
        return dir.sqrMagnitude < 0.25f ? porDefecto : dir.normalized;
    }

    // Dibuja las bocas en la Scene para ubicarlas fácil (solo en el editor)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (bocaCanon != null) Gizmos.DrawWireSphere(bocaCanon.position, 0.12f);
        Gizmos.color = Color.cyan;
        if (bocaDobleArriba != null) Gizmos.DrawWireSphere(bocaDobleArriba.position, 0.12f);
        if (bocaDobleAbajo != null) Gizmos.DrawWireSphere(bocaDobleAbajo.position, 0.12f);
    }

    private IEnumerator Recargar()
    {
        recargando = true;
        inicioRecarga = Time.time;
        if (objetoRecargando != null) objetoRecargando.SetActive(true);
        AudioManager.Sonar(Sonido.Recarga);
        if (efectos != null) efectos.InicioRecarga(tiempoRecarga);
        ActualizarUIMunicion();

        yield return new WaitForSeconds(tiempoRecarga);

        TerminarRecarga();
    }

    void TerminarRecarga()
    {
        balasActuales = balasMaximas;
        recargando = false;
        rutinaRecarga = null;
        if (efectos != null) efectos.FinRecarga();
        if (objetoRecargando != null) objetoRecargando.SetActive(false);
        ActualizarUIMunicion();
    }

    void ActualizarUIMunicion()
    {
        if (textoMunicion == null) return;
        if (dobleDisparoActivo) textoMunicion.text = "MAX";
        else if (recargando) textoMunicion.text = "Recargando...";
        else textoMunicion.text = balasActuales + " / " + balasMaximas;
    }

    private IEnumerator ActivarDobleDisparoTemporal()
    {
        // Si estaba recargando, la habilidad termina la recarga al instante
        if (recargando)
        {
            if (rutinaRecarga != null) StopCoroutine(rutinaRecarga);
            TerminarRecarga();
        }

        dobleDisparoActivo = true;
        activoRestante = duracionHabilidad;
        ActualizarUIMunicion();
        if (visualDobleCanon != null) visualDobleCanon.SetActive(true);

        yield return new WaitForSeconds(duracionHabilidad);

        dobleDisparoActivo = false;
        activoRestante = 0f;
        if (visualDobleCanon != null) visualDobleCanon.SetActive(false);
        cooldownRestante = cooldownHabilidad;

        balasActuales = balasMaximas; // sale de la habilidad con el cargador lleno
        ActualizarUIMunicion();
    }
}
