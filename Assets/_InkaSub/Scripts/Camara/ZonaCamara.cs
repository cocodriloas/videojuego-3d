using UnityEngine;

public enum ModoZona
{
    Lateral,      // Estilo Crash bajo el agua: cámara de costado, el submarino se mueve en un plano
    Profundidad   // La cámara va detrás y el submarino avanza hacia el fondo de la pantalla
}

// Una caja invisible (Box Collider con "Is Trigger") que dice cómo se juega dentro de ella.
// Cuando el submarino entra, esta zona pasa a ser la "zona actual" y la cámara y los
// controles se adaptan solos.
//
// Cómo orientarla (mira las flechas de la zona en la Scene):
//   - Flecha ROJA (derecha): hacia dónde avanza el nivel.
//   - Flecha AZUL (adelante): en modo Lateral, hacia dónde mira la cámara (el plano del juego
//     pasa por el centro de la zona). En modo Profundidad, hacia dónde se avanza (el "fondo").
[RequireComponent(typeof(BoxCollider))]
public class ZonaCamara : MonoBehaviour
{
    // Zona en la que está el submarino ahora mismo (null = usar la configuración por defecto)
    public static ZonaCamara Actual { get; private set; }

    [Header("Tipo de zona")]
    public ModoZona modo = ModoZona.Lateral;

    [Header("Cámara")]
    [Tooltip("Qué tan lejos queda la cámara del submarino")]
    public float distancia = 12f;
    [Tooltip("Cuánto más arriba que el submarino va la cámara")]
    public float altura = 2f;
    [Tooltip("Cuánto se adelanta la cámara hacia donde avanza el nivel")]
    public float adelanto = 2f;

    [Tooltip("Opcional: si lo asignas, la cámara se queda FIJA mirando este punto (como en la sala del jefe)")]
    public Transform centroFijo;
    [Tooltip("Segundos del paso de la cámara al ENTRAR a esta zona (0 = lo normal, 0.7 s). Más largo si la cámara viaja lejos")]
    public float duracionTransicion = 0f;
    [Tooltip("Entrar o salir de esta zona es un CORTE con un fundido rápido (la cámara no viaja). Para cuando el viaje atravesaría rocas")]
    public bool corteDirecto = false;
    [Tooltip("Niebla propia de esta zona en metros (0 = la del nivel). En el túnel se ve más lejos")]
    public float nieblaInicio = 0f;
    public float nieblaFin = 0f;
    [Tooltip("Ambiente propio (por ejemplo, la sala del jefe: agua más oscura y opresiva)")]
    public bool ambientePropio = false;
    public UnityEngine.Color colorNiebla = new UnityEngine.Color(0.03f, 0.02f, 0.06f);
    public UnityEngine.Color colorAmbiente = new UnityEngine.Color(0.12f, 0.08f, 0.14f);
    [Tooltip("Cuánto de la luz del sol llega (1 = la normal del nivel)")]
    public float intensidadSol = 0.35f;

    [Header("Prioridad (si dos zonas se tocan, gana la más alta)")]
    public int prioridad = 0;

    // ---------- Datos que usan el movimiento, el apuntado y la cámara ----------
    public Vector3 Derecha => transform.right;
    public Vector3 Fondo => transform.forward;
    public Vector3 PuntoPlano => transform.position;

    // Lo mismo, pero para la zona actual. Si todavía no hay zona, se juega de costado
    // sobre el plano Z = 0, mirando hacia +Z (como un juego 2D normal).
    public static ModoZona ModoActual => Actual != null ? Actual.modo : ModoZona.Lateral;
    public static Vector3 DerechaActual => Actual != null ? Actual.Derecha : Vector3.right;
    public static Vector3 FondoActual => Actual != null ? Actual.Fondo : Vector3.forward;
    public static Vector3 PuntoPlanoActual => Actual != null ? Actual.PuntoPlano : Vector3.zero;

    void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    [Tooltip("Mensaje que aparece la primera vez que entras (por ejemplo, cómo se avanza)")]
    public string mensajeAlEntrar = "";
    private bool mensajeMostrado = false;

    // Margen para ENTRAR a una zona de más prioridad: hay que estar este tanto adentro.
    // Para SALIR basta con cruzar el borde. Esa diferencia evita que la cámara salte
    // de un modo a otro cuando el submarino se queda justo en el borde.
    const float MargenEntrada = 1f;

    static readonly System.Collections.Generic.List<ZonaCamara> todas = new System.Collections.Generic.List<ZonaCamara>();

    void OnEnable() { todas.Add(this); }

    void OnDisable()
    {
        todas.Remove(this);
        if (Actual == this) Actual = null;
    }

    // La llama el submarino en cada paso de física con su posición
    public static void Actualizar(Vector3 pos)
    {
        ZonaCamara anterior = Actual;

        // ¿Hay una zona de MÁS prioridad en la que ya estemos bien adentro?
        foreach (ZonaCamara z in todas)
            if (z != Actual && (Actual == null || z.prioridad > Actual.prioridad) && z.Contiene(pos, MargenEntrada))
                Actual = z;

        // Si salimos de la zona actual, pasamos a la mejor zona que nos contenga
        if (Actual == null || !Actual.Contiene(pos, 0f))
        {
            ZonaCamara mejor = null;
            foreach (ZonaCamara z in todas)
                if (z.Contiene(pos, 0f) && (mejor == null || z.prioridad > mejor.prioridad)) mejor = z;
            // Si no hay ninguna, nos quedamos con la de antes para que el control no cambie de golpe
            if (mejor != null) Actual = mejor;
        }

        if (Actual != anterior && Actual != null && !Actual.mensajeMostrado && !string.IsNullOrEmpty(Actual.mensajeAlEntrar))
        {
            Actual.mensajeMostrado = true;
            MensajePantalla.Mostrar(Actual.mensajeAlEntrar);
        }
    }

    // ¿El punto está dentro de la caja, al menos "margen" metros hacia adentro?
    public bool Contiene(Vector3 punto, float margen = 0f)
    {
        BoxCollider caja = GetComponent<BoxCollider>();
        Vector3 local = transform.InverseTransformPoint(punto) - caja.center;
        Vector3 medio = caja.size * 0.5f - Vector3.one * margen;
        return Mathf.Abs(local.x) <= medio.x && Mathf.Abs(local.y) <= medio.y && Mathf.Abs(local.z) <= medio.z;
    }

    // Dibuja la zona y el plano de juego en la Scene (solo en el editor)
    void OnDrawGizmos()
    {
        BoxCollider caja = GetComponent<BoxCollider>();
        if (caja == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = modo == ModoZona.Lateral ? new Color(0.2f, 0.8f, 1f, 0.08f) : new Color(1f, 0.6f, 0.2f, 0.08f);
        Gizmos.DrawCube(caja.center, caja.size);
        Gizmos.color = modo == ModoZona.Lateral ? new Color(0.2f, 0.8f, 1f, 0.6f) : new Color(1f, 0.6f, 0.2f, 0.6f);
        Gizmos.DrawWireCube(caja.center, caja.size);

        if (modo == ModoZona.Lateral)
        {
            // El plano por donde se mueve el submarino
            Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.5f);
            Gizmos.DrawWireCube(new Vector3(caja.center.x, caja.center.y, 0f), new Vector3(caja.size.x, caja.size.y, 0.01f));
        }

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * 3f);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);
    }
}
