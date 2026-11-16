# 🌊 Diseño de Secciones — Inka Sub 3D

> Documento de diseño de niveles. Diseño completo con bocetos y referencias en
> [Figma](https://www.figma.com/board/SGBGWj3NOiVKHdJs2yqsFC/INKA-SUB-3D?node-id=0-1&t=79SBvzkudSpREJ1-0).

---

## 🗺️ Mapa general del nivel

El juego es un **nivel continuo** que baja cada vez más profundo en el **Mar de Grau**.
La cámara cambia según la zona:

| Zona | Tipo de cámara |
| --- | --- |
| Corredores y salas | 📷 **Cámara 2D** (de costado) |
| Túneles | 🎥 **Cámara atrás** (detrás del submarino) |

**Recorrido completo:**

1. **Inicio:** corredor de aprendizaje con la tortuga y el primer monstruo.
2. **Fosa:** caída vertical que lleva a lo profundo.
3. **Túnel 1 (cámara atrás):** se avanza hacia el fondo esquivando redes y trampas.
4. **Salas de oleadas 01 y 02:** se cierran con compuertas de energía y solo se abren al vencer todas las oleadas.
5. **Pasillo diagonal:** el pingüino y una bacteria.
6. **Zonas 03 y 04:** pasillo superior e inferior, cada uno con su oleada, conectados por una compuerta en el piso.
7. **Mero gigante:** advierte del Iceberg y entrega el Escudo de Burbujas.
8. **Túnel en "U" (La Garganta):** baja hacia el fondo, gira y vuelve.
9. **Cangrejo (punto de control):** guarda la partida antes del jefe.
10. **Sala del Jefe:** batalla final contra el Iceberg de Basura (3 fases).

> 🔴 **Compuertas y muros de energía:** bloquean el paso hasta completar un objetivo
> (hablar con un animal o vencer una oleada).

---

## 🐢 Sección 1: Tutorial

**Cámara:** 📷 2D (de costado)
**Objetivo:** aprender lo básico — moverse, hablar con los NPCs y el primer combate.

### Recorrido

1. **Inicio:** el submarino empieza a la izquierda y encuentra a la **tortuga** (ícono **E** para hablar).
2. **Muro de energía:** no se abre hasta que el jugador habla primero con la tortuga.
   Así se enseña a interactuar con los NPCs.
3. **Primer combate:** del otro lado espera el **Fantasma de Red**, un monstruo melee que
   patrulla con movimiento horizontal y embiste si te acercas.
4. **Fosa:** al final del corredor está la caída que lleva a la siguiente zona.

### Elementos de la sección

| Elemento | Tipo | Función |
| --- | --- | --- |
| 🐢 Tortuga | NPC | Enseña a interactuar (tecla **E**) |
| 🟥 Muro de energía | Bloqueo | Se abre al hablar con la tortuga |
| 👻 Fantasma de Red | Enemigo melee | Primer combate: patrulla y embiste |
| ⬇️ Fosa | Transición | Lleva a la zona profunda |

**Referencia visual:** estilo de cámara lateral de *Crash Bandicoot*.