using UnityEngine;

// Un ojo débil del Iceberg de Basura (lo crea JefeIceberg3D).
// ABIERTO = rojo brillante que late: las balas le hacen daño al jefe.
// CERRADO = pequeño y oscuro: las balas se pierden sin daño.
public class JefeOjo3D : MonoBehaviour, IDañable
{
    [HideInInspector] public JefeIceberg3D jefe;

    private bool abierto;
    private float destelloHasta = -1f, abriendoDesde = -10f;
    private Vector3 escalaBase;
    private Renderer rend;
    private MaterialPropertyBlock bloque;
    private Light luz;

    public bool Abierto => abierto;

    void Awake()
    {
        escalaBase = transform.localScale;
        rend = GetComponent<Renderer>();
        bloque = new MaterialPropertyBlock();
        GameObject l = new GameObject("Brillo");
        l.transform.SetParent(transform, false);
        l.transform.localPosition = Vector3.left * 0.6f;
        luz = l.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.2f, 0.15f);
        luz.range = 6f;
    }

    public void Abrir()
    {
        if (!abierto) abriendoDesde = Time.time;
        abierto = true;
    }

    public void Cerrar() { abierto = false; }
    public void Destello() { destelloHasta = Time.time + 0.08f; }

    void Update()
    {
        bool destello = Time.time < destelloHasta;
        float pulso = Mathf.Sin(Time.time * 8f);
        // Al abrirse crece rápido desde chiquito (como un párpado que se abre)
        float abrir = Mathf.Clamp01((Time.time - abriendoDesde) / 0.25f);

        Color color, emision;
        float escala;
        if (abierto)
        {
            float brillo = 0.75f + 0.25f * pulso;
            color = new Color(1f, 0.25f, 0.2f);
            emision = new Color(2.5f, 0.35f, 0.25f) * brillo;
            escala = Mathf.Lerp(0.5f, 1f + 0.08f * pulso, abrir);
            luz.intensity = 3f * brillo;
        }
        else
        {
            color = new Color(0.25f, 0.07f, 0.07f);
            emision = Color.black;
            escala = 0.55f;
            luz.intensity = 0f;
        }
        if (destello) { color = Color.white; emision = Color.white * 3f; escala *= 1.15f; }

        transform.localScale = escalaBase * escala;
        rend.GetPropertyBlock(bloque);
        bloque.SetColor("_BaseColor", color);
        bloque.SetColor("_EmissionColor", emision);
        rend.SetPropertyBlock(bloque);
    }

    // Las balas llaman a esto: el jefe decide si cuenta (solo con el ojo abierto)
    public void RecibirDaño(int cantidad)
    {
        if (jefe != null) jefe.GolpeEnOjo(this, cantidad);
    }
}
