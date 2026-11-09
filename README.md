<div align="center">

# 🎮 GOLOSA AR

### ¡Salta, aprende y juega en realidad aumentada!

**Un videojuego educativo para Android que transforma la golosa tradicional en una experiencia interactiva 3D.**

![Unity](https://img.shields.io/badge/Unity-6000.6.4f1-222222?style=for-the-badge&logo=unity)
![C%23](https://img.shields.io/badge/C%23-Lenguaje-6f42c1?style=for-the-badge)
![Android](https://img.shields.io/badge/Android-8.0%2B-3DDC84?style=for-the-badge&logo=android&logoColor=white)
![ARCore](https://img.shields.io/badge/ARCore-6.6.2-4285F4?style=for-the-badge)

**Ingeniería de Sistemas · Fundación Universitaria Juan de Castellanos · 2026-2**

</div>

---

## 📖 ¿Qué es Golosa AR?

**Golosa AR** es un videojuego educativo para Android que combina la golosa tradicional —también conocida como *rayuela*— con un tablero tridimensional, avatares y preguntas educativas. El proyecto busca conservar la tradición de los juegos populares e impulsar el aprendizaje mediante experiencias interactivas y realidad aumentada.

El jugador puede escoger una modalidad, configurar su perfil, seleccionar un personaje 3D y participar en partidas individuales o por turnos.

## 📑 Índice

1. [Objetivo](#-objetivo)
2. [Funcionalidades](#-funcionalidades)
3. [Tecnologías y versiones](#%EF%B8%8F-tecnologías-y-versiones)
4. [Requisitos y compatibilidad](#-requisitos-y-compatibilidad)
5. [Estructura del repositorio](#-estructura-del-repositorio)
6. [Abrir el proyecto en Unity](#-abrir-el-proyecto-en-unity)
7. [Instalación de la APK](#-instalación-de-la-apk)
8. [Cómo jugar](#-cómo-jugar)
9. [Solución de problemas](#-solución-de-problemas)
10. [Recomendaciones para desarrolladores](#-recomendaciones-para-desarrolladores)
11. [Entrega académica y créditos](#-entrega-académica-y-créditos)

## 🎯 Objetivo

Promover el aprendizaje interactivo y la preservación de los juegos tradicionales mediante una adaptación digital de la golosa que integra un tablero 3D, desafíos educativos y realidad aumentada.

## ✨ Funcionalidades

| Funcionalidad | Descripción |
|---|---|
| 👤 **Perfil de jugador** | Permite registrar el nombre y seleccionar la categoría. |
| 🧍 **Avatares 3D** | Permite elegir un personaje disponible. |
| 🎯 **Modo individual** | Permite jugar una partida personal. |
| 👥 **Modo por turnos** | Permite configurar partidas de **2 a 4 participantes**. |
| 📚 **Categorías** | Incluye **Niños** y **Avanzados**. |
| 🟪 **Tablero virtual** | Representa una golosa tridimensional de diez casillas. |
| 📷 **Realidad aumentada** | Permite preparar la ubicación del tablero mediante detección de superficies en dispositivos compatibles. |
| 🔳 **Marcador del proyecto** | Ofrece un flujo de colocación mediante el marcador propio de Golosa AR. |
| ❓ **Preguntas educativas** | Incorpora desafíos durante la experiencia. |
| 🏆 **Resultados** | Permite consultar puntuaciones y registros locales de partidas. |

> **Nota:** algunas funciones requieren permiso de cámara y compatibilidad con ARCore. Para el modo de marcador se utiliza el recurso específico del proyecto.

## 🛠️ Tecnologías y versiones

| Tecnología | Versión / función |
|---|---|
| **Unity Editor** | `6000.6.4f1` |
| **C#** | Lógica del juego, interfaces y navegación |
| **AR Foundation** | `6.6.2` |
| **Google ARCore XR Plugin** | `6.6.2` |
| **Universal Render Pipeline (URP)** | `17.6.0` |
| **Unity UI y TextMesh Pro** | Controles, pantallas y textos |
| **PlayerPrefs** | Preferencias y datos locales |
| **Android** | Plataforma de ejecución |

Las versiones se verifican en `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json` y `ProjectSettings/ProjectSettings.asset`.

## 📱 Requisitos y compatibilidad

- **Android 8.0 (API 26) o superior**, según la versión mínima configurada en el proyecto.
- Cámara funcional y autorización del permiso correspondiente.
- Espacio de almacenamiento suficiente para instalar la APK.
- Para la detección de superficies: **dispositivo compatible con ARCore** y servicios requeridos.
- Un espacio seguro, despejado y bien iluminado.

> [!IMPORTANT]
> Cumplir el requisito mínimo de Android **no garantiza** compatibilidad con realidad aumentada. Comprueba el modelo en la [lista oficial de dispositivos compatibles con ARCore](https://developers.google.com/ar/devices).

Los emuladores Android convencionales pueden no admitir las funciones de RA. Se recomienda probar en un teléfono físico compatible.

## 📁 Estructura del repositorio

```text
GolosaAR/
├── Assets/             # Escenas, scripts C#, modelos, prefabs y recursos
├── Packages/           # Paquetes y dependencias de Unity
├── ProjectSettings/    # Configuración del editor, proyecto y Android
├── .gitignore          # Exclusión de archivos temporales
└── README.md           # Documentación general
```

### Escenas principales

| Escena | Propósito |
|---|---|
| `00_Inicio` | Bienvenida y acceso al videojuego. |
| `01_Perfil` | Información del jugador, categoría y selección del avatar. |
| `02_ModoJuego` | Selección de la modalidad. |
| `03_Juego` | Desarrollo de la partida y experiencia de RA. |

Los recursos y scripts se encuentran dentro de `Assets/`. Para evitar referencias rotas, **no cambies nombres ni muevas escenas, scripts o prefabs fuera del editor de Unity**.

## 💻 Abrir el proyecto en Unity

### 1. Herramientas necesarias

- **Unity Hub**.
- **Unity Editor `6000.6.4f1`**.
- **Android Build Support**, con Android SDK & NDK Tools y OpenJDK.
- **Git**, si deseas clonar el repositorio desde la terminal.

### 2. Descargar el código fuente

**Opción A — Git:**

```bash
git clone https://github.com/CarenGarciaDev/GolosaAR.git
```

**Opción B — ZIP:** entra a la [página principal del repositorio](https://github.com/CarenGarciaDev/GolosaAR), selecciona **Code → Download ZIP** y descomprime el archivo.

### 3. Importar el proyecto

1. Abre **Unity Hub**.
2. Selecciona **Add project from disk**.
3. Elige la carpeta que contiene `Assets`, `Packages` y `ProjectSettings`.
4. Abre con la versión de Unity indicada y permite la importación de recursos y paquetes.
5. Localiza y abre la escena inicial `00_Inicio` desde la ventana **Project**.

### 4. Compilar para Android

1. Comprueba que **Android Build Support** esté instalado.
2. Abre **Build Profiles** y selecciona **Android**.
3. Revisa la inclusión y el orden de las escenas necesarias.
4. Verifica ajustes de Android, permisos y compatibilidad del dispositivo.
5. Genera la **APK** y pruébala en un teléfono físico compatible.

> Las carpetas `Library`, `Temp`, `Logs` y `Obj` se generan automáticamente y no forman parte del código fuente versionado.

## 📦 Instalación de la APK

La APK se entrega **por separado** del repositorio de código fuente.

1. Copia el archivo APK a un teléfono Android compatible.
2. Ábrelo desde el administrador de archivos y autoriza la instalación desde esa fuente si Android lo solicita.
3. Inicia **Golosa AR**.
4. Concede el permiso de **cámara** cuando se solicite.

## 🚀 Cómo jugar

1. En la pantalla inicial, entra al videojuego.
2. Selecciona la modalidad **Individual** o **Por turnos**.
3. Configura el nombre, categoría y avatar según el flujo de la modalidad seleccionada.
4. Elige la preparación del tablero mediante **detección de superficies** o el **marcador del proyecto**.
5. Sigue las indicaciones de escaneo y coloca el tablero virtual.
6. Responde los desafíos durante la partida y revisa los resultados.

> **Uso del marcador:** emplea el marcador oficial de Golosa AR, completo y bien iluminado. Un código QR genérico no lo sustituye.

## ❓ Solución de problemas

| Problema | Recomendación |
|---|---|
| La APK no se instala | Comprueba la versión de Android, el espacio disponible y los permisos de instalación. |
| La cámara no se abre | Revisa los permisos de cámara en los ajustes del teléfono. |
| No se detecta una superficie | Comprueba ARCore, mejora la iluminación y enfoca una superficie con textura. |
| El marcador no se reconoce | Utiliza el marcador oficial, sin recortes, obstrucciones ni reflejos. |
| El tablero no aparece | Mantén el teléfono estable, sigue las instrucciones y revisa las opciones de recuperación de la app. |
| La RA no funciona en un emulador | Realiza pruebas en un dispositivo físico compatible con ARCore. |

## 🤝 Recomendaciones para desarrolladores

- Utiliza la misma versión de Unity para minimizar incompatibilidades.
- Conserva los archivos `.meta`, las escenas, los prefabs y las referencias serializadas.
- Documenta los componentes y métodos principales de C# con comentarios claros y, cuando corresponda, etiquetas XML como `/// <summary>`.
- No subas cachés de Unity, compilaciones temporales, credenciales ni información privada.
- Antes de subir cambios, comprueba que el proyecto importe correctamente y que las escenas principales sigan funcionando.
- Evita renombrar clases, métodos vinculados a eventos o campos serializados sin revisar sus referencias en las escenas.

## 🎓 Entrega académica y créditos

La entrega académica comprende:

- **APK** ejecutable para Android.
- **Proyecto Unity comprimido** con código fuente y recursos.
- **Repositorio de GitHub:** [CarenGarciaDev/GolosaAR](https://github.com/CarenGarciaDev/GolosaAR).
- **Manual de usuario**, presentado como documento independiente.

| Información | Detalle |
|---|---|
| **Proyecto** | Golosa AR |
| **Programa** | Ingeniería de Sistemas |
| **Institución** | Fundación Universitaria Juan de Castellanos |
| **Asignatura** | Plataformas de Computación |
| **Periodo académico** | 2026-2 |

Este README contiene información general y técnica; **no reemplaza el manual de usuario**.

---

<div align="center">

**💜 GOLOSA AR**

*Tradición, aprendizaje e innovación tecnológica.*

</div>
