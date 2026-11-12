using UnityEngine;

// Cangrejo peludo del punto de control, como en el juego 2D: mientras espera camina un poco
// de lado; cuando el submarino se acerca, BRILLA, da un saltito y LEVANTA LAS ANTENAS.
// Las antenas son piezas aparte (el modelo de Meshy es de una sola pieza) y se crean solas.
public class CangrejoAnimado : MonoBehaviour
{
    [Tooltip("El modelo del cangrejo (hijo). Si está vacío usa el primer hijo con Renderer")]
    public Transform modelo;
    [Tooltip("Altura de las antenas sobre el cangrejo y separación entre ellas")]
    public Vector3 baseAntenas = new Vector3(0.25f, 0.9f, 0.45f);
    public Color colorBrillo = new Color(1f, 0.85f, 0.3f);

    [Header("Esperando")]
    public float pasoLateral = 0.3f;
    public float velocidadPaso = 1.5f;

    private Transform antenaIzq, antenaDer;
    private Light luz;
    private Vector3 posicionModelo;
    private bool activado;
    private float tiempoActivado, desfase;

    void Start()
    {
        if (modelo == null)
            foreach (Transform hijo in transform)
                if (hijo.GetComponentInChildren<Renderer>() != null) { modelo = hijo; break; }
        if (modelo == null) modelo = transform;
        posicionModelo = modelo.localPosition;
        desfase = Random.value * 5f;

        antenaIzq = CrearAntena(-1);
        antenaDer = CrearAntena(1);

        GameObject l = new GameObject("Brillo");
        l.transform.SetParent(transform, false);
        l.transform.localPosition = Vector3.up * 1.5f;
        luz = l.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = colorBrillo;
        luz.range = 7f;
        luz.intensity = 0f;
    }

    public void Activar()
    {
        if (activado) return;
        activado = true;
        tiempoActivado = Time.time;
    }

    void Update()
    {
        float t = Time.time + desfase;

        if (!activado)
        {
            // Camina de lado, como los cangrejos
            modelo.localPosition = posicionModelo + Vector3.right * Mathf.Sin(t * velocidadPaso) * pasoLateral
                                   + Vector3.up * Mathf.Abs(Mathf.Sin(t * velocidadPaso * 2f)) * 0.05f;
            float caida = 20f + Mathf.Sin(t * 3f) * 8f; // antenas medio caídas, moviéndose un poco
            antenaIzq.localRotation = Quaternion.Euler(caida, 0f, -15f);
            antenaDer.localRotation = Quaternion.Euler(caida, 0f, 15f);
            return;
        }

        float desde = Time.time - tiempoActivado;
        // Saltito al activarse
        float salto = desde < 0.5f ? Mathf.Sin(desde / 0.5f * Mathf.PI) * 0.6f : 0f;
        modelo.localPosition = posicionModelo + Vector3.up * salto;
        // Antenas levantadas y contentas
        float alegria = Mathf.Sin(t * 8f) * 6f;
        float subir = Mathf.Lerp(20f, -25f, Mathf.Clamp01(desde / 0.3f));
        antenaIzq.localRotation = Quaternion.Euler(subir, 0f, -8f + alegria);
        antenaDer.localRotation = Quaternion.Euler(subir, 0f, 8f - alegria);
        // Brilla fuerte al principio y luego queda un brillo suave que late
        luz.intensity = desde < 0.6f ? Mathf.Lerp(8f, 3f, desde / 0.6f) : 2.5f + Mathf.Sin(t * 3f) * 0.8f;
    }

    Transform CrearAntena(int lado)
    {
        Transform raiz = new GameObject(lado < 0 ? "Antena izquierda" : "Antena derecha").transform;
        raiz.SetParent(modelo, false);
        raiz.localPosition = new Vector3(baseAntenas.x * lado, baseAntenas.y, baseAntenas.z);

        Material tallo = Mat(new Color(0.55f, 0.2f, 0.12f), Color.black);
        Material punta = Mat(colorBrillo, colorBrillo * 2f);

        GameObject palo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(palo.GetComponent<Collider>());
        palo.transform.SetParent(raiz, false);
        palo.transform.localPosition = Vector3.up * 0.35f;
        palo.transform.localScale = new Vector3(0.05f, 0.35f, 0.05f);
        palo.GetComponent<Renderer>().sharedMaterial = tallo;

        GameObject bolita = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(bolita.GetComponent<Collider>());
        bolita.transform.SetParent(raiz, false);
        bolita.transform.localPosition = Vector3.up * 0.72f;
        bolita.transform.localScale = Vector3.one * 0.14f;
        bolita.GetComponent<Renderer>().sharedMaterial = punta;
        return raiz;
    }

    static Material Mat(Color c, Color emision)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        Material m = new Material(s != null ? s : Shader.Find("Standard"));
        m.SetColor("_BaseColor", c);
        if (emision.maxColorComponent > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emision); }
        return m;
    }
}
