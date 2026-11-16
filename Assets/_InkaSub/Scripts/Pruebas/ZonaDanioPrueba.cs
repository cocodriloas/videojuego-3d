using UnityEngine;

// Zona de prueba que daña al submarino mientras está dentro (por ejemplo, agua tóxica).
// Uso: crea un Cubo, marca "Is Trigger" en su Collider y agrégale este script.
// Sirve para comprobar en la escena Prueba3D la invulnerabilidad, el parpadeo y la derrota.
[RequireComponent(typeof(Collider))]
public class ZonaDanioPrueba : MonoBehaviour
{
    [Tooltip("Daño que hace cada vez")]
    public int danio = 1;
    [Tooltip("Segundos entre cada golpe mientras el submarino sigue dentro")]
    public float intervalo = 1.5f;
    [Tooltip("Color de la zona en el editor")]
    public Color colorZona = new Color(1f, 0.2f, 0.2f, 0.35f);

    private float siguienteGolpe;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider otro)
    {
        siguienteGolpe = 0f; // golpe inmediato al entrar
        Dañar(otro);
    }

    void OnTriggerStay(Collider otro)
    {
        Dañar(otro);
    }

    void Dañar(Collider otro)
    {
        if (Time.time < siguienteGolpe) return;
        SubmarinoSalud3D salud = otro.GetComponentInParent<SubmarinoSalud3D>();
        if (salud == null) return;
        salud.RecibirDaño(danio);
        siguienteGolpe = Time.time + intervalo;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = colorZona;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
    }
}
