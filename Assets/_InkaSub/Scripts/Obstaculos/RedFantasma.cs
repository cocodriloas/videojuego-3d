using System.Collections;
using UnityEngine;

// OBSTÁCULO: red de pesca abandonada ("red fantasma") que cuelga y se balancea.
// Si el submarino la toca, queda ATRAPADO: no puede moverse y pierde casco cada cierto tiempo.
// Para soltarse hay que dispararle a la red (o esperar a que se rompa sola, con daño).
// Necesita un Collider con "Is Trigger" (la zona de la red).
public class RedFantasma : MonoBehaviour, IDañable
{
    [Header("Atrapar")]
    [Tooltip("Disparos necesarios para romper la red y soltarse")]
    public int resistencia = 3;
    [Tooltip("Cada cuántos segundos quita 1 de casco mientras estás atrapado")]
    public float intervaloDanio = 1.5f;
    [Tooltip("Si no la rompes, te suelta sola después de este tiempo")]
    public float tiempoMaximoAtrapado = 4.5f;
    [Tooltip("Segundos sin poder atraparte de nuevo después de soltarte")]
    public float tiempoGracia = 1.5f;

    [Header("Balanceo")]
    [Tooltip("Lo que se balancea (normalmente el hijo con la malla de la red)")]
    public Transform visual;
    public float anguloBalanceo = 12f;
    public float velocidadBalanceo = 1.2f;

    private int golpesRecibidos = 0;
    private bool ocupada = false;
    private float graciaRestante = 0f;
    private float desfase;
    private Renderer[] renderers;
    private SubmarinoMovimiento3D presa;

    void Start()
    {
        desfase = Random.value * 10f;
        if (visual == null) visual = transform;
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        if (graciaRestante > 0f) graciaRestante -= Time.deltaTime;

        // Se balancea con la corriente (más fuerte si tiene a alguien atrapado)
        float fuerza = ocupada ? 2f : 1f;
        float ang = Mathf.Sin((Time.time + desfase) * velocidadBalanceo * fuerza) * anguloBalanceo * fuerza;
        visual.localRotation = Quaternion.Euler(0f, 0f, ang);
    }

    void OnTriggerEnter(Collider otro)
    {
        if (ocupada || graciaRestante > 0f || !otro.CompareTag("Player")) return;
        SubmarinoMovimiento3D mov = otro.GetComponent<SubmarinoMovimiento3D>();
        if (mov == null) return;
        StartCoroutine(Atrapar(mov));
    }

    IEnumerator Atrapar(SubmarinoMovimiento3D mov)
    {
        ocupada = true;
        presa = mov;
        golpesRecibidos = 0;
        mov.Atrapar(mov.transform.position * 0.5f + transform.position * 0.5f);
        AudioManager.Sonar(Sonido.Garra);

        float t = 0f, siguienteDaño = intervaloDanio;
        SubmarinoSalud3D saludPresa = mov.GetComponent<SubmarinoSalud3D>();
        while (ocupada && t < tiempoMaximoAtrapado)
        {
            // Si el submarino perdió mientras estaba atrapado, la red lo suelta
            if (saludPresa != null && saludPresa.EstaDerrotado) { Soltar(); yield break; }
            t += Time.deltaTime;
            if (t >= siguienteDaño)
            {
                siguienteDaño += intervaloDanio;
                SubmarinoSalud3D salud = mov.GetComponent<SubmarinoSalud3D>();
                if (salud != null) salud.RecibirDaño(1);
            }
            yield return null;
        }

        // Se soltó sola (no la rompiste): un último tirón de daño
        if (ocupada)
        {
            SubmarinoSalud3D salud = mov.GetComponent<SubmarinoSalud3D>();
            if (salud != null) salud.RecibirDaño(1);
            Soltar();
        }
    }

    // Los disparos del submarino rompen la red
    public void RecibirDaño(int cantidad)
    {
        golpesRecibidos += cantidad;
        AudioManager.Sonar(Sonido.EnemigoGolpeado);
        StartCoroutine(Parpadeo());

        if (golpesRecibidos >= resistencia)
        {
            AudioManager.Sonar(Sonido.EnemigoMuere);
            Soltar();
            // La red rota se deshace
            StartCoroutine(Deshacer());
        }
    }

    void Soltar()
    {
        if (presa != null) presa.Liberar();
        presa = null;
        ocupada = false;
        graciaRestante = tiempoGracia;
    }

    IEnumerator Parpadeo()
    {
        foreach (Renderer r in renderers) if (r != null) r.enabled = false;
        yield return new WaitForSeconds(0.06f);
        foreach (Renderer r in renderers) if (r != null) r.enabled = true;
    }

    IEnumerator Deshacer()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        Vector3 escala = transform.localScale;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escala, new Vector3(escala.x, 0f, escala.z), t / 0.4f);
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnDisable()
    {
        if (presa != null) presa.Liberar();
    }
}
