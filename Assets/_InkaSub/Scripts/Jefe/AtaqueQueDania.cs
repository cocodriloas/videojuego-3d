using UnityEngine;

// Algo del jefe que daña al submarino al tocarlo (bolas de basura, tentáculo).
// Las bolas caen, giran y se deshacen en burbujas al llegar al suelo.
public class AtaqueQueDania : MonoBehaviour
{
    public int danio = 1;
    public Vector3 velocidad;
    public Vector3 giro;
    public float alturaSuelo = -999f;
    public bool burbujasAlTocarSuelo;
    public bool destruirAlGolpear = true;

    void Update()
    {
        if (velocidad != Vector3.zero) transform.position += velocidad * Time.deltaTime;
        if (giro != Vector3.zero) transform.Rotate(giro * Time.deltaTime, Space.World);
        if (transform.position.y < alturaSuelo)
        {
            if (burbujasAlTocarSuelo) JefeAtaques3D.Burbujas(transform.position, 12, 0.6f);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider otro)
    {
        if (!otro.CompareTag("Player")) return;
        SubmarinoSalud3D salud = otro.GetComponent<SubmarinoSalud3D>();
        if (salud != null) salud.RecibirDaño(danio);
        if (destruirAlGolpear) Destroy(gameObject);
    }
}
