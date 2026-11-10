using UnityEngine;
using UnityEngine.InputSystem;

// Apunta con el mouse y gira el MODELO del submarino (no el objeto raíz, que tiene el Rigidbody).
//   - Lateral: el submarino mira a la izquierda o a la derecha según el mouse y solo se
//     inclina un poco hacia arriba o abajo (como en el juego 2D).
//   - Profundidad: gira hacia donde apunta el mouse, también con poca inclinación.
// Los disparos siguen saliendo exactamente hacia el punto del mouse.
//
// IMPORTANTE para tu modelo 3D: ponlo como hijo de "Modelo" y gíralo para que la punta
// del submarino mire hacia la flecha AZUL (eje Z) de "Modelo".
public class SubmarinoApuntar3D : MonoBehaviour
{
    [Header("Qué se gira")]
    [Tooltip("El hijo 'Modelo' que contiene el modelo 3D y las bocas de los cañones")]
    public Transform modelo;

    [Header("Inclinación")]
    [Tooltip("Cuántos grados como máximo se inclina hacia arriba o abajo")]
    public float inclinacionMaxima = 20f;
    [Tooltip("Qué tan rápido gira (más alto = más rápido)")]
    public float suavizado = 10f;
    [Tooltip("Cuánto se ladea (alabeo) al girar en modo Profundidad")]
    public float alabeoMaximo = 15f;

    [Header("Modo Profundidad")]
    [Tooltip("Si el mouse no toca nada, se apunta a este punto de distancia")]
    public float distanciaApuntadoLibre = 30f;
    [Tooltip("Capas que el mouse puede 'tocar' para apuntar (no incluyas la del submarino)")]
    public LayerMask capasApuntables = ~0;

    // Dirección y punto reales hacia el mouse — los usa SubmarinoDisparo3D
    public Vector3 DireccionApuntada { get; private set; } = Vector3.right;
    public Vector3 PuntoApuntado { get; private set; }

    private Camera cam;
    private float anguloGiroAnterior;

    void Start()
    {
        cam = Camera.main;
        if (modelo == null && transform.childCount > 0) modelo = transform.Find("Modelo");
        PuntoApuntado = transform.position + Vector3.right * 5f;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (Mouse.current != null)
        {
            Ray rayo = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            PuntoApuntado = CalcularPuntoApuntado(rayo);
        }
        else
        {
            // Sin mouse (por ejemplo, con control o en pruebas): apunta hacia donde avanza la zona
            Vector3 adelante = ZonaCamara.ModoActual == ModoZona.Lateral ? ZonaCamara.DerechaActual : ZonaCamara.FondoActual;
            PuntoApuntado = transform.position + adelante * 10f;
        }

        Vector3 direccion = PuntoApuntado - transform.position;
        if (direccion.sqrMagnitude < 0.0001f) return;
        DireccionApuntada = direccion.normalized;

        if (modelo != null) GirarModelo();
    }

    Vector3 CalcularPuntoApuntado(Ray rayo)
    {
        if (ZonaCamara.ModoActual == ModoZona.Lateral)
        {
            // El mouse "toca" el plano por donde se mueve el submarino
            Plane plano = new Plane(ZonaCamara.FondoActual, transform.position);
            if (plano.Raycast(rayo, out float d)) return rayo.GetPoint(d);
            return transform.position + ZonaCamara.DerechaActual * 5f;
        }

        // Profundidad: apuntamos a lo que haya bajo el mouse (enemigos, rocas...).
        // Solo cuenta lo que está MÁS ALLÁ del submarino: lo que queda entre la cámara y él
        // (rocas de la entrada, paredes invisibles) haría que apunte hacia atrás.
        float profundidadSub = Vector3.Dot(transform.position - rayo.origin, rayo.direction);
        RaycastHit[] golpes = Physics.RaycastAll(rayo, 200f, capasApuntables, QueryTriggerInteraction.Ignore);
        float mejor = float.MaxValue;
        Vector3 punto = rayo.GetPoint(Mathf.Max(distanciaApuntadoLibre, profundidadSub + 10f));
        foreach (RaycastHit g in golpes)
        {
            if (g.transform.IsChildOf(transform)) continue; // no apuntarnos a nosotros mismos
            if (g.distance < profundidadSub + 1f) continue;
            if (g.distance < mejor) { mejor = g.distance; punto = g.point; }
        }
        return punto;
    }

    void GirarModelo()
    {
        Vector3 horizontal;
        if (ZonaCamara.ModoActual == ModoZona.Lateral)
        {
            // Solo izquierda o derecha a lo largo del nivel
            Vector3 derecha = ZonaCamara.DerechaActual;
            horizontal = Vector3.Dot(DireccionApuntada, derecha) < 0f ? -derecha : derecha;
        }
        else
        {
            horizontal = Vector3.ProjectOnPlane(DireccionApuntada, Vector3.up);
            if (horizontal.sqrMagnitude < 0.0001f) horizontal = ZonaCamara.FondoActual;
            // En el túnel el submarino siempre mira hacia adelante (nunca de reversa)
            Vector3 fondo = ZonaCamara.FondoActual;
            if (Vector3.Dot(horizontal.normalized, fondo) < 0.25f)
            {
                Vector3 lado = Vector3.ProjectOnPlane(horizontal, fondo);
                horizontal = lado.sqrMagnitude > 0.0001f ? Vector3.Slerp(fondo, lado.normalized, 0.6f) : fondo;
            }
        }
        horizontal.Normalize();

        // Inclinación hacia arriba/abajo, limitada
        float horiz = Vector3.Dot(DireccionApuntada, horizontal);
        float angulo = Mathf.Atan2(DireccionApuntada.y, Mathf.Max(0.0001f, Mathf.Abs(horiz))) * Mathf.Rad2Deg;
        angulo = Mathf.Clamp(angulo, -inclinacionMaxima, inclinacionMaxima);

        // Alabeo: se ladea un poco al girar, para que se sienta como un vehículo
        float giroActual = Mathf.Atan2(horizontal.x, horizontal.z) * Mathf.Rad2Deg;
        float velocidadGiro = Mathf.DeltaAngle(anguloGiroAnterior, giroActual) / Mathf.Max(Time.deltaTime, 0.0001f);
        anguloGiroAnterior = giroActual;
        float alabeo = ZonaCamara.ModoActual == ModoZona.Profundidad
            ? Mathf.Clamp(-velocidadGiro * 0.1f, -alabeoMaximo, alabeoMaximo)
            : 0f;

        Quaternion objetivo = Quaternion.LookRotation(horizontal, Vector3.up) * Quaternion.Euler(-angulo, 0f, alabeo);
        modelo.rotation = Quaternion.Slerp(modelo.rotation, objetivo, suavizado * Time.deltaTime);
    }
}
