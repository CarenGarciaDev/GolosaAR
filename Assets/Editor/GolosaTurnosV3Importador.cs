using UnityEditor;
using UnityEngine;
public static class GolosaTurnosV3Importador
{
    [MenuItem("Golosa AR/Por turnos/Configurar sprites V3")]
    public static void Importar()
    {
        string path = "Assets/Resources/GolosaTurnosUI";
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D", new [] {path}))
        {
            string p=AssetDatabase.GUIDToAssetPath(guid);
            var importer=AssetImporter.GetAtPath(p) as TextureImporter;
            if(importer==null)continue;
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;
            importer.SaveAndReimport();
        }
        AssetDatabase.Refresh();
        Debug.Log("Golosa AR: sprites Por turnos V3 configurados.");
    }
}
