using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Oleada3D
{
    [Tooltip("Qué enemigo sale en cada punto (mismo orden que los puntos)")]
    public GameObject[] prefabsEnemigos;
    public Transform[] puntosDeSpawn;
}

// Sala de oleadas (igual que GeneradorOleadas del juego 2D): al entrar se CIERRAN las
// compuertas, salen las oleadas una tras otra (la siguiente cuando mueren todos) y al
// vencer la última se ABREN las compuertas. Necesita un Box Collider con "Is Trigger".
public class GeneradorOleadas3D : MonoBehaviour
{
    public Oleada3D[] oleadas;
    public float tiempoEntreOleadas = 2f;
    [Tooltip("Se cierran al entrar y se abren al vencer todas las oleadas")]
    public Barrera3D[] barreras;
    public string mensajeFinal = "¡ZONA DESPEJADA!";

    [Header("Aparición de cada enemigo")]
    [Tooltip("Material de partículas para las burbujas de aparición")]
    public Material materialBurbujas;

    private bool activado = false;
    private readonly List<GameObject> vivos = new List<GameObject>();

    public bool Terminado { get; private set; }

    // Para la partida guardada (la sala está siempre en el mismo lugar)
    public string Id => ProgresoJuego3D.Id(name, transform.position);

    // Al cargar una partida: esta sala ya se despejó (compuertas abiertas, no vuelve a empezar)
    public void MarcarTerminado()
    {
        Terminado = true;
        foreach (Barrera3D b in barreras) if (b != null) b.AbrirAlInstante();
    }

    private Coroutine rutina;

    void OnTriggerEnter(Collider otro)
    {
        if (activado || Terminado || !otro.CompareTag("Player")) return;
        activado = true;
        rutina = StartCoroutine(GenerarOleadas());
    }

    void Update()
    {
        // Si el submarino pierde a mitad de las oleadas, reaparece en el cangrejo (fuera de la sala).
        // Se reinicia todo para que pueda volver a entrar: si no, quedaría encerrado afuera.
        if (activado && !Terminado && SubmarinoSalud3D.instancia != null && SubmarinoSalud3D.instancia.EstaDerrotado)
            Reiniciar();
    }

    void Reiniciar()
    {
        if (rutina != null) StopCoroutine(rutina);
        rutina = null;
        foreach (GameObject e in vivos) if (e != null) Destroy(e);
        vivos.Clear();
        CamaraSeguimiento3D.CentroPelea = null;
        // Las compuertas vuelven a como estaban antes de empezar (la entrada abierta, la salida cerrada):
        // así se puede volver a entrar, pero no saltarse la sala
        for (int i = 0; i < barreras.Length; i++)
            if (barreras[i] != null && estabaAbierta != null && i < estabaAbierta.Length)
            {
                if (estabaAbierta[i]) barreras[i].Abrir(); else barreras[i].Cerrar();
            }
        activado = false;
    }

    private bool[] estabaAbierta;

    IEnumerator GenerarOleadas()
    {
        // La cámara se aleja mientras dura la pelea, para que se vea a todos los enemigos
        CamaraSeguimiento3D.CentroPelea = CentroSala();
        CamaraSeguimiento3D.DistanciaExtraPelea = distanciaExtraCamara;
        estabaAbierta = new bool[barreras.Length];
        for (int i = 0; i < barreras.Length; i++) estabaAbierta[i] = barreras[i] != null && barreras[i].EstaAbierta;
        foreach (Barrera3D b in barreras) if (b != null) b.Cerrar();

        for (int n = 0; n < oleadas.Length; n++)
        {
            MensajePantalla.Mostrar("OLEADA " + (n + 1) + " / " + oleadas.Length);
            AudioManager.Sonar(Sonido.AvisoEnemigo);
            yield return new WaitForSeconds(1.2f);

            Oleada3D oleada = oleadas[n];
            vivos.Clear();

            // Dónde sale cada uno: nunca encima del submarino (si el punto está cerca, se usa el más lejano libre)
            List<Vector3> posiciones = ElegirPosiciones(oleada);

            // Aviso: burbujas y una luz roja donde van a salir, un momento antes
            foreach (Vector3 pos in posiciones) { Burbujas(pos); StartCoroutine(LuzAviso(pos, tiempoAviso)); }
            yield return new WaitForSeconds(tiempoAviso);

            for (int i = 0; i < oleada.prefabsEnemigos.Length; i++)
            {
                Vector3 pos = posiciones[i];
                Vector3 haciaJugador = Jugador != null ? Jugador.position - pos : Vector3.left;
                haciaJugador.y = 0f; haciaJugador.z = 0f;
                GameObject e = Instantiate(oleada.prefabsEnemigos[i], pos, Quaternion.LookRotation(haciaJugador.x >= 0f ? Vector3.right : Vector3.left));
                vivos.Add(e);
                StartCoroutine(Aparecer(e));
                yield return new WaitForSeconds(0.25f); // no todos de golpe
            }

            // La siguiente oleada sale cuando mueren todos los de esta.
            // Vigilante: si alguno se sale de la sala (empujado o retrocediendo), vuelve a un punto de la sala;
            // si no, la oleada nunca terminaría porque no se lo puede alcanzar
            while (vivos.Exists(e => e != null))
            {
                yield return new WaitForSeconds(0.3f);
                VolverALaSala(oleada);
            }
            if (n < oleadas.Length - 1) yield return new WaitForSeconds(tiempoEntreOleadas);
        }

        CamaraSeguimiento3D.CentroPelea = null;
        MensajePantalla.Mostrar(mensajeFinal);
        AudioManager.Sonar(Sonido.Desbloqueo);
        foreach (Barrera3D b in barreras) if (b != null) b.Abrir();
        Terminado = true;
    }

    [Header("Que no aparezcan encima del jugador")]
    [Tooltip("Ningún enemigo sale a menos de esta distancia del submarino")]
    public float distanciaMinimaJugador = 6f;
    [Tooltip("Segundos de aviso (burbujas y luz roja) antes de que salgan")]
    public float tiempoAviso = 0.9f;

    Transform Jugador => SubmarinoSalud3D.instancia != null ? SubmarinoSalud3D.instancia.transform : null;

    List<Vector3> ElegirPosiciones(Oleada3D oleada)
    {
        List<Vector3> libres = new List<Vector3>();
        foreach (Transform t in oleada.puntosDeSpawn) if (t != null) libres.Add(t.position);
        List<Vector3> elegidas = new List<Vector3>();
        Transform j = Jugador;
        for (int i = 0; i < oleada.prefabsEnemigos.Length; i++)
        {
            Vector3 pos = i < oleada.puntosDeSpawn.Length && oleada.puntosDeSpawn[i] != null ? oleada.puntosDeSpawn[i].position : transform.position;
            if (j != null && Vector3.Distance(pos, j.position) < distanciaMinimaJugador)
            {
                // El punto libre más lejano del submarino
                Vector3 mejor = pos; float lejos = -1f;
                foreach (Vector3 c in libres)
                {
                    if (elegidas.Contains(c)) continue;
                    float d = Vector3.Distance(c, j.position);
                    if (d > lejos) { lejos = d; mejor = c; }
                }
                // (siempre un punto de la sala: correrlo a un lado podía dejarlo afuera, detrás de una pared)
                pos = mejor;
            }
            elegidas.Add(pos);
        }
        return elegidas;
    }

    [Header("Cámara durante la pelea")]
    [Tooltip("Metros que se aleja la cámara mientras hay oleadas (para ver a todos los enemigos)")]
    public float distanciaExtraCamara = 6f;

    Vector3 CentroSala()
    {
        Vector3 c = Vector3.zero; int n = 0;
        foreach (Oleada3D o in oleadas) foreach (Transform t in o.puntosDeSpawn) if (t != null) { c += t.position; n++; }
        return n > 0 ? c / n : transform.position;
    }

    // ¿Hay una compuerta o muro de energía entre el centro de la sala y el enemigo? Entonces quedó afuera
    static bool DetrasDeBarrera(Vector3 centro, Vector3 pos)
    {
        foreach (RaycastHit h in Physics.RaycastAll(centro, pos - centro, Vector3.Distance(centro, pos), ~0, QueryTriggerInteraction.Ignore))
            if (h.collider.GetComponentInParent<Barrera3D>() != null) return true;
        return false;
    }

    void OnDisable() { if (activado && !Terminado) CamaraSeguimiento3D.CentroPelea = null; }

    void VolverALaSala(Oleada3D oleada)
    {
        List<Vector3> puntos = new List<Vector3>();
        foreach (Oleada3D o in oleadas) foreach (Transform t in o.puntosDeSpawn) if (t != null) puntos.Add(t.position);
        if (puntos.Count == 0) return;
        Vector3 centro = Vector3.zero;
        foreach (Vector3 p in puntos) centro += p;
        centro /= puntos.Count;
        float radio = 0f;
        foreach (Vector3 p in puntos) radio = Mathf.Max(radio, Vector3.Distance(p, centro));
        radio += 5f;   // un margen: los enemigos se mueven un poco alrededor de su punto

        foreach (GameObject e in vivos)
        {
            // Afuera = muy lejos del centro, o del otro lado de una compuerta (aunque esté cerca)
            if (e == null || (Vector3.Distance(e.transform.position, centro) <= radio && !DetrasDeBarrera(centro, e.transform.position))) continue;
            Vector3 destino = puntos[Random.Range(0, puntos.Count)];
            Rigidbody rb = e.GetComponent<Rigidbody>();
            if (rb != null) { rb.position = destino; if (!rb.isKinematic) rb.linearVelocity = Vector3.zero; }
            e.transform.position = destino;
            foreach (EnemigoPatrulla3D s in e.GetComponents<EnemigoPatrulla3D>()) s.Reubicar(destino);
            foreach (EnemigoMovimiento3D s in e.GetComponents<EnemigoMovimiento3D>()) s.Reubicar(destino);
            Burbujas(destino);
        }
    }

    IEnumerator LuzAviso(Vector3 pos, float duracion)
    {
        GameObject go = new GameObject("AvisoAparicion");
        go.transform.position = pos + Vector3.back;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.25f, 0.2f);
        l.range = 4f;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            l.intensity = 3f * Mathf.Abs(Mathf.Sin(t * 14f));
            yield return null;
        }
        Destroy(go);
    }

    // El enemigo aparece creciendo dentro de una nube de burbujas. Mientras crece no choca,
    // no ataca ni se mueve (así no le quita vida al submarino de sorpresa)
    IEnumerator Aparecer(GameObject e)
    {
        Burbujas(e.transform.position);
        Collider[] cols = e.GetComponentsInChildren<Collider>();
        MonoBehaviour[] ia = e.GetComponents<MonoBehaviour>()
            .Where(m => m is EnemigoPatrulla3D || m is EnemigoMovimiento3D || m is EnemigoDistancia3D || m is EnemigoGarra3D).ToArray();
        foreach (Collider c in cols) c.enabled = false;
        foreach (MonoBehaviour m in ia) m.enabled = false;

        Vector3 escala = e.transform.localScale;
        for (float t = 0f; t < 0.6f && e != null; t += Time.deltaTime)
        {
            e.transform.localScale = escala * Mathf.SmoothStep(0.05f, 1f, t / 0.6f);
            yield return null;
        }
        if (e == null) yield break;
        e.transform.localScale = escala;
        foreach (Collider c in cols) if (c != null) c.enabled = true;
        foreach (MonoBehaviour m in ia) if (m != null) m.enabled = true;
    }

    void Burbujas(Vector3 pos)
    {
        GameObject go = new GameObject("BurbujasAparicion");
        go.transform.position = pos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false;
        main.duration = 0.3f;
        main.startLifetime = 1f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.gravityModifier = -0.4f;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });
        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 1f;
        if (materialBurbujas != null) go.GetComponent<ParticleSystemRenderer>().sharedMaterial = materialBurbujas;
        ps.Play();
        Destroy(go, 2f);
    }

    void OnDrawGizmos()
    {
        if (oleadas == null) return;
        Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
        foreach (Oleada3D o in oleadas)
            if (o != null && o.puntosDeSpawn != null)
                foreach (Transform p in o.puntosDeSpawn)
                    if (p != null) Gizmos.DrawWireSphere(p.position, 0.6f);
    }
}
