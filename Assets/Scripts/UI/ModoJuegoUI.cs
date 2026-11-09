using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Control existente de 02_ModoJuego: solamente Individual y Por turnos.
// La escena conserva el mismo componente ModoJuegoUI y el mismo flujo de juego.
/// <summary>
/// Gestiona la selección de juego individual o por turnos y la navegación desde esa pantalla.
/// </summary>
public class ModoJuegoUI : MonoBehaviour
{
    [Header("Saludo y avisos")]
    public TextMeshProUGUI saludo;
    public TextMeshProUGUI mensaje;

    [Header("Arte interactivo de las dos opciones")]
    public Image fondoModo;
    public Sprite imagenIndividual;
    public Sprite imagenPorTurnos;

    [Header("Ayuda")]
    public GameObject panelAyuda;

    // Se conservan estos campos antiguos para que la escena previa no pierda
    // referencias serializadas antes de volver a generar 02_ModoJuego.
    [HideInInspector] public Image indicadorIndividual;
    [HideInInspector] public Image indicadorEquipos;
    [HideInInspector] public Image indicadorMultijugador;
    [HideInInspector] public Sprite checkVerde;
    [HideInInspector] public Sprite flechaVerde;
    [HideInInspector] public Sprite checkMorado;
    [HideInInspector] public Sprite flechaMorada;
    [HideInInspector] public Sprite checkRosa;
    [HideInInspector] public Sprite flechaRosa;

    private string modo = "Individual";

    /// <summary>
    /// Inicializa la selección de modo y oculta el panel de ayuda.
    /// </summary>
    private void Start()
    {
        CargarModo();
        if (panelAyuda != null) panelAyuda.SetActive(false);
    }

    /// <summary>
    /// Recarga el modo seleccionado al habilitarse esta interfaz.
    /// </summary>
    private void OnEnable() { CargarModo(); }

    /// <summary>
    /// Lee el modo previamente guardado y actualiza la interfaz.
    /// </summary>
    private void CargarModo()
    {
        modo = PlayerPrefs.GetString("Golosa.ModoJuego", "Individual");
        if (modo != "Por turnos") modo = "Individual";
        if (saludo != null) saludo.text = "¿Cómo quieres jugar hoy?";
        ActualizarVista();
    }

    /// <summary>
    /// Selecciona el modo individual sin cambiar todavía de escena.
    /// </summary>
    public void SeleccionarIndividual()
    {
        modo = "Individual";
        ActualizarVista();
    }

    // Conservamos el nombre del método usado por la escena antigua.
    /// <summary>
    /// Selecciona el modo por turnos conservando el nombre del evento anterior.
    /// </summary>
    public void SeleccionarEquipos()
    {
        modo = "Por turnos";
        ActualizarVista();
    }

    /// <summary>
    /// Actualiza la imagen representativa de la modalidad seleccionada.
    /// </summary>
    private void ActualizarVista()
    {
        if (fondoModo != null)
            fondoModo.sprite = modo == "Por turnos" ? imagenPorTurnos : imagenIndividual;
        if (mensaje != null) mensaje.text = "";
    }

    /// <summary>
    /// Muestra el panel contextual de ayuda, si está asignado.
    /// </summary>
    public void MostrarAyuda()
    {
        if (panelAyuda != null) panelAyuda.SetActive(true);
    }

    /// <summary>
    /// Oculta el panel contextual de ayuda, si está asignado.
    /// </summary>
    public void CerrarAyuda()
    {
        if (panelAyuda != null) panelAyuda.SetActive(false);
    }

    /// <summary>
    /// Guarda la modalidad y abre la configuración de turnos o el perfil individual.
    /// </summary>
    public void Continuar()
    {
        PlayerPrefs.SetString("Golosa.ModoJuego", modo);
        PlayerPrefs.SetString("Golosa.Modo", modo);
        PlayerPrefs.Save();

        if (modo == "Por turnos")
        {
            GolosaConfiguracionTurnosUI.Abrir();
            return;
        }

        GolosaTurnosDatos.Cancelar();
        if (Application.CanStreamedLevelBeLoaded("01_Perfil"))
            SceneManager.LoadScene("01_Perfil");
        else if (mensaje != null)
            mensaje.text = "Agrega 01_Perfil a Build Profiles.";
    }

    /// <summary>
    /// Regresa a la escena de inicio, si está disponible para cargar.
    /// </summary>
    public void Volver()
    {
        if (Application.CanStreamedLevelBeLoaded("00_Inicio"))
            SceneManager.LoadScene("00_Inicio");
        else if (mensaje != null)
            mensaje.text = "Agrega 00_Inicio a Build Profiles.";
    }
}
