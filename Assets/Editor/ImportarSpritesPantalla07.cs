#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ImportarSpritesPantalla07
{
    [MenuItem("Golosa AR/Pantalla 07/Configurar PNG como Sprites")]
    public static void Configurar()
    {
        string carpeta = "Assets/Art/Pantalla07/Resources/Pantalla07";
        string[] ids = AssetDatabase.FindAssets("t:Texture2D", new[] { carpeta });
        int total = 0;
        foreach (string id in ids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(id);
            if (!ruta.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
            TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null) continue;
            if (importador.textureType != TextureImporterType.Sprite || importador.spriteImportMode != SpriteImportMode.Single)
            {
                importador.textureType = TextureImporterType.Sprite;
                importador.spriteImportMode = SpriteImportMode.Single;
                importador.alphaIsTransparency = true;
                importador.SaveAndReimport();
            }
            total++;
        }
        AssetDatabase.Refresh();
        Debug.Log("Golosa AR: " + total + " sprites PNG de Pantalla 07 configurados.");
    }
}
#endif
