using UnityEngine;
using UnityEngine.Events;

// Vida de un enemigo 3D. El proyectil del submarino lo encuentra porque implementa IDañable.
// Al recibir daño destella y se infla un poco; al morir suena, deja un efecto (opcional),
// avisa con "alMorir" (así las ZonaEncuentro3D cuentan las muertes) y se encoge hasta borrarse.
public class EnemigoSalud3D : MonoBehaviour, IDañable
{
    [Header("Vida del enemigo")]
    public int vidaMaxima = 3;
    private int vidaActual;

    [Header("Drop de curación al morir (opcional)")]
    public GameObject prefabCuracion;
    [Range(0f, 1f)] public float probabilidadDrop = 0.25f;

    [Header("Efectos")]
    [Tooltip("Efecto de partículas al morir (opcional)")]
    public GameObject prefabEfectoMuerte;

    [Header("Eventos")]
    [Tooltip("Se llama una sola vez, justo cuando el enemigo muere")]
    public UnityEvent alMorir = new UnityEvent();

    private bool muerto = false;
    private EnemigoAnimaciones3D anim;

    public int VidaActual => vidaActual;
    public bool EstaMuerto => muerto;

    // Para la partida guardada: los enemigos vencidos antes del cangrejo no vuelven.
    // (Los que empiezan desactivados no pasan por Awake: se calcula al pedirlo, aún sin moverse)
    private string id;
    public string Id => id ?? (id = ProgresoJuego3D.Id(name, transform.position));

    void Awake()
    {
        id = ProgresoJuego3D.Id(name, transform.position);
        vidaActual = vidaMaxima;
        anim = GetComponent<EnemigoAnimaciones3D>();
    }

    public void RecibirDaño(int cantidad)
    {
        if (muerto) return;

        vidaActual -= cantidad;

        if (vidaActual > 0)
        {
            if (anim != null) anim.RecibirGolpe();
            AudioManager.Sonar(Sonido.EnemigoGolpeado);
            return;
        }

        Morir();
    }

    void Morir()
    {
        muerto = true;
        ProgresoJuego3D.EnemigoVencido(Id);
        AudioManager.Sonar(Sonido.EnemigoMuere);

        if (prefabEfectoMuerte != null)
            Instantiate(prefabEfectoMuerte, transform.position, Quaternion.identity);

        if (prefabCuracion != null && Random.value <= probabilidadDrop)
            Instantiate(prefabCuracion, transform.position, Quaternion.identity);

        // Avisamos antes de borrarlo, para que la zona de encuentro lo cuente al instante
        alMorir.Invoke();

        // Con animaciones se encoge y luego se borra; sin ellas desaparece al momento
        if (anim != null) anim.Morir();
        else Destroy(gameObject);
    }
}
