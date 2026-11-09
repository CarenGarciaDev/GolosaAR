#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Crea un prefab para el juego copiando el avatar que YA funciona en 01_Perfil.
/// No altera FBX, materiales, prefabs originales ni escenas guardadas.
/// </summary>
public static class GolosaNinoDesdePerfil07
{
    const string Carpeta = "Assets/Avatar3d/GolosaCorregidos07";
    const string Destino = Carpeta + "/Golosa_Nino_DesdePerfil.prefab";

    [MenuItem("Golosa AR/07 - Crear niño visible desde Perfil")]
    public static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Golosa AR", "Detén Play antes de ejecutar esta operación.", "Aceptar");
            return;
        }

        // Cargar 01_Perfil temporalmente: NO se guarda ni se modifica.
        string escenaRuta = null;
        foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta).Equals("01_Perfil", StringComparison.OrdinalIgnoreCase))
            { escenaRuta = ruta; break; }
        }
        if (string.IsNullOrEmpty(escenaRuta))
        {
            EditorUtility.DisplayDialog("Golosa AR", "No encontré 01_Perfil.unity en Assets. Abre 01_Perfil y ejecútalo otra vez.", "Aceptar");
            return;
        }

        Scene escena = SceneManager.GetSceneByPath(escenaRuta);
        bool cargadaAqui = !escena.isLoaded;
        if (cargadaAqui) escena = EditorSceneManager.OpenScene(escenaRuta, OpenSceneMode.Additive);
        try
        {
            PerfilUI perfil = null;
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                perfil = raiz.GetComponentInChildren<PerfilUI>(true);
                if (perfil != null) break;
            }
            if (perfil == null || perfil.avataresDisponibles == null || perfil.avataresDisponibles.Length == 0)
            {
                Aviso("01_Perfil no contiene una lista de avatares en PerfilUI. No se cambió nada.");
                return;
            }

            // La selección del niño corresponde habitualmente a la primera tarjeta.
            // Validación: si el primero tiene un nombre inesperado, solicitar elegirlo.
            GameObject fuente = perfil.avataresDisponibles[0];
            if (fuente == null)
            {
                Aviso("El primer avatar de PerfilUI no está asignado. Revisa PerfilUI > Avatares Disponibles.");
                return;
            }
            string nombre = fuente.name.ToLowerInvariant();
            if (!(nombre.Contains("niño") || nombre.Contains("nino") || nombre.Contains("hi1") || nombre.Contains("boy") || nombre.Contains("avatar")))
            {
                if (!EditorUtility.DisplayDialog("Confirmar avatar", "El primer personaje en PerfilUI se llama '" + fuente.name + "'. ¿Es el NIÑO que se ve correctamente en Perfil?", "Sí, es el niño", "Cancelar")) return;
            }

            GameObject raizNueva = new GameObject("Golosa_Nino_DesdePerfil");
            try
            {
                GameObject cuerpo = UnityEngine.Object.Instantiate(fuente, raizNueva.transform, false);
                cuerpo.name = "Modelo_Nino_Perfil";
                cuerpo.SetActive(true);
                raizNueva.transform.position = Vector3.zero;
                raizNueva.transform.rotation = Quaternion.identity;
                raizNueva.transform.localScale = Vector3.one;
                cuerpo.transform.localPosition = Vector3.zero;

                foreach (Camera camara in raizNueva.GetComponentsInChildren<Camera>(true)) camara.enabled = false;
                foreach (AudioListener audio in raizNueva.GetComponentsInChildren<AudioListener>(true)) audio.enabled = false;
                foreach (Animator anim in raizNueva.GetComponentsInChildren<Animator>(true)) anim.applyRootMotion = false;

                int mallas = 0;
                Bounds bounds = new Bounds();
                bool hayBounds = false;
                foreach (Renderer r in raizNueva.GetComponentsInChildren<Renderer>(true))
                {
                    if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;
                    r.enabled = true;
                    if (r is SkinnedMeshRenderer piel) { piel.updateWhenOffscreen = true; piel.forceRenderingOff = false; }
                    if (r is MeshRenderer && r.GetComponent<MeshFilter>()?.sharedMesh == null) continue;
                    if (r is SkinnedMeshRenderer sk && sk.sharedMesh == null) continue;
                    mallas++;
                    if (!hayBounds) { bounds = r.bounds; hayBounds = true; }
                    else bounds.Encapsulate(r.bounds);
                }
                if (mallas == 0 || !hayBounds)
                {
                    Aviso("El avatar de Perfil no tiene mallas visibles. No se creó el prefab.");
                    return;
                }
                // Normaliza el modelo al origen del prefab, apoyando los pies en Y=0.
                // No altera escalas ni materiales del modelo copiado.
                Vector3 centro = bounds.center;
                cuerpo.transform.position -= new Vector3(centro.x, bounds.min.y, centro.z);

                if (!AssetDatabase.IsValidFolder("Assets/Avatar3d"))
                { Aviso("Falta Assets/Avatar3d."); return; }
                if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/Avatar3d", "GolosaCorregidos07");
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Destino) != null)
                {
                    if (!EditorUtility.DisplayDialog("Golosa AR", "Ya existe Golosa_Nino_DesdePerfil.prefab. ¿Quieres actualizar esta copia?", "Actualizar", "Cancelar")) return;
                }
                GameObject creado = PrefabUtility.SaveAsPrefabAsset(raizNueva, Destino);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = creado;
                EditorGUIUtility.PingObject(creado);
                Debug.Log("Golosa AR: prefab creado desde el avatar visible de PerfilUI: " + Destino + "; mallas=" + mallas + "; fuente=" + fuente.name);
                Aviso("Prefabricado creado: " + Destino + "\n\nAhora en 03_Juego > XR Origin > Golosa Catalogo Avatares asigna este archivo en AVATAR NIÑO. Guarda la escena. No uses hi1.fbx directamente.");
            }
            finally { UnityEngine.Object.DestroyImmediate(raizNueva); }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Aviso("No pude crear el prefab. Revisa el error en Console: " + ex.Message);
        }
        finally
        {
            if (cargadaAqui && escena.isLoaded) EditorSceneManager.CloseScene(escena, true);
        }
    }

    private static void Aviso(string mensaje) => EditorUtility.DisplayDialog("Golosa AR", mensaje, "Aceptar");
}
#endif
