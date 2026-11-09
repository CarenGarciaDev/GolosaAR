using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

// Control de escaneo de 03_Juego.
// No modifica AR Session, XR Origin ni los recursos de UI de la escena.
// Solo coloca la golosa cuando ARCore confirma un plano horizontal adecuado.
public class PrepararRAUI : MonoBehaviour
{
    [Header("AR Foundation / ARCore")]
    public ARSession sesion;
    public ARPlaneManager planos;
    public ARRaycastManager raycasts;
    public ARCameraManager camaraAR;
    public ARCameraBackground fondoCamaraAR;

    [Header("Alternativa: marcador impreso sobre el suelo")]
    [Tooltip("AR Tracked Image Manager del XR Origin. Debe tener asignada la biblioteca GolosaAR_Marcador.")]
    public ARTrackedImageManager imagenes;
    [Tooltip("Nombre exacto de la imagen dentro de la biblioteca XR Reference Image Library.")]
    public string nombreMarcador = "GolosaAR_Marcador";
    private bool usandoMarcador;
    private GameObject pantallaMetodo;
    private TextMeshProUGUI avisoMetodo;
    private GameObject pantallaEscaneo;
    private TextMeshProUGUI estadoEscaneo;
    private bool controladorPrincipal = true;

    // Durante los primeros instantes ARCore aún no ha entregado una imagen.
    // Con ClearFlags.Depth, el teléfono puede mantener el último fotograma
    // (por ejemplo, el fondo amarillo de la pantalla anterior).
    // Un color neutro evita ese "pantallazo" sin cubrir la cámara con un panel UI.
    private static readonly Color FondoEsperaCamara = new Color32(22, 25, 40, 255);

    /// <summary>
    /// Ajusta únicamente el fondo de la cámara AR. La imagen real sigue
    /// dibujándose mediante ARCameraBackground cuando Android la proporciona.
    /// </summary>
    /// <summary>
    /// Establece un color neutro para los fotogramas previos a la primera imagen real de ARCore. No desactiva ni sustituye ARCameraBackground.
    /// </summary>
    private void PrepararFondoCamara()
    {
        if (camaraAR == null) return;
        Camera camara = camaraAR.GetComponent<Camera>();
        if (camara == null) return;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = FondoEsperaCamara;
    }

    // Recuperacion de seguimiento: el tablero existente se recoloca sin destruir
    // GolosaJuegoUI, sus puntuaciones ni el estado de la partida.
    private bool recuperandoTablero;
    private GameObject hudRecuperacion;
    private TextMeshProUGUI avisoSeguimiento;
    private float inicioPerdidaSeguimiento = -1f;
    // ARCore puede seguir en SessionTracking aunque la golosa ya no aparezca
    // dentro del encuadre. Comprobamos también sus casillas reales en cámara.
    [Header("Detección de golosa visible")]
    [Tooltip("Tiempo de tolerancia para que un giro breve de cámara no interrumpa la partida.")]
    [Range(0.2f, 3f)] public float segundosParaPausar = 0.7f;
    private readonly List<Renderer> renderersCasillas = new List<Renderer>();
    private GolosaTablero3D tableroCacheVisibilidad;


    /// <summary>
    /// Conecta la pérdida y recuperación de seguimiento con el cronómetro
    /// de la misma instancia del tablero, sin tocar Time.timeScale.
    /// </summary>
    /// <summary>
    /// Comunica al controlador de la partida si el seguimiento requiere detener o reanudar el cronómetro, sin modificar la escala global de tiempo de Unity.
    /// </summary>
    /// <param name="pausar">Indica si el reloj debe permanecer detenido por seguimiento.</param>
    private void PausarRelojPorSeguimiento(bool pausar)
    {
        if (tablero == null) return;
        GolosaJuegoUI juego = tablero.GetComponent<GolosaJuegoUI>();
        if (juego != null) juego.EstablecerPausaPorSeguimiento(pausar);
    }
    private readonly Dictionary<Renderer, bool> visibilidadOriginal = new Dictionary<Renderer, bool>();
    [Header("Presentacion del boceto")]
    public GameObject fondoPresentacion;
    public Button botonEscanear;
    public GameObject mensajeEditor;
    public TextMeshProUGUI textoMensajeEditor;

    [Header("Interfaz sobre la camara real")]
    public GameObject hudEscaneoReal;
    public TextMeshProUGUI textoEstado;

    [Header("Sprites del HUD - asignados desde Assets/Art/JuegoUI")]
    public Sprite[] spritesJuego;


    [Header("Colocacion de la golosa")]
    [Tooltip("Distancia horizontal minima desde la camara al CENTRO de la golosa, en metros.")]
    [Min(1.2f)] public float distanciaMinima = 1.5f;
    [Tooltip("Se espera un plano suficientemente amplio para las siete filas del tablero.")]
    [Min(1.0f)] public float dimensionMinimaPlano = 1.3f;
    [Tooltip("Intervalo entre intentos de detectar un punto adecuado.")]
    [Range(0.1f, 1f)] public float intervaloBusqueda = 0.2f;
    private bool escaneando;
    private bool colocado;
    private float siguienteBusqueda;
    private GolosaTablero3D tablero;
    private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();
    private float inicioEscaneo;
    private static readonly Vector2[] PuntosMuestreo = {
        new Vector2(.50f,.50f), new Vector2(.50f,.60f), new Vector2(.50f,.70f),
        new Vector2(.40f,.55f), new Vector2(.60f,.55f),
        new Vector2(.35f,.68f), new Vector2(.65f,.68f),
        new Vector2(.50f,.80f), new Vector2(.50f,.40f),
        new Vector2(.30f,.60f), new Vector2(.70f,.60f),
        new Vector2(.45f,.75f), new Vector2(.55f,.75f)
    };

#if UNITY_ANDROID && !UNITY_EDITOR
    private PermissionCallbacks permisos;
    private bool solicitudPendiente;
    private bool permisoDenegadoPermanentemente;
#endif

    /// <summary>
    /// Se ejecuta al cargar la escena: evita que varios controladores gestionen la misma sesión, localiza el seguidor de imágenes y apaga el seguimiento hasta escoger un método.
    /// </summary>
    private void Awake()
    {
        // La escena antigua contiene dos componentes PrepararRAUI en el mismo objeto.
        // Solo el primero puede construir la interfaz y controlar la sesion.
        var controladores = GetComponents<PrepararRAUI>();
        if (controladores.Length > 1 && controladores[0] != this)
        {
            controladorPrincipal = false;
            enabled = false;
            return;
        }
        if (controladores.Length > 1)
            Debug.LogWarning("Golosa AR: hay dos PrepararRAUI en PrepararRAGenerado. Se desactivan los duplicados. Puedes retirar el segundo componente del Inspector.");

        if (imagenes == null)
            imagenes = FindFirstObjectByType<ARTrackedImageManager>(FindObjectsInactive.Include);
        PrepararFondoCamara();
        ApagarSeguimiento();
    }

    /// <summary>
    /// Construye las interfaces de selección, escaneo y recuperación al iniciar la escena; deja visible únicamente la selección del método.
    /// </summary>
    private void Start()
    {
        if (!controladorPrincipal) return;
        // Todos los paneles viejos estan dentro de PrepararRAGenerado.
        // Se apagan para que NO se mezclen con la seleccion ni con la camara.
        OcultarPanelesAntiguos();
        ConstruirPantallaMetodo();
        ConstruirHUDDeEscaneo();
        ConstruirHUDRecuperacion();
        pantallaMetodo.SetActive(true);
        pantallaEscaneo.SetActive(false);
        hudRecuperacion.SetActive(false);
    }

    /// <summary>
    /// Oculta las interfaces preexistentes de escaneo para que no se superpongan con los controles construidos en tiempo de ejecución.
    /// </summary>
    private void OcultarPanelesAntiguos()
    {
        foreach (Transform hijo in transform)
            hijo.gameObject.SetActive(false);
        if (fondoPresentacion != null) fondoPresentacion.SetActive(false);
        if (hudEscaneoReal != null) hudEscaneoReal.SetActive(false);
        if (botonEscanear != null) botonEscanear.gameObject.SetActive(false);
        if (mensajeEditor != null) mensajeEditor.SetActive(false);
    }

    /// <summary>
    /// Deshabilita de forma coordinada la sesión y los gestores de realidad aumentada cuando no debe realizarse un nuevo seguimiento.
    /// </summary>
    private void ApagarSeguimiento()
    {
        if (imagenes != null) imagenes.enabled = false;
        if (planos != null) planos.enabled = false;
        if (raycasts != null) raycasts.enabled = false;
        if (fondoCamaraAR != null) fondoCamaraAR.enabled = false;
        if (camaraAR != null) camaraAR.enabled = false;
        if (sesion != null) sesion.enabled = false;
    }

    /// <summary>
    /// Crea un RectTransform centrado dentro de un contenedor y define la posición y dimensiones iniciales que utilizará la interfaz.
    /// </summary>
    /// <param name="nombre">Nombre del objeto de interfaz.</param>
    /// <param name="padre">Transformación que contendrá al nuevo objeto.</param>
    /// <param name="centro">Posición local del centro del elemento.</param>
    /// <param name="tamano">Dimensiones del elemento.</param>
    /// <returns>RectTransform creado y asociado a su padre.</returns>
    private static RectTransform CrearRect(string nombre, Transform padre,
        Vector2 centro, Vector2 tamano)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = centro;
        r.sizeDelta = tamano;
        return r;
    }

    /// <summary>
    /// Obtiene un recurso gráfico del modo de colocación desde Resources; cuando solo hay una textura, crea un Sprite utilizable por UI.
    /// </summary>
    /// <param name="nombre">Nombre del recurso sin extensión dentro de GolosaMetodo.</param>
    /// <returns>Sprite encontrado o null si no existe el recurso.</returns>
    private static Sprite ObtenerSprite(string nombre)
    {
        Sprite sprite = Resources.Load<Sprite>("GolosaMetodo/" + nombre);
        if (sprite != null) return sprite;
        Texture2D textura = Resources.Load<Texture2D>("GolosaMetodo/" + nombre);
        return textura == null ? null : Sprite.Create(textura,
            new Rect(0, 0, textura.width, textura.height), new Vector2(.5f, .5f));
    }

    /// <summary>
    /// Crea un elemento Image y aplica el recurso gráfico solicitado o un color alternativo cuando no está disponible.
    /// </summary>
    /// <param name="nombre">Identificador del objeto de UI.</param>
    /// <param name="padre">Contenedor de la imagen.</param>
    /// <param name="recurso">Nombre del recurso gráfico a cargar.</param>
    /// <param name="posicion">Posición centrada local.</param>
    /// <param name="tamano">Dimensiones de la imagen.</param>
    /// <param name="respaldo">Color utilizado como alternativa al recurso.</param>
    /// <returns>Componente Image configurado.</returns>
    private static Image CrearImagen(string nombre, Transform padre, string recurso,
        Vector2 posicion, Vector2 tamano, Color respaldo)
    {
        var r = CrearRect(nombre, padre, posicion, tamano);
        var img = r.gameObject.AddComponent<Image>();
        var tex = ObtenerSprite(recurso);
        if (tex != null) { img.sprite = tex; img.type = Image.Type.Simple; img.preserveAspect = false; }
        else img.color = respaldo;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>
    /// Genera un texto TextMeshPro de interfaz con tamaño adaptable y ajuste de líneas para diferentes resoluciones.
    /// </summary>
    /// <param name="nombre">Nombre del objeto de texto.</param>
    /// <param name="padre">Contenedor de la UI.</param>
    /// <param name="texto">Contenido inicial mostrado.</param>
    /// <param name="posicion">Posición del texto.</param>
    /// <param name="tamano">Área disponible.</param>
    /// <param name="tamFuente">Tamaño máximo de fuente.</param>
    /// <param name="color">Color de los caracteres.</param>
    /// <param name="estilo">Estilo tipográfico opcional.</param>
    /// <returns>Referencia al texto creado.</returns>
    private static TextMeshProUGUI CrearTexto(string nombre, Transform padre,
        string texto, Vector2 posicion, Vector2 tamano, float tamFuente,
        Color color, FontStyles estilo = FontStyles.Bold)
    {
        var r = CrearRect(nombre, padre, posicion, tamano);
        var tmp = r.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = texto; tmp.fontSize = tamFuente; tmp.color = color;
        tmp.fontStyle = estilo; tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true; tmp.fontSizeMin = tamFuente * .77f;
        tmp.fontSizeMax = tamFuente;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>
    /// Crea un botón de interfaz con estilo visual y registra la acción que se ejecutará al pulsarlo.
    /// </summary>
    /// <param name="nombre">Identificador del botón.</param>
    /// <param name="padre">Contenedor del botón.</param>
    /// <param name="texto">Etiqueta mostrada.</param>
    /// <param name="posicion">Posición local.</param>
    /// <param name="tamano">Área interactiva.</param>
    /// <param name="colorFondo">Color alternativo de fondo.</param>
    /// <param name="colorTexto">Color del rótulo.</param>
    /// <param name="accion">Función asociada al evento de clic.</param>
    /// <param name="recurso">Sprite opcional para el fondo.</param>
    /// <param name="fuente">Tamaño de letra del rótulo.</param>
    /// <returns>Botón con su evento configurado.</returns>
    private static Button CrearBoton(string nombre, Transform padre, string texto,
        Vector2 posicion, Vector2 tamano, Color colorFondo, Color colorTexto,
        UnityEngine.Events.UnityAction accion, string recurso = null, int fuente = 38)
    {
        var r = CrearRect(nombre, padre, posicion, tamano);
        var bg = r.gameObject.AddComponent<Image>();
        bg.color = colorFondo;
        if (!string.IsNullOrEmpty(recurso))
        {
            var sprite = ObtenerSprite(recurso);
            if (sprite != null) { bg.sprite = sprite; bg.type = Image.Type.Simple; }
        }
        var boton = r.gameObject.AddComponent<Button>();
        boton.targetGraphic = bg;
        boton.onClick.AddListener(accion);
        var colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colores.pressedColor = new Color(.85f, .83f, .96f, 1f);
        boton.colors = colores;
        CrearTexto("Texto", r, texto, Vector2.zero, tamano - new Vector2(24, 14), fuente, colorTexto);
        return boton;
    }

    /// <summary>
    /// Construye la pantalla de selección usando el arte final aprobado.
    /// La imagen es puramente visual: las interacciones se resuelven mediante
    /// zonas táctiles transparentes, sin duplicar textos o botones dibujados.
    /// La resolución de referencia coincide con la relación de aspecto del arte.
    /// </summary>
    /// <summary>
    /// Monta la interfaz para elegir la colocación por plano o marcador, utilizando una imagen de fondo y zonas táctiles proporcionales.
    /// </summary>
    private void ConstruirPantallaMetodo()
    {
        if (pantallaMetodo != null) return;

        var root = new GameObject("SeleccionMetodoColocacion",
            typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        pantallaMetodo = root;

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(941, 1672);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // La composición final incluye logotipo, tarjetas y rótulos de los botones.
        // No se crean copias de estos elementos para evitar superposiciones.
        var fondo = root.AddComponent<Image>();
        fondo.sprite = ObtenerSprite("PantallaMetodoFinal");
        fondo.color = Color.white;
        fondo.type = Image.Type.Simple;
        fondo.preserveAspect = false;
        fondo.raycastTarget = true;

        // Los rectángulos interactivos coinciden con los botones visibles
        // del arte. Se amplía la zona de contacto dentro de cada tarjeta.
        CrearZonaTactil("ElegirRealidadAumentada", root.transform,
            new Vector2(0.260f, 0.335f), new Vector2(0.435f, 0.165f),
            () => ElegirMetodo(false));
        CrearZonaTactil("ElegirCodigoQR", root.transform,
            new Vector2(0.740f, 0.335f), new Vector2(0.435f, 0.165f),
            () => ElegirMetodo(true));
        CrearZonaTactil("Volver", root.transform,
            new Vector2(0.500f, 0.160f), new Vector2(0.540f, 0.090f), Volver);

        // Aviso dinámico: solo aparece cuando falta configurar el marcador
        // o cuando la plataforma no permite iniciar el método solicitado.
        var panelAviso = CrearRect("PanelAvisoMetodo", root.transform,
            new Vector2(0, -478), new Vector2(820, 150));
        var imagenAviso = panelAviso.gameObject.AddComponent<Image>();
        imagenAviso.color = new Color32(56, 12, 108, 245);
        avisoMetodo = CrearTexto("TextoAviso", panelAviso, "", Vector2.zero,
            new Vector2(770, 120), 29, Color.white, FontStyles.Normal);
        panelAviso.gameObject.SetActive(false);
    }

    /// <summary>
    /// Define una zona táctil proporcional a la pantalla para mantener la
    /// interacción alineada al gráfico en diferentes resoluciones Android.
    /// El gráfico transparente recibe eventos sin dibujar elementos nuevos.
    /// </summary>
    /// <summary>
    /// Añade una región interactiva prácticamente transparente que coincide con una acción dibujada dentro de la imagen principal.
    /// </summary>
    /// <param name="nombre">Nombre de la zona.</param>
    /// <param name="padre">Contenedor gráfico.</param>
    /// <param name="centroNormalizado">Centro de la región en coordenadas normalizadas.</param>
    /// <param name="tamanoNormalizado">Extensión de la región en coordenadas normalizadas.</param>
    /// <param name="accion">Acción al pulsarla.</param>
    private static void CrearZonaTactil(string nombre, Transform padre,
        Vector2 centroNormalizado, Vector2 tamanoNormalizado,
        UnityEngine.Events.UnityAction accion)
    {
        var objeto = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        objeto.transform.SetParent(padre, false);
        var rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = centroNormalizado - tamanoNormalizado * 0.5f;
        rect.anchorMax = centroNormalizado + tamanoNormalizado * 0.5f;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var imagen = objeto.GetComponent<Image>();
        imagen.color = new Color(1f, 1f, 1f, 0.001f);
        imagen.raycastTarget = true;
        var boton = objeto.GetComponent<Button>();
        boton.targetGraphic = imagen;
        boton.transition = Selectable.Transition.None;
        boton.onClick.AddListener(accion);
    }

    /// <summary>
    /// Presenta un mensaje de configuración sin activar los paneles antiguos
    /// de la escena, que provocaban contenido duplicado sobre la selección.
    /// </summary>
    /// <summary>
    /// Presenta avisos de configuración o incompatibilidad en la propia pantalla de selección sin activar paneles antiguos.
    /// </summary>
    /// <param name="mensaje">Contenido del aviso para el usuario.</param>
    private void AvisarEnSeleccion(string mensaje)
    {
        if (avisoMetodo == null) return;
        avisoMetodo.text = mensaje;
        avisoMetodo.transform.parent.gameObject.SetActive(true);
    }

    /// <summary>
    /// Crea la capa de instrucciones sobre la cámara y el botón Cambiar método, sin bloquear la imagen real de ARCore.
    /// </summary>
    private void ConstruirHUDDeEscaneo()
    {
        if (pantallaEscaneo != null) return;
        var root = new GameObject("HUD_SoloEscaneoAR", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        pantallaEscaneo = root;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 610;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2340);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;

        CrearImagen("TarjetaInstrucciones", root.transform, "PanelConsejo",
            new Vector2(0, 885), new Vector2(1010, 310), new Color32(255, 248, 232, 255));
        CrearTexto("TituloEscaneo", root.transform, "¡PREPARA TU GOLOSA!",
            new Vector2(0, 972), new Vector2(905, 88), 53, new Color32(76, 20, 139, 255));
        estadoEscaneo = CrearTexto("EstadoEscaneo", root.transform,
            "Mueve el celular lentamente y busca un suelo despejado.",
            new Vector2(0, 856), new Vector2(900, 146), 36, new Color32(75, 25, 130, 255), FontStyles.Normal);
        // BotonMorado no está en Assets/Resources/GolosaMetodo de esta
        // versión. Usamos colores de respaldo contrastados en lugar de
        // blanco sobre blanco (que hacía desaparecer el texto).
        CrearBoton("CambiarMetodo", root.transform, "CAMBIAR MÉTODO",
            new Vector2(0, -989), new Vector2(710, 116),
            new Color32(92, 30, 157, 255), Color.white,
            RegresarASeleccion, "BotonMorado", 42);
    }

    /// <summary>
    /// Control flotante disponible después de colocar la golosa. Se mantiene
    /// accesible aunque el tablero haya salido del encuadre o ARCore pierda
    /// temporalmente la referencia espacial.
    /// </summary>
    /// <summary>
    /// Construye los controles de recuperación para una partida ya colocada, manteniéndolos independientes del tablero 3D.
    /// </summary>
    private void ConstruirHUDRecuperacion()
    {
        if (hudRecuperacion != null) return;

        hudRecuperacion = new GameObject("HUD_RecuperarGolosa",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = hudRecuperacion.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 620;

        CanvasScaler scaler = hudRecuperacion.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2340);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;

        CrearBoton("BotonRecuperarGolosa", hudRecuperacion.transform,
            "RECUPERAR GOLOSA", new Vector2(0, 664), new Vector2(530, 104),
            Color.white, new Color32(68, 18, 125, 255),
            RecuperarGolosa, "BotonAmarillo", 37);

        avisoSeguimiento = CrearTexto("AvisoPerdidaSeguimiento",
            hudRecuperacion.transform, "", new Vector2(0, 520),
            new Vector2(920, 145), 34, Color.white, FontStyles.Bold);
        avisoSeguimiento.gameObject.SetActive(false);
    }

    /// <summary>
    /// Reactiva el método seleccionado sin reiniciar la sesión AR ni crear
    /// una nueva partida. El tablero y sus controles permanecen en memoria
    /// hasta que una superficie o un marcador válidos permitan reubicarlo.
    /// </summary>
    /// <summary>
    /// Inicia una nueva búsqueda de suelo o marcador para reubicar la golosa actual sin eliminar la partida ni sus estadísticas.
    /// </summary>
    public void RecuperarGolosa()
    {
        if (escaneando || tablero == null) return;

        recuperandoTablero = true;
        inicioPerdidaSeguimiento = -1f;
        // Pausar de inmediato durante la búsqueda manual de suelo o marcador.
        PausarRelojPorSeguimiento(true);
        OcultarGeometriaTablero();
        if (hudRecuperacion != null) hudRecuperacion.SetActive(false);
        // El mismo flujo valida planos (AR) o imágenes (QR).
        EmpezarAR(usandoMarcador);
        CambiarEstado(usandoMarcador
            ? "Reencuadra el marcador QR para recuperar tu golosa."
            : "Apunta a un suelo despejado para recuperar tu golosa.");
    }

    /// <summary>
    /// Durante la recolocación se ocultan únicamente las mallas 3D.
    /// Desactivar el GameObject completo borraría de la interfaz activa
    /// los controles y las rutinas que conservan el progreso del juego.
    /// </summary>
    /// <summary>
    /// Deshabilita temporalmente los renderizadores 3D y recuerda sus estados previos mientras se busca una ubicación nueva.
    /// </summary>
    private void OcultarGeometriaTablero()
    {
        visibilidadOriginal.Clear();
        if (tablero == null) return;
        foreach (Renderer renderer in tablero.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            visibilidadOriginal[renderer] = renderer.enabled;
            renderer.enabled = false;
        }
    }

    /// <summary>
    /// Restablece los estados originales de los renderizadores tras completar la recuperación del tablero.
    /// </summary>
    private void RestaurarGeometriaTablero()
    {
        foreach (KeyValuePair<Renderer, bool> elemento in visibilidadOriginal)
        {
            if (elemento.Key != null) elemento.Key.enabled = elemento.Value;
        }
        visibilidadOriginal.Clear();
    }

    /// <summary>
    /// Reposiciona la instancia original usando el mismo criterio geométrico
    /// que GolosaTablero3D.Crear. Se conserva la escala ajustada por el jugador.
    /// </summary>
    /// <summary>
    /// Mueve y orienta la instancia existente del tablero hacia la nueva ubicación, preservando su escala y la partida activa.
    /// </summary>
    /// <param name="centro">Punto de colocación detectado.</param>
    /// <param name="camara">Transformación de la cámara empleada para orientar el tablero.</param>
    private void ReposicionarTablero(Vector3 centro, Transform camara)
    {
        if (tablero == null) return;
        Transform t = tablero.transform;
        t.position = centro + Vector3.up * .018f;
        Vector3 adelante = camara != null ? centro - camara.position : Vector3.forward;
        adelante.y = 0f;
        if (adelante.sqrMagnitude < .01f && camara != null)
        {
            adelante = camara.forward;
            adelante.y = 0f;
        }
        if (adelante.sqrMagnitude < .01f) adelante = Vector3.forward;
        t.rotation = Quaternion.LookRotation(adelante.normalized, Vector3.up);
        RestaurarGeometriaTablero();
        recuperandoTablero = false;
    }

    /// <summary>
    /// Comprueba si alguna casilla renderizable del tablero entra en el campo
    /// visual de la cámara AR. No se usan Canvas, avatar ni botones para evitar
    /// falsos positivos al estar visible solamente la interfaz.
    /// </summary>
    /// <summary>
    /// Comprueba si alguna malla de casillas del tablero se encuentra dentro del volumen de visión de la cámara AR; excluye la interfaz y los avatares.
    /// </summary>
    /// <returns>True si una casilla activa entra en el campo visual; false en caso contrario.</returns>
    private bool GolosaDentroDeLaCamara()
    {
        if (tablero == null || camaraAR == null) return false;
        Camera camara = camaraAR.GetComponent<Camera>();
        if (camara == null || !camara.isActiveAndEnabled) return false;

        if (tableroCacheVisibilidad != tablero || renderersCasillas.Count == 0)
        {
            tableroCacheVisibilidad = tablero;
            renderersCasillas.Clear();
            for (int i = 1; i <= 10; i++)
            {
                Transform casilla = tablero.transform.Find("Casilla_" + i);
                if (casilla == null) continue;
                renderersCasillas.AddRange(casilla.GetComponentsInChildren<Renderer>(true));
            }
        }

        // Sin Renderer identificable no afirmamos que el tablero se ve.
        if (renderersCasillas.Count == 0) return false;
        Plane[] planosCamara = GeometryUtility.CalculateFrustumPlanes(camara);
        foreach (Renderer renderer in renderersCasillas)
        {
            if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                GeometryUtility.TestPlanesAABB(planosCamara, renderer.bounds))
                return true;
        }
        return false;
    }

    /// <summary>
    /// ARCore puede conservar seguimiento del mundo cuando el usuario deja
    /// de apuntar a la golosa. El juego se pausa también en ese caso.
    /// Se necesita una recuperación estable antes de reanudar el reloj.
    /// </summary>
    /// <summary>
    /// Distingue el seguimiento activo de una golosa realmente visible; después del margen de tolerancia pausa el tiempo y muestra la instrucción de recuperación.
    /// </summary>
    private void ComprobarSeguimiento()
    {
        bool rastreoAR = ARSession.state == ARSessionState.SessionTracking;
        bool tableroEnPantalla = rastreoAR && GolosaDentroDeLaCamara();
        if (tableroEnPantalla && !recuperandoTablero)
        {
            inicioPerdidaSeguimiento = -1f;
            PausarRelojPorSeguimiento(false);
            if (avisoSeguimiento != null) avisoSeguimiento.gameObject.SetActive(false);
            return;
        }

        if (inicioPerdidaSeguimiento < 0f) inicioPerdidaSeguimiento = Time.unscaledTime;
        if (Time.unscaledTime - inicioPerdidaSeguimiento < segundosParaPausar) return;

        PausarRelojPorSeguimiento(true);
        if (avisoSeguimiento == null) return;
        avisoSeguimiento.text = !rastreoAR
            ? "Seguimiento AR perdido. Tiempo pausado. Apunta al suelo o pulsa RECUPERAR GOLOSA."
            : "Golosa fuera de cámara. Tiempo pausado. Apunta de nuevo al tablero o pulsa RECUPERAR GOLOSA.";
        avisoSeguimiento.gameObject.SetActive(true);
    }

    /// <summary>
    /// Interrumpe el escaneo actual y vuelve a la elección de método conservando el tablero existente cuando se está recuperando una partida.
    /// </summary>
    private void RegresarASeleccion()
    {
        escaneando = false;
        colocado = false;
        // Si el usuario está recuperando su partida, conservamos la
        // instancia existente al cambiar entre el método AR y el método QR.
        usandoMarcador = false;
        ApagarSeguimiento();
        OcultarPanelesAntiguos();
        if (pantallaEscaneo != null) pantallaEscaneo.SetActive(false);
        if (pantallaMetodo != null) pantallaMetodo.SetActive(true);
        if (avisoMetodo != null)
        {
            avisoMetodo.text = "";
            avisoMetodo.transform.parent.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Valida el método de colocación elegido y gestiona permiso de cámara o avisos de incompatibilidad según la plataforma.
    /// </summary>
    /// <param name="marcador">True para reconocimiento del marcador; false para detección de plano.</param>
    private void ElegirMetodo(bool marcador)
    {
        if (marcador && (imagenes == null || imagenes.referenceLibrary == null))
        {
            AvisarEnSeleccion("El modo QR requiere configurar AR Tracked Image Manager y su biblioteca en XR Origin.");
            return;
        }
#if UNITY_EDITOR
        AvisarEnSeleccion("La colocación real se prueba en un Android compatible con ARCore.");
#elif UNITY_ANDROID
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
            EmpezarAR(marcador);
        else
        {
            metodoPendienteQR = marcador;
            SolicitarCamara(true);
        }
#else
        AvisarEnSeleccion("Necesitas un celular Android compatible con ARCore.");
#endif
    }

    private bool metodoPendienteQR;

    /// <summary>
    /// Punto de entrada para iniciar la búsqueda de un plano horizontal con las comprobaciones correspondientes de plataforma y permiso.
    /// </summary>
    public void BuscarSuperficie()
    {
        if (escaneando || colocado) return;
        metodoPendienteQR = false;
#if UNITY_EDITOR
        MostrarAviso("La deteccion de suelo real se prueba en un Android compatible con ARCore.");
#elif UNITY_ANDROID
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
            EmpezarAR();
        else
            SolicitarCamara(true);
#else
        MostrarAviso("Esta funcion requiere un telefono compatible con ARCore.");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // El parametro indica si se pulso ESCANEAR: al conceder el permiso
    // podemos iniciar la sesion inmediatamente en ese caso.
    /// <summary>
    /// Solicita a Android el permiso de cámara y gestiona los tres posibles resultados del diálogo del sistema.
    /// </summary>
    /// <param name="iniciarTrasAutorizar">True para comenzar el escaneo inmediatamente después de conceder el permiso.</param>
    private void SolicitarCamara(bool iniciarTrasAutorizar)
    {
        if (solicitudPendiente) return;
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            if (iniciarTrasAutorizar) EmpezarAR(metodoPendienteQR);
            return;
        }
        if (permisoDenegadoPermanentemente)
        {
            MostrarAviso("La camara esta bloqueada. Abre Ajustes > Aplicaciones > Golosa AR > Permisos > Camara y permitela.");
            return;
        }

        MostrarAviso("Golosa AR necesita tu camara para detectar el suelo y mostrar el tablero. Acepta la solicitud de Android.");
        solicitudPendiente = true;
        permisos = new PermissionCallbacks();
        permisos.PermissionGranted += permiso =>
        {
            solicitudPendiente = false;
            permisoDenegadoPermanentemente = false;
            if (iniciarTrasAutorizar)
                EmpezarAR(metodoPendienteQR);
            else
            {
                if (mensajeEditor != null) mensajeEditor.SetActive(false);
                Debug.Log("Golosa AR: permiso de camara concedido. Pulsa ESCANEAR.");
            }
        };
        permisos.PermissionDenied += permiso =>
        {
            solicitudPendiente = false;
            PermisoDenegado(false);
        };
        permisos.PermissionDeniedAndDontAskAgain += permiso =>
        {
            solicitudPendiente = false;
            PermisoDenegado(true);
        };
        Permission.RequestUserPermission(Permission.Camera, permisos);
    }
#endif

    /// <summary>
    /// Informa sobre la denegación del permiso y orienta al usuario para concederlo nuevamente o desde Ajustes.
    /// </summary>
    /// <param name="permanente">True si el sistema no volverá a mostrar el diálogo de autorización.</param>
    private void PermisoDenegado(bool permanente)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        permisoDenegadoPermanentemente = permanente;
#endif
        MostrarAviso(permanente
            ? "La camara esta bloqueada. Ve a Ajustes > Aplicaciones > Golosa AR > Permisos > Camara."
            : "No se autorizo la camara. Pulsa ESCANEAR para volver a solicitarla.");
    }

    // Util para conectar opcionalmente un boton de la UI a Ajustes de Android.
    /// <summary>
    /// Abre los ajustes de Golosa AR en Android para que el usuario pueda revisar los permisos de cámara.
    /// </summary>
    public void AbrirAjustesAplicacion()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var actividad = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS"))
            using (var uriClass = new AndroidJavaClass("android.net.Uri"))
            using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", "package:" + Application.identifier))
            {
                intent.Call<AndroidJavaObject>("setData", uri);
                actividad.Call("startActivity", intent);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudieron abrir los ajustes: " + e.Message);
            MostrarAviso("Abre Ajustes del telefono > Aplicaciones > Golosa AR > Permisos > Camara.");
        }
#else
        MostrarAviso("Los permisos se configuran desde Ajustes del dispositivo Android.");
#endif
    }

    /// <summary>
    /// Al regresar a la aplicación desde Ajustes, verifica si el permiso de cámara fue concedido y limpia avisos obsoletos.
    /// </summary>
    /// <param name="tieneFoco">Indica si la aplicación recuperó el foco.</param>
    private void OnApplicationFocus(bool tieneFoco)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Si el usuario vuelve desde Ajustes, vuelve a comprobar el permiso.
        if (tieneFoco && Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            permisoDenegadoPermanentemente = false;
            if (!escaneando && !colocado && mensajeEditor != null)
                mensajeEditor.SetActive(false);
        }
#endif
    }

    /// <summary>
    /// Inicializa el flujo de escaneo del método elegido, activa los gestores necesarios y conserva la partida si se está recolocando el tablero.
    /// </summary>
    /// <param name="marcador">True para buscar la imagen registrada; false para detectar el suelo.</param>
    private void EmpezarAR(bool marcador = false)
    {
        if (sesion == null || planos == null || raycasts == null ||
            camaraAR == null || fondoCamaraAR == null)
        {
            MostrarAviso("Falta conectar AR Session, XR Origin o la camara AR en 03_Juego.");
            return;
        }

        // En recuperación se reutiliza la instancia: destruirla también
        // destruiría el componente GolosaJuegoUI y el progreso en memoria.
        if (!recuperandoTablero && tablero != null) Destroy(tablero.gameObject);
        if (!recuperandoTablero) tablero = null;
        escaneando = true;
        colocado = false;
        usandoMarcador = marcador;
        // Limites efectivos: los valores serializados del Inspector pueden conservar
        // los antiguos 2.65 m y 2.5 m aunque el script tenga otros valores por defecto.
        // Reducir estos umbrales evita esperar un plano excesivamente grande.
        siguienteBusqueda = Time.time + 0.15f;
        inicioEscaneo = Time.time;

        // Oculta todo el boceto y los controles viejos ANTES de abrir la camara.
        OcultarPanelesAntiguos();
        if (pantallaMetodo != null) pantallaMetodo.SetActive(false);
        if (hudRecuperacion != null) hudRecuperacion.SetActive(false);
        if (pantallaEscaneo != null) pantallaEscaneo.SetActive(true);
        // Al entrar en 03_Juego ya se inicia una sesion nueva.
        // Evitamos Reset() al iniciar el escaneo porque reinicia el tracking
        // y obliga a ARCore a volver a descubrir el entorno.
        // Eliminar el fotograma amarillo residual antes de habilitar ARCore.
        // La cámara empieza a renderizar sin requerir otra pulsación.
        PrepararFondoCamara();
        sesion.enabled = true;
        camaraAR.enabled = true;
        fondoCamaraAR.enabled = true;
        planos.requestedDetectionMode = PlaneDetectionMode.Horizontal;
        planos.enabled = !usandoMarcador;
        raycasts.enabled = !usandoMarcador;
        // El seguimiento de imagen convive con el de planos. Solo se usa
        // para colocar el tablero cuando se selecciona MODO MARCADOR.
        if (imagenes != null)
        {
            if (imagenes.referenceLibrary != null) imagenes.enabled = usandoMarcador;
            else Debug.LogWarning("Golosa AR: falta XR Reference Image Library en ARTrackedImageManager.");
        }
        CambiarEstado(usandoMarcador
            ? "Apunta al marcador completo, impreso y plano sobre el suelo."
            : "Busca suelo libre y bien iluminado. Mueve el celular despacio.");
    }

    /// <summary>
    /// Supervisa cada fotograma: comprueba la visibilidad de una golosa ya colocada o realiza intentos de detección mientras el escaneo está activo.
    /// </summary>
    private void Update()
    {
        // Aunque la golosa ya esté colocada, ARCore puede perder el tracking
        // por movimientos bruscos. El botón de recuperación sigue disponible.
        if (colocado && !escaneando)
        {
            ComprobarSeguimiento();
            return;
        }
        if (!escaneando || Time.time < siguienteBusqueda) return;
        siguienteBusqueda = Time.time + intervaloBusqueda;

        if (ARSession.state == ARSessionState.Unsupported)
        {
            CambiarEstado("Este dispositivo no admite ARCore o requiere actualizar sus servicios de RA.");
            escaneando = false;
            return;
        }

        if (ARSession.state != ARSessionState.SessionTracking)
        {
            CambiarEstado("Activando cámara AR... Cuando aparezca el entorno, apunta a un suelo iluminado.");
            return;
        }
        if (usandoMarcador)
        {
            BuscarMarcadorRegistrado();
            return;
        }
        if (planos == null || raycasts == null || camaraAR == null) return;

        // Buscar impactos reales sobre el poligono detectado en distintos puntos.
        // La verificacion de dimensiones es aproximada; no es una garantia de
        // que toda la golosa quepa dentro del contorno del plano.
        Transform cam = camaraAR.transform;
        // Capamos los valores antiguos guardados en la escena (Inspector).
        float distanciaRequerida = Mathf.Clamp(distanciaMinima, 1.2f, 1.5f);
        float largoRequerido = Mathf.Clamp(dimensionMinimaPlano, 1.0f, 1.3f);
        bool huboPlano = false;
        bool huboPlanoLejano = false;
        foreach (Vector2 uv in PuntosMuestreo)
        {
            Hits.Clear();
            Vector2 pantalla = new Vector2(Screen.width * uv.x, Screen.height * uv.y);
            if (!raycasts.Raycast(pantalla, Hits, TrackableType.PlaneWithinPolygon)) continue;
            foreach (ARRaycastHit hit in Hits)
            {
                ARPlane plano = planos.GetPlane(hit.trackableId);
                if (plano == null || plano.trackingState != TrackingState.Tracking ||
                    plano.alignment != PlaneAlignment.HorizontalUp) continue;
                huboPlano = true;

                Vector3 distancia = hit.pose.position - cam.position;
                distancia.y = 0f;
                if (distancia.magnitude < distanciaRequerida) continue;
                huboPlanoLejano = true;

                float largo = Mathf.Max(plano.size.x, plano.size.y);
                float ancho = Mathf.Min(plano.size.x, plano.size.y);
                if (largo < largoRequerido || ancho < .55f) continue;

                ColocarTablero(hit.pose.position, cam);
                if (pantallaEscaneo != null) pantallaEscaneo.SetActive(false);
                CambiarEstado("¡Golosa lista! Ajusta el tamano y comienza.");
                Debug.Log("Golosa AR: plano confirmado; distancia " +
                          distancia.magnitude.ToString("F2") + " m.");
                return;
            }
        }
        if (Time.time - inicioEscaneo > 12f && !huboPlano)
            CambiarEstado("ARCore aun no reconoce un plano. Apunta al suelo y mueve el telefono lentamente en un arco.");
        else if (!huboPlano)
            CambiarEstado("Todavia no se detecta suelo horizontal. Mueve lentamente el celular.");
        else if (!huboPlanoLejano)
            CambiarEstado("Suelo detectado, pero esta muy cerca. Apunta mas adelante.");
        else
            CambiarEstado("Suelo detectado. Mueve un poco la camara para ampliar el plano.");
    }

    // Este metodo se usa tanto en deteccion de suelo como con imagen QR.
    // Reutiliza GolosaTablero3D.Crear y GolosaJuegoUI ya existentes.
    /// <summary>
    /// Crea el tablero por primera vez o reposiciona el existente, conecta los controles y actualiza la interfaz de recuperación.
    /// </summary>
    /// <param name="centro">Coordenada del suelo o marcador donde se colocará el tablero.</param>
    /// <param name="cam">Cámara AR utilizada como referencia de orientación.</param>
    private void ColocarTablero(Vector3 centro, Transform cam)
    {
        if (colocado) return;
        if (recuperandoTablero && tablero != null)
        {
            // Restaurar exactamente la misma partida, sin reconstruir sus UI.
            ReposicionarTablero(centro, cam);
            Debug.Log("Golosa AR: tablero existente reubicado; progreso conservado.");
        }
        else
        {
            tablero = GolosaTablero3D.Crear(centro, cam);
            GolosaJuegoUI controles = tablero.GetComponent<GolosaJuegoUI>();
            if (controles != null) controles.ConfigurarSprites(spritesJuego);
        }
        recuperandoTablero = false;
        inicioPerdidaSeguimiento = -1f;
        // Al crear o reposicionar el tablero cambian las mallas que se verifican.
        renderersCasillas.Clear();
        tableroCacheVisibilidad = null;
        colocado = true;
        // Al volver a colocar la misma golosa, continuar desde el segundo
        // conservado (si la partida ya había iniciado).
        // Se reanuda en ComprobarSeguimiento al confirmar visibilidad real.
        PausarRelojPorSeguimiento(true);
        if (hudRecuperacion != null) hudRecuperacion.SetActive(true);
        if (avisoSeguimiento != null) avisoSeguimiento.gameObject.SetActive(false);
        escaneando = false;
        if (pantallaEscaneo != null) pantallaEscaneo.SetActive(false);
        if (imagenes != null) imagenes.enabled = false;
        if (planos != null) planos.enabled = false;
        if (raycasts != null) raycasts.enabled = false;
    }

    /// <summary>
    /// Busca una imagen de referencia detectada y orientada horizontalmente, y utiliza su posición para colocar o recuperar la golosa.
    /// </summary>
    private void BuscarMarcadorRegistrado()
    {
        if (imagenes == null || imagenes.referenceLibrary == null)
        {
            CambiarEstado("Falta configurar la biblioteca de imagenes QR en XR Origin.");
            return;
        }
        foreach (ARTrackedImage encontrada in imagenes.trackables)
        {
            if (encontrada == null || encontrada.trackingState != TrackingState.Tracking)
                continue;
            if (encontrada.referenceImage.name != nombreMarcador) continue;
            // La imagen debe estar impresa sobre el suelo, boca arriba.
            // No usamos decodificacion QR: ARCore aporta posicion y orientacion
            // a partir de la imagen de referencia, sin esperar un plano grande.
            Vector3 normal = encontrada.transform.forward;
            if (Mathf.Abs(Vector3.Dot(normal.normalized, Vector3.up)) < 0.60f)
            {
                CambiarEstado("Pon el marcador QR horizontal sobre el suelo.");
                continue;
            }
            ColocarTablero(encontrada.transform.position, camaraAR.transform);
            CambiarEstado("Marcador reconocido: golosa colocada.");
            Debug.Log("Golosa AR: tablero colocado con marcador QR.");
            return;
        }
        CambiarEstado("Apunta al marcador completo, impreso y plano sobre el suelo.");
    }

    /// <summary>
    /// Muestra las recomendaciones básicas para detectar una superficie adecuada.
    /// </summary>
    public void MostrarAyuda()
    {
        MostrarAviso("Busca un suelo despejado y mueve el celular lentamente. Se requiere alrededor de 1.5 m hasta el centro del tablero.");
    }

    /// <summary>
    /// Enruta un aviso hacia la pantalla que se encuentra activa y lo registra en la consola para diagnóstico.
    /// </summary>
    /// <param name="mensaje">Aviso que se quiere comunicar.</param>
    private void MostrarAviso(string mensaje)
    {
        // No reactivar el ant.
        if (pantallaMetodo != null && pantallaMetodo.activeSelf)
            AvisarEnSeleccion(mensaje);
        else if (estadoEscaneo != null && pantallaEscaneo != null && pantallaEscaneo.activeSelf)
            estadoEscaneo.text = mensaje;
        Debug.Log("Golosa AR: " + mensaje);
    }

    /// <summary>
    /// Actualiza los textos de estado del escaneo actual en las interfaces disponibles.
    /// </summary>
    /// <param name="mensaje">Estado o indicación que se mostrará al jugador.</param>
    private void CambiarEstado(string mensaje)
    {
        if (estadoEscaneo != null) estadoEscaneo.text = mensaje;
        if (textoEstado != null) textoEstado.text = mensaje;
    }

    /// <summary>
    /// Libera el tablero y los elementos temporales de AR antes de regresar a la escena correspondiente según el modo de juego.
    /// </summary>
    public void Volver()
    {
        escaneando = false;

        if (tablero != null)
            Destroy(tablero.gameObject);

        tablero = null;
        colocado = false;

        // Apagar los componentes de realidad aumentada.
        if (raycasts != null) raycasts.enabled = false;
        if (planos != null) planos.enabled = false;
        if (imagenes != null) imagenes.enabled = false;
        if (pantallaMetodo != null) Destroy(pantallaMetodo);
        if (pantallaEscaneo != null) Destroy(pantallaEscaneo);
        if (hudRecuperacion != null) Destroy(hudRecuperacion);
        visibilidadOriginal.Clear();
        if (fondoCamaraAR != null) fondoCamaraAR.enabled = false;
        if (camaraAR != null) camaraAR.enabled = false;
        if (sesion != null) sesion.enabled = false;

        // Identificar el modo de juego.
        string modo = PlayerPrefs.GetString(
            "Golosa.ModoJuego", "Individual"
        );

        if (modo == "Por turnos")
        {
            if (Application.CanStreamedLevelBeLoaded("02_ModoJuego"))
                SceneManager.LoadScene("02_ModoJuego");
        }
        else
        {
            if (Application.CanStreamedLevelBeLoaded("01_Perfil"))
                SceneManager.LoadScene("01_Perfil");
        }
    }

}