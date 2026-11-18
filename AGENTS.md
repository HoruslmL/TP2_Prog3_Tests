# Guia para agentes

## Proyecto

- Motor: Unity `6000.3.11f1` (ver `ProjectSettings/ProjectVersion.txt`).
- Proyecto Unity del TP2 de Programacion 3. Implementa un sistema de bolitas, sus datos y habilidades, interacciones, creacion y pooling, inventario, flujo de juego y tests EditMode/PlayMode.
- Los paquetes y sus versiones estan declarados en `Packages/manifest.json`.

## Mapa del proyecto

- `Assets/_Project/Scripts/`: codigo de juego del assembly `TP2.Gameplay`.
  - `Balls/`: `BallController`, `BallRuntime`, `ImpactContext`, `InteractionResolver` y activacion de efectos.
  - `Balls/Data/`: `BallData`, `BallDataAsset` y `BallAbilityType`.
  - `Balls/Abilities/`: `IBallAbility`, `ExplosiveAbility`, ejecutores de efectos y sus interfaces.
  - `Balls/Factory/`: `BallFactory`, `BallPool` y `BallSpawner`.
  - `GameFlow/`: eventos, estados, `GameStateMachine` y `GameFlowController`.
  - `Inventory/`: `Inventory`.
  - `Levels/`: modelo de configuracion de niveles del TP3 (`LevelDataAsset`, `BallSpawnPointData`, `BallSpawnOption`) y `LevelDataValidator`.
  - `TestingInGame/` y `BallTestLauncher.cs`: utilidades de prueba dentro del juego.
  - `TP2.Gameplay.asmdef`: assembly de Runtime; actualmente no declara referencias adicionales.
- `Assets/_Project/Tests/EditMode/`: tests EditMode; assembly `EditModeFolder`, restringido a la plataforma Editor. Cubre datos, validacion de niveles, interacciones, inventario y flujo/eventos.
- `Assets/_Project/Tests/PlayMode/`: tests PlayMode; assembly `PlayModeFolder`. Cubre control de bolitas, factory/pool/spawner, efectos y flujo.
- `Assets/_Project/Scenes/`: escena de colisiones de prueba. `Assets/Scenes/` contiene la escena principal.
- `Assets/_Project/Scripts/Balls/Prefabs/` y `Balls/ScriptableData/`: prefabs y assets de datos de bolitas.
- `ProjectSettings/`: configuracion del proyecto, incluida la version de Unity.

## Ejecutar tests

En Unity, abrir **Window > General > Test Runner**, elegir EditMode o PlayMode y ejecutar la suite correspondiente.

Tambien se puede usar el editor instalado de Unity desde PowerShell, una plataforma por ejecucion:

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe"
$project = (Get-Location).Path
& $unity -batchmode -nographics -projectPath $project -runTests -testPlatform EditMode -testResults TestResults-EditMode.xml -logFile EditMode.log
& $unity -batchmode -nographics -projectPath $project -runTests -testPlatform PlayMode -testResults TestResults-PlayMode.xml -logFile PlayMode.log
```

Ejecutar los comandos **secuencialmente**: Unity no permite abrir el mismo proyecto en dos instancias a la vez. Los resultados XML y logs son artefactos de ejecucion; no agregarlos al repositorio salvo que una tarea lo solicite.

### Baseline inicial

Ejecutado con Unity `6000.3.11f1` el 2026-11-16:

- EditMode: 27 aprobados, 0 fallidos, 0 omitidos.
- PlayMode: 32 aprobados, 0 fallidos, 0 omitidos.
- Sin errores de compilacion detectados.
- El primer intento simultaneo de PlayMode fue rechazado porque EditMode aun tenia abierta una instancia del proyecto; PlayMode se repitio secuencialmente y aprobo. No quedaron cambios de escenas ni otros artefactos en el repositorio.

## Reglas de arquitectura

- Mantener el codigo del juego dentro de `TP2.Gameplay` y sus dependencias orientadas hacia las abstracciones de Runtime, no hacia los tests o el Editor.
- Mantener separados los datos de las bolitas (`BallData`/`BallDataAsset`), el comportamiento de ejecucion, las habilidades y las responsabilidades de creacion/pooling.
- Usar `IBallAbility` y las interfaces existentes para extensiones de habilidades; evitar acoplar el flujo de juego a una implementacion concreta cuando ya exista una abstraccion.
- Mantener los tests EditMode y PlayMode en sus assemblies existentes y respetar sus plataformas. No cambiar o relajar tests solo para hacer pasar una implementacion.
- Preservar el comportamiento y las referencias serializadas existentes en escenas, prefabs y assets; hacer cambios acotados y actualizar pruebas solo para comportamiento nuevo o corregido.

## Separacion Runtime y Editor

- El codigo Runtime vive bajo `Assets/_Project/Scripts/` y se compila en `TP2.Gameplay.asmdef`. No importar ni referenciar `UnityEditor` desde ese assembly.
- Para herramientas o extensiones del Editor, crear `Assets/_Project/Editor/` con un assembly separado (por ejemplo, `TP2.Gameplay.Editor.asmdef`), limitar `includePlatforms` a `Editor` y agregar una referencia a `TP2.Gameplay` cuando corresponda.
- Colocar los scripts que usan APIs de Editor unicamente en ese assembly Editor. No ubicarlos bajo `Scripts/` ni resolver dependencias de Editor agregando directivas condicionales o referencias de Editor al assembly Runtime.
- Los tests EditMode pueden usar APIs de Editor porque su assembly esta limitado a esa plataforma. Los tests PlayMode deben seguir en su assembly separado.

## Limites de cambios

- No refactorizar el TP2 ni alterar comportamiento existente como parte de preparar o implementar una tarea del TP3.
- Modificar solamente los archivos necesarios para la tarea solicitada. No cambiar escenas, prefabs, assets serializados, paquetes, configuracion del proyecto o tests existentes sin que el alcance de la tarea lo requiera explicitamente.
- No agregar funcionalidades futuras del TP3 antes de que sean solicitadas.
- Mantener los archivos `.meta` de Unity junto a los assets nuevos y evitar incluir resultados temporales de pruebas.
- Ejecutar las pruebas relevantes despues de cambios y comunicar fallos preexistentes o problemas del entorno sin ocultarlos.
