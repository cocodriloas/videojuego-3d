# Casos de prueba — Semana 1

**Proyecto:** Inka Sub: Misión Mar de Grau (videojuego 3D)
**Módulo probado:** Escena de prueba 3D (prototipo de mecánicas básicas)
**Responsable QA:** Antoni — Probador de calidad
**Fecha de ejecución:** 15/11/2026
**Rama / versión:** `main` (versión disponible al momento de la prueba)
**Motor / plataforma:** Unity 6.3 (6000.3.23f1), URP — PC (Windows)
**Evidencia:** [`Docs/QA/Evidencias/EvA1.mp4`](../Evidencias/EvA1.mp4) (42 s)
**Reporte asociado:** [`BugsA1.md`](../Reportes-de-bugs/BugsA1.md)

---

## 1. Alcance de la semana 1

Esta semana se valida únicamente el comportamiento básico del submarino en la escena de prueba 3D, tomada como base para convertir el juego de 2D a 3D:

- Movimiento y orientación del submarino.
- Sistema básico de disparo y munición.
- Cámara de seguimiento.
- Colisiones, sistema de vida y daño.
- Visualización de los elementos 3D de la escena.

**Fuera de alcance (se probará en semanas posteriores):** habilidades especiales (dash, disparo doble, escudo), enemigos y trampas, jefe Iceberg, diálogos con NPC, menú, pausa y guardado, audio y compilación WebGL.

## 2. Controles de la escena

Indicados en el texto que muestra la propia escena:

| Acción | Control |
|---|---|
| Mover el submarino | `W` `A` `S` `D` |
| Apuntar | Mouse |
| Disparar | Clic izquierdo |
| Recargar | `R` |
| Dash | Clic derecho (solo si está desbloqueado) |
| Subir / bajar (tramo hacia el fondo) | `Espacio` / `Ctrl` |

## 3. Criterios de estado

| Estado | Significado |
|---|---|
| ✅ Aprobado | El comportamiento observado coincide con lo esperado. |
| ⚠️ Aprobado con observaciones | Funciona, pero hay un detalle a revisar (ver el reporte). |
| ⏳ Pendiente | No se pudo comprobar con la grabación; requiere una prueba dirigida. |
| ❌ Fallido | El comportamiento observado no coincide con lo esperado. |

## 4. Resumen de resultados

| ID | Caso de prueba | Estado |
|---|---|---|
| CP-S1-01 | Movimiento horizontal con WASD | ✅ Aprobado |
| CP-S1-02 | Cambio de altura del submarino | ✅ Aprobado |
| CP-S1-03 | Orientación y apuntado con el mouse | ✅ Aprobado |
| CP-S1-04 | Disparo con clic izquierdo | ✅ Aprobado |
| CP-S1-05 | Contador de munición | ✅ Aprobado |
| CP-S1-06 | Recarga de munición | ✅ Aprobado |
| CP-S1-07 | Cámara de seguimiento | ✅ Aprobado |
| CP-S1-08 | Visualización de la escena 3D | ⚠️ Aprobado con observaciones |
| CP-S1-09 | Estabilidad durante la sesión | ⚠️ Aprobado con observaciones |
| CP-S1-10 | Colisiones del submarino con obstáculos | ⏳ Pendiente |
| CP-S1-11 | Sistema de vida y daño | ⏳ Pendiente |

**Totales:** 11 casos — 7 aprobados, 2 aprobados con observaciones, 2 pendientes, 0 fallidos.

---

## 5. Detalle de los casos

### CP-S1-01 — Movimiento horizontal con WASD
- **Precondición:** escena de prueba 3D cargada y en ejecución.
- **Pasos:**
  1. Mantener presionadas `W`, `A`, `S` y `D` por separado y en combinación.
  2. Observar el desplazamiento del submarino sobre el escenario.
- **Resultado esperado:** el submarino se desplaza en la dirección indicada, de forma continua y sin saltos.
- **Resultado obtenido:** el submarino recorre el escenario en distintas direcciones con movimiento fluido (0:09–0:33).
- **Estado:** ✅ Aprobado

### CP-S1-02 — Cambio de altura del submarino
- **Precondición:** escena en ejecución.
- **Pasos:**
  1. Subir y bajar el submarino con `Espacio` y `Ctrl`.
  2. Observar la altura respecto al suelo y su sombra.
- **Resultado esperado:** el submarino cambia de altura y la sombra proyectada sobre el suelo acompaña el cambio.
- **Resultado obtenido:** el submarino aparece a distintas alturas durante la grabación (alto al inicio, cerca del suelo en el tramo central) y su sombra se ajusta correctamente.
- **Estado:** ✅ Aprobado
- **Nota:** las teclas pulsadas no se ven en el video; se valida por el efecto visible.

### CP-S1-03 — Orientación y apuntado con el mouse
- **Precondición:** escena en ejecución.
- **Pasos:**
  1. Mover el mouse a distintas posiciones de la pantalla.
  2. Observar hacia dónde se orienta el submarino.
- **Resultado esperado:** el submarino gira hacia el punto apuntado sin giros bruscos.
- **Resultado obtenido:** el submarino cambia de orientación de forma continua siguiendo el cursor (0:00–0:42).
- **Estado:** ✅ Aprobado

### CP-S1-04 — Disparo con clic izquierdo
- **Precondición:** munición disponible (mayor a 0).
- **Pasos:**
  1. Hacer clic izquierdo, de forma aislada y en ráfaga.
  2. Observar los proyectiles.
- **Resultado esperado:** por cada clic sale un proyectil desde el submarino en la dirección apuntada.
- **Resultado obtenido:** se generan proyectiles (óvalos verdes claros) desde el submarino hacia la dirección apuntada, tanto en disparos sueltos (0:00–0:12) como en ráfaga (0:36–0:42).
- **Estado:** ✅ Aprobado

### CP-S1-05 — Contador de munición
- **Precondición:** HUD de munición visible (esquina inferior izquierda, formato `actual / 20`).
- **Pasos:**
  1. Disparar varias veces.
  2. Observar el contador.
- **Resultado esperado:** el contador disminuye al disparar y no cambia mientras no se dispara.
- **Resultado obtenido:** baja al disparar (18/20 → 17/20 en 0:09–0:12; 10/20 → 3/20 en la ráfaga final) y se mantiene en 17/20 entre 0:12 y 0:33 sin disparos.
- **Estado:** ✅ Aprobado

### CP-S1-06 — Recarga de munición
- **Precondición:** munición por debajo de 20.
- **Pasos:**
  1. Presionar `R`.
  2. Observar el contador.
- **Resultado esperado:** el contador vuelve al máximo (20/20).
- **Resultado obtenido:** el contador sube hasta 20/20 en los primeros segundos de la grabación (0:00–0:06) y desde ahí se puede volver a disparar.
- **Estado:** ✅ Aprobado
- **Nota:** la recarga es progresiva, no instantánea. Se recomienda confirmar con el equipo de programación que ese es el comportamiento diseñado.

### CP-S1-07 — Cámara de seguimiento
- **Precondición:** escena en ejecución.
- **Pasos:**
  1. Desplazar el submarino en distintas direcciones y alturas.
  2. Observar el encuadre.
- **Resultado esperado:** la cámara sigue al submarino y lo mantiene visible en todo momento.
- **Resultado obtenido:** el submarino permanece en pantalla durante toda la grabación, tanto visto de costado como desde atrás, sin cortes ni saltos de cámara.
- **Estado:** ✅ Aprobado

### CP-S1-08 — Visualización de la escena 3D
- **Precondición:** escena en ejecución.
- **Pasos:**
  1. Recorrer el escenario observando suelo, rocas, algas, iluminación, sombras y fondo.
- **Resultado esperado:** todos los elementos se muestran con su material, sombra e iluminación correctos, sin parpadeos ni elementos faltantes.
- **Resultado obtenido:** el submarino, las rocas, las algas y los demás objetos se ven con iluminación y sombras correctas, sin parpadeos. Los objetos son formas básicas de prueba (cápsula, esferas, cilindros), no los modelos finales. Al final de la grabación (0:41) se alcanza a ver el borde del plano del suelo y el vacío detrás.
- **Estado:** ⚠️ Aprobado con observaciones → ver `OBS-S1-01`.

### CP-S1-09 — Estabilidad durante la sesión
- **Precondición:** escena en ejecución durante varios segundos.
- **Pasos:**
  1. Mover, apuntar y disparar de forma continua durante la sesión.
- **Resultado esperado:** sin cierres inesperados, congelamientos ni retrasos visibles.
- **Resultado obtenido:** la sesión de 42 s transcurre sin cierres, congelamientos ni tirones visibles. La consola de Unity no aparece en el video, por lo que no se puede confirmar la ausencia de errores o advertencias.
- **Estado:** ⚠️ Aprobado con observaciones → ver `OBS-S1-03`.

### CP-S1-10 — Colisiones del submarino con obstáculos
- **Precondición:** escena en ejecución con rocas y algas.
- **Pasos:**
  1. Dirigir el submarino contra una roca, un alga y el suelo.
  2. Observar si lo atraviesa o se detiene.
- **Resultado esperado:** el submarino no atraviesa los objetos sólidos.
- **Resultado obtenido:** la grabación no muestra un choque directo y claro, por lo que no se puede concluir el resultado.
- **Estado:** ⏳ Pendiente (prueba dirigida en la siguiente sesión).

### CP-S1-11 — Sistema de vida y daño
- **Precondición:** el submarino puede recibir daño y existe un indicador de vida.
- **Pasos:**
  1. Provocar un impacto contra un objeto o fuente de daño.
  2. Observar la reducción de vida.
- **Resultado esperado:** la vida disminuye al recibir un impacto y el cambio se refleja en el HUD.
- **Resultado obtenido:** en la grabación no hay indicador de vida visible ni impactos recibidos, por lo que no se puede evaluar.
- **Estado:** ⏳ Pendiente (prueba dirigida en la siguiente sesión).

---

## 6. Conclusión

En la semana 1 el prototipo cumple lo evaluado: movimiento, orientación con el mouse, disparo, munición, recarga y cámara de seguimiento funcionan sin errores visibles. No se detectaron defectos que bloqueen el avance. Quedan pendientes las colisiones y el sistema de vida y daño, que requieren una prueba dirigida. Las observaciones se detallan en el reporte asociado.
