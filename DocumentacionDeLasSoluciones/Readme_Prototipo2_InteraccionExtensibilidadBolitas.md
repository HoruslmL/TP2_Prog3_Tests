# **Prototipo 1: Interacción entre bolitas y extensibilidad**

## **1\. Objetivo del prototipo**

El objetivo de este prototipo es implementar un sistema de interacción entre bolitas que permita resolver distintos comportamientos ante las colisiones, sin concentrar toda la lógica dentro de un único componente.

Se busca que el sistema pueda incorporar nuevas habilidades y efectos sin necesidad de modificar constantemente la lógica principal de las bolitas.

Para demostrarlo, se implementaron dos tipos de bolitas:

* **Normal:** no posee ninguna habilidad especial.  
* **Explosiva:** activa una explosión al recibir un impacto, afectando a las bolitas cercanas y pudiendo provocar reacciones en cadena con otras bolitas explosivas.

El prototipo se enfoca en la arquitectura y el funcionamiento de estas interacciones, no en implementar el juego completo.

## **2\. Problema de diseño**

Uno de los problemas identificados durante el diseño fue cómo administrar las interacciones entre distintos tipos de bolitas sin generar dependencias excesivas entre sus componentes.

Una implementación directa podría hacer que `BallController` contenga todas las reglas de interacción y los comportamientos particulares de cada tipo de bolita. Esto provocaría que, al agregar nuevas habilidades, también fuera necesario modificar ese controlador.

Para evitarlo, se decidió separar las responsabilidades:

* La detección de colisiones y la comunicación con Unity quedan a cargo de `BallController`.  
* La resolución de las interacciones queda a cargo de `InteractionResolver`.  
* Las reglas particulares de cada habilidad se implementan mediante `IBallAbility`.  
* La ejecución de los efectos que necesitan interactuar con Unity queda separada en componentes ejecutores.

De esta manera, se busca reducir el acoplamiento y facilitar la incorporación de nuevas habilidades.

## **3\. Solución implementada**

### **3.1. BallController**

Es el componente que conecta la lógica del sistema con Unity.

Se encarga de recibir las colisiones físicas, obtener la información del impacto y construir un `ImpactContext` para enviarlo al sistema de resolución.

También administra la ejecución de las solicitudes de activación de efectos, incluyendo el tiempo de espera configurado para cada habilidad.

### **3.2. ImpactContext**

Es el objeto que contiene la información necesaria para resolver una interacción.

Incluye:

* Bolita que origina el impacto (`Source`).  
* Bolita que recibe el impacto (`Target`).  
* Posición del impacto.  
* Dirección del impacto.  
* Intensidad o fuerza del impacto.

Esto permite que las habilidades reciban la información que necesitan sin depender directamente de los componentes físicos de Unity.

### **3.3. InteractionResolver**

Es el encargado de determinar qué habilidades deben activarse ante una interacción.

Recibe un `ImpactContext` y delega la resolución en las habilidades correspondientes.

Por ejemplo:

* Normal contra Normal: no genera solicitudes de efectos.  
* Normal contra Explosiva: activa la habilidad de la bolita explosiva.  
* Explosiva contra Normal: activa la habilidad de la bolita explosiva.  
* Explosiva contra Explosiva: ambas pueden programar su efecto.

El resolver centraliza las reglas de interacción y evita que cada bolita tenga que conocer directamente el comportamiento de las demás.

### **3.4. IBallAbility y ExplosiveAbility**

`IBallAbility` define el contrato que deben cumplir las habilidades de las bolitas.

Esto permite implementar diferentes comportamientos respetando una misma interfaz.

`ExplosiveAbility` es la implementación utilizada en este prototipo. Se encarga de evaluar la interacción y generar las solicitudes correspondientes cuando participa una bolita explosiva.

### **3.5. EffectActivationRequest**

Representa una solicitud para activar un efecto.

Contiene la referencia al propietario del efecto, el tipo de habilidad y el tiempo de demora obtenido de los datos de la bolita.

Esto permite separar la decisión de activar una habilidad de su ejecución física.

### **3.6. ExplosiveEffectExecutor**

Es el componente encargado de ejecutar el efecto explosivo dentro de Unity.

Busca las bolitas que se encuentran dentro del radio de explosión y aplica un impulso a las que corresponda.

Además, evita afectar a la propia bolita que genera la explosión y se encarga de retirarla al finalizar su efecto.

Cuando una bolita explosiva cercana recibe el impacto de otra explosión, puede programar su propia activación, permitiendo generar reacciones en cadena.

### **3.7. BallData y BallRuntime**

Se separan los datos de configuración de una bolita de su estado durante la partida.

`BallData` contiene información como el identificador, la masa, el tipo de habilidad, el radio de efecto y la demora de activación.

`BallRuntime` representa la instancia lógica de una bolita durante su ciclo de vida. Administra estados como:

* `Available`  
* `Active`  
* `PendingEffect`  
* `EffectExecuted`  
* `BeingCollected`

Esto permite controlar qué operaciones puede realizar una bolita según su estado actual.

## **4\. Decisiones de diseño**

### **Separación de responsabilidades**

Se decidió evitar que `BallController` contenga las reglas particulares de todas las habilidades.

El controlador se ocupa de la comunicación con Unity, mientras que `InteractionResolver` y las implementaciones de `IBallAbility` administran las reglas de interacción.

Esto permite agregar habilidades sin tener que reescribir el funcionamiento general de las colisiones.

### **Uso de interfaces para las habilidades**

Se utiliza `IBallAbility` para establecer un contrato común entre las distintas habilidades.

De esta manera, el sistema puede trabajar con diferentes implementaciones sin necesitar conocer sus detalles internos.

### **Separación entre resolución y ejecución de efectos**

Se decidió diferenciar la solicitud de activación de un efecto de su ejecución física.

`ExplosiveAbility` determina cuándo corresponde activar la habilidad, mientras que `ExplosiveEffectExecutor` realiza las operaciones físicas necesarias dentro de Unity.

Esto evita mezclar las reglas de interacción con la manipulación directa de objetos físicos.

### **Contexto de impacto**

Se utiliza `ImpactContext` para transportar toda la información necesaria de una colisión.

Así, las habilidades no necesitan acceder directamente a los componentes de Unity para conocer qué bolitas participaron en el impacto o dónde ocurrió.

## **5\. Tests automatizados**

Se implementaron dos grupos de pruebas: **Edit Mode y Play Mode**.

La separación se realizó según el tipo de comportamiento que se necesita verificar.

* Los tests de Edit Mode comprueban la lógica y los contratos del sistema sin necesitar ejecutar la simulación física.  
* Los tests de Play Mode comprueban comportamientos que dependen del ciclo de vida de Unity, las colisiones físicas, los componentes y la ejecución de efectos dentro de la escena.

### **5.1. Tests de Edit Mode**

Se implementaron 10 tests.

Estos tests permiten verificar la lógica interna de forma aislada, sin depender de una escena activa ni de la simulación física de Unity.

| Clase | Test | Comportamiento verificado |
| ----- | ----- | ----- |
| `BallDataAssetTests` | `ToBallData_ConvertsExplosiveConfigurationCorrectly` | Comprueba que la configuración del asset se convierta correctamente en un objeto `BallData`. |
| `BallDataTest` | `NormalBall_StoresBasicConfiguration` | Comprueba que una bolita normal almacene correctamente sus datos básicos y no tenga habilidad especial. |
| `BallDataTest` | `ExplosiveBall_StoresExplosionConfiguration` | Comprueba que una bolita explosiva almacene correctamente el tipo de habilidad, radio y demora. |
| `BallRuntimeTests` | `ExplosiveBall_CanCompleteLifecycleAndReturnToPool` | Comprueba que una bolita explosiva pueda recorrer sus estados hasta regresar a `Available`. |
| `BallRuntimeTests` | `NormalBall_CannotScheduleExplosion` | Comprueba que una bolita normal no pueda programar una explosión. |
| `EffectActivationRequestTest` | `Constructor_StoresOwnerAndConfiguration` | Comprueba que la solicitud conserve el propietario, el tipo de habilidad y la demora correspondientes. |
| `ImpactContextTests` | `Constructor_StoresBallsAndImpactInformation` | Comprueba que el contexto conserve las bolitas involucradas y los datos del impacto. |
| `InteractionResolverTests` | `NormalHitsExplosive_ReturnsOneRequestForExplosive` | Comprueba que una interacción entre una bolita normal y una explosiva genere una solicitud para la explosiva. |
| `InteractionResolverTests` | `TwoExplosiveBallsImpact_ReturnsTwoRequests` | Comprueba que dos bolitas explosivas puedan generar sus respectivas solicitudes de activación. |
| `InteractionResolverTests` | `RepeatedImpact_DoesNotCreateDuplicateRequest` | Comprueba que un mismo impacto no genere solicitudes duplicadas para una bolita que ya tiene un efecto programado. |

#### **¿Por qué estos tests se ejecutan en Edit Mode?**

Porque verifican principalmente lógica independiente de la simulación de Unity.

Por ejemplo, no es necesario crear una escena ni simular una colisión para comprobar que `InteractionResolver` genere una solicitud correctamente o que `BallRuntime` rechace una transición inválida.

Esto permite ejecutar las pruebas de manera rápida y aislada, facilitando la detección de errores en los contratos y las reglas internas.

### **5.2. Tests de Play Mode**

Se implementaron 4 tests.

A diferencia de los anteriores, estos necesitan ejecutar el entorno de Unity para comprobar que los componentes interactúen correctamente con la física y el ciclo de vida de los objetos.

| Clase | Test | Comportamiento verificado |
| ----- | ----- | ----- |
| `BallControllerPlayModeTest` | `BallController_InitializesRuntimeFromBallData` | Comprueba que el controlador inicialice correctamente el estado lógico de la bolita y configure la masa del `Rigidbody2D` a partir de sus datos. |
| `BallCollisionPlayModeTest` | `NormalBallHitsExplosiveBall_ActivatesExplosion` | Comprueba que una colisión física entre una bolita normal y una explosiva active la habilidad de la explosiva, mientras la normal continúa activa. |
| `ExplosiveEffectExecutorPlayModeTests` | `Explosion_AffectsOnlyNearbyBallsAndDestroysOwner` | Comprueba que la explosión aplique fuerza a las bolitas dentro de su radio, no afecte a las que están fuera, no impulse a su propietario y retire a la bolita explosiva. |
| `ExplosiveEffectExecutorPlayModeTests` | `Explosion_ActivatesNearbyExplosiveBall` | Comprueba que una explosión pueda activar otra bolita explosiva que se encuentre dentro del radio, generando una reacción en cadena. |

#### **¿Por qué estos tests se ejecutan en Play Mode?**

Porque los comportamientos que verifican dependen de funcionalidades que Unity ejecuta durante la partida.

Por ejemplo:

* La detección real de colisiones entre `Collider2D`.  
* La interacción entre `Rigidbody2D` y la simulación física.  
* La aplicación de impulsos mediante la física de Unity.  
* La búsqueda de objetos dentro de un radio de explosión.  
* La destrucción de objetos durante la ejecución.  
* La inicialización y el funcionamiento de los componentes en una escena.

Estos comportamientos no se pueden validar completamente con una prueba aislada de lógica en Edit Mode.

Por eso se utilizan tests de Play Mode, que permiten comprobar que los diferentes componentes funcionen juntos dentro del entorno real de ejecución.

## **6\. Alcance y limitaciones**

El prototipo demuestra el funcionamiento de las interacciones entre bolitas normales y explosivas, incluyendo la activación de efectos y las reacciones en cadena.

Se priorizó la implementación de los contratos y las responsabilidades de cada componente antes que la incorporación de nuevas habilidades o funcionalidades de juego.

Quedan fuera del alcance de esta entrega:

* La implementación de todas las habilidades previstas para el juego.  
* La interfaz gráfica y los sistemas completos de progresión.  
* La integración con el juego final.  
* La cobertura automatizada de todas las combinaciones posibles de habilidades.  
* Una prueba específica que mida el tiempo real de espera antes de ejecutar la explosión.

La demora de activación forma parte de la configuración y de las solicitudes de efecto, y se verifica su transmisión mediante los tests de Edit Mode. Sin embargo, la espera real durante la ejecución no cuenta con un test automatizado específico en este prototipo.

## **7\. Ejecución de los tests**

Para reproducir las pruebas:

1. Abrir el proyecto con la versión de Unity utilizada para el desarrollo.  
2. Abrir **Window → General → Test Runner**.  
3. Seleccionar la pestaña **EditMode** para ejecutar las pruebas de lógica.  
4. Seleccionar la pestaña **PlayMode** para ejecutar las pruebas de integración con Unity.  
5. Ejecutar los tests correspondientes y comprobar que todos finalicen con estado `Passed`.

### **Resultado de las pruebas**

| Tipo | Cantidad de tests | Resultado |
| ----- | ----- | ----- |
| Edit Mode | 10 | Passed |
| Play Mode | 4 | Passed |
| **Total** | **14** | **Todos aprobados** |

## **8\. Prueba visual de integración en Unity**

Además de los tests automatizados, se preparó una escena específica para comprobar visualmente la integración entre las colisiones físicas de Unity y el sistema de resolución de interacciones.

Escena: `BallCollisionTest`

La escena contiene dos bolitas:

* NormalBall: ubicada en la posición X \= \-2, Y \= 0\.  
* ExplosiveBall: ubicada en la posición X \= 2, Y \= 0\.

Ambas utilizan `Rigidbody2D` con cuerpo dinámico, gravedad desactivada y masa 1\.

La bolita normal tiene agregado un componente `BallTestLauncher`, configurado para utilizar como objetivo a la bolita explosiva.

Funcionamiento de la prueba

Al iniciar la escena, `BallTestLauncher` aplica automáticamente un impulso a la bolita normal en dirección a la explosiva, utilizando una velocidad configurada de 5\.

De esta manera, se genera una colisión física real entre ambas instancias.

Cuando ocurre el impacto:

1. Unity detecta la colisión entre los `Collider2D`.  
2. `BallController` recibe el evento de colisión.  
3. Se construye un `ImpactContext` con la información de las bolitas involucradas y del impacto.  
4. `InteractionResolver` recibe el contexto y delega la resolución en las habilidades correspondientes.  
5. `ExplosiveAbility` determina que corresponde activar la habilidad de la bolita explosiva.  
6. La solicitud de activación se procesa y la bolita explosiva pasa al estado `PendingEffect`.

Resultado esperado

Al ejecutar la escena, la bolita normal debe desplazarse hasta impactar contra la explosiva.

Como resultado de la interacción, la bolita explosiva debe quedar con su efecto programado, lo que puede verificarse mediante los mensajes de depuración de la consola y el estado lógico de su `BallRuntime`.

Esta prueba está enfocada en comprobar la conexión entre la simulación física de Unity y la lógica de interacción. No representa una prueba visual de la explosión en sí misma, sino de la detección del impacto y la activación de la habilidad correspondiente.

Diferencia con los tests automatizados

Esta prueba se realiza manualmente desde una escena de Unity y permite observar el comportamiento de las bolitas durante la ejecución.

No se contabiliza dentro de los 14 tests automatizados del prototipo. Su finalidad es complementar las pruebas de Play Mode mediante una comprobación visual del flujo completo de colisión y resolución de la interacción.

## 

## **9\. Conclusión**

El prototipo permitió implementar una arquitectura de interacciones que separa la detección de colisiones, la resolución de habilidades y la ejecución de efectos físicos.

La utilización de interfaces, contextos de impacto y solicitudes de activación permite mantener las responsabilidades diferenciadas y facilita la incorporación de nuevos comportamientos.

Los tests de Edit Mode verifican la lógica interna y los contratos del sistema, mientras que los tests de Play Mode comprueban su funcionamiento integrado con la física y el ciclo de vida de Unity.

De esta manera, el prototipo cumple con el objetivo de demostrar una solución extensible y comprobable para las interacciones entre bolitas, sin necesidad de desarrollar el juego completo.

