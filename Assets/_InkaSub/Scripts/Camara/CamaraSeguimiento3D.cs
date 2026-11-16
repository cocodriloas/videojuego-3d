using UnityEngine;

// Cámara que sigue al submarino y cambia sola según la ZonaCamara en la que esté:
//   - Lateral: de costado, como Crash bajo el agua.
//   - Profundidad: detrás del submarino, mirando hacia el fondo.
// Los cambios entre zonas son suaves porque la posición y el giro se interpolan.
public class CamaraSeguimiento3D : MonoBehaviour
{
    [Header("A quién seguir")]
    public Transform objetivo; // el submarino

    [Header("Valores si el submarino no está en ninguna zona")]
    public float distanciaPorDefecto = 12f;
    public float alturaPorDefecto = 2f;
    public float adelantoPorDefecto = 2f;

    [Header("Suavizado")]
    [Tooltip("Segundos que tarda en alcanzar al submarino (más bajo = más pegada)")]
    public float suavizadoPosicion = 0.12f;
    [Tooltip("Qué tan rápido gira al cambiar de zona")]
    public float velocidadGiro = 4f;

    [Header("Mirar hacia donde apunta el submarino")]
    [Tooltip("Cuánto se desplaza la cámara hacia el lado al que mira el submarino (solo en modo Lateral)")]
    public float desplazamientoMirada = 2f;

    private Vector3 velocidadActual;
    private float ladoSuave = 1f;
    private SubmarinoApuntar3D apuntado;

    void Awake() { CentroPelea = null; }   // al cargar la escena no queda una pelea vieja

    void Start()
    {
        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null) objetivo = jugador.transform;
        }
        if (objetivo != null)
        {
            apuntado = objetivo.GetComponent<SubmarinoApuntar3D>();
            // Empezar ya en su sitio, sin "viajar" desde donde estaba la cámara
            CalcularDestino(out Vector3 pos, out Quaternion rot);
            transform.SetPositionAndRotation(pos, rot);
        }
    }

    [Header("Cambio de zona")]
    [Tooltip("Segundos que dura el paso de una vista a otra (por ejemplo, de costado a detrás)")]
    public float duracionTransicion = 0.7f;

    private ZonaCamara zonaAnterior;
    private float inicioTransicion = -10f;
    private float duracionActual = 0.7f;
    private Vector3 posicionInicioTransicion;
    private Quaternion rotacionInicioTransicion;

    void LateUpdate()
    {
        if (objetivo == null) return;

        CalcularDestino(out Vector3 posicionDeseada, out Quaternion rotacionDeseada);

        // Al cambiar de zona: transición de duración fija con curva suave (empieza y termina lento).
        // Así siempre se siente igual, en vez de un giro largo e irregular.
        if (!tieneBase) { posicionBase = transform.position; tieneBase = true; }

        if (ZonaCamara.Actual != zonaAnterior)
        {
            if (zonaAnterior != null)
            {
                // Entrar o salir de una zona con duración propia (la cámara fija del jefe viaja lejos)
                ZonaCamara z = ZonaCamara.Actual != null && ZonaCamara.Actual.duracionTransicion > 0f ? ZonaCamara.Actual : zonaAnterior;
                duracionActual = z.duracionTransicion > 0f ? z.duracionTransicion : duracionTransicion;
                inicioTransicion = Time.time;
                // Corte: la cámara salta (si viajara, atravesaría rocas) y la pantalla se funde un instante
                if ((ZonaCamara.Actual != null && ZonaCamara.Actual.corteDirecto) || zonaAnterior.corteDirecto)
                {
                    duracionActual = 0.01f;
                    Fundido();
                }
                posicionInicioTransicion = posicionBase;
                rotacionInicioTransicion = transform.rotation;
            }
            zonaAnterior = ZonaCamara.Actual;
        }

        float p = (Time.time - inicioTransicion) / Mathf.Max(0.01f, duracionActual);
        if (p < 1f)
        {
            float k = Mathf.SmoothStep(0f, 1f, p);
            posicionBase = Vector3.Lerp(posicionInicioTransicion, posicionDeseada, k);
            transform.rotation = Quaternion.Slerp(rotacionInicioTransicion, rotacionDeseada, k);
            velocidadActual = Vector3.zero;
        }
        else
        {
            posicionBase = Vector3.SmoothDamp(posicionBase, posicionDeseada, ref velocidadActual, suavizadoPosicion);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, velocidadGiro * 2f * Time.deltaTime);
        }

        AjustarNiebla();

        // La sacudida (al recibir daño) se suma encima, sin tocar la posición "base":
        // así no ensucia el suavizado y también funciona durante los cambios de zona
        Vector3 sacudida = Time.time < finSacudida
            ? Random.insideUnitSphere * fuerzaSacudida * ((finSacudida - Time.time) / duracionSacudida)
            : Vector3.zero;
        transform.position = NoAtravesar(posicionBase + sacudida);
    }

    // En los túneles (cámara detrás) la cámara no puede quedar dentro de la roca: si el techo o una
    // pared se mete entre el submarino y la cámara, la cámara se acerca hasta justo delante.
    // (De costado no: ahí la cámara está afuera del nivel a propósito, mirando el plano.)
    Vector3 NoAtravesar(Vector3 deseada)
    {
        if (objetivo == null || ZonaCamara.ModoActual != ModoZona.Profundidad) return deseada;
        Vector3 origen = objetivo.position + Vector3.up * 0.3f;
        Vector3 hacia = deseada - origen;
        float largo = hacia.magnitude;
        if (largo < 0.01f) return deseada;
        Vector3 dir = hacia / largo;
        float mas = largo + 0.4f, cerca = mas;
        foreach (RaycastHit h in Physics.SphereCastAll(origen, 0.3f, dir, mas, ~0, QueryTriggerInteraction.Ignore))
        {
            Collider c = h.collider;
            if (c.transform.IsChildOf(objetivo) || c.CompareTag("Enemigo") || c.CompareTag("Player")) continue;
            if (c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic) continue;   // trozos que caen, botellas
            if (h.distance > 0f && h.distance < cerca) cerca = h.distance;
        }
        if (cerca >= mas) return deseada;
        return origen + dir * Mathf.Max(0.6f, cerca - 0.35f);
    }

    // Pantalla negra que se aclara en 0.35 s (tapa el salto de la cámara en los cortes)
    private UnityEngine.UI.Image cortina;
    private Coroutine rutinaFundido;
    void Fundido()
    {
        if (cortina == null)
        {
            GameObject c = new GameObject("Cortina de corte");
            Canvas canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;   // encima del HUD, debajo de los menús
            GameObject img = new GameObject("Negro", typeof(RectTransform));
            img.transform.SetParent(c.transform, false);
            RectTransform rt = img.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            cortina = img.AddComponent<UnityEngine.UI.Image>();
            cortina.raycastTarget = false;
        }
        if (rutinaFundido != null) StopCoroutine(rutinaFundido);
        rutinaFundido = StartCoroutine(Aclarar());
    }

    System.Collections.IEnumerator Aclarar()
    {
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            cortina.color = new Color(0f, 0.02f, 0.05f, 1f - t / 0.35f);
            yield return null;
        }
        cortina.color = Color.clear;
        rutinaFundido = null;
    }

    // Cada zona puede tener su propia niebla (por ejemplo, en el túnel se ve más lejos para
    // distinguir a los enemigos): se pasa de una a otra suavemente
    private float nieblaInicioNivel = -1f, nieblaFinNivel, solNivel;
    private Color nieblaNivel, ambienteNivel, fondoNivel;
    private Light sol;
    private Camera cam;
    void AjustarNiebla()
    {
        if (nieblaInicioNivel < 0f)
        {
            nieblaInicioNivel = RenderSettings.fogStartDistance; nieblaFinNivel = RenderSettings.fogEndDistance;
            nieblaNivel = RenderSettings.fogColor; ambienteNivel = RenderSettings.ambientLight;
            cam = GetComponent<Camera>();
            if (cam != null) fondoNivel = cam.backgroundColor;
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sol = l; break; }
            if (sol != null) solNivel = sol.intensity;
        }
        ZonaCamara z = ZonaCamara.Actual;
        float ini = z != null && z.nieblaFin > 0f ? z.nieblaInicio : nieblaInicioNivel;
        float fin = z != null && z.nieblaFin > 0f ? z.nieblaFin : nieblaFinNivel;
        bool propio = z != null && z.ambientePropio;
        float k = 1f - Mathf.Exp(-Time.deltaTime * 2f);
        RenderSettings.fogStartDistance = Mathf.Lerp(RenderSettings.fogStartDistance, ini, k);
        RenderSettings.fogEndDistance = Mathf.Lerp(RenderSettings.fogEndDistance, fin, k);
        // Color del agua, luz ambiente y sol (la sala del jefe es más oscura)
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, propio ? z.colorNiebla : nieblaNivel, k);
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, propio ? z.colorAmbiente : ambienteNivel, k);
        if (cam != null) cam.backgroundColor = Color.Lerp(cam.backgroundColor, propio ? z.colorNiebla : fondoNivel, k);
        if (sol != null) sol.intensity = Mathf.Lerp(sol.intensity, propio ? solNivel * z.intensidadSol : solNivel, k);
    }

    // Pone la cámara directo en su sitio, sin transición (al cargar una partida)
    public void Saltar()
    {
        if (objetivo == null) return;
        CalcularDestino(out Vector3 pos, out Quaternion rot);
        transform.SetPositionAndRotation(pos, rot);
        posicionBase = pos;
        tieneBase = true;
        velocidadActual = Vector3.zero;
        zonaAnterior = ZonaCamara.Actual;
        inicioTransicion = -10f;
    }

    private Vector3 posicionBase;
    private bool tieneBase;
    private float finSacudida, fuerzaSacudida, duracionSacudida = 1f;

    public void Sacudir(float fuerza, float duracion)
    {
        fuerzaSacudida = fuerza;
        duracionSacudida = Mathf.Max(0.01f, duracion);
        finSacudida = Time.time + duracionSacudida;
    }

    // Oleadas: la cámara se aleja y mira un poco hacia el centro de la sala, para ver a todos los enemigos.
    // Lo pide GeneradorOleadas3D mientras pelea (null = nada).
    public static Vector3? CentroPelea;
    public static float DistanciaExtraPelea;
    private float extraSuave, pesoPelea, alturaTunel = float.NaN;
    private Vector3 centroPeleaSuave;

    void CalcularDestino(out Vector3 posicion, out Quaternion rotacion)
    {
        ZonaCamara zona = ZonaCamara.Actual;
        float distancia = zona != null ? zona.distancia : distanciaPorDefecto;
        float altura = zona != null ? zona.altura : alturaPorDefecto;
        bool pelea = CentroPelea.HasValue && ZonaCamara.ModoActual == ModoZona.Lateral && (zona == null || zona.centroFijo == null);
        extraSuave = Mathf.MoveTowards(extraSuave, pelea ? DistanciaExtraPelea : 0f, Time.deltaTime * 4f);
        pesoPelea = Mathf.MoveTowards(pesoPelea, pelea ? 0.35f : 0f, Time.deltaTime * 0.6f);
        if (CentroPelea.HasValue) centroPeleaSuave = CentroPelea.Value;
        distancia += extraSuave;
        float adelanto = zona != null ? zona.adelanto : adelantoPorDefecto;

        Vector3 derecha = ZonaCamara.DerechaActual;
        Vector3 fondo = ZonaCamara.FondoActual;
        Vector3 centro = objetivo.position;

        // Zona con cámara FIJA (la sala del jefe): no sigue al submarino, muestra toda la sala
        if (zona != null && zona.centroFijo != null)
        {
            Vector3 c = zona.centroFijo.position;
            posicion = c - fondo * distancia + Vector3.up * altura;
            rotacion = Quaternion.LookRotation(c - posicion, Vector3.up);
            return;
        }

        if (ZonaCamara.ModoActual == ModoZona.Lateral)
        {
            // La cámara se adelanta un poco hacia el lado al que mira el submarino
            float lado = apuntado != null ? Mathf.Sign(Vector3.Dot(apuntado.DireccionApuntada, derecha)) : 1f;
            ladoSuave = Mathf.MoveTowards(ladoSuave, lado, Time.deltaTime * 2f);
            Vector3 foco = centro + derecha * (adelanto + desplazamientoMirada * ladoSuave);
            // En plena oleada el encuadre se corre hacia el centro de la sala (sin dejar de seguir al submarino)
            if (pesoPelea > 0f)
            {
                Vector3 haciaSala = centroPeleaSuave - foco;
                haciaSala -= fondo * Vector3.Dot(haciaSala, fondo);
                foco += haciaSala * pesoPelea;
            }

            posicion = foco - fondo * distancia + Vector3.up * altura;
            rotacion = Quaternion.LookRotation(foco - posicion + Vector3.up * -altura * 0.5f, Vector3.up);
        }
        else
        {
            // Detrás del submarino, mirando hacia el fondo de la zona.
            // Si el techo del túnel está cerca (el submarino subió mucho), la cámara BAJA en vez de
            // pegarse al submarino: antes quedaba a menos de 1 m y las burbujas tapaban la pantalla
            float libre = altura + 1f;
            foreach (RaycastHit h in Physics.SphereCastAll(centro - fondo * distancia * 0.5f, 0.3f, Vector3.up, altura + 1f, ~0, QueryTriggerInteraction.Ignore))
            {
                Collider cl = h.collider;
                if (cl.transform.IsChildOf(objetivo) || cl.CompareTag("Enemigo") || cl.CompareTag("Player")) continue;
                if (cl.attachedRigidbody != null && !cl.attachedRigidbody.isKinematic) continue;
                if (h.distance > 0f) libre = Mathf.Min(libre, h.distance);
            }
            float alturaDeseada = Mathf.Clamp(libre - 0.8f, -1.2f, altura);
            if (float.IsNaN(alturaTunel)) alturaTunel = alturaDeseada;
            alturaTunel = Mathf.MoveTowards(alturaTunel, alturaDeseada, Time.deltaTime * 6f);
            Vector3 foco = centro + fondo * adelanto;
            posicion = centro - fondo * distancia + Vector3.up * alturaTunel;
            rotacion = Quaternion.LookRotation(foco - posicion, Vector3.up);
        }
    }
}
