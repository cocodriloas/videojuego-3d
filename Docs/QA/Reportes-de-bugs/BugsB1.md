# Reporte de bugs — Semana 2

**Proyecto:** Inka Sub: Misión Mar de Grau (videojuego 3D)
**Módulo probado:** Sección 1 · La entrada al Mar de Grau (combate, casco, bacterias y túnel)
**Responsable QA:** Antoni — Probador de calidad
**Periodo:** Semana 2
**Rama / versión:** `main` (versión disponible al momento de la prueba)
**Motor / plataforma:** Unity 6.3 (6000.3.23f1), URP — PC (Windows)
**Evidencias:**
- [`Docs/QA/Evidencias/EvB1.mp4`](../Evidencias/EvB1.mp4) (21 s)
- [`Docs/QA/Evidencias/EvB2.mp4`](../Evidencias/EvB2.mp4) (20 s)

**Casos de prueba asociados:** [`PruebasB1.md`](../Casos-de-prueba/PruebasB1.md)

---

## 1. Resumen

| Concepto | Cantidad |
|---|---|
| Bugs confirmados | **0** |
| Bugs críticos o de severidad alta | **0** |
| Observaciones (a confirmar o dar seguimiento) | **5** (1 de severidad media, 4 de severidad baja) |
| Casos de prueba ejecutados | 13 (5 aprobados, 3 aprobados con observaciones, 5 pendientes) |

**Resultado de la semana:** no se detectaron errores que impidan el avance. Las 5 observaciones requieren confirmación de desarrollo, sobre todo la OBS-S2-01, porque afecta la experiencia de juego.

## 2. Escala de severidad

| Severidad | Descripción |
|---|---|
| Crítica | Cierra el juego o impide continuar la partida. |
| Alta | Una mecánica principal no funciona. |
| Media | Una mecánica funciona con fallas que afectan la experiencia. |
| Baja | Detalle visual o de comportamiento sin impacto en el juego. |

## 3. Bugs confirmados

No se confirmaron bugs en la semana 2. El casco, el daño por impacto, el enemigo, la recolección de bacterias, la munición y la navegación por el túnel funcionaron como se esperaba.

## 4. Observaciones

### OBS-S2-01 — Al llegar el casco a 0 no hay aviso de derrota
- **Tipo:** funcional / retroalimentación al jugador
- **Severidad:** Media
- **Estado:** Abierta (por confirmar con desarrollo)
- **Evidencia:** `EvB1.mp4`, ≈0:08 a ≈0:10
- **Descripción:** cuando el casco llega a 0/5, el submarino desaparece unos 2 segundos y reaparece al lado izquierdo de la sala con el casco completo (5/5). No se muestra ningún mensaje, pantalla de derrota ni efecto que avise al jugador.
- **Pasos para reproducir:**
  1. Abrir la Sección 1.
  2. Dejar que el enemigo impacte al submarino hasta que el casco llegue a 0.
  3. Observar la pantalla durante los siguientes 2 segundos.
- **Resultado esperado:** un aviso claro de derrota o de reaparición.
- **Resultado obtenido:** el submarino reaparece con el casco completo y sin aviso.
- **Nota:** puede ser un comportamiento provisional, ya que la pantalla de derrota forma parte de la interfaz planificada.
- **Acción solicitada:** confirmar si la reaparición automática con el casco completo es lo diseñado.

### OBS-S2-02 — El contador de munición muestra "MAX"
- **Tipo:** funcional / HUD
- **Severidad:** Baja
- **Estado:** Abierta (por aclarar con desarrollo)
- **Evidencia:** `EvB2.mp4`, 0:17 a 0:19
- **Descripción:** después de recargar (14/20 → 18/20), el contador cambia a la palabra "MAX" en lugar de `n / 20`. Mientras se ve "MAX" siguen saliendo proyectiles (≈0:18). En la semana 1, el contador mostraba `20 / 20` con la munición llena. En esa misma zona, el submarino muestra cañones adicionales.
- **Resultado esperado:** un significado claro del texto, por ejemplo munición llena o mejora activa.
- **Resultado obtenido:** no es posible saber qué indica "MAX" ni si la munición sigue bajando mientras se muestra.
- **Acción solicitada:** confirmar si "MAX" significa munición llena, munición ilimitada o un poder activo (como el disparo doble).

### OBS-S2-03 — La bacteria no pausa el juego ni muestra la ficha científica
- **Tipo:** funcional / diseño
- **Severidad:** Baja
- **Estado:** Abierta (por aclarar con desarrollo)
- **Evidencia:** `EvB1.mp4`, ≈0:16 a ≈0:18
- **Descripción:** al recoger la bacteria aparece solo el mensaje "Bacteria Ideonella recogida (1)". El enemigo sigue moviéndose, por lo que el juego no se pausa, y no se muestra el dato científico ni un poder desbloqueado. El diseño del juego indica que cada bacteria pausa la partida, muestra un dato científico y desbloquea un poder.
- **Resultado esperado:** pausa, ficha científica y desbloqueo de poder.
- **Resultado obtenido:** solo el mensaje flotante.
- **Acción solicitada:** confirmar si la pausa y la ficha están pendientes de implementar o si no se activaron en esta prueba.

### OBS-S2-04 — Una columna tapa parcialmente al submarino
- **Tipo:** visual / cámara
- **Severidad:** Baja
- **Estado:** Abierta
- **Evidencia:** `EvB1.mp4`, ≈0:16 a ≈0:18
- **Descripción:** al pasar por la zona de la columna, el submarino queda parcialmente oculto detrás de ella desde la vista de la cámara, justo cuando aparece el mensaje de la bacteria.
- **Resultado esperado:** el submarino permanece visible, o el objeto que lo tapa se vuelve transparente.
- **Resultado obtenido:** el submarino queda cubierto en parte.
- **Sugerencia:** reubicar la columna o ajustar la transparencia de los objetos que bloquean la cámara.

### OBS-S2-05 — Líneas horizontales en una pared oscura
- **Tipo:** visual / iluminación
- **Severidad:** Baja
- **Estado:** Abierta
- **Evidencia:** `EvB2.mp4`, ≈0:02 a ≈0:03
- **Descripción:** en la pared oscura detrás del submarino se ven franjas horizontales. Parece un artefacto de sombras o de iluminación.
- **Resultado esperado:** superficie lisa, sin franjas.
- **Resultado obtenido:** se ven líneas horizontales sobre la pared.
- **Sugerencia:** revisar el sesgo (bias) de las sombras y las luces de esa zona.

## 5. Pruebas pendientes

Estos puntos de la semana 2 no se pudieron concluir con las grabaciones y requieren una prueba dirigida:

| ID | Punto pendiente | Motivo |
|---|---|---|
| CP-S2-06 | Disparo al enemigo y daño al enemigo | En EvB1 no se disparó (munición fija en 12/20). |
| CP-S2-09 | Disparo doble | No se puede confirmar con la grabación. |
| CP-S2-11 | Dash | No se utilizó, y el texto de controles ya no lo menciona. |
| CP-S2-12 | Escudo de burbujas | No se utilizó. |
| CP-S2-13 | Jefe Iceberg (fases, lluvia y tentáculos) | La sala del jefe no aparece en las grabaciones. |

## 6. Recomendaciones

1. Confirmar con desarrollo qué debe ocurrir al llegar el casco a 0 (OBS-S2-01).
2. Aclarar el significado de "MAX" en el contador de munición (OBS-S2-02).
3. Confirmar si la pausa y la ficha científica de las bacterias están pendientes (OBS-S2-03).
4. Preparar una grabación dirigida que cubra disparo al enemigo, dash, escudo y combate contra el jefe.
5. En la siguiente sesión, grabar mostrando la consola de Unity para verificar que no haya errores internos.
