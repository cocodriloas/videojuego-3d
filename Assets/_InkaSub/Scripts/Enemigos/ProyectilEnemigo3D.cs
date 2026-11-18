using UnityEngine;

// Proyectil que disparan los enemigos. Avanza hacia su eje Z (la flecha azul),
// daña al submarino y desaparece al chocar con una pared.
// Atraviesa a los otros enemigos y a las zonas invisibles (triggers).
[RequireComponent(typeof(Rigidbody))]
public class ProyectilEnemigo3D : MonoBehaviour
{
    [Header("Configuración")]
    public float velocidad = 6f;
    public float tiempoDeVida = 5f;   // a 6 m/s llega a ~30 m: alcanza al submarino en los túneles largos
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

        Destroy(gameObject, tiempoDeVida);
    }

    // Por si alguien quiere cambiar la dirección después de crearlo
    public void SetDireccion(Vector3 direccion)
    {
        if (direccion.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(direccion);
        if (rb != null) rb.linearVelocity = direccion.normalized * velocidad;
    }

    void OnTriggerEnter(Collider otro)
    {
        // ¿Es el submarino? (el collider puede estar en un hijo, por eso miramos el Rigidbody también)
        bool esJugador = otro.CompareTag("Player") ||
                         (otro.attachedRigidbody != null && otro.attachedRigidbody.CompareTag("Player"));
        if (esJugador)
        {
            SubmarinoSalud3D salud = otro.GetComponentInParent<SubmarinoSalud3D>();
            if (salud == null) salud = SubmarinoSalud3D.instancia;
            if (salud != null) salud.RecibirDaño(danio);
            Impactar();
            return;
        }

        // Las zonas invisibles, otros proyectiles y otros enemigos no lo detienen
        if (otro.isTrigger) return;
        if (otro.CompareTag("Enemigo") || otro.GetComponentInParent<EnemigoSalud3D>() != null) return;

        // Cualquier otra cosa sólida es una pared
        Impactar();
    }

    void Impactar()
    {
        if (efectoImpacto != null) Instantiate(efectoImpacto, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
