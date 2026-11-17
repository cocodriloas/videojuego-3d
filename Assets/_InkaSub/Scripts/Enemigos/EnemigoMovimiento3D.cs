using UnityEngine;

// Mueve a un enemigo 3D hacia el submarino usando su Rigidbody (así choca con las paredes).
//   - En zonas Lateral se queda sobre el plano del juego, igual que el submarino.
//   - En zonas de Profundidad persigue libremente en 3D.
// Además el modelo mira siempre al jugador y flota un poco, para que los modelos
// estáticos de Meshy se sientan vivos.
// Con "distanciaPreferida" mayor que 0 no se pega al jugador: se mantiene a esa distancia
// (lo usa el enemigo a distancia, EnemigoDistancia3D).
[RequireComponent(typeof(Rigidbody))]
public class EnemigoMovimiento3D : MonoBehaviour
{
    [Header("Persecución")]
    public float velocidad = 2f;
    [Tooltip("Qué tan rápido cambia de velocidad (más alto = gira más brusco)")]
    public float aceleracion = 4f;
    [Tooltip("A qué distancia descubre al submarino y empieza a seguirlo")]
    public float rangoDeteccion = 18f;
    [Tooltip("En los túneles (cámara detrás) se ve mucho más lejos: el enemigo a distancia te descubre desde aquí")]
    public float rangoDeteccionTunel = 26f;
    [Tooltip("Si el submarino se aleja más que rangoDeteccion × este valor, deja de seguirlo")]
    public float multiplicadorOlvido = 2f;

    [Header("Distancia a mantener (0 = se pega al jugador)")]
    [Tooltip("El enemigo a distancia usa esto: se acerca si está lejos y se aleja si está cerca")]
    public float distanciaPreferida = 0f;
    [Tooltip("Margen alrededor de la distancia preferida donde se queda quieto")]
    public float margenDistancia = 0.5f;

    [Header("Área limitada (0 = sin límite)")]
    [Tooltip("No se aleja más que esto de donde empezó (el enemigo a distancia casi no se mueve)")]
    public float radioMaximoDesdeInicio = 0f;

    [Header("Plano lateral")]
    [Tooltip("Qué tan fuerte vuelve al plano en las zonas Lateral")]
    public float fuerzaVolverAlPlano = 6f;

    [Header("Modelo (si se deja vacío, busca el hijo llamado \"Modelo\")")]
    public Transform modelo;
    [Tooltip("Qué tan rápido gira para mirar al jugador")]
    public float velocidadGiro = 5f;

    [Header("Flotar")]
    public float alturaFlotar = 0.25f;
    public float velocidadFlotar = 1.6f;
    [Tooltip("Cuánto se balancea de lado a lado (grados)")]
    public float balanceo = 6f;

    // Los ataques lo bajan a 0 mientras cargan (así sigue flotando y mirando al jugador)
    [HideInInspector] public float multiplicadorVelocidad = 1f;

    private Rigidbody rb;
    private Transform objetivo;
    private bool persiguiendo = false;
    private Vector3 posicionModeloBase;
    private float desfase;
    private ZonaCamara zona;
    private float proximaRevisionZona = 0f;
    private float proximaBusquedaJugador = 0f;

    public Transform Objetivo => objetivo;
    public bool Persiguiendo => persiguiendo;
    public ZonaCamara Zona => zona;
    public bool EnPlanoLateral => EnemigoPlano.EsLateral(zona);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        if (modelo == null) modelo = EnemigoAnimaciones3D.BuscarModelo(transform);
        posicionModeloBase = modelo.localPosition;
        desfase = Random.value * 10f; // para que no floten todos al mismo ritmo
    }

    private Vector3 posicionInicio;

    void Start()
    {
        posicionInicio = transform.position;
        BuscarJugador();
        RevisarZona();
    }

    void Update()
    {
        BuscarJugador();
        RevisarZona();
        if (objetivo == null) return;

        // Detecta al jugador cuando se acerca y lo "olvida" solo si se va muy lejos
        // En los túneles el jugador ve lejos: el enemigo a distancia también (si no, se quedaba mirando sin disparar)
        float rango = distanciaPreferida > 0f && zona != null && zona.modo == ModoZona.Profundidad ? Mathf.Max(rangoDeteccion, rangoDeteccionTunel) : rangoDeteccion;
        float distancia = Vector3.Distance(transform.position, objetivo.position);
        if (distancia <= rango) persiguiendo = true;
        else if (distancia > rango * multiplicadorOlvido) persiguiendo = false;

        MirarAlJugador();
        Flotar();
    }

    void FixedUpdate()
    {
        Vector3 velocidadDeseada = Vector3.zero;

        if (objetivo != null && persiguiendo)
        {
            Vector3 haciaJugador = objetivo.position - rb.position;
            if (EnPlanoLateral) haciaJugador = EnemigoPlano.Aplanar(haciaJugador, zona);
            float distancia = haciaJugador.magnitude;
            Vector3 dir = distancia > 0.001f ? haciaJugador / distancia : Vector3.zero;
            float vel = velocidad * multiplicadorVelocidad;

            if (distanciaPreferida <= 0f)
            {
                // Cuerpo a cuerpo: se acerca hasta tocarlo
                if (distancia > 0.05f) velocidadDeseada = dir * vel;
            }
            else
            {
                // A distancia: si está muy lejos se acerca, si está muy cerca se aleja
                if (distancia > distanciaPreferida + margenDistancia) velocidadDeseada = dir * vel;
                else if (distancia < distanciaPreferida - margenDistancia) velocidadDeseada = -dir * vel;
            }
        }

        // Si tiene un área limitada y se sale, vuelve hacia su punto de inicio
        if (radioMaximoDesdeInicio > 0f)
        {
            Vector3 desdeInicio = rb.position - posicionInicio;
            if (desdeInicio.magnitude > radioMaximoDesdeInicio && Vector3.Dot(velocidadDeseada, desdeInicio) > 0f)
                velocidadDeseada = -desdeInicio.normalized * velocidad * 0.5f;
        }

        // Cambio de velocidad suave (en el agua nada frena en seco)
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, velocidadDeseada, aceleracion * Time.fixedDeltaTime);

        if (EnPlanoLateral) MantenerEnElPlano();
    }

    // Igual que el submarino: se quita la parte de la velocidad que lo saca del plano
    // y se le empuja suavemente de vuelta
    void MantenerEnElPlano()
    {
        Vector3 normal = EnemigoPlano.Normal(zona);
        float distanciaAlPlano = EnemigoPlano.DistanciaAlPlano(rb.position, zona);

        Vector3 v = rb.linearVelocity;
        v -= normal * Vector3.Dot(v, normal);
        v -= normal * distanciaAlPlano * fuerzaVolverAlPlano;
        rb.linearVelocity = v;
    }

    // Si lo devuelven a la sala (vigilante de las oleadas), su zona de movimiento pasa a ser ahí
    public void Reubicar(Vector3 p) { posicionInicio = p; }

    void MirarAlJugador()
    {
        if (!persiguiendo) return;
        Vector3 dir = objetivo.position - transform.position;
        // Siempre DERECHO: solo gira para mirar a la izquierda o a la derecha (de costado) o hacia el
        // submarino sin inclinarse (en los túneles). El disparo sí sale apuntado hacia el submarino.
        if (EnPlanoLateral && zona != null) dir = zona.Derecha * (Vector3.Dot(dir, zona.Derecha) >= 0f ? 1f : -1f);
        else dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        // El balanceo es un pequeño giro de lado a lado, como un pez flotando
        Quaternion mirar = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion balanceoActual = Quaternion.Euler(0f, 0f, Mathf.Sin((Time.time + desfase) * velocidadFlotar * 0.7f) * balanceo);
        Quaternion destino = mirar * balanceoActual;

        // Si no hay un hijo con el modelo, se gira el propio enemigo
        if (modelo != transform) modelo.rotation = Quaternion.Slerp(modelo.rotation, destino, velocidadGiro * Time.deltaTime);
        else rb.MoveRotation(Quaternion.Slerp(rb.rotation, destino, velocidadGiro * Time.deltaTime));
    }

    // Sube y baja un poco el modelo (no el Rigidbody, para no pelear con la física)
    void Flotar()
    {
        if (modelo == transform) return;
        float y = Mathf.Sin((Time.time + desfase) * velocidadFlotar) * alturaFlotar;
        modelo.localPosition = posicionModeloBase + Vector3.up * y;
    }

    void BuscarJugador()
    {
        if (objetivo != null || Time.time < proximaBusquedaJugador) return;
        proximaBusquedaJugador = Time.time + 0.5f; // buscar por nombre de tag es caro
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null) objetivo = jugador.transform;
    }

    // La zona en la que está el enemigo no cambia a cada frame: la revisamos de vez en cuando
    void RevisarZona()
    {
        if (Time.time < proximaRevisionZona) return;
        proximaRevisionZona = Time.time + 0.5f;
        zona = EnemigoPlano.ZonaEn(transform.position);
    }

    // Si otro script lo apaga, se queda quieto
    void OnDisable()
    {
        if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
    }
}
