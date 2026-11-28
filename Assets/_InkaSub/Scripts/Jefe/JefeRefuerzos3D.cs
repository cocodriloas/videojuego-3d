using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Refuerzos del Iceberg de Basura: manda enemigos desde el suelo cada cierto tiempo (igual que el 2D).
// Va en el mismo objeto que JefeIceberg3D. Se detiene si el jefe muere o si la pelea se reinicia.
[RequireComponent(typeof(JefeIceberg3D))]
public class JefeRefuerzos3D : MonoBehaviour
{
    public GameObject[] prefabsEnemigos;
    public Transform[] puntosDeAparicion;
    [Tooltip("No aparecen a menos de esta distancia del submarino (si hay otro punto más lejos)")]
    public float distanciaMinimaDelJugador = 6f;
    public float pausaInicial = 4f;
    public Vector2 segundosEntreOleadas = new Vector2(5f, 10f);
    public int[] enemigosPorOleadaPorFase = { 1, 1, 2 };
    public int maximoVivos = 3;

    private JefeIceberg3D jefe;
    private Transform jugador;
    private readonly List<GameObject> vivos = new List<GameObject>();
    private Coroutine rutina;

    void Start()
    {
        jefe = GetComponent<JefeIceberg3D>();
        GameObject j = GameObject.FindGameObjectWithTag("Player");
        if (j != null) jugador = j.transform;
    }

    void Update()
    {
        if (jefe.EstaPeleando && rutina == null) rutina = StartCoroutine(Rutina());
        if (!jefe.EstaPeleando && rutina != null)
        {
            StopCoroutine(rutina);
            rutina = null;
            // Si la pelea se reinició (el submarino perdió), los refuerzos que quedaban desaparecen
            if (!jefe.EstaMuerto) { foreach (GameObject e in vivos) if (e != null) Destroy(e); vivos.Clear(); }
        }
    }

    IEnumerator Rutina()
    {
        yield return new WaitForSeconds(pausaInicial);
        while (jefe.EstaPeleando)
        {
            yield return new WaitForSeconds(Random.Range(Mathf.Max(1f, segundosEntreOleadas.x), Mathf.Max(segundosEntreOleadas.x, segundosEntreOleadas.y)));
            if (!jefe.EstaPeleando) break;
            vivos.RemoveAll(e => e == null);
            int libres = maximoVivos - vivos.Count;
            int fase = Mathf.Clamp(jefe.FaseActual - 1, 0, enemigosPorOleadaPorFase.Length - 1);
            for (int i = 0; i < Mathf.Min(libres, enemigosPorOleadaPorFase[fase]); i++) Aparecer();
        }
        rutina = null;
    }

    void Aparecer()
    {
        if (prefabsEnemigos == null || prefabsEnemigos.Length == 0 || puntosDeAparicion == null || puntosDeAparicion.Length == 0) return;
        Transform punto = ElegirPunto();
        GameObject prefab = prefabsEnemigos[Random.Range(0, prefabsEnemigos.Length)];
        if (punto == null || prefab == null) return;
        StartCoroutine(AparecerConAviso(punto.position, prefab));
    }

    // Aviso (burbujas y luz roja), y el enemigo crece sin chocar ni atacar hasta estar completo:
    // así no le quita vida al submarino de sorpresa
    IEnumerator AparecerConAviso(Vector3 pos, GameObject prefab)
    {
        JefeAtaques3D.Burbujas(pos, 30, 1f);
        GameObject aviso = new GameObject("AvisoRefuerzo");
        aviso.transform.position = pos + Vector3.back;
        Light l = aviso.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.25f, 0.2f); l.range = 4f;
        for (float t = 0f; t < 0.9f; t += Time.deltaTime) { l.intensity = 3f * Mathf.Abs(Mathf.Sin(t * 14f)); yield return null; }
        Destroy(aviso);
        if (!jefe.EstaPeleando) yield break;

        GameObject e = Instantiate(prefab, pos, Quaternion.LookRotation(Vector3.left));
        vivos.Add(e);
        Collider[] cols = e.GetComponentsInChildren<Collider>();
        List<MonoBehaviour> ia = new List<MonoBehaviour>();
        foreach (MonoBehaviour m in e.GetComponents<MonoBehaviour>())
            if (m is EnemigoPatrulla3D || m is EnemigoMovimiento3D || m is EnemigoDistancia3D || m is EnemigoGarra3D) ia.Add(m);
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

    Transform ElegirPunto()
    {
        List<Transform> lejanos = new List<Transform>();
        Transform masLejano = null;
        float mayor = -1f;
        foreach (Transform p in puntosDeAparicion)
        {
            if (p == null) continue;
            float d = jugador != null ? Vector3.Distance(p.position, jugador.position) : 999f;
            if (d >= distanciaMinimaDelJugador) lejanos.Add(p);
            if (d > mayor) { mayor = d; masLejano = p; }
        }
        return lejanos.Count > 0 ? lejanos[Random.Range(0, lejanos.Count)] : masLejano;
    }
}
