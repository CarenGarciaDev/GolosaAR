using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GolosaConfiguracionTurnosUI : MonoBehaviour
{
    private static readonly string[] AvatarIds = { "AvatarNiño", "Astronauta", "Cachucha", "Pirate", "hiNiña", "hiOso" };
    private static readonly string[] NombresAvatares = { "Niño", "Astronauta", "Cachucha", "Pirata", "Niña", "Oso" };
    private static readonly Color Uva = new Color32(64, 22, 126, 255);
    private static readonly Color Morado = new Color32(114, 38, 199, 255);
    private readonly List<Fila> filas = new List<Fila>();
    private RectTransform contenido, viewport;
    private TextMeshProUGUI contador;
    private GameObject ventanaPopup;
    private RectTransform tarjetaPopup;
    private CanvasGroup grupoPopup;
    private TextMeshProUGUI tituloPopup, mensajePopup, principalPopup, secundarioPopup;
    private Image franjaPopup, simboloPopup, botonPrincipalPopup;
    private Button accionPrincipalPopup, accionSecundariaPopup;
    private System.Action accionConfirmarPopup;
    private Coroutine transicionPopup;
    private ScrollRect scroll;
    private Canvas canvas;
    private int total = 2;
    private GolosaCatalogoAvatares catalogo3D;
    private Texture2D texturaPaneles;
    private Sprite spritePaneles;
    // Altura de cada tarjeta y separación entre jugadores (coordenadas Canvas).
    private const float AltoTarjeta = 565f;
    private const float PasoTarjeta = 590f;
    private class Selector { public int valor; public TextMeshProUGUI etiqueta; public string[] opciones; public System.Action<int> alCambiar; }
    private class Fila { public TMP_InputField nombre; public Selector categoria, avatar; public GameObject raiz; }

    public static void Abrir()
    {
        if (FindFirstObjectByType<GolosaConfiguracionTurnosUI>() != null) return;
        new GameObject("Configuracion_PorTurnos").AddComponent<GolosaConfiguracionTurnosUI>();
    }

    private Sprite ImagenRecurso(string nombre)
    {
        var sprite = Resources.Load<Sprite>("GolosaTurnosUI/" + nombre);
        if (sprite == null) Debug.LogWarning("Golosa AR - recurso visual no encontrado: " + nombre + ". Comprueba que su Texture Type sea Sprite (2D and UI).");
        return sprite;
    }

    private static RectTransform Nuevo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var r = go.GetComponent<RectTransform>(); r.SetParent(padre, false);
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.pivot = new Vector2(.5f, .5f);
        return r;
    }
    private static void Fijar(RectTransform r, Vector2 posicion, Vector2 tam)
    { r.anchoredPosition = posicion; r.sizeDelta = tam; }
    private Image Fondo(Transform padre, string nombre, Vector2 posicion, Vector2 tam, string recurso, Color? alternativo = null)
    {
        RectTransform r = Nuevo(nombre, padre); Fijar(r, posicion, tam);
        Image img = r.gameObject.AddComponent<Image>(); img.sprite = ImagenRecurso(recurso);
        img.color = img.sprite != null ? Color.white : (alternativo ?? new Color(.65f,.4f,.87f,1));
        img.raycastTarget = false;
        return img;
    }
    private TextMeshProUGUI Texto(Transform padre, string nombre, string contenido, Vector2 pos, Vector2 tam, int fuente, Color color)
    {
        var rt = Nuevo(nombre, padre); Fijar(rt, pos, tam);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = contenido; t.color = color; t.fontSize = fuente; t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center; t.enableAutoSizing = true;
        t.fontSizeMin = 18; t.fontSizeMax = fuente;
        t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Ellipsis; t.raycastTarget = false;
        return t;
    }
    private Button Boton(Transform padre, string nombre, string etiqueta, Vector2 pos, Vector2 tam, string recurso,
        UnityEngine.Events.UnityAction accion, int fuente = 42, Color? textoColor = null)
    {
        Image img = Fondo(padre, nombre, pos, tam, recurso);
        img.raycastTarget = true;
        Button btn = img.gameObject.AddComponent<Button>(); btn.targetGraphic = img;
        btn.onClick.AddListener(accion);
        var textoBoton=Texto(img.transform, "Texto", etiqueta, Vector2.zero, tam-new Vector2(48,24), fuente, textoColor ?? Uva);
        textoBoton.overflowMode=TextOverflowModes.Ellipsis;
        return btn;
    }
    private void Start()
    {
        CrearSpriteRedondeado();
        catalogo3D = FindFirstObjectByType<GolosaCatalogoAvatares>();
        if (catalogo3D == null)
            Debug.LogWarning("Golosa AR: agrega GolosaCatalogoAvatares a 02_ModoJuego y vincula los seis prefabs para ver avatares 3D.");
        var go = new GameObject("CanvasPorTurnos", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 900;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080,2340); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        var raiz = Nuevo("ZonaSegura", go.transform);
        Rect safe = Screen.safeArea;
        raiz.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        raiz.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        raiz.offsetMin = raiz.offsetMax = Vector2.zero;
        // Fondo pantalla completa: no transparente, no deja ver los botones de 02_ModoJuego.
        var fondo = Fondo(raiz, "FondoCompleto", Vector2.zero, Vector2.zero, "FondoEstrellado", new Color(.5f,.3f,.83f));
        var frt = fondo.rectTransform; frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        fondo.raycastTarget = true;

        var cabecera = Nuevo("Cabecera", raiz);
        cabecera.anchorMin = cabecera.anchorMax = new Vector2(.5f, 1f);
        Fijar(cabecera, new Vector2(0, -240), new Vector2(1050, 460));
        // Cabecera 3D REAL (PNG transparente): letras, volumen, estrellas y subtitulo
        // estan dibujados en el sprite; no se duplican usando TextMeshPro.
        Image titulo3D = Fondo(cabecera,"Titulo3DReal",new Vector2(0,5),new Vector2(965,460),"Titulo3DReal");
        titulo3D.preserveAspect = true;
        // El boton es independiente de la imagen y sigue siendo interactivo.
        Boton(cabecera,"Cerrar","<",new Vector2(-455,165),new Vector2(90,90),"BotonBlanco",SolicitarSalir,62);

        var cantidad = Nuevo("SelectorCantidad",raiz);
        cantidad.anchorMin=cantidad.anchorMax=new Vector2(.5f,1f);
        Fijar(cantidad,new Vector2(0,-595),new Vector2(1000,155));
        Boton(cantidad,"Menos","-",new Vector2(-370,0),new Vector2(140,135),"MenosNuevo",()=>CambiarTotal(-1),75,Color.white);
        Fondo(cantidad,"PanelCantidad",Vector2.zero,new Vector2(495,150),"Contador");
        contador=Texto(cantidad,"Contador","2 JUGADORES",Vector2.zero,new Vector2(450,125),49,Uva);
        Boton(cantidad,"Mas","+",new Vector2(370,0),new Vector2(140,135),"MasNuevo",()=>CambiarTotal(1),75,Color.white);

        viewport = Nuevo("VentanaJugadores",raiz);
        viewport.anchorMin=new Vector2(0,.145f); viewport.anchorMax=new Vector2(1,.715f);
        viewport.offsetMin=new Vector2(30,0); viewport.offsetMax=new Vector2(-30,0);
        var maskImage=viewport.gameObject.AddComponent<Image>(); maskImage.color=new Color(1,1,1,.002f);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
        scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.vertical=true;
        scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=34;
        contenido=Nuevo("ListaJugadores",viewport);
        contenido.anchorMin=contenido.anchorMax=new Vector2(.5f,1f); contenido.pivot=new Vector2(.5f,1f);
        contenido.anchoredPosition=Vector2.zero; contenido.sizeDelta=new Vector2(980, 4*PasoTarjeta+30);
        scroll.viewport=viewport; scroll.content=contenido;
        for(int i=0;i<4;i++)CrearFila(i);

        var pie=Nuevo("Pie",raiz); pie.anchorMin=pie.anchorMax=new Vector2(.5f,0);
        Fijar(pie,new Vector2(0,145),new Vector2(1050,250));
        Boton(pie,"ComenzarPartida","",new Vector2(0,-15),new Vector2(940,186),"ComenzarNuevo",Iniciar,52);
        Refrescar();
        CrearVentanaPopup(go.transform);
    }
    private void CambiarTotal(int delta) {total=Mathf.Clamp(total+delta,2,4);Refrescar();}
    private void Refrescar()
    {
        for(int i=0;i<filas.Count;i++)filas[i].raiz.SetActive(i<total);
        contador.text=total+" JUGADORES";
        contenido.sizeDelta=new Vector2(980,total*PasoTarjeta+20);
        if(scroll!=null)scroll.verticalNormalizedPosition=1f;
    }
    // Paneles redondeados sencillos, sin sprites adicionales ni bordes decorativos dobles.
    private void CrearSpriteRedondeado()
    {
        const int lado = 96;
        const float radio = 22f;
        texturaPaneles = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        texturaPaneles.name = "Golosa_PanelRedondeado_Temporal";
        texturaPaneles.filterMode = FilterMode.Bilinear;
        texturaPaneles.wrapMode = TextureWrapMode.Clamp;
        Color[] pixeles = new Color[lado * lado];
        for (int y = 0; y < lado; y++)
        for (int x = 0; x < lado; x++)
        {
            float px = Mathf.Abs(x + .5f - lado / 2f) - (lado / 2f - radio);
            float py = Mathf.Abs(y + .5f - lado / 2f) - (lado / 2f - radio);
            float distancia = Mathf.Sqrt(Mathf.Max(px, 0f) * Mathf.Max(px, 0f) +
                                        Mathf.Max(py, 0f) * Mathf.Max(py, 0f)) +
                              Mathf.Min(Mathf.Max(px, py), 0f) - radio;
            pixeles[y * lado + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(.5f - distancia));
        }
        texturaPaneles.SetPixels(pixeles);
        texturaPaneles.Apply();
        spritePaneles = Sprite.Create(texturaPaneles, new Rect(0, 0, lado, lado),
            new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(28f, 28f, 28f, 28f));
    }

    private Image PanelLimpio(Transform padre, string nombre, Vector2 pos, Vector2 tam, Color color)
    {
        RectTransform r = Nuevo(nombre, padre);
        Fijar(r, pos, tam);
        Image imagen = r.gameObject.AddComponent<Image>();
        imagen.sprite = spritePaneles;
        imagen.type = Image.Type.Sliced;
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    private void CrearFila(int i)
    {
        RectTransform r = Nuevo("Jugador_" + (i + 1), contenido);
        r.anchorMin = r.anchorMax = new Vector2(.5f, 1f);
        Fijar(r, new Vector2(0, -AltoTarjeta / 2f - i * PasoTarjeta),
            new Vector2(960, AltoTarjeta));

        bool amarillo = i % 2 == 0;
        Color colorExterior = amarillo
            ? new Color32(255, 215, 72, 255)
            : new Color32(170, 103, 234, 255);
        // Una sola tarjeta de color, sin acumulacion de marcos y brillos.
        PanelLimpio(r, "SombraSuave", new Vector2(0, -10),
            new Vector2(944, 548), new Color(0.27f, 0.09f, 0.43f, .22f));
        PanelLimpio(r, "TarjetaSimple", Vector2.zero,
            new Vector2(940, 540), colorExterior);
        PanelLimpio(r, "PanelDatos", new Vector2(213, -65),
            new Vector2(468, 385), new Color32(255, 250, 252, 255));

        Color tituloColor = amarillo ? Uva : Color.white;
        Texto(r, "JugadorTitulo", "JUGADOR " + (i + 1),
            new Vector2(206, 190), new Vector2(450, 76), 53, tituloColor);

        // Zona de avatar con separacion del panel de datos; flechas dentro del marco.
        PanelLimpio(r, "BordeBlancoAvatar", new Vector2(-230, -14),
            new Vector2(370, 433), Color.white);
        Image marco = PanelLimpio(r, "MarcoAvatar", new Vector2(-230, -14),
            new Vector2(352, 414), new Color32(96, 35, 181, 255));
        marco.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        Image respaldo = Fondo(marco.transform, "RetratoRespaldo", Vector2.zero,
            new Vector2(352, 414), "AvatarNino");
        respaldo.preserveAspect = true;
        RectTransform vista = Nuevo("VistaAvatar3D", marco.transform);
        Fijar(vista, Vector2.zero, new Vector2(352, 414));
        RawImage raw = vista.gameObject.AddComponent<RawImage>();
        raw.raycastTarget = false;
        var preview = r.gameObject.AddComponent<GolosaVistaTurno3D>();
        preview.Inicializar(raw, respaldo, catalogo3D, i);

        TextMeshProUGUI nombreLabel = Texto(r, "NombreLabel", "NOMBRE DEL JUGADOR",
            new Vector2(213, 63), new Vector2(410, 48), 30, Uva);
        nombreLabel.alignment = TextAlignmentOptions.Left;
        TMP_InputField input = CrearInput(r, "Nombre_" + (i + 1),
            new Vector2(213, -1), new Vector2(400, 78),
            "Nombre del jugador " + (i + 1));
        Texto(r, "CategoriaLabel", "CATEGORÍA",
            new Vector2(213, -105), new Vector2(400, 48), 31, Uva);
        Selector categoria = SelectorCiclico(r, "Categoria",
            new Vector2(213, -178), new Vector2(382, 87),
            new[] { "Niños", "Avanzados" }, "SelectorRosa");

        Selector avatar = new Selector { valor = i % AvatarIds.Length, opciones = NombresAvatares };
        avatar.alCambiar = indice =>
        {
            string[] recursos = { "AvatarNino", "AvatarAstronauta", "AvatarCachucha",
                                  "AvatarPirata", "AvatarNina", "AvatarOso" };
            respaldo.sprite = ImagenRecurso(recursos[indice]);
            preview.Mostrar(AvatarIds[indice]);
        };
        Boton(r, "AvatarAnterior", "", new Vector2(-363, -14), new Vector2(76, 76),
            "FlechaIzq", () => CambiarAvatar(avatar, -1), 48, Uva);
        Boton(r, "AvatarSiguiente", "", new Vector2(-98, -14), new Vector2(76, 76),
            "FlechaDer", () => CambiarAvatar(avatar, 1), 48, Uva);
        avatar.alCambiar(avatar.valor);
        filas.Add(new Fila { nombre = input, categoria = categoria,
                            avatar = avatar, raiz = r.gameObject });
    }
    private static void CambiarAvatar(Selector avatar,int delta)
    {
        avatar.valor=(avatar.valor+delta+avatar.opciones.Length)%avatar.opciones.Length;
        if(avatar.alCambiar!=null) avatar.alCambiar(avatar.valor);
    }
    // TMP_InputField necesita un textViewport valido para convertir las coordenadas
    // del toque en posicion del cursor. Si falta, puede lanzar
    // RectTransformUtility.ScreenPointToWorldPointInRectangle: NullReferenceException.
    private TMP_InputField CrearInput(Transform padre,string nombre,Vector2 pos,Vector2 tam,string sugerencia)
    {
        Image bg=Fondo(padre,nombre,pos,tam,"Input",Color.white);
        bg.raycastTarget=true;
        RectTransform caja = bg.rectTransform;

        // La zona de escritura permanece DENTRO del campo, con margen para el cursor.
        RectTransform textViewport = Nuevo("Text Area", caja);
        textViewport.anchorMin = Vector2.zero;
        textViewport.anchorMax = Vector2.one;
        textViewport.offsetMin = new Vector2(24f, 8f);
        textViewport.offsetMax = new Vector2(-24f, -8f);
        Image zonaGrafica = textViewport.gameObject.AddComponent<Image>();
        zonaGrafica.color = new Color(1,1,1,0);
        zonaGrafica.raycastTarget = false;
        textViewport.gameObject.AddComponent<RectMask2D>();

        var t=Texto(textViewport,"Texto","",Vector2.zero,Vector2.zero,35,Uva);
        RectTransform textoRect = t.rectTransform;
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = textoRect.offsetMax = Vector2.zero;
        t.alignment=TextAlignmentOptions.MidlineLeft;
        t.fontStyle=FontStyles.Normal;
        t.enableAutoSizing = false;
        t.fontSize=35;

        var ph=Texto(textViewport,"Placeholder",sugerencia,Vector2.zero,Vector2.zero,27,new Color32(142,119,166,255));
        RectTransform phRect = ph.rectTransform;
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = phRect.offsetMax = Vector2.zero;
        ph.alignment=TextAlignmentOptions.MidlineLeft;
        ph.fontStyle=FontStyles.Normal;
        ph.enableAutoSizing = true;
        ph.fontSizeMin = 18;

        TMP_InputField input=bg.gameObject.AddComponent<TMP_InputField>();
        input.textViewport = textViewport;
        input.textComponent = t;
        input.placeholder = ph;
        input.characterLimit = 24;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.selectionColor = new Color32(186,137,240,140);
        input.caretColor = Uva;
        input.customCaretColor = true;
        return input;
    }
    private Selector SelectorCiclico(Transform padre,string nombre,Vector2 pos,Vector2 tam,string[] opciones,string recurso)
    {
        var s=new Selector{valor=0,opciones=opciones};
        var btn=Boton(padre,nombre,"",pos,tam,recurso,()=>
        {
            s.valor=(s.valor+1)%s.opciones.Length;
            s.etiqueta.text=s.opciones[s.valor]+"  ›";
            if(s.alCambiar!=null)s.alCambiar(s.valor);
        },41);
        s.etiqueta=btn.GetComponentInChildren<TextMeshProUGUI>();
        s.etiqueta.text=opciones[0]+"  ›";
        s.etiqueta.fontSize=37;
        s.etiqueta.fontSizeMax=37;
        s.etiqueta.fontSizeMin=22;
        s.etiqueta.margin=new Vector4(18,5,18,5);
        return s;
    }
    private void OnDestroy()
    {
        // Destruir el panel completo evita raycasters/campos TMP huérfanos al volver.
        if (canvas != null) Destroy(canvas.gameObject);
        if (spritePaneles != null) Destroy(spritePaneles);
        if (texturaPaneles != null) Destroy(texturaPaneles);
    }

    private void Iniciar()
    {
        if (!Application.CanStreamedLevelBeLoaded("03_Juego"))
        {
            MostrarPopup(false, "¡FALTA LA ESCENA!", "Agrega 03_Juego a Build Profiles para poder empezar.",
                "ENTENDIDO", null);
            return;
        }
        var jugadores = new List<GolosaTurnosDatos.Jugador>();
        var nombres = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < total; i++)
        {
            var f = filas[i];
            string nombre = f.nombre.text.Trim();
            if (string.IsNullOrEmpty(nombre))
            {
                MostrarPopup(false, "¡FALTA UN NOMBRE!",
                    "Escribe el nombre del jugador " + (i + 1) + " antes de comenzar.",
                    "ENTENDIDO", null);
                return;
            }
            if (!nombres.Add(nombre))
            {
                MostrarPopup(false, "¡NOMBRE REPETIDO!",
                    "Usa un nombre diferente para cada jugador.", "ENTENDIDO", null);
                return;
            }
            jugadores.Add(new GolosaTurnosDatos.Jugador
            {
                nombre = nombre,
                categoria = f.categoria.valor == 0 ? "Niños" : "Avanzados",
                avatarId = AvatarIds[f.avatar.valor]
            });
        }
        // Evita crear la partida hasta que la persona confirme.
        MostrarPopup(true, "¡TODO LISTO!",
            "¿Comenzamos la aventura con " + total + " jugadores?",
            "¡A JUGAR!", () =>
            {
                GolosaTurnosDatos.Iniciar(jugadores);
                SceneManager.LoadScene("03_Juego");
            }, "REVISAR");
    }

    private void SolicitarSalir()
    {
        bool hayDatos = false;
        foreach (var fila in filas)
        {
            if (!string.IsNullOrWhiteSpace(fila.nombre.text)) { hayDatos = true; break; }
        }
        if (!hayDatos) { Destroy(gameObject); return; }
        MostrarPopup(false, "¿QUIERES SALIR?",
            "Si regresas ahora, perderás los datos escritos de los jugadores.",
            "SÍ, SALIR", () => Destroy(gameObject), "QUEDARME");
    }

    // Popups de Golosa AR con recursos visuales reales. Sin controladores adicionales.
    // Los PNG se cargan como Texture2D para que funcionen aun si Unity importa PNG como Default.
    private Sprite CargarSpritePopup(string nombre)
    {
        Texture2D tex = Resources.Load<Texture2D>("GolosaTurnosUI/" + nombre);
        if (tex == null)
        {
            Debug.LogWarning("Golosa AR: falta Assets/Resources/GolosaTurnosUI/" + nombre + ".png");
            return null;
        }
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
            new Vector2(.5f, .5f), 100f);
    }

    private Image ImagenPopup(Transform padre, string nombre, Vector2 posicion,
        Vector2 tamano, string recurso)
    {
        RectTransform r = Nuevo(nombre, padre);
        Fijar(r, posicion, tamano);
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = CargarSpritePopup(recurso);
        img.color = img.sprite != null ? Color.white : new Color32(244, 211, 255, 255);
        img.raycastTarget = false;
        return img;
    }

    private void CrearVentanaPopup(Transform padre)
    {
        RectTransform capa = Nuevo("VentanasEmergentes_PorTurnos", padre);
        capa.anchorMin = Vector2.zero;
        capa.anchorMax = Vector2.one;
        capa.offsetMin = capa.offsetMax = Vector2.zero;
        ventanaPopup = capa.gameObject;
        Image oscurecer = capa.gameObject.AddComponent<Image>();
        oscurecer.color = new Color(.11f, .045f, .20f, .76f);
        oscurecer.raycastTarget = true;
        grupoPopup = capa.gameObject.AddComponent<CanvasGroup>();

        Image panel = ImagenPopup(capa, "PanelDecorado", Vector2.zero,
            new Vector2(1010, 800), "PopupGolosaConfirmacion");
        tarjetaPopup = panel.rectTransform;
        // Referencias existentes para actualizar la variante durante la ejecución.
        franjaPopup = panel;
        simboloPopup = null;

        tituloPopup = Texto(panel.transform, "Titulo", "", new Vector2(0, 225),
            new Vector2(740, 102), 60, new Color32(255, 255, 255, 255));
        tituloPopup.fontSizeMin = 35;
        mensajePopup = Texto(panel.transform, "Mensaje", "", new Vector2(83, 2),
            new Vector2(550, 185), 39, new Color32(67, 26, 113, 255));
        mensajePopup.fontStyle = FontStyles.Normal;
        mensajePopup.fontSizeMin = 27;

        botonPrincipalPopup = ImagenPopup(panel.transform, "Confirmar", new Vector2(0, -236),
            new Vector2(578, 142), "BotonPopupAmarillo");
        botonPrincipalPopup.raycastTarget = true;
        accionPrincipalPopup = botonPrincipalPopup.gameObject.AddComponent<Button>();
        accionPrincipalPopup.targetGraphic = botonPrincipalPopup;
        principalPopup = Texto(botonPrincipalPopup.transform, "TextoBoton", "", Vector2.zero,
            new Vector2(500, 110), 48, new Color32(72, 21, 128, 255));
        principalPopup.fontSizeMin = 29;
        accionPrincipalPopup.onClick.AddListener(() =>
        {
            System.Action accion = accionConfirmarPopup;
            OcultarPopup();
            if (accion != null) accion();
        });

        Image segundo = ImagenPopup(panel.transform, "Cancelar", new Vector2(220, -231),
            new Vector2(388, 128), "BotonPopupClaro");
        segundo.raycastTarget = true;
        accionSecundariaPopup = segundo.gameObject.AddComponent<Button>();
        accionSecundariaPopup.targetGraphic = segundo;
        secundarioPopup = Texto(segundo.transform, "TextoBoton", "", Vector2.zero,
            new Vector2(345, 94), 39, Uva);
        secundarioPopup.fontSizeMin = 26;
        accionSecundariaPopup.onClick.AddListener(OcultarPopup);
        ventanaPopup.SetActive(false);
    }

    private void MostrarPopup(bool exito, string titulo, string mensaje,
        string boton, System.Action alConfirmar, string alternativa = null)
    {
        if (ventanaPopup == null) { Debug.LogWarning("Golosa AR: " + mensaje); return; }
        // Recursos PNG distintos: golosa amarilla para confirmaciones, gamer morado para avisos.
        franjaPopup.sprite = CargarSpritePopup(exito ? "PopupGolosaConfirmacion" : "PopupGolosaAviso");
        franjaPopup.color = Color.white;
        tituloPopup.text = titulo;
        tituloPopup.color = exito ? new Color32(80, 20, 134, 255) : Color.white;
        mensajePopup.text = mensaje;
        principalPopup.text = boton;
        accionConfirmarPopup = alConfirmar;
        botonPrincipalPopup.sprite = CargarSpritePopup(exito ? "BotonPopupMorado" : "BotonPopupAmarillo");
        principalPopup.color = exito ? Color.white : Uva;
        bool dosBotones = !string.IsNullOrEmpty(alternativa);
        accionSecundariaPopup.gameObject.SetActive(dosBotones);
        if (dosBotones) secundarioPopup.text = alternativa;
        botonPrincipalPopup.rectTransform.anchoredPosition = dosBotones
            ? new Vector2(-170, -236) : new Vector2(0, -236);
        botonPrincipalPopup.rectTransform.sizeDelta = dosBotones
            ? new Vector2(405, 135) : new Vector2(578, 142);
        principalPopup.rectTransform.sizeDelta = dosBotones
            ? new Vector2(355, 100) : new Vector2(500, 110);
        ventanaPopup.SetActive(true);
        ventanaPopup.transform.SetAsLastSibling();
        if (transicionPopup != null) StopCoroutine(transicionPopup);
        transicionPopup = StartCoroutine(AnimarPopup());
    }

    private IEnumerator AnimarPopup()
    {
        grupoPopup.alpha = 0f;
        tarjetaPopup.localScale = Vector3.one * .86f;
        float t = 0f;
        while (t < .22f)
        {
            t += Time.unscaledDeltaTime;
            float progreso = Mathf.Clamp01(t / .22f);
            progreso = 1f - (1f - progreso) * (1f - progreso);
            grupoPopup.alpha = progreso;
            tarjetaPopup.localScale = Vector3.one * Mathf.Lerp(.86f, 1f, progreso);
            yield return null;
        }
        tarjetaPopup.localScale = Vector3.one;
        grupoPopup.alpha = 1f;
        transicionPopup = null;
    }

    private void OcultarPopup()
    {
        if (transicionPopup != null) { StopCoroutine(transicionPopup); transicionPopup = null; }
        if (ventanaPopup != null) ventanaPopup.SetActive(false);
        accionConfirmarPopup = null;
    }
}


// Vista previa independiente de ARCore. Solo representa los prefabs ya existentes.
// No se guarda ningun personaje nuevo en el proyecto.
class GolosaVistaTurno3D : MonoBehaviour
{
    private const int CapaPreview = 30;
    private RawImage destino;
    private Image respaldo;
    private GolosaCatalogoAvatares catalogo;
    private Camera camara;
    private RenderTexture textura;
    private GameObject modelo;
    private GameObject raizVisual;
    private int indice;
    private Animator animadorHumano;
    private Transform brazoIzquierdo, brazoDerecho, antebrazoDerecho;
    private bool saludoProcedural;
    private float instanteSaludo;
    private Transform brazoGenerico, antebrazoGenerico;
    private Quaternion giroBaseBrazo, giroBaseAntebrazo;
    private bool saludoGenerico;
    // Estos dos personajes tienen clips de saludo reales en los controladores originales.
    // Se reproducen solo en la copia de previsualizacion, nunca en el prefab.
    private Animator animadorSaludoReal;
    private int estadoSaludoReal;


    public void Inicializar(RawImage imagen, Image imagenRespaldo, GolosaCatalogoAvatares fuente, int numero)
    {
        destino = imagen;
        respaldo = imagenRespaldo;
        catalogo = fuente;
        indice = numero;
        // Se usa una region del mundo muy separada del tablero y de la UI.
        raizVisual = new GameObject("Preview3D_Turno_" + numero);
        raizVisual.transform.position = new Vector3(10000f + numero * 40f, 0f, 10000f);
        GameObject objetoCamara = new GameObject("CamaraPreview_Turno_" + numero);
        objetoCamara.transform.SetParent(raizVisual.transform, false);
        camara = objetoCamara.AddComponent<Camera>();
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camara.cullingMask = 1 << CapaPreview;
        camara.orthographic = true;
        camara.nearClipPlane = 0.02f;
        camara.farClipPlane = 100f;
        camara.depth = -10f;
        camara.allowHDR = false;
        textura = new RenderTexture(512, 640, 16, RenderTextureFormat.ARGB32);
        textura.Create();
        textura.filterMode = FilterMode.Bilinear;
        camara.targetTexture = textura;
        destino.texture = textura;
        destino.enabled = false;
        // Iluminacion suave solo para la vista previa 3D de Por turnos.
        // La luz antigua incidia lateralmente con intensidad alta y producia
        // sombras fuertes en la piel. No se alteran materiales ni prefabs.
        var luzObj = new GameObject("LuzFrontalPreview");
        luzObj.transform.SetParent(raizVisual.transform, false);
        luzObj.transform.localRotation = Quaternion.LookRotation(
            new Vector3(-0.25f, -0.25f, -1f).normalized, Vector3.up);
        var luz = luzObj.AddComponent<Light>();
        luz.type = LightType.Directional;
        luz.color = new Color(1f, 0.96f, 0.93f);
        luz.intensity = 0.32f;
        luz.shadows = LightShadows.None;
        luz.cullingMask = 1 << CapaPreview;

        // Relleno neutro para dar detalle al lado oscuro del rostro.
        var rellenoObj = new GameObject("LuzRellenoPreview");
        rellenoObj.transform.SetParent(raizVisual.transform, false);
        rellenoObj.transform.localRotation = Quaternion.LookRotation(
            new Vector3(0.65f, -0.2f, -1f).normalized, Vector3.up);
        var relleno = rellenoObj.AddComponent<Light>();
        relleno.type = LightType.Directional;
        relleno.color = Color.white;
        relleno.intensity = 0.08f;
        relleno.shadows = LightShadows.None;
        relleno.cullingMask = 1 << CapaPreview;
        camara.enabled = enabled && gameObject.activeInHierarchy;
    }

    public void Mostrar(string avatarId)
    {
        if (modelo != null) Destroy(modelo);
        modelo = null;
        saludoProcedural = false;
        saludoGenerico = false;
        brazoGenerico = antebrazoGenerico = null;
        brazoIzquierdo = brazoDerecho = antebrazoDerecho = null;
        animadorHumano = null;
        animadorSaludoReal = null;
        estadoSaludoReal = 0;
        GameObject prefab = catalogo != null ? catalogo.Obtener(avatarId) : null;
        if (prefab == null)
        {
            if (destino != null) destino.enabled = false;
            if (respaldo != null) respaldo.enabled = true;
            return;
        }
        modelo = Instantiate(prefab, raizVisual.transform);
        modelo.name = "Avatar3D_Preview_" + avatarId;
        modelo.SetActive(true);
        modelo.transform.localPosition = Vector3.zero;
        modelo.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        // Niña y oso: forzar el clip de saludo de su controlador específico.
        // Los controladores en Resources deben contener como estado predeterminado
        // los clips hiNiña.fbx / hiOso.fbx. No se modifica el prefab original.
        bool tieneSaludo = false;
        if (avatarId == "hiNiña" || avatarId == "hiOso")
        {
            string ruta = avatarId == "hiNiña"
                ? "GolosaTurnosAnim/controllerNina"
                : "GolosaTurnosAnim/controllerOso";
            RuntimeAnimatorController controlador = Resources.Load<RuntimeAnimatorController>(ruta);
            Animator anim = modelo.GetComponentInChildren<Animator>(true);
            if (controlador != null && anim != null)
            {
                // Importante: algunos prefabs guardan su Animator deshabilitado.
                // Activarlo solo en la copia 3D de la tarjeta.
                anim.gameObject.SetActive(true);
                anim.enabled = false;
                anim.runtimeAnimatorController = controlador;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                anim.updateMode = AnimatorUpdateMode.Normal;
                anim.speed = 1f;
                anim.enabled = true;
                anim.Rebind();
                // Fuerza la evaluación del estado predeterminado inmediatamente.
                anim.Play(0, 0, 0f);
                anim.Update(0f);
                animadorSaludoReal = anim;
                estadoSaludoReal = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
                tieneSaludo = estadoSaludoReal != 0;
                if (!tieneSaludo)
                    Debug.LogWarning("Golosa AR: Animator sin estado activo en " + avatarId);
            }
            else
            {
                Debug.LogWarning("Golosa AR: falta controlador o Animator para " + avatarId
                    + ". Comprueba Assets/Resources/GolosaTurnosAnim.");
            }
        }
        foreach (Animator anim in modelo.GetComponentsInChildren<Animator>(true))
        {
            if (animadorHumano == null && anim.isHuman) animadorHumano = anim;
            if (anim.runtimeAnimatorController == null) continue;
            foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
            {
                string n = clip.name.ToLowerInvariant();
                if (!(n.Contains("wave") || n.Contains("salud") || n.Contains("hello"))) continue;
                int estado = Animator.StringToHash(clip.name);
                if (!anim.HasState(0, estado)) continue;
                anim.Play(estado, 0, 0f);
                tieneSaludo = true;
                break;
            }
            if (tieneSaludo) break;
        }
        if (!tieneSaludo && animadorHumano != null && animadorHumano.isHuman)
        {
            brazoIzquierdo = animadorHumano.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            brazoDerecho = animadorHumano.GetBoneTransform(HumanBodyBones.RightUpperArm);
            antebrazoDerecho = animadorHumano.GetBoneTransform(HumanBodyBones.RightLowerArm);
            saludoProcedural = brazoDerecho != null && antebrazoDerecho != null;
            instanteSaludo = Time.unscaledTime;
        }
        // Modelos Generic (por ejemplo, el oso de este proyecto): detectar
        // huesos del brazo sin cambiar el rig ni el prefab usado en Individual.
        if (!tieneSaludo && !saludoProcedural)
        {
            Transform[] huesos = modelo.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in huesos)
            {
                string n = t.name.ToLowerInvariant().Replace("_", "").Replace(" ", "");
                bool derecho = n.Contains("right") || n.Contains("rarm") || n.Contains("brazo der");
                if (!derecho) continue;
                if (brazoGenerico == null && (n.Contains("upperarm") || n.Contains("arm1") || n.Contains("hombro")))
                    brazoGenerico = t;
                else if (antebrazoGenerico == null && (n.Contains("forearm") || n.Contains("lowerarm")))
                    antebrazoGenerico = t;
            }
            if (brazoGenerico != null)
            {
                giroBaseBrazo = brazoGenerico.localRotation;
                if (antebrazoGenerico != null) giroBaseAntebrazo = antebrazoGenerico.localRotation;
                saludoGenerico = true;
                instanteSaludo = Time.unscaledTime;
            }
            else if (avatarId == "hiOso")
            {
                Debug.LogWarning("Golosa AR: el oso no tiene huesos de brazo reconocibles para el saludo; necesita un clip de saludo compatible con su rig.");
            }
        }
        // Impedir que cámaras internas de prefabs sustituyan la cámara de vista previa.
        foreach (Camera c in modelo.GetComponentsInChildren<Camera>(true)) c.enabled = false;
        foreach (AudioListener a in modelo.GetComponentsInChildren<AudioListener>(true)) a.enabled = false;
        foreach (Transform t in modelo.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = CapaPreview;

        // En 01_Perfil, hi1 tiene dos materiales sustituidos en la escena:
        // "cara" (Face___merged_) y "New Material" (Body___merged_).
        // Los reutilizamos SOLO en la copia de vista previa de Por turnos.
        // No se cambia el prefab ni los materiales del modo Individual.
        if (avatarId == "AvatarNiño")
        {
            Material materialCara = Resources.Load<Material>("GolosaTurnosUI/Materiales/hi1_cara");
            Material materialCuerpo = Resources.Load<Material>("GolosaTurnosUI/Materiales/hi1_cuerpo");
            foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name.StartsWith("Face", System.StringComparison.OrdinalIgnoreCase) && materialCara != null)
                    r.sharedMaterial = materialCara;
                else if (r.name.StartsWith("Body", System.StringComparison.OrdinalIgnoreCase) && materialCuerpo != null)
                    r.sharedMaterial = materialCuerpo;
            }
            if (materialCara == null || materialCuerpo == null)
                Debug.LogWarning("Golosa AR: faltan hi1_cara o hi1_cuerpo en Resources/GolosaTurnosUI/Materiales.");
        }

        Bounds bounds = new Bounds();
        bool hayBounds = false;
        foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled) continue;
            if (!hayBounds) { bounds = r.bounds; hayBounds = true; }
            else bounds.Encapsulate(r.bounds);
        }
        if (!hayBounds || bounds.size.y <= 0.001f)
        {
            destino.enabled = false;
            respaldo.enabled = true;
            return;
        }
        // Vista de cabeza y torso, sin cortar la parte superior de la cabeza.
        // El encuadre anterior (0.29) acercaba demasiado la camara a algunos prefabs.
        // Se calcula respecto a la altura real de cada personaje y no a una escala fija.
        float alto = bounds.size.y;
        Vector3 objetivo = new Vector3(bounds.center.x,
            bounds.min.y + alto * 0.66f, bounds.center.z);
        float aspecto = (float)textura.width / textura.height;
        float semialto = alto * 0.48f;
        // Controla las poses con los brazos abiertos sin alejar excesivamente el rostro.
        float semiancho = Mathf.Min(bounds.size.x * 0.30f, alto * 0.30f);
        camara.orthographicSize = Mathf.Max(0.1f, semialto, semiancho / aspecto);
        // La camara observa desde +Z (frente habitual en Unity).
        // Si un modelo mira al otro lado, modificar SOLO su giro en el catalogo.
        camara.transform.position = objetivo + Vector3.forward * Mathf.Max(2f, alto * 2.5f);
        camara.transform.rotation = Quaternion.LookRotation(objetivo - camara.transform.position, Vector3.up);
        destino.enabled = true;
        respaldo.enabled = false;
    }

    // Animacion sencilla sin cambiar los prefabs: solo rigs Humanoid sin
    // un estado de saludo disponible. Los rigs Generic requieren su propio clip.
    private void LateUpdate()
    {
        // Mantener el clip de saludo real en bucle sin reproducir saltos
        // o animaciones posteriores del controlador.
        if (animadorSaludoReal != null)
        {
            // Tras el primer Update, Unity ya conoce el estado real de la capa.
            var info = animadorSaludoReal.GetCurrentAnimatorStateInfo(0);
            if (estadoSaludoReal == 0 && info.fullPathHash != 0)
                estadoSaludoReal = info.fullPathHash;
            if (estadoSaludoReal != 0 &&
                (info.fullPathHash != estadoSaludoReal || info.normalizedTime >= 0.98f))
                animadorSaludoReal.Play(estadoSaludoReal, 0, 0f);
            return;
        }
        if (saludoGenerico && brazoGenerico != null)
        {
            float t = Time.unscaledTime - instanteSaludo;
            // Giro local controlado: no edita el prefab ni sus animaciones.
            // En rigs Generic la orientación del hueso puede variar según el modelo.
            float movimiento = Mathf.Sin(t * 4.5f) * 14f;
            brazoGenerico.localRotation = giroBaseBrazo * Quaternion.Euler(0f, 0f, -55f + movimiento);
            if (antebrazoGenerico != null)
                antebrazoGenerico.localRotation = giroBaseAntebrazo * Quaternion.Euler(0f, 0f, -35f + movimiento * 0.7f);
        }
        if (!saludoProcedural || animadorHumano == null || modelo == null) return;
        // Orientaciones en espacio del personaje, compatibles con un rig
        // Humanoid configurado. Se vuelven a aplicar tras el Animator.
        Transform root = animadorHumano.transform;
        if (brazoIzquierdo != null)
            OrientarHueso(brazoIzquierdo, HumanBodyBones.LeftLowerArm,
                root.TransformDirection(new Vector3(-0.24f,-1f,0.06f)));
        float saludo = Mathf.Sin((Time.unscaledTime - instanteSaludo) * 5.0f);
        OrientarHueso(brazoDerecho, HumanBodyBones.RightLowerArm,
            root.TransformDirection(new Vector3(0.82f,0.7f,0.05f)));
        OrientarHueso(antebrazoDerecho, HumanBodyBones.RightHand,
            root.TransformDirection(new Vector3(0.22f + saludo * 0.25f,1f,0.10f)));
    }

    private void OrientarHueso(Transform hueso, HumanBodyBones siguiente, Vector3 direccion)
    {
        if (hueso == null || animadorHumano == null) return;
        Transform extremo = animadorHumano.GetBoneTransform(siguiente);
        if (extremo == null) return;
        Vector3 actual = extremo.position - hueso.position;
        if (actual.sqrMagnitude < 0.000001f) return;
        hueso.rotation = Quaternion.FromToRotation(actual.normalized, direccion.normalized) * hueso.rotation;
    }

    private void OnEnable() { if (camara != null) camara.enabled = true; }
    private void OnDisable() { if (camara != null) camara.enabled = false; }
    private void OnDestroy()
    {
        if (camara != null) camara.targetTexture = null;
        if (textura != null) { textura.Release(); Destroy(textura); }
        if (raizVisual != null) Destroy(raizVisual);
    }
}
