using UnityEngine;

// Rayo de luz que baja desde la superficie: un plano transparente que se mece y "respira".
public class RayoDeLuz : MonoBehaviour
{
    public float balanceo = 4f;
    public float velocidad = 0.3f;
    private Quaternion inicio;
    private Renderer rend;
    private float desfase;
    private MaterialPropertyBlock bloque;

    void Start()
    {
        inicio = transform.rotation;
        rend = GetComponentInChildren<Renderer>();
        desfase = Random.value * 10f;
        bloque = new MaterialPropertyBlock();
    }

    void Update()
    {
        float t = Time.time * velocidad + desfase;
        transform.rotation = inicio * Quaternion.Euler(0f, 0f, Mathf.Sin(t) * balanceo);
        if (rend != null)
        {
            rend.GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", new Color(0.7f, 0.9f, 1f, 0.10f + Mathf.Sin(t * 1.7f) * 0.04f));
            rend.SetPropertyBlock(bloque);
        }
    }
}
