using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Solo reemplaza el contenido del GolosaJuegoUI.cs existente.
// Las imagenes se asignan desde Assets/Art/JuegoUI mediante el generador del Editor.
public class GolosaJuegoUI : MonoBehaviour
{
    private static readonly Color Uva = C("#401678");
    private static readonly Color Blanco = C("#FFFFFF");
    private static readonly Color Morado = C("#6D24C8");
    private Canvas canvas;
    private RectTransform area;
    private GameObject preparacion, controlesJuego, bienvenida;
    private bool bienvenidaActiva;
    private readonly List<Animator> animadoresSaludo = new List<Animator>();
    private Quaternion rotacionJuego = Quaternion.identity;
    private Coroutine rutinaConfeti;
    private TextMeshProUGUI puntajeTexto, casillaTexto, mensajeTexto, estadoRonda, tiempoTexto, marcadorJugador;
    private float segundosPartida;
    private bool relojActivo;
    // Pausa exclusiva del seguimiento AR/QR: no altera el reloj por recompensas
    // ni reinicia los segundos acumulados. La gestiona PrepararRAUI.
    private bool pausaPorSeguimiento;

    /// <summary>
    /// Suspende o reanuda solo el cronómetro de la partida cuando deja de ser
    /// posible localizar la golosa. Mantiene puntos, casilla y tiempo acumulado.
    /// Se puede llamar antes de que el jugador pulse «Comenzar».
    /// </summary>
    public void EstablecerPausaPorSeguimiento(bool pausar)
    {
        if (pausaPorSeguimiento == pausar) return;
        pausaPorSeguimiento = pausar;
        // Actualizar interactuabilidad al cambiar el estado, no solo tras
        // lanzar/saltar: con pérdida de seguimiento, ambos quedan bloqueados.
        ActualizarBotones();
        Debug.Log(pausar
            ? "Golosa AR: cronómetro pausado por pérdida de seguimiento."
            : "Golosa AR: cronómetro reanudado al recuperar seguimiento.");
    }
    private bool partidaFinalizada;
    private Button lanzarBoton, saltarBoton;
    private Transform piedra;
    private GolosaTablero3D tablero;
    private Transform avatarJuego;
    private Vector3 desfaseAvatar;

    [Header("Pantalla 07 - posicion y tamano del avatar")]
    [Tooltip("Altura visual del avatar en unidades locales del tablero (casilla de 0.325). No modifica el prefab.")]
    [Range(0.14f, 0.31f)] public float alturaAvatarTablero = 0.24f;
    private const float AlturaSuperficieInicio = 0.065f;
    private static readonly Vector3 InicioTablero = new Vector3(0f, AlturaSuperficieInicio, -1.29f);

    // Materiales sustituidos SOLO en la instancia del avatar, nunca en el prefab.
    private readonly List<Material> materialesAvatarTemporales = new List<Material>();

    private Material materialPiedra;
    private bool comenzado, puedeAjustar, gestoActivo, lanzando, piedraColocada, saltando;
    private float distanciaAnterior, escalaBase = 1f, escalaActual = 1f;
    private int numeroPiedra, casillaActual, puntos;
    private GolosaPantallas071011 pantallas;
    private bool resultadoTurnoRegistrado;
    private string tituloJugadorBase;
    private int desafiosResueltos, respuestasCorrectas;
    private bool esperandoResultado, esperandoEvento, guardado;
    private bool continuarPendiente;
    private bool piedraLanzadaEnPartida;
    private float congeladoHasta;
    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private readonly List<Sprite> spritesCreados = new List<Sprite>();

    // El controlador AR entrega los Sprites serializados en la escena.
    // No usa Resources.Load ni requiere mover ninguna imagen.
    private static readonly string[] NombresSprites =
    {
        "BarraMarcador",
        "PanelInferior",
        "TarjetaMarcador",
        "BotonComenzar",
        "BotonLanzar",
        "BotonSaltar",
        "BotonReiniciar",
        "Icono_play",
        "Icono_dice",
        "Icono_foot",
        "Icono_retry",
        "Icono_back",
    };

    public void ConfigurarSprites(Sprite[] referencias)
    {
        sprites.Clear();
        if (referencias == null) return;
        for (int i = 0; i < NombresSprites.Length && i < referencias.Length; i++)
            if (referencias[i] != null)
                sprites[NombresSprites[i]] = referencias[i];
    }

    private void Start()
    {
        tablero = GetComponent<GolosaTablero3D>();
        if (GolosaTurnosDatos.Activa) GolosaTurnosDatos.AplicarJugadorActual();
        CrearHUD();
        pantallas = gameObject.AddComponent<GolosaPantallas071011>();
        pantallas.Vincular(this);
        StartCoroutine(EsperarTablero());
    }

    private IEnumerator EsperarTablero()
    {
        yield return new WaitForSeconds(0.85f);
        escalaBase = Mathf.Max(0.001f, transform.localScale.x);
        puedeAjustar = true;
    }

    // La interfaz utiliza una sola barra superior y un panel inferior compacto.
    // No modifica la sesión AR, el tablero ni los gestos de dos dedos.
    private void CrearHUD()
    {
        GameObject raiz = new GameObject("GOLOSA_HUD_IMAGENES", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;
        CanvasScaler scaler = raiz.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2340);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        area = new GameObject("AreaSegura", typeof(RectTransform)).GetComponent<RectTransform>();
        area.SetParent(raiz.transform, false);
        Rect s = Screen.safeArea;
        area.anchorMin = new Vector2(s.xMin / Screen.width, s.yMin / Screen.height);
        area.anchorMax = new Vector2(s.xMax / Screen.width, s.yMax / Screen.height);
        area.offsetMin = area.offsetMax = Vector2.zero;

        // Logo, atrás y marcador en una sola franja superior.
        ImagenHud(area, "LogoGolosa", "LogoCompacto", new Vector2(0, -85),
            new Vector2(570, 147), new Vector2(.5f, 1f));
        BotonHud(area, "VolverAModos", "BotonAtras", new Vector2(-455, -94),
            new Vector2(105, 105), new Vector2(.5f, 1f), Volver);
        RectTransform barra = ImagenHud(area, "BarraHUD", "BarraHUD", new Vector2(0, -266),
            new Vector2(1018, 175), new Vector2(.5f, 1f));
        string nombre = PlayerPrefs.GetString("Golosa.Nombre", "Jugador");
        if (nombre.Length > 11) nombre = nombre.Substring(0, 10) + "…";
        tituloJugadorBase = nombre;
        marcadorJugador = Texto(barra, "Jugador", "JUGADOR\n" + nombre,
            new Vector2(-369, 1), new Vector2(230, 132), 33, Uva);
        puntajeTexto = Texto(barra, "Puntos", "PUNTOS\n0",
            new Vector2(-119, 1), new Vector2(220, 132), 36, Uva);
        casillaTexto = Texto(barra, "Casilla", "CASILLA\n0/10",
            new Vector2(128, 1), new Vector2(220, 132), 36, Uva);
        tiempoTexto = Texto(barra, "Temporizador", "00:00",
            new Vector2(374, 0), new Vector2(215, 120), 48, Uva);
        // Separadores: no bloquean toques de AR.
        for (int n = 0; n < 3; n++)
            Panel(barra, "Divisor" + n, new Vector2(-245 + n * 247, 0),
                new Vector2(3, 92), new Color(.67f, .37f, .83f, .7f));

        // Solo el borde inferior es imagen; el centro queda limpio.
        ImagenHud(area, "PanelInferior", "PanelAjuste", new Vector2(0, 191),
            new Vector2(1030, 360), new Vector2(.5f, 0f));
        mensajeTexto = Texto(area, "MensajeEstado", "Ajusta la golosa con dos dedos",
            new Vector2(0, 316), new Vector2(900, 78), 37, Uva);
        // Botón principal sin textos diminutos repetidos.
        preparacion = new GameObject("Preparacion", typeof(RectTransform));
        RectTransform prep = preparacion.GetComponent<RectTransform>();
        Colocar(prep, area, new Vector2(0, 175), new Vector2(980, 215), new Vector2(.5f, 0f));
        BotonImagen(prep, "Comenzar", "BotonComenzar", "¡COMENZAR A JUGAR!", "Icono_play",
            Vector2.zero, new Vector2(920, 192), 45, Uva, Comenzar, false);

        controlesJuego = new GameObject("ControlesJuego", typeof(RectTransform));
        RectTransform juego = controlesJuego.GetComponent<RectTransform>();
        Colocar(juego, area, new Vector2(0, 175), new Vector2(1000, 270), new Vector2(.5f, 0f));
        lanzarBoton = BotonImagen(juego, "Lanzar", "BotonLanzar", "LANZAR", "Icono_dice",
            new Vector2(-317, 0), new Vector2(305, 205), 35, Blanco, Lanzar, true);
        saltarBoton = BotonImagen(juego, "Saltar", "BotonSaltar", "SALTAR", "Icono_foot",
            Vector2.zero, new Vector2(305, 205), 35, Uva, Saltar, true);
        BotonImagen(juego, "Reiniciar", "BotonReiniciar", "REINICIAR", "Icono_retry",
            new Vector2(317, 0), new Vector2(305, 205), 28, Blanco, Reiniciar, true);
        estadoRonda = Texto(juego, "EstadoRonda", "Primero, lanza la piedra",
            new Vector2(0, -130), new Vector2(930, 60), 26, Uva);
        controlesJuego.SetActive(false);
        ActualizarBotones();
    }

    private Sprite SpriteHud(string nombre)
    {
        // Carga los PNG como Texture2D: funciona incluso sin cambiar Texture Type en el Inspector.
        Texture2D textura = Resources.Load<Texture2D>("GolosaARHUD/" + nombre);
        if (textura == null) { Debug.LogWarning("Golosa AR: falta recurso visual " + nombre); return null; }
        Sprite s = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height),
            new Vector2(.5f, .5f), 100f);
        spritesCreados.Add(s);
        return s;
    }

    private RectTransform ImagenHud(Transform padre, string nombre, string recurso, Vector2 posicion,
        Vector2 tam, Vector2 ancla)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        Colocar(rt, padre, posicion, tam, ancla);
        Image imagen = go.GetComponent<Image>();
        imagen.sprite = SpriteHud(recurso);
        imagen.color = Color.white;
        imagen.raycastTarget = false;
        return rt;
    }

    private Button BotonHud(Transform padre, string nombre, string recurso, Vector2 posicion,
        Vector2 tam, Vector2 ancla, UnityEngine.Events.UnityAction accion)
    {
        RectTransform rt = ImagenHud(padre, nombre, recurso, posicion, tam, ancla);
        Image imagen = rt.GetComponent<Image>();
        imagen.raycastTarget = true;
        Button boton = rt.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        boton.onClick.AddListener(accion);
        return boton;
    }

    private void Update()
    {
        // El reloj se actualiza incluso cuando el ajuste con dos dedos termina.
        if (relojActivo && !pausaPorSeguimiento && Time.unscaledTime >= congeladoHasta)
        {
            segundosPartida += Time.deltaTime;
            int total = Mathf.FloorToInt(segundosPartida);
            int minutos = total / 60;
            int segundos = total % 60;
            if (tiempoTexto != null)
                tiempoTexto.text = minutos.ToString("00") + ":" + segundos.ToString("00");
        }
        if (comenzado || !puedeAjustar) { gestoActivo = false; return; }
        Vector2 a, b;
        if (!DosDedos(out a, out b)) { gestoActivo = false; return; }
        float d = Vector2.Distance(a, b);
        if (d < 20f) { gestoActivo = false; return; }
        if (gestoActivo)
        {
            escalaActual = Mathf.Clamp(escalaActual * Mathf.Clamp(d / Mathf.Max(1f, distanciaAnterior), .85f, 1.15f), .55f, 1.50f);
            transform.localScale = Vector3.one * (escalaBase * escalaActual);
            if (mensajeTexto) mensajeTexto.text = "Tamaño de la golosa: " + Mathf.RoundToInt(escalaActual * 100f) + "%";
        }
        distanciaAnterior = d;
        gestoActivo = true;
    }

    private static bool DosDedos(out Vector2 a, out Vector2 b)
    {
        a = b = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        Touchscreen pantalla = Touchscreen.current;
        if (pantalla != null)
        {
            int n = 0;
            foreach (var t in pantalla.touches)
            {
                if (!t.press.isPressed) continue;
                if (n == 0) a = t.position.ReadValue();
                else if (n == 1) b = t.position.ReadValue();
                if (++n > 2) return false;
            }
            return n == 2;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount == 2)
        {
            a = Input.GetTouch(0).position;
            b = Input.GetTouch(1).position;
            return true;
        }
#endif
        return false;
    }

    private void Comenzar()
    {
        if (!puedeAjustar || comenzado) return;
        string id = PlayerPrefs.GetString("Golosa.AvatarId", "");
        // Se resuelve el prefab de forma determinista; en 03_Juego hay catálogos
        // diferentes y FindFirstObjectByType no garantiza el mismo en cada dispositivo.
        if (ResolverPrefabAvatar(id) == null)
        {
            mensajeTexto.text = "Configura los prefabs del avatar antes de comenzar";
            Debug.LogWarning("Golosa AR: falta el catalogo de avatares o el prefab de " + id);
            return;
        }
        comenzado = true;
        resultadoTurnoRegistrado = false;
        partidaFinalizada = false;
        segundosPartida = 0f;
        desafiosResueltos = 0; respuestasCorrectas = 0; guardado = false; esperandoResultado = false; esperandoEvento = false; piedraLanzadaEnPartida=false;
        relojActivo = false; // El contador arranca al aceptar la bienvenida.
        if (tiempoTexto != null) tiempoTexto.text = "00:00";
        gestoActivo = false;
        preparacion.SetActive(false);
        controlesJuego.SetActive(false);
        casillaActual = 0;
        if (tablero != null) tablero.MarcarCasilla(0);
        ColocarAvatarSeleccionado();
        casillaTexto.text = "CASILLA\nINICIO";
        mensajeTexto.text = avatarJuego != null ? "¡Bienvenido a la partida!" :
            "Falta asignar el prefab del avatar en la escena AR";
        if (avatarJuego != null) MostrarBienvenida();
        ActualizarBotones();
    }

    private void Lanzar()
    {
        if (pausaPorSeguimiento || !comenzado || bienvenidaActiva || partidaFinalizada || lanzando || saltando || piedraLanzadaEnPartida || piedraColocada) return;
        // Una sola piedra por partida. Evitar casilla 1 para permitir el primer avance.
        // La meta (10) se reserva para que siempre sea posible alcanzarla.
        numeroPiedra = Random.Range(2, 10);
        if (numeroPiedra == casillaActual)
            numeroPiedra = casillaActual < 9 ? casillaActual + 1 : 1;
        StartCoroutine(AnimarPiedra());
    }

    private IEnumerator AnimarPiedra()
    {
        lanzando = true;
        ActualizarBotones();
        QuitarPiedra();
        GameObject roca = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        roca.name = "Piedra_3D_Aleatoria";
        Collider col = roca.GetComponent<Collider>();
        if (col) Destroy(col);
        piedra = roca.transform;
        piedra.SetParent(transform, false);
        piedra.localScale = new Vector3(.095f, .063f, .08f);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader != null)
        {
            materialPiedra = new Material(shader);
            Color gris = C("#766C82");
            if (materialPiedra.HasProperty("_BaseColor")) materialPiedra.SetColor("_BaseColor", gris);
            if (materialPiedra.HasProperty("_Color")) materialPiedra.SetColor("_Color", gris);
            roca.GetComponent<Renderer>().sharedMaterial = materialPiedra;
        }
        // Tomar el centro REAL de la casilla construida, no una formula duplicada.
        Transform casillaDestino = transform.Find("Casilla_" + numeroPiedra);
        if (casillaDestino == null)
        {
            Debug.LogError("Golosa AR: no existe Casilla_" + numeroPiedra + ". Lanzamiento cancelado.");
            QuitarPiedra();
            lanzando = false;
            ActualizarBotones();
            yield break;
        }
        // La superficie visual se crea a 0.050 de altura respecto al centro de casilla.
        // La esfera tiene semialtura 0.0315; se deja un pequeño margen.
        Vector3 destino = casillaDestino.localPosition + new Vector3(0f, .090f, 0f);
        // Sale desde el centro del cartel INICIO, no desde el aire o una casilla.
        Vector3 inicio = InicioTablero + Vector3.up * .09f;
        float tiempo = 0f;
        while (tiempo < 1f)
        {
            while (pausaPorSeguimiento) yield return null;
            tiempo = Mathf.Min(1f, tiempo + Time.deltaTime / 1.0f);
            piedra.localPosition = Vector3.Lerp(inicio, destino, tiempo) + Vector3.up * (Mathf.Sin(tiempo * Mathf.PI) * .53f);
            piedra.Rotate(Vector3.up, 280f * Time.deltaTime);
            yield return null;
        }
        piedra.localPosition = destino;
        // Confirmar que la roca siga encima del centro de una casilla válida.
        Vector3 centroReal = casillaDestino.localPosition;
        bool impactoValido = Vector2.Distance(
            new Vector2(piedra.localPosition.x, piedra.localPosition.z),
            new Vector2(centroReal.x, centroReal.z)) < .10f;
        lanzando = false;
        piedraColocada = impactoValido;
        if (impactoValido) piedraLanzadaEnPartida = true;
        if (!impactoValido)
        {
            QuitarPiedra();
            mensajeTexto.text = "La piedra cayó fuera. Lanza de nuevo.";
            ActualizarBotones();
            yield break;
        }
        estadoRonda.text = "Piedra en casilla " + numeroPiedra + " · ¡No la pises!";
        mensajeTexto.text = "¡La piedra cayó en la casilla " + numeroPiedra + "!";
        ActualizarBotones();
    }

    private void Saltar()
    {
        if (pausaPorSeguimiento || !comenzado || bienvenidaActiva || partidaFinalizada || !piedraColocada || lanzando || saltando || esperandoResultado || esperandoEvento) return;
        if (pantallas == null) { Debug.LogError("Golosa AR: Falta la interfaz de desafíos."); return; }
        pantallas.MostrarDesafio(ResponderDesafio);
    }

    public bool PuedeUsarPoderes { get { return comenzado && !partidaFinalizada && !pausaPorSeguimiento; } }
    public void PistaActivada() { if (pantallas != null) pantallas.PistaActivadaEnPregunta(); }
    public void CongelarTemporizador() { congeladoHasta = Time.unscaledTime + 10f; }
    public void SumarPremio(int cantidad) { puntos += cantidad; ActualizarMarcadores(); }
    // +15 segundos equivale a 15 segundos extra en el contador disponible.
    // Como la partida mide tiempo transcurrido, se descuenta del tiempo registrado sin permitir negativo.
    public void SumarTiempoPremio(float cantidad) { segundosPartida = Mathf.Max(0,segundosPartida - cantidad); if(tiempoTexto!=null)tiempoTexto.text=FormatoTiempo(segundosPartida); }
    public void AlternarVisualTablero()
    {
        bool visible = true;
        Transform casilla = transform.Find("Casilla_1");
        if(casilla!=null)visible=!casilla.gameObject.activeSelf;
        for(int i=1;i<=10;i++){ Transform c=transform.Find("Casilla_"+i);if(c!=null)c.gameObject.SetActive(visible); }
        foreach(string n in new[]{"Senal_INICIO","Senal_META"}){Transform r=transform.Find(n);if(r!=null)r.gameObject.SetActive(visible);}
        if(piedra!=null) foreach(Renderer r in piedra.GetComponentsInChildren<Renderer>(true))r.enabled=visible;
        if(avatarJuego!=null) foreach(Renderer r in avatarJuego.GetComponentsInChildren<Renderer>(true))r.enabled=visible;
    }
    private void ResponderDesafio(bool correcta)
    {
        if (pausaPorSeguimiento || partidaFinalizada || esperandoResultado) return;
        esperandoResultado=true; continuarPendiente=false; desafiosResueltos++;
        bool escudoProtegio = !correcta && pantallas != null && pantallas.ConsumirEscudo();
        if (correcta)
        {
            respuestasCorrectas++;
            puntos += pantallas != null && pantallas.PuntosDobles ? 20 : 10;
            if(pantallas!=null)pantallas.ConsumirDoble();
        }
        else
        {
            if (!escudoProtegio) puntos -= 5;
        }
        ActualizarMarcadores();
        StartCoroutine(MoverTrasRespuesta(correcta, escudoProtegio));
    }
    private IEnumerator MoverTrasRespuesta(bool respuestaCorrecta, bool protegido)
    {
        saltando=true;ActualizarBotones();
        int destino = protegido ? casillaActual : respuestaCorrecta ? casillaActual + 1 : casillaActual - 1;
        destino = Mathf.Clamp(destino, casillaActual==0 ? 0 : 1, 10);
        if(destino==numeroPiedra) destino = Mathf.Clamp(destino + (respuestaCorrecta?1:-1),1,10);
        if(destino==numeroPiedra) destino = casillaActual;
        if(avatarJuego!=null && tablero!=null)
        {
            Vector3 origen=avatarJuego.localPosition;
            Vector3 fin=(destino==0?InicioTablero:tablero.PosicionLocalCasilla(destino))+desfaseAvatar;
            float d=.55f, t=0f;
            while(t<d)
            {
                while (pausaPorSeguimiento) yield return null;
                t=Mathf.Min(d,t+Time.deltaTime);float v=t/d;
                avatarJuego.localPosition=Vector3.Lerp(origen,fin,v)+Vector3.up*(Mathf.Sin(v*Mathf.PI)*.14f);
                yield return null;
            }
            avatarJuego.localPosition=fin;
        }
        casillaActual=destino;
        if(tablero!=null)tablero.MarcarCasilla(casillaActual);
        if(mensajeTexto!=null)mensajeTexto.text=respuestaCorrecta?"¡Correcto! Avanzaste.": protegido?"¡Escudo activado! Sin penalización.":"Respuesta incorrecta: retrocedes.";
        if(estadoRonda!=null)estadoRonda.text="DESAFÍO " + desafiosResueltos + " · CASILLA " + (casillaActual==0?"INICIO":casillaActual.ToString());
        ActualizarMarcadores();
        if(casillaActual>=10 && respuestaCorrecta)
        {
            yield return StartCoroutine(SaltarAMeta());
            partidaFinalizada=true; relojActivo=false;piedraColocada=false;
            QuitarPiedra(); puntos+=100; ActualizarMarcadores();
            if(mensajeTexto!=null)mensajeTexto.text="¡FELICIDADES! ¡COMPLETASTE LA GOLOSA!";
            if(estadoRonda!=null)estadoRonda.text="META · Tiempo: "+FormatoTiempo(segundosPartida);
        }
        saltando=false;ActualizarBotones();
        if (!partidaFinalizada && desafiosResueltos%3==0) esperandoEvento=true;
        // La piedra permanece en la misma casilla hasta terminar la partida.
        // Los siguientes desafios no requieren nuevos lanzamientos.
        // Si CONTINUAR se pulsó durante la animación, finalizar ahora la transición.
        // El resultado ya se cerró antes de iniciar el movimiento.
        // Tras llegar a la casilla, permitir que SALTAR abra el siguiente desafío.
        TerminarResultadoPregunta();
    }
    private IEnumerator SaltarAMeta()
    {
        if(avatarJuego==null) yield break;
        Transform meta=tablero!=null?tablero.transform.Find("Senal_META"):null;
        Vector3 posicion=meta!=null?meta.localPosition+new Vector3(0,.095f,0):new Vector3(0,.095f,1.29f);
        Vector3 origen=avatarJuego.localPosition,fin=posicion+desfaseAvatar;
        float tiempo=0f,d=.65f;
        while(tiempo<d){while(pausaPorSeguimiento)yield return null;tiempo=Mathf.Min(d,tiempo+Time.deltaTime);float t=tiempo/d;avatarJuego.localPosition=Vector3.Lerp(origen,fin,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.14f;yield return null;}
        avatarJuego.localPosition=fin; if(tablero!=null)tablero.MarcarCasilla(0);
    }
    public void TerminarResultadoPregunta()
    {
        if (saltando) return;
        esperandoResultado = false;
        continuarPendiente = false;
        ActualizarBotones();
        if (partidaFinalizada)
        {
            MostrarResultadoFinal();
            return;
        }
        // Una pregunta nueva SOLO debe abrirse cuando el jugador pulse SALTAR.
        // Tras cada 3 desafíos, se conserva el evento sorpresa ya existente.
        if (esperandoEvento && pantallas != null)
            pantallas.MostrarEvento();
        else if (mensajeTexto != null)
            mensajeTexto.text = "¡Ahora pulsa SALTAR para el siguiente desafío!";
    }

    public void ContinuarDespuesEvento()
    {
        esperandoEvento = false;
        ActualizarBotones();
        if (estadoRonda != null)
            estadoRonda.text = "¡Continúa con SALTAR para el siguiente desafío!";
        if (mensajeTexto != null)
            mensajeTexto.text = "¡Evento completado! Pulsa SALTAR.";
    }

    private void MostrarResultadoFinal()
    {
        if (GolosaTurnosDatos.Activa)
        {
            if (resultadoTurnoRegistrado) return;
            resultadoTurnoRegistrado = true;
            // Guardar ANTES de cambiar el participante para preservar su nombre y tiempo.
            string nombreTerminado = PlayerPrefs.GetString("Golosa.Nombre", "Jugador");
            float tiempoTerminado = segundosPartida;
            int puntosTerminados = puntos;
            bool haySiguiente = GolosaTurnosDatos.RegistrarMetaYAvanzar(tiempoTerminado, puntosTerminados);
            if (pantallas != null)
            {
                if (haySiguiente)
                    pantallas.MostrarSiguienteJugador(nombreTerminado, tiempoTerminado, puntosTerminados,
                        GolosaTurnosDatos.Actual.nombre, PrepararSiguienteJugador);
                else
                    pantallas.MostrarClasificacionFinalTurnos(GolosaTurnosDatos.Jugadores, SalirTurnosAlMenu);
            }
            return;
        }
        GuardarRecord();
        if (pantallas != null)
            pantallas.MostrarResultado(PlayerPrefs.GetString("Golosa.Nombre","Jugador"),puntos,segundosPartida,respuestasCorrectas,casillaActual);
    }

    private void PrepararSiguienteJugador()
    {
        // Nunca destruye el tablero ni reinicia la sesión AR.
        Reiniciar();
        if (avatarJuego != null) Destroy(avatarJuego.gameObject);
        avatarJuego = null;
        desfaseAvatar = Vector3.zero;
        rotacionJuego = Quaternion.identity;
        comenzado = false;
        relojActivo = false;
        resultadoTurnoRegistrado = false;
        preparacion.SetActive(true);
        controlesJuego.SetActive(false);
        if (pantallas != null) pantallas.PrepararPreguntasJugadorActual();
        if (marcadorJugador != null)
        {
            string n = PlayerPrefs.GetString("Golosa.Nombre", "Jugador");
            marcadorJugador.text = "JUGADOR\n" + (n.Length > 11 ? n.Substring(0, 10) + "…" : n);
        }
        if (mensajeTexto != null) mensajeTexto.text = "¡Nuevo turno! Comienza cuando estés listo.";
        ActualizarBotones();
    }

    private void SalirTurnosAlMenu()
    {
        GolosaTurnosDatos.Cancelar();
        if (Application.CanStreamedLevelBeLoaded("02_ModoJuego"))
            SceneManager.LoadScene("02_ModoJuego");
        else Debug.LogError("Falta 02_ModoJuego en Build Profiles.");
    }
    public void GuardarRecord()
    {
        if(guardado)return;guardado=true;
        GolosaRegistroLocal.GuardarPartida(PlayerPrefs.GetString("Golosa.Nombre","Jugador"),puntos,segundosPartida,partidaFinalizada?"Completada":"Interrumpida");
    }
    public void ReiniciarDesdeResultado() { Reiniciar(); }
    public void IrMenuPrincipal()
    {
        if (GolosaTurnosDatos.Activa) GolosaTurnosDatos.Cancelar();
        if(Application.CanStreamedLevelBeLoaded("00_Inicio"))SceneManager.LoadScene("00_Inicio");
        else if(Application.CanStreamedLevelBeLoaded("02_ModoJuego"))SceneManager.LoadScene("02_ModoJuego");
        else Debug.LogError("Golosa AR: agrega 00_Inicio a Build Profiles > Scene List.");
    }
    private void ActualizarMarcadores()
    {
        if(puntajeTexto!=null)puntajeTexto.text="PUNTOS\n"+puntos;
        if(casillaTexto!=null)casillaTexto.text=partidaFinalizada?"CASILLA\nMETA":casillaActual<=0?"CASILLA\nINICIO":"CASILLA\n"+casillaActual+"/10";
    }

    private void Reiniciar()
    {
        StopAllCoroutines();
        rutinaConfeti = null;
        bienvenidaActiva = false;
        if (bienvenida != null) bienvenida.SetActive(false);
        DesactivarSaludo();
        if (avatarJuego != null) avatarJuego.localRotation = rotacionJuego;
        segundosPartida = 0f;
        desafiosResueltos=0;respuestasCorrectas=0;esperandoResultado=false;esperandoEvento=false;guardado=false;continuarPendiente=false;piedraLanzadaEnPartida=false;
        if(pantallas!=null)pantallas.CerrarPanel();
        partidaFinalizada = false;
        relojActivo = comenzado;
        if (tiempoTexto != null) tiempoTexto.text = "00:00";
        lanzando = false;
        piedraColocada = false;
        saltando = false;
        casillaActual = 0;
        if (tablero != null) tablero.MarcarCasilla(casillaActual);
        if (avatarJuego != null && tablero != null)
        {
            // La escala debe permanecer válida también al reiniciar la partida.
            if (avatarJuego.localScale.sqrMagnitude < 0.000001f)
                avatarJuego.localScale = Vector3.one;
            avatarJuego.localPosition = InicioTablero + desfaseAvatar;
        }
        puntos = 0;
        numeroPiedra = 0;
        QuitarPiedra();
        puntajeTexto.text = "PUNTOS\n0";
        casillaTexto.text = "CASILLA\nINICIO";
        mensajeTexto.text = comenzado ? "¡Nueva partida! Lanza la piedra" : "Ajusta la golosa con dos dedos";
        if (estadoRonda) estadoRonda.text = "Primero, lanza la piedra";
        ActualizarBotones();
    }
    private void ActualizarBotones()
    {
        if (lanzarBoton) lanzarBoton.interactable = !pausaPorSeguimiento && comenzado && !bienvenidaActiva && !partidaFinalizada && !lanzando && !saltando && !piedraLanzadaEnPartida && !piedraColocada && !esperandoResultado && !esperandoEvento;
        if (saltarBoton) saltarBoton.interactable = !pausaPorSeguimiento && comenzado && !bienvenidaActiva && !partidaFinalizada && piedraColocada && !lanzando && !saltando && !esperandoResultado && !esperandoEvento;
    }

    private static string FormatoTiempo(float tiempo)
    {
        int total = Mathf.FloorToInt(tiempo);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    private void QuitarPiedra()
    {
        if (piedra != null) { Destroy(piedra.gameObject); piedra = null; }
        if (materialPiedra != null) { Destroy(materialPiedra); materialPiedra = null; }
    }

    /// <summary>
    /// Selecciona el prefab válido del avatar entre todos los catálogos de 03_Juego.
    /// La escena conserva un catálogo con los seis modelos originales y otro con
    /// el niño corregido. Se prioriza este último para evitar diferencias entre
    /// dispositivos causadas por el orden no determinista de búsqueda.
    /// </summary>
    private static GameObject ResolverPrefabAvatar(string id)
    {
        GolosaCatalogoAvatares[] catalogos =
            FindObjectsByType<GolosaCatalogoAvatares>(FindObjectsSortMode.None);

        GameObject candidato = null;
        foreach (GolosaCatalogoAvatares catalogo in catalogos)
        {
            if (catalogo == null || !catalogo.isActiveAndEnabled) continue;
            GameObject prefab = catalogo.Obtener(id);
            if (prefab == null) continue;

            // El prefab optimizado para la escena AR se utiliza exclusivamente
            // cuando se eligió el niño; los demás personajes conservan su catálogo.
            if (id == "AvatarNiño" &&
                (prefab.name.Contains("Visible07") || prefab.name.Contains("Nino_Visible")))
                return prefab;

            if (candidato == null) candidato = prefab;
        }

        if (candidato == null)
            Debug.LogError("Golosa AR: ningún catálogo activo contiene el avatar '" + id + "'.");
        return candidato;
    }

    private void ColocarAvatarSeleccionado()
    {
        if (avatarJuego != null) return;
        string id = PlayerPrefs.GetString("Golosa.AvatarId", "");
        GameObject prefab = ResolverPrefabAvatar(id);
        if (prefab == null)
        {
            Debug.LogWarning("Golosa AR: no hay prefab asignado para el avatar elegido: " + id);
            return;
        }
        GameObject instancia = Instantiate(prefab, transform);
        instancia.name = "AvatarJuego_" + id;
        instancia.SetActive(true);
        avatarJuego = instancia.transform;
        // Algunos prefabs exportados desde escenas incluyen una cámara propia.
        // Si permanece activa, sustituye visualmente la cámara de ARCore
        // y se observa un horizonte/skybox en lugar del suelo real.
        PrepararInstanciaVisual(instancia);
        // El oso utiliza una textura de atlas que puede perderse en la conversion
        // automatica de materiales durante el renderizado en Android. La restauramos
        // exclusivamente en la instancia de la partida, sin alterar su prefab.
        if (id == "hiOso") RestaurarTexturaOso(instancia);
        avatarJuego.localPosition = Vector3.zero;
        avatarJuego.localRotation = Quaternion.identity;
        avatarJuego.localScale = prefab.transform.localScale;

        // Conservamos los Animator originales de las instancias para saludar.
        // No se editan controladores ni prefabs. Root motion queda desactivado.
        animadoresSaludo.Clear();
        foreach (Animator animator in instancia.GetComponentsInChildren<Animator>(true))
        {
            animator.applyRootMotion = false;
            // No apagar los Animator: algunos avatares VRM requieren el esqueleto animado
            // para mantener una postura correcta. Los que no tienen controller
            // conservan su estado original.
            if (animator.runtimeAnimatorController != null)
            {
                animator.enabled = true;
                animadoresSaludo.Add(animator);
            }
        }

        // Los prefabs usados en vistas previas pueden conservar capas que la
        // cámara AR no renderiza. La instancia de la partida siempre usa Default.
        // Para modelos con SkinnedMeshRenderer, evitar el descarte por bounds
        // desactualizados durante animaciones y movimientos del dispositivo.
        foreach (Transform parte in instancia.GetComponentsInChildren<Transform>(true))
            parte.gameObject.layer = 0;
        int mallasActivas = 0;
        foreach (Renderer malla in instancia.GetComponentsInChildren<Renderer>(true))
        {
            malla.enabled = true;
            if (malla is SkinnedMeshRenderer animada)
                animada.updateWhenOffscreen = true;
            mallasActivas++;
        }
        Debug.Log("Golosa AR: instancia visible preparada: " + instancia.name +
                  "; mallas=" + mallasActivas);

        // Medir la instancia, sin editar el prefab. Altura relativa al tablero,
        // de forma que el avatar crezca junto con el gesto de pinza.
        Renderer[] renderers = instancia.GetComponentsInChildren<Renderer>(true);
        Bounds bounds;
        if (MedirAvatar(renderers, out bounds))
        {
            float escalaPadre = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
            float alturaLocalActual = bounds.size.y / escalaPadre;
            if (alturaLocalActual > 0.0001f)
            {
                // No normalizar a Vector3.one: algunos prefabs tienen escala base propia.
                float factor = Mathf.Clamp(alturaAvatarTablero / alturaLocalActual, 0.01f, 100f);
                avatarJuego.localScale = avatarJuego.localScale * factor;
            }
            // Volver a medir DESPUES de escalar; las cotas anteriores ya no valen.
            if (MedirAvatar(renderers, out bounds))
            {
                Vector3 centroLocal = transform.InverseTransformPoint(bounds.center);
                Vector3 piesLocal = transform.InverseTransformPoint(
                    new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
                desfaseAvatar = new Vector3(-centroLocal.x, -piesLocal.y, -centroLocal.z);
            }
        }
        // Nunca aceptar una escala prácticamente nula: haría que el avatar
        // avanzara de casilla sin ser visible (efecto «fantasma»).
        if (avatarJuego.localScale.sqrMagnitude < 0.000001f)
        {
            Debug.LogWarning("Golosa AR: escala inválida; se restablece escala del prefab.");
            avatarJuego.localScale = Vector3.one;
        }
        avatarJuego.localPosition = InicioTablero + desfaseAvatar;
        rotacionJuego = avatarJuego.localRotation;
        if (renderers.Length == 0) Debug.LogError("Golosa AR: avatar sin Renderer: " + id);
        else Debug.Log("Golosa AR: avatar cargado " + id + " / renderers " + renderers.Length);

    }

    private void PrepararInstanciaVisual(GameObject instancia)
    {
        // Solo hijos del avatar instanciado: NO se toca la cámara AR de la escena.
        int camarasDesactivadas = 0;
        foreach (Camera cam in instancia.GetComponentsInChildren<Camera>(true))
        {
            cam.enabled = false;
            camarasDesactivadas++;
        }
        foreach (AudioListener oyente in instancia.GetComponentsInChildren<AudioListener>(true))
            oyente.enabled = false;
        if (camarasDesactivadas > 0)
            Debug.LogWarning("Golosa AR: desactivadas " + camarasDesactivadas +
                " camaras internas del prefab de avatar (solo en la instancia). ");

        // Conserva las texturas existentes. Solo crea sustitutos de URP/Lit
        // cuando el shader original no es compatible con esta plataforma.
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null) return;
        foreach (Renderer r in instancia.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materiales = r.sharedMaterials;
            bool cambio = false;
            for (int i = 0; i < materiales.Length; i++)
            {
                Material original = materiales[i];
                if (original == null)
                {
                    Debug.LogWarning("Golosa AR: material nulo en " + r.name);
                    continue;
                }
                if (original.shader != null && original.shader.isSupported) continue;
                Texture textura = null;
                if (original.HasProperty("_BaseMap")) textura = original.GetTexture("_BaseMap");
                if (textura == null && original.HasProperty("_MainTex"))
                    textura = original.GetTexture("_MainTex");
                Color tono = Color.white;
                if (original.HasProperty("_BaseColor")) tono = original.GetColor("_BaseColor");
                else if (original.HasProperty("_Color")) tono = original.GetColor("_Color");
                Material adaptado = new Material(urp);
                adaptado.name = original.name + "_GolosaURP_Runtime";
                if (adaptado.HasProperty("_BaseColor")) adaptado.SetColor("_BaseColor", tono);
                if (textura != null && adaptado.HasProperty("_BaseMap"))
                    adaptado.SetTexture("_BaseMap", textura);
                materialesAvatarTemporales.Add(adaptado);
                materiales[i] = adaptado;
                cambio = true;
                Debug.LogWarning("Golosa AR: shader incompatible en material " + original.name +
                    "; se aplicó URP/Lit a la instancia de " + instancia.name);
            }
            if (cambio) r.sharedMaterials = materiales;
        }
    }

    /// <summary>
    /// Recupera la textura de atlas original del oso durante la partida AR.
    /// El cambio se limita a los Renderer de la instancia y no afecta a
    /// Crear Perfil, Por Turnos ni a los otros cinco personajes del catalogo.
    /// </summary>
    /// <remarks>
    /// Utiliza una copia del atlas original incluida como recurso de Unity
    /// para garantizar su disponibilidad en la compilacion Android. Se crea
    /// un material URP/Lit compartido por las mallas del oso; el material se
    /// libera posteriormente junto a los demas materiales temporales.
    /// </remarks>
    private void RestaurarTexturaOso(GameObject instancia)
    {
        const string rutaAtlas = "GolosaAvatarFix/OsoAlbedo";
        Texture2D atlas = Resources.Load<Texture2D>(rutaAtlas);
        if (atlas == null)
        {
            Debug.LogWarning("Golosa AR: no se encontro la textura del oso en Resources/" + rutaAtlas);
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning("Golosa AR: shader URP/Lit no disponible para restaurar la textura del oso.");
            return;
        }

        Material materialOso = new Material(shader);
        materialOso.name = "GolosaAR_Oso_Texturizado_Instancia";
        materialOso.SetTexture("_BaseMap", atlas);
        materialOso.SetColor("_BaseColor", Color.white);
        materialOso.SetFloat("_Metallic", 0f);
        materialOso.SetFloat("_Smoothness", 0.18f);
        materialesAvatarTemporales.Add(materialOso);

        int mallasActualizadas = 0;
        foreach (Renderer renderer in instancia.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materiales = renderer.sharedMaterials;
            if (materiales == null || materiales.Length == 0) continue;
            // El prefab Golosa_Oso_Texturas07 usa un mismo atlas en sus mallas.
            // Mantener el numero de ranuras impide alterar el Renderer original.
            for (int i = 0; i < materiales.Length; i++) materiales[i] = materialOso;
            renderer.sharedMaterials = materiales;
            mallasActualizadas++;
        }

        Debug.Log("Golosa AR: textura original del oso restaurada en " +
                  mallasActualizadas + " Renderer(s) de la instancia AR.");
    }

    private static bool MedirAvatar(Renderer[] renderers, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool encontrado = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (!encontrado) { bounds = renderer.bounds; encontrado = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return encontrado && bounds.size.y > 0.0001f;
    }

    // Bienvenida integrada en el Canvas existente: no se crea otra escena ni cámara.
    private void MostrarBienvenida()
    {
        bienvenidaActiva = true;
        if (bienvenida == null) CrearBienvenida();
        // La tarjeta se reutiliza entre turnos: actualizar siempre el nombre
        // del participante activo, aunque el objeto UI ya esté construido.
        ActualizarNombreBienvenida();
        bienvenida.SetActive(true);
        if (avatarJuego != null)
        {
            // El tablero avanza por +Z; hacia la cámara del jugador normalmente es -Z.
            // Se rota únicamente la instancia; después se restaura la orientación.
            avatarJuego.localRotation = rotacionJuego * Quaternion.Euler(0f, 180f, 0f);
        }
        foreach (Animator a in animadoresSaludo)
        {
            if (a == null) continue;
            a.applyRootMotion = false;
            // Usar la animacion predeterminada del controller sin forzar Rebind/Play.
            // Algunos VRM pierden su pose al reiniciar el Animator en caliente.
            a.enabled = true;
        }
        if (rutinaConfeti != null) StopCoroutine(rutinaConfeti);
        rutinaConfeti = StartCoroutine(AnimarConfeti());
    }

    /// <summary>
    /// Actualiza el saludo antes de cada turno. La pantalla de bienvenida
    /// se construye una sola vez y por ello no puede depender del texto que
    /// recibió cuando comenzó el primer jugador.
    /// </summary>
    private void ActualizarNombreBienvenida()
    {
        if (bienvenida == null) return;
        Transform tarjeta = bienvenida.transform.Find("TarjetaBienvenida");
        if (tarjeta == null) return;
        Transform titulo = tarjeta.Find("Titulo");
        if (titulo == null) return;
        TextMeshProUGUI etiqueta = titulo.GetComponent<TextMeshProUGUI>();
        if (etiqueta == null) return;

        // El turno se actualiza desde GolosaTurnosDatos; en el modo
        // Individual se conserva el nombre guardado en PlayerPrefs.
        string nombre = GolosaTurnosDatos.Activa && GolosaTurnosDatos.Actual != null
            ? GolosaTurnosDatos.Actual.nombre
            : PlayerPrefs.GetString("Golosa.Nombre", "Jugador");
        if (string.IsNullOrWhiteSpace(nombre)) nombre = "Jugador";
        nombre = nombre.Trim();
        if (nombre.Length > 14) nombre = nombre.Substring(0, 14) + "…";
        etiqueta.text = "¡HOLA, " + nombre.ToUpperInvariant() + "!";
    }

    // Diseño visual independiente del juego AR: usa dos PNG y mantiene el texto dinámico.
    private void CrearBienvenida()
    {
        bienvenida = new GameObject("Pantalla07_Bienvenida", typeof(RectTransform));
        RectTransform raiz = bienvenida.GetComponent<RectTransform>();
        Colocar(raiz, area, new Vector2(0, 350), new Vector2(1040, 550), new Vector2(.5f, 0f));

        // Los PNG se cargan como Texture2D, así Unity no necesita convertirlos a Sprite.
        Texture2D fondoTextura = Resources.Load<Texture2D>("GolosaARBienvenida/PanelBienvenida");
        RectTransform panel = new GameObject("TarjetaBienvenida", typeof(RectTransform), typeof(RawImage)).GetComponent<RectTransform>();
        Colocar(panel, raiz, Vector2.zero, new Vector2(1020, 530));
        RawImage fondo = panel.GetComponent<RawImage>();
        fondo.texture = fondoTextura;
        fondo.color = fondoTextura != null ? Color.white : C("#FFF6FC");
        fondo.raycastTarget = true;

        string nombre = PlayerPrefs.GetString("Golosa.Nombre", "Jugador").Trim();
        if (string.IsNullOrEmpty(nombre)) nombre = "Jugador";
        if (nombre.Length > 14) nombre = nombre.Substring(0, 14) + "…";

        Texto(panel, "Titulo", "¡HOLA, " + nombre.ToUpperInvariant() + "!",
            new Vector2(0, 121), new Vector2(810, 100), 60, Uva);
        Texto(panel, "Mensaje", "¡Tu avatar te da la bienvenida a la aventura de Golosa AR!",
            new Vector2(0, 25), new Vector2(820, 110), 34, Uva);

        Texture2D botonTextura = Resources.Load<Texture2D>("GolosaARBienvenida/BotonEmpecemos");
        RectTransform botonR = new GameObject("BotonEmpecemos", typeof(RectTransform), typeof(RawImage)).GetComponent<RectTransform>();
        Colocar(botonR, panel, new Vector2(0, -145), new Vector2(770, 155));
        RawImage imagenBoton = botonR.GetComponent<RawImage>();
        imagenBoton.texture = botonTextura;
        imagenBoton.color = botonTextura != null ? Color.white : C("#FFDB25");
        imagenBoton.raycastTarget = true;
        Button boton = botonR.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagenBoton;
        boton.onClick.AddListener(AceptarBienvenida);
        Texto(botonR, "Etiqueta", "¡EMPECEMOS!", new Vector2(38, 3),
            new Vector2(630, 110), 52, Uva);
    }

    // Animación discreta de entrada. No depende de símbolos de fuentes TMP.
    private IEnumerator AnimarConfeti()
    {
        if (bienvenida == null) yield break;
        Transform panel = bienvenida.transform.Find("TarjetaBienvenida");
        if (panel == null) yield break;
        RectTransform rt = panel as RectTransform;
        float t = 0f;
        while (bienvenidaActiva && t < 0.26f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.26f);
            rt.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, 1f - (1f - p) * (1f - p));
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    private void DesactivarSaludo()
    {
        // Mantener activos los Animator para no dejar avatares VRM
        // en postura de reposo o con los huesos mal colocados.
        // El desplazamiento del avatar sigue controlado por el juego
        // y el root motion permanece desactivado.
    }

    private void AceptarBienvenida()
    {
        if (!bienvenidaActiva) return;
        bienvenidaActiva = false;
        if (rutinaConfeti != null) { StopCoroutine(rutinaConfeti); rutinaConfeti = null; }
        if (bienvenida != null) bienvenida.SetActive(false);
        DesactivarSaludo();
        if (avatarJuego != null) avatarJuego.localRotation = rotacionJuego;
        controlesJuego.SetActive(true);
        if (pantallas != null) pantallas.ActivarMenuJuego();
        relojActivo = true;
        mensajeTexto.text = "¡Tu turno! Lanza la piedra";
        ActualizarBotones();
    }

    private void Volver()
    {
        if (GolosaTurnosDatos.Activa) GolosaTurnosDatos.Cancelar();
        if (Application.CanStreamedLevelBeLoaded("02_ModoJuego")) SceneManager.LoadScene("02_ModoJuego");
        else Debug.LogWarning("Agrega 02_ModoJuego en File > Build Profiles > Scene List.");
    }

    private void OnDestroy()
    {
        QuitarPiedra();
        foreach (Material m in materialesAvatarTemporales)
            if (m != null) Destroy(m);
        materialesAvatarTemporales.Clear();
        if (avatarJuego != null) Destroy(avatarJuego.gameObject);
        if (canvas != null) Destroy(canvas.gameObject);
        foreach (var s in spritesCreados) if (s != null) Destroy(s);
    }
    // Las posiciones coinciden con el GolosaTablero3D recibido.
    private static Vector3 PosicionCasilla(int n)
    {
        int fila = n == 1 ? 0 : n <= 3 ? 1 : n == 4 ? 2 : n <= 6 ? 3 : n == 7 ? 4 : n <= 9 ? 5 : 6;
        float x = (n == 2 || n == 5 || n == 8) ? -.166f : (n == 3 || n == 6 || n == 9) ? .166f : 0f;
        return new Vector3(x, 0f, (fila - 3) * .317f);
    }

    private static Color C(string h) { ColorUtility.TryParseHtmlString(h, out Color color); return color; }

    private Sprite Recurso(string nombre)
    {
        Sprite imagen;
        if (sprites.TryGetValue(nombre, out imagen) && imagen != null)
            return imagen;
        Debug.LogWarning("Golosa AR: no se vinculo el Sprite '" + nombre +
            "'. En Unity ejecuta Golosa AR > 03 - Vincular imagenes del juego.");
        return null;
    }

    private static void Colocar(RectTransform rt, Transform padre, Vector2 posicion, Vector2 tam, Vector2? ancla = null)
    {
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = ancla ?? new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tam;
    }

    private RectTransform Imagen(Transform padre, string nombre, string recurso, Vector2 posicion,
        Vector2 tam, Vector2? ancla = null)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        Colocar(rt, padre, posicion, tam, ancla);
        Image imagen = go.GetComponent<Image>();
        imagen.sprite = Recurso(recurso);
        imagen.color = Blanco;
        imagen.raycastTarget = false;
        return rt;
    }

    private static RectTransform Panel(Transform padre, string nombre, Vector2 posicion, Vector2 tam,
        Color color, Vector2? ancla = null)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        Colocar(rt, padre, posicion, tam, ancla);
        Image i = go.GetComponent<Image>();
        i.color = color;
        i.raycastTarget = false;
        return rt;
    }

    private static TextMeshProUGUI Texto(Transform padre, string nombre, string contenido, Vector2 posicion,
        Vector2 tam, float fontSize, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        Colocar(rt, padre, posicion, tam);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = contenido;
        t.fontSize = fontSize;
        t.fontSizeMin = 20f;
        t.fontSizeMax = fontSize;
        t.enableAutoSizing = true;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Truncate;
        t.raycastTarget = false;
        return t;
    }

    private Button BotonImagen(Transform padre, string nombre, string recurso, string etiqueta, string icono,
        Vector2 pos, Vector2 tam, float fuente, Color color, UnityEngine.Events.UnityAction accion, bool vertical)
    {
        RectTransform r = Imagen(padre, nombre, recurso, pos, tam);
        Image img = r.GetComponent<Image>();
        img.raycastTarget = true;
        Button btn = r.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(accion);
        if (!string.IsNullOrEmpty(icono))
        {
            Imagen(r, "Icono", icono, vertical ? new Vector2(0, 34) : new Vector2(-310, 2),
                vertical ? new Vector2(75, 75) : new Vector2(90, 90));
        }
        Texto(r, "Etiqueta", etiqueta, vertical ? new Vector2(0, -49) : new Vector2(65, 0),
            vertical ? new Vector2(tam.x - 28, 57) : new Vector2(tam.x - 250, 130), fuente, color);
        return btn;
    }
}

