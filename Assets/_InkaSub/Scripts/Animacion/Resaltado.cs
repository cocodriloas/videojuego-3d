using UnityEngine;

// Hace que algo importante se distinga del escenario: un halo suave que siempre mira a la cámara
// y una luz que late. Lo llevan los animales con los que se habla (dorado) y las bacteria (verde lima).
public class Resaltado : MonoBehaviour
{
    public Color color = new Color(1f, 0.8f, 0.35f);
    [Tooltip("Tamaño del halo (metros)")]
    public float tamanioHalo = 3f;
    public float intensidadLuz = 2.5f;
    public float alcanceLuz = 5f;
    public float velocidadLatido = 2f;
    [Tooltip("Altura del centro del halo sobre el objeto")]
    public Vector3 desplazamiento = Vector3.zero;
    [Tooltip("Material aditivo con una textura redonda y suave (el de las partículas)")]
    public Material materialHalo;
    [Tooltip("Se apaga cuando ya no hace falta (por ejemplo, el NPC después de hablar)")]
    public NPCDialogo3D apagarAlHablar;
    [Tooltip("El halo va DETRÁS del objeto (como una luz de fondo): no lo tapa ni lo lava. Para los animales")]
    public bool detras = false;

    private Transform halo;
    private Renderer rendHalo;
    private Light luz;
    private MaterialPropertyBlock bloque;
    private float desfase;
    private float fuerza = 1f;

    void Start()
    {
        desfase = Random.value * 6f;
        bloque = new MaterialPropertyBlock();

        GameObject l = new GameObject("Luz resaltado");
        l.transform.SetParent(transform, false);
        l.transform.localPosition = desplazamiento + Vector3.back * 0.8f;   // un poco hacia la cámara
        luz = l.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = color;
        luz.range = alcanceLuz;
        luz.shadows = LightShadows.None;
        luz.renderingLayerMask = CapasLuz.Todas;   // también alumbra a los animales (capa Personajes)

        if (materialHalo != null)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Halo";
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(transform, false);
            halo = q.transform;
            rendHalo = q.GetComponent<Renderer>();
            rendHalo.sharedMaterial = MaterialBrillo(materialHalo);
            rendHalo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    // Un solo material para todos los halos: el aditivo de las partículas, pero con un brillo
    // redondo y suave (la textura de burbuja se veía como un anillo)
    static Material brillo;
    static Material MaterialBrillo(Material baseAditivo)
    {
        if (brillo != null) return brillo;
        const int n = 64;
        Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "BrilloSuave" };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = Mathf.Pow(1f - d, 2.2f);   // centro fuerte que se apaga suave hacia el borde
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        brillo = new Material(baseAditivo) { name = "HaloResaltado" };
        brillo.SetTexture("_BaseMap", t);
        return brillo;
    }

    void LateUpdate()
    {
        // Se apaga despacio cuando ya hablaste con el animal (sigue un brillo muy suave)
        float objetivo = apagarAlHablar != null && apagarAlHablar.YaHablo ? 0.25f : 1f;
        fuerza = Mathf.MoveTowards(fuerza, objetivo, Time.deltaTime * 0.5f);

        float k = (Mathf.Sin(Time.time * velocidadLatido + desfase) + 1f) * 0.5f;
        if (luz != null) luz.intensity = intensidadLuz * fuerza * (0.7f + k * 0.5f);
        if (halo == null) return;

        // El halo mira siempre a la cámara y no gira ni se estira con el objeto
        Camera cam = Camera.main;
        Vector3 centro = transform.position + transform.rotation * desplazamiento;
        Vector3 haciaCam = cam != null ? (cam.transform.position - centro).normalized : Vector3.back;
        halo.position = centro + haciaCam * (detras ? -1.2f : 0.6f);
        if (cam != null) halo.rotation = cam.transform.rotation;
        float escalaPadre = Mathf.Max(0.001f, transform.lossyScale.x);
        halo.localScale = Vector3.one * tamanioHalo * (0.9f + k * 0.2f) / escalaPadre;
        rendHalo.GetPropertyBlock(bloque);
        bloque.SetColor("_BaseColor", new Color(color.r, color.g, color.b, (0.22f + k * 0.18f) * fuerza));
        rendHalo.SetPropertyBlock(bloque);
    }
}
