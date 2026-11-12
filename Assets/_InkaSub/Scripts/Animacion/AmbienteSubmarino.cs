using UnityEngine;
using UnityEngine.Rendering.Universal;

// Lo que hace que "se sienta bajo el agua":
//   - Cáusticas: la red de luz del sol que se mueve sobre el fondo (galleta de la luz del sol).
//   - Nieve marina: partículas pequeñas flotando alrededor de la cámara.
//   - Rayos de luz que bajan desde la superficie y se mecen.
// Ponlo en la cámara. La niebla y el postproceso los configura el constructor del nivel.
public class AmbienteSubmarino : MonoBehaviour
{
    [Header("Cáusticas")]
    public Light sol;
    public Texture2D texturaCausticas;
    [Tooltip("Tamaño (metros) de cada repetición del dibujo de cáusticas")]
    public float tamanioCausticas = 9f;
    public Vector2 velocidadCausticas = new Vector2(0.35f, 0.2f);

    [Header("Nieve marina")]
    public Material materialParticulas;
    public int cantidadParticulas = 350;
    public float radioParticulas = 14f;

    private UniversalAdditionalLightData datosSol;
    private ParticleSystem nieve;

    void Start()
    {
        if (sol != null && texturaCausticas != null)
        {
            sol.cookie = texturaCausticas;
            datosSol = sol.GetComponent<UniversalAdditionalLightData>();
            if (datosSol == null) datosSol = sol.gameObject.AddComponent<UniversalAdditionalLightData>();
            datosSol.lightCookieSize = Vector2.one * tamanioCausticas;
        }
        nieve = CrearNieve();
    }

    void Update()
    {
        // Las cáusticas se mueven (dos ondas sumadas para que no parezca un simple desplazamiento)
        if (datosSol != null)
        {
            float t = Time.time;
            datosSol.lightCookieOffset = velocidadCausticas * t + new Vector2(Mathf.Sin(t * 0.7f), Mathf.Cos(t * 0.5f)) * 0.6f;
        }
    }

    ParticleSystem CrearNieve()
    {
        GameObject go = new GameObject("NieveMarina");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.forward * radioParticulas * 0.6f;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 9f;
        main.startSpeed = 0.05f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.07f);
        main.startColor = new Color(0.85f, 0.95f, 1f, 0.55f);
        main.maxParticles = cantidadParticulas;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // quedan en el agua al moverse la cámara
        main.gravityModifier = 0.003f;                               // caen muy despacio

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = Vector3.one * radioParticulas * 2f;

        var emision = ps.emission;
        emision.rateOverTime = cantidadParticulas / 9f;

        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.15f;
        ruido.frequency = 0.3f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.maxParticleSize = 0.04f;   // una mota de nieve marina pegada a la cámara no tapa la pantalla
        r.sharedMaterial = materialParticulas != null ? materialParticulas : LinternaSubmarino.MaterialAditivo();
        ps.Play();
        return ps;
    }
}
