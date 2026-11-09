

\# Golosa AR 🎮



\*\*Golosa AR\*\* es un videojuego educativo para Android que integra la golosa tradicional con realidad aumentada. Su propósito es incentivar el uso y la conservación de los juegos tradicionales mediante una experiencia en la que la tecnología es una aliada del aprendizaje.



\*\*Proyecto académico:\*\* Plataformas de Computación · Ingeniería de Sistemas · Fundación Universitaria Juan de Castellanos · 2026-2.



\## 1. Objetivo



Promover el aprendizaje interactivo y la preservación de los juegos tradicionales mediante una versión digital de la golosa que incorpora un tablero virtual 3D, preguntas educativas y realidad aumentada.



\## 2. Funcionalidades



| Función | Descripción |

| --- | --- |

| \*\*Individual\*\* | Permite configurar un perfil y completar una partida personal. |

| \*\*Por turnos\*\* | Permite configurar entre 2 y 4 participantes y registrar sus resultados. |

| \*\*Selección de avatar\*\* | Incluye personajes 3D para representar a los jugadores. |

| \*\*Categorías educativas\*\* | Ofrece las categorías \*\*Niños\*\* y \*\*Avanzados\*\*. |

| \*\*Tablero 3D\*\* | Representa diez casillas de golosa sobre el entorno. |

| \*\*Realidad aumentada / QR\*\* | Permite preparar la colocación del tablero mediante detección de superficies o con el marcador del proyecto. |

| \*\*Preguntas y desafíos\*\* | Integra retos educativos en la mecánica del juego. |

| \*\*Resultados\*\* | Registra puntuaciones e historial local de partidas. |



\## 3. Tecnologías



\- \*\*Unity:\*\* `6000.6.4f1`.

\- \*\*Lenguaje:\*\* C#.

\- \*\*AR Foundation:\*\* `6.6.2`.

\- \*\*Google ARCore XR Plugin:\*\* `6.6.2`.

\- \*\*Universal Render Pipeline (URP):\*\* `17.6.0`.

\- \*\*Interfaz:\*\* Unity UI y TextMesh Pro.

\- \*\*Persistencia local:\*\* PlayerPrefs y archivos JSON para preferencias e historial.



Las versiones se obtienen de `ProjectSettings/ProjectVersion.txt` y `Packages/manifest.json`.



\## 4. Requisitos y compatibilidad



\- \*\*Android 8.0 (API 26) o superior:\*\* versión mínima configurada en el proyecto.

\- \*\*Cámara funcional\*\* y permiso para utilizarla.

\- \*\*Dispositivo compatible con ARCore\*\* para la detección de superficies y las funciones de realidad aumentada que lo requieren.

\- Espacio disponible para instalar la APK.

\- Se recomienda un lugar despejado, bien iluminado y con una superficie visible para la cámara.



\*\*Importante:\*\* tener Android 8.0 o superior \*\*no garantiza\*\* que la realidad aumentada funcione. La compatibilidad también depende del modelo del teléfono, sus sensores y los servicios de RA disponibles. Verifica el modelo en la \[lista oficial de dispositivos compatibles con ARCore](https://developers.google.com/ar/devices).



Los emuladores generales como BlueStacks pueden no ejecutar correctamente ARCore, incluso si consiguen instalar la APK.



\## 5. Estructura del proyecto



```text

GolosaAR/

├── Assets/

│   ├── Scenes/                # Escenas principales del videojuego

│   ├── Scripts/

│   │   ├── AR/               # Preparación y control de la experiencia AR

│   │   ├── Game/             # Tablero, avatares, turnos e historial

│   │   └── UI/               # Interfaces, preguntas y navegación

│   └── Editor/               # Herramientas de generación y configuración

├── Packages/                 # Paquetes y dependencias de Unity

├── ProjectSettings/          # Configuración de proyecto y Android

├── .gitignore                # Excluye cachés y archivos generados

└── README.md

```



\### Escenas principales



| Escena | Uso |

| --- | --- |

| `00\_Inicio` | Pantalla de bienvenida. |

| `01\_Perfil` | Datos del jugador, categoría y avatar. |

| `02\_ModoJuego` | Selección de modalidad. |

| `03\_Juego` | Preparación AR y partida. |



Los scripts creados para el proyecto se encuentran principalmente en `Assets/Scripts/` y `Assets/Editor/`. Se han documentado los componentes principales; algunos scripts auxiliares conservan comentarios breves. Los archivos de terceros se mantienen sin modificaciones.



\## 6. Abrir el proyecto en Unity



1\. Instala \*\*Unity Hub\*\* y la versión \*\*Unity `6000.6.4f1`\*\*, incluyendo \*\*Android Build Support\*\* (SDK/NDK y OpenJDK).

2\. Clona el repositorio:



&#x20;  ```bash

&#x20;  git clone https://github.com/CarenGarciaDev/GolosaAR.git

&#x20;  ```



3\. En \*\*Unity Hub\*\*, selecciona \*\*Add project from disk\*\* y abre la carpeta descargada.

4\. Deja que Unity importe los recursos y resuelva las dependencias de `Packages`.

5\. Abre la escena `Assets/Scenes/00\_Inicio.unity`.

6\. Para compilar en Android, revisa los perfiles de compilación, las escenas habilitadas y la configuración del dispositivo de prueba.



No hace falta descargar `Library`, `Temp`, `Logs` ni `Obj`: Unity las genera automáticamente.



\## 7. Instalación y guía rápida



La \*\*APK\*\* se entrega por separado del código fuente.



1\. Copia la APK al dispositivo Android compatible.

2\. Instálala desde el administrador de archivos; autoriza la instalación desde esa fuente si Android lo solicita.

3\. Abre \*\*Golosa AR\*\* y concede el permiso de cámara.

4\. Selecciona la modalidad \*\*Individual\*\* o \*\*Por turnos\*\*.

5\. Configura el perfil, categoría y avatar según el modo.

6\. Elige la opción de preparación del tablero: \*\*realidad aumentada\*\* o \*\*marcador QR\*\*.

7\. Sigue las indicaciones de la pantalla, coloca la golosa y comienza la partida.



\*\*Marcador QR:\*\* utiliza únicamente el marcador oficial del proyecto. Debe mostrarse o imprimirse completo, sin doblar ni cubrir los bordes, y colocarse en una superficie plana con buena iluminación. \*\*Un QR genérico no sustituye al marcador oficial.\*\*



\## 8. Problemas frecuentes



| Problema | Recomendación |

| --- | --- |

| La APK no se instala | Revisa versión de Android, espacio disponible, arquitectura y permisos de instalación. |

| La cámara no abre | Autoriza el permiso de cámara desde los ajustes de Android. |

| No se detecta el suelo | Comprueba compatibilidad ARCore, buena iluminación y una superficie con textura. |

| No se reconoce el marcador | Usa el marcador oficial, completamente visible, sin reflejos y bien iluminado. |

| El tablero desaparece | Mantén el celular estable y utiliza las opciones de recuperación del juego, si están disponibles. |

| Falla en un emulador | Prueba en un dispositivo físico compatible con ARCore. |



\## 9. Entrega académica



Los productos solicitados se presentan por separado:



\- \*\*APK\*\* funcional del videojuego.

\- \*\*Carpeta comprimida\*\* del proyecto Unity.

\- \*\*Enlace al repositorio GitHub:\*\* https://github.com/CarenGarciaDev/GolosaAR

\- \*\*Manual de usuario\*\* según la guía GAATI.



Este README es documentación técnica de acceso al proyecto y \*\*no reemplaza al manual de usuario\*\*.



\## 10. Créditos



\*\*Proyecto:\*\* Golosa AR.  

\*\*Asignatura:\*\* Plataformas de Computación.  

\*\*Programa:\*\* Ingeniería de Sistemas.  

\*\*Institución:\*\* Fundación Universitaria Juan de Castellanos.  

\*\*Docente:\*\* David Fernando Bernal Bolívar.  

\*\*Periodo:\*\* 2026-2.



\---



\*Golosa AR: tradición, aprendizaje e innovación tecnológica.\*



