// IMPORTANTE: pega TODO este contenido en el archivo EXISTENTE
// Assets/Editor/GeneradorGolosaPrepararRA.cs (no crees un segundo archivo .cs).
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

public static class GeneradorGolosaPrepararRA
{
    private const string Carpeta = "Assets/Art/PrepararRA/";
    private static readonly Color Morado = new Color32(72, 18, 141, 255);
    private static readonly Color Blanco = Color.white;

    [MenuItem("Golosa AR/03 - Vincular imagenes del juego")]
    public static void VincularImagenesJuego()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Golosa AR", "Sal de Play antes de vincular imagenes.", "Entendido");
            return;
        }
        if (SceneManager.GetActiveScene().name != "03_Juego")
        {
            EditorUtility.DisplayDialog("Golosa AR", "Abre primero la escena 03_Juego.", "Entendido");
            return;
        }
        PrepararRAUI ui = Object.FindAnyObjectByType<PrepararRAUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Golosa AR", "No encontre PrepararRAUI en 03_Juego. No se modifico la escena.", "Entendido");
            return;
        }
        string[] nombres =
        {
            "BarraMarcador", "PanelInferior", "TarjetaMarcador", "BotonComenzar",
            "BotonLanzar", "BotonSaltar", "BotonReiniciar", "Icono_play",
            "Icono_dice", "Icono_foot", "Icono_retry", "Icono_back"
        };
        Sprite[] imagenes = new Sprite[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
        {
            string ruta = "Assets/Art/JuegoUI/" + nombres[i] + ".png";
            imagenes[i] = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
            if (imagenes[i] == null)
            {
                EditorUtility.DisplayDialog("Golosa AR", "No se encontro un Sprite en " + ruta +
                    ". Selecciona el PNG en Unity y pon Texture Type = Sprite (2D and UI).", "Entendido");
                return;
            }
        }
        Undo.RecordObject(ui, "Vincular imagenes de Golosa AR");
        ui.spritesJuego = imagenes;
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Golosa AR: vinculados 12 sprites desde Assets/Art/JuegoUI; sin mover archivos.");
        EditorUtility.DisplayDialog("Golosa AR", "Listo: 12 imagenes vinculadas desde Assets/Art/JuegoUI. No se movio ningun PNG.", "Aceptar");
    }

    [MenuItem("Golosa AR/03 - Actualizar SOLO diseno de escaneo")]
    public static void Generar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Golosa AR", "Deten Play antes de actualizar.", "Aceptar");
            return;
        }
        if (SceneManager.GetActiveScene().name != "03_Juego")
        {
            EditorUtility.DisplayDialog("Golosa AR", "Abre primero Assets/Scenes/03_Juego.unity y guarda una copia de seguridad.", "Aceptar");
            return;
        }
        PrepararRAUI controlador = Object.FindAnyObjectByType<PrepararRAUI>(FindObjectsInactive.Include);
        if (controlador == null)
        {
            EditorUtility.DisplayDialog("Golosa AR", "No encontre PrepararRAUI en esta escena. No se modifico nada.", "Aceptar");
            return;
        }
        Transform root = controlador.transform;
        Transform hudExistente = root.Find("HUD_EscaneoReal");
        if (hudExistente == null)
        {
            EditorUtility.DisplayDialog("Golosa AR", "No encontre HUD_EscaneoReal bajo PrepararRAGenerado. No se modifico nada.", "Aceptar");
            return;
        }

        // Comprueba ANTES de cambiar la escena que los sprites realmente estan importados.
        string[] recursos = { "CabeceraEscaneo", "PanelInstrucciones", "PanelEstado", "BotonVolver", "Icono_phone", "Icono_scan" };
        foreach (string recurso in recursos)
        {
            if (Cargar(recurso) == null)
            {
                EditorUtility.DisplayDialog("Golosa AR", "Falta el sprite: " + Carpeta + recurso + ".png. Selecciona ese PNG en Unity y pon Texture Type = Sprite (2D and UI).", "Aceptar");
                return;
            }
        }
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Redisenar HUD escaneo Golosa AR");
        bool visible = hudExistente.gameObject.activeSelf;
        // SOLO se reconstruyen los elementos hijos del HUD de escaneo.
        for (int i = hudExistente.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(hudExistente.GetChild(i).gameObject);

        RectTransform hud = hudExistente as RectTransform;
        if (hud == null) { Debug.LogError("HUD_EscaneoReal debe ser RectTransform."); return; }
        hud.anchorMin = Vector2.zero;
        hud.anchorMax = Vector2.one;
        hud.offsetMin = Vector2.zero;
        hud.offsetMax = Vector2.zero;

        // Encabezado: compacto, dentro de la zona superior de la pantalla.
        Image encabezado = Imagen(hud, "CabeceraEscaneo", "CabeceraEscaneo", 0f, -190f, 980f, 275f, true);
        encabezado.raycastTarget = false;
        // La cabecera PNG es decorativa; el texto visible se dibuja con TMP para buena nitidez.
        Texto(encabezado.rectTransform, "Titulo", "¡PREPARA TU GOLOSA!", 54, Blanco, 0, 48, 900, 92, true);
        Texto(encabezado.rectTransform, "Subtitulo", "Busca un lugar amplio para jugar", 30, Blanco, 0, -55, 880, 70, false);

        // Visor AR central completamente transparente; solo cuatro esquinas.
        Color aqua = new Color32(43, 233, 235, 255);
        float centroY = -1060f;
        for (int c = 0; c < 4; c++)
        {
            float x = (c % 2 == 0 ? -315f : 315f);
            float y = centroY + (c < 2 ? 310f : -310f);
            // Barras estrechas para evitar tapar la camara.
            Cuadro(hud, "ReticulaH" + c, x + (x < 0 ? 45f : -45f), y, 94, 14, aqua);
            Cuadro(hud, "ReticulaV" + c, x, y + (c < 2 ? -42f : 42f), 14, 94, aqua);
        }
        Texto(hud, "Centro", "+", 82, Blanco, 0, centroY, 130, 130, true);

        // Instruccion muy por encima del boton; ningun texto se superpone.
        RectTransform tarjeta = Imagen(hud, "PanelInstrucciones", "PanelInstrucciones", 0, 315, 950, 230, false).rectTransform;
        Imagen(tarjeta, "IconoTelefono", "Icono_phone", -368, 0, 105, 105, false);
        Texto(tarjeta, "TextoAyuda", "Mueve lentamente tu celular\nApunta a un suelo plano y bien iluminado", 34, Morado, 63, 0, 725, 150, true);

        // Estado dinamico: el script PrepararRAUI actualiza exactamente esta referencia.
        RectTransform estadoPanel = Imagen(hud, "PanelEstado", "PanelEstado", 0, 540, 945, 122, false).rectTransform;
        TextMeshProUGUI estado = Texto(estadoPanel, "TextoEstado", "Buscando una superficie horizontal...", 30, Morado, 0, 0, 880, 94, true);
        controlador.textoEstado = estado;

        // Boton real: el texto es hijo del sprite y no se dibuja sobre la imagen de camara.
        Image imagenBoton = Imagen(hud, "BotonVolver", "BotonVolver", 0, 95, 770, 138, false);
        imagenBoton.raycastTarget = true;
        Button volver = imagenBoton.gameObject.AddComponent<Button>();
        volver.targetGraphic = imagenBoton;
        volver.transition = Selectable.Transition.ColorTint;
        Texto(imagenBoton.rectTransform, "EtiquetaVolver", "←  VOLVER A MODOS", 40, Blanco, 0, 0, 710, 105, true);
        UnityEventTools.AddPersistentListener(volver.onClick, controlador.Volver);

        controlador.hudEscaneoReal = hud.gameObject;
        hud.gameObject.SetActive(visible);
        EditorUtility.SetDirty(controlador);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Golosa AR: HUD de escaneo actualizado. No se alteraron XR Origin, AR Session ni las referencias de camara.");
        EditorUtility.DisplayDialog("Golosa AR", "Diseño de escaneo actualizado. Guarda la escena y prueba la vista Game. Para ver el cambio en el celular, genera e instala una nueva APK.", "Listo");
    }
    private static Sprite Cargar(string nombre)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(Carpeta + nombre + ".png");
    }

    private static RectTransform Rect(Transform padre, string nombre, float x, float y, float w, float h, bool arriba)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var r = go.GetComponent<RectTransform>();
        r.SetParent(padre, false);
        r.anchorMin = r.anchorMax = arriba ? new Vector2(.5f, 1f) : new Vector2(.5f, 0f);
        r.pivot = new Vector2(.5f, .5f);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
        return r;
    }

    private static Image Imagen(Transform padre, string nombre, string sprite, float x, float y, float w, float h, bool arriba)
    {
        RectTransform r = Rect(padre, nombre, x, y, w, h, arriba);
        Image im = r.gameObject.AddComponent<Image>();
        im.sprite = Cargar(sprite);
        im.preserveAspect = false;
        im.raycastTarget = false;
        return im;
    }
    private static void Cuadro(Transform padre, string nombre, float x, float y, float w, float h, Color color)
    {
        RectTransform r = Rect(padre, nombre, x, y, w, h, true);
        Image im = r.gameObject.AddComponent<Image>();
        im.color = color;
        im.raycastTarget = false;
    }

    private static TextMeshProUGUI Texto(Transform padre, string nombre, string valor, float tam, Color color, float x, float y, float w, float h, bool negrita)
    {
        // Los textos dentro de un panel se anclan al centro del propio panel.
        RectTransform r = Rect(padre, nombre, x, y, w, h, true);
        r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = valor;
        t.color = color;
        t.fontSize = tam;
        t.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
        t.alignment = TextAlignmentOptions.Center;
        t.enableAutoSizing = true;
        t.fontSizeMin = 20;
        t.fontSizeMax = tam;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }
}



