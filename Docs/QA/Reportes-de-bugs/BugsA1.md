# Reporte de bugs — Semana 1

**Proyecto:** Inka Sub: Misión Mar de Grau (videojuego 3D)
**Módulo probado:** Escena de prueba 3D (prototipo de mecánicas básicas)
**Responsable QA:** Antoni — Probador de calidad
**Fecha de ejecución:** 15/11/2026
**Rama / versión:** `main` (versión disponible al momento de la prueba)
**Motor / plataforma:** Unity 6.3 (6000.3.23f1), URP — PC (Windows)
**Evidencia:** [`Docs/QA/Evidencias/EvA1.mp4`](../Evidencias/EvA1.mp4) (42 s)
**Casos de prueba asociados:** [`PruebasA1.md`](../Casos-de-prueba/PruebasA1.md)

---

## 1. Resumen

| Concepto | Cantidad |
|---|---|
| Bugs confirmados | **0** |
| Bugs críticos o de severidad alta | **0** |
| Observaciones menores (a confirmar o dar seguimiento) | **3** |
| Casos de prueba ejecutados | 11 (7 aprobados, 2 aprobados con observaciones, 2 pendientes) |

**Resultado de la semana:** no se detectaron errores que impidan el avance. Se registran 3 observaciones menores que no bloquean ninguna mecánica evaluada.

## 2. Escala de severidad

| Severidad | Descripción |
|---|---|
| Crítica | Cierra el juego o impide continuar la partida. |
| Alta | Una mecánica principal no funciona. |
| Media | Una mecánica funciona con fallas que afectan la experiencia. |
| Baja | Detalle visual o de comportamiento sin impacto en el juego. |

## 3. Bugs confirmados

No se confirmaron bugs en la semana 1. Las mecánicas evaluadas (movimiento, orientación, disparo, munición, recarga y cámara de seguimiento) funcionaron como se esperaba.

## 4. Observaciones

### OBS-S1-01 — El borde del plano del suelo es visible desde la cámara
- **Tipo:** visual / límites del escenario
- **Severidad:** Baja
- **Estado:** Abierta (por confirmar)
- **Evidencia:** `EvA1.mp4`, 0:41
- **Descripción:** al desplazarse hacia un extremo de la escena, la cámara deja ver dónde termina el plano del suelo y el vacío azul que hay detrás.
- **Pasos para reproducir:**
  1. Abrir la escena de prueba 3D.
  2. Desplazar el submarino hacia el límite del escenario.
  3. Observar el borde del suelo desde la cámara de seguimiento.
- **Resultado esperado:** los límites del escenario no se perciben (terreno más amplio, niebla o muros invisibles).
- **Resultado obtenido:** se ve el borde del suelo y el fondo vacío.
- **Nota:** es una escena de prueba, por lo que puede ser un comportamiento provisional. Se deja registrado para tenerlo en cuenta al construir los niveles definitivos.
- **Sugerencia:** colocar límites invisibles o ampliar el terreno, y comprobar si el submarino puede salir del área jugable.

### OBS-S1-02 — Esferas rojas sin identificar en la parte superior de la escena
- **Tipo:** funcional / por aclarar
- **Severidad:** Baja
- **Estado:** Abierta (por aclarar con desarrollo)
- **Evidencia:** `EvA1.mp4`, 0:06, 0:09, 0:18 y 0:21
- **Descripción:** se ven esferas rojas flotando en la parte alta del escenario, entre el agua y el horizonte, que no se relacionan con ninguna acción del jugador visible en el video.
- **Pasos para reproducir:**
  1. Abrir la escena de prueba 3D.
  2. Desplazar el submarino y observar la zona superior de la escena.
- **Resultado esperado:** todos los objetos de la escena tienen una función conocida (objetivo de prueba, obstáculo o decoración).
- **Resultado obtenido:** no se puede determinar qué son ni si interactúan con el submarino.
- **Acción solicitada:** que desarrollo confirme si son objetos de prueba intencionales. En ese caso, se cierra la observación y se agrega un caso de prueba para ellos.

### OBS-S1-03 — No se pudo verificar la consola de Unity ni el rendimiento
- **Tipo:** cobertura de pruebas
- **Severidad:** Baja
- **Estado:** Abierta (para la siguiente sesión)
- **Evidencia:** `EvA1.mp4` (toda la grabación)
- **Descripción:** el video no incluye la ventana de consola ni un contador de FPS. Se comprobó que no hay cierres ni congelamientos, pero no se pudo confirmar la ausencia de errores o advertencias internas.
- **Acción solicitada:** en la siguiente sesión, grabar mostrando la consola de Unity y el panel de estadísticas del Game View.

## 5. Pruebas pendientes

Estos puntos de la semana 1 no se pudieron concluir con la grabación y requieren una prueba dirigida:

| ID | Punto pendiente | Motivo |
|---|---|---|
| CP-S1-10 | Colisiones del submarino con rocas, algas y suelo | No se ve un choque directo y claro en el video. |
| CP-S1-11 | Sistema de vida y daño | No hay indicador de vida visible ni impactos recibidos en el video. |

## 6. Recomendaciones

1. Confirmar con desarrollo el origen y la función de las esferas rojas (OBS-S1-02).
2. Preparar una grabación dirigida con choques contra objetos y recepción de daño para cerrar CP-S1-10 y CP-S1-11.
3. Definir los límites del escenario antes de armar los niveles definitivos (OBS-S1-01).
4. Confirmar que la recarga progresiva (no instantánea) es el comportamiento diseñado.
