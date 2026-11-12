using UnityEngine;

// Animación por código para animales que no traen animaciones (modelos de Meshy de una pieza).
// Nada de un lado a otro dentro de un área, da la vuelta en los extremos, sube y baja,
// y se mece como si moviera las aletas. Ponlo en la RAÍZ del animal (el modelo puede ser hijo).
public class AnimacionNadar : MonoBehaviour
{
    [Header("Recorrido (0 = nada en su sitio)")]
    [Tooltip("Cuánto nada hacia cada lado de su punto de inicio")]
    public float distanciaNado = 2f;
    public float velocidad = 0.8f;
    [Tooltip("Eje por el que nada (en el mundo)")]
    public Vector3 eje = Vector3.right;
    [Tooltip("Segundos que tarda en dar la vuelta")]
    public float tiempoGiro = 1.2f;

    [Header("Movimiento del cuerpo")]
    public float alturaFlotar = 0.2f;
    public float velocidadFlotar = 1.2f;
    [Tooltip("Grados que se mece de lado a lado (como al mover las aletas)")]
    public float mecerse = 8f;
    [Tooltip("Grados que cabecea arriba/abajo")]
    public float cabeceo = 5f;
    public float velocidadAletas = 2.2f;

    private Vector3 inicio;
    private float recorrido;     // -1..1 a lo largo del eje
    private int sentido = 1;
    private float giroActual;    // 0 = mira hacia +eje, 180 = hacia -eje
    private float desfase;
    private Quaternion rotacionBase;

    void Start()
    {
        inicio = transform.position;
        eje = eje.normalized;
        desfase = Random.value * 10f;
        // La rotación que le dio el nivel indica hacia dónde mira al empezar
        sentido = Vector3.Dot(transform.forward, eje) >= 0f ? 1 : -1;
        giroActual = sentido > 0 ? 0f : 180f;
        rotacionBase = Quaternion.LookRotation(eje, Vector3.up);
    }

    void Update()
    {
        float t = Time.time + desfase;

        // Avanza y da la vuelta en los extremos
        if (distanciaNado > 0f)
        {
            recorrido += sentido * velocidad * Time.deltaTime / distanciaNado;
            if (recorrido > 1f) { recorrido = 1f; sentido = -1; }
            if (recorrido < -1f) { recorrido = -1f; sentido = 1; }
        }
        float giroObjetivo = sentido > 0 ? 0f : 180f;
        giroActual = Mathf.MoveTowards(giroActual, giroObjetivo, 180f / Mathf.Max(0.1f, tiempoGiro) * Time.deltaTime);

        Vector3 pos = inicio + eje * recorrido * distanciaNado + Vector3.up * Mathf.Sin(t * velocidadFlotar) * alturaFlotar;
        Quaternion cuerpo = Quaternion.Euler(Mathf.Sin(t * velocidadAletas * 0.5f) * cabeceo,
                                             Mathf.Sin(t * velocidadAletas) * mecerse,
                                             Mathf.Sin(t * velocidadAletas + 1f) * mecerse * 0.5f);
        transform.SetPositionAndRotation(pos, rotacionBase * Quaternion.Euler(0f, giroActual, 0f) * cuerpo);
    }
}
