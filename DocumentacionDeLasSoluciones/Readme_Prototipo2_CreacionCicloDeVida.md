# **Prototipo 2: Creación y ciclo de vida de las bolitas**

## **1\. Objetivo**

El objetivo de este prototipo es implementar un sistema responsable de la creación, configuración, reutilización y devolución de las bolitas durante la ejecución del juego.

Se busca evitar la creación y destrucción constante de objetos físicos, reutilizando instancias existentes mediante un sistema de *Object Pooling*. Además, se busca garantizar que cada bolita pueda ser configurada con distintos datos y habilidades sin conservar información de su uso anterior.

## **2\. Problema de diseño**

Durante una partida, el juego necesita crear y retirar numerosas bolitas. Crear y destruir continuamente objetos de Unity puede generar trabajo innecesario para el sistema y afectar el rendimiento.

Por otro lado, reutilizar una instancia física presenta otro problema: una bolita puede conservar propiedades de su utilización anterior, como velocidad, masa, escala, estado lógico o componentes asociados a una habilidad.

Por ejemplo, una bolita explosiva podría volver al sistema y ser reutilizada como una bolita normal. Si no se realiza una reinicialización correcta, podría conservar el comportamiento explosivo de su uso anterior.

Por lo tanto, es necesario separar los datos de configuración de las instancias físicas y establecer un proceso controlado de creación, reutilización y devolución.

## **3\. Solución implementada**

La solución se organiza mediante las siguientes clases:

### **Inventory**

Administra la cantidad disponible de cada tipo de bolita.

* Permite agregar unidades al inventario.  
* Permite consultar las cantidades disponibles.  
* Permite consumir una unidad cuando se solicita una bolita.  
* Controla que no se supere la capacidad máxima establecida.

El inventario almacena cantidades por tipo de bolita, sin mantener referencias a los objetos físicos.

### **BallData**

Representa los datos de configuración de una bolita, como su identificador, masa, tipo de habilidad, radio del efecto, tiempo de activación y escala.

Se utiliza para definir las propiedades que tendrá una bolita durante una utilización determinada.

### **BallDataAsset**

Es un ScriptableObject que permite configurar los datos de las bolitas desde el Inspector de Unity.

Su responsabilidad es almacenar la configuración y proporcionar una instancia de BallData mediante el método ToBallData().

Esto permite modificar los parámetros de cada tipo de bolita sin tener que modificar la lógica de creación.

### **BallRuntime**

Representa el estado lógico de una utilización concreta de una bolita.

Cada vez que una instancia física se reutiliza, se genera un nuevo BallRuntime. De esta manera, la nueva utilización comienza con una identidad y un estado lógico independientes de los anteriores.

### **BallFactory**

Se encarga de crear nuevas instancias físicas a partir de un prefab y configurarlas con los datos recibidos.

Su responsabilidad se limita a la creación e inicialización inicial de las bolitas.

### **BallPool**

Administra las instancias físicas existentes y permite reutilizarlas.

Cuando se solicita una bolita, primero comprueba si existe una instancia disponible. Si existe, la reinicializa y la devuelve. Si no hay ninguna disponible, solicita su creación a BallFactory, siempre que no se haya alcanzado el límite máximo.

Al devolver una bolita, el sistema limpia su estado físico y lógico, elimina los componentes dinámicos asociados a habilidades y espera hasta que la instancia se encuentre completamente neutralizada antes de volver a ponerla a disposición.

El límite de instancias está establecido en 100 bolitas.

### **BallSpawner**

Coordina la solicitud de una bolita desde el juego.

Recibe el tipo de bolita solicitado y su posición de aparición. Luego verifica que exista una unidad disponible en el inventario, obtiene los datos correspondientes y solicita una instancia a BallPool.

El inventario solamente consume la unidad cuando la obtención de la bolita se realiza correctamente.

## **4\. Flujo de funcionamiento**

El funcionamiento general del sistema es el siguiente:

1. Se solicita una bolita indicando su tipo y posición de aparición.  
2. BallSpawner verifica que exista una unidad disponible en Inventory.  
3. Se obtienen los datos de configuración correspondientes al tipo solicitado.  
4. BallSpawner solicita una instancia a BallPool.  
5. BallPool comprueba si existe una instancia reutilizable.  
6. Si existe, reinicializa sus propiedades y genera un nuevo BallRuntime. Si no existe, solicita su creación a BallFactory.  
7. Una vez obtenida correctamente la bolita, se consume una unidad del inventario.  
8. Cuando la bolita debe retirarse, se devuelve al pool.  
9. BallPool limpia sus propiedades y elimina los componentes dinámicos asociados a sus habilidades.  
10. Una vez que la instancia se encuentra neutralizada, vuelve a estar disponible para una futura utilización.

## **5\. Decisiones de diseño**

### **Separación entre datos e instancias físicas**

Se decidió mantener separados los datos de configuración y los objetos físicos de Unity.

BallData y BallDataAsset representan la configuración de las bolitas, mientras que BallController administra la instancia física y su interacción con Unity.

Esto permite modificar las características de una bolita sin depender directamente de la creación del GameObject.

### **Reutilización de instancias mediante Object Pooling**

Se implementó BallPool para evitar crear y destruir objetos constantemente.

La reutilización permite aprovechar las instancias existentes y mantener un límite máximo de objetos físicos.

### **Reinicialización del estado**

Cada reutilización debe comenzar desde un estado limpio.

Se reinician propiedades como:

* Velocidad lineal y angular.  
* Masa.  
* Escala.  
* Posición.  
* Estado lógico.  
* Componentes dinámicos asociados a habilidades.

Además, cada nueva utilización recibe un BallRuntime independiente.

### **Gestión de componentes dinámicos**

Se decidió eliminar los componentes que implementan IEffectExecutor cuando una bolita vuelve al pool.

De esta manera, una instancia puede cambiar de tipo y comportamiento entre diferentes utilizaciones sin conservar habilidades anteriores.

Esta decisión también permite incorporar nuevos ejecutores de efectos sin tener que mantener una lista fija de componentes específicos dentro del sistema de devolución.

## **6\. Tests automatizados**

Se implementaron pruebas automatizadas para verificar las responsabilidades principales del prototipo.

### **Inventory (EditMode)**

**AddBall\_IncreasesQuantity**

Verifica que agregar bolitas incremente correctamente la cantidad disponible del tipo correspondiente.

Resultado esperado: la cantidad aumenta según las unidades agregadas.

**TryUseBall\_DecreasesQuantity**

Verifica que consumir una bolita reduzca correctamente la cantidad disponible.

Resultado esperado: el método devuelve true y la cantidad disminuye en una unidad.

**TryUseBall\_WhenInventoryIsEmpty\_ReturnsFalse**

Verifica que no sea posible consumir una bolita cuando no hay unidades disponibles.

Resultado esperado: el método devuelve false y la cantidad permanece en cero.

### **BallFactory (PlayMode)**

**Create\_InitializesBallCorrectly**

Verifica que una bolita creada mediante BallFactory reciba correctamente sus datos, posición y configuración física.

Resultado esperado: la instancia se crea activa y con sus propiedades inicializadas.

**Create\_WithNullPrefab\_ReturnsNull**

Verifica el comportamiento cuando no se proporciona un prefab válido.

Resultado esperado: la creación devuelve null.

**Create\_WithNullData\_ReturnsNull**

Verifica el comportamiento cuando no se proporcionan datos de configuración.

Resultado esperado: la creación devuelve null.

### **BallPool (PlayMode)**

**GetBall\_CreatesInitializedBall**

Verifica que el pool pueda crear y entregar una bolita correctamente inicializada cuando no existen instancias disponibles.

Resultado esperado: se obtiene una instancia activa y configurada.

**ReturnBall\_ThenGetBall\_ReusesAndResetsInstance**

Verifica que una bolita devuelta al pool pueda reutilizarse y que sus propiedades se reinicien.

Resultado esperado: se obtiene la misma instancia física, pero con los nuevos datos y sin conservar propiedades de la utilización anterior.

**GetBall\_WhenLimitReached\_ReturnsNull**

Verifica que el pool respete el límite máximo de instancias.

Resultado esperado: cuando se alcanza el límite y no existen instancias disponibles, la solicitud devuelve null.

**ExplosiveBall\_AfterExplosion\_CanBeReusedAsNormal**

Verifica que una bolita explosiva pueda volver al pool y posteriormente reutilizarse como una bolita normal.

Resultado esperado: se reutiliza la misma instancia física, se elimina el ejecutor explosivo y la nueva bolita queda configurada correctamente.

### **BallSpawner (PlayMode)**

**SpawnBall\_WithAvailableInventory\_CreatesBallAndConsumesInventory**

Verifica que el sistema pueda generar una bolita cuando existe una unidad disponible en el inventario.

Resultado esperado: se obtiene una bolita correctamente configurada y se consume una unidad.

**SpawnBall\_WithEmptyInventory\_ReturnsNull**

Verifica que no se pueda generar una bolita cuando el inventario no dispone de unidades del tipo solicitado.

Resultado esperado: el método devuelve null y el inventario permanece sin cambios.

## **7\. Pruebas manuales en Unity**

Además de las pruebas automatizadas, se realizaron verificaciones directamente en una escena de Unity.

Se comprobaron los siguientes comportamientos:

* Generación de bolitas normales y explosivas.  
* Reutilización de una bolita explosiva como normal.  
* Reutilización de una bolita normal como explosiva.  
* Eliminación de los componentes asociados a habilidades anteriores.  
* Reinicialización de masa, escala y velocidades.  
* Comportamiento del inventario al consumir las unidades disponibles.

Estas pruebas permitieron comprobar el funcionamiento conjunto de los componentes dentro del entorno de Unity.

## **8\. Alcance y limitaciones**

El prototipo se centra en la creación, configuración y ciclo de vida de las bolitas.

No contempla todavía una implementación completa de la interfaz de usuario ni todas las mecánicas de progresión del juego.

La selección de datos y componentes dinámicos está implementada para los tipos de bolitas utilizados actualmente, normal y explosiva. La incorporación de nuevos tipos requerirá extender la configuración correspondiente.

El objetivo principal es demostrar que las instancias físicas pueden crearse, reutilizarse y reinicializarse de forma controlada, manteniendo separadas las responsabilidades de inventario, creación y administración de objetos.

## **9\. Conclusión**

El prototipo permite administrar el ciclo de vida de las bolitas mediante responsabilidades separadas.

La combinación de Inventory, BallSpawner, BallFactory y BallPool permite controlar la solicitud, creación y reutilización de instancias, mientras que BallData y BallRuntime mantienen separadas la configuración y el estado de cada utilización.

Las pruebas automatizadas y las verificaciones manuales permiten comprobar que las instancias pueden reutilizarse sin conservar propiedades ni comportamientos de usos anteriores.

