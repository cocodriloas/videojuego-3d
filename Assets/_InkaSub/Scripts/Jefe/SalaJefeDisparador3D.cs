using System.Collections;
using UnityEngine;

// Entrada a la sala del jefe (igual que SalaJefeDisparador del 2D). Cuando el submarino la cruza
// (y ya habló con el mero): se cierra el muro de escape, suena la música del jefe, la cámara se
// aleja para mostrar toda la sala (ZonaCamara con punto fijo) y aparece el Iceberg de Basura.
// Si el submarino pierde durante la pelea, todo se reinicia para poder intentarlo otra vez.
// Necesita un Box Collider con "Is Trigger".
public class SalaJefeDisparador3D : MonoBehaviour
{
    public JefeIceberg3D jefe;
    public Barrera3D muroEscape;
    [Tooltip("Hay que haber hablado con él antes de entrar (opcional)")]
    public NPCDialogo3D meroGigante;
    public string mensajeSiFaltaHablar = "Primero habla con el mero gigante (E)";
    public string textoAviso = "¡EL ICEBERG DE BASURA!";
    [Tooltip("Segundos entre que se cierra el muro y aparece el jefe (la cámara se aleja)")]
    public float esperaAntesDelJefe = 2f;

    private bool activado;
    private float ultimoAviso = -10f;

    void OnTriggerEnter(Collider otro) { Intentar(otro); }
    void OnTriggerStay(Collider otro) { Intentar(otro); }

    void Intentar(Collider otro)
    {
        if (activado || !otro.CompareTag("Player")) return;
        if (meroGigante != null && !meroGigante.YaHablo)
        {
            if (Time.time - ultimoAviso > 2.5f) { ultimoAviso = Time.time; MensajePantalla.Mostrar(mensajeSiFaltaHablar); }
            return;
        }
        activado = true;
        StartCoroutine(Empezar());
    }

    IEnumerator Empezar()
    {
        if (muroEscape != null) muroEscape.Cerrar();
        MensajePantalla.Mostrar(textoAviso);
        AudioManager.IniciarMusicaJefe();
        yield return new WaitForSeconds(esperaAntesDelJefe);
        if (jefe != null) jefe.IniciarPelea();
    }

    void Update()
    {
        // El submarino perdió durante la pelea: reaparece en el cangrejo (afuera de la sala).
        // Se reinicia todo para que pueda volver a entrar.
        if (activado && jefe != null && !jefe.EstaMuerto && SubmarinoSalud3D.instancia != null && SubmarinoSalud3D.instancia.EstaDerrotado)
        {
            StopAllCoroutines();
            jefe.Reiniciar();
            if (muroEscape != null) muroEscape.Abrir();
            AudioManager.TerminarMusicaJefe();
            foreach (AtaqueQueDania a in FindObjectsByType<AtaqueQueDania>(FindObjectsSortMode.None)) Destroy(a.gameObject);
            foreach (ProyectilEnemigo3D p in FindObjectsByType<ProyectilEnemigo3D>(FindObjectsSortMode.None)) Destroy(p.gameObject);
            activado = false;
        }
    }
}
