using UnityEngine;
using UnityEngine.InputSystem;

// Escudo de Burbujas: tecla 3. Durante unos segundos una burbuja rodea al submarino y absorbe
// TODO el daño (balas, rocas, tentáculos, monstruos). Después se recarga.
// Se consigue al hablar con el Mero (justo antes del jefe final).
public class EscudoSubmarino : MonoBehaviour
{
    public bool desbloqueado = false;
    [Tooltip("Se desbloquea solo al terminar de hablar con este animal")]
    public string npcQueLoDa = "Mero";
    public float duracion = 4f;
    public float recarga = 14f;
    [Tooltip("Material aditivo con la textura de burbuja (un aro): la burbuja del escudo")]
    public Material materialBurbuja;

    private float activoRestante, recargaRestante;
    private Transform burbuja;
    private Renderer rendBurbuja;
    private Light luz;
    private MaterialPropertyBlock bloque;
    private float destello;
    private NPCDialogo3D npc;

    public bool Activo => activoRestante > 0f;
    public bool Listo => desbloqueado && !Activo && recargaRestante <= 0f;
    // 1 = lleno; mientras está activo baja, mientras se recarga sube (para el HUD)
    public float Progreso => Activo ? activoRestante / duracion : 1f - Mathf.Clamp01(recargaRestante / Mathf.Max(0.01f, recarga));

    void Start()
    {
        bloque = new MaterialPropertyBlock();
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Burbuja escudo";
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(transform, false);
        burbuja = q.transform;
        rendBurbuja = q.GetComponent<Renderer>();
        if (materialBurbuja != null) rendBurbuja.sharedMaterial = materialBurbuja;
        rendBurbuja.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        GameObject l = new GameObject("Luz escudo");
        l.transform.SetParent(transform, false);
        luz = l.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(0.4f, 0.9f, 1f);
        luz.range = 5f;
        luz.renderingLayerMask = CapasLuz.Todas;
        Mostrar(false);
    }

    void Update()
    {
        // Se consigue al hablar con el Mero
        if (!desbloqueado)
        {
            if (npc == null) foreach (NPCDialogo3D n in FindObjectsByType<NPCDialogo3D>(FindObjectsSortMode.None)) if (n.nombreNPC == npcQueLoDa) npc = n;
            if (npc != null && npc.YaHablo)
            {
                desbloqueado = true;
                AudioManager.Sonar(Sonido.Desbloqueo);
                HUDSubmarino.Anunciar(HUDSubmarino.PoderNuevo.Escudo);   // ícono al centro, explicación y vuela al HUD
            }
        }

        if (recargaRestante > 0f) recargaRestante -= Time.deltaTime;
        if (activoRestante > 0f)
        {
            activoRestante -= Time.deltaTime;
            if (activoRestante <= 0f) { recargaRestante = recarga; Mostrar(false); }
        }

        Keyboard t = Keyboard.current;
        if (t != null && Time.timeScale > 0f && t.digit3Key.wasPressedThisFrame)
        {
            if (Listo) Activar();
            else if (!desbloqueado) MensajePantalla.Mostrar("El escudo te lo da el Mero (habla con él)");
        }
        if (Activo) Animar();
    }

    void Activar()
    {
        activoRestante = duracion;
        AudioManager.Sonar(Sonido.Recarga);
        Mostrar(true);
    }

    // Lo llama SubmarinoSalud3D antes de quitar vida: si el escudo está activo, el golpe no hace nada
    public bool Absorber()
    {
        if (!Activo) return false;
        destello = 1f;
        AudioManager.Sonar(Sonido.EnemigoGolpeado);
        return true;
    }

    void Animar()
    {
        Camera cam = Camera.main;
        if (cam != null) burbuja.rotation = cam.transform.rotation;
        burbuja.position = transform.position + (cam != null ? (cam.transform.position - transform.position).normalized * 0.3f : Vector3.zero);
        destello = Mathf.MoveTowards(destello, 0f, Time.deltaTime * 3f);
        float k = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
        // Los últimos segundos parpadea: se está por acabar
        bool parpadeo = activoRestante < 1f && Mathf.Repeat(Time.time, 0.2f) < 0.1f;
        burbuja.localScale = Vector3.one * (3.2f + k * 0.15f + destello * 0.6f) / Mathf.Max(0.001f, transform.lossyScale.x);
        rendBurbuja.GetPropertyBlock(bloque);
        bloque.SetColor("_BaseColor", new Color(0.45f, 0.95f, 1f, parpadeo ? 0.2f : 0.65f + destello * 0.35f));
        rendBurbuja.SetPropertyBlock(bloque);
        luz.intensity = 2f + k + destello * 4f;
    }

    void Mostrar(bool si)
    {
        if (rendBurbuja != null) rendBurbuja.enabled = si;
        if (luz != null) luz.enabled = si;
    }
}
