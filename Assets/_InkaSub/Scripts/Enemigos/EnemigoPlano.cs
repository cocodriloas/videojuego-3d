using UnityEngine;

// Ayudante que usan todos los enemigos para saber si están en una zona Lateral
// (todo pasa sobre un plano, como en 2D) o en una zona de Profundidad (3D libre).
// Se fija en la zona donde está EL ENEMIGO, no solo en la del submarino, porque un enemigo
// puede estar esperando en una zona a la que el jugador todavía no ha llegado.
public static class EnemigoPlano
{
    private static ZonaCamara[] zonas;

    // Devuelve la ZonaCamara que contiene el punto (la de mayor prioridad).
    // Si el punto no está en ninguna zona, usa la zona actual del submarino.
    public static ZonaCamara ZonaEn(Vector3 punto)
    {
        // Buscamos las zonas una sola vez (es caro) y de nuevo solo si alguna se borró
        if (zonas == null || HayZonaBorrada())
            zonas = Object.FindObjectsByType<ZonaCamara>(FindObjectsSortMode.None);

        ZonaCamara mejor = null;
        foreach (ZonaCamara z in zonas)
        {
            if (z == null || !z.isActiveAndEnabled || !z.Contiene(punto)) continue;
            if (mejor == null || z.prioridad > mejor.prioridad) mejor = z;
        }
        return mejor != null ? mejor : ZonaCamara.Actual;
    }

    public static bool EsLateral(ZonaCamara zona)
    {
        return zona != null ? zona.modo == ModoZona.Lateral : ZonaCamara.ModoActual == ModoZona.Lateral;
    }

    // Normal del plano (la dirección "hacia la cámara / hacia el fondo")
    public static Vector3 Normal(ZonaCamara zona)
    {
        return zona != null ? zona.Fondo : ZonaCamara.FondoActual;
    }

    public static Vector3 PuntoPlano(ZonaCamara zona)
    {
        return zona != null ? zona.PuntoPlano : ZonaCamara.PuntoPlanoActual;
    }

    // Quita de una dirección la parte que la sacaría del plano
    public static Vector3 Aplanar(Vector3 direccion, ZonaCamara zona)
    {
        Vector3 normal = Normal(zona);
        return direccion - normal * Vector3.Dot(direccion, normal);
    }

    // Qué tan lejos está un punto del plano (con signo)
    public static float DistanciaAlPlano(Vector3 punto, ZonaCamara zona)
    {
        return Vector3.Dot(punto - PuntoPlano(zona), Normal(zona));
    }

    static bool HayZonaBorrada()
    {
        foreach (ZonaCamara z in zonas) if (z == null) return true;
        return false;
    }
}
