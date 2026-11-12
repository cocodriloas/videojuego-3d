using UnityEngine;
using UnityEngine.InputSystem;

// Linterna en la punta del submarino: una luz de foco y un "haz" visible (cono transparente).
// Se prende y apaga con la tecla F. Como es hija de "Modelo", apunta hacia donde mira el submarino.
public class LinternaSubmarino : MonoBehaviour
{
    public bool encendida = true;
    [Tooltip("Posición de la punta del submarino dentro de 'Modelo' (la punta mira hacia +Z)")]
    public Vector3 posicionPunta = new Vector3(0f, 0.05f, 1.25f);
    public Color color = new Color(1f, 0.95f, 0.8f);
    public float alcance = 22f;
    public float angulo = 55f;
    public float intensidad = 6f;
    [Tooltip("Material transparente aditivo para el haz (si está vacío se crea uno)")]
    public Material materialHaz;

    private Light foco;
    private GameObject haz;

    void Start()
    {
        Transform modelo = transform.Find("Modelo");
        if (modelo == null) modelo = transform;

        GameObject go = new GameObject("Linterna");
        go.transform.SetParent(modelo, false);
        go.transform.localPosition = posicionPunta;
        foco = go.AddComponent<Light>();
        foco.type = LightType.Spot;
        foco.color = color;
        foco.range = alcance;
        foco.spotAngle = angulo;
        foco.innerSpotAngle = angulo * 0.5f;
        foco.intensity = intensidad;
        foco.shadows = LightShadows.None;
        // Solo alumbra la capa de luz "Default": los animales con los que se habla van en la capa 1
        // y la linterna no los quema (se veían feos al alumbrarlos de cerca)
        foco.renderingLayerMask = CapasLuz.Escenario;

        haz = CrearHaz(go.transform);
        Aplicar();
    }

    void Update()
    {
        if (Keyboard.current != null && Time.timeScale > 0f && Keyboard.current.fKey.wasPressedThisFrame)
        {
            encendida = !encendida;
            AudioManager.Sonar(Sonido.BotonUI);
            Aplicar();
        }
        AtenuarSobreAnimales();
    }

    // El haz visible (el cono transparente) se dibuja ENCIMA de lo que alumbra: sobre un animal lo
    // dejaba como un fantasma. Si apunta a un animal cercano, el haz se desvanece casi del todo.
    private NPCDialogo3D[] animales;
    private Renderer rendHaz;
    private MaterialPropertyBlock bloqueHaz;
    private float fuerzaHaz = 1f;

    void AtenuarSobreAnimales()
    {
        if (haz == null || !haz.activeSelf) return;
        if (animales == null) animales = FindObjectsByType<NPCDialogo3D>(FindObjectsSortMode.None);
        if (rendHaz == null) { rendHaz = haz.GetComponent<Renderer>(); bloqueHaz = new MaterialPropertyBlock(); }
        bool sobreAnimal = false;
        Vector3 boca = haz.transform.position, dir = haz.transform.forward;
        foreach (NPCDialogo3D a in animales)
        {
            if (a == null) continue;
            Vector3 hacia = a.transform.position - boca;
            if (hacia.magnitude < alcance * 0.5f && Vector3.Angle(dir, hacia) < angulo * 0.75f) { sobreAnimal = true; break; }
        }
        fuerzaHaz = Mathf.MoveTowards(fuerzaHaz, sobreAnimal ? 0.12f : 1f, Time.deltaTime * 4f);
        rendHaz.GetPropertyBlock(bloqueHaz);
        bloqueHaz.SetColor("_BaseColor", new Color(1f, 1f, 1f, fuerzaHaz));
        rendHaz.SetPropertyBlock(bloqueHaz);
    }

    private bool suspendida;

    // Mientras el submarino está "hundido" la linterna se apaga sin perder si estaba prendida
    public void Suspender(bool s)
    {
        suspendida = s;
        if (foco != null) Aplicar();
    }

    void Aplicar()
    {
        foco.enabled = encendida && !suspendida;
        haz.SetActive(encendida && !suspendida);
    }

    // Cono hueco hecho por código, más ancho y transparente hacia la punta
    GameObject CrearHaz(Transform padre)
    {
        const int lados = 24;
        float largo = alcance * 0.35f;
        float radio = Mathf.Tan(angulo * 0.5f * Mathf.Deg2Rad) * largo;

        Vector3[] v = new Vector3[(lados + 1) * 2];
        Color[] c = new Color[v.Length];
        Vector2[] uv = new Vector2[v.Length];
        int[] tri = new int[lados * 6];
        for (int i = 0; i <= lados; i++)
        {
            float a = i * Mathf.PI * 2f / lados;
            Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            v[i * 2] = dir * 0.12f;                               // boca de la linterna
            v[i * 2 + 1] = dir * radio + Vector3.forward * largo; // final del haz
            c[i * 2] = new Color(color.r, color.g, color.b, 0.045f); // muy suave: solo se insinúa el haz
            c[i * 2 + 1] = new Color(color.r, color.g, color.b, 0f);
            uv[i * 2] = new Vector2(i / (float)lados, 0f);
            uv[i * 2 + 1] = new Vector2(i / (float)lados, 1f);
        }
        for (int i = 0; i < lados; i++)
        {
            int a = i * 2, b = a + 1, cc = a + 2, d = a + 3, t = i * 6;
            tri[t] = a; tri[t + 1] = cc; tri[t + 2] = b;
            tri[t + 3] = b; tri[t + 4] = cc; tri[t + 5] = d;
        }
        Mesh m = new Mesh { name = "HazLinterna", vertices = v, colors = c, uv = uv, triangles = tri };
        m.RecalculateBounds();

        GameObject go = new GameObject("Haz");
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.sharedMaterial = materialHaz != null ? materialHaz : MaterialAditivo();
        return go;
    }

    // Material transparente que SUMA luz (se ve como un haz brillante en el agua)
    public static Material MaterialAditivo()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material m = new Material(s != null ? s : Shader.Find("Sprites/Default"));
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 2f); // aditivo
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_Cull", 0f);  // se ve desde cualquier lado
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.EnableKeyword("_BLENDMODE_ADD");
        m.renderQueue = 3000;
        m.SetColor("_BaseColor", Color.white);
        return m;
    }
}
