using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

// Golosa AR: lógica del perfil y personalización del VRM.
// Solo cambia materiales de la INSTANCIA y crea accesorios 3D durante Play.
// No cambia el archivo VRM original ni la pantalla de inicio.
/// <summary>
/// Gestiona la selección de avatar, los datos del perfil y la personalización visual del personaje.
/// </summary>
public class PerfilUI : MonoBehaviour
{
    public TMP_InputField campoNombre;
    // Compatibilidad con versiones anteriores del generador (no se usan en la UI actual).
    public TMP_Dropdown campoEdad;
    public TextMeshProUGUI textoCategoria;
    public Image tarjetaNinos;
    public Image tarjetaAvanzados;
    public TextMeshProUGUI indicadorNinos;
    public TextMeshProUGUI indicadorAvanzados;
    public TextMeshProUGUI mensaje;
    public Transform avatar3D;
    public GameObject panelPersonalizar;

    [Header("Seleccion de personajes - asignar desde Hierarchy")]
    public GameObject[] avataresDisponibles;
    public TextMeshProUGUI textoAvatarElegido;
    [Header("Boton elegir avatar (opcional: se busca automaticamente)")]
    public Button botonElegirAvatar;

    [Header("Espaciado de tarjetas (opcional)")]
    [Tooltip("Arrastra aqui el RectTransform del panel completo del nombre, NO el texto.")]
    public RectTransform panelNombre;
    [Tooltip("Arrastra aqui el RectTransform del panel completo de categoria.")]
    public RectTransform panelCategoria;
    [Tooltip("Distancia adicional hacia abajo de la tarjeta de categorias, en unidades UI.")]
    [Min(0f)] public float espacioEntreTarjetas = 14f;

    private bool botonConectado;
    private int indiceAvatar;
    private bool avatarConfirmado;

    public TextMeshProUGUI mensajePersonalizacion;
    public Image[] muestrasCabello;
    public Image[] muestrasRopa;
    public Image[] muestrasAccesorios;
    public Image[] fondosPestanas;
    public GameObject[] panelesOpciones;

    private string categoria = "Niños";
    private Quaternion rotacionBase = Quaternion.identity;
    private float anguloObjetivo;
    private int cabelloElegido;
    private int ropaElegida;
    private int accesorioElegido;
    private GameObject objetoAccesorio;
    private Transform huesoCabeza;
    private bool avisoCabeza;

    private const string ClaveCabello = "Golosa.Avatar.Cabello";
    private const string ClaveRopa = "Golosa.Avatar.Ropa";
    private const string ClaveAccesorio = "Golosa.Avatar.Accesorio";
    private const string ClaveNombre = "Golosa.Nombre";
    private const string ClaveCategoria = "Golosa.Categoria";

    private static readonly Color Seleccionado = new Color32(224, 240, 255, 255);
    private static readonly Color NoSeleccionado = Color.white;
    private static readonly Color BordeActivo = new Color32(125, 42, 224, 255);
    private static readonly Color BordeNormal = new Color32(196, 207, 234, 255);
    private static readonly Color[] ColoresCabello =
    {
        Color.white,                    // 0 = tono ORIGINAL del VRM
        new Color32(112, 67, 48, 255), // 1 = chocolate
        new Color32(50, 45, 53, 255),  // 2 = negro suave
        new Color32(224, 173, 91, 255) // 3 = rubio dorado
    };
    private static readonly Color[] ColoresRopa =
    {
        Color.white,                    // 0 = ropa ORIGINAL del VRM
        new Color32(126, 41, 226, 255), // 1 = morado Golosa
        new Color32(92, 173, 245, 255), // 2 = azul cielo
        new Color32(94, 217, 194, 255), // 3 = turquesa
        new Color32(255, 201, 81, 255)  // 4 = amarillo
    };

    private readonly List<Material> materialesCabello = new List<Material>();
    private readonly List<Material> materialesRopa = new List<Material>();
    private readonly Dictionary<Material, Color> originales = new Dictionary<Material, Color>();
    private readonly Dictionary<Material, Color> sombras = new Dictionary<Material, Color>();

    /// <summary>
    /// Recupera preferencias, prepara la vista previa de avatares y conecta controles.
    /// </summary>
    private void Start()
    {
        if (mensaje != null) { mensaje.text = ""; mensaje.gameObject.SetActive(false); }
        if (campoNombre != null)
            campoNombre.SetTextWithoutNotify(PlayerPrefs.GetString(ClaveNombre, ""));
        categoria = PlayerPrefs.GetString(ClaveCategoria, "Niños");
        if (categoria != "Niños" && categoria != "Avanzados") categoria = "Niños";
        if (avatar3D != null) rotacionBase = avatar3D.localRotation;
        if (panelPersonalizar != null) panelPersonalizar.SetActive(false);

        DetectarMaterialesVRM();
        cabelloElegido = Mathf.Clamp(PlayerPrefs.GetInt(ClaveCabello, 0), 0, 3);
        ropaElegida = Mathf.Clamp(PlayerPrefs.GetInt(ClaveRopa, 0), 0, 4);
        accesorioElegido = Mathf.Clamp(PlayerPrefs.GetInt(ClaveAccesorio, 0), 0, 3);
        BuscarCabeza();
        AplicarColores();
        CambiarObjetoAccesorio();
        ActualizarTarjetas();
        ActualizarSeleccionMuestras();
        SeleccionarPestana(0);
        InicializarSeleccionAvatares();
        ConfigurarBotonElegir();
        AjustarEspaciadoTarjetas();
    }

    // Las flechas ahora CAMBIAN de personaje, no lo hacen girar.
    /// <summary>
    /// Gira la vista previa del avatar sobre su eje vertical.
    /// </summary>
    public void GirarAvatar() { if (avatar3D != null) avatar3D.Rotate(0f, 45f, 0f, Space.Self); }
    /// <summary>
    /// Muestra el avatar disponible anterior.
    /// </summary>
    public void GirarAvatarIzquierda() { CambiarAvatar(-1); }
    /// <summary>
    /// Muestra el siguiente avatar disponible.
    /// </summary>
    public void GirarAvatarDerecha() { CambiarAvatar(1); }

    /// <summary>
    /// Recupera la selección persistida y establece el avatar visible inicial.
    /// </summary>
    private void InicializarSeleccionAvatares()
    {
        if (avataresDisponibles == null || avataresDisponibles.Length == 0)
        {
            Avisar("Asigna los avatares en el Inspector de PerfilUI.");
            return;
        }
        string guardado = PlayerPrefs.GetString("Golosa.AvatarId", "");
        indiceAvatar = 0;
        // Comparar IDs estables; los nombres de los objetos de la vista previa
        // no tienen por qué coincidir con los IDs del catálogo de 03_Juego.
        for (int i = 0; i < avataresDisponibles.Length; i++)
        {
            if (avataresDisponibles[i] != null && IdAvatarEnIndice(i) == guardado)
            {
                indiceAvatar = i;
                break;
            }
        }
        avatarConfirmado = false;
        MostrarAvatarActual();
        ActualizarTextoBotonAvatar();
    }

    /// <summary>
    /// Recorre el catálogo de avatares evitando referencias nulas.
    /// </summary>
    /// <param name="direccion">Desplazamiento en la lista de avatares.</param>
    private void CambiarAvatar(int direccion)
    {
        if (avataresDisponibles == null || avataresDisponibles.Length == 0)
        {
            Avisar("Primero conecta los avatares en el Inspector.");
            return;
        }
        // Busca el siguiente objeto existente por si hay huecos en la lista.
        for (int intentos = 0; intentos < avataresDisponibles.Length; intentos++)
        {
            indiceAvatar = (indiceAvatar + direccion + avataresDisponibles.Length) % avataresDisponibles.Length;
            if (avataresDisponibles[indiceAvatar] != null) break;
        }
        avatarConfirmado = false;
        MostrarAvatarActual();
        ActualizarTextoBotonAvatar();
        if (mensaje != null) mensaje.text = "";
    }

    /// <summary>
    /// Activa únicamente el objeto del avatar seleccionado y actualiza su etiqueta.
    /// </summary>
    private void MostrarAvatarActual()
    {
        if (avataresDisponibles == null) return;
        for (int i = 0; i < avataresDisponibles.Length; i++)
            if (avataresDisponibles[i] != null)
                avataresDisponibles[i].SetActive(i == indiceAvatar);

        GameObject elegido = indiceAvatar < avataresDisponibles.Length ? avataresDisponibles[indiceAvatar] : null;
        if (textoAvatarElegido != null)
            textoAvatarElegido.text = elegido != null ? elegido.name.Replace("Golosa_Preview", "") : "Elige tu personaje";
    }

    // Conectar al botón «¡QUIERO ESTE AVATAR!» en el generador o Inspector.
    /// <summary>
    /// Confirma el avatar elegido y muestra el aviso correspondiente.
    /// </summary>
    public void ElegirAvatarActual()
    {
        if (avataresDisponibles == null || avataresDisponibles.Length == 0 ||
            avataresDisponibles[indiceAvatar] == null)
        {
            Avisar("No hay ningún avatar disponible.");
            return;
        }
        avatarConfirmado = true;
        ActualizarTextoBotonAvatar();
        Avisar("¡Avatar elegido! Escribe tu nombre y continúa.", true);
    }
    // Si se deja sin asignar, localiza el boton que antes decia PERSONALIZAR.
    // Los eventos antiguos se deshabilitan solo durante Play, sin alterar la escena guardada.
    /// <summary>
    /// Busca el botón de selección y vincula su acción durante la ejecución.
    /// </summary>
    private void ConfigurarBotonElegir()
    {
        if (botonElegirAvatar == null)
        {
            Canvas lienzo = GetComponentInParent<Canvas>();
            if (lienzo == null) lienzo = FindFirstObjectByType<Canvas>();
            if (lienzo != null)
            {
                foreach (Button candidato in lienzo.GetComponentsInChildren<Button>(true))
                {
                    TextMeshProUGUI etiqueta = candidato.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (etiqueta == null) continue;
                    string contenido = etiqueta.text.ToUpperInvariant();
                    if (contenido.Contains("PERSONALIZAR") || contenido.Contains("QUIERO ESTE AVATAR"))
                    {
                        botonElegirAvatar = candidato;
                        break;
                    }
                }
            }
        }

        if (botonElegirAvatar == null)
        {
            Debug.LogWarning("Golosa AR: Asigna 'Boton Elegir Avatar' en PerfilUI para convertir PERSONALIZAR en seleccionar avatar.");
            return;
        }

        if (!botonConectado)
        {
            // Desactiva los OnClick serializados del boton, sin modificarlos en el Editor.
            for (int i = 0; i < botonElegirAvatar.onClick.GetPersistentEventCount(); i++)
                botonElegirAvatar.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
            botonElegirAvatar.onClick.RemoveAllListeners();
            botonElegirAvatar.onClick.AddListener(ElegirAvatarActual);
            botonConectado = true;
        }
        ActualizarTextoBotonAvatar();
    }

    /// <summary>
    /// Refleja en el botón si el avatar actual ha sido confirmado.
    /// </summary>
    private void ActualizarTextoBotonAvatar()
    {
        if (botonElegirAvatar == null) return;
        TextMeshProUGUI etiqueta = botonElegirAvatar.GetComponentInChildren<TextMeshProUGUI>(true);
        if (etiqueta != null)
            etiqueta.text = avatarConfirmado ? "✓ ¡AVATAR ELEGIDO!" : "¡QUIERO ESTE AVATAR!";
    }

    // El espaciado es opcional y solo se aplica si asignas los paneles en Inspector.
    // No mueve el tablero, ni la camara, ni los personajes.
    /// <summary>
    /// Aplica un desplazamiento opcional a las tarjetas sin interferir con LayoutGroup.
    /// </summary>
    private void AjustarEspaciadoTarjetas()
    {
        if (panelNombre == null || panelCategoria == null) return;
        // Si hay un LayoutGroup controlando posiciones, evitar interferir.
        if (panelCategoria.GetComponentInParent<LayoutGroup>() != null) return;
        Vector2 posicion = panelCategoria.anchoredPosition;
        panelCategoria.anchoredPosition = new Vector2(posicion.x, posicion.y - espacioEntreTarjetas);
    }

    /// <summary>
    /// Marca la categoría Niños y refresca las tarjetas.
    /// </summary>
    public void SeleccionarNinos() { categoria = "Niños"; ActualizarTarjetas(); }
    /// <summary>
    /// Marca la categoría Avanzados y refresca las tarjetas.
    /// </summary>
    public void SeleccionarAvanzados() { categoria = "Avanzados"; ActualizarTarjetas(); }

    /// <summary>
    /// Resalta visualmente la categoría de jugador elegida.
    /// </summary>
    private void ActualizarTarjetas()
    {
        bool esNinos = categoria == "Niños";
        AplicarSeleccion(tarjetaNinos, indicadorNinos, esNinos);
        AplicarSeleccion(tarjetaAvanzados, indicadorAvanzados, !esNinos);
        if (mensaje != null) mensaje.text = "";
    }

    /// <summary>
    /// Configura los colores y el indicador de una tarjeta según su estado.
    /// </summary>
    private static void AplicarSeleccion(Image tarjeta, TextMeshProUGUI indicador, bool activo)
    {
        if (tarjeta != null)
        {
            tarjeta.color = activo ? Seleccionado : NoSeleccionado;
            Outline borde = tarjeta.GetComponent<Outline>();
            if (borde != null) borde.effectColor = activo ? BordeActivo : BordeNormal;
        }
        if (indicador != null)
        {
            indicador.text = activo ? "●" : "";
            indicador.color = BordeActivo;
        }
    }

    /// <summary>
    /// Comprueba si un texto contiene alguna de las palabras indicadas.
    /// </summary>
    private static bool Contiene(string texto, params string[] palabras)
    {
        foreach (string palabra in palabras)
            if (texto.Contains(palabra)) return true;
        return false;
    }

    /// <summary>
    /// Obtiene el nombre de la propiedad cromática compatible con el material.
    /// </summary>
    private static string PropiedadColor(Material material)
    {
        if (material.HasProperty("_BaseColor")) return "_BaseColor";
        if (material.HasProperty("_Color")) return "_Color";
        return null;
    }

    /// <summary>
    /// Identifica materiales editables de cabello y ropa, conservando sus colores originales.
    /// </summary>
    private void DetectarMaterialesVRM()
    {
        materialesCabello.Clear();
        materialesRopa.Clear();
        originales.Clear();
        sombras.Clear();
        if (avatar3D == null) return;

        HashSet<Material> vistos = new HashSet<Material>();
        foreach (Renderer r in avatar3D.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in r.materials)
            {
                if (material == null || !vistos.Add(material)) continue;
                string nombre = material.name.ToLowerInvariant();
                string ruta = r.name.ToLowerInvariant();
                bool exclusion = Contiene(nombre, "skin", "face", "eye", "eyebrow", "eyelash") ||
                    Contiene(ruta, "face", "eye", "eyebrow", "eyelash");
                bool cabello = !exclusion && (Contiene(nombre, "hair", "cabello") ||
                    Contiene(ruta, "hair", "cabello"));
                bool ropa = !exclusion && !cabello &&
                    (Contiene(nombre, "tops", "bottoms", "outfit", "coat", "hoodie",
                        "shirt", "jacket", "pants", "shorts", "clothing", "uniform", "cloth") ||
                     Contiene(ruta, "tops", "bottoms", "outfit", "coat", "cloth"));
                if (!cabello && !ropa) continue;
                string propiedad = PropiedadColor(material);
                if (propiedad == null) continue;
                originales[material] = material.GetColor(propiedad);
                if (material.HasProperty("_ShadeColor"))
                    sombras[material] = material.GetColor("_ShadeColor");
                if (cabello) materialesCabello.Add(material);
                else materialesRopa.Add(material);
            }
        }
        Debug.Log("Golosa AR: materiales editables: cabello=" + materialesCabello.Count +
                  ", ropa=" + materialesRopa.Count);
    }

    /// <summary>
    /// Aplica una opción de color preservando transparencia y sombra del material.
    /// </summary>
    private static void Pintar(List<Material> materiales, int indice, Color[] paleta,
        Dictionary<Material, Color> bases, Dictionary<Material, Color> sombrasOriginales)
    {
        foreach (Material material in materiales)
        {
            if (material == null || !bases.ContainsKey(material)) continue;
            string propiedad = PropiedadColor(material);
            if (propiedad == null) continue;
            Color original = bases[material];
            Color nuevo = indice == 0 ? original : paleta[indice];
            nuevo.a = original.a; // conserva las transparencias VRM
            material.SetColor(propiedad, nuevo);
            if (sombrasOriginales.ContainsKey(material))
            {
                Color sombra = indice == 0 ? sombrasOriginales[material] :
                    Color.Lerp(Color.black, nuevo, .68f);
                sombra.a = sombrasOriginales[material].a;
                material.SetColor("_ShadeColor", sombra);
            }
        }
    }

    /// <summary>
    /// Actualiza los materiales de cabello y ropa con las selecciones actuales.
    /// </summary>
    private void AplicarColores()
    {
        Pintar(materialesCabello, cabelloElegido, ColoresCabello, originales, sombras);
        Pintar(materialesRopa, ropaElegida, ColoresRopa, originales, sombras);
    }

    /// <summary>
    /// Actualiza los bordes de las muestras para identificar la opción seleccionada.
    /// </summary>
    private static void Resaltar(Image[] muestras, int elegido)
    {
        if (muestras == null) return;
        for (int i = 0; i < muestras.Length; i++)
        {
            Image imagen = muestras[i];
            if (imagen == null) continue;
            Outline borde = imagen.GetComponent<Outline>();
            if (borde != null)
            {
                borde.effectColor = i == elegido ? new Color32(255, 192, 47, 255) :
                    new Color32(190, 202, 228, 255);
                borde.effectDistance = i == elegido ? new Vector2(7, -7) : new Vector2(2, -2);
            }
        }
    }

    /// <summary>
    /// Refresca los indicadores de cabello, ropa y accesorios.
    /// </summary>
    private void ActualizarSeleccionMuestras()
    {
        Resaltar(muestrasCabello, cabelloElegido);
        Resaltar(muestrasRopa, ropaElegida);
        Resaltar(muestrasAccesorios, accesorioElegido);
    }

    // Solo se ve una pestaña a la vez, para evitar recortes en 1080x2340.
    /// <summary>
    /// Muestra solo el panel de personalización correspondiente a la pestaña activa.
    /// </summary>
    private void SeleccionarPestana(int indice)
    {
        if (panelesOpciones != null)
            for (int i = 0; i < panelesOpciones.Length; i++)
                if (panelesOpciones[i] != null) panelesOpciones[i].SetActive(i == indice);
        if (fondosPestanas != null)
            for (int i = 0; i < fondosPestanas.Length; i++)
                if (fondosPestanas[i] != null)
                    fondosPestanas[i].color = i == indice ?
                        new Color32(255, 197, 62, 255) : Color.white;
    }

    /// <summary>
    /// Abre las opciones de cabello.
    /// </summary>
    public void MostrarCabello() { SeleccionarPestana(0); }
    /// <summary>
    /// Abre las opciones de ropa.
    /// </summary>
    public void MostrarRopa() { SeleccionarPestana(1); }
    /// <summary>
    /// Abre las opciones de accesorios.
    /// </summary>
    public void MostrarAccesorios() { SeleccionarPestana(2); }

    /// <summary>
    /// Abre el panel de personalización y actualiza sus controles.
    /// </summary>
    public void AbrirPersonalizar()
    {
        if (panelPersonalizar != null) panelPersonalizar.SetActive(true);
        SeleccionarPestana(0);
        ActualizarSeleccionMuestras();
        MostrarEstado();
    }

    /// <summary>
    /// Muestra información contextual sobre el avatar y sus materiales.
    /// </summary>
    private void MostrarEstado()
    {
        if (mensajePersonalizacion == null) return;
        if (avatar3D == null)
            mensajePersonalizacion.text = "No encontramos el avatar 3D en la escena.";
        else if (materialesCabello.Count == 0 || materialesRopa.Count == 0)
            mensajePersonalizacion.text = "Si un color no cambia, revisaremos los materiales VRM.";
        else if (avisoCabeza)
            mensajePersonalizacion.text = "El accesorio está unido al avatar (sin hueso de cabeza).";
        else
            mensajePersonalizacion.text = accesorioElegido == 0 ? "¡Combina colores y accesorios a tu estilo!" : "Accesorio 3D seleccionado: gira el avatar para verlo.";
    }

    /// <summary>
    /// Descarta ajustes no guardados y cierra la personalización.
    /// </summary>
    public void CerrarPersonalizar()
    {
        // Deshacer cambios nuevos: recuperar las opciones guardadas anteriormente.
        cabelloElegido = Mathf.Clamp(PlayerPrefs.GetInt(ClaveCabello, 0), 0, 3);
        ropaElegida = Mathf.Clamp(PlayerPrefs.GetInt(ClaveRopa, 0), 0, 4);
        accesorioElegido = Mathf.Clamp(PlayerPrefs.GetInt(ClaveAccesorio, 0), 0, 3);
        AplicarColores();
        CambiarObjetoAccesorio();
        ActualizarSeleccionMuestras();
        if (panelPersonalizar != null) panelPersonalizar.SetActive(false);
    }

    /// <summary>
    /// Confirma los ajustes y cierra el panel.
    /// </summary>
    public void GuardarPersonalizacion()
    {
        GuardarOpciones();
        if (panelPersonalizar != null) panelPersonalizar.SetActive(false);
    }

    /// <summary>
    /// Almacena las elecciones de cabello, ropa y accesorio en PlayerPrefs.
    /// </summary>
    private void GuardarOpciones()
    {
        PlayerPrefs.SetInt(ClaveCabello, cabelloElegido);
        PlayerPrefs.SetInt(ClaveRopa, ropaElegida);
        PlayerPrefs.SetInt(ClaveAccesorio, accesorioElegido);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Selecciona un tono de cabello y actualiza la vista previa.
    /// </summary>
    private void CambiarCabello(int indice)
    {
        cabelloElegido = Mathf.Clamp(indice, 0, 3);
        AplicarColores();
        ActualizarSeleccionMuestras();
        MostrarEstado();
    }

    /// <summary>
    /// Selecciona un tono de ropa y actualiza la vista previa.
    /// </summary>
    private void CambiarRopa(int indice)
    {
        ropaElegida = Mathf.Clamp(indice, 0, 4);
        AplicarColores();
        ActualizarSeleccionMuestras();
        MostrarEstado();
    }

    /// <summary>
    /// Selecciona un accesorio tridimensional para la vista previa.
    /// </summary>
    private void CambiarAccesorio(int indice)
    {
        accesorioElegido = Mathf.Clamp(indice, 0, 3);
        CambiarObjetoAccesorio();
        ActualizarSeleccionMuestras();
        MostrarEstado();
    }

    // Conectores sin argumentos, para los botones persistentes del Editor.
    public void Cabello0() { CambiarCabello(0); }
    public void Cabello1() { CambiarCabello(1); }
    public void Cabello2() { CambiarCabello(2); }
    public void Cabello3() { CambiarCabello(3); }
    public void Ropa0() { CambiarRopa(0); }
    public void Ropa1() { CambiarRopa(1); }
    public void Ropa2() { CambiarRopa(2); }
    public void Ropa3() { CambiarRopa(3); }
    public void Ropa4() { CambiarRopa(4); }
    public void Accesorio0() { CambiarAccesorio(0); }
    public void Accesorio1() { CambiarAccesorio(1); }
    public void Accesorio2() { CambiarAccesorio(2); }
    public void Accesorio3() { CambiarAccesorio(3); }

    // Busca el hueso de la cabeza para que el accesorio pueda acompañar la animación.
    // Si el importador VRM no lo expone, usamos la raíz del avatar como respaldo.
    /// <summary>
    /// Busca un hueso de cabeza válido o usa el avatar como anclaje de respaldo.
    /// </summary>
    private void BuscarCabeza()
    {
        huesoCabeza = null;
        avisoCabeza = false;
        if (avatar3D == null) return;

        Animator animador = avatar3D.GetComponentInChildren<Animator>(true);
        if (animador != null && animador.isHuman)
        {
            try { huesoCabeza = animador.GetBoneTransform(HumanBodyBones.Head); }
            catch (System.Exception e)
            {
                Debug.LogWarning("Golosa AR: hueso humanoide no accesible: " + e.Message);
            }
        }
        if (huesoCabeza == null)
        {
            foreach (Transform t in avatar3D.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (n == "head" || n == "j_bip_c_head" || n == "headbone" ||
                    n == "mixamorig:head" || n == "bip001 head")
                {
                    huesoCabeza = t;
                    break;
                }
            }
        }
        if (huesoCabeza == null)
        {
            huesoCabeza = avatar3D; // visible incluso con un VRM sin humanoide válido
            avisoCabeza = true;
        }
    }

    // Obtiene la altura VISIBLE del VRM. Evita desplazar los accesorios con números
    // fijos relativos al hueso, que podían enterrarlos en el pelo.
    /// <summary>
    /// Calcula el punto superior visible del modelo para posicionar accesorios.
    /// </summary>
    /// <param name="centroCorona">Posición calculada para el accesorio.</param>
    /// <param name="escala">Factor de escala estimado del avatar.</param>
    /// <returns>Verdadero si pudo obtenerse un límite visible del avatar.</returns>
    private bool ObtenerCoronilla(out Vector3 centroCorona, out float escala)
    {
        centroCorona = avatar3D == null ? Vector3.zero : avatar3D.position;
        escala = 1f;
        if (avatar3D == null) return false;

        bool encontrado = false;
        Bounds limites = new Bounds(avatar3D.position, Vector3.zero);
        foreach (Renderer r in avatar3D.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            bool esAccesorio = false;
            Transform padre = r.transform;
            while (padre != null && padre != avatar3D)
            {
                if (padre.name == "AccesorioGolosa3D") { esAccesorio = true; break; }
                padre = padre.parent;
            }
            if (esAccesorio) continue;
            if (!encontrado) { limites = r.bounds; encontrado = true; }
            else limites.Encapsulate(r.bounds);
        }

        if (!encontrado)
        {
            Debug.LogWarning("Golosa AR: no encontré los renderizadores del VRM.");
            return false;
        }
        float altura = Mathf.Max(.5f, limites.size.y);
        escala = Mathf.Clamp(altura / 1.25f, .5f, 3.0f);
        // Un poco arriba de la parte más alta del cabello. El eje Z positivo es el
        // frente de nuestro avatar VRM en la vista de perfil.
        centroCorona = new Vector3(limites.center.x, limites.max.y + .018f * altura,
            limites.center.z) + avatar3D.forward * (.015f * altura);
        return true;
    }

    /// <summary>
    /// Elimina el accesorio anterior y construye el seleccionado sobre el avatar.
    /// </summary>
    private void CambiarObjetoAccesorio()
    {
        if (objetoAccesorio != null)
        {
            Destroy(objetoAccesorio);
            objetoAccesorio = null;
        }
        if (accesorioElegido == 0 || avatar3D == null) return;

        if (huesoCabeza == null) BuscarCabeza();
        Vector3 corona;
        float s;
        if (!ObtenerCoronilla(out corona, out s)) return;

        objetoAccesorio = new GameObject("AccesorioGolosa3D");
        objetoAccesorio.transform.position = corona;
        objetoAccesorio.transform.rotation = avatar3D.rotation;
        objetoAccesorio.transform.SetParent(huesoCabeza != null ? huesoCabeza : avatar3D, true);
        objetoAccesorio.layer = avatar3D.gameObject.layer;

        if (accesorioElegido == 1)
        {
            // Moño grande de color violeta, en la parte alta DELANTE del cabello.
            CrearPieza(PrimitiveType.Sphere, "LazoIzquierdo", objetoAccesorio.transform,
                new Vector3(-.105f * s, .06f * s, .10f * s),
                new Vector3(.155f * s, .095f * s, .075f * s),
                new Color32(152, 70, 235, 255), new Vector3(0, 0, 28));
            CrearPieza(PrimitiveType.Sphere, "LazoDerecho", objetoAccesorio.transform,
                new Vector3(.105f * s, .06f * s, .10f * s),
                new Vector3(.155f * s, .095f * s, .075f * s),
                new Color32(152, 70, 235, 255), new Vector3(0, 0, -28));
            CrearPieza(PrimitiveType.Sphere, "NudoDorado", objetoAccesorio.transform,
                new Vector3(0, .05f * s, .146f * s),
                new Vector3(.075f * s, .076f * s, .05f * s),
                new Color32(255, 205, 70, 255), Vector3.zero);
        }
        else if (accesorioElegido == 2)
        {
            // Gorra deportiva azul, sobre la cabeza con visera hacia delante.
            CrearPieza(PrimitiveType.Sphere, "GorraCopa", objetoAccesorio.transform,
                new Vector3(0, .065f * s, 0),
                new Vector3(.35f * s, .17f * s, .31f * s),
                new Color32(64, 161, 235, 255), Vector3.zero);
            CrearPieza(PrimitiveType.Sphere, "GorraVisera", objetoAccesorio.transform,
                new Vector3(0, .012f * s, .135f * s),
                new Vector3(.38f * s, .036f * s, .23f * s),
                new Color32(64, 91, 198, 255), Vector3.zero);
            CrearPieza(PrimitiveType.Sphere, "AdornoAmarillo", objetoAccesorio.transform,
                new Vector3(0, .10f * s, .17f * s),
                new Vector3(.07f * s, .055f * s, .024f * s),
                new Color32(255, 206, 63, 255), Vector3.zero);
        }
        else if (accesorioElegido == 3)
        {
            CrearPieza(PrimitiveType.Sphere, "EstrellaBroche", objetoAccesorio.transform,
                new Vector3(.11f * s, .095f * s, .09f * s),
                new Vector3(.12f * s, .12f * s, .065f * s),
                new Color32(255, 198, 61, 255), Vector3.zero);
        }
        Debug.Log("Golosa AR: accesorio " + accesorioElegido +
                  " creado sobre el avatar (escala " + s.ToString("F2") + ").");
    }

    /// <summary>
    /// Busca un shader compatible para colorear las piezas tridimensionales.
    /// </summary>
    private static Shader ShaderAccesorio()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        return shader;
    }

    /// <summary>
    /// Aplica un nuevo material de color a un objeto generado.
    /// </summary>
    private static void Colorear(GameObject objeto, Color color)
    {
        Renderer r = objeto.GetComponent<Renderer>();
        if (r == null) return;
        Shader shader = ShaderAccesorio();
        if (shader == null)
        {
            Debug.LogWarning("Golosa AR: no se encontró shader para accesorio.");
            return;
        }
        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        r.material = mat;
    }

    /// <summary>
    /// Genera una pieza primitiva del accesorio y configura su transformación.
    /// </summary>
    private void CrearPieza(PrimitiveType forma, string nombre, Transform padre,
        Vector3 posicion, Vector3 escala, Color color, Vector3 angulos)
    {
        GameObject pieza = GameObject.CreatePrimitive(forma);
        pieza.name = nombre;
        pieza.transform.SetParent(padre, false);
        pieza.transform.localPosition = posicion;
        pieza.transform.localRotation = Quaternion.Euler(angulos);
        pieza.transform.localScale = escala;
        pieza.layer = avatar3D.gameObject.layer; // misma capa de la cámara del preview
        Collider colision = pieza.GetComponent<Collider>();
        if (colision != null) Destroy(colision);
        Colorear(pieza, color);
    }

    /// <summary>
    /// Construye geométricamente una estrella tridimensional.
    /// </summary>
    private void CrearEstrella(Transform padre)
    {
        GameObject estrella = new GameObject("EstrellaDorada3D", typeof(MeshFilter), typeof(MeshRenderer));
        estrella.transform.SetParent(padre, false);
        estrella.transform.localPosition = new Vector3(.072f, .14f, .106f);
        estrella.transform.localRotation = Quaternion.identity;
        estrella.layer = avatar3D.gameObject.layer;

        Vector3[] vertices = new Vector3[22];
        vertices[0] = new Vector3(0f, 0f, .012f);
        vertices[11] = new Vector3(0f, 0f, -.012f);
        for (int i = 0; i < 10; i++)
        {
            float angulo = (90f + 36f * i) * Mathf.Deg2Rad;
            float radio = i % 2 == 0 ? .067f : .027f;
            float x = radio * Mathf.Cos(angulo);
            float y = radio * Mathf.Sin(angulo);
            vertices[i + 1] = new Vector3(x, y, .012f);
            vertices[i + 12] = new Vector3(x, y, -.012f);
        }
        List<int> triangulos = new List<int>();
        for (int i = 0; i < 10; i++)
        {
            int siguiente = (i + 1) % 10;
            triangulos.Add(0); triangulos.Add(i + 1); triangulos.Add(siguiente + 1);
            triangulos.Add(11); triangulos.Add(siguiente + 12); triangulos.Add(i + 12);
            triangulos.Add(i + 1); triangulos.Add(i + 12); triangulos.Add(siguiente + 1);
            triangulos.Add(siguiente + 1); triangulos.Add(i + 12); triangulos.Add(siguiente + 12);
        }
        Mesh malla = new Mesh();
        malla.vertices = vertices;
        malla.triangles = triangulos.ToArray();
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        estrella.GetComponent<MeshFilter>().mesh = malla;
        Colorear(estrella, new Color32(255, 201, 66, 255));
    }


    // Ventana emergente con identidad visual Golosa AR.
    // Se crea en el Canvas existente: no requiere sprites ni scripts nuevos.
    private GameObject avisoEmergente;
    private TextMeshProUGUI avisoEmergenteTexto;
    private TextMeshProUGUI avisoEmergenteTitulo;
    private GameObject avisoIconoGolosa;
    private GameObject avisoIconoControl;
    private TextMeshProUGUI avisoEmergenteBotonTexto;
    private Image avisoEmergenteEncabezado;
    private Image avisoEmergenteBotonImagen;
    private RectTransform avisoEmergenteTarjeta;
    private CanvasGroup avisoEmergenteGrupo;
    private Coroutine animacionEmergente;
    private static Sprite redondeadoAviso;

    // Un solo sistema de avisos: Golosa para éxitos, control gamer para advertencias.
    // Reutiliza el Canvas de 01_Perfil y no cambia el funcionamiento de Individual.
    /// <summary>
    /// Presenta un aviso de confirmación o advertencia usando el estilo del juego.
    /// </summary>
    /// <param name="texto">Mensaje que verá el jugador.</param>
    /// <param name="esConfirmacion">Indica si es una confirmación o advertencia.</param>
    private void Avisar(string texto, bool esConfirmacion = false)
    {
        if (string.IsNullOrEmpty(texto)) return;
        if (mensaje != null) mensaje.text = string.Empty;
        if (avisoEmergente == null) CrearAvisoEmergente();
        if (avisoEmergente == null) return;

        avisoEmergenteTitulo.text = esConfirmacion ? "¡AVATAR LISTO!" : "¡OJO, JUGADOR!";
        avisoEmergenteTexto.text = texto;
        avisoEmergenteBotonTexto.text = esConfirmacion ? "¡GENIAL!" : "ENTENDIDO";
        avisoEmergenteEncabezado.color = esConfirmacion
            ? new Color32(126, 39, 219, 255) : new Color32(83, 25, 166, 255);
        avisoIconoGolosa.SetActive(esConfirmacion);
        avisoIconoControl.SetActive(!esConfirmacion);
        avisoEmergente.SetActive(true);
        avisoEmergente.transform.SetAsLastSibling();
        if (animacionEmergente != null) StopCoroutine(animacionEmergente);
        animacionEmergente = StartCoroutine(AnimarEntradaAviso());
    }

    /// <summary>
    /// Anima la escala y la opacidad del aviso de forma independiente al tiempo de juego.
    /// </summary>
    private IEnumerator AnimarEntradaAviso()
    {
        if (avisoEmergenteTarjeta == null || avisoEmergenteGrupo == null) yield break;
        float tiempo = 0f;
        const float duracion = .26f;
        avisoEmergenteGrupo.alpha = 0f;
        avisoEmergenteTarjeta.localScale = Vector3.one * .82f;
        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracion);
            float rebote = 1f + 1.7f * Mathf.Pow(1f - t, 2f) * Mathf.Sin(t * Mathf.PI);
            avisoEmergenteTarjeta.localScale = Vector3.one * Mathf.Lerp(.82f, 1f, t) * rebote;
            avisoEmergenteGrupo.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
        avisoEmergenteTarjeta.localScale = Vector3.one;
        avisoEmergenteGrupo.alpha = 1f;
        animacionEmergente = null;
    }

    // Sprite procedural de esquinas redondeadas, compartido por todos los paneles.
    /// <summary>
    /// Crea y reutiliza un sprite procedural de esquinas redondeadas.
    /// </summary>
    private static Sprite SpriteRedondoAviso()
    {
        if (redondeadoAviso != null) return redondeadoAviso;
        const int tam = 96;
        const float radio = 23f;
        Texture2D tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        tex.name = "GolosaAR_RedondeadoAviso";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] pix = new Color32[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float px = Mathf.Max(0f, Mathf.Abs(x + .5f - tam / 2f) - (tam / 2f - radio));
                float py = Mathf.Max(0f, Mathf.Abs(y + .5f - tam / 2f) - (tam / 2f - radio));
                float alpha = Mathf.Clamp01(radio - Mathf.Sqrt(px * px + py * py) + .5f);
                pix[y * tam + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
        tex.SetPixels32(pix);
        tex.Apply();
        redondeadoAviso = Sprite.Create(tex, new Rect(0, 0, tam, tam),
            new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(28, 28, 28, 28));
        redondeadoAviso.name = "GolosaAR_PanelRedondeado";
        return redondeadoAviso;
    }

    /// <summary>
    /// Construye un panel UI reutilizable con sprite redondeado.
    /// </summary>
    private static Image PanelAviso(Transform padre, string nombre, Vector2 pos,
        Vector2 tam, Color color, bool interactivo = false)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        Image image = go.GetComponent<Image>();
        image.sprite = SpriteRedondoAviso();
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = interactivo;
        return image;
    }

    /// <summary>
    /// Añade un destello decorativo rotado en la ventana emergente.
    /// </summary>
    private static void RayoAviso(Transform padre, Vector2 pos, float rotacion, Color color)
    {
        Image rayo = PanelAviso(padre, "Brillo", pos, new Vector2(17, 65), color);
        rayo.rectTransform.localRotation = Quaternion.Euler(0, 0, rotacion);
    }

    // Tarjeta inspirada en la opción 4 aprobada, con insignia de golosa.
    // Opción 2 para advertencias, con mando de juego. Todo se dibuja con UI estándar,
    // sin PNG externos, sin prefab nuevo y sin scripts duplicados.
    /// <summary>
    /// Construye la ventana emergente y conecta el botón de cierre.
    /// </summary>
    private void CrearAvisoEmergente()
    {
        Canvas lienzo = GetComponentInParent<Canvas>();
        if (lienzo == null) { Debug.LogWarning("Golosa AR: falta Canvas en Perfil."); return; }
        avisoEmergente = new GameObject("AvisoEmergentePerfil", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        avisoEmergente.transform.SetParent(lienzo.transform, false);
        RectTransform raiz = (RectTransform)avisoEmergente.transform;
        raiz.anchorMin = Vector2.zero;
        raiz.anchorMax = Vector2.one;
        raiz.offsetMin = raiz.offsetMax = Vector2.zero;
        Image oscurecer = avisoEmergente.GetComponent<Image>();
        oscurecer.color = new Color(0.10f, 0.025f, 0.21f, 0.72f);
        oscurecer.raycastTarget = true;
        avisoEmergenteGrupo = avisoEmergente.GetComponent<CanvasGroup>();

        // Marco exterior: sombra profunda, borde brillante, relleno crema.
        PanelAviso(raiz, "Sombra3D", new Vector2(0, -33), new Vector2(906, 762),
            new Color32(44, 11, 102, 255));
        PanelAviso(raiz, "BordeMorado", new Vector2(0, -7), new Vector2(906, 754),
            new Color32(104, 25, 193, 255));
        PanelAviso(raiz, "BordeBrillante", new Vector2(0, 5), new Vector2(888, 734),
            new Color32(255, 206, 54, 255));
        Image tarjeta = PanelAviso(raiz, "TarjetaCrema", new Vector2(0, 13),
            new Vector2(862, 703), new Color32(255, 249, 253, 255));
        avisoEmergenteTarjeta = tarjeta.rectTransform;

        // Dos esquinas decorativas y una banda brillante, sin cubrir el texto.
        PanelAviso(avisoEmergenteTarjeta, "AcentoSuperior", new Vector2(0, 280),
            new Vector2(822, 112), new Color32(242, 218, 255, 255));
        PanelAviso(avisoEmergenteTarjeta, "AcentoInferior", new Vector2(0, -305),
            new Vector2(820, 10), new Color32(245, 210, 255, 255));
        avisoEmergenteEncabezado = PanelAviso(avisoEmergenteTarjeta, "BandaTitulo",
            new Vector2(0, 115), new Vector2(758, 116), new Color32(126, 39, 219, 255));
        PanelAviso(avisoEmergenteTarjeta, "BandaSombra", new Vector2(0, 52),
            new Vector2(720, 12), new Color32(255, 196, 47, 255));

        // Insignia de golosa: casillas 1, 2, 3 sobrepuestas, estilo juguete 3D.
        avisoIconoGolosa = new GameObject("InsigniaGolosa", typeof(RectTransform));
        avisoIconoGolosa.transform.SetParent(avisoEmergenteTarjeta, false);
        RectTransform golosa = (RectTransform)avisoIconoGolosa.transform;
        golosa.anchorMin = golosa.anchorMax = new Vector2(.5f, .5f);
        golosa.anchoredPosition = new Vector2(0, 245);
        golosa.sizeDelta = new Vector2(320, 260);
        CrearCasillaAviso(golosa, "Casilla1", "1", new Vector2(0, -45), -8,
            new Color32(115, 39, 215, 255));
        CrearCasillaAviso(golosa, "Casilla2", "2", new Vector2(-66, 40), 8,
            new Color32(255, 202, 48, 255));
        CrearCasillaAviso(golosa, "Casilla3", "3", new Vector2(66, 40), -9,
            new Color32(151, 80, 235, 255));

        // Insignia alternativa: mando sencillo para validaciones y advertencias.
        avisoIconoControl = new GameObject("InsigniaMando", typeof(RectTransform));
        avisoIconoControl.transform.SetParent(avisoEmergenteTarjeta, false);
        RectTransform mando = (RectTransform)avisoIconoControl.transform;
        mando.anchorMin = mando.anchorMax = new Vector2(.5f, .5f);
        mando.anchoredPosition = new Vector2(0, 247);
        mando.sizeDelta = new Vector2(310, 180);
        PanelAviso(mando, "SombraMando", new Vector2(0, -14), new Vector2(264, 148),
            new Color32(70, 24, 140, 255));
        PanelAviso(mando, "MandoBlanco", Vector2.zero, new Vector2(258, 146),
            new Color32(250, 244, 255, 255));
        PanelAviso(mando, "CruzVertical", new Vector2(-66, 0), new Vector2(19, 66),
            new Color32(106, 34, 197, 255));
        PanelAviso(mando, "CruzHorizontal", new Vector2(-66, 0), new Vector2(66, 19),
            new Color32(106, 34, 197, 255));
        PanelAviso(mando, "BotonA", new Vector2(61, 22), new Vector2(29, 29),
            new Color32(255, 185, 36, 255));
        PanelAviso(mando, "BotonB", new Vector2(96, -16), new Vector2(29, 29),
            new Color32(141, 48, 227, 255));

        // Decoraciones exteriores discretas, siempre detrás del contenido.
        RayoAviso(avisoEmergenteTarjeta, new Vector2(-345, 184), -32,
            new Color32(255, 197, 42, 255));
        RayoAviso(avisoEmergenteTarjeta, new Vector2(345, 184), 32,
            new Color32(255, 197, 42, 255));
        TextoDecorativoAviso(avisoEmergenteTarjeta, "EstrellaIzq", new Vector2(-332, -58));
        TextoDecorativoAviso(avisoEmergenteTarjeta, "EstrellaDer", new Vector2(332, -58));

        avisoEmergenteTitulo = CrearTextoAviso(avisoEmergenteTarjeta, "Titulo", "¡AVATAR LISTO!",
            new Vector2(0, 115), new Vector2(710, 105), 65, Color.white);
        avisoEmergenteTexto = CrearTextoAviso(avisoEmergenteTarjeta, "Mensaje", string.Empty,
            new Vector2(0, -64), new Vector2(725, 171), 41, new Color32(67, 27, 114, 255));
        avisoEmergenteTexto.fontStyle = FontStyles.Normal;
        avisoEmergenteTexto.fontSizeMin = 31;

        PanelAviso(avisoEmergenteTarjeta, "SombraBoton", new Vector2(0, -239),
            new Vector2(610, 124), new Color32(79, 19, 143, 255));
        avisoEmergenteBotonImagen = PanelAviso(avisoEmergenteTarjeta, "BotonAmarillo",
            new Vector2(0, -224), new Vector2(604, 119),
            new Color32(255, 194, 31, 255), true);
        Button boton = avisoEmergenteBotonImagen.gameObject.AddComponent<Button>();
        boton.targetGraphic = avisoEmergenteBotonImagen;
        boton.transition = Selectable.Transition.ColorTint;
        ColorBlock colores = boton.colors;
        colores.normalColor = Color.white;
        colores.highlightedColor = new Color32(255, 239, 176, 255);
        colores.pressedColor = new Color32(230, 166, 39, 255);
        boton.colors = colores;
        boton.onClick.AddListener(() =>
        {
            if (avisoEmergente != null) avisoEmergente.SetActive(false);
        });
        avisoEmergenteBotonTexto = CrearTextoAviso(avisoEmergenteBotonImagen.transform,
            "TextoBoton", "¡GENIAL!", Vector2.zero,
            new Vector2(545, 110), 57, new Color32(82, 21, 149, 255));
        avisoIconoControl.SetActive(false);
        avisoEmergente.SetActive(false);
    }

    /// <summary>
    /// Dibuja una pequeña casilla decorativa dentro de la insignia.
    /// </summary>
    private static void CrearCasillaAviso(Transform padre, string nombre, string numero,
        Vector2 posicion, float angulo, Color color)
    {
        Image sombra = PanelAviso(padre, nombre + "Sombra",
            posicion + new Vector2(0, -12), new Vector2(122, 116),
            new Color32(68, 14, 124, 255));
        sombra.rectTransform.localRotation = Quaternion.Euler(0, 0, angulo);
        Image casilla = PanelAviso(padre, nombre, posicion, new Vector2(122, 116), color);
        casilla.rectTransform.localRotation = Quaternion.Euler(0, 0, angulo);
        var texto = CrearTextoAviso(casilla.transform, "Numero", numero,
            Vector2.zero, new Vector2(105, 97), 81, Color.white);
        texto.fontSizeMin = 60;
    }

    /// <summary>
    /// Crea un destello UI sin depender de caracteres tipográficos.
    /// </summary>
    private static void TextoDecorativoAviso(Transform padre, string nombre, Vector2 posicion)
    {
        // Un destello simple construido con UI para evitar glifos faltantes en TMP.
        Image vertical = PanelAviso(padre, nombre + "V", posicion,
            new Vector2(14, 65), new Color32(255, 198, 45, 255));
        Image horizontal = PanelAviso(padre, nombre + "H", posicion,
            new Vector2(65, 14), new Color32(255, 198, 45, 255));
        vertical.rectTransform.localRotation = Quaternion.Euler(0, 0, 27);
        horizontal.rectTransform.localRotation = Quaternion.Euler(0, 0, 27);
    }

    /// <summary>
    /// Genera un elemento TextMeshProUGUI configurado para la ventana emergente.
    /// </summary>
    private static TextMeshProUGUI CrearTextoAviso(Transform padre, string nombre, string texto,
        Vector2 posicion, Vector2 tamano, int fuente, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        r.anchoredPosition = posicion;
        r.sizeDelta = tamano;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = fuente;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 28;
        tmp.fontSizeMax = fuente;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>
    /// Traduce la posición de la lista serializada del Perfil al ID que entiende
    /// GolosaCatalogoAvatares en 03_Juego. Orden verificado en Assets (3).zip,
    /// Assets/Scenes/01_Perfil.unity mediante las referencias de prefab (GUID).
    /// Si se reordena avataresDisponibles en el Inspector, actualizar esta función.
    /// </summary>
    private static string IdAvatarEnIndice(int indice)
    {
        switch (indice)
        {
            case 0: return "Pirate";
            case 1: return "AvatarNiño";
            case 2: return "Astronauta";
            case 3: return "Cachucha";
            case 4: return "hiNiña";
            case 5: return "hiOso";
            default: return string.Empty;
        }
    }

    /// <summary>
    /// Valida el perfil, guarda sus preferencias y abre la escena de juego.
    /// </summary>
    public void Continuar()
    {
        string nombre = campoNombre == null ? "" : campoNombre.text.Trim();
        if (nombre.Length < 2)
        {
            Avisar("Escribe un nombre de al menos 2 caracteres.");
            return;
        }
        PlayerPrefs.SetString(ClaveNombre, nombre);
        PlayerPrefs.SetString(ClaveCategoria, categoria);
        if (avataresDisponibles != null && avataresDisponibles.Length > 0)
        {
            if (!avatarConfirmado)
            {
                Avisar("Primero pulsa ¡QUIERO ESTE AVATAR!");
                return;
            }
            if (avataresDisponibles[indiceAvatar] == null)
            {
                Avisar("El avatar seleccionado no está disponible.");
                return;
            }
            // El catálogo en 03_Juego utiliza IDs estables; los objetos de preview
            // de 01_Perfil tienen nombres distintos (por ejemplo Golosa_Preview...).
            if (indiceAvatar < 0 || indiceAvatar >= 6)
            {
                Avisar("Revisa el orden de los avatares en PerfilUI.");
                return;
            }
            string id = IdAvatarEnIndice(indiceAvatar);
            PlayerPrefs.SetString("Golosa.AvatarId", id);
            Debug.Log($"Golosa AR Perfil: índice={indiceAvatar}, preview='{avataresDisponibles[indiceAvatar].name}', ID={id}");
        }
        else
        {
            PlayerPrefs.SetString("Golosa.AvatarId", "AvatarNinaGolosa");
        }
        GuardarOpciones();

        PlayerPrefs.SetString("Golosa.ModoJuego", "Individual");
        PlayerPrefs.SetString("Golosa.Modo", "Individual");
        PlayerPrefs.Save();
        if (Application.CanStreamedLevelBeLoaded("03_Juego"))
            SceneManager.LoadScene("03_Juego");
        else
            Avisar("No se encontró 03_Juego en Build Profiles.");
    }

    /// <summary>
    /// Navega a la pantalla de selección de modo de juego.
    /// </summary>
    public void Volver()
    {
        if (Application.CanStreamedLevelBeLoaded("02_ModoJuego"))
            SceneManager.LoadScene("02_ModoJuego");
        else
            Avisar("No se encontró 02_ModoJuego en Build Profiles.");
    }
}
