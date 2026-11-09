
using UnityEngine;
using UnityEngine.SceneManagement;

public class NavegacionInicio : MonoBehaviour
{
    public GameObject panelReglas;

    // Botón JUGAR
    public void IrAPerfil()
    {
        SceneManager.LoadScene("01_Perfil");
    }

    // Botón ¿CÓMO JUGAR?
    public void MostrarComoJugar()
    {
        if (panelReglas != null)
        {
            panelReglas.SetActive(true);
        }
    }

    // Botón CERRAR
    public void CerrarComoJugar()
    {
        if (panelReglas != null)
        {
            panelReglas.SetActive(false);
        }
    }
}
