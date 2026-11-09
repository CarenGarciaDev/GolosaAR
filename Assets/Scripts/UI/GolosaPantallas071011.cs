using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presenta los desafíos y pantallas secundarias de Golosa AR sobre la escena de juego.
/// Reutiliza los datos de puntuación, historial y poderes sin crear escenas adicionales.
/// La capa visual se construye independientemente del HUD y de los componentes AR.
/// </summary>
/// <remarks>Las preguntas proceden de Resources/GolosaPreguntas (TSV UTF-8).</remarks>
public class GolosaPantallas071011 : MonoBehaviour
{
    public GolosaJuegoUI juego;
    private Canvas canvas;
    private GameObject ventana;
    private GameObject panelEspecial; // Se crea exclusivamente para Poderes, Clasificación o Registro.
    private GameObject menuAccesos;
    private Button botonMenu;
    private RectTransform area;
    private TextMeshProUGUI titulo, detalle, auxiliar;
    private readonly Button[] opciones = new Button[4];
    private readonly Image[] fondos = new Image[4];
    private readonly TextMeshProUGUI[] etiquetas = new TextMeshProUGUI[4];
    private Button confirmar, botonPista, botonEscudo, botonDoble, botonCongelar;
    private Button continuar;
    private Sprite spriteMenuTemporal;
    // Solo presentación: reutiliza la lógica de preguntas y respuestas existente.
    private Image fondoModal;
    private bool temaDesafio;
    private readonly Dictionary<string,Sprite> spritesTema = new Dictionary<string,Sprite>();
    private int seleccion = -1, correcta;
    private bool respondida;
    private Action<bool> onResponder;
    private Coroutine rutinaCerrarResultado;
    private bool resultadoPendiente;
    private readonly List<Pregunta> banco = new List<Pregunta>();
    private readonly List<int> bolsa = new List<int>();
    private string categoria;
    private bool pistaUsada;
    private int escudos = 1, pistas = 1, dobles = 1, congelar = 1;
    private bool dobleActivo;
    private bool escudoActivo;
    private bool recompensaElegida;
    private bool continuarEsDesafio;
    private readonly Color morado = new Color32(100,37,185,255), amarillo = new Color32(255,218,77,255);
    private readonly Color suave = new Color32(249,244,255,255), uva = new Color32(62,26,117,255);

    [Serializable] private class Pregunta { public string texto; public string[] opciones; }

    private void Awake()
    {
        categoria = PlayerPrefs.GetString("Golosa.Categoria", "Niños") == "Avanzados" ? "Avanzados" : "Niños";
        CargarPreguntas();
    }

    public void Vincular(GolosaJuegoUI partida) { juego = partida; CrearCanvas(); }
    public bool PuntosDobles { get { return dobleActivo; } }
    public bool ConsumirEscudo() { if (!escudoActivo) return false; escudoActivo = false; return true; }
    public void ConsumirDoble() { dobleActivo = false; }
    public int Escudos { get { return escudos; } }
    public void AgregarEscudo() { escudos++; }
    public int CantidadPreguntas { get { return banco.Count; } }

    private void CargarPreguntas()
    {
        TextAsset recurso = Resources.Load<TextAsset>("GolosaPreguntas/" + (categoria == "Niños" ? "Ninos" : "Avanzados"));
        if (recurso == null) { Debug.LogError("Golosa AR: falta Resources/GolosaPreguntas/" + (categoria == "Niños" ? "Ninos" : "Avanzados") + ".txt"); return; }
        string[] filas = recurso.text.Replace("\r", "").Split('\n');
        for (int i = 0; i < filas.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(filas[i]) || filas[i].StartsWith("#")) continue;
            string[] c = filas[i].Split('\t');
            if (c.Length != 5 || c.Any(x => string.IsNullOrWhiteSpace(x)))
            { Debug.LogError("Golosa AR: fila inválida de preguntas, línea " + (i+1)); continue; }
            banco.Add(new Pregunta { texto = c[0], opciones = new [] { c[1],c[2],c[3],c[4] } });
        }
        // El proyecto incorpora las 50 preguntas originales más preguntas nuevas.
        // La validación usa un mínimo, ya que el banco puede seguir ampliándose.
        if (banco.Count < 100)
            Debug.LogWarning("Golosa AR: categoría " + categoria + " tiene solo " +
                             banco.Count + " preguntas; se esperan al menos 100.");
        CargarBolsaPersistente();
    }

    /// <summary>
    /// Recupera las preguntas todavía no utilizadas de la categoría actual.
    /// La lista se guarda en PlayerPrefs para que cerrar o reiniciar el juego
    /// no haga que las mismas preguntas vuelvan a aparecer al principio.
    /// </summary>
    private void CargarBolsaPersistente()
    {
        bolsa.Clear();
        string clave = ClaveBolsaPreguntas;
        string guardado = PlayerPrefs.GetString(clave, "");
        if (!string.IsNullOrEmpty(guardado))
        {
            string[] partes = guardado.Split(',');
            var indicesVistos = new HashSet<int>();
            bool valido = true;
            foreach (string parte in partes)
            {
                int indice;
                if (!int.TryParse(parte, out indice) ||
                    indice < 0 || indice >= banco.Count ||
                    !indicesVistos.Add(indice))
                {
                    valido = false;
                    break;
                }
                bolsa.Add(indice);
            }
            if (valido && bolsa.Count > 0)
            {
                // Migración desde la versión de 50 preguntas: conservar las que
                // aún no han salido y añadir solamente las preguntas nuevas.
                // No reinsertar las antiguas que el jugador ya respondió.
                int tamanoAnterior = PlayerPrefs.GetInt(ClaveTamanoBanco, 50);
                if (tamanoAnterior < banco.Count)
                {
                    var nuevas = new List<int>();
                    for (int i = Mathf.Max(0, tamanoAnterior); i < banco.Count; i++)
                        if (indicesVistos.Add(i)) nuevas.Add(i);

                    // Mezclar preguntas nuevas con las pendientes anteriores;
                    // evita mostrar bloques enteros de preguntas del mismo lote.
                    bolsa.AddRange(nuevas);
                    MezclarBolsa();
                    GuardarBolsa();
                }
                PlayerPrefs.SetInt(ClaveTamanoBanco, banco.Count);
                Debug.Log("Golosa AR: " + categoria + ": " + bolsa.Count +
                          " preguntas pendientes restauradas.");
                return;
            }
            bolsa.Clear();
            Debug.LogWarning("Golosa AR: lista de preguntas agotada o inválida; se crea una nueva ronda.");
        }
        RellenarBolsa();
    }

    private string ClaveBolsaPreguntas
    {
        get { return "Golosa.Preguntas.Pendientes." +
                     (categoria == "Niños" ? "Ninos" : "Avanzados"); }
    }

    private string ClaveTamanoBanco
    {
        get { return "Golosa.Preguntas.Tamano." +
                     (categoria == "Niños" ? "Ninos" : "Avanzados"); }
    }

    private string ClaveUltimaPregunta
    {
        get { return "Golosa.Preguntas.Ultima." +
                     (categoria == "Niños" ? "Ninos" : "Avanzados"); }
    }

    /// <summary>
    /// Inicia otra ronda solo cuando se agotaron las preguntas pendientes.
    /// La mezcla Fisher-Yates mantiene un orden aleatorio, pero evita que
    /// la última pregunta de una ronda sea la primera de la siguiente.
    /// </summary>
    private void RellenarBolsa()
    {
        bolsa.Clear();
        for (int i = 0; i < banco.Count; i++) bolsa.Add(i);

        MezclarBolsa();

        // MostrarDesafio extrae desde el final de la lista.
        int ultima = PlayerPrefs.GetInt(ClaveUltimaPregunta, -1);
        if (bolsa.Count > 1 && bolsa[bolsa.Count - 1] == ultima)
        {
            int temp = bolsa[0];
            bolsa[0] = bolsa[bolsa.Count - 1];
            bolsa[bolsa.Count - 1] = temp;
        }
        GuardarBolsa();
    }

    /// <summary>Mezcla las preguntas pendientes mediante Fisher-Yates.</summary>
    private void MezclarBolsa()
    {
        for (int i = bolsa.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int temp = bolsa[i];
            bolsa[i] = bolsa[j];
            bolsa[j] = temp;
        }
    }

    /// <summary>
    /// Guarda el orden restante inmediatamente después de asignar una pregunta.
    /// Las categorías cuentan con claves separadas y conservan su progreso.
    /// </summary>
    private void GuardarBolsa()
    {
        PlayerPrefs.SetString(ClaveBolsaPreguntas, string.Join(",", bolsa));
        PlayerPrefs.SetInt(ClaveTamanoBanco, banco.Count);
        PlayerPrefs.Save();
    }

    private RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); var r=go.GetComponent<RectTransform>();
        r.SetParent(parent,false); r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.pivot=new Vector2(.5f,.5f);
        r.anchoredPosition=pos; r.sizeDelta=size; return r;
    }
    private Image Fondo(RectTransform rt, Color color, Sprite sprite=null)
    {
        var im=rt.gameObject.AddComponent<Image>(); im.sprite=sprite; im.color=color;
        im.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        return im;
    }
    private TextMeshProUGUI Texto(Transform parent, string name, string value, Vector2 pos, Vector2 size, int fs, Color color)
    {
        var rt=Rect(parent,name,pos,size); var t=rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text=value; t.color=color; t.fontSize=fs; t.alignment=TextAlignmentOptions.Center;
        t.enableAutoSizing=true; t.fontSizeMin=17; t.fontSizeMax=fs; t.textWrappingMode=TextWrappingModes.Normal;
        t.raycastTarget=false; return t;
    }
    private Button Boton(Transform p,string name,string label,Vector2 pos,Vector2 size,Color color,Action click,int sizeText=31)
    {
        var rt=Rect(p,name,pos,size); var img=Fondo(rt,color);
        var b=rt.gameObject.AddComponent<Button>(); b.targetGraphic=img; b.onClick.AddListener(()=>click());
        Texto(rt,"Texto",label,Vector2.zero,size- new Vector2(20,10),sizeText,color==amarillo?uva:Color.white);
        return b;
    }
    private Sprite RecursoTema(string nombre)
    {
        if (spritesTema.ContainsKey(nombre)) return spritesTema[nombre];
        Sprite sprite = Resources.Load<Sprite>("GolosaDesafios/" + nombre);
        if (sprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>("GolosaDesafios/" + nombre);
            if (texture != null)
                sprite = Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100f);
        }
        spritesTema[nombre] = sprite;
        return sprite;
    }
    private void EstilizarBoton(Button b, string recurso, Color texto)
    {
        Image image = b.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = RecursoTema(recurso);
            image.color = Color.white;
            image.type = Image.Type.Simple;
        }
        TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.color = texto;
    }
    // Diseño de preguntas exclusivamente: HUD, tablero, controles y RA se conservan.
    // Cuatro opciones en columna: se evita que palabras largas se recorten en
    // las tarjetas angostas de dos columnas de las versiones anteriores.
    private void DistribucionDesafio(bool avanzada)
    {
        temaDesafio = true;
        string tema = avanzada ? "Avanzados" : "Ninos";
        RectTransform modal = ventana.GetComponent<RectTransform>();
        modal.sizeDelta = new Vector2(940, 1140);
        modal.anchoredPosition = new Vector2(0, -5);
        fondoModal.sprite = RecursoTema("Panel" + tema);
        fondoModal.color = Color.white;
        fondoModal.type = Image.Type.Simple;

        titulo.rectTransform.anchoredPosition = new Vector2(0, 440);
        titulo.rectTransform.sizeDelta = new Vector2(750, 112);
        titulo.fontSize = 49;
        titulo.fontSizeMin = 36;
        titulo.fontSizeMax = 49;
        titulo.fontStyle = FontStyles.Bold;
        titulo.color = avanzada ? Color.white : uva;

        // La pregunta puede ocupar hasta cuatro líneas, ajustando su tamaño
        // sin invadir las respuestas. Nunca se corta el contenido original.
        detalle.rectTransform.anchoredPosition = new Vector2(0, 258);
        detalle.rectTransform.sizeDelta = new Vector2(765, 188);
        detalle.enableAutoSizing = true;
        detalle.fontSize = 36;
        detalle.fontSizeMin = 25;
        detalle.fontSizeMax = 36;
        detalle.textWrappingMode = TextWrappingModes.Normal;
        detalle.overflowMode = TextOverflowModes.Ellipsis;
        detalle.fontStyle = FontStyles.Bold;
        detalle.margin = new Vector4(10, 6, 10, 6);
        detalle.color = avanzada ? Color.white : uva;

        // Mensajes de pista y resultado entre la última respuesta y Confirmar.
        auxiliar.rectTransform.anchoredPosition = new Vector2(0, -324);
        auxiliar.rectTransform.sizeDelta = new Vector2(810, 49);
        auxiliar.fontSize = 26;
        auxiliar.fontSizeMin = 18;
        auxiliar.fontSizeMax = 26;
        auxiliar.enableAutoSizing = true;
        auxiliar.textWrappingMode = TextWrappingModes.Normal;
        auxiliar.color = avanzada ? new Color32(158, 251, 255, 255) : Color.white;

        for (int i = 0; i < 4; i++)
        {
            RectTransform rt = opciones[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 54 - i * 98);
            rt.sizeDelta = new Vector2(830, 91);
            fondos[i].sprite = RecursoTema("Respuesta" + tema);
            fondos[i].type = Image.Type.Simple;
            fondos[i].color = Color.white;

            TextMeshProUGUI etiqueta = etiquetas[i];
            etiqueta.rectTransform.anchoredPosition = Vector2.zero;
            etiqueta.rectTransform.sizeDelta = new Vector2(748, 82);
            etiqueta.enableAutoSizing = true;
            etiqueta.fontSize = 34;
            etiqueta.fontSizeMin = 23;
            etiqueta.fontSizeMax = 34;
            etiqueta.fontStyle = FontStyles.Bold;
            etiqueta.textWrappingMode = TextWrappingModes.Normal;
            etiqueta.overflowMode = TextOverflowModes.Ellipsis;
            etiqueta.margin = new Vector4(12, 5, 12, 5);
            etiqueta.alignment = TextAlignmentOptions.Center;
            etiqueta.color = avanzada ? Color.white : uva;
        }

        foreach (Button b in new[] { confirmar, continuar })
        {
            RectTransform rt = b.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -399);
            rt.sizeDelta = new Vector2(790, 89);
            EstilizarBoton(b, "Confirmar" + tema, uva);
            TextMeshProUGUI label = b.GetComponentInChildren<TextMeshProUGUI>();
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSize = 36;
            label.fontSizeMin = 24;
            label.fontSizeMax = 36;
            label.rectTransform.anchoredPosition = Vector2.zero;
            label.rectTransform.sizeDelta = new Vector2(718, 78);
            ColorBlock estados = b.colors;
            estados.normalColor = Color.white;
            estados.highlightedColor = new Color32(255, 247, 225, 255);
            estados.pressedColor = new Color32(243, 216, 183, 255);
            estados.selectedColor = Color.white;
            estados.disabledColor = new Color(1f, 1f, 1f, .76f);
            estados.colorMultiplier = 1f;
            b.colors = estados;
        }

        var poderes = new[] { botonPista, botonEscudo, botonDoble, botonCongelar };
        var tipos = new[] { "Pista", "Escudo", "X2", "Pausa" };
        for (int i = 0; i < poderes.Length; i++)
        {
            RectTransform rt = poderes[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-333 + i * 222, -499);
            rt.sizeDelta = new Vector2(215, 105);
            EstilizarBoton(poderes[i], "Poder" + tipos[i] + tema,
                avanzada ? Color.white : uva);
            TextMeshProUGUI txt = poderes[i].GetComponentInChildren<TextMeshProUGUI>();
            // Los iconos ya están integrados en la imagen PNG del botón.
            txt.rectTransform.anchoredPosition = new Vector2(27, 0);
            txt.rectTransform.sizeDelta = new Vector2(142, 95);
            txt.fontSize = 27;
            txt.fontSizeMax = 27;
            txt.fontSizeMin = 19;
            txt.enableAutoSizing = true;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            ColorBlock estados = poderes[i].colors;
            estados.disabledColor = new Color(1f, 1f, 1f, .42f);
            estados.normalColor = Color.white;
            poderes[i].colors = estados;
        }
    }
    private void ActualizarFondoSeleccion()
    {
        if (!temaDesafio) return;
        string tema = categoria == "Avanzados" ? "Avanzados" : "Ninos";
        for (int i = 0; i < fondos.Length; i++)
        {
            fondos[i].sprite = RecursoTema(i == seleccion ? "RespuestaSeleccion" + tema : "Respuesta" + tema);
            fondos[i].color = Color.white;
            etiquetas[i].color = i == seleccion ? uva : (categoria == "Avanzados" ? Color.white : uva);
        }
    }
    private void ActualizarDisponibilidadPoderes()
    {
        if (!temaDesafio || respondida) return;
        botonPista.interactable = pistas > 0 && !pistaUsada;
        botonEscudo.interactable = escudos > 0 && !escudoActivo;
        botonDoble.interactable = dobles > 0 && !dobleActivo;
        botonCongelar.interactable = congelar > 0;
        // Muestra también los poderes agotados, atenuados, para no mover los demás botones.
    }
    private void DistribucionGeneral()
    {
        // Restablece siempre el layout base al salir de un panel especial.
        // El diseño de desafíos se vuelve a aplicar solo al abrir una pregunta.
        temaDesafio=false;
        RectTransform modal=ventana.GetComponent<RectTransform>();
        modal.sizeDelta=new Vector2(1010,1510);
        modal.anchoredPosition=new Vector2(0,-35);
        fondoModal.sprite=null;
        fondoModal.color=new Color(.34f,.10f,.64f,.97f);
        titulo.rectTransform.anchoredPosition=new Vector2(0,650);
        titulo.rectTransform.sizeDelta=new Vector2(930,140);
        titulo.fontSizeMax=55; titulo.color=amarillo;
        detalle.rectTransform.anchoredPosition=new Vector2(0,395);
        detalle.rectTransform.sizeDelta=new Vector2(920,310);
        detalle.fontSizeMax=42; detalle.color=Color.white;
        auxiliar.rectTransform.anchoredPosition=new Vector2(0,-440);
        auxiliar.rectTransform.sizeDelta=new Vector2(930,125);
        auxiliar.fontSizeMax=31; auxiliar.color=amarillo;
        for (int i=0;i<4;i++)
        {
            var r=opciones[i].GetComponent<RectTransform>();
            r.anchoredPosition=new Vector2(0,120-i*150);
            r.sizeDelta=new Vector2(900,125);
            fondos[i].sprite=null;
            fondos[i].color=suave;
            etiquetas[i].rectTransform.sizeDelta=new Vector2(860,115);
            etiquetas[i].fontSizeMax=35; etiquetas[i].color=uva;
        }
        foreach(Button b in new[]{confirmar,continuar})
        {
            var r=b.GetComponent<RectTransform>();
            r.anchoredPosition=new Vector2(0,-580);
            r.sizeDelta=new Vector2(840,108);
            Image im=b.GetComponent<Image>();
            im.sprite=null;im.color=amarillo;
            b.GetComponentInChildren<TextMeshProUGUI>().color=uva;
        }
        var poderes=new[]{botonPista,botonEscudo,botonDoble,botonCongelar};
        for(int i=0;i<4;i++)
        {
            var r=poderes[i].GetComponent<RectTransform>();
            r.anchoredPosition=new Vector2(-350+i*232,-710);
            r.sizeDelta=new Vector2(220,70);
            var im=poderes[i].GetComponent<Image>();im.sprite=null;im.color=morado;
            poderes[i].GetComponentInChildren<TextMeshProUGUI>().color=Color.white;
        }
    }
    private void CrearCanvas()
    {
        if(canvas != null)return;
        var go=new GameObject("Golosa_Overlay_07_10_11",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=500;
        var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1080,2340); scaler.matchWidthOrHeight=.5f;
        area=Rect(go.transform,"SafeArea",Vector2.zero,Vector2.zero);
        Rect safe=Screen.safeArea; area.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);
        area.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
        area.offsetMin=area.offsetMax=Vector2.zero;
        // Accesos secundarios plegables: no ocupan el centro de la cámara AR.
        var botonRt = Rect(area, "BotonMenuJuego", new Vector2(-94,-94), new Vector2(110,110));
        botonRt.anchorMin = botonRt.anchorMax = new Vector2(1f,1f);
        botonRt.anchoredPosition = new Vector2(-94,-94);
        var botonImg = Fondo(botonRt, Color.white);
        Texture2D texturaMenu = Resources.Load<Texture2D>("GolosaARHUD/BotonMenu");
        if (texturaMenu != null)
        {
            // El sprite se crea para texturas importadas como Default o Sprite.
            var icono = Sprite.Create(texturaMenu, new Rect(0,0,texturaMenu.width,texturaMenu.height),
                new Vector2(.5f,.5f),100f);
            botonImg.sprite = icono;
            spriteMenuTemporal = icono;
        }
        botonMenu = botonRt.gameObject.AddComponent<Button>();
        botonMenu.targetGraphic = botonImg;
        botonMenu.onClick.AddListener(AlternarMenuJuego);
        // Solo tres accesos: Poderes, Clasificación y Registro.
        menuAccesos = Rect(area, "MenuDesplegable", new Vector2(-240,-335), new Vector2(390,342)).gameObject;
        var menuRt=menuAccesos.GetComponent<RectTransform>();
        menuRt.anchorMin=menuRt.anchorMax=new Vector2(1f,1f);
        menuRt.anchoredPosition=new Vector2(-242,-335);
        Fondo(menuRt,new Color(0.96f,0.91f,1f,0.97f));
        Boton(menuRt,"Poderes","PODERES",new Vector2(0,104),new Vector2(344,87),morado,
            ()=>{ menuAccesos.SetActive(false); MostrarPoderes(); },30);
        Boton(menuRt,"Clasificacion","CLASIFICACIÓN",new Vector2(0,0),new Vector2(344,87),morado,
            ()=>{ menuAccesos.SetActive(false); MostrarClasificacion(); },28);
        Boton(menuRt,"Registro","REGISTRO",new Vector2(0,-104),new Vector2(344,87),morado,
            ()=>{ menuAccesos.SetActive(false); MostrarRegistro(); },30);
        menuAccesos.SetActive(false);
        botonMenu.gameObject.SetActive(false); // Solo al iniciar la partida.
        ventana=Rect(area,"Modal",new Vector2(0,-35),new Vector2(1010,1510)).gameObject;
        fondoModal = Fondo(ventana.GetComponent<RectTransform>(),new Color(0.34f,.10f,.64f,.97f));
        titulo=Texto(ventana.transform,"Titulo","",new Vector2(0,650),new Vector2(930,140),55,amarillo);
        detalle=Texto(ventana.transform,"Detalle","",new Vector2(0,395),new Vector2(920,310),42,Color.white);
        auxiliar=Texto(ventana.transform,"Auxiliar","",new Vector2(0,-440),new Vector2(930,125),31,amarillo);
        for(int i=0;i<4;i++)
        {
            int n=i;
            var rt=Rect(ventana.transform,"Opcion"+i,new Vector2(0,120-i*150),new Vector2(900,125));
            fondos[i]=Fondo(rt,suave); opciones[i]=rt.gameObject.AddComponent<Button>(); opciones[i].targetGraphic=fondos[i];
            opciones[i].onClick.AddListener(()=>Seleccionar(n));
            etiquetas[i]=Texto(rt,"Etiqueta","",Vector2.zero,new Vector2(860,115),35,uva);
        }
        confirmar=Boton(ventana.transform,"Confirmar","CONFIRMAR RESPUESTA",new Vector2(0,-580),new Vector2(840,108),amarillo,Confirmar,37);
        continuar=Boton(ventana.transform,"Continuar","CONTINUAR",new Vector2(0,-580),new Vector2(840,108),amarillo,Continuar,37);
        botonPista=Boton(ventana.transform,"Pista","💡 PISTA",new Vector2(-350,-710),new Vector2(220,70),morado,PistaActivadaEnPregunta,24);
        botonEscudo=Boton(ventana.transform,"Escudo","🛡 ESCUDO",new Vector2(-118,-710),new Vector2(220,70),morado,ActivarEscudoDuranteDesafio,23);
        botonDoble=Boton(ventana.transform,"Doble","★ X2",new Vector2(114,-710),new Vector2(220,70),morado,ActivarDobleDuranteDesafio,25);
        botonCongelar=Boton(ventana.transform,"Congelar","⏱ PAUSA",new Vector2(348,-710),new Vector2(220,70),morado,ActivarCongelacionDuranteDesafio,23);
        ventana.SetActive(false);
    }
    // El menú se habilita cuando el jugador ya ha confirmado el comienzo.
    public void ActivarMenuJuego()
    {
        if (botonMenu != null) botonMenu.gameObject.SetActive(true);
    }

    private void AlternarMenuJuego()
    {
        if (menuAccesos != null) menuAccesos.SetActive(!menuAccesos.activeSelf);
    }

    private void LimpiarVentana(string encabezado,string cuerpo)
    {
        if (rutinaCerrarResultado != null) { StopCoroutine(rutinaCerrarResultado); rutinaCerrarResultado = null; }
        resultadoPendiente = false;
        CrearCanvas();
        // Restablece los elementos genéricos antes de abrir otro tipo de modal.
        // De lo contrario, los gráficos exclusivos de una ventana podrían quedar
        // superpuestos a los desafíos y afectar sus controles interactivos.
        if (panelEspecial != null)
        {
            panelEspecial.SetActive(false);
            Destroy(panelEspecial);
            panelEspecial = null;
        }
        DistribucionGeneral();
        titulo.gameObject.SetActive(true);
        detalle.gameObject.SetActive(true);
        auxiliar.gameObject.SetActive(true);
        ventana.SetActive(true); titulo.text=encabezado; detalle.text=cuerpo; continuarEsDesafio=false;
        auxiliar.text=""; confirmar.gameObject.SetActive(false); continuar.gameObject.SetActive(false);
        if(botonPista!=null)botonPista.gameObject.SetActive(false);
        if(botonEscudo!=null)botonEscudo.gameObject.SetActive(false);
        if(botonDoble!=null)botonDoble.gameObject.SetActive(false);
        if(botonCongelar!=null)botonCongelar.gameObject.SetActive(false);
        for(int i=0;i<4;i++) opciones[i].gameObject.SetActive(false);
    }
    private void Opcion(int index,string text,Action action)
    {
        opciones[index].gameObject.SetActive(true); opciones[index].interactable=true; fondos[index].color=Color.white; etiquetas[index].text=text;
        opciones[index].onClick.RemoveAllListeners(); opciones[index].onClick.AddListener(()=>action());
    }
    public void MostrarDesafio(Action<bool> callback)
    {
        if(banco.Count==0) { LimpiarVentana("FALTAN PREGUNTAS","Debes incorporar las preguntas originales de la categoría " + categoria + "."); continuar.gameObject.SetActive(true); return; }
        Pregunta p;
        // Aproximadamente dos de cada cinco desafíos usan números variables:
        // requieren razonamiento aunque se recuerden las preguntas originales.
        if (UnityEngine.Random.value < 0.40f)
        {
            p = GenerarPreguntaVariable();
        }
        else
        {
            if (bolsa.Count == 0) RellenarBolsa();
            int ix = bolsa[bolsa.Count - 1];
            bolsa.RemoveAt(bolsa.Count - 1);
            PlayerPrefs.SetInt(ClaveUltimaPregunta, ix);
            GuardarBolsa();
            p = banco[ix];
        }
        var indices=new List<int>{0,1,2,3};
        for(int i=3;i>0;i--){ int j=UnityEngine.Random.Range(0,i+1);int t=indices[i];indices[i]=indices[j];indices[j]=t; }
        correcta=indices.IndexOf(0); seleccion=-1;respondida=false;pistaUsada=false;onResponder=callback;
        LimpiarVentana("DESAFÍO " + categoria.ToUpper(),p.texto); continuarEsDesafio=true;
        DistribucionDesafio(categoria == "Avanzados");
        for(int i=0;i<4;i++){int n=i;Opcion(i,p.opciones[indices[i]],()=>Seleccionar(n));}
        confirmar.gameObject.SetActive(true); confirmar.interactable=false;
        botonPista.gameObject.SetActive(true);
        botonEscudo.gameObject.SetActive(true);
        botonDoble.gameObject.SetActive(true);
        botonCongelar.gameObject.SetActive(true);
        ActualizarEtiquetasPoderes();
        ActualizarFondoSeleccion();
    }

    /// <summary>
    /// Crea desafíos con valores numéricos nuevos para evitar el aprendizaje
    /// mecánico de una respuesta fija. Son adicionales a las 100 preguntas por
    /// categoría; no consumen la bolsa persistente de preguntas originales.
    /// </summary>
    private Pregunta GenerarPreguntaVariable()
    {
        int tipoAnterior = PlayerPrefs.GetInt(
            categoria == "Niños" ? "Golosa.Preguntas.TipoAnterior.N" :
                                    "Golosa.Preguntas.TipoAnterior.A", -1);
        int tipo = UnityEngine.Random.Range(0, 6);
        if (tipo == tipoAnterior) tipo = (tipo + UnityEngine.Random.Range(1, 6)) % 6;
        PlayerPrefs.SetInt(categoria == "Niños" ? "Golosa.Preguntas.TipoAnterior.N" :
                                                  "Golosa.Preguntas.TipoAnterior.A", tipo);

        int a, b, r;
        if (categoria == "Niños")
        {
            switch (tipo)
            {
                case 0:
                    a = UnityEngine.Random.Range(8, 36);
                    b = UnityEngine.Random.Range(3, 18);
                    r = a + b;
                    return PreguntaNumerica("Tienes " + a + " fichas y te regalan " + b +
                        ". ¿Cuántas tienes ahora?", r, "", 2);
                case 1:
                    a = UnityEngine.Random.Range(22, 65);
                    b = UnityEngine.Random.Range(5, 20);
                    r = a - b;
                    return PreguntaNumerica("Había " + a + " frutas y se vendieron " + b +
                        ". ¿Cuántas quedan?", r, "", 3);
                case 2:
                    a = UnityEngine.Random.Range(3, 10);
                    b = UnityEngine.Random.Range(3, 10);
                    r = a * b;
                    return PreguntaNumerica("Hay " + a + " paquetes con " + b +
                        " pegatinas cada uno. ¿Cuántas pegatinas hay?", r, "", 4);
                case 3:
                    a = UnityEngine.Random.Range(4, 12);
                    b = UnityEngine.Random.Range(3, 9);
                    r = a;
                    return PreguntaNumerica("Repartes " + (a*b) + " caramelos entre " + b +
                        " amigos por igual. ¿Cuántos recibe cada uno?", r, "", 2);
                case 4:
                    a = UnityEngine.Random.Range(3, 17);
                    b = UnityEngine.Random.Range(2, 6);
                    r = a * b;
                    return PreguntaNumerica("Una golosa tiene " + a + " casillas por fila y " +
                        b + " filas iguales. ¿Cuántas casillas hay?", r, "", 3);
                default:
                    a = UnityEngine.Random.Range(4, 16);
                    b = UnityEngine.Random.Range(2, 7);
                    r = a + b;
                    return PreguntaNumerica("Si avanzaste " + a + " casillas y luego " + b +
                        " más, ¿cuántas avanzaste en total?", r, "", 2);
            }
        }

        switch (tipo)
        {
            case 0:
                a = UnityEngine.Random.Range(4, 19) * 20;
                b = UnityEngine.Random.Range(1, 6) * 5;
                r = a * b / 100;
                return PreguntaNumerica("¿Cuánto es el " + b + " % de " + a + "?", r, "", 5);
            case 1:
                a = UnityEngine.Random.Range(5, 18);
                b = UnityEngine.Random.Range(2, 10);
                r = a;
                return PreguntaNumerica("Resuelve: " + b + "x + " + (b+3) + " = " +
                    (b*a+b+3) + ". ¿Cuánto vale x?", r, "", 2);
            case 2:
                a = UnityEngine.Random.Range(10, 31) * 5;
                b = UnityEngine.Random.Range(2, 6);
                r = a * b;
                return PreguntaNumerica("Un vehículo recorre " + a +
                    " km cada hora durante " + b + " horas. ¿Cuántos km recorre?", r, " km", 10);
            case 3:
                a = UnityEngine.Random.Range(6, 22);
                b = UnityEngine.Random.Range(2, 9);
                r = (a+b) * 2;
                return PreguntaNumerica("¿Cuál es el perímetro de un rectángulo de " + a +
                    " cm por " + b + " cm?", r, " cm", 4);
            case 4:
                a = UnityEngine.Random.Range(4, 20);
                b = UnityEngine.Random.Range(2, 9);
                r = a*b;
                return PreguntaNumerica("El promedio de " + (r-b) + ", " + r + " y " +
                    (r+b) + " es...", r, "", 2);
            default:
                a = UnityEngine.Random.Range(8, 21) * 20;
                b = UnityEngine.Random.Range(2, 6) * 5;
                r = a * b / 100;
                return PreguntaNumerica("Una compra de " + a + " monedas tiene un descuento del " +
                    b + " %. ¿Cuánto dinero te descuentan?", r, " monedas", 5);
        }
    }

    /// <summary>
    /// Construye cuatro alternativas distintas y plausibles: la posición cero
    /// es la respuesta correcta; MostrarDesafio mezcla las posiciones después.
    /// </summary>
    private static Pregunta PreguntaNumerica(string enunciado, int valor,
                                             string unidad, int paso)
    {
        var valores = new List<int> { valor };
        int[] desplazamientos = { paso, -paso, paso*2, -paso*2,
                                  paso*3, -paso*3, 1, -1, 5, -5 };
        for (int i = desplazamientos.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int temp = desplazamientos[i];
            desplazamientos[i] = desplazamientos[j];
            desplazamientos[j] = temp;
        }
        foreach (int cambio in desplazamientos)
        {
            int candidato = valor + cambio;
            if (candidato > 0 && !valores.Contains(candidato)) valores.Add(candidato);
            if (valores.Count == 4) break;
        }
        // Para respuestas pequeñas, garantizar cuatro alternativas válidas.
        for (int candidato = 1; valores.Count < 4; candidato++)
            if (!valores.Contains(candidato)) valores.Add(candidato);
        return new Pregunta { texto = enunciado,
            opciones = valores.Select(n => n + unidad).ToArray() };
    }

    private void ActualizarEtiquetasPoderes()
    {
        if(botonEscudo!=null)botonEscudo.GetComponentInChildren<TextMeshProUGUI>().text = escudoActivo ? "ESCUDO\nACTIVO" : "ESCUDO\n("+escudos+")";
        if(botonDoble!=null)botonDoble.GetComponentInChildren<TextMeshProUGUI>().text = dobleActivo ? "X2\nACTIVO" : "X2\n("+dobles+")";
        if(botonCongelar!=null)botonCongelar.GetComponentInChildren<TextMeshProUGUI>().text = "PAUSA\n("+congelar+")";
        if(botonPista!=null)botonPista.GetComponentInChildren<TextMeshProUGUI>().text = "PISTA\n("+pistas+")";
        ActualizarDisponibilidadPoderes();
    }
    private void ActivarEscudoDuranteDesafio()
    {
        if(respondida || escudoActivo || escudos<=0) return;
        escudos--; escudoActivo=true; auxiliar.text="ESCUDO listo: protege de un fallo";
        ActualizarEtiquetasPoderes();
    }
    private void ActivarDobleDuranteDesafio()
    {
        if(respondida || dobleActivo || dobles<=0) return;
        dobles--; dobleActivo=true; auxiliar.text="X2 listo: +20 por acierto";
        ActualizarEtiquetasPoderes();
    }
    private void ActivarCongelacionDuranteDesafio()
    {
        if(respondida || congelar<=0 || juego==null) return;
        congelar--; juego.CongelarTemporizador(); auxiliar.text="Reloj congelado durante 10 segundos";
        ActualizarEtiquetasPoderes();
    }

    private void Seleccionar(int i)
    {
        if(respondida)return;
        seleccion=i;confirmar.interactable=true;
        ActualizarFondoSeleccion();
    }
    private void Confirmar()
    {
        if(seleccion<0||respondida)return;
        respondida=true;bool acierto=seleccion==correcta;
        auxiliar.text=(acierto?"★ ¡CORRECTO!":"INCORRECTO")+"  · Respuesta: "+etiquetas[correcta].text;
        for(int i=0;i<4;i++) {opciones[i].interactable=false;fondos[i].color=i==correcta?new Color32(115,241,166,255):i==seleccion?new Color32(255,151,163,255):Color.white;}
        confirmar.gameObject.SetActive(false); if(botonPista!=null)botonPista.gameObject.SetActive(false);
        if(botonEscudo!=null)botonEscudo.gameObject.SetActive(false);
        if(botonDoble!=null)botonDoble.gameObject.SetActive(false);
        if(botonCongelar!=null)botonCongelar.gameObject.SetActive(false);
        continuar.gameObject.SetActive(true);
        resultadoPendiente = true;
        // La respuesta se muestra brevemente; luego vuelve al tablero antes del salto.
        rutinaCerrarResultado = StartCoroutine(CerrarResultadoAutomaticamente(acierto));
    }
    private IEnumerator CerrarResultadoAutomaticamente(bool acierto)
    {
        yield return new WaitForSecondsRealtime(1.2f);
        rutinaCerrarResultado = null;
        CerrarResultadoYAplicar(acierto);
    }
    private void CerrarResultadoYAplicar(bool acierto)
    {
        if (!resultadoPendiente) return;
        resultadoPendiente = false;
        continuarEsDesafio = false;
        ventana.SetActive(false);
        Action<bool> callback = onResponder;
        onResponder = null;
        if (callback != null) callback(acierto);
    }
    private void Continuar()
    {
        if (resultadoPendiente)
        {
            if (rutinaCerrarResultado != null) { StopCoroutine(rutinaCerrarResultado); rutinaCerrarResultado = null; }
            CerrarResultadoYAplicar(seleccion == correcta);
            return;
        }
        continuarEsDesafio = false;
        ventana.SetActive(false);
    }
    /// <summary>
    /// Construye el panel claro compartido por Poderes, Clasificación y Registro.
    /// Los rótulos y valores son TextMeshPro dinámicos: los PNG solo contienen
    /// elementos gráficos decorativos, nunca datos simulados.
    /// </summary>
    private RectTransform AbrirPanelEspecial(string recurso, string encabezado, string subtitulo)
    {
        LimpiarVentana(encabezado, "");
        var modal = ventana.GetComponent<RectTransform>();
        modal.sizeDelta = new Vector2(940, 1380);
        modal.anchoredPosition = new Vector2(0, -20);
        fondoModal.color = Color.clear;

        titulo.gameObject.SetActive(false);
        detalle.gameObject.SetActive(false);
        auxiliar.gameObject.SetActive(false);

        var panel = Rect(ventana.transform, "Contenido_" + recurso,
            Vector2.zero, new Vector2(940, 1380));
        panelEspecial = panel.gameObject;
        Image ilustracion = Fondo(panel, Color.white, RecursoVentana(recurso));
        if (ilustracion.sprite == null)
            ilustracion.color = new Color32(250, 246, 255, 255);
        ilustracion.raycastTarget = false;

        // El título encaja dentro de la franja violeta del PNG (x:134-806).
        TextMeshProUGUI cabecera = Texto(panel, "Encabezado", encabezado,
            new Vector2(0, 505), new Vector2(655, 98), 57, Color.white);
        cabecera.fontStyle = FontStyles.Bold;
        cabecera.fontSizeMin = 40;
        TextMeshProUGUI subtituloTMP = Texto(panel, "Subtitulo", subtitulo,
            new Vector2(0, 385), new Vector2(805, 66), 31, uva);
        subtituloTMP.fontSizeMin = 23;
        return panel;
    }

    /// <summary>Obtiene las ilustraciones desde Resources incluso si Unity las importó como Texture.</summary>
    private Sprite RecursoVentana(string nombre)
    {
        const string carpeta = "GolosaVentanas/";
        Sprite sprite = Resources.Load<Sprite>(carpeta + nombre);
        if (sprite != null) return sprite;
        Texture2D textura = Resources.Load<Texture2D>(carpeta + nombre);
        if (textura == null)
        {
            Debug.LogWarning("Golosa AR: falta recurso visual " + carpeta + nombre);
            return null;
        }
        return Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height),
            new Vector2(.5f, .5f), 100f);
    }

    /// <summary>
    /// Dibuja un botón ilustrado. La acción y el estado habilitado proceden de
    /// las variables originales de la partida; el recurso visual no altera lógica.
    /// </summary>
    private Button BotonIlustrado(Transform padre, string identificador, string recurso,
        string etiqueta, Vector2 posicion, Vector2 tamano, Action accion, bool disponible = true)
    {
        RectTransform rt = Rect(padre, identificador, posicion, tamano);
        Image fondo = Fondo(rt, Color.white, RecursoVentana(recurso));
        if (fondo.sprite == null) fondo.color = new Color32(246, 235, 255, 255);
        Button boton = rt.gameObject.AddComponent<Button>();
        boton.targetGraphic = fondo;
        boton.onClick.AddListener(() => accion());
        boton.interactable = disponible;
        ColorBlock colores = boton.colors;
        colores.pressedColor = new Color(.90f, .83f, .96f, 1f);
        colores.disabledColor = new Color(1f, 1f, 1f, .48f);
        boton.colors = colores;

        TextMeshProUGUI texto = Texto(rt, "Etiqueta", etiqueta,
            new Vector2(82, 0), new Vector2(tamano.x - 245, tamano.y - 22),
            40, uva);
        texto.fontStyle = FontStyles.Bold;
        texto.fontSizeMin = 27;
        return boton;
    }

    private void BotonCerrarEspecial(Transform padre, string etiqueta = "VOLVER AL JUEGO")
    {
        Button boton = BotonIlustrado(padre, "VolverAlJuego", "BotonContinuar",
            etiqueta, new Vector2(0, -562), new Vector2(730, 135),
            () => CerrarPanel());
        TextMeshProUGUI texto = boton.GetComponentInChildren<TextMeshProUGUI>();
        texto.rectTransform.anchoredPosition = Vector2.zero;
        texto.rectTransform.sizeDelta = new Vector2(670, 100);
        texto.fontSize = 43;
        texto.fontSizeMax = 43;
    }

    /// <summary>Ofrece los poderes disponibles y respeta los contadores y estados activos.</summary>
    private void MostrarPoderes()
    {
        RectTransform panel = AbrirPanelEspecial("PanelPoderes", "PODERES",
            "Selecciona un poder disponible");
        int[] cantidades = { pistas, escudos, dobles, congelar };
        string[] nombres = { "PISTA", "ESCUDO", "DOBLE PUNTUACIÓN", "CONGELAR 10 S" };
        string[] imagenes = { "PoderPista", "PoderEscudo", "PoderDoble", "PoderPausa" };
        Action[] acciones =
        {
            () => MostrarAvisoPoder("La pista se usa al responder una pregunta."),
            () => { if (escudos <= 0) return; escudos--; escudoActivo = true; CerrarPanel(); },
            () => { if (dobles <= 0) return; dobles--; dobleActivo = true; CerrarPanel(); },
            () => { if (congelar <= 0 || juego == null) return; congelar--; juego.CongelarTemporizador(); CerrarPanel(); }
        };
        for (int i = 0; i < nombres.Length; i++)
        {
            bool activo = i == 1 ? escudoActivo : i == 2 && dobleActivo;
            string rotulo = nombres[i] + (activo ? "  ·  ACTIVO" : "  (" + cantidades[i] + ")");
            BotonIlustrado(panel, "Poder_" + i, imagenes[i], rotulo,
                new Vector2(0, 260 - i * 205), new Vector2(790, 158),
                acciones[i], cantidades[i] > 0 && !activo);
        }
        BotonCerrarEspecial(panel, "CONTINUAR");
    }

    private void MostrarAvisoPoder(string mensaje)
    {
        if (panelEspecial == null) return;
        // El aviso sustituye el subtítulo para no superponerse al botón Continuar.
        Transform subtitulo = panelEspecial.transform.Find("Subtitulo");
        if (subtitulo != null)
        {
            TextMeshProUGUI etiqueta = subtitulo.GetComponent<TextMeshProUGUI>();
            if (etiqueta != null) etiqueta.text = mensaje;
        }
    }

    /// <summary>
    /// Crea una región desplazable con filas totalmente visibles desde el primer
    /// elemento. Usa anclaje superior para evitar recortes de la primera posición.
    /// </summary>
    private RectTransform CrearListaDesplazable(Transform padre, int filas, bool esRegistro = false)
    {
        const float altoViewport = 754;
        const float anchoViewport = 792;
        float paso = esRegistro ? 205f : 166f;
        RectTransform contenedor = Rect(padre, "ListaDesplazable",
            new Vector2(0, -40), new Vector2(anchoViewport, altoViewport));
        ScrollRect scroll = contenedor.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 26f;
        RectTransform recorte = Rect(contenedor, "Recorte", Vector2.zero,
            new Vector2(anchoViewport, altoViewport));
        Image fondoRecorte = Fondo(recorte, new Color(1f, 1f, 1f, .01f));
        fondoRecorte.raycastTarget = true;
        Mask mascara = recorte.gameObject.AddComponent<Mask>();
        mascara.showMaskGraphic = false;
        float altoContenido = Mathf.Max(altoViewport, 18f + filas * paso);
        RectTransform contenido = Rect(recorte, "Contenido", Vector2.zero,
            new Vector2(778, altoContenido));
        contenido.anchorMin = contenido.anchorMax = new Vector2(.5f, 1f);
        contenido.pivot = new Vector2(.5f, 1f);
        contenido.anchoredPosition = Vector2.zero;
        scroll.viewport = recorte;
        scroll.content = contenido;
        scroll.verticalNormalizedPosition = 1f;
        return contenido;
    }

    /// <summary>
    /// Distribuye los datos de una puntuación o partida sin solapamiento:
    /// en ranking: puesto, nombre y puntos; en registro: nombre/fecha a la
    /// izquierda y puntos/tiempo/estado a la derecha.
    /// </summary>
    private void CrearFilaListado(RectTransform contenido, int indice, string recurso,
        string ordinal, string nombre, string detalleFila)
    {
        bool esRegistro = recurso == "FilaRegistro";
        int alto = esRegistro ? 194 : 154;
        int paso = esRegistro ? 205 : 166;
        RectTransform fila = Rect(contenido, "Fila_" + indice,
            new Vector2(0, -(18f + alto / 2f + indice * paso)),
            new Vector2(778, alto));
        fila.anchorMin = fila.anchorMax = new Vector2(.5f, 1f);
        fila.pivot = new Vector2(.5f, .5f);
        Image imagen = Fondo(fila, Color.white, RecursoVentana(recurso));
        if (imagen.sprite == null) imagen.color = suave;
        imagen.raycastTarget = false;

        TextMeshProUGUI numero = Texto(fila, "Posicion", ordinal,
            new Vector2(-312, 0), new Vector2(96, 92), 46,
            indice == 0 && !esRegistro ? new Color32(137, 80, 14, 255) : uva);
        numero.fontStyle = FontStyles.Bold;

        if (esRegistro)
        {
            string[] identidad = nombre.Split(new[] { '\n' }, 2);
            string[] valores = detalleFila.Split(new[] { '\n' }, 3);
            TextMeshProUGUI jugador = Texto(fila, "Jugador", identidad[0],
                new Vector2(-122, 31), new Vector2(329, 58), 38, uva);
            jugador.alignment = TextAlignmentOptions.Left;
            jugador.fontStyle = FontStyles.Bold;
            TextMeshProUGUI fecha = Texto(fila, "Fecha", identidad.Length > 1 ? identidad[1] : "",
                new Vector2(-120, -30), new Vector2(333, 46), 25, new Color32(104, 89, 128, 255));
            fecha.alignment = TextAlignmentOptions.Left;
            TextMeshProUGUI puntos = Texto(fila, "Puntos", valores[0],
                new Vector2(242, 43), new Vector2(215, 48), 32, uva);
            puntos.fontStyle = FontStyles.Bold;
            Texto(fila, "Tiempo", valores.Length > 1 ? valores[1] : "",
                new Vector2(242, -11), new Vector2(210, 43), 28, uva);
            TextMeshProUGUI estado = Texto(fila, "Estado", valores.Length > 2 ? valores[2] : "",
                new Vector2(242, -60), new Vector2(210, 40), 22,
                new Color32(15, 119, 99, 255));
            estado.fontStyle = FontStyles.Bold;
        }
        else
        {
            TextMeshProUGUI jugador = Texto(fila, "Jugador", nombre,
                new Vector2(-105, 0), new Vector2(342, 88), 39, uva);
            jugador.alignment = TextAlignmentOptions.Left;
            jugador.fontStyle = FontStyles.Bold;
            TextMeshProUGUI puntos = Texto(fila, "Puntuacion", detalleFila,
                new Vector2(250, 0), new Vector2(224, 84), 37, uva);
            puntos.fontStyle = FontStyles.Bold;
        }
    }

    public void MostrarClasificacion()
    {
        var registros = GolosaRegistroLocal.ObtenerClasificacion(10);
        var panel = AbrirPanelEspecial("PanelClasificacion", "CLASIFICACIÓN",
            "Ranking de jugadores · mejores puntuaciones");
        if (registros.Count == 0)
            Texto(panel, "SinResultados", "Aún no hay récords guardados.",
                new Vector2(0, 40), new Vector2(750, 200), 39, uva);
        else
        {
            RectTransform contenido = CrearListaDesplazable(panel, registros.Count);
            for (int i = 0; i < registros.Count; i++)
            {
                string fondoFila = i == 0 ? "FilaOro" : i == 1 ? "FilaPlata" :
                    i == 2 ? "FilaBronce" : "FilaNormal";
                CrearFilaListado(contenido, i, fondoFila, (i + 1).ToString(),
                    registros[i].nombre, registros[i].puntos + " pts");
            }
        }
        BotonCerrarEspecial(panel);
    }

    public void MostrarRegistro()
    {
        var registros = GolosaRegistroLocal.ObtenerHistorial().Take(10).ToList();
        var panel = AbrirPanelEspecial("PanelRegistro", "REGISTRO",
            "Tus últimas partidas guardadas");
        if (registros.Count == 0)
            Texto(panel, "SinPartidas", "Aún no hay partidas guardadas.",
                new Vector2(0, 40), new Vector2(750, 200), 39, uva);
        else
        {
            RectTransform contenido = CrearListaDesplazable(panel, registros.Count, true);
            for (int i = 0; i < registros.Count; i++)
            {
                var partida = registros[i];
                int segundos = Mathf.RoundToInt(partida.tiempoSegundos);
                string resumen = partida.puntos + " pts\n" + (segundos / 60).ToString("00") +
                    ":" + (segundos % 60).ToString("00");
                // Conservamos el resultado y la fecha del historial original,
                // además de los puntos y el tiempo; no se pierde información.
                CrearFilaListado(contenido, i, "FilaRegistro", (i + 1).ToString(),
                    partida.nombre + "\n" + partida.fecha,
                    resumen + "\n" + partida.resultado);
            }
        }
        BotonCerrarEspecial(panel);
    }
    public void MostrarEvento()
    {
        recompensaElegida=false;
        LimpiarVentana("★ EVENTO SORPRESA ★","Elige una sola recompensa");
        Opcion(0,"+100 PUNTOS",()=>ElegirRecompensa(0));
        Opcion(1,"+15 SEGUNDOS",()=>ElegirRecompensa(1));
        Opcion(2,"+1 ESCUDO",()=>ElegirRecompensa(2));
    }
    private void ElegirRecompensa(int i)
    {
        if(recompensaElegida)return; recompensaElegida=true;
        if(i==0)juego.SumarPremio(100);
        else if(i==1)juego.SumarTiempoPremio(15f);
        else escudos++;
        ventana.SetActive(false); juego.ContinuarDespuesEvento();
    }
    /// <summary>
    /// Presenta el resultado individual en tarjetas ilustradas con valores reales.
    /// Mantiene las acciones de guardar récord, reiniciar y regresar al menú.
    /// No modifica el HUD, las preguntas, la pausa AR ni los datos de la partida.
    /// </summary>
    public void MostrarResultado(string nombre, int puntos, float segundos, int correctas, int casillas)
    {
        LimpiarVentana("RESULTADO FINAL", "");
        RectTransform modal = ventana.GetComponent<RectTransform>();
        modal.sizeDelta = new Vector2(940, 1300);
        modal.anchoredPosition = new Vector2(0, -35);
        fondoModal.color = Color.clear;
        titulo.gameObject.SetActive(false);
        detalle.gameObject.SetActive(false);
        auxiliar.gameObject.SetActive(false);

        RectTransform panel = Rect(ventana.transform, "ResultadoIndividualVisual",
            Vector2.zero, new Vector2(940, 1300));
        panelEspecial = panel.gameObject;
        Image ilustracion = Fondo(panel, Color.white, RecursoResultado("PanelResultado"));
        if (ilustracion.sprite == null) ilustracion.color = suave;
        ilustracion.raycastTarget = false;

        // Coordenadas vinculadas al recurso PanelResultado.png (940 x 1300).
        TextMeshProUGUI encabezado = Texto(panel, "TituloResultado", "RESULTADO FINAL",
            new Vector2(0, 523), new Vector2(690, 90), 50, Color.white);
        encabezado.fontStyle = FontStyles.Bold;
        Texto(panel, "JugadorResultado", nombre,
            new Vector2(0, 230), new Vector2(650, 80), 43, uva).fontStyle = FontStyles.Bold;

        // Estadísticas repartidas en cuatro tarjetas, sin texto superpuesto.
        DatoResultado(panel, "Puntos", "PUNTOS", puntos.ToString(), new Vector2(-198, 45));
        DatoResultado(panel, "Tiempo", "TIEMPO", TextoTiempo(segundos), new Vector2(197, 45));
        DatoResultado(panel, "Correctas", "CORRECTAS", correctas.ToString(), new Vector2(-198, -136));
        DatoResultado(panel, "Casillas", "CASILLAS", casillas + "/10", new Vector2(197, -136));

        // Se conserva el guardado original; se informa y se impiden dobles pulsaciones.
        Button guardar = BotonResultado(panel, "Guardar", "BotonGuardar", "GUARDAR RÉCORD LOCAL",
            new Vector2(0, -316), () => {} , uva);
        TextMeshProUGUI estado = Texto(panel, "EstadoRecord", "",
            new Vector2(0, 153), new Vector2(710, 40), 22, uva);
        estado.fontStyle = FontStyles.Bold;
        var mejores = GolosaRegistroLocal.ObtenerClasificacion(1);
        estado.text = mejores.Count == 0 ? "Aún no hay récords guardados" :
            "Mejor récord local: " + mejores[0].puntos + " pts";
        guardar.onClick.RemoveAllListeners();
        guardar.onClick.AddListener(() =>
        {
            juego.GuardarRecord();
            estado.text = "¡Partida guardada correctamente!";
            guardar.interactable = false;
        });

        BotonResultado(panel, "Repetir", "BotonRepetir", "JUGAR OTRA VEZ",
            new Vector2(0, -425), () =>
            {
                ventana.SetActive(false);
                juego.ReiniciarDesdeResultado();
            }, uva);
        BotonResultado(panel, "MenuPrincipal", "BotonMenu", "MENÚ PRINCIPAL",
            new Vector2(0, -533), () => juego.IrMenuPrincipal(), Color.white);
    }

    /// <summary>Crea una estadística en dos líneas dentro de una tarjeta del PNG.</summary>
    private void DatoResultado(Transform padre, string id, string rotulo, string valor, Vector2 centro)
    {
        TextMeshProUGUI encabezado = Texto(padre, id + "Etiqueta", rotulo,
            centro + new Vector2(0, 35), new Vector2(310, 48), 26, uva);
        encabezado.fontStyle = FontStyles.Bold;
        TextMeshProUGUI cifra = Texto(padre, id + "Valor", valor,
            centro + new Vector2(0, -17), new Vector2(320, 66), 42, uva);
        cifra.fontStyle = FontStyles.Bold;
        cifra.textWrappingMode = TextWrappingModes.NoWrap;
    }

    /// <summary>Genera un botón ilustrado con el texto como elemento interactivo de Unity.</summary>
    private Button BotonResultado(Transform padre, string nombre, string recurso,
        string etiqueta, Vector2 centro, Action accion, Color colorTexto)
    {
        RectTransform rect = Rect(padre, nombre, centro, new Vector2(720, 104));
        Image imagen = Fondo(rect, Color.white, RecursoResultado(recurso));
        if (imagen.sprite == null) imagen.color = amarillo;
        Button boton = rect.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        boton.onClick.AddListener(() => accion());
        TextMeshProUGUI texto = Texto(rect, "Etiqueta", etiqueta,
            Vector2.zero, new Vector2(650, 80), 32, colorTexto);
        texto.fontStyle = FontStyles.Bold;
        texto.fontSizeMin = 23;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        return boton;
    }

    /// <summary>Admite la importación de las ilustraciones como Sprite o Texture2D.</summary>
    private Sprite RecursoResultado(string nombre)
    {
        const string directorio = "GolosaResultado/";
        Sprite sprite = Resources.Load<Sprite>(directorio + nombre);
        if (sprite != null) return sprite;
        Texture2D textura = Resources.Load<Texture2D>(directorio + nombre);
        if (textura == null)
        {
            Debug.LogWarning("Golosa AR: recurso visual no encontrado: " + directorio + nombre);
            return null;
        }
        return Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height),
            new Vector2(.5f, .5f), 100f);
    }

    /// <summary>
    /// Presenta el cierre de turno con el diseño ilustrado y datos dinámicos.
    /// Respeta el mismo callback de la pantalla original: avanzar al siguiente
    /// jugador no elimina los resultados ni reinicia la escena AR.
    /// </summary>
    public void MostrarSiguienteJugador(string anterior, float segundos, int puntos,
        string siguiente, Action empezarSiguiente)
    {
        LimpiarVentana("TURNO COMPLETADO", "");
        RectTransform modal = ventana.GetComponent<RectTransform>();
        modal.sizeDelta = new Vector2(940, 1240);
        modal.anchoredPosition = new Vector2(0, -30);
        fondoModal.color = Color.clear;
        titulo.gameObject.SetActive(false);
        detalle.gameObject.SetActive(false);
        auxiliar.gameObject.SetActive(false);

        RectTransform panel = Rect(ventana.transform, "TurnoCompletadoVisual",
            Vector2.zero, new Vector2(940, 1240));
        panelEspecial = panel.gameObject;
        Image marco = Fondo(panel, Color.white, RecursoTurno("PanelTurno"));
        if (marco.sprite == null) marco.color = suave;
        marco.raycastTarget = false;

        // Los datos nunca se dibujan en el PNG: se actualizan desde cada turno.
        TextMeshProUGUI nombre = Texto(panel, "NombreFinalizo", anterior,
            new Vector2(0, 17), new Vector2(660, 70), 44, uva);
        nombre.fontStyle = FontStyles.Bold;
        nombre.fontSizeMin = 24;
        nombre.textWrappingMode = TextWrappingModes.NoWrap;

        TextMeshProUGUI puntosTexto = Texto(panel, "PuntosFinalizados", puntos + " pts",
            new Vector2(-108, -85), new Vector2(220, 65), 35, uva);
        puntosTexto.fontStyle = FontStyles.Bold;
        puntosTexto.textWrappingMode = TextWrappingModes.NoWrap;

        TextMeshProUGUI tiempoTexto = Texto(panel, "TiempoFinalizado", TextoTiempo(segundos),
            new Vector2(238, -85), new Vector2(210, 65), 36, uva);
        tiempoTexto.fontStyle = FontStyles.Bold;
        tiempoTexto.textWrappingMode = TextWrappingModes.NoWrap;

        TextMeshProUGUI siguienteTexto = Texto(panel, "JugadorSiguiente", siguiente,
            new Vector2(0, -296), new Vector2(650, 70), 42, uva);
        siguienteTexto.fontStyle = FontStyles.Bold;
        siguienteTexto.fontSizeMin = 23;
        siguienteTexto.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform botonRect = Rect(panel, "AccionSiguienteJugador",
            new Vector2(0, -460), new Vector2(745, 122));
        Image fondoBoton = Fondo(botonRect, Color.white, RecursoTurno("BotonSiguiente"));
        if (fondoBoton.sprite == null) fondoBoton.color = amarillo;
        Button boton = botonRect.gameObject.AddComponent<Button>();
        boton.targetGraphic = fondoBoton;
        boton.onClick.AddListener(() =>
        {
            ventana.SetActive(false);
            if (empezarSiguiente != null) empezarSiguiente();
        });
        TextMeshProUGUI textoBoton = Texto(botonRect, "Texto", "SIGUIENTE JUGADOR",
            Vector2.zero, new Vector2(650, 80), 35, uva);
        textoBoton.fontStyle = FontStyles.Bold;
    }

    /// <summary>
    /// Obtiene los recursos de la ventana de fin de turno. Admite tanto Sprite
    /// como Texture2D para funcionar sin ajustes manuales del importador.
    /// </summary>
    private Sprite RecursoTurno(string nombre)
    {
        const string ruta = "GolosaTurno/";
        Sprite recurso = Resources.Load<Sprite>(ruta + nombre);
        if (recurso != null) return recurso;
        Texture2D textura = Resources.Load<Texture2D>(ruta + nombre);
        if (textura == null)
        {
            Debug.LogWarning("Golosa AR: falta recurso visual " + ruta + nombre);
            return null;
        }
        return Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height),
            new Vector2(.5f, .5f), 100f);
    }

    /// <summary>
    /// Abre una clasificación final ilustrada, con nombres y puntuaciones reales.
    /// El puesto se determina por menor tiempo; los puntos desempatan, igual que
    /// en las reglas originales del modo por turnos. No se alteran sus resultados.
    /// Los gráficos son decorativos: nunca tienen nombres o cifras incrustados.
    /// </summary>
    /// <summary>
    /// Presenta el resultado final en tarjetas claras y perfectamente alineadas.
    /// Mantiene la regla: menor tiempo gana y los puntos desempatan.
    /// Los textos se generan desde los participantes reales, no desde las imágenes.
    /// </summary>
    public void MostrarClasificacionFinalTurnos(List<GolosaTurnosDatos.Jugador> jugadores, Action salir)
    {
        var ranking = jugadores.Where(j => j.finalizo)
            .OrderBy(j => j.tiempoSegundos).ThenByDescending(j => j.puntos).ToList();

        LimpiarVentana("CLASIFICACIÓN FINAL", "");
        RectTransform modal = ventana.GetComponent<RectTransform>();
        modal.sizeDelta = new Vector2(940, 1280);
        modal.anchoredPosition = new Vector2(0, -50);
        fondoModal.color = Color.clear;
        titulo.gameObject.SetActive(false);
        detalle.gameObject.SetActive(false);
        auxiliar.gameObject.SetActive(false);

        RectTransform panel = Rect(ventana.transform, "ClasificacionFinalVisual",
            Vector2.zero, new Vector2(940, 1280));
        panelEspecial = panel.gameObject;
        Image fondo = Fondo(panel, Color.white, RecursoFinal("PanelClasificacionFinal"));
        if (fondo.sprite == null) fondo.color = suave;
        fondo.raycastTarget = false;

        string nombreGanador = ranking.Count > 0 ? "GANADOR: " + ranking[0].nombre : "SIN GANADOR";
        TextMeshProUGUI ganador = Texto(panel, "Ganador", nombreGanador,
            new Vector2(0, 205), new Vector2(590, 75), 42, uva);
        ganador.fontStyle = FontStyles.Bold;
        ganador.fontSizeMin = 26;
        ganador.textWrappingMode = TextWrappingModes.NoWrap;

        // El recorte mantiene las filas dentro de la tarjeta incluso con grupos grandes.
        // Corresponde al rectángulo y los encabezados pintados en el PNG.
        RectTransform recorte = Rect(panel, "RecorteRankingFinal",
            new Vector2(0, -120), new Vector2(760, 390));
        Image superficieRecorte = Fondo(recorte, new Color(1f, 1f, 1f, .01f));
        superficieRecorte.raycastTarget = true;
        Mask mascara = recorte.gameObject.AddComponent<Mask>();
        mascara.showMaskGraphic = false;
        ScrollRect scroll = recorte.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 45f;

        const float altoFila = 124f;
        const float salto = 139f;
        float altoContenido = Mathf.Max(390f, 15f + ranking.Count * salto);
        RectTransform contenido = Rect(recorte, "ParticipantesFinal",
            Vector2.zero, new Vector2(760, altoContenido));
        contenido.anchorMin = contenido.anchorMax = new Vector2(.5f, 1f);
        contenido.pivot = new Vector2(.5f, 1f);
        contenido.anchoredPosition = Vector2.zero;
        scroll.viewport = recorte;
        scroll.content = contenido;

        for (int i = 0; i < ranking.Count; i++)
        {
            string recurso = i == 0 ? "FilaGanador" : i == 1 ? "FilaSegundo" :
                i == 2 ? "FilaTercero" : "FilaParticipante";
            RectTransform fila = Rect(contenido, "Puesto_" + (i + 1),
                new Vector2(0, -(altoFila * .5f + 10 + i * salto)),
                new Vector2(760, altoFila));
            fila.anchorMin = fila.anchorMax = new Vector2(.5f, 1f);
            fila.pivot = new Vector2(.5f, .5f);
            Image fondoFila = Fondo(fila, Color.white, RecursoFinal(recurso));
            if (fondoFila.sprite == null) fondoFila.color = suave;
            fondoFila.raycastTarget = false;

            // Los centros coinciden con las cuatro zonas de Fila*.png.
            TextMeshProUGUI puesto = Texto(fila, "Puesto", (i + 1).ToString(),
                new Vector2(-318, 0), new Vector2(75, 76), 38, uva);
            puesto.fontStyle = FontStyles.Bold;
            TextMeshProUGUI jugador = Texto(fila, "Jugador", ranking[i].nombre,
                new Vector2(-95, 0), new Vector2(280, 75), 34, uva);
            jugador.fontStyle = FontStyles.Bold;
            jugador.alignment = TextAlignmentOptions.MidlineLeft;
            jugador.margin = new Vector4(6, 0, 0, 0);
            jugador.fontSizeMin = 21;
            jugador.textWrappingMode = TextWrappingModes.NoWrap;
            TextMeshProUGUI tiempo = Texto(fila, "Tiempo",
                TextoTiempo(ranking[i].tiempoSegundos),
                new Vector2(155, 0), new Vector2(140, 75), 32, uva);
            tiempo.fontStyle = FontStyles.Bold;
            tiempo.textWrappingMode = TextWrappingModes.NoWrap;
            TextMeshProUGUI puntos = Texto(fila, "Puntos",
                ranking[i].puntos + " pts",
                new Vector2(303, 0), new Vector2(148, 75), 28, uva);
            puntos.fontStyle = FontStyles.Bold;
            puntos.fontSizeMin = 19;
            puntos.textWrappingMode = TextWrappingModes.NoWrap;
        }
        if (ranking.Count == 0)
            Texto(recorte, "SinResultados", "Aún no hay jugadores que hayan terminado.",
                Vector2.zero, new Vector2(650, 100), 28, uva);
        scroll.verticalNormalizedPosition = 1f;

        RectTransform botonRect = Rect(panel, "BotonVolverModos",
            new Vector2(0, -502), new Vector2(735, 130));
        Image botonImagen = Fondo(botonRect, Color.white, RecursoFinal("BotonVolverModos"));
        if (botonImagen.sprite == null) botonImagen.color = amarillo;
        Button boton = botonRect.gameObject.AddComponent<Button>();
        boton.targetGraphic = botonImagen;
        boton.onClick.AddListener(() =>
        {
            ventana.SetActive(false);
            if (salir != null) salir();
        });
        TextMeshProUGUI textoBoton = Texto(botonRect, "TextoBoton",
            "VOLVER A MODOS", new Vector2(20, 0), new Vector2(570, 88), 40, uva);
        textoBoton.fontStyle = FontStyles.Bold;
    }

    /// <summary>
    /// Carga los recursos PNG de la clasificación final desde Resources; si
    /// Unity los importa como Texture2D se crea un Sprite en ejecución.
    /// No altera los demás recursos gráficos del proyecto.
    /// </summary>
    private Sprite RecursoFinal(string nombre)
    {
        const string ruta = "GolosaFinal/";
        Sprite sprite = Resources.Load<Sprite>(ruta + nombre);
        if (sprite != null) return sprite;
        Texture2D textura = Resources.Load<Texture2D>(ruta + nombre);
        if (textura == null)
        {
            Debug.LogWarning("Golosa AR: falta recurso visual " + ruta + nombre);
            return null;
        }
        return Sprite.Create(textura,
            new Rect(0, 0, textura.width, textura.height), new Vector2(.5f, .5f), 100f);
    }

    private static string TextoTiempo(float segundos)
    {
        int n = Mathf.FloorToInt(segundos);
        return (n / 60).ToString("00") + ":" + (n % 60).ToString("00");
    }

    public void PrepararPreguntasJugadorActual()
    {
        CerrarPanel();
        if (rutinaCerrarResultado != null)
        {
            StopCoroutine(rutinaCerrarResultado);
            rutinaCerrarResultado = null;
        }
        resultadoPendiente = false;
        onResponder = null;
        seleccion = -1; respondida = false; pistaUsada = false;
        escudos = 1; pistas = 1; dobles = 1; congelar = 1;
        escudoActivo = false; dobleActivo = false;
        categoria = PlayerPrefs.GetString("Golosa.Categoria", "Niños") == "Avanzados" ? "Avanzados" : "Niños";
        banco.Clear(); bolsa.Clear();
        CargarPreguntas();
    }

    public void CerrarPanel() {if(ventana!=null) ventana.SetActive(false);}
    public void PistaActivadaEnPregunta()
    {
        if(ventana==null||!ventana.activeSelf||respondida||pistaUsada)return;
        if(pistas<=0)return;pistas--;pistaUsada=true;
        if(botonPista!=null)botonPista.gameObject.SetActive(false);
        for(int i=0;i<4;i++)if(i!=correcta&&i!=seleccion){opciones[i].interactable=false;fondos[i].color=Color.gray;auxiliar.text="PISTA: se descartó una opción incorrecta";break;}
    }
    private void OnDestroy()
    {
        if (spriteMenuTemporal != null) Destroy(spriteMenuTemporal);
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
