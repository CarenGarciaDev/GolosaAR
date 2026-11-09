#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
public static class GolosaTurnosImportarArte {
  [MenuItem("Golosa AR/Por turnos/Configurar imagenes PNG")]
  public static void Configurar() {
    string[] rutas=AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/GolosaTurnosUI"});
    int cambios=0;
    foreach(string id in rutas) {
      string ruta=AssetDatabase.GUIDToAssetPath(id);
      var ti=AssetImporter.GetAtPath(ruta) as TextureImporter;
      if(ti==null)continue;
      if(ti.textureType!=TextureImporterType.Sprite || ti.spriteImportMode!=SpriteImportMode.Single){
        ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;
        ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.SaveAndReimport();cambios++;
      }
    }
    AssetDatabase.Refresh();
    Debug.Log("Golosa AR: imagenes de Por turnos listas. Cambios: "+cambios);
  }
}
#endif
