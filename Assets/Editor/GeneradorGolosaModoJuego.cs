using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

// Actualiza el generador que YA EXISTE; no crea otra pantalla ni otro controlador.
// Solo modifica el contenido del Canvas en 02_ModoJuego.
/// <summary>
/// Herramienta de editor que reconstruye la interfaz de selección del modo de juego
/// dentro de la escena 02_ModoJuego, utilizando las imágenes y el controlador existentes.
/// No participa en la ejecución del juego en el dispositivo Android.
/// </summary>
public static class GeneradorGolosaModoJuego
{
    // Ruta de los sprites usados por el generador, dimensiones de referencia
    // del diseño original y color corporativo para los mensajes.
    private const string Carpeta = "Assets/Art/ModoJuego/";
    private const float W = 941f;
    private const float H = 1672f;
    private static readonly Color Uva = new Color32(67, 24, 124, 255);

    /// <summary>
    /// Punto de entrada disponible desde el menú de Unity. Valida la escena y los
    /// recursos, crea la interfaz, conecta los eventos y guarda los cambios.
    /// Debe ejecutarse fuera del modo Play y con 02_ModoJuego abierta.
    /// </summary>
    [MenuItem("Golosa AR/Generar pantalla de modo de juego")]
    public static void Generar()
    {
        // Evita alterar objetos de escena mientras Unity reproduce o inicia Play.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Golosa AR", "Detén Play antes de actualizar el diseño.", "Aceptar");
            return;
        }
        // Impide generar esta interfaz por error en una escena distinta.
        if (SceneManager.GetActiveScene().name != "02_ModoJuego")
        {
            EditorUtility.DisplayDialog("Golosa AR", "Abre la escena 02_ModoJuego primero.", "Aceptar");
            return;
        }
        // Ambos recursos visuales son obligatorios para las dos opciones del menú.
        Sprite individual = Cargar("Modo_Individual.png");
        Sprite turnos = Cargar("Modo_PorTurnos.png");
        if (individual == null || turnos == null)
        {
            EditorUtility.DisplayDialog("Golosa AR", "Faltan Modo_Individual.png o Modo_PorTurnos.png en Assets/Art/ModoJuego.", "Aceptar");
            return;
        }

        // Reutiliza el Canvas de la escena o crea uno si todavía no existe.
        // La escala se adapta a la resolución de referencia de un dispositivo móvil.
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
            canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2340f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        // Garantiza que la UI pueda recibir pulsaciones; admite el Input System
        // moderno y, si no está disponible, el módulo de entrada tradicional.
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var e = new GameObject("EventSystem", typeof(EventSystem));
            var tipo = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (tipo != null) e.AddComponent(tipo); else e.AddComponent<StandaloneInputModule>();
        }

        // Elimina únicamente las versiones generadas de ESTA pantalla.
        Transform anterior = canvas.transform.Find("ModoJuegoGenerado");
        if (anterior != null) Object.DestroyImmediate(anterior.gameObject);
        // Compatibilidad con la carpeta que entregamos antes, si se llegó a usar.
        anterior = canvas.transform.Find("ModoDosDiseno");
        if (anterior != null) Object.DestroyImmediate(anterior.gameObject);

        // Contenedor a pantalla completa para el diseño generado.
        // La imagen de fondo no captura eventos táctiles.
        var root = new GameObject("ModoJuegoGenerado", typeof(RectTransform), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();
        var fondo = root.GetComponent<Image>();
        fondo.sprite = individual;
        fondo.preserveAspect = false;
        fondo.raycastTarget = false;

        // Asocia al controlador existente los sprites y el texto de mensajes.
        var ui = root.AddComponent<ModoJuegoUI>();
        ui.fondoModo = fondo;
        ui.imagenIndividual = individual;
        ui.imagenPorTurnos = turnos;
        ui.mensaje = Texto(rect, "Mensaje", "", 50, 1335, 835, 65, 30);

        // Construye zonas táctiles transparentes sobre los botones dibujados
        // en la imagen y vincula las acciones persistentes de cada una.
        var volver = Zona(rect, "BtnVolver", 35, 40, 122, 140);
        var ayuda = Zona(rect, "BtnAyuda", 770, 35, 124, 142);
        var individualBtn = Zona(rect, "BtnIndividual", 45, 638, 842, 292);
        var turnosBtn = Zona(rect, "BtnPorEquipos", 45, 947, 842, 295);
        var continuar = Zona(rect, "BtnContinuar", 45, 1432, 845, 155);
        UnityEventTools.AddPersistentListener(volver.onClick, ui.Volver);
        UnityEventTools.AddPersistentListener(ayuda.onClick, ui.MostrarAyuda);
        UnityEventTools.AddPersistentListener(individualBtn.onClick, ui.SeleccionarIndividual);
        UnityEventTools.AddPersistentListener(turnosBtn.onClick, ui.SeleccionarEquipos);
        UnityEventTools.AddPersistentListener(continuar.onClick, ui.Continuar);

        // Ventana de ayuda real: una tarjeta centrada y textos dentro de ella.
        // Se reutiliza el ModoJuegoUI y su panelAyuda existente.
        var panel = new GameObject("PanelAyuda", typeof(RectTransform), typeof(Image));
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.SetParent(rect, false);
        pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one;
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        var oscurecer = panel.GetComponent<Image>();
        oscurecer.color = new Color32(23, 8, 46, 193);
        oscurecer.raycastTarget = true;

        // Superpone la tarjeta ilustrada de ayuda dentro del fondo oscurecido.
        RectTransform tarjeta = RectLocal(pr, "TarjetaAyuda", new Vector2(0, 35), new Vector2(930, 1085));
        var grafico = tarjeta.gameObject.AddComponent<Image>();
        grafico.sprite = Cargar("PanelAyudaGolosa.png");
        grafico.color = Color.white;
        grafico.raycastTarget = true;

        // Los textos se crean por separado para mantenerlos legibles y editables.
        TextoModal(tarjeta, "TituloAyuda", "¿CÓMO JUGAR?", 0, 398, 790, 98, 66, Color.white);
        TextoModal(tarjeta, "SubtituloAyuda", "Escoge tu aventura", 0, 260, 720, 48, 34, new Color32(95, 38, 160, 255));
        TextoModal(tarjeta, "IndividualTitulo", "INDIVIDUAL", 0, 155, 690, 67, 48, new Color32(25, 124, 75, 255));
        TextoModal(tarjeta, "IndividualDetalle", "Crea tu perfil, elige tu avatar y juega a tu ritmo.", 0, 71, 660, 108, 34, new Color32(52, 53, 84, 255));
        TextoModal(tarjeta, "TurnosTitulo", "POR TURNOS", 0, -117, 690, 67, 48, new Color32(102, 28, 181, 255));
        TextoModal(tarjeta, "TurnosDetalle", "Configura de 2 a 4 jugadores y compite por el mejor tiempo.", 0, -209, 670, 115, 34, new Color32(52, 53, 84, 255));

        // Configura el cierre de la ayuda utilizando el método del controlador.
        RectTransform rCerrar = RectLocal(tarjeta, "CerrarAyuda", new Vector2(0, -390), new Vector2(620, 126));
        Image botonImagen = rCerrar.gameObject.AddComponent<Image>();
        botonImagen.sprite = Cargar("BotonAyudaGolosa.png");
        botonImagen.color = Color.white;
        botonImagen.raycastTarget = true;
        var cerrar = rCerrar.gameObject.AddComponent<Button>();
        cerrar.targetGraphic = botonImagen;
        cerrar.transition = Selectable.Transition.ColorTint;
        TextoModal(rCerrar, "TextoBoton", "¡ENTENDIDO!", 0, 0, 550, 95, 48, Color.white);
        UnityEventTools.AddPersistentListener(cerrar.onClick, ui.CerrarAyuda);
        ui.panelAyuda = panel;
        pr.SetAsLastSibling();
        panel.SetActive(false);

        // Registra los cambios realizados por el generador y guarda la escena.
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Golosa AR: escena 02_ModoJuego actualizada sobre los scripts existentes; solo Individual y Por turnos.");
    }

    /// <summary>
    /// Crea un RectTransform hijo, centrado respecto de su contenedor,
    /// con la posición y las dimensiones locales solicitadas.
    /// </summary>
    /// <param name="padre">Objeto UI que contendrá al nuevo elemento.</param>
    /// <param name="nombre">Nombre del objeto dentro de la jerarquía.</param>
    /// <param name="pos">Posición local anclada al centro.</param>
    /// <param name="tam">Ancho y alto del elemento.</param>
    /// <returns>RectTransform configurado.</returns>
    private static RectTransform RectLocal(Transform padre, string nombre, Vector2 pos, Vector2 tam)
    {
        var obj = new GameObject(nombre, typeof(RectTransform));
        var r = obj.GetComponent<RectTransform>();
        r.SetParent(padre, false);
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
        return r;
    }

    /// <summary>
    /// Añade texto TextMeshPro a una ventana modal, con alineación centrada,
    /// ajuste automático de tamaño y color proporcionado.
    /// </summary>
    /// <param name="padre">Contenedor del texto.</param>
    /// <param name="nombre">Nombre del objeto de texto.</param>
    /// <param name="texto">Contenido visible.</param>
    /// <param name="x">Desplazamiento horizontal local.</param>
    /// <param name="y">Desplazamiento vertical local.</param>
    /// <param name="w">Ancho de la caja de texto.</param>
    /// <param name="h">Alto de la caja de texto.</param>
    /// <param name="tam">Tamaño máximo de la fuente.</param>
    /// <param name="color">Color del texto.</param>
    /// <returns>Componente TextMeshPro creado.</returns>
    private static TextMeshProUGUI TextoModal(Transform padre, string nombre, string texto,
        float x, float y, float w, float h, float tam, Color color)
    {
        RectTransform r = RectLocal(padre, nombre, new Vector2(x, y), new Vector2(w, h));
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.color = color;
        t.fontSize = tam;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true;
        t.fontSizeMin = 24;
        t.fontSizeMax = tam;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>
    /// Localiza un recurso gráfico y comprueba sus ajustes de importación.
    /// Si hace falta, lo configura como sprite único y lo reimporta.
    /// </summary>
    /// <param name="archivo">Nombre del PNG dentro de la carpeta de diseño.</param>
    /// <returns>Sprite cargado, o null si no existe un importador asociado.</returns>
    private static Sprite Cargar(string archivo)
    {
        string ruta = Carpeta + archivo;
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) return null;
        // Para usarlo en Image se requiere un Sprite único y sin mipmaps.
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || imp.mipmapEnabled)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    /// <summary>
    /// Crea una región pulsable invisible sobre un área de la imagen base.
    /// Convierte coordenadas del boceto a la resolución de referencia del Canvas.
    /// </summary>
    /// <param name="padre">Contenedor de la región táctil.</param>
    /// <param name="nombre">Nombre del botón en la jerarquía.</param>
    /// <param name="x">Origen horizontal en coordenadas del boceto.</param>
    /// <param name="y">Origen vertical en coordenadas del boceto.</param>
    /// <param name="w">Ancho de la zona en el boceto.</param>
    /// <param name="h">Alto de la zona en el boceto.</param>
    /// <returns>Botón con imagen transparente capaz de recibir pulsaciones.</returns>
    private static Button Zona(Transform padre, string nombre, float x, float y, float w, float h)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(padre, false);
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        r.pivot = new Vector2(.5f, .5f);
        r.sizeDelta = new Vector2(w / W * 1080f, h / H * 2340f);
        r.anchoredPosition = new Vector2(((x + w / 2) / W - .5f) * 1080f, (.5f - (y + h / 2) / H) * 2340f);
        var img = go.GetComponent<Image>();
        img.color = new Color(1, 1, 1, 0);
        img.raycastTarget = true;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;
        return btn;
    }

    /// <summary>
    /// Crea un mensaje TextMeshPro. Usa coordenadas relativas al boceto en
    /// los contenedores generados, y dimensiones locales en otros padres.
    /// </summary>
    /// <param name="padre">Contenedor del mensaje.</param>
    /// <param name="nombre">Nombre del objeto de texto.</param>
    /// <param name="mensaje">Mensaje; acepta secuencias de salto de línea.</param>
    /// <param name="x">Posición horizontal en el boceto.</param>
    /// <param name="y">Posición vertical en el boceto.</param>
    /// <param name="w">Ancho disponible.</param>
    /// <param name="h">Alto disponible.</param>
    /// <param name="tam">Tamaño máximo de fuente.</param>
    /// <returns>Componente de texto configurado.</returns>
    private static TextMeshProUGUI Texto(Transform padre, string nombre, string mensaje,
        float x, float y, float w, float h, int tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        var r = go.GetComponent<RectTransform>();
        r.SetParent(padre, false);
        // Los contenedores generados utilizan el sistema de coordenadas
        // escalado del diseño; otros contenedores usan dimensiones locales.
        if (padre.name == "ModoJuegoGenerado" || padre.name == "PanelAyuda")
        {
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            r.anchoredPosition = new Vector2(((x + w / 2) / W - .5f) * 1080f, (.5f - (y + h / 2) / H) * 2340f);
            r.sizeDelta = new Vector2(w / W * 1080f, h / H * 2340f);
        }
        else
        {
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(w, h);
        }
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = mensaje.Replace("\\n", "\n");
        t.color = Uva;
        t.fontSize = tam;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true;
        t.fontSizeMin = 22;
        t.fontSizeMax = tam;
        t.raycastTarget = false;
        return t;
    }
}
