// Capas de luz (Rendering Layers de URP): qué luces alumbran a qué objetos.
// Los animales con los que se habla van solo en la capa "Personajes": las luces normales del nivel
// alumbran todas las capas, pero la linterna del submarino solo el escenario (así no los quema).
public static class CapasLuz
{
    public const int Escenario = 1 << 0;    // "Default"
    public const int Personajes = 1 << 1;   // "Light Layer 1"
    public const int Todas = -1;
}
