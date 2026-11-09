using UnityEngine;

// Añadir este componente a un objeto EXISTENTE en 03_Juego (por ejemplo, el controlador AR).
// Asignar aquí prefabs/modelos del Project, NO objetos de la escena 01_Perfil.
public class GolosaCatalogoAvatares : MonoBehaviour
{
    [Header("Arrastra los prefabs o modelos originales desde Project")]
    public GameObject avatarNino;
    public GameObject astronauta;
    public GameObject cachucha;
    public GameObject pirate;
    public GameObject hiNina;
    public GameObject hiOso;

    [Header("Tamano del personaje en la golosa AR")]
    [Tooltip("Altura aproximada del personaje en metros. Recomendada: 0.18 a 0.22 m.")]
    [Range(0.12f, 0.30f)]
    public float alturaAvatarMetros = 0.20f;

    public GameObject Obtener(string id)
    {
        switch (id)
        {
            case "AvatarNiño": return avatarNino;
            case "Astronauta": return astronauta;
            case "Cachucha": return cachucha;
            case "Pirate": return pirate;
            case "hiNiña": return hiNina;
            case "hiOso": return hiOso;
            default:
                Debug.LogWarning("Avatar no reconocido: '" + id + "'. Revisa Golosa.AvatarId.");
                return null;
        }
    }
}
