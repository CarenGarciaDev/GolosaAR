using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

// Golosa AR: generador UNICO de la pantalla de inicio (Figma, Unity 6).
// Se ejecuta desde el menú Golosa AR > Actualizar inicio (diseño referencia).
// Solo modifica el objeto "InicioGenerado" de la escena 00_Inicio.
public static class GeneradorGolosaInicio
{
    private static readonly Color Morado = new Color32(65, 22, 105, 255);
    private static readonly Color Violeta = new Color32(128, 42, 224, 255);
    private static readonly Color TextoOscuro = new Color32(45, 51, 77, 255);

    [MenuItem("Golosa AR/Actualizar inicio (diseño referencia)")]
    public static void Generar()
    {
        if (SceneManager.GetActiveScene().name != "00_Inicio")
        {
            EditorUtility.DisplayDialog("Golosa AR", "Abre Assets/Scenes/00_Inicio.unity antes de generar la pantalla.", "Entendido");
            return;
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Golosa AR", "No encontré el Canvas de 00_Inicio.", "Entendido");
            return;
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2340);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventos = new GameObject("EventSystem", typeof(EventSystem));
            System.Type tipo = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (tipo != null) eventos.AddComponent(tipo);
            else eventos.AddComponent<StandaloneInputModule>();
        }

        // Al regenerar, no crea duplicados ni altera las otras escenas.
        Transform viejo = canvas.transform.Find("InicioGenerado");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);

        RectTransform raiz = Rect(canvas.transform, "InicioGenerado", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Estirar(raiz);
        raiz.SetAsLastSibling();

        // Fondo: cielo y parque desenfocado, a pantalla completa.
        Sprite parque = SpriteEnProyecto("Assets/Art/Backgrounds/FondoParqueGolosa.jpg");
        if (parque != null)
        {
            Image fondo = Imagen(raiz, "FondoParque", parque, Color.white, Vector2.zero, Vector2.zero);
            Estirar(fondo.rectTransform);
            fondo.preserveAspect = false;
        }
        else
        {
            Image fallback = Imagen(raiz, "FondoAzulProvisional", null, new Color32(94, 165, 229, 255), Vector2.zero, Vector2.zero);
            Estirar(fallback.rectTransform);
        }

        // Personaje PNG decorativo de la portada (el avatar jugable 3D será independiente).
        Sprite personaje = SpriteEnProyecto("Assets/Art/Backgrounds/inicio.png");
        if (personaje != null)
        {
            Image avatar = Imagen(raiz, "PersonajeDeEspaldas", personaje, Color.white, new Vector2(0, -685), new Vector2(825, 1100));
            avatar.preserveAspect = true;
        }

        // Etiqueta semitransparente arriba.
        Image capsula = Imagen(raiz, "EtiquetaSuperior", SpriteEnProyecto("Assets/Art/UI/GolosaRedondeado.png"),
            new Color32(67, 136, 201, 122), new Vector2(0, -290), new Vector2(555, 86), new Vector2(0.5f, 1f));
        capsula.type = capsula.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        Outline contornoCapsula = capsula.gameObject.AddComponent<Outline>();
        contornoCapsula.effectColor = new Color32(255, 255, 255, 110);
        contornoCapsula.effectDistance = new Vector2(1.3f, -1.3f);
        contornoCapsula.useGraphicAlpha = false;
        TextMeshProUGUI textoEtiqueta = Texto(capsula.transform, "EtiquetaTexto", "TU PRÓXIMA AVENTURA", 30, Color.white,
            Vector2.zero, new Vector2(500, 66), Vector2.one * 0.5f, true, 2.2f);
        Shadow sombraEtiqueta = textoEtiqueta.gameObject.AddComponent<Shadow>();
        sombraEtiqueta.effectColor = new Color32(22, 55, 110, 205);
        sombraEtiqueta.effectDistance = new Vector2(1.5f, -2f);
        Sprite estrellaBlanca = SpriteEnProyecto("Assets/Art/UI/EstrellaBlanca.png");
        Sprite estrellaDorada = SpriteEnProyecto("Assets/Art/UI/EstrellaDorada.png");
        if (estrellaBlanca != null)
        {
            Imagen(capsula.transform, "BrilloEtiqueta", estrellaBlanca, Color.white, new Vector2(-224, 0), new Vector2(29, 29));
            Imagen(raiz, "BrilloIzquierdo", estrellaBlanca, new Color32(255, 255, 255, 210),
                new Vector2(-405, -853), new Vector2(76, 76), new Vector2(0.5f, 1));
        }
        if (estrellaDorada != null)
            Imagen(raiz, "BrilloDerecho", estrellaDorada, Color.white,
                new Vector2(422, -708), new Vector2(74, 74), new Vector2(0.5f, 1));

        // Logotipos rasterizados con letra redondeada, contorno grueso y sombra.
        // Esto se parece más al diseño que el TMP sans-serif genérico anterior.
        Sprite logoGolosa = SpriteEnProyecto("Assets/Art/UI/LogoGolosaReferencia.png");
        Sprite logoAR = SpriteEnProyecto("Assets/Art/UI/LogoARReferencia.png");
        if (logoGolosa != null)
            Imagen(raiz, "LogoGolosaReferencia", logoGolosa, Color.white,
                new Vector2(0, -470), new Vector2(720, 230), new Vector2(0.5f, 1f)).preserveAspect = true;
        else
            Texto(raiz, "LogoGolosa", "GOLOSA", 162, Color.white,
                new Vector2(0, -480), new Vector2(930, 220), new Vector2(0.5f, 1f));
        if (logoAR != null)
            Imagen(raiz, "LogoARReferencia", logoAR, Color.white,
                new Vector2(0, -715), new Vector2(360, 225), new Vector2(0.5f, 1f)).preserveAspect = true;
        else
            Texto(raiz, "LogoAR", "AR", 165, new Color32(255, 184, 65, 255),
                new Vector2(0, -710), new Vector2(480, 200), new Vector2(0.5f, 1f));

        TextMeshProUGUI eslogan = Texto(raiz, "Eslogan", "Juega, aprende y muévete\nen el mundo real",
            44, TextoOscuro, new Vector2(0, -945), new Vector2(920, 136), new Vector2(0.5f, 1f), true);
        eslogan.lineSpacing = 7;

        TextMeshProUGUI frase = Texto(raiz, "FraseInferior", "LA DIVERSIÓN COBRA VIDA", 31,
            Color.white, new Vector2(0, 545), new Vector2(940, 65), new Vector2(0.5f, 0f), true, 1.2f);
        Shadow sombraFrase = frase.gameObject.AddComponent<Shadow>();
        sombraFrase.effectColor = new Color32(42, 39, 57, 180);
        sombraFrase.effectDistance = new Vector2(1.5f, -2.5f);

        // Botones fijados ABAJO: así no se montan sobre el título aunque cambie la altura.
        Button jugar = Boton(raiz, "BtnJugar", "JUGAR", new Vector2(0, 405),
            new Vector2(982, 164), Violeta, Color.white, 52, new Vector2(0.5f, 0f));
        Image play = Imagen(jugar.transform, "IconoPlay", SpriteEnProyecto("Assets/Art/UI/IconoJugarBlanco.png"),
            Color.white, new Vector2(-135, 0), new Vector2(52, 52));
        play.preserveAspect = true;

        Button reglas = Boton(raiz, "BtnComoJugar", "¿CÓMO JUGAR?", new Vector2(0, 207),
            new Vector2(982, 143), new Color32(247, 239, 255, 255), Morado, 40, new Vector2(0.5f, 0f));
        Image fondoAyuda = Imagen(reglas.transform, "IconoAyudaFondo", SpriteEnProyecto("Assets/Art/UI/GolosaCirculo.png"),
            Morado, new Vector2(-200, 0), new Vector2(43, 43));
        fondoAyuda.type = fondoAyuda.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        Texto(fondoAyuda.transform, "SimboloAyuda", "?", 30, Color.white,
            Vector2.zero, new Vector2(40, 40));

        Texto(raiz, "PiePagina", "Un salto más allá de la pantalla.", 26, Color.white,
            new Vector2(0, 95), new Vector2(920, 52), new Vector2(0.5f, 0f), true);

        // Panel de reglas: se abre con ¿CÓMO JUGAR? y se cierra con X.
        RectTransform panel = Rect(raiz, "PanelComoJugar", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Estirar(panel);
        Image velo = panel.gameObject.AddComponent<Image>();
        velo.color = new Color32(21, 15, 44, 189);
        velo.raycastTarget = true;
        Image tarjeta = Imagen(panel, "TarjetaReglas", SpriteEnProyecto("Assets/Art/UI/GolosaRedondeado.png"),
            new Color32(250, 246, 255, 255), Vector2.zero, new Vector2(930, 850));
        tarjeta.type = tarjeta.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        Texto(tarjeta.transform, "Titulo", "¿CÓMO JUGAR?", 73, Morado,
            new Vector2(0, 280), new Vector2(840, 115));
        TextMeshProUGUI instrucciones = Texto(tarjeta.transform, "Instrucciones",
            "1. Crea tu perfil y elige tu categoría.\n\n" +
            "2. Selecciona el modo de juego.\n\n" +
            "3. Sigue las instrucciones de la golosa.\n\n" +
            "4. ¡Salta, aprende y diviértete!",
            37, TextoOscuro, new Vector2(0, -8), new Vector2(810, 475), Vector2.one * 0.5f, false);
        instrucciones.alignment = TextAlignmentOptions.MidlineLeft;
        Button cerrar = Boton(tarjeta.transform, "BtnCerrarReglas", "CERRAR",
            new Vector2(0, -320), new Vector2(630, 115), Violeta, Color.white, 44, Vector2.one * 0.5f);

        NavegacionInicio navegacion = raiz.gameObject.AddComponent<NavegacionInicio>();
        navegacion.panelReglas = panel.gameObject;
        UnityEventTools.AddPersistentListener(jugar.onClick, navegacion.IrAPerfil);
        UnityEventTools.AddPersistentListener(reglas.onClick, navegacion.MostrarComoJugar);
        UnityEventTools.AddPersistentListener(cerrar.onClick, navegacion.CerrarComoJugar);
        panel.gameObject.SetActive(false);

        // Unity/Android en vertical.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        EditorUtility.SetDirty(navegacion);
        EditorUtility.SetDirty(jugar);
        EditorUtility.SetDirty(reglas);
        EditorUtility.SetDirty(cerrar);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Golosa AR: pantalla 00_Inicio regenerada con ajustes de Figma.");
        EditorUtility.DisplayDialog("Golosa AR", "Pantalla de inicio actualizada. Prueba JUGAR y ¿CÓMO JUGAR? en Game.", "¡Listo!");
    }

    private static RectTransform Rect(Transform padre, string nombre, Vector2 posicion, Vector2 tamano,
        Vector2 ancla)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;
        return rt;
    }

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Sprite SpriteEnProyecto(string ruta)
    {
        TextureImporter importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importer == null) return null;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static Image Imagen(Transform padre, string nombre, Sprite sprite, Color color,
        Vector2 posicion, Vector2 tamano, Vector2? ancla = null)
    {
        RectTransform rt = Rect(padre, nombre, posicion, tamano, ancla ?? Vector2.one * 0.5f);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI Texto(Transform padre, string nombre, string contenido,
        float fuente, Color color, Vector2 posicion, Vector2 tamano,
        Vector2? ancla = null, bool negrita = true, float espaciado = 0)
    {
        RectTransform rt = Rect(padre, nombre, posicion, tamano, ancla ?? Vector2.one * 0.5f);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = contenido;
        tmp.color = color;
        tmp.fontSize = fuente;
        tmp.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 19;
        tmp.fontSizeMax = fuente;
        tmp.characterSpacing = espaciado;
        tmp.raycastTarget = false;
        TMP_FontAsset roboto = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset");
        if (roboto != null) tmp.font = roboto;
        return tmp;
    }

    private static Button Boton(Transform padre, string nombre, string titulo, Vector2 posicion,
        Vector2 tamano, Color fondo, Color colorTexto, float fuente, Vector2? ancla = null)
    {
        Sprite bordes = SpriteEnProyecto("Assets/Art/UI/GolosaRedondeado.png");
        Image img = Imagen(padre, nombre, bordes, fondo, posicion, tamano, ancla);
        img.type = bordes != null ? Image.Type.Sliced : Image.Type.Simple;
        img.raycastTarget = true;
        Shadow sombra = img.gameObject.AddComponent<Shadow>();
        sombra.effectColor = new Color32(39, 25, 59, 105);
        sombra.effectDistance = new Vector2(0, -8);
        Outline brillo = img.gameObject.AddComponent<Outline>();
        brillo.effectColor = new Color32(255, 255, 255, 170);
        brillo.effectDistance = new Vector2(2f, -2f);
        Button boton = img.gameObject.AddComponent<Button>();
        boton.targetGraphic = img;
        ColorBlock cb = boton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color32(245, 237, 255, 255);
        cb.pressedColor = new Color32(218, 192, 250, 255);
        cb.selectedColor = Color.white;
        boton.colors = cb;
        Texto(img.transform, "Texto", titulo, fuente, colorTexto, Vector2.zero,
            new Vector2(tamano.x - 75, tamano.y - 20));
        return boton;
    }
}
