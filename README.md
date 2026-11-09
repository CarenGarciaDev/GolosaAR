<div align="center">🎮 Golosa AR
Tradición, aprendizaje e innovación con realidad aumentada
Videojuego educativo para Android · Unity 6 · C# · AR Foundation · ARCore

Proyecto académico · Ingeniería de Sistemas · Fundación Universitaria Juan de Castellanos · 2026-2
</div>📖 ¿Qué es Golosa AR?
Golosa AR es un videojuego educativo para Android que adapta el juego tradicional de la golosa a una experiencia interactiva con realidad aumentada, personajes 3D y desafíos educativos.

Su propósito es promover el aprendizaje y la conservación de los juegos tradicionales a través de la tecnología. Los jugadores pueden configurar su perfil, escoger un avatar, participar individualmente o por turnos y jugar sobre un tablero virtual.
🎯 Objetivo del proyecto
Promover el aprendizaje interactivo y la preservación de los juegos tradicionales mediante una versión digital de la golosa que integra realidad aumentada, un tablero tridimensional y preguntas educativas.
✨ Funcionalidades principales
Funcionalidad	Descripción
Modo individual	Permite crear un perfil y desarrollar una partida personal.
Modo por turnos	Permite configurar partidas de 2 a 4 participantes y registrar sus resultados.
Avatares 3D	Permite elegir personajes para representar a los jugadores.
Categorías educativas	Incluye Niños y Avanzados.
Tablero virtual	Presenta una golosa 3D de diez casillas.
Realidad aumentada	Ofrece colocación del tablero mediante reconocimiento de superficies en equipos compatibles.
Marcador QR	Incluye un flujo de colocación mediante el marcador del proyecto.
Desafíos educativos	Incorpora preguntas durante las partidas.
Resultados	Gestiona información local de puntuaciones e historial.


Algunas funciones dependen de la compatibilidad del dispositivo y de las condiciones del entorno.

🛠️ Tecnologías utilizadas
Tecnología	Versión / función
Unity 6	6000.6.4f1
C#	Lógica del videojuego y control de interfaces
AR Foundation	6.6.2
Google ARCore XR Plugin	6.6.2
Universal Render Pipeline	17.6.0
Unity UI y TextMesh Pro	Interfaz y textos
PlayerPrefs	Persistencia local de preferencias


Las versiones se pueden comprobar en ProjectSettings/ProjectVersion.txt y Packages/manifest.json.
📱 Requisitos y compatibilidad
- Android 8.0 (API 26) o superior: mínimo establecido en la configuración del proyecto.
- Cámara funcional y autorización del permiso de cámara.
- Dispositivo compatible con ARCore para utilizar la detección de superficies.
- Espacio disponible para instalar la APK.
- Un lugar seguro, despejado y bien iluminado para utilizar la realidad aumentada.
Importante: Android 8.0 o superior no garantiza que el teléfono admita ARCore. Para comprobar la compatibilidad, consulta la lista oficial de dispositivos compatibles con ARCore.

Los emuladores Android comunes pueden no admitir correctamente funciones de realidad aumentada. Se recomienda probar la aplicación en un teléfono físico compatible.
📁 Estructura del repositorio
GolosaAR/
├── Assets/             # Escenas, scripts C#, modelos 3D y recursos
├── Packages/           # Manifiesto y dependencias de Unity
├── ProjectSettings/    # Configuración del editor, proyecto y Android
├── .gitignore          # Exclusión de archivos generados por Unity
└── README.md           # Documentación principal
Escenas principales
Escena	Propósito
00_Inicio	Pantalla de bienvenida y acceso al videojuego.
01_Perfil	Registro de datos y selección del avatar.
02_ModoJuego	Selección de modalidad.
03_Juego	Experiencia de juego y realidad aumentada.


Los scripts del proyecto se almacenan dentro de Assets/. No se recomienda mover ni renombrar archivos directamente fuera de Unity, porque las escenas pueden depender de referencias a dichos recursos.
💻 Cómo descargar y abrir el proyecto en Unity
Herramientas necesarias
1. Unity Hub.
2. Unity Editor 6000.6.4f1.
3. Módulo Android Build Support, con Android SDK & NDK Tools y OpenJDK.
4. Conexión a Internet para resolver dependencias durante la primera importación, si es necesario.
Descargar el código fuente
Opción A: Git
git clone https://github.com/CarenGarciaDev/GolosaAR.git
Opción B: abre el repositorio, selecciona Code → Download ZIP y descomprime el archivo.
Abrir y compilar
1. En Unity Hub, selecciona Add project from disk.
2. Abre la carpeta del proyecto que contiene Assets, Packages y ProjectSettings.
3. Espera a que Unity importe los recursos y descargue los paquetes necesarios.
4. Abre la escena 00_Inicio desde el editor.
5. Para generar una APK, selecciona la plataforma Android en los perfiles de compilación.
6. Comprueba las escenas incluidas y configura los ajustes de compilación del dispositivo de prueba antes de generar la APK.
Las carpetas Library, Temp, Logs y Obj se generan automáticamente. No se incluyen en el repositorio.

🚀 Instalación y uso básico de la APK
La APK se entrega por separado del código fuente.
1. Copia el archivo APK al dispositivo Android compatible.
2. Instálalo. Si Android lo solicita, habilita la instalación desde esa fuente.
3. Abre Golosa AR y concede el permiso de cámara.
4. Selecciona Individual o Por turnos.
5. Configura el perfil y el avatar correspondiente.
6. Escoge la opción de preparación del tablero que ofrezca la pantalla: detección de superficie o marcador del proyecto.
7. Sigue las indicaciones para colocar el tablero y comenzar la partida.
Marcador QR: utiliza únicamente el marcador preparado para Golosa AR. Debe estar completo y visible, sin reflejos ni bordes tapados, preferiblemente sobre una superficie plana y bien iluminada. Un código QR genérico no lo reemplaza.
❓ Solución de problemas
Problema	Recomendación
La APK no se instala	Comprueba la versión de Android, el espacio y los permisos de instalación.
La cámara no funciona	Verifica que el permiso de cámara esté habilitado.
No se detecta una superficie	Comprueba ARCore, mejora la iluminación y enfoca una superficie con textura.
No reconoce el marcador	Utiliza el marcador oficial sin recortes, reflejos ni obstrucciones.
El tablero no aparece	Mantén el teléfono estable y consulta las opciones de recuperación que presente la aplicación.
Falla en un emulador	Prueba en un dispositivo Android físico compatible con ARCore.


📦 Entrega académica
La entrega del proyecto comprende, según las instrucciones de la asignatura:
- APK de la aplicación para Android.
- Proyecto Unity comprimido con código fuente y recursos.
- Enlace del repositorio: github.com/CarenGarciaDev/GolosaAR.
- Manual de usuario de la aplicación.
Este README describe el proyecto y cómo acceder al código; no reemplaza el manual de usuario.
🎓 Información académica
Campo	Información
Proyecto	Golosa AR
Programa	Ingeniería de Sistemas
Institución	Fundación Universitaria Juan de Castellanos
Asignatura	Plataformas de Computación
Periodo académico	2026-2


<div align="center">Golosa AR — Una forma diferente de jugar, aprender y conservar nuestras tradiciones. 💜
</div>
