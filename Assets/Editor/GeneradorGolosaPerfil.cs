using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

// Sustituye solamente el contenido autogenerado de 01_Perfil.
// Diseño calculado para 1080 x 2340. No altera 00_Inicio.
/// <summary>
/// Herramienta de editor que construye la interfaz de la escena 01_Perfil de Golosa AR.
/// Genera la selección de categoría, la vista previa del avatar y su personalización,
/// y conecta los controles con el componente PerfilUI existente.
/// </summary>
/// <remarks>
/// Se ejecuta desde el menú de Unity Editor. No es un controlador de la partida.
/// </remarks>
public static class GeneradorGolosaPerfil
{
    private const string Arte = "Assets/Art/Perfil/";
    private static readonly Color Uva = new Color32(61, 24, 110, 255);
    private static readonly Color Violeta = new Color32(126, 41, 226, 255);
    private static readonly Color Amarillo = new Color32(255, 197, 62, 255);
    private static readonly Color Azul = new Color32(86, 165, 229, 255);
    private static readonly Color Gris = new Color32(97, 85, 127, 255);
    private static readonly Color Claro = new Color32(244, 247, 255, 255);

    /// <summary>
    /// Reconstruye los elementos visuales de la pantalla de perfil en la escena activa.
    /// Comprueba que Unity no esté entrando en Play, que la escena sea la correcta
    /// y que los recursos gráficos necesarios estén disponibles.
    /// </summary>
    /// <remarks>
    /// Reutiliza el Canvas cuando existe, prepara la cámara de vista previa,
    /// enlaza eventos persistentes con PerfilUI y guarda los cambios de la escena.
    /// </remarks>
    [MenuItem("Golosa AR/Generar pantalla de perfil")]
    public static void Generar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Golosa AR", "Desactiva Play antes de generar.", "Aceptar");
            return;
        }
        if (SceneManager.GetActiveScene().name != "01_Perfil")
        {
            EditorUtility.DisplayDialog("Golosa AR", "Primero abre Assets/Scenes/01_Perfil.unity", "Aceptar");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(Arte + "TituloPerfilGolosa.png") == null)
        {
            EditorUtility.DisplayDialog("Golosa AR", "Falta Assets/Art/Perfil/TituloPerfilGolosa.png. Copia también la carpeta Art del ZIP.", "Aceptar");
            return;
        }

        // Inicializa o reutiliza la superficie de UI y su escalado para pantallas móviles.
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
            canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2340);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventos = new GameObject("EventSystem", typeof(EventSystem));
            System.Type modulo = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (modulo != null) eventos.AddComponent(modulo);
            else eventos.AddComponent<StandaloneInputModule>();
        }

        // Se sustituye solo la interfaz previamente generada; no se elimina el resto de la escena.
        Transform previo = canvas.transform.Find("PerfilGenerado");
        if (previo != null) Object.DestroyImmediate(previo.gameObject);

        // Crea o recupera el canal de renderizado utilizado para mostrar el modelo 3D en el Canvas.
        RenderTexture renderAvatar;
        Transform avatar;
        PrepararAvatar(out renderAvatar, out avatar);

        RectTransform raiz = Rect(canvas.transform, "PerfilGenerado", new Vector2(.5f, .5f),
            Vector2.zero, Vector2.zero);
        Estirar(raiz);
        raiz.SetAsLastSibling();

        // Usamos EXACTAMENTE el mismo parque de la pantalla principal.
        Texture2D fondoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Art/Backgrounds/FondoParqueGolosa.jpg");
        if (fondoTex == null)
            fondoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Arte + "ParqueGolosaPerfil.jpg");
        if (fondoTex != null)
        {
            RawImage fondo = Raw(raiz, "FondoParque", Vector2.zero, Vector2.zero);
            Estirar(fondo.rectTransform);
            fondo.texture = fondoTex;
            fondo.color = Color.white;
        }
        else
        {
            Image fondo = Panel(raiz, "FondoAzul", Vector2.zero, Vector2.zero, Azul);
            Estirar(fondo.rectTransform);
        }

        // 1080 x 2340. TODOS los elementos útiles respetan 65 px o más de margen.
        Button atras = Boton(raiz, "BtnVolver", "←", new Vector2(98, -115),
            new Vector2(110, 110), Color.white, Uva, 67, new Vector2(0, 1));
        Sombra(atras.gameObject, new Color32(37, 20, 80, 105), new Vector2(0, -7));
        Image etiqueta = Panel(raiz, "EtiquetaSuperior", new Vector2(0, -110),
            new Vector2(570, 79), new Color32(104, 158, 211, 175), new Vector2(.5f, 1));
        Texto(etiqueta.transform, "EtiquetaTexto", "✦  TU PRÓXIMA AVENTURA", 33,
            Color.white, Vector2.zero, new Vector2(525, 69), true);

        Imagen(raiz, "TituloEstiloPrincipal", "TituloPerfilGolosa.png",
            new Vector2(0, -290), new Vector2(865, 270), new Vector2(.5f, 1));
        Image desc = Panel(raiz, "SubtituloFondo", new Vector2(0, -470),
            new Vector2(850, 91), new Color32(239, 246, 255, 224), new Vector2(.5f, 1));
        Texto(desc.transform, "Subtitulo", "Elige tu personaje y comienza la aventura", 32,
            Uva, Vector2.zero, new Vector2(790, 76), true);
        Estrella(raiz, "EstrellaIzquierda", -422, -326, false);
        Estrella(raiz, "EstrellaDerecha", 425, -365, true);

        // Vista 3D sobre la plataforma. Flechas giran al avatar durante Play.
        Imagen(raiz, "Plataforma", "PlataformaAvatar.png",
            new Vector2(0, -1431), new Vector2(795, 170), new Vector2(.5f, 1));
        RawImage vista = Raw(raiz, "VistaDelAvatar3D", new Vector2(0, -971),
            new Vector2(670, 865), new Vector2(.5f, 1));
        vista.texture = renderAvatar;
        vista.color = Color.white;
        if (avatar == null)
            Texto(raiz, "AvisoAvatar", "Coloca AvatarNinaGolosa en la escena", 34,
                Uva, new Vector2(0, -935), new Vector2(720, 100), true, new Vector2(.5f, 1));
        Button izq = Boton(raiz, "FlechaIzquierda", "‹", new Vector2(-432, -1058),
            new Vector2(130, 130), Color.white, Uva, 97, new Vector2(.5f, 1));
        Button der = Boton(raiz, "FlechaDerecha", "›", new Vector2(432, -1058),
            new Vector2(130, 130), Color.white, Uva, 97, new Vector2(.5f, 1));
        Sombra(izq.gameObject, new Color32(36, 20, 80, 110), new Vector2(0, -7));
        Sombra(der.gameObject, new Color32(36, 20, 80, 110), new Vector2(0, -7));
        Button personalizar = Boton(raiz, "BtnPersonalizar", "✦  PERSONALIZAR", new Vector2(0, -1535),
            new Vector2(635, 108), Violeta, Color.white, 45, new Vector2(.5f, 1));
        Sombra(personalizar.gameObject, new Color32(52, 16, 109, 115), new Vector2(0, -9));

        // Campo del nombre, completamente dentro de los márgenes.
        Image cajaNombre = Panel(raiz, "TarjetaNombre", new Vector2(0, -1722),
            new Vector2(950, 183), new Color32(248, 250, 255, 251), new Vector2(.5f, 1));
        Sombra(cajaNombre.gameObject, new Color32(40, 30, 83, 90), new Vector2(0, -8));
        TextMeshProUGUI tituloNombre = Texto(cajaNombre.transform, "TituloNombre", "¿CÓMO TE LLAMAS?", 42,
            Uva, new Vector2(-185, 56), new Vector2(500, 61), true);
        tituloNombre.alignment = TextAlignmentOptions.MidlineLeft;
        TMP_InputField nombre = CampoNombre(cajaNombre.transform, new Vector2(0, -38));

        // Dos y SOLO dos categorías: niño/a (6-12) y avanzado/a (13+).
        Image cajaCategorias = Panel(raiz, "TarjetaCategorias", new Vector2(0, -1953),
            new Vector2(950, 280), new Color32(248, 250, 255, 253), new Vector2(.5f, 1));
        Sombra(cajaCategorias.gameObject, new Color32(40, 30, 83, 90), new Vector2(0, -7));
        TextMeshProUGUI tituloCategoria = Texto(cajaCategorias.transform, "TituloCategoria",
            "SELECCIONA TU CATEGORÍA", 38, Uva, new Vector2(-153, 99),
            new Vector2(610, 54), true);
        tituloCategoria.alignment = TextAlignmentOptions.MidlineLeft;
        Image ninos = TarjetaCategoria(cajaCategorias.transform, "CardNinos", "NIÑOS",
            "6 - 12 años", "IconoCategoriaNinos.png", -225, true);
        Image avanzados = TarjetaCategoria(cajaCategorias.transform, "CardAvanzados", "AVANZADOS",
            "13+ años", "IconoCategoriaAvanzados.png", 225, false);

        Button continuar = Boton(raiz, "BtnContinuar", "CONTINUAR    →", new Vector2(0, 125),
            new Vector2(945, 125), Violeta, Color.white, 53, new Vector2(.5f, 0));
        Sombra(continuar.gameObject, new Color32(52, 16, 109, 125), new Vector2(0, -9));
        TextMeshProUGUI mensaje = Texto(raiz, "MensajeValidacion", "", 25,
            Color.white, new Vector2(0, 27), new Vector2(950, 54), true, new Vector2(.5f, 0));
        mensaje.gameObject.SetActive(false); // avisos ahora se presentan como popup desde PerfilUI

        Button cerrar, izqPopup, derPopup, guardar, cancelar;
        Button tabCabello, tabRopa, tabAccesorios;
        Button[] botonesCabello, botonesRopa, botonesAccesorios;
        Image[] muestrasCabello, muestrasRopa, muestrasAccesorios, fondosPestanas;
        GameObject[] panelesOpciones;
        GameObject popup = CrearPopup(raiz, renderAvatar,
            out cerrar, out izqPopup, out derPopup, out guardar, out cancelar,
            out tabCabello, out tabRopa, out tabAccesorios,
            out botonesCabello, out botonesRopa, out botonesAccesorios,
            out muestrasCabello, out muestrasRopa, out muestrasAccesorios,
            out fondosPestanas, out panelesOpciones);
        popup.SetActive(false);

        // Inyecta en PerfilUI las referencias de los controles para que gestione la interacción en Play.
        PerfilUI logica = raiz.gameObject.AddComponent<PerfilUI>();
        logica.campoNombre = nombre;
        logica.tarjetaNinos = ninos;
        logica.tarjetaAvanzados = avanzados;
        logica.indicadorNinos = ninos.transform.Find("Indicador").GetComponent<TextMeshProUGUI>();
        logica.indicadorAvanzados = avanzados.transform.Find("Indicador").GetComponent<TextMeshProUGUI>();
        logica.mensaje = mensaje;
        logica.avatar3D = avatar;
        logica.panelPersonalizar = popup;
        logica.muestrasCabello = muestrasCabello;
        logica.muestrasRopa = muestrasRopa;
        logica.muestrasAccesorios = muestrasAccesorios;
        logica.fondosPestanas = fondosPestanas;
        logica.panelesOpciones = panelesOpciones;
        logica.mensajePersonalizacion = popup.transform.Find("Tarjeta/EstadoPersonalizacion")
            .GetComponent<TextMeshProUGUI>();
        // Enlaza eventos persistentes: los botones invocan métodos del controlador existente.
        UnityEventTools.AddPersistentListener(atras.onClick, logica.Volver);
        UnityEventTools.AddPersistentListener(izq.onClick, logica.GirarAvatarIzquierda);
        UnityEventTools.AddPersistentListener(der.onClick, logica.GirarAvatarDerecha);
        UnityEventTools.AddPersistentListener(personalizar.onClick, logica.AbrirPersonalizar);
        UnityEventTools.AddPersistentListener(ninos.GetComponent<Button>().onClick, logica.SeleccionarNinos);
        UnityEventTools.AddPersistentListener(avanzados.GetComponent<Button>().onClick, logica.SeleccionarAvanzados);
        UnityEventTools.AddPersistentListener(continuar.onClick, logica.Continuar);
        UnityEventTools.AddPersistentListener(cerrar.onClick, logica.CerrarPersonalizar);
        UnityEventTools.AddPersistentListener(guardar.onClick, logica.GuardarPersonalizacion);
        UnityEventTools.AddPersistentListener(cancelar.onClick, logica.CerrarPersonalizar);
        UnityEventTools.AddPersistentListener(tabCabello.onClick, logica.MostrarCabello);
        UnityEventTools.AddPersistentListener(tabRopa.onClick, logica.MostrarRopa);
        UnityEventTools.AddPersistentListener(tabAccesorios.onClick, logica.MostrarAccesorios);
        UnityEventTools.AddPersistentListener(izqPopup.onClick, logica.GirarAvatarIzquierda);
        UnityEventTools.AddPersistentListener(derPopup.onClick, logica.GirarAvatarDerecha);
        UnityEventTools.AddPersistentListener(botonesCabello[0].onClick, logica.Cabello0);
        UnityEventTools.AddPersistentListener(botonesCabello[1].onClick, logica.Cabello1);
        UnityEventTools.AddPersistentListener(botonesCabello[2].onClick, logica.Cabello2);
        UnityEventTools.AddPersistentListener(botonesCabello[3].onClick, logica.Cabello3);
        UnityEventTools.AddPersistentListener(botonesRopa[0].onClick, logica.Ropa0);
        UnityEventTools.AddPersistentListener(botonesRopa[1].onClick, logica.Ropa1);
        UnityEventTools.AddPersistentListener(botonesRopa[2].onClick, logica.Ropa2);
        UnityEventTools.AddPersistentListener(botonesRopa[3].onClick, logica.Ropa3);
        UnityEventTools.AddPersistentListener(botonesRopa[4].onClick, logica.Ropa4);
        UnityEventTools.AddPersistentListener(botonesAccesorios[0].onClick, logica.Accesorio0);
        UnityEventTools.AddPersistentListener(botonesAccesorios[1].onClick, logica.Accesorio1);
        UnityEventTools.AddPersistentListener(botonesAccesorios[2].onClick, logica.Accesorio2);
        UnityEventTools.AddPersistentListener(botonesAccesorios[3].onClick, logica.Accesorio3);

        // Guarda la jerarquía y los vínculos creados para que persistan en la escena.
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("Golosa AR: 01_Perfil actualizado, formato 1080x2340, dos categorías y avatar 3D.");
    }

    /// <summary>
    /// Construye una tarjeta seleccionable con icono, nombre, rango de edades
    /// e indicador visual del estado activo.
    /// </summary>
    /// <param name="padre">Contenedor que recibirá la tarjeta.</param>
    /// <param name="objeto">Nombre del objeto creado en la jerarquía.</param>
    /// <param name="titulo">Nombre de la categoría que verá el jugador.</param>
    /// <param name="edades">Rango de edades mostrado en la tarjeta.</param>
    /// <param name="archivoIcono">Archivo PNG que representa la categoría.</param>
    /// <param name="x">Posición horizontal dentro del contenedor.</param>
    /// <param name="activo">Indica si la categoría aparece seleccionada inicialmente.</param>
    /// <returns>Imagen de la tarjeta, que también contiene su botón interactivo.</returns>
    private static Image TarjetaCategoria(Transform padre, string objeto, string titulo,
        string edades, string archivoIcono, float x, bool activo)
    {
        Image tarjeta = Panel(padre, objeto, new Vector2(x, -34), new Vector2(430, 156),
            activo ? new Color32(224, 240, 255, 255) : Color.white);
        tarjeta.raycastTarget = true;
        Outline borde = tarjeta.gameObject.AddComponent<Outline>();
        borde.effectColor = activo ? Violeta : new Color32(196, 207, 234, 255);
        borde.effectDistance = new Vector2(3, -3);
        Button boton = tarjeta.gameObject.AddComponent<Button>();
        boton.targetGraphic = tarjeta;
        boton.transition = Selectable.Transition.None;
        Imagen(tarjeta.transform, "Retrato", archivoIcono, new Vector2(-136, 0),
            new Vector2(114, 112));
        Texto(tarjeta.transform, "Titulo", titulo, titulo == "NIÑOS" ? 38 : 34,
            Uva, new Vector2(69, 25), new Vector2(270, 55), true);
        Texto(tarjeta.transform, "Rango", edades, 28,
            Gris, new Vector2(69, -26), new Vector2(258, 48));
        Texto(tarjeta.transform, "Indicador", activo ? "●" : "", 35,
            Violeta, new Vector2(181, 60), new Vector2(48, 42), true);
        return tarjeta;
    }

    /// <summary>
    /// Crea el campo de texto para el nombre del jugador, con borde, texto editable,
    /// marcador de posición y color de cursor configurados para la interfaz.
    /// </summary>
    /// <param name="padre">Contenedor de la tarjeta de nombre.</param>
    /// <param name="pos">Posición local del campo.</param>
    /// <returns>Campo TMP listo para asignarse al controlador PerfilUI.</returns>
    private static TMP_InputField CampoNombre(Transform padre, Vector2 pos)
    {
        Image fondo = Panel(padre, "CampoNombre", pos, new Vector2(856, 79), Color.white);
        fondo.raycastTarget = true;
        Outline borde = fondo.gameObject.AddComponent<Outline>();
        borde.effectColor = new Color32(208, 215, 237, 255);
        borde.effectDistance = new Vector2(2, -2);
        TMP_InputField campo = fondo.gameObject.AddComponent<TMP_InputField>();
        campo.targetGraphic = fondo;
        campo.characterLimit = 28;
        RectTransform area = Rect(fondo.transform, "AreaTexto", new Vector2(0, 0), new Vector2(770, 70));
        area.gameObject.AddComponent<RectMask2D>();
        TextMeshProUGUI sugerencia = Texto(area, "Placeholder", "Escribe tu nombre...", 36,
            new Color32(137, 137, 159, 255), Vector2.zero, new Vector2(745, 65));
        sugerencia.alignment = TextAlignmentOptions.MidlineLeft;
        TextMeshProUGUI escrito = Texto(area, "Texto", "", 40,
            Uva, Vector2.zero, new Vector2(745, 65));
        escrito.alignment = TextAlignmentOptions.MidlineLeft;
        campo.textViewport = area;
        campo.textComponent = escrito;
        campo.placeholder = sugerencia;
        campo.customCaretColor = true;
        campo.caretColor = Violeta;
        return campo;
    }

    // Personalizador de Golosa AR. Conserva todos los objetos de la pantalla principal.
    // 1080x2340: ventana a pantalla casi completa, sin textos ni controles recortados.
    /// <summary>
    /// Genera la ventana de personalización del avatar 3D, incluyendo las pestañas
    /// Cabello, Ropa y Accesorios, sus muestras y los botones de confirmación.
    /// </summary>
    /// <param name="padre">Raíz visual de la pantalla del perfil.</param>
    /// <param name="textura">RenderTexture que muestra el avatar desde la cámara de vista previa.</param>
    /// <param name="cerrar">Botón de cierre de la ventana.</param>
    /// <param name="izq">Control para girar a la izquierda.</param>
    /// <param name="der">Control para girar a la derecha.</param>
    /// <param name="guardar">Botón para confirmar la personalización.</param>
    /// <param name="cancelar">Botón para descartar y cerrar.</param>
    /// <param name="tabCabello">Pestaña de color de cabello.</param>
    /// <param name="tabRopa">Pestaña de color de ropa.</param>
    /// <param name="tabAccesorios">Pestaña de accesorios.</param>
    /// <param name="botonesCabello">Botones de las variantes de cabello.</param>
    /// <param name="botonesRopa">Botones de las variantes de ropa.</param>
    /// <param name="botonesAccesorios">Botones de las variantes de accesorios.</param>
    /// <param name="muestrasCabello">Referencias visuales de los colores de cabello.</param>
    /// <param name="muestrasRopa">Referencias visuales de los colores de ropa.</param>
    /// <param name="muestrasAccesorios">Referencias visuales de los accesorios.</param>
    /// <param name="fondosPestanas">Fondos que señalan la pestaña activa.</param>
    /// <param name="panelesOpciones">Paneles de contenido que se alternan entre pestañas.</param>
    /// <returns>Objeto raíz de la ventana emergente de personalización.</returns>
    private static GameObject CrearPopup(Transform padre, RenderTexture textura,
        out Button cerrar, out Button izq, out Button der, out Button guardar,
        out Button cancelar, out Button tabCabello, out Button tabRopa,
        out Button tabAccesorios, out Button[] botonesCabello,
        out Button[] botonesRopa, out Button[] botonesAccesorios,
        out Image[] muestrasCabello, out Image[] muestrasRopa,
        out Image[] muestrasAccesorios, out Image[] fondosPestanas,
        out GameObject[] panelesOpciones)
    {
        // Oscurece la pantalla de fondo mientras la ventana de personalización está abierta.
        Image mascara = Panel(padre, "PanelPersonalizar", Vector2.zero, Vector2.zero,
            new Color32(13, 25, 70, 245));
        Estirar(mascara.rectTransform);
        mascara.raycastTarget = true;

        // La tarjeta cubre casi todo el teléfono; deja margen para pantallas con notch.
        Image tarjeta = Panel(mascara.transform, "Tarjeta", Vector2.zero,
            new Vector2(970, 2200), new Color32(244, 247, 255, 255));
        Sombra(tarjeta.gameObject, new Color32(19, 12, 64, 140), new Vector2(0, -13));

        Image cielo = Panel(tarjeta.transform, "CabeceraAzul", new Vector2(0, 840),
            new Vector2(935, 465), new Color32(82, 160, 233, 255));
        Texto(cielo.transform, "EstrellasCabecera", "✦                               ✦", 44,
            Amarillo, new Vector2(0, -105), new Vector2(830, 75), true);
        TextMeshProUGUI titulo1 = Texto(tarjeta.transform, "TituloBlanco", "PERSONALIZA", 84,
            Color.white, new Vector2(-12, 981), new Vector2(760, 125), true);
        Contorno(titulo1, Uva, .24f);
        TextMeshProUGUI titulo2 = Texto(tarjeta.transform, "TituloDorado", "TU AVATAR 3D", 75,
            Amarillo, new Vector2(-12, 879), new Vector2(800, 116), true);
        Contorno(titulo2, Uva, .25f);
        cerrar = Boton(tarjeta.transform, "BtnCerrar", "X", new Vector2(422, 982),
            new Vector2(98, 98), Violeta, Color.white, 54);
        Sombra(cerrar.gameObject, new Color32(54, 20, 100, 105), new Vector2(0, -6));

        // Cámara VRM original: sigue siendo modelo 3D real; no una foto del avatar.
        Image fondoVista = Panel(tarjeta.transform, "Escenario3D", new Vector2(0, 392),
            new Vector2(895, 725), new Color32(207, 234, 255, 255));
        Image circulo = Panel(fondoVista.transform, "Halo", new Vector2(0, 5),
            new Vector2(655, 625), new Color32(230, 219, 255, 225));
        circulo.raycastTarget = false;
        Imagen(fondoVista.transform, "Plataforma3D", "PlataformaAvatar.png",
            new Vector2(0, -291), new Vector2(610, 120));
        RawImage vista = Raw(fondoVista.transform, "VistaAvatarReal", new Vector2(0, 5),
            new Vector2(570, 690));
        vista.texture = textura;
        vista.color = Color.white;
        izq = Boton(fondoVista.transform, "GirarIzquierda", "‹", new Vector2(-367, -23),
            new Vector2(116, 116), Color.white, Uva, 94);
        der = Boton(fondoVista.transform, "GirarDerecha", "›", new Vector2(367, -23),
            new Vector2(116, 116), Color.white, Uva, 94);
        Sombra(izq.gameObject, new Color32(39, 28, 84, 90), new Vector2(0, -5));
        Sombra(der.gameObject, new Color32(39, 28, 84, 90), new Vector2(0, -5));
        Texto(tarjeta.transform, "AyudaVista", "‹   Gira tu personaje para verlo mejor   ›", 28,
            Uva, new Vector2(0, -20), new Vector2(835, 60), true);

        // Pestañas de personalización; una sección visible a la vez.
        tabCabello = Boton(tarjeta.transform, "TabCabello", "CABELLO", new Vector2(-298, -121),
            new Vector2(276, 108), Amarillo, Uva, 42);
        tabRopa = Boton(tarjeta.transform, "TabRopa", "ROPA", new Vector2(0, -121),
            new Vector2(276, 108), Color.white, Uva, 42);
        tabAccesorios = Boton(tarjeta.transform, "TabAccesorios", "ACCESORIOS", new Vector2(298, -121),
            new Vector2(276, 108), Color.white, Uva, 36);
        fondosPestanas = new Image[] {
            tabCabello.GetComponent<Image>(), tabRopa.GetComponent<Image>(),
            tabAccesorios.GetComponent<Image>()
        };

        // Mantiene tres contenedores reutilizables: uno por pestaña de personalización.
        GameObject[] opciones = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            Image contenedor = Panel(tarjeta.transform, "Opciones" + i,
                new Vector2(0, -463), new Vector2(896, 550), Color.white);
            Sombra(contenedor.gameObject, new Color32(75, 40, 135, 35), new Vector2(0, -5));
            opciones[i] = contenedor.gameObject;
        }
        panelesOpciones = opciones;

        // Cabello: cuatro tonos. El peinado original se conserva (es parte del VRM).
        Texto(opciones[0].transform, "Titulo", "COLOR DE CABELLO", 48, Uva,
            new Vector2(0, 202), new Vector2(835, 77), true);
        Texto(opciones[0].transform, "Descripcion", "Elige un tono para tu personaje", 30, Gris,
            new Vector2(0, 126), new Vector2(835, 58));
        botonesCabello = new Button[4];
        muestrasCabello = new Image[4];
        Color[] coloresCabello = {
            new Color32(165, 117, 88, 255), new Color32(112, 67, 48, 255),
            new Color32(50, 45, 53, 255), new Color32(224, 173, 91, 255)
        };
        string[] nombresCabello = { "ORIGINAL", "CHOCOLATE", "NEGRO", "RUBIO" };
        for (int i = 0; i < 4; i++)
        {
            float x = -302 + i * 201;
            botonesCabello[i] = Boton(opciones[0].transform, "Cabello" + i, "",
                new Vector2(x, -13), new Vector2(158, 149), coloresCabello[i], Color.white, 26);
            muestrasCabello[i] = botonesCabello[i].GetComponent<Image>();
            BordeMuestra(botonesCabello[i].gameObject);
            Texto(opciones[0].transform, "EtiquetaCabello" + i, nombresCabello[i], 26,
                Uva, new Vector2(x, -135), new Vector2(189, 52), true);
        }
        Texto(opciones[0].transform, "Nota", "El peinado permanece; puedes cambiar su color.", 27,
            Gris, new Vector2(0, -214), new Vector2(790, 48));

        // Ropa: cinco combinaciones coherentes con el logo GOLOSA AR.
        Texto(opciones[1].transform, "Titulo", "COLOR DE ROPA", 48, Uva,
            new Vector2(0, 202), new Vector2(835, 77), true);
        Texto(opciones[1].transform, "Descripcion", "¡Viste a tu personaje con tus colores favoritos!", 29,
            Gris, new Vector2(0, 126), new Vector2(835, 58));
        botonesRopa = new Button[5];
        muestrasRopa = new Image[5];
        Color[] coloresRopa = {
            new Color32(167, 111, 243, 255), new Color32(126, 41, 226, 255),
            new Color32(92, 173, 245, 255), new Color32(94, 217, 194, 255),
            new Color32(255, 201, 81, 255)
        };
        string[] nombresRopa = { "ORIGINAL", "MORADO", "AZUL", "TURQUESA", "AMARILLO" };
        for (int i = 0; i < 5; i++)
        {
            float x = -326 + i * 163;
            botonesRopa[i] = Boton(opciones[1].transform, "Ropa" + i, "",
                new Vector2(x, -13), new Vector2(128, 149), coloresRopa[i], Color.white, 22);
            muestrasRopa[i] = botonesRopa[i].GetComponent<Image>();
            BordeMuestra(botonesRopa[i].gameObject);
            Texto(opciones[1].transform, "EtiquetaRopa" + i, nombresRopa[i], 24,
                Uva, new Vector2(x, -135), new Vector2(165, 52), true);
        }
        Texto(opciones[1].transform, "Nota", "Puedes volver al color original cuando quieras.", 27,
            Gris, new Vector2(0, -214), new Vector2(790, 48));

        // Accesorios: creados en 3D y unidos al hueso de la cabeza por PerfilUI.
        Texto(opciones[2].transform, "Titulo", "ELIGE UN ACCESORIO", 46, Uva,
            new Vector2(0, 202), new Vector2(835, 77), true);
        Texto(opciones[2].transform, "Descripcion", "Prueba una opción sobre tu avatar 3D", 29,
            Gris, new Vector2(0, 126), new Vector2(835, 58));
        botonesAccesorios = new Button[4];
        muestrasAccesorios = new Image[4];
        string[] nombresAccesorios = { "NINGUNO", "MOÑO", "GORRA", "ESTRELLA" };
        string[] iconos = { "—", "✿", "▲", "★" };
        Color[] coloresAccesorios = {
            new Color32(223, 233, 247, 255), new Color32(225, 196, 255, 255),
            new Color32(169, 216, 255, 255), new Color32(255, 231, 151, 255)
        };
        for (int i = 0; i < 4; i++)
        {
            float x = -302 + i * 201;
            botonesAccesorios[i] = Boton(opciones[2].transform, "Accesorio" + i,
                iconos[i], new Vector2(x, -13), new Vector2(158, 149),
                coloresAccesorios[i], Uva, 73);
            muestrasAccesorios[i] = botonesAccesorios[i].GetComponent<Image>();
            BordeMuestra(botonesAccesorios[i].gameObject);
            Texto(opciones[2].transform, "EtiquetaAccesorio" + i,
                nombresAccesorios[i], 26, Uva, new Vector2(x, -135),
                new Vector2(189, 52), true);
        }
        Texto(opciones[2].transform, "Nota",
            "Accesorios de prueba 3D: se fijan a la cabeza.", 27,
            Gris, new Vector2(0, -214), new Vector2(790, 48));
        opciones[0].SetActive(true);
        opciones[1].SetActive(false);
        opciones[2].SetActive(false);

        Texto(tarjeta.transform, "EstadoPersonalizacion", "", 27,
            Uva, new Vector2(0, -794), new Vector2(865, 56));
        cancelar = Boton(tarjeta.transform, "BtnCancelar", "CANCELAR",
            new Vector2(-227, -963), new Vector2(414, 125),
            new Color32(225, 217, 252, 255), Uva, 43);
        guardar = Boton(tarjeta.transform, "BtnGuardarCambios", "GUARDAR CAMBIOS",
            new Vector2(227, -963), new Vector2(414, 125), Violeta, Color.white, 42);
        Sombra(guardar.gameObject, new Color32(41, 17, 89, 130), new Vector2(0, -8));
        return mascara.gameObject;
    }

    /// <summary>
    /// Aplica contorno tipográfico y sombra a un texto destacado del diseño.
    /// </summary>
    /// <param name="texto">Texto TextMeshPro que recibirá el efecto.</param>
    /// <param name="color">Color del contorno.</param>
    /// <param name="ancho">Grosor del contorno.</param>
    private static void Contorno(TextMeshProUGUI texto, Color color, float ancho)
    {
        if (texto == null) return;
        texto.outlineColor = color;
        texto.outlineWidth = ancho;
        texto.extraPadding = true;
        Sombra(texto.gameObject, new Color32(40, 20, 80, 120), new Vector2(0, -5));
    }

    /// <summary>
    /// Añade un borde visual a una muestra de color o accesorio.
    /// </summary>
    /// <param name="muestra">Objeto gráfico que recibirá el borde.</param>
    private static void BordeMuestra(GameObject muestra)
    {
        Outline borde = muestra.AddComponent<Outline>();
        borde.effectColor = new Color32(192, 204, 229, 255);
        borde.effectDistance = new Vector2(2, -2);
    }

    /// <summary>
    /// Prepara la vista previa del avatar mediante una cámara independiente,
    /// una RenderTexture, una capa exclusiva y una luz para el modelo 3D.
    /// </summary>
    /// <param name="textura">Referencia de salida a la textura donde se renderiza el avatar.</param>
    /// <param name="avatar">Referencia de salida al Transform del personaje localizado.</param>
    /// <remarks>
    /// La cámara principal excluye la capa de vista previa para impedir
    /// que el personaje se dibuje por duplicado en la escena normal.
    /// </remarks>
    private static void PrepararAvatar(out RenderTexture textura, out Transform avatar)
    {
        const string ruta = "Assets/GolosaAR/Avatares/VistaAvatarPerfil.renderTexture";
        if (!AssetDatabase.IsValidFolder("Assets/GolosaAR"))
            AssetDatabase.CreateFolder("Assets", "GolosaAR");
        if (!AssetDatabase.IsValidFolder("Assets/GolosaAR/Avatares"))
            AssetDatabase.CreateFolder("Assets/GolosaAR", "Avatares");
        textura = AssetDatabase.LoadAssetAtPath<RenderTexture>(ruta);
        if (textura == null)
        {
            textura = new RenderTexture(768, 1024, 24, RenderTextureFormat.ARGB32);
            textura.name = "VistaAvatarPerfil";
            AssetDatabase.CreateAsset(textura, ruta);
        }
        else if (textura.width != 768 || textura.height != 1024)
        {
            textura.Release();
            textura.width = 768;
            textura.height = 1024;
            EditorUtility.SetDirty(textura);
        }
        // Aísla el avatar de vista previa en un espacio separado de la escena de juego.
        GameObject escenario = GameObject.Find("Golosa_Preview3D");
        if (escenario == null) escenario = new GameObject("Golosa_Preview3D");
        escenario.transform.position = new Vector3(1000f, 0, 0);
        escenario.SetActive(true);

        GameObject personaje = GameObject.Find("AvatarNinaGolosa");
        if (personaje == null)
        {
            Transform[] todos = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Transform t in todos)
            {
                if (t.name == "AvatarNinaGolosa" && t.gameObject.scene == SceneManager.GetActiveScene())
                { personaje = t.gameObject; break; }
            }
        }
        avatar = personaje == null ? null : personaje.transform;
        if (avatar != null)
        {
            avatar.SetParent(escenario.transform, false);
            avatar.localPosition = Vector3.zero;
            avatar.localRotation = Quaternion.identity;
            avatar.localScale = Vector3.one;
            avatar.gameObject.SetActive(true);
        }
        int layer = AsegurarCapa();
        if (avatar != null) AsignarCapa(avatar.gameObject, layer);

        // La cámara ortográfica encuadra el avatar y dibuja únicamente su capa.
        GameObject camGO = GameObject.Find("Golosa_CamaraAvatar");
        if (camGO == null) camGO = new GameObject("Golosa_CamaraAvatar", typeof(Camera));
        Camera cam = camGO.GetComponent<Camera>();
        Vector3 centro = new Vector3(1000, 0.8f, 0);
        float alturaVista = 1.2f;
        if (avatar != null)
        {
            Renderer[] renders = avatar.GetComponentsInChildren<Renderer>(true);
            bool encontrado = false;
            Bounds limites = new Bounds(avatar.position, Vector3.zero);
            foreach (Renderer r in renders)
            {
                if (!r.enabled) continue;
                if (!encontrado) { limites = r.bounds; encontrado = true; }
                else limites.Encapsulate(r.bounds);
            }
            if (encontrado)
            {
                centro = limites.center;
                // Encaja su altura y sus brazos incluso al girar.
                alturaVista = Mathf.Clamp(Mathf.Max(limites.extents.y * 1.09f,
                    limites.extents.x * 1.27f), .62f, 2.8f);
            }
        }
        cam.transform.position = centro + new Vector3(0, 0, 5);
        cam.transform.LookAt(centro);
        cam.orthographic = true;
        cam.orthographicSize = alturaVista;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.cullingMask = 1 << layer;
        cam.nearClipPlane = .05f;
        cam.farClipPlane = 20;
        cam.targetTexture = textura;
        cam.depth = -10;
        cam.enabled = true;

        Camera principal = GameObject.Find("Main Camera")?.GetComponent<Camera>();
        if (principal != null) principal.cullingMask &= ~(1 << layer);

        // Ilumina el personaje en su renderizado independiente del entorno AR.
        GameObject luzGO = GameObject.Find("Golosa_LuzAvatar");
        if (luzGO == null) luzGO = new GameObject("Golosa_LuzAvatar", typeof(Light));
        Light luz = luzGO.GetComponent<Light>();
        luz.type = LightType.Directional;
        luz.color = new Color(1, .95f, .85f);
        luz.intensity = 1.7f;
        luz.cullingMask = 1 << layer;
        luz.transform.rotation = Quaternion.Euler(48, -28, 0);
    }

    /// <summary>
    /// Obtiene la capa GolosaPreview o intenta crearla en un espacio disponible
    /// de las capas de usuario del proyecto.
    /// </summary>
    /// <returns>Índice de la capa asignada a la vista previa.</returns>
    private static int AsegurarCapa()
    {
        int layer = LayerMask.NameToLayer("GolosaPreview");
        if (layer >= 0) return layer;
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets.Length == 0) return 30;
        SerializedObject config = new SerializedObject(assets[0]);
        SerializedProperty capas = config.FindProperty("layers");
        for (int i = 30; i >= 8; i--)
        {
            SerializedProperty entrada = capas.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(entrada.stringValue)) continue;
            entrada.stringValue = "GolosaPreview";
            config.ApplyModifiedProperties();
            return i;
        }
        Debug.LogWarning("No quedan capas libres para GolosaPreview.");
        return 30;
    }

    /// <summary>
    /// Asigna una capa a un objeto y recorre recursivamente todos sus hijos.
    /// </summary>
    /// <param name="go">Objeto raíz de la jerarquía que se actualizará.</param>
    /// <param name="layer">Número de la capa de renderizado.</param>
    private static void AsignarCapa(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform h in go.transform) AsignarCapa(h.gameObject, layer);
    }

    /// <summary>
    /// Añade una estrella decorativa si existe su recurso gráfico.
    /// </summary>
    /// <param name="padre">Contenedor de la estrella.</param>
    /// <param name="nombre">Nombre del objeto creado.</param>
    /// <param name="x">Coordenada horizontal.</param>
    /// <param name="y">Coordenada vertical.</param>
    /// <param name="derecha">Selecciona el recurso dorado o blanco.</param>
    private static void Estrella(Transform padre, string nombre, float x, float y, bool derecha)
    {
        string ruta = derecha ? "EstrellaDorada.png" : "EstrellaBlanca.png";
        Sprite s = SpriteDe("Assets/Art/UI/" + ruta);
        if (s == null) return;
        Image imagen = Panel(padre, nombre, new Vector2(x, y), new Vector2(75, 75), Color.white,
            new Vector2(.5f, 1));
        imagen.sprite = s;
        imagen.type = Image.Type.Simple;
        imagen.preserveAspect = true;
    }

    /// <summary>
    /// Localiza un recurso gráfico y lo prepara como Sprite de tipo Single
    /// para su uso en componentes Image del Canvas.
    /// </summary>
    /// <param name="ruta">Ruta del recurso dentro del proyecto Unity.</param>
    /// <returns>Sprite importado o null si no está disponible.</returns>
    private static Sprite SpriteDe(string ruta)
    {
        TextureImporter importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importer != null)
        {
            bool changed = importer.textureType != TextureImporterType.Sprite;
            if (changed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
            }
            // El borde permite redimensionar tarjetas sin estirar las esquinas.
            if (ruta.EndsWith("UIRedondeado.png"))
            {
                Vector4 borde = new Vector4(46, 46, 46, 46);
                if (importer.spriteBorder != borde) { importer.spriteBorder = borde; changed = true; }
            }
            if (changed) importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    /// <summary>
    /// Inserta una imagen en la interfaz usando un Sprite del directorio de arte.
    /// </summary>
    /// <param name="padre">Transform del contenedor.</param>
    /// <param name="nombre">Nombre de la imagen en la jerarquía.</param>
    /// <param name="archivo">Nombre del recurso gráfico.</param>
    /// <param name="pos">Posición local de la imagen.</param>
    /// <param name="tam">Dimensiones del elemento.</param>
    /// <param name="ancla">Anclaje opcional; centrado si se omite.</param>
    /// <returns>Componente Image configurado.</returns>
    private static Image Imagen(Transform padre, string nombre, string archivo,
        Vector2 pos, Vector2 tam, Vector2? ancla = null)
    {
        Image imagen = Panel(padre, nombre, pos, tam, Color.white, ancla);
        imagen.sprite = SpriteDe(Arte + archivo);
        imagen.type = Image.Type.Simple;
        imagen.preserveAspect = true;
        imagen.raycastTarget = false;
        return imagen;
    }

    /// <summary>
    /// Construye un botón visual con rótulo TextMeshPro, colores y dimensiones
    /// configurables. La acción onClick se enlaza posteriormente.
    /// </summary>
    /// <param name="padre">Contenedor de la interfaz.</param>
    /// <param name="nombre">Nombre del botón.</param>
    /// <param name="rotulo">Texto mostrado sobre el botón.</param>
    /// <param name="pos">Posición local.</param>
    /// <param name="tam">Tamaño del botón.</param>
    /// <param name="fondo">Color de su fondo.</param>
    /// <param name="colorTexto">Color de su rótulo.</param>
    /// <param name="fuente">Tamaño inicial del texto.</param>
    /// <param name="ancla">Anclaje opcional.</param>
    /// <returns>Botón listo para vincular su evento.</returns>
    private static Button Boton(Transform padre, string nombre, string rotulo, Vector2 pos,
        Vector2 tam, Color fondo, Color colorTexto, float fuente, Vector2? ancla = null)
    {
        Image imagen = Panel(padre, nombre, pos, tam, fondo, ancla);
        imagen.raycastTarget = true;
        Button boton = imagen.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        Texto(imagen.transform, "Rotulo", rotulo, fuente, colorTexto,
            Vector2.zero, new Vector2(tam.x - 20, tam.y - 12), true);
        return boton;
    }

    /// <summary>
    /// Añade un efecto de sombra a un elemento gráfico de la interfaz.
    /// </summary>
    /// <param name="go">Objeto que recibirá el componente Shadow.</param>
    /// <param name="color">Color y transparencia de la sombra.</param>
    /// <param name="distancia">Desplazamiento visual de la sombra.</param>
    private static void Sombra(GameObject go, Color color, Vector2 distancia)
    {
        Shadow s = go.AddComponent<Shadow>();
        s.effectColor = color;
        s.effectDistance = distancia;
    }

    /// <summary>
    /// Crea un panel de tipo Image y, si existe, utiliza el Sprite redondeado
    /// con escalado por nueve segmentos para conservar las esquinas.
    /// </summary>
    /// <param name="padre">Contenedor principal.</param>
    /// <param name="nombre">Nombre del nuevo panel.</param>
    /// <param name="pos">Posición local.</param>
    /// <param name="tam">Dimensiones del panel.</param>
    /// <param name="color">Color de fondo.</param>
    /// <param name="ancla">Anclaje opcional.</param>
    /// <returns>Imagen que representa el panel creado.</returns>
    private static Image Panel(Transform padre, string nombre, Vector2 pos,
        Vector2 tam, Color color, Vector2? ancla = null)
    {
        RectTransform r = Rect(padre, nombre, pos, tam, ancla);
        Image imagen = r.gameObject.AddComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        Sprite redondeado = SpriteDe(Arte + "UIRedondeado.png");
        if (redondeado != null)
        {
            imagen.sprite = redondeado;
            imagen.type = Image.Type.Sliced;
        }
        return imagen;
    }

    /// <summary>
    /// Crea una RawImage destinada a mostrar texturas como la vista previa 3D.
    /// </summary>
    /// <param name="padre">Contenedor de la imagen.</param>
    /// <param name="nombre">Nombre del objeto.</param>
    /// <param name="pos">Posición local.</param>
    /// <param name="tam">Dimensiones.</param>
    /// <param name="ancla">Anclaje opcional.</param>
    /// <returns>Componente RawImage creado.</returns>
    private static RawImage Raw(Transform padre, string nombre, Vector2 pos,
        Vector2 tam, Vector2? ancla = null)
    {
        RectTransform r = Rect(padre, nombre, pos, tam, ancla);
        return r.gameObject.AddComponent<RawImage>();
    }

    /// <summary>
    /// Genera un texto TextMeshPro con ajuste automático de tamaño, alineación
    /// centrada y opción de negrita.
    /// </summary>
    /// <param name="padre">Contenedor del texto.</param>
    /// <param name="nombre">Nombre del objeto de texto.</param>
    /// <param name="contenido">Contenido literal que se mostrará.</param>
    /// <param name="fuente">Tamaño máximo previsto para la fuente.</param>
    /// <param name="color">Color del texto.</param>
    /// <param name="pos">Posición local.</param>
    /// <param name="tam">Dimensiones del área de texto.</param>
    /// <param name="negrita">Indica si se aplica negrita.</param>
    /// <param name="ancla">Anclaje opcional.</param>
    /// <returns>Texto creado y configurado.</returns>
    private static TextMeshProUGUI Texto(Transform padre, string nombre, string contenido,
        float fuente, Color color, Vector2 pos, Vector2 tam, bool negrita = false,
        Vector2? ancla = null)
    {
        RectTransform r = Rect(padre, nombre, pos, tam, ancla);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = contenido;
        t.fontSize = fuente;
        t.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.enableAutoSizing = true;
        t.fontSizeMin = 23;
        t.fontSizeMax = fuente;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>
    /// Crea un RectTransform hijo con posición, tamaño y anclajes explícitos.
    /// </summary>
    /// <param name="padre">Transform que contendrá el elemento.</param>
    /// <param name="nombre">Nombre del GameObject.</param>
    /// <param name="pos">Posición del rectángulo respecto a sus anclajes.</param>
    /// <param name="tam">Dimensiones del rectángulo.</param>
    /// <param name="ancla">Anclaje opcional, por defecto en el centro.</param>
    /// <returns>RectTransform del objeto creado.</returns>
    private static RectTransform Rect(Transform padre, string nombre, Vector2 pos,
        Vector2 tam, Vector2? ancla = null)
    {
        Vector2 a = ancla ?? new Vector2(.5f, .5f);
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(padre, false);
        r.anchorMin = a;
        r.anchorMax = a;
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
        return r;
    }

    /// <summary>
    /// Extiende un RectTransform hasta cubrir por completo el área de su padre.
    /// </summary>
    /// <param name="rect">RectTransform que se ajustará.</param>
    private static void Estirar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
