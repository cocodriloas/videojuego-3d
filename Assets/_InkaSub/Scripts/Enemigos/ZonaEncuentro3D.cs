using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Una caja invisible (Box Collider con "Is Trigger"). Cuando el submarino entra:
//   1) Cierra las barreras (las activa) para que no pueda escapar.
//   2) Despierta a los enemigos de la lista.
//   3) Cuando todos mueren, vuelve a abrir las barreras (las desactiva) y se borra.
// Si no tiene barreras, solo despierta a los enemigos y se borra al momento (como en 2D).
[RequireComponent(typeof(BoxCollider))]
public class ZonaEncuentro3D : MonoBehaviour
{
    [Header("Enemigos a activar en esta zona")]
    [Tooltip("Pueden empezar desactivados (se activan) o con sus scripts apagados (se encienden)")]
    public GameObject[] enemigosAActivar;

    [Header("Barreras: se CIERRAN al entrar y se ABREN al vencer a todos (opcional)")]
    [Tooltip("GameObjects que solo existen mientras quedan enemigos vivos (rocas, redes, corrientes...)")]
    public GameObject[] barreras;

    [Header("Eventos (opcional)")]
    public UnityEvent alEmpezar = new UnityEvent();
    public UnityEvent alDespejar = new UnityEvent();

    private bool yaActivada = false;
    private int enemigosVivos = 0;

    public bool YaActivada => yaActivada;
    public int EnemigosVivos => enemigosVivos;

    void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void Awake()
    {
        // Las barreras empiezan abiertas: solo aparecen cuando empieza la pelea
        AbrirBarreras();
    }

    void OnTriggerEnter(Collider otro)
    {
        if (yaActivada || !EsJugador(otro)) return;
        yaActivada = true;
        StartCoroutine(Encuentro());
    }

    bool EsJugador(Collider otro)
    {
        return otro.CompareTag("Player") ||
               (otro.attachedRigidbody != null && otro.attachedRigidbody.CompareTag("Player"));
    }

    IEnumerator Encuentro()
    {
        if (barreras != null) foreach (GameObject b in barreras) if (b != null) b.SetActive(true);
        alEmpezar.Invoke();

        // Despertar a los enemigos y escuchar cuándo muere cada uno
        enemigosVivos = 0;
        if (enemigosAActivar != null)
        {
            foreach (GameObject enemigo in enemigosAActivar)
            {
                if (enemigo == null) continue;
                Despertar(enemigo);

                EnemigoSalud3D salud = enemigo.GetComponent<EnemigoSalud3D>();
                if (salud != null && !salud.EstaMuerto)
                {
                    enemigosVivos++;
                    salud.alMorir.AddListener(UnoMenos);
                }
            }
        }

        // Si esta zona no tiene muros, no hace falta esperar: termina aquí (como en 2D)
        if (barreras == null || barreras.Length == 0)
        {
            Destroy(gameObject);
            yield break;
        }

        // Esperar a que todos mueran. Además del contador revisamos si se borraron
        // por otro motivo (por ejemplo, un enemigo sin EnemigoSalud3D).
        while (enemigosVivos > 0 || QuedaAlgunoSinSalud())
            yield return new WaitForSeconds(0.25f);

        AbrirBarreras();
        alDespejar.Invoke();
        Destroy(gameObject);
    }

    // Activa el GameObject y enciende sus scripts de IA, por si estaban apagados
    void Despertar(GameObject enemigo)
    {
        enemigo.SetActive(true);
        foreach (EnemigoMovimiento3D s in enemigo.GetComponents<EnemigoMovimiento3D>()) s.enabled = true;
        foreach (EnemigoGarra3D s in enemigo.GetComponents<EnemigoGarra3D>()) s.enabled = true;
        foreach (EnemigoDistancia3D s in enemigo.GetComponents<EnemigoDistancia3D>()) s.enabled = true;
    }

    void UnoMenos()
    {
        enemigosVivos--;
    }

    bool QuedaAlgunoSinSalud()
    {
        foreach (GameObject enemigo in enemigosAActivar)
            if (enemigo != null && enemigo.GetComponent<EnemigoSalud3D>() == null) return true;
        return false;
    }

    void AbrirBarreras()
    {
        if (barreras == null) return;
        foreach (GameObject b in barreras) if (b != null) b.SetActive(false);
    }

    // Dibuja la zona en la Scene (solo en el editor) y una línea hacia cada enemigo
    void OnDrawGizmos()
    {
        BoxCollider caja = GetComponent<BoxCollider>();
        if (caja == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.08f);
        Gizmos.DrawCube(caja.center, caja.size);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawWireCube(caja.center, caja.size);
        Gizmos.matrix = Matrix4x4.identity;

        if (enemigosAActivar == null) return;
        Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.5f);
        foreach (GameObject e in enemigosAActivar)
            if (e != null) Gizmos.DrawLine(transform.position, e.transform.position);
    }
}
