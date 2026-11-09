using UnityEngine;
using UnityEngine.SceneManagement;

// Componente para el boton JUGAR de 00_Inicio: envia al selector de modo.
public class GolosaInicioAModos : MonoBehaviour
{
    public void AbrirModoJuego()
    {
        if (Application.CanStreamedLevelBeLoaded("02_ModoJuego"))
            SceneManager.LoadScene("02_ModoJuego");
        else
            Debug.LogError("Agrega 02_ModoJuego a Build Profiles > Scene List.");
    }
}
