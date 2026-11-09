#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Genera COPIAS de uso en juego. Nunca reescribe FBX, PNG ni materiales originales.
public static class GolosaRepararAvatares07
{
    private const string Salida = "Assets/Avatar3d/GolosaCorregidos07";
    private static readonly string[] Origenes = {
        "Assets/Avatar3d/1Niño/hi1.fbx",
        "Assets/Avatar3d/Oso/hiOso.fbx",
        "Assets/Avatar3d/Niña/Niña/hiNiña.fbx"
    };
    private static readonly string[] Nombres = {"Golosa_Nino_Visible07", "Golosa_Oso_Texturas07", "Golosa_Nina_Texturas07"};

    [MenuItem("Golosa AR/07 - Reparar niño, oso y niña")]
    public static void Reparar()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Golosa AR", "Sal del modo Play antes de reparar los modelos.", "Aceptar");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Avatar3d"))
        {
            EditorUtility.DisplayDialog("Golosa AR", "No se encontró Assets/Avatar3d. No se modificó nada.", "Aceptar");
            return;
        }
        if (!AssetDatabase.IsValidFolder(Salida)) AssetDatabase.CreateFolder("Assets/Avatar3d", "GolosaCorregidos07");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Golosa AR", "URP/Lit no está disponible. Revisa el paquete Universal Render Pipeline.", "Aceptar");
            return;
        }
        string informe = "GOLOSA AR - Resultado de reparación\n";
        int creados = 0;
        for (int i = 0; i < Origenes.Length; i++)
        {
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(Origenes[i]);
            if (original == null) { informe += "FALTA: " + Origenes[i] + "\n"; continue; }
            GameObject copia = (GameObject)PrefabUtility.InstantiatePrefab(original);
            if (copia == null) copia = UnityEngine.Object.Instantiate(original);
            copia.name = Nombres[i];
            try
            {
                foreach (Camera cam in copia.GetComponentsInChildren<Camera>(true)) cam.enabled = false;
                foreach (AudioListener listener in copia.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
                foreach (Animator a in copia.GetComponentsInChildren<Animator>(true)) a.applyRootMotion = false;
                Texture2D texturaPrincipal = null;
                Texture2D texturaCara = null;
                if (i == 0)
                {
                    texturaPrincipal = CargarTextura("Assets/Avatar3d/1Niño/textures/N00_000_00_Body_00_SKIN__Instance__Diffuse.png");
                    texturaCara = CargarTextura("Assets/Avatar3d/1Niño/textures/N00_000_00_FaceMouth_00_FACE__Instance__Diffuse.png");
                }
                else if (i == 1)
                {
                    // Los materiales originales de oso ya contienen el mapeo de UV y texturas.
                    texturaPrincipal = CargarTextura("Assets/Avatar3d/Oso/textures/Image_0.png");
                    texturaCara = CargarTextura("Assets/Avatar3d/Oso/textures/Image_1.png");
                }
                else texturaPrincipal = CargarTextura("Assets/Avatar3d/Niña/Niña/textures/yk.png");
                Renderer[] renderers = copia.GetComponentsInChildren<Renderer>(true);
                int mallas = 0;
                foreach (Renderer r in renderers)
                {
                    if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer)) continue;
                    if (r is SkinnedMeshRenderer skinned)
                    {
                        skinned.updateWhenOffscreen = true;
                        skinned.forceRenderingOff = false;
                        if (skinned.sharedMesh != null) mallas++;
                    }
                    else if (r is MeshRenderer) mallas++;
                    r.enabled = true;
                    Material[] actuales = r.sharedMaterials;
                    if (actuales == null || actuales.Length == 0) actuales = new Material[1];
                    Material[] nuevos = new Material[actuales.Length];
                    for (int m = 0; m < actuales.Length; m++)
                    {
                        string nombreMaterial = actuales[m] != null ? actuales[m].name.ToLowerInvariant() : "";
                        string nombreObjeto = r.name.ToLowerInvariant();
                        bool esCara = nombreMaterial.Contains("cara") || nombreMaterial.Contains("face") ||
                            nombreObjeto.Contains("face") || nombreObjeto.Contains("mouth") || nombreObjeto.Contains("cara");
                        Texture tex = esCara && texturaCara != null ? texturaCara : texturaPrincipal;
                        if (i == 1)
                        {
                            // Preferir exactamente los materiales originales del oso.
                            string rutaMat = esCara ? "Assets/Avatar3d/Oso/textures/cara.mat" : "Assets/Avatar3d/Oso/textures/Cuerpo.mat";
                            Material matOso = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
                            if (matOso != null && matOso.HasProperty("_BaseMap") && matOso.GetTexture("_BaseMap") != null)
                            { nuevos[m] = matOso; continue; }
                        }
                        if (actuales[m] != null)
                        {
                            Texture heredada = null;
                            if (actuales[m].HasProperty("_BaseMap")) heredada = actuales[m].GetTexture("_BaseMap");
                            if (heredada == null && actuales[m].HasProperty("_MainTex")) heredada = actuales[m].GetTexture("_MainTex");
                            if (heredada != null) tex = heredada;
                        }
                        Material creado = new Material(shader);
                        creado.name = Nombres[i] + "_" + r.name + "_" + m;
                        if (tex != null) creado.SetTexture("_BaseMap", tex);
                        creado.SetColor("_BaseColor", Color.white);
                        creado.SetFloat("_Smoothness", 0.15f);
                        string nombreArchivo = Sanear(creado.name);
                        string rutaSalida = Salida + "/" + nombreArchivo + ".mat";
                        // Evitar reemplazar archivos ya creados de otra reparación.
                        var existente = AssetDatabase.LoadAssetAtPath<Material>(rutaSalida);
                        if (existente == null) { AssetDatabase.CreateAsset(creado, rutaSalida); nuevos[m] = creado; }
                        else { existente.shader = shader; existente.SetColor("_BaseColor", Color.white); if (tex != null) existente.SetTexture("_BaseMap", tex); EditorUtility.SetDirty(existente); nuevos[m] = existente; UnityEngine.Object.DestroyImmediate(creado); }
                    }
                    r.sharedMaterials = nuevos;
                }
                string salidaPrefab = Salida + "/" + Nombres[i] + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(copia, salidaPrefab);
                informe += "CREADO: " + salidaPrefab + " (mallas visibles: " + mallas + ")\n";
                creados++;
            }
            catch (Exception ex) { informe += "ERROR " + Nombres[i] + ": " + ex.Message + "\n"; Debug.LogException(ex); }
            finally { UnityEngine.Object.DestroyImmediate(copia); }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        File.WriteAllText("Assets/Avatar3d/GolosaCorregidos07/RESULTADO.txt", informe);
        Debug.Log(informe);
        EditorUtility.DisplayDialog("Golosa AR", "Se generaron " + creados + " prefabs nuevos en Assets/Avatar3d/GolosaCorregidos07. Ahora asígnalos en Golosa Catalogo Avatares de XR Origin (niño, hi niña, hi oso). Mira RESULTADO.txt.", "Aceptar");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(Salida);
    }
    private static Texture2D CargarTextura(string ruta)
    {
        Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        if (t == null) Debug.LogWarning("Golosa AR: no se encontró textura " + ruta);
        return t;
    }
    private static string Sanear(string s)
    {
        foreach(char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace('/', '_').Replace(' ', '_');
    }
}
#endif
