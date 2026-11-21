using System.Collections;
using UnityEngine;

// Ataque cuerpo a cuerpo (Fantasma de Red). Cuando el submarino está cerca:
//   1) AVISO: se detiene, suena el aviso y se pone amarillo y más grande (tiempoDeCarga).
//   2) ZARPAZO: se lanza hacia el jugador.
//   3) Si el jugador sigue al alcance, le hace daño.
//   4) Descansa (tiempoEntreAtaques) antes de poder atacar otra vez.
// Mismos tiempos que la versión 2D (EnemigoAtaqueGarra).
[RequireComponent(typeof(EnemigoMovimiento3D))]
public class EnemigoGarra3D : MonoBehaviour
{
    [Header("Configuración del ataque")]
    [Tooltip("A qué distancia (centro a centro) empieza el ataque. En 3D los modelos miden ~3 m, por eso es más que en 2D")]
    public float rangoAtaque = 3.5f;
    public float tiempoEntreAtaques = 2f;
    [Tooltip("Cuánto brilla amarillo antes de golpear. Más alto = más tiempo para esquivar")]
    public float tiempoDeCarga = 0.6f;
    [Tooltip("Cuántos segundos después de lanzar el zarpazo llega el daño")]
    public float momentoDelGolpe = 0.15f;
    [Tooltip("Cuánto tarda en recuperarse después del zarpazo antes de volver a moverse")]
    public float tiempoRecuperacion = 0.3f;
    public int danioGarra = 2;

    [Header("Embestida")]
    [Tooltip("Velocidad con la que se lanza hacia el jugador al dar el zarpazo")]
    public float velocidadEmbestida = 9f;

    private EnemigoMovimiento3D movimiento;
    private EnemigoAnimaciones3D anim;
    private Rigidbody rb;

    private float cooldownRestante = 0f;
    private bool estaAtacando = false;

    void Start()
    {
        movimiento = GetComponent<EnemigoMovimiento3D>();
        anim = GetComponent<EnemigoAnimaciones3D>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (cooldownRestante > 0) cooldownRestante -= Time.deltaTime;

        Transform objetivo = movimiento.Objetivo;
        if (objetivo == null || estaAtacando || !movimiento.Persiguiendo) return;

        if (DistanciaAlJugador(objetivo) <= rangoAtaque && cooldownRestante <= 0)
            StartCoroutine(RealizarAtaque());
    }

    private IEnumerator RealizarAtaque()
    {
        estaAtacando = true;

        // Se detiene mientras "carga" el golpe (pero sigue flotando y mirando al jugador)
        movimiento.multiplicadorVelocidad = 0f;

        // 1) AVISO
        AudioManager.Sonar(Sonido.AvisoEnemigo);
        if (anim != null) anim.IniciarAviso(tiempoDeCarga);

        yield return new WaitForSeconds(tiempoDeCarga);

        // 2) ZARPAZO: se lanza hacia donde está el jugador ahora
        if (anim != null) anim.Disparar();
        AudioManager.Sonar(Sonido.Garra);
        Transform objetivo = movimiento.Objetivo;
        if (objetivo != null && rb != null)
        {
            Vector3 dir = objetivo.position - transform.position;
            if (movimiento.EnPlanoLateral) dir = EnemigoPlano.Aplanar(dir, movimiento.Zona);
            rb.linearVelocity = dir.normalized * velocidadEmbestida;
        }

        // 3) El daño llega cuando la garra está estirada
        yield return new WaitForSeconds(momentoDelGolpe);

        objetivo = movimiento.Objetivo;
        if (objetivo != null && DistanciaAlJugador(objetivo) <= rangoAtaque + 0.5f)
            DañarJugador(objetivo);

        // Frena la embestida para no salir disparado lejos
        if (rb != null && !rb.isKinematic) rb.linearVelocity *= 0.2f;

        // 4) Recoge la garra antes de volver a moverse
        yield return new WaitForSeconds(tiempoRecuperacion);

        movimiento.multiplicadorVelocidad = 1f;
        cooldownRestante = tiempoEntreAtaques;
        estaAtacando = false;
    }

    float DistanciaAlJugador(Transform objetivo)
    {
        return Vector3.Distance(transform.position, objetivo.position);
    }

    void DañarJugador(Transform objetivo)
    {
        SubmarinoSalud3D salud = objetivo.GetComponent<SubmarinoSalud3D>();
        if (salud == null) salud = SubmarinoSalud3D.instancia;
        if (salud != null) salud.RecibirDaño(danioGarra);
    }

    // Si el enemigo muere (o lo apagan) a mitad del ataque, se cancela el golpe
    void OnDisable()
    {
        StopAllCoroutines();
        estaAtacando = false;
        if (movimiento != null) movimiento.multiplicadorVelocidad = 1f;
    }
}
