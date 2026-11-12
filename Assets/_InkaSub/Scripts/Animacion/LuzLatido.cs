using UnityEngine;

// Luz que late despacio (las luces rojas y violetas de la sala del jefe: el lugar "respira").
public class LuzLatido : MonoBehaviour
{
    public float minimo = 1f, maximo = 3.5f, velocidad = 1.2f;
    private Light luz;
    private float desfase;

    void Start()
    {
        luz = GetComponent<Light>();
        desfase = Random.value * 10f;
    }

    void Update()
    {
        if (luz != null) luz.intensity = Mathf.Lerp(minimo, maximo, (Mathf.Sin(Time.time * velocidad + desfase) + 1f) * 0.5f);
    }
}
