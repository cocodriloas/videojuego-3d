using UnityEngine;

// Ataque a distancia (Conglomerado). Se mantiene lejos del submarino y cada cierto tiempo
// dispara 3 proyectiles: uno directo al jugador y dos abiertos en diagonal.
// Antes de disparar avisa (sonido + se pone amarillo) y se mueve más lento.
// En las zonas Lateral los disparos se quedan sobre el plano del juego.
// El movimiento lo hace EnemigoMovimiento3D; este script le dice a qué distancia quedarse.
[RequireComponent(typeof(EnemigoMovimiento3D))]
public class EnemigoDistancia3D : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject prefabProyectilEnemigo;

    [Header("Comportamiento")]
    [Tooltip("Qué tan lejos quiere mantenerse del submarino (se copia a EnemigoMovimiento3D)")]
    public float distanciaPreferida = 8f;
    [Tooltip("Velocidad de movimiento (se copia a EnemigoMovimiento3D)")]
    public float velocidad = 1.5f;
    public float tiempoEntreDisparos = 2f;
    [Tooltip("Qué tan abiertos van los disparos laterales (grados)")]
    public float anguloDiagonal = 25f;
    [Tooltip("Qué tan lejos del centro del enemigo nacen los proyectiles (para que no salgan desde dentro del modelo)")]
    public float distanciaSalida = 1.6f;

    [Header("Aviso antes de disparar (brillo amarillo)")]
    [Tooltip("Cuántos segundos brilla antes de disparar. Da tiempo al jugador para esquivar")]
    public float tiempoAviso = 0.7f;
    [Tooltip("Mientras avisa, se mueve más lento (0 = se queda quieto, 1 = velocidad normal)")]
    [Range(0f, 1f)] public float lentitudDuranteAviso = 0.3f;

    private EnemigoMovimiento3D movimiento;
    private EnemigoAnimaciones3D anim;
    private float tiempoRestanteParaDisparar;
    private bool avisando = false;

    void Start()
    {
        movimiento = GetComponent<EnemigoMovimiento3D>();
        anim = GetComponent<EnemigoAnimaciones3D>();
        movimiento.distanciaPreferida = distanciaPreferida;
        movimiento.velocidad = velocidad;
        tiempoRestanteParaDisparar = tiempoEntreDisparos;
    }

    void Update()
    {
        // Solo dispara si ya descubrió al submarino
        if (movimiento.Objetivo == null || !movimiento.Persiguiendo) return;

        tiempoRestanteParaDisparar -= Time.deltaTime;

        // Un momento antes de disparar: empieza a brillar (aviso) y va más lento
        if (!avisando && tiempoRestanteParaDisparar <= tiempoAviso)
        {
            avisando = true;
            movimiento.multiplicadorVelocidad = lentitudDuranteAviso;
            if (anim != null) anim.IniciarAviso(Mathf.Max(0.1f, tiempoRestanteParaDisparar));
            AudioManager.Sonar(Sonido.AvisoEnemigo);
        }

        if (tiempoRestanteParaDisparar <= 0)
        {
            if (anim != null) anim.Disparar();
            Disparar();
            AudioManager.Sonar(Sonido.DisparoEnemigo);
            avisando = false;
            movimiento.multiplicadorVelocidad = 1f;
            tiempoRestanteParaDisparar = Mathf.Max(tiempoEntreDisparos, tiempoAviso + 0.2f);
        }
    }

    void OnDisable()
    {
        avisando = false;
        if (movimiento != null) movimiento.multiplicadorVelocidad = 1f;
    }

    // --- SISTEMA DE DISPARO TRIPLE ---

    void Disparar()
    {
        Transform objetivo = movimiento.Objetivo;
        if (prefabProyectilEnemigo == null || objetivo == null) return;

        bool lateral = movimiento.EnPlanoLateral;
        Vector3 direccionCentral = objetivo.position - transform.position;
        if (lateral) direccionCentral = EnemigoPlano.Aplanar(direccionCentral, movimiento.Zona);
        if (direccionCentral.sqrMagnitude < 0.0001f) return;
        direccionCentral.Normalize();

        // Eje para abrir los disparos diagonales:
        //  - Lateral: la normal del plano, así los 3 disparos quedan sobre el plano.
        //  - Profundidad: el eje vertical, así se abren en abanico horizontal.
        Vector3 eje = lateral ? EnemigoPlano.Normal(movimiento.Zona) : Vector3.up;
        if (Vector3.Cross(direccionCentral, eje).sqrMagnitude < 0.01f) eje = Vector3.right; // disparo casi vertical

        DispararEnDireccion(direccionCentral);
        DispararEnDireccion(Quaternion.AngleAxis(anguloDiagonal, eje) * direccionCentral);
        DispararEnDireccion(Quaternion.AngleAxis(-anguloDiagonal, eje) * direccionCentral);
    }

    void DispararEnDireccion(Vector3 direccion)
    {
        // El proyectil avanza hacia su eje Z, así que lo creamos mirando hacia la dirección del disparo
        Vector3 salida = transform.position + direccion * distanciaSalida;
        Instantiate(prefabProyectilEnemigo, salida, Quaternion.LookRotation(direccion));
    }
}
