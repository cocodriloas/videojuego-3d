using UnityEngine;
using UnityEngine.InputSystem;

// Solo para la escena de prueba (Prueba3D): permite probar la vida y el daño del
// submarino con el teclado, sin necesitar enemigos.
//   K = recibir daño   |   H = curar 1   |   J = vida completa
// Ponlo en un objeto vacío de la escena (por ejemplo "PruebasVida").
public class PruebaVidaSubmarino : MonoBehaviour
{
    [Tooltip("Daño que se aplica al presionar K")]
    public int danioPrueba = 1;
    [Tooltip("Vida que se recupera al presionar H")]
    public int curaPrueba = 1;
    [Tooltip("Mostrar en la consola la vida después de cada prueba")]
    public bool mostrarEnConsola = true;

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        SubmarinoSalud3D salud = SubmarinoSalud3D.instancia;
        if (teclado == null || salud == null) return;

        if (teclado.kKey.wasPressedThisFrame)
        {
            salud.RecibirDaño(danioPrueba);
            Informar("Daño de prueba");
        }
        if (teclado.hKey.wasPressedThisFrame)
        {
            salud.Curar(curaPrueba);
            Informar("Curación de prueba");
        }
        if (teclado.jKey.wasPressedThisFrame)
        {
            salud.CurarCompleto();
            Informar("Vida completa");
        }
    }

    void Informar(string accion)
    {
        if (!mostrarEnConsola) return;
        SubmarinoSalud3D salud = SubmarinoSalud3D.instancia;
        Debug.Log($"[Prueba] {accion} -> vida {salud.VidaActual}/{salud.vidaMaxima}");
    }
}
