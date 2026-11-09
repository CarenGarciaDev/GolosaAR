#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public static class GolosaTurnosTitulo3DImportar
{
    [MenuItem("Golosa AR/Por turnos/Importar titulo 3D real")]
    public static void Importar()
    {
        string path = "Assets/Resources/GolosaTurnosUI/Titulo3DReal.png";
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogError("No se encontro: " + path); return; }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        Debug.Log("Golosa AR: titulo 3D importado correctamente.");
    }
}
#endif
