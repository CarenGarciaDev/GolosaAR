<div align="center">🎮 GOLOSA AR
¡Salta, aprende y juega en realidad aumentada!
Un videojuego educativo para Android que transforma la golosa tradicional en una experiencia interactiva 3D.

Unity 6 · C# · AR Foundation · Google ARCore




Ingeniería de Sistemas · Fundación Universitaria Juan de Castellanos · 2026-2

Explorar funcionalidades · Instalar · Abrir en Unity
</div>📖 Descripción
Golosa AR es un videojuego educativo para dispositivos Android que combina la dinámica de la golosa (también conocida como rayuela) con un tablero tridimensional, avatares y desafíos de aprendizaje. La experiencia busca conectar la tradición de los juegos populares con herramientas digitales de realidad aumentada.

El jugador puede elegir una modalidad, configurar un perfil, seleccionar un personaje y participar en una partida con preguntas y seguimiento de resultados. La experiencia de realidad aumentada depende de las capacidades del dispositivo y de las condiciones del entorno.
📑 Contenido
1. Objetivo
2. Funcionalidades
3. Tecnologías y versiones
4. Requisitos y compatibilidad
5. Estructura del repositorio
6. Configuración para desarrolladores
7. Instalación de la aplicación
8. Guía rápida de uso
9. Solución de problemas
10. Notas para colaboradores
11. Entregables y documentación
12. Información académica
🎯 Objetivo
Promover el aprendizaje interactivo y la preservación de los juegos tradicionales mediante una adaptación digital de la golosa que integra escenarios tridimensionales, desafíos educativos y experiencias de realidad aumentada.
✨ Funcionalidades
Funcionalidad	¿Qué permite hacer?
👤 Perfil de jugador	Registrar un nombre y elegir la categoría de juego.
🧍 Avatares 3D	Seleccionar un personaje disponible en la aplicación.
🎯 Modo individual	Participar en una partida individual.
👥 Modo por turnos	Configurar partidas para 2 a 4 participantes.
📚 Categorías educativas	Elegir entre Niños y Avanzados.
🟪 Tablero virtual	Interactuar con una representación 3D de la golosa de diez casillas.
📷 Realidad aumentada	Preparar la ubicación del tablero mediante reconocimiento de superficies, en dispositivos compatibles.
🔳 Marcador del proyecto	Utilizar el flujo de colocación asociado al marcador preparado para Golosa AR.
❓ Desafíos	Responder preguntas durante la experiencia de juego.
🏆 Resultados	Consultar los resultados e información de partidas almacenados localmente.


Nota: algunas funciones requieren permiso de cámara, compatibilidad con ARCore y condiciones apropiadas de iluminación. El marcador oficial se entrega por separado cuando corresponde.

🛠️ Tecnologías y versiones
Componente	Versión / finalidad
Unity Editor	6000.6.4f1
C#	Lógica del juego, navegación y componentes interactivos
AR Foundation	6.6.2
ARCore XR Plugin	6.6.2
Universal Render Pipeline (URP)	17.6.0
Unity UI / TextMesh Pro	Controles, textos y pantallas
PlayerPrefs	Almacenamiento de preferencias locales
Android	Plataforma de ejecución


Fuentes dentro del proyecto: ProjectSettings/ProjectVersion.txt, Packages/manifest.json y ProjectSettings/ProjectSettings.asset.
📱 Requisitos y compatibilidad
Para instalar la aplicación:
- Android 8.0 (API 26) o superior, según el mínimo configurado en Unity.
- Cámara funcional y autorización del permiso correspondiente.
- Espacio de almacenamiento suficiente para la APK y los datos de la aplicación.
- Para la detección de superficies en RA: un dispositivo compatible con Google ARCore y los servicios requeridos.
- Un lugar seguro, despejado y con iluminación apropiada.
Importante: cumplir el requisito mínimo de Android no garantiza compatibilidad con realidad aumentada. Consulta el listado oficial de dispositivos compatibles con ARCore.

El funcionamiento de la RA en emuladores Android convencionales puede ser limitado o no estar disponible. Para las pruebas se recomienda un dispositivo físico compatible.
📁 Estructura del repositorio
GolosaAR/
├── Assets/                  # Escenas, scripts, recursos gráficos, modelos y prefabs
├── Packages/                # Dependencias y manifiesto de paquetes
├── ProjectSettings/         # Ajustes del editor y configuración de Android
├── .gitignore               # Exclusiones de archivos temporales y generados
└── README.md                # Guía y documentación general del proyecto
Flujo principal de escenas
Escena	Propósito
00_Inicio	Bienvenida y acceso al juego.
01_Perfil	Datos del jugador y elección de avatar.
02_ModoJuego	Selección de la modalidad.
03_Juego	Experiencia de juego y componentes de RA.


Los scripts C# y recursos se encuentran dentro de Assets/. La ubicación exacta de un archivo puede consultarse en el explorador del repositorio.
Para desarrolladores: evita mover o renombrar archivos directamente desde el explorador de Windows. Hazlo desde el editor de Unity para conservar los archivos .meta y sus referencias.

💻 Configuración para desarrolladores
1. Instalar las herramientas
- Unity Hub.
- Unity Editor 6000.6.4f1.
- Módulo Android Build Support, incluidos Android SDK & NDK Tools y OpenJDK.
- Git (opcional, para trabajar con el repositorio).
2. Obtener el código fuente
Clona el proyecto:
git clone https://github.com/CarenGarciaDev/GolosaAR.git
También puedes utilizar Code → Download ZIP desde la página principal del repositorio y descomprimir el archivo.
3. Importar el proyecto
1. Abre Unity Hub.
2. Selecciona Add project from disk.
3. Elige la carpeta GolosaAR, que debe contener Assets, Packages y ProjectSettings.
4. Abre el proyecto con la versión de Unity indicada.
5. Espera a que finalice la importación de recursos y la restauración de dependencias.
6. Abre la escena inicial 00_Inicio desde la ventana Project de Unity.
4. Compilar para Android
1. Verifica que el módulo Android Build Support esté instalado.
2. Abre los perfiles de compilación (Build Profiles) y selecciona Android.
3. Revisa que las escenas necesarias estén incluidas en el perfil de compilación.
4. Comprueba los ajustes del reproductor, permisos y configuración del dispositivo de prueba.
5. Genera la APK y pruébala en un teléfono compatible.
Nota: es posible que Unity necesite descargar paquetes durante la primera importación. Las carpetas Library, Temp, Logs, Obj y los archivos generados no forman parte del código fuente versionado.

📦 Instalación de la aplicación
La APK se distribuye como entregable independiente; este repositorio contiene principalmente el proyecto editable de Unity.
1. Copia la APK al dispositivo Android.
2. Ábrela desde el administrador de archivos y autoriza la instalación desde esa fuente si el sistema lo solicita.
3. Inicia Golosa AR.
4. Concede el permiso de cámara cuando se requiera.
🚀 Guía rápida de uso
1. En la pantalla de inicio, entra a la experiencia de juego.
2. Selecciona Individual o Por turnos.
3. Configura el nombre, categoría y avatar según la modalidad.
4. Sigue las indicaciones para preparar el tablero mediante detección de superficie o el marcador del proyecto.
5. Coloca el tablero virtual en el entorno y comienza la partida.
6. Responde los desafíos y revisa el resultado al finalizar.
Uso del marcador: emplea únicamente el marcador oficial de Golosa AR, completo, sin recortes y bien iluminado. Un código QR genérico no sustituye al marcador del proyecto.
❓ Solución de problemas
Situación	Qué comprobar
La APK no se instala	Versión de Android, almacenamiento y permisos de instalación.
La cámara no se activa	Permiso de cámara en la configuración de Android.
No se detecta el suelo	Compatibilidad ARCore, iluminación, textura y estabilidad del teléfono.
No se reconoce el marcador	Que sea el oficial, esté completo, plano y sin reflejos.
El tablero no se muestra	Sigue las instrucciones de escaneo y comprueba las opciones de recuperación disponibles.
La RA falla en un emulador	Realiza la prueba en un dispositivo físico compatible con ARCore.


🤝 Notas para colaboradores
- Trabaja preferiblemente con la misma versión de Unity para reducir incompatibilidades.
- Conserva los archivos .meta, escenas, prefabs y referencias serializadas.
- Documenta los cambios relevantes en scripts C# mediante comentarios claros y, cuando corresponda, documentación XML (/// <summary>).
- No subas cachés de Unity, carpetas temporales, APK de prueba ni credenciales o datos privados.
- Antes de publicar cambios, comprueba que el proyecto abra y que las escenas principales sigan funcionando.
- Evita refactorizaciones que cambien nombres de clases o propiedades serializadas sin validar previamente las escenas.
📚 Entregables y documentación
La entrega académica contempla:
- APK ejecutable para Android.
- Proyecto Unity comprimido con scripts y recursos.
- Repositorio del código fuente: CarenGarciaDev/GolosaAR.
- Manual de usuario de la aplicación, presentado como documento independiente.
Este README ofrece orientación técnica e información general; no reemplaza el manual de usuario ni garantiza por sí solo la compatibilidad con todos los dispositivos Android.
🎓 Información académica
Campo	Información
Proyecto	Golosa AR
Programa	Ingeniería de Sistemas
Institución	Fundación Universitaria Juan de Castellanos
Asignatura	Plataformas de Computación
Periodo académico	2026-2


<div align="center">💜 Golosa AR

Tradición, aprendizaje e innovación tecnológica.
</div>
