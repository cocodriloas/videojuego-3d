using UnityEngine;

// Gira el objeto sin parar (grados por segundo en cada eje). Lo usan los microplásticos que disparan los enemigos.
public class Girar : MonoBehaviour
{
    public Vector3 velocidad = new Vector3(0f, 90f, 0f);

    void Update()
    {
        transform.Rotate(velocidad * Time.deltaTime, Space.Self);
    }
}
