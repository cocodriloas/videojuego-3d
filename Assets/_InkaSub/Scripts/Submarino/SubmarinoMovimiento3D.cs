using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento del submarino en 3D. W/S SIEMPRE suben y bajan; lo demás depende de la ZonaCamara:
//   - Lateral (de costado):  A/D = avanzar/retroceder.
//                            El submarino se queda pegado al plano de la zona.
//   - Profundidad (al fondo): A/D = izquierda/derecha, Espacio = avanzar hacia el fondo,
//                             Ctrl = retroceder. Si sueltas todo, se queda quieto.
[RequireComponent(typeof(Rigidbody))]
public class SubmarinoMovimiento3D : MonoBehaviour
{
    [Header("Velocidad de movimiento")]
    public float velocidad = 6f;
    [Tooltip("Qué tan rápido llega a su velocidad (más alto = más directo, más bajo = más 'agua')")]
    public float aceleracion = 18f;

    [Header("Plano de juego (modo Lateral)")]
    [Tooltip("Qué tan fuerte vuelve al plano si se sale (por ejemplo, al cambiar de zona)")]
    public float fuerzaVolverAlPlano = 6f;

    [Header("Dash (se desbloquea con 2 bacterias)")]
    public bool dashDesbloqueado = false;
    public float velocidadDash = 18f;
    public float duracionDash = 0.2f;
    public float cooldownDash = 1.5f;

    [Header("Estela del dash (si la dejas vacía se crea sola)")]
    public TrailRenderer estelaDash;
    public Color colorEstela = new Color(0.4f, 0.9f, 1f, 0.6f);

    private Rigidbody rb;
    private Vector3 direccionMovimiento;
    private Vector3 ultimaDireccion = Vector3.right;
    private bool estaHaciendoDash = false;
    private float tiempoRestanteCooldown = 0f;

    // Atrapado por una red fantasma: no puede moverse y es arrastrado hacia la red
    private bool atrapado = false;
    private Vector3 puntoAtrapado;

    // Para el indicador del dash en el HUD
    public bool DashListo => dashDesbloqueado && !estaHaciendoDash && tiempoRestanteCooldown <= 0f && !atrapado;
    public bool EstaHaciendoDash => estaHaciendoDash;
    // Durante el dash (y un instante después) el submarino es inmune al daño: sirve para atravesar ataques
    public bool InmunePorDash => estaHaciendoDash || Time.time < finDash + margenInmuneDash;
    [Tooltip("Segundos de inmunidad extra al terminar el dash")]
    public float margenInmuneDash = 0.15f;
    [Tooltip("Cuánto empuja cada clic de la rueda del mouse en los túneles (segundos de avance)")]
    public float segundosPorClicRueda = 0.35f;
    private float impulsoRueda, finDash = -10f;
    public bool Atrapado => atrapado;
    // 1 = listo, 0 = recién usado (sube mientras se recarga)
    public float ProgresoDash => estaHaciendoDash ? 0f : 1f - Mathf.Clamp01(tiempoRestanteCooldown / Mathf.Max(0.01f, cooldownDash));

    public void Atrapar(Vector3 punto)
    {
        atrapado = true;
        puntoAtrapado = punto;
    }

    public void Liberar()
    {
        atrapado = false;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;          // bajo el agua no cae
        rb.freezeRotation = true;       // el giro lo hace el modelo (SubmarinoApuntar3D)
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (estelaDash == null) estelaDash = CrearEstela();
        estelaDash.emitting = false;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // juego en pausa

        Keyboard teclado = Keyboard.current;   // (puede no haber teclado: la rueda del mouse sigue sirviendo)

        float x = teclado == null ? 0f : (teclado.dKey.isPressed ? 1f : 0f) - (teclado.aKey.isPressed ? 1f : 0f);
        float y = teclado == null ? 0f : (teclado.wKey.isPressed ? 1f : 0f) - (teclado.sKey.isPressed ? 1f : 0f);

        // A/D a lo largo de la zona y W/S arriba/abajo, en los dos modos
        Vector3 dir = ZonaCamara.DerechaActual * x + Vector3.up * y;

        // En Profundidad, además: la RUEDA del mouse hacia adelante avanza al fondo y hacia atrás retrocede
        // (cada "clic" de la rueda empuja un momento; seguir girando sigue avanzando).
        // Espacio / Ctrl siguen funcionando para quien no tenga rueda.
        Mouse rueda = Mouse.current;
        float giro = rueda != null ? rueda.scroll.ReadValue().y : 0f;
        if (giro > 0.01f) impulsoRueda = Mathf.Min(impulsoRueda + segundosPorClicRueda, segundosPorClicRueda * 3f);
        else if (giro < -0.01f) impulsoRueda = Mathf.Max(impulsoRueda - segundosPorClicRueda, -segundosPorClicRueda * 3f);
        impulsoRueda = Mathf.MoveTowards(impulsoRueda, 0f, Time.deltaTime);
        if (ZonaCamara.ModoActual == ModoZona.Profundidad)
        {
            float z = teclado == null ? 0f : (teclado.spaceKey.isPressed ? 1f : 0f) - (teclado.leftCtrlKey.isPressed || teclado.rightCtrlKey.isPressed ? 1f : 0f);
            if (Mathf.Abs(impulsoRueda) > 0.001f) z = Mathf.Sign(impulsoRueda);
            dir += ZonaCamara.FondoActual * z;
        }
        else impulsoRueda = 0f;

        direccionMovimiento = Vector3.ClampMagnitude(dir, 1f);

        // El dash va hacia donde se está moviendo (incluido el avance automático)
        if (direccionMovimiento.sqrMagnitude > 0.01f)
            ultimaDireccion = direccionMovimiento.normalized;

        if (tiempoRestanteCooldown > 0)
            tiempoRestanteCooldown -= Time.deltaTime;

        // Dash con clic derecho del mouse o la tecla 1
        Mouse mouse = Mouse.current;
        Keyboard tecla = Keyboard.current;
        if ((mouse != null && mouse.rightButton.wasPressedThisFrame) || (tecla != null && tecla.digit1Key.wasPressedThisFrame))
        {
            if (DashListo) StartCoroutine(HacerDash());
            else if (!dashDesbloqueado) Debug.Log("Dash bloqueado: se desbloquea con 2 bacterias (o marca 'Dash Desbloqueado' en el Submarino).");
        }
    }

    void FixedUpdate()
    {
        ZonaCamara.Actualizar(rb.position);

        if (atrapado)
        {
            // La red lo sujeta: se queda casi quieto, tirando hacia el centro de la red
            Vector3 haciaRed = (puntoAtrapado - rb.position) * 3f;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, haciaRed + direccionMovimiento * 0.6f, 10f * Time.fixedDeltaTime);
        }
        else if (!estaHaciendoDash)
        {
            Vector3 objetivo = direccionMovimiento * velocidad;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, objetivo, aceleracion * Time.fixedDeltaTime);
        }

        if (ZonaCamara.ModoActual == ModoZona.Lateral)
            MantenerEnElPlano();
    }

    // En modo Lateral el submarino no puede alejarse ni acercarse a la cámara:
    // se quita esa parte de la velocidad y se le empuja suavemente de vuelta al plano.
    void MantenerEnElPlano()
    {
        Vector3 normal = ZonaCamara.FondoActual;
        float distanciaAlPlano = Vector3.Dot(rb.position - ZonaCamara.PuntoPlanoActual, normal);

        Vector3 v = rb.linearVelocity;
        v -= normal * Vector3.Dot(v, normal);
        v -= normal * distanciaAlPlano * fuerzaVolverAlPlano;
        rb.linearVelocity = v;
    }

    private IEnumerator HacerDash()
    {
        estaHaciendoDash = true;
        rb.linearVelocity = ultimaDireccion * velocidadDash;
        estelaDash.emitting = true;
        AudioManager.Sonar(Sonido.Dash);

        yield return new WaitForSeconds(duracionDash);

        estaHaciendoDash = false;
        finDash = Time.time;
        tiempoRestanteCooldown = cooldownDash;
        estelaDash.emitting = false;
    }

    TrailRenderer CrearEstela()
    {
        GameObject go = new GameObject("EstelaDash");
        go.transform.SetParent(transform, false);

        TrailRenderer t = go.AddComponent<TrailRenderer>();
        t.time = 0.3f;
        t.startWidth = 0.6f;
        t.endWidth = 0f;
        t.minVertexDistance = 0.05f;
        t.startColor = colorEstela;
        t.endColor = new Color(colorEstela.r, colorEstela.g, colorEstela.b, 0f);
        t.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader != null)
        {
            Material m = new Material(shader);
            m.SetFloat("_Surface", 1f); // transparente
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            t.material = m;
        }
        return t;
    }
}
