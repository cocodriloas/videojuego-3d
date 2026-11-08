using UnityEngine;

// Bacteria disparada por el submarino. Avanza hacia su eje Z (la flecha azul),
// daña lo que tenga un script IDañable y desaparece al chocar con una pared.
[RequireComponent(typeof(Rigidbody))]
public class ProyectilMovimiento3D : MonoBehaviour
{
    [Header("Configuración del proyectil")]
    public float velocidad = 18f;
    public float tiempoDeVida = 3f; // segundos antes de autodestruirse
    public int danio = 1;

    [Header("Efecto al impactar (opcional)")]
    public GameObject efectoImpacto;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.linearVelocity = transform.forward * velocidad;

        // Así no se acumulan proyectiles invisibles llenando la memoria
        Destroy(gameObject, tiempoDeVida);
    }

    void OnTriggerEnter(Collider otro)
    {
        if (otro.CompareTag("Player")) return;
        // Los muros de energía (compuertas) dejan pasar las balas del submarino: así se puede
        // vencer a un enemigo que quedó del otro lado (el submarino sigue sin poder pasar)
        if (otro.GetComponentInParent<Barrera3D>() != null) return;

        IDañable objetivo = otro.GetComponentInParent<IDañable>();
        // Las zonas invisibles (triggers) que no se pueden dañar no detienen la bala
        if (otro.isTrigger && objetivo == null) return;

        if (objetivo != null) objetivo.RecibirDaño(danio);

        // Choca con un enemigo o con una pared: desaparece
        Impactar();
    }

    void Impactar()
    {
        if (efectoImpacto != null) Instantiate(efectoImpacto, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
