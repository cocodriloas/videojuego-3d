using UnityEngine;

// Va en un objeto vacío del nivel ("Progreso"). Al empezar, si hay una partida guardada:
//   - pone el submarino (y la cámara, sin viajar) en el último cangrejo,
//   - devuelve las bacterias, el dash y el Disparo Doble,
//   - quita las bacterias que ya recogiste y los enemigos que ya venciste,
//   - recuerda con qué animales hablaste (sus muros quedan abiertos),
//   - deja abiertas las salas de oleadas que ya despejaste.
// Se ejecuta DESPUÉS de los demás scripts (la cámara y la ficha de datos ya están listas).
[DefaultExecutionOrder(100)]
public class RestaurarProgreso3D : MonoBehaviour
{
    void Start()
    {
        ProgresoJuego3D.Datos d = ProgresoJuego3D.Cargar();
        if (d == null) return;

        PuntoControl3D punto = null;
        foreach (PuntoControl3D p in FindObjectsByType<PuntoControl3D>(FindObjectsSortMode.None))
            if (p.name == d.puntoControl) punto = p;
        if (punto == null)
        {
            // El nivel cambió (por ejemplo, se volvió a construir distinto): se empieza de cero
            Debug.LogWarning("Partida guardada en un cangrejo que ya no existe (" + d.puntoControl + "). Se empieza de cero.");
            return;
        }
        ProgresoJuego3D.Sembrar(d);

        // 1) Habilidades y bacterias
        Recolectable3D.FijarConteo(d.bacterias);
        foreach (Recolectable3D r in FindObjectsByType<Recolectable3D>(FindObjectsSortMode.None))
            if (d.bacteriasRecogidas.Contains(r.Id)) Destroy(r.gameObject);
        FichaCientifica.ActualizarContador(d.bacterias);

        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador != null)
        {
            SubmarinoMovimiento3D mov = jugador.GetComponent<SubmarinoMovimiento3D>();
            SubmarinoDisparo3D disp = jugador.GetComponent<SubmarinoDisparo3D>();
            if (mov != null) mov.dashDesbloqueado = d.dash;
            if (disp != null) disp.habilidadDesbloqueada = d.dobleDisparo;
        }

        // 2) Animales con los que ya hablaste: sus muros se abren sin animación
        foreach (NPCDialogo3D npc in FindObjectsByType<NPCDialogo3D>(FindObjectsSortMode.None))
            if (d.npcsHablados.Contains(npc.nombreNPC)) npc.MarcarHablado();
        foreach (Barrera3D b in FindObjectsByType<Barrera3D>(FindObjectsSortMode.None))
            if (b.npcRequerido != null && b.npcRequerido.YaHablo) b.AbrirAlInstante();

        // 3) Salas de oleadas despejadas y enemigos vencidos
        foreach (GeneradorOleadas3D g in FindObjectsByType<GeneradorOleadas3D>(FindObjectsSortMode.None))
            if (d.oleadasTerminadas.Contains(g.Id)) g.MarcarTerminado();
        foreach (EnemigoSalud3D e in FindObjectsByType<EnemigoSalud3D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (d.enemigosVencidos.Contains(e.Id)) Destroy(e.gameObject);

        // 4) El cangrejo queda activado (sin mensaje) y el submarino aparece a su lado
        punto.Restaurar();
        if (jugador != null)
        {
            Vector3 destino = punto.PosicionReaparicion;
            Rigidbody rb = jugador.GetComponent<Rigidbody>();
            if (rb != null) { rb.position = destino; rb.linearVelocity = Vector3.zero; }
            jugador.transform.position = destino;
            ZonaCamara.Actualizar(destino);
            CamaraSeguimiento3D cam = FindFirstObjectByType<CamaraSeguimiento3D>();
            if (cam != null) cam.Saltar();
        }
        MensajePantalla.Mostrar("PARTIDA CARGADA");
    }
}
