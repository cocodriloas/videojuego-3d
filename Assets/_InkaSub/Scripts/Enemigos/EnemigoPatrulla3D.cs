using UnityEngine;

// Enemigo cuerpo a cuerpo estilo Crash: NO persigue al submarino.
// Patrulla de izquierda a derecha dentro de su área y, si el submarino se acerca por delante,
// hace una EMBESTIDA: avisa (tiembla y se echa hacia atrás), se lanza hacia adelante y vuelve.
// Si lo tocas, pierdes casco (eso lo maneja SubmarinoSalud3D al chocar con el tag "Enemigo").
[RequireComponent(typeof(Rigidbody))]
public class EnemigoPatrulla3D : MonoBehaviour
{
    [Header("Patrulla")]
    [Tooltip("Cuánto se aleja hacia cada lado de su punto de inicio")]
    public float distanciaPatrulla = 3f;
    public float velocidad = 2.2f;
    [Tooltip("Se queda quieto un momento en cada extremo antes de dar la vuelta")]
    public float pausaEnExtremos = 0.6f;

    [Header("Embestida")]
    [Tooltip("Si el submarino está más cerca que esto, embiste")]
    public float rangoEmbestida = 5f;
    [Tooltip("Solo embiste si el submarino está a esta diferencia de altura o menos (la embestida es horizontal)")]
    public float alturaAtaque = 1.8f;
    [Tooltip("Tiempo de aviso: tiembla y se echa hacia atrás (para que se pueda esquivar)")]
    public float tiempoAviso = 0.6f;
    public float distanciaEmbestida = 4f;
    public float velocidadEmbestida = 12f;
    public float tiempoEntreEmbestidas = 2.5f;

    [Header("Modelo (si se deja vacío, busca el hijo \"Modelo\")")]
    public Transform modelo;
    public float velocidadGiro = 6f;
    public float alturaFlotar = 0.25f;
    public float velocidadFlotar = 1.6f;

    enum Estado { Patrullando, Pausa, Aviso, Embistiendo, Volviendo }
    private Estado estado = Estado.Patrullando;

    private Rigidbody rb;
    private Transform jugador;
    private Vector3 inicio, eje, destinoEmbestida, mirarHacia;
    private Vector3 posicionModeloBase, escalaModeloBase = Vector3.one;
    private TrailRenderer garrazo;
    private Transform pivoteGarrazo;
    private int sentido = 1;
    private float tiempoEstado, proximaEmbestida, desfase;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true; // se mueve por código: el submarino no lo empuja
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        if (modelo == null) modelo = EnemigoAnimaciones3D.BuscarModelo(transform);
        posicionModeloBase = modelo != transform ? modelo.localPosition : Vector3.zero;
        escalaModeloBase = modelo.localScale;
        garrazo = CrearGarrazo();
        desfase = Random.value * 10f;
    }

    void Start()
    {
        inicio = transform.position;
        // Patrulla a lo largo de la zona en la que está (de costado: izquierda-derecha de la pantalla)
        ZonaCamara zona = EnemigoPlano.ZonaEn(inicio);
        eje = zona != null ? zona.Derecha : Vector3.right;
        mirarHacia = eje;
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;
    }

    void FixedUpdate()
    {
        tiempoEstado += Time.fixedDeltaTime;
        Vector3 pos = rb.position;

        switch (estado)
        {
            case Estado.Patrullando:
            {
                Vector3 destino = inicio + eje * distanciaPatrulla * sentido;
                // Si hay una pared adelante, da la vuelta (no la atraviesa)
                if (!Libre(eje * sentido, 0.4f)) { Cambiar(Estado.Pausa); break; }
                MoverHacia(destino, velocidad);
                mirarHacia = eje * sentido;
                if (Vector3.Distance(pos, destino) < 0.05f) Cambiar(Estado.Pausa);
                if (JugadorCerca()) EmpezarAviso();
                break;
            }
            case Estado.Pausa:
                if (tiempoEstado >= pausaEnExtremos) { sentido = -sentido; Cambiar(Estado.Patrullando); }
                if (JugadorCerca()) EmpezarAviso();
                break;

            case Estado.Aviso:
                // Embiste SOLO en horizontal, por su línea de patrulla (en diagonal se veía raro)
                if (jugador != null) mirarHacia = eje * (Vector3.Dot(jugador.position - pos, eje) >= 0f ? 1f : -1f);
                if (tiempoEstado >= tiempoAviso)
                {
                    // La embestida se corta antes de la pared
                    destinoEmbestida = pos + mirarHacia.normalized * DistanciaLibre(mirarHacia.normalized, distanciaEmbestida);
                    AudioManager.Sonar(Sonido.Garra);
                    Cambiar(Estado.Embistiendo);
                }
                break;

            case Estado.Embistiendo:
                MoverHacia(destinoEmbestida, velocidadEmbestida);
                if (Vector3.Distance(rb.position, destinoEmbestida) < 0.05f || tiempoEstado > 1f) Cambiar(Estado.Volviendo);
                break;

            case Estado.Volviendo:
            {
                // Vuelve al punto más cercano de su línea de patrulla
                float t = Mathf.Clamp(Vector3.Dot(pos - inicio, eje), -distanciaPatrulla, distanciaPatrulla);
                Vector3 vuelta = inicio + eje * t;
                Vector3 haciaVuelta = vuelta - pos;
                // Si una pared le impide volver a su línea, sigue patrullando desde donde está
                if (haciaVuelta.sqrMagnitude > 0.01f && !Libre(haciaVuelta.normalized, Mathf.Min(0.4f, haciaVuelta.magnitude)))
                {
                    inicio = pos;
                    Cambiar(Estado.Patrullando);
                    break;
                }
                MoverHacia(vuelta, velocidad * 1.5f);
                mirarHacia = Plano(vuelta - pos);
                if (Vector3.Distance(pos, vuelta) < 0.05f) Cambiar(Estado.Patrullando);
                break;
            }
        }
    }

    void Update()
    {
        if (modelo == transform) return;

        // Siempre DERECHO, mirando solo a la izquierda o a la derecha (como en el juego 2D):
        // así nunca se ve acostado aunque embista hacia arriba o abajo
        float lado = Vector3.Dot(mirarHacia, eje) >= 0f ? 1f : -1f;
        Quaternion destino = Quaternion.LookRotation(eje * lado, Vector3.up);
        modelo.rotation = Quaternion.Slerp(modelo.rotation, destino, velocidadGiro * Time.deltaTime);

        // Aviso: se encoge y tiembla (toma impulso). Embestida: se estira hacia adelante.
        Vector3 escala = Vector3.one;
        Vector3 offset = Vector3.up * Mathf.Sin((Time.time + desfase) * velocidadFlotar) * alturaFlotar;
        if (estado == Estado.Aviso)
        {
            float k = Mathf.Clamp01(tiempoEstado / 0.25f);
            escala = new Vector3(1f + 0.12f * k, 1f - 0.15f * k, 1f - 0.15f * k);
            offset += Random.insideUnitSphere * 0.06f;
        }
        else if (estado == Estado.Embistiendo)
        {
            escala = new Vector3(0.9f, 0.92f, 1.25f);
        }
        modelo.localScale = Vector3.Lerp(modelo.localScale, Vector3.Scale(escalaModeloBase, escala), 20f * Time.deltaTime);
        modelo.localPosition = posicionModeloBase + offset;

        if (garrazo != null) garrazo.emitting = estado == Estado.Embistiendo;
    }

    // Estela curva delante del enemigo durante la embestida: se lee como un "garrazo"
    TrailRenderer CrearGarrazo()
    {
        Transform pivote = new GameObject("Garrazo").transform;
        pivote.SetParent(transform, false);
        GameObject punta = new GameObject("Punta");
        punta.transform.SetParent(pivote, false);
        punta.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        TrailRenderer t = punta.AddComponent<TrailRenderer>();
        t.time = 0.25f;
        t.startWidth = 0.35f;
        t.endWidth = 0f;
        t.startColor = new Color(0.7f, 1f, 0.8f, 0.9f);
        t.endColor = new Color(0.2f, 0.6f, 0.4f, 0f);
        t.emitting = false;
        t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s != null) { Material m = new Material(s); m.SetFloat("_Surface", 1f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 3000; t.material = m; }
        pivoteGarrazo = pivote;
        return t;
    }

    void LateUpdate()
    {
        // El garrazo da una vuelta rápida delante del enemigo mientras embiste
        if (pivoteGarrazo == null) return;
        if (estado == Estado.Embistiendo)
        {
            pivoteGarrazo.position = rb.position + mirarHacia.normalized * 1.2f;
            pivoteGarrazo.rotation = Quaternion.LookRotation(mirarHacia.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, -tiempoEstado * 900f);
        }
    }

    // Si lo devuelven a la sala (vigilante de las oleadas), patrulla desde ahí
    public void Reubicar(Vector3 p) { inicio = p; Cambiar(Estado.Patrullando); }

    bool JugadorCerca()
    {
        if (jugador == null || Time.time < proximaEmbestida) return false;
        // Como embiste en horizontal, solo ataca si el submarino está más o menos a su altura
        if (Mathf.Abs(jugador.position.y - rb.position.y) > alturaAtaque) return false;
        return Vector3.Distance(jugador.position, rb.position) < rangoEmbestida;
    }

    void EmpezarAviso()
    {
        proximaEmbestida = Time.time + tiempoEntreEmbestidas;
        AudioManager.Sonar(Sonido.AvisoEnemigo);
        Cambiar(Estado.Aviso);
    }

    // ¿Puede avanzar "distancia" metros en esa dirección sin chocar con una pared?
    bool Libre(Vector3 direccion, float distancia)
    {
        return DistanciaLibre(direccion, distancia) >= distancia - 0.01f;
    }

    // Cuánto puede avanzar en esa dirección antes de tocar una pared (como mucho "maximo").
    // Ignora al propio enemigo, al submarino, a otros enemigos y a las zonas invisibles.
    float DistanciaLibre(Vector3 direccion, float maximo)
    {
        const float radio = 0.9f;
        float libre = maximo;
        foreach (RaycastHit h in Physics.SphereCastAll(rb.position, radio, direccion, maximo, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform) || h.collider.CompareTag("Player") || h.collider.CompareTag("Enemigo")) continue;
            if (h.distance <= 0f) continue; // ya estaba tocando algo al empezar: no lo frena
            libre = Mathf.Min(libre, h.distance);
        }
        return libre;
    }

    void MoverHacia(Vector3 destino, float vel)
    {
        rb.MovePosition(Vector3.MoveTowards(rb.position, destino, vel * Time.fixedDeltaTime));
    }

    // En zonas de costado, que no se salga del plano del juego
    Vector3 Plano(Vector3 v)
    {
        ZonaCamara zona = EnemigoPlano.ZonaEn(rb.position);
        return EnemigoPlano.EsLateral(zona) ? EnemigoPlano.Aplanar(v, zona) : v;
    }

    void Cambiar(Estado nuevo)
    {
        estado = nuevo;
        tiempoEstado = 0f;
    }

    // Dibuja su área de patrulla en la Scene
    void OnDrawGizmosSelected()
    {
        Vector3 o = Application.isPlaying ? inicio : transform.position;
        Vector3 e = Application.isPlaying ? eje : Vector3.right;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(o - e * distanciaPatrulla, o + e * distanciaPatrulla);
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.5f);
        Gizmos.DrawWireSphere(o, rangoEmbestida);
    }
}
