# TP3 - Level Ball Editor

Herramienta de edición y configuración de niveles desarrollada en Unity
para el proyecto de Programación de Videojuegos III.

## 1. Prototipo elegido

Para el TP3 se desarrolló **Level Ball Editor**, una herramienta
personalizada dentro del Editor de Unity que permite crear, configurar,
visualizar y validar niveles del juego de las bolitas sin necesidad de
iniciar el juego para cada modificación.

La herramienta trabaja con puntos de aparición. Cada punto puede
contener distintas opciones de bolitas y sus probabilidades de
aparición. La configuración se guarda en un `LevelDataAsset`.

### Funcionalidades principales

-   Crear y editar puntos de aparición desde una ventana personalizada.
-   Seleccionar y mover puntos en la vista de escena.
-   Configurar los tipos de bolitas disponibles y sus probabilidades.
-   Generar una previsualización determinista a partir de una semilla
    (*seed*).
-   Aplicar balance dinámico a las probabilidades durante la generación.
-   Validar la configuración y mostrar errores y advertencias.
-   Detectar superposiciones entre puntos y advertir cuando no se
    respeta la distancia mínima configurada.
-   Integrar los niveles con el sistema de generación y aparición de
    bolitas del juego.

La previsualización se realiza sobre los datos del nivel: no instancia
bolitas físicas ni modifica el estado de juego.

## 2. Requisitos

-   **Unity 6.3.11f1** (versión utilizada durante el desarrollo).
-   Abrir el proyecto desde Unity Hub.
-   Esperar a que Unity importe los recursos y resuelva los paquetes del
    proyecto.

Se recomienda utilizar la misma versión de Unity para evitar diferencias
de compatibilidad.

## 3. Cómo abrir la herramienta

1.  Abrir el proyecto desde Unity Hub.
2.  Esperar a que termine la importación y compilación inicial.
3.  Abrir la escena que contiene el objeto `LevelRoot` y el contexto del
    nivel.
4.  Abrir la herramienta **Level Ball Editor** desde el menú del Editor
    de Unity.
5.  Seleccionar o asignar el `LevelDataAsset` que se desea editar y
    configurar el `LevelRoot` correspondiente.
6.  Utilizar la ventana y la vista de escena para crear, seleccionar,
    configurar y mover los puntos de aparición.

> **Nota:** los puntos del `LevelDataAsset` se guardan en coordenadas
> locales respecto de `LevelRoot`. Para que la validación, la
> previsualización y la aparición durante el juego representen la misma
> disposición, se debe utilizar la raíz correspondiente al nivel.

## 4. Cómo configurar y revisar un nivel

1.  Crear o seleccionar un `LevelDataAsset`.
2.  Agregar puntos de aparición desde la herramienta.
3.  Configurar las opciones de bolitas disponibles en cada punto y sus
    probabilidades.
4.  Ajustar la semilla (*seed*) para obtener una distribución
    reproducible.
5.  Revisar la previsualización de la distribución.
6.  Consultar los resultados de validación.

### Errores y advertencias

-   **Errores:** indican configuraciones que impiden generar el nivel.
-   **Advertencias:** señalan situaciones que conviene revisar, pero que
    no necesariamente bloquean la generación.

Entre los controles espaciales se encuentran la detección de puntos
superpuestos y las advertencias por distancia mínima insuficiente entre
puntos.

## 5. Cómo ejecutar los tests

El proyecto utiliza el **Unity Test Framework**.

1.  Abrir el proyecto en Unity.
2.  Ir a `Window > General > Test Runner`.
3.  En la ventana Test Runner, seleccionar **EditMode** para ejecutar
    las pruebas de lógica, datos, validaciones y herramientas de
    edición.
4.  Seleccionar **PlayMode** para ejecutar las pruebas que necesitan el
    entorno de juego, como las relacionadas con la aparición de bolitas.
5.  Presionar **Run All** para ejecutar las pruebas de la categoría
    seleccionada.

Se recomienda ejecutar ambas categorías y comprobar que no haya pruebas
fallidas. Algunas pruebas de Editor verifican operaciones y estados
internos; las interacciones visuales de Scene View también deben
comprobarse manualmente en Unity.

## 6. Organización general

El proyecto mantiene separadas las responsabilidades de ejecución del
juego y las herramientas del Editor:

-   **Runtime:** contiene los datos y la lógica utilizados durante el
    juego, incluyendo la validación, generación de distribución y
    aparición de niveles.
-   **Editor:** contiene la ventana personalizada, el Inspector de
    `LevelDataAsset` y las operaciones de edición específicas de Unity
    Editor.
-   **Tests:** contiene pruebas automatizadas para verificar el
    comportamiento de los componentes y flujos principales.

La separación entre los assemblies Runtime y Editor evita que la lógica
del juego dependa de APIs de `UnityEditor`.

## 7. Documentación adicional

Este README resume cómo abrir, utilizar y probar el prototipo. Para
evaluar el funcionamiento completo, se recomienda abrir el proyecto en
Unity, probar la creación y edición de puntos, revisar la
previsualización y las validaciones, y ejecutar los tests de EditMode y
PlayMode.
