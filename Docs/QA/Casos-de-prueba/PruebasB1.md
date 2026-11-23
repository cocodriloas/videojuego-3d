# Casos de prueba — Semana 2

**Proyecto:** Inka Sub: Misión Mar de Grau (videojuego 3D)
**Módulo probado:** Sección 1 · La entrada al Mar de Grau (combate, casco, bacterias y túnel)
**Responsable QA:** Antoni — Probador de calidad
**Periodo:** Semana 2
**Rama / versión:** `main` (versión disponible al momento de la prueba)
**Motor / plataforma:** Unity 6.3 (6000.3.23f1), URP — PC (Windows)
**Evidencias:**
- [`Docs/QA/Evidencias/EvB1.mp4`](../Evidencias/EvB1.mp4) — combate y casco en la sala de entrada (21 s)
- [`Docs/QA/Evidencias/EvB2.mp4`](../Evidencias/EvB2.mp4) — túnel, cámara, munición y disparo (20 s)

**Reporte asociado:** [`BugsB1.md`](../Reportes-de-bugs/BugsB1.md)

---

## 1. Alcance de la semana 2

Esta semana se valida el combate y las habilidades del submarino sobre la Sección 1 del nivel, que ya usa los modelos 3D del juego en lugar de las formas básicas de la semana 1:

- Casco (vida) del submarino y daño recibido.
- Comportamiento del enemigo Fantasma de Red.
- Recolección de bacterias Ideonella.
- Munición, recarga y disparo.
- Navegación por el túnel y cambios de cámara.
- Habilidades especiales (dash, escudo) y jefe Iceberg.

**Fuera de alcance (se probará en semanas posteriores):** menú principal, pausa y guardado, balance de dificultad, audio y compilación WebGL.

## 2. Controles de la escena

Indicados en el texto que muestra la propia escena:

| Acción | Control |
|---|---|
| Mover el submarino | `W` `A` `S` `D` |
| Apuntar | Mouse |
| Disparar | Clic izquierdo |
| Recargar | `R` |
| Avanzar / retroceder en el túnel | `Espacio` / `Ctrl` |

El texto de controles de esta versión ya no menciona el dash (clic derecho), que sí aparecía en la escena de la semana 1.

## 3. Criterios de estado

| Estado | Significado |
|---|---|
| ✅ Aprobado | El comportamiento observado coincide con lo esperado. |
| ⚠️ Aprobado con observaciones | Funciona, pero hay un detalle a revisar (ver el reporte). |
| ⏳ Pendiente | No se pudo comprobar con las grabaciones; requiere una prueba dirigida. |
| ❌ Fallido | El comportamiento observado no coincide con lo esperado. |

## 4. Resumen de resultados

| ID | Caso de prueba | Estado |
|---|---|---|
| CP-S2-01 | Visualización de la Sección 1 con modelos 3D | ✅ Aprobado |
| CP-S2-02 | HUD de sección, controles, casco y munición | ✅ Aprobado |
| CP-S2-03 | Daño al submarino por impacto | ✅ Aprobado |
| CP-S2-04 | Casco en cero | ⚠️ Aprobado con observaciones |
| CP-S2-05 | Comportamiento del enemigo Fantasma de Red | ✅ Aprobado |
| CP-S2-06 | Disparo al enemigo y daño al enemigo | ⏳ Pendiente |
| CP-S2-07 | Recolección de bacteria Ideonella | ⚠️ Aprobado con observaciones |
| CP-S2-08 | Munición, recarga y contador | ⚠️ Aprobado con observaciones |
| CP-S2-09 | Disparo doble | ⏳ Pendiente |
| CP-S2-10 | Navegación por el túnel y cámara | ✅ Aprobado |
| CP-S2-11 | Dash | ⏳ Pendiente |
| CP-S2-12 | Escudo de burbujas | ⏳ Pendiente |
| CP-S2-13 | Jefe Iceberg (fases, lluvia y tentáculos) | ⏳ Pendiente |

**Totales:** 13 casos — 5 aprobados, 3 aprobados con observaciones, 5 pendientes, 0 fallidos.

---

## 5. Detalle de los casos

### CP-S2-01 — Visualización de la Sección 1 con modelos 3D
- **Precondición:** Sección 1 cargada y en ejecución.
- **Pasos:**
  1. Observar el submarino, el enemigo, la tortuga, los corales, las rocas y la bacteria.
  2. Moverse por la sala observando materiales e iluminación.
- **Resultado esperado:** los modelos se muestran completos, con sus texturas e iluminación, sin elementos faltantes.
- **Resultado obtenido:** el submarino, el enemigo, la tortuga, los corales, las rocas y la bacteria aparecen con sus modelos y materiales (EvB1, 0:00–0:21). Ya no hay formas básicas de prueba.
- **Estado:** ✅ Aprobado

### CP-S2-02 — HUD de sección, controles, casco y munición
- **Precondición:** Sección 1 en ejecución.
- **Pasos:**
  1. Observar el texto superior y los indicadores de la esquina inferior izquierda.
- **Resultado esperado:** se muestran el nombre de la sección, los controles, el casco y la munición.
- **Resultado obtenido:** se ven "SECCIÓN 1 · La entrada al Mar de Grau", el texto de controles, el indicador **Casco** (5 segmentos) y el contador de munición (`n / 20`) durante toda la grabación.
- **Estado:** ✅ Aprobado

### CP-S2-03 — Daño al submarino por impacto
- **Precondición:** el submarino con casco por encima de 0 y el enemigo cerca.
- **Pasos:**
  1. Dejar que el enemigo alcance al submarino.
  2. Observar el indicador de casco y la pantalla.
- **Resultado esperado:** el casco baja al recibir el impacto y hay una señal visual de daño.
- **Resultado obtenido:** en cada impacto la pantalla se tiñe de rojo un instante y el casco baja 2 segmentos: 3/5 → 1/5 (≈0:02), 5/5 → 3/5 (≈0:14) y 3/5 → 1/5 (≈0:19). EvB1.
- **Estado:** ✅ Aprobado

### CP-S2-04 — Casco en cero
- **Precondición:** casco en 1/5 o menos.
- **Pasos:**
  1. Recibir un impacto más con el casco en 1/5.
  2. Observar la pantalla y el submarino.
- **Resultado esperado:** al llegar el casco a 0, el juego lo indica (derrota o reaparición avisada).
- **Resultado obtenido:** el casco llega a 0/5 (≈0:08), el submarino desaparece unos 2 segundos y reaparece al lado izquierdo de la sala con el casco completo (5/5, ≈0:10). No aparece ningún mensaje ni pantalla de derrota.
- **Estado:** ⚠️ Aprobado con observaciones → ver `OBS-S2-01`.

### CP-S2-05 — Comportamiento del enemigo Fantasma de Red
- **Precondición:** Sección 1 en ejecución.
- **Pasos:**
  1. Observar al enemigo mientras el submarino se mueve por la sala.
- **Resultado esperado:** el enemigo se desplaza por la sala y se acerca al submarino.
- **Resultado obtenido:** el enemigo se desplaza de forma continua por la sala, sube y baja, y se acerca al submarino varias veces (EvB1, 0:00–0:21). No se queda quieto ni atraviesa el suelo.
- **Estado:** ✅ Aprobado

### CP-S2-06 — Disparo al enemigo y daño al enemigo
- **Precondición:** munición disponible y el enemigo a la vista.
- **Pasos:**
  1. Apuntar al enemigo y disparar.
  2. Observar el impacto y el estado del enemigo.
- **Resultado esperado:** el proyectil impacta al enemigo y este recibe daño hasta ser purificado.
- **Resultado obtenido:** no se pudo evaluar. En EvB1 la munición se mantiene en 12/20 durante todo el video, es decir, no se disparó al enemigo.
- **Estado:** ⏳ Pendiente (prueba dirigida).

### CP-S2-07 — Recolección de bacteria Ideonella
- **Precondición:** bacteria visible en la sala.
- **Pasos:**
  1. Llevar el submarino hasta la bacteria.
  2. Observar la pantalla.
- **Resultado esperado:** la bacteria se recoge, se muestra la señal en pantalla y se aplica la recompensa (dato científico y desbloqueo de poder, según el diseño del juego).
- **Resultado obtenido:** al recogerla aparece el mensaje "Bacteria Ideonella recogida (1)" (≈0:16–0:18) y la bacteria desaparece. En la grabación el juego no se pausa ni se muestra una ficha científica, y no se ve ningún poder desbloqueado.
- **Estado:** ⚠️ Aprobado con observaciones → ver `OBS-S2-03`.

### CP-S2-08 — Munición, recarga y contador
- **Precondición:** EvB2, túnel en ejecución.
- **Pasos:**
  1. Disparar varias veces y recargar con `R`.
  2. Observar el contador.
- **Resultado esperado:** el contador baja al disparar y vuelve al máximo al recargar.
- **Resultado obtenido:** 17/20 (0:00–0:13) → 14/20 (0:14–0:16) → 18/20 (0:17) → el contador muestra **MAX** (0:18–0:19). El formato cambia de `n / 20` a la palabra "MAX".
- **Estado:** ⚠️ Aprobado con observaciones → ver `OBS-S2-02`.

### CP-S2-09 — Disparo doble
- **Precondición:** poder de disparo doble desbloqueado.
- **Pasos:**
  1. Disparar con clic izquierdo.
  2. Observar el número de proyectiles y sus cañones.
- **Resultado esperado:** cada clic dispara dos proyectiles lineales desde los dos cañones.
- **Resultado obtenido:** a partir de ≈0:17 el submarino muestra cañones adicionales y se ven 2 a 3 proyectiles por ráfaga (≈0:16 y ≈0:18). La grabación no deja confirmar si es el disparo doble ni cuándo se activó.
- **Estado:** ⏳ Pendiente (prueba dirigida).

### CP-S2-10 — Navegación por el túnel y cámara
- **Precondición:** EvB2, túnel en ejecución.
- **Pasos:**
  1. Avanzar y retroceder por el túnel con `Espacio` y `Ctrl`.
  2. Mover el submarino con `WASD` y observar la cámara.
- **Resultado esperado:** el submarino recorre el túnel y la cámara lo sigue de forma estable.
- **Resultado obtenido:** el submarino avanza por el corredor (0:07–0:13) con la cámara detrás, y la cámara pasa a una vista lateral y de altura al salir a la zona de bloques (0:00–0:06 y 0:14–0:19). El submarino permanece siempre en pantalla, sin saltos de cámara ni atravesar las paredes.
- **Estado:** ✅ Aprobado
- **Nota:** se detectaron dos detalles visuales menores (`OBS-S2-04` y `OBS-S2-05`).

### CP-S2-11 — Dash
- **Precondición:** dash desbloqueado.
- **Pasos:**
  1. Activar el dash.
  2. Observar el desplazamiento y su indicador.
- **Resultado esperado:** el submarino se impulsa en la dirección de movimiento y el HUD refleja el estado.
- **Resultado obtenido:** no se utilizó en las grabaciones, y el texto de controles ya no lo menciona.
- **Estado:** ⏳ Pendiente (prueba dirigida).

### CP-S2-12 — Escudo de burbujas
- **Precondición:** escudo desbloqueado.
- **Pasos:**
  1. Activar el escudo.
  2. Recibir un impacto con el escudo activo.
- **Resultado esperado:** el escudo absorbe el daño y el casco no baja.
- **Resultado obtenido:** no se utilizó en las grabaciones.
- **Estado:** ⏳ Pendiente (prueba dirigida).

### CP-S2-13 — Jefe Iceberg (fases, lluvia y tentáculos)
- **Precondición:** sala del jefe accesible.
- **Pasos:**
  1. Combatir contra el jefe y recorrer sus fases.
  2. Observar los ataques de lluvia y tentáculos, y el daño recibido.
- **Resultado esperado:** el jefe cambia de fase, ataca como se definió y puede ser derrotado.
- **Resultado obtenido:** la sala del jefe no aparece en las grabaciones.
- **Estado:** ⏳ Pendiente (prueba dirigida).

---

## 6. Conclusión

En la semana 2 la Sección 1 ya funciona con los modelos 3D del juego. El casco, el daño por impacto, el enemigo, la recolección de bacterias, la munición y la navegación por el túnel funcionan sin errores que bloqueen el avance. Hay tres puntos a aclarar con desarrollo: qué ocurre al llegar el casco a 0, qué significa "MAX" en el contador y por qué la bacteria no pausa el juego ni muestra su ficha científica. Quedan pendientes el disparo al enemigo, el disparo doble, el dash, el escudo y el jefe Iceberg, que requieren una prueba dirigida. Los detalles se encuentran en el reporte asociado.
