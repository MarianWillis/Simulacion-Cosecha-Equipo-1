# Simulacion-Cosecha-Equipo-1

Visualizador 3D en Unity de la simulación multiagente de cosecha corrida en Python
(`granja_multiagente/granja.py`, repo hermano). Unity nunca simula: solo pinta lo
que el puente WebSocket (`granja_multiagente/puente_unity.py`) le manda.

## Flujo del dashboard (`Assets/Scripts/UI/`, `Assets/Scripts/Puente/`)

```
HomeView.BuildConfigCard()          10 NumberField, cada uno escribe directo a
                                     _state.Config (SimConfig) con su propio clamp
        │
        ▼
DashboardBootstrap.RestartRun()  →  SimulationDataAdapter.Restart(cfg)
        │
        ▼
ConexionSimulacion.EnviarReiniciar(...)  →  JSON {"tipo":"reiniciar","parametros":{...}}
        │  (WebSocket, NativeWebSocket)
        ▼
puente_unity.py: _escuchar_comandos → control.parametros.update(...) → reinicia el modelo
        │
        ▼
granja.py: GranjaModel(p).sim_setup(...) → manda "init" fresco → GestorSimulacion reconstruye la escena
```

`GestorSimulacion` instancia terreno/agentes desde `init` y los mueve en cada `paso`.
`SimulationDataAdapter` se suscribe a los mismos eventos (`AlIniciar`/`AlAvanzarPaso`)
para alimentar `DashboardState` sin duplicar lógica — separación limpia, Python es la
única fuente de verdad del estado de simulación.

**`Puente/PanelControlSimulacion.cs` es código muerto**: el dashboard actual no lo usa
(usa `HomeView` → `SimulationDataAdapter` → `ConexionSimulacion` directo). Solo sigue
referenciado en la escena vieja `Assets/Scenes/Websocket Simulation.unity`, no en la
del dashboard.

## Fallas conocidas, no arregladas (encontradas 2026-09-07, verificadas corriendo `granja.py`)

Los rangos que `HomeView.cs` permite en el panel de Configuración pueden romper la
simulación del lado Python. El puente (`puente_unity.py`) ya no crashea con estos
casos (manda `{"tipo":"error","mensaje":...}` y se queda esperando un `reiniciar`
válido), pero **`ConexionSimulacion.ProcesarMensaje` no reconoce `tipo:"error"`** —
cae en el `default` y solo hace `Debug.LogWarning`. Resultado: el usuario aprieta
RESTART, no pasa nada visible, y no hay señal en pantalla de por qué.

1. **`Cosechadores = 0` cuelga el restart.** `HomeView.cs:141` permite
   `Mathf.Clamp(v, 0, 4)`. Con `n_harvesters=0`, `granja.py` crashea en
   `setup()` (`tractor.harvester_seguido = self.harvesters[i % len(self.harvesters)]`
   → `ZeroDivisionError`). Fix: mínimo 1, no 0.

2. **`PctObstaculos`/`ProbDescompostura`: mismatch de unidades.**
   `HomeView.cs:145-146` los clampea a **0–100** (UI los trata como porcentaje),
   pero se mandan tal cual a Python, que los espera como **fracción 0–1**
   (`pct_obstaculos=0.02` = 2%). Los defaults de `SimConfig` ya vienen rotos
   (`PctObstaculos=10f`, `ProbDescompostura=2f`, `DashboardState.cs:60,63`): con
   un restart sin tocar esos campos, `pct_obstaculos=10` ya no crashea (clampeado
   del lado Python a `n_obstaculos <= celdas_cultivo`) pero vuelve **el 100% del
   cultivo obstáculo** — campo inutilizable, `cosechado_pct` no avanza nunca.
   Fix: dividir entre 100 antes de mandar (o mostrar la UI ya en fracción 0–1).

3. **`Semilla` no tiene ningún clamp.** `HomeView.cs:144`:
   `v => _state.Config.Semilla = v`. Un seed negativo rompe
   `numpy.random.default_rng` (`ValueError: expected non-negative integer`).
   Fix: `Mathf.Max(v, 0)`.

4. **`Rows`/`Cols` clampeados a `[3, 8]`** (`HomeView.cs:139-140`). Con
   `ancho_camino=2` fijo en `granja.py`, hace falta `>=7` para que quede algo de
   cultivo (3–6 da `total_cultivo=0`: ya no crashea, pero la sim "termina" en el
   instante 0 reportando 100%, confuso en el dashboard). Incluso en 7–8 el
   cultivo disponible es de 1 a 4 celdas — con `Cosechadores` hasta 4 sobran
   harvesters sin nada que hacer. Fix: subir el mínimo a algo como 10-15.

5. **Falta feedback de error end-to-end.** Raíz común de 1 y 3: el puente no
   crashea más, pero no hay contrato Unity↔Python para `tipo:"error"`. Fix:
   `ConexionSimulacion` debería exponer un evento (`AlErrorReinicio`) que
   `GestorSimulacion`/dashboard puedan escuchar para mostrar algo visible en vez
   de tragárselo en un `Debug.LogWarning`.

Ver también `granja_multiagente/CLAUDE.md` para los límites y invariantes del lado
Python (`pct_obstaculos`, `total_cultivo`, umbral de gasolina, etc.) que motivan
estos fixes.
