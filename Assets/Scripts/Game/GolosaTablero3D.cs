
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Dibuja la golosa 3D en la posicion validada por PrepararRAUI.
// No altera ARCore ni mueve el tablero fuera del plano detectado.
// Agrega un punto invisible para el futuro avatar.
/// <summary>
/// Genera en tiempo de ejecución el tablero tridimensional de golosa, sus casillas y animación de entrada.
/// </summary>
public class GolosaTablero3D : MonoBehaviour
{
    private readonly List<Material> materiales = new List<Material>();
    private readonly List<Mesh> mallas = new List<Mesh>();

    private float animacion;
    private readonly Dictionary<int, Renderer> superficies = new Dictionary<int, Renderer>();
    private readonly Dictionary<int, Color> coloresOriginales = new Dictionary<int, Color>();
    private int casillaMarcada;

    /// <summary>
    /// Devuelve la posición local correspondiente a una casilla del 1 al 10.
    /// </summary>
    /// <param name="n">Número de casilla solicitado.</param>
    /// <returns>Posición local o Vector3.zero si el número no corresponde a una casilla.</returns>
    public Vector3 PosicionLocalCasilla(int n)
    {
        if (n < 1 || n > 10) return Vector3.zero;
        int fila = n == 1 ? 0 : n <= 3 ? 1 : n == 4 ? 2 : n <= 6 ? 3 : n == 7 ? 4 : n <= 9 ? 5 : 6;
        float x = (n == 2 || n == 5 || n == 8) ? -.166f : (n == 3 || n == 6 || n == 9) ? .166f : 0f;
        return new Vector3(x, .095f, (fila - 3) * .317f);
    }

    /// <summary>
    /// Restaura el color de la casilla anterior y resalta la casilla indicada.
    /// </summary>
    /// <param name="n">Número de casilla que debe destacarse.</param>
    public void MarcarCasilla(int n)
    {
        if (casillaMarcada > 0 && superficies.TryGetValue(casillaMarcada, out Renderer anterior))
        {
            Material m = anterior.sharedMaterial;
            if (m != null)
            {
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", coloresOriginales[casillaMarcada]);
                else if (m.HasProperty("_Color")) m.SetColor("_Color", coloresOriginales[casillaMarcada]);
            }
        }
        casillaMarcada = n;
        if (n > 0 && superficies.TryGetValue(n, out Renderer actual))
        {
            Material m = actual.sharedMaterial;
            if (m != null)
            {
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color32(255, 248, 180, 255));
                else if (m.HasProperty("_Color")) m.SetColor("_Color", new Color32(255, 248, 180, 255));
            }
        }
    }


    // Punto reservado para colocar el avatar cuando se implemente.
    // Es solo un Transform vacio: NO crea personajes.
    public Transform puntoAvatarFuturo { get; private set; }

    private static readonly string[] Paleta =
    {
        "#F9C846", // 1 amarillo
        "#47D4DB", // 2 turquesa
        "#B38AF9", // 3 lila
        "#79CCF2", // 4 azul cielo
        "#A4E38E", // 5 verde
        "#FFD17C", // 6 durazno
        "#C2A0FF", // 7 violeta
        "#66C5EE", // 8 azul
        "#F2A7D3", // 9 rosa
        "#F6D657"  // 10 amarillo
    };
    /// <summary>
    /// Crea, orienta y construye el tablero sobre la posición de suelo validada.
    /// </summary>
    /// <param name="posicionSuelo">Punto del suelo detectado.</param>
    /// <param name="camaraAR">Transform de la cámara de realidad aumentada.</param>
    /// <returns>Componente del tablero creado.</returns>
    public static GolosaTablero3D Crear(
        Vector3 posicionSuelo,
        Transform camaraAR)
    {
        GameObject objeto = new GameObject(
            "GOLOSA_3D_SOBRE_EL_SUELO"
        );

        objeto.transform.position =
            posicionSuelo + Vector3.up * 0.018f;

        // Orientar la golosa alejandose de la camara AR.
        Vector3 adelante = camaraAR != null
            ? posicionSuelo - camaraAR.position
            : Vector3.forward;

        adelante.y = 0f;

        if (adelante.sqrMagnitude < 0.01f && camaraAR != null)
        {
            adelante = camaraAR.forward;
            adelante.y = 0f;
        }

        if (adelante.sqrMagnitude < 0.01f)
        {
            adelante = Vector3.forward;
        }

        objeto.transform.rotation = Quaternion.LookRotation(
            adelante.normalized,
            Vector3.up
        );

        // Crear el tablero.
        GolosaTablero3D tablero =
            objeto.AddComponent<GolosaTablero3D>();

        tablero.Construir();

        // Empezar pequeno para mostrar la animacion.
        objeto.transform.localScale = Vector3.one * 0.15f;

        // NUEVO: conectar la interfaz y controles del juego.
        // Este componente debe existir en el proyecto.
        objeto.AddComponent<GolosaJuegoUI>();

        return tablero;
    }

    /// <summary>
    /// Actualiza gradualmente la escala durante la animación de aparición.
    /// </summary>
    private void Update()
    {
        // Animacion de aparicion.
        if (animacion >= 1f)
            return;

        animacion = Mathf.Min(
            1f,
            animacion + Time.deltaTime * 1.9f
        );
        float factor = Mathf.SmoothStep(
            0.15f,
            1f,
            animacion
        );

        transform.localScale = Vector3.one * factor;
    }

    /// <summary>
    /// Distribuye las diez casillas, añade inicio, meta y reserva un anclaje para el avatar.
    /// </summary>
    private void Construir()
    {
        // Distribucion:
        // 1 / 2-3 / 4 / 5-6 / 7 / 8-9 / 10
        int[][] filas =
        {
            new[] { 1 },
            new[] { 2, 3 },
            new[] { 4 },
            new[] { 5, 6 },
            new[] { 7 },
            new[] { 8, 9 },
            new[] { 10 }
        };

        const float espacioFila = 0.317f;
        const float mitad = 0.166f;

        for (int fila = 0; fila < filas.Length; fila++)
        {
            float z = (fila - 3) * espacioFila;
            int[] numeros = filas[fila];

            for (int j = 0; j < numeros.Length; j++)
            {
                int numero = numeros[j];

                float x = numeros.Length == 1
                    ? 0f
                    : (j == 0 ? -mitad : mitad);

                CrearCasilla(
                    numero,
                    new Vector3(x, 0f, z)
                );
            }
        }

        CrearSenal("INICIO", -1.29f, "#19C7D8");
        CrearSenal("META", 1.29f, "#822CE7");

        GameObject reserva = new GameObject("PuntoAvatarFuturo_NO_VISIBLE");
        reserva.transform.SetParent(transform, false);
        reserva.transform.localPosition = new Vector3(0f, 0.02f, -1.53f);
        puntoAvatarFuturo = reserva.transform;
    }
    /// <summary>
    /// Construye una casilla con capas tridimensionales, número y decoración.
    /// </summary>
    /// <param name="numero">Identificador de casilla.</param>
    /// <param name="centro">Posición local del centro de la casilla.</param>
    private void CrearCasilla(int numero, Vector3 centro)
    {
        GameObject baseCasilla = new GameObject(
            "Casilla_" + numero
        );

        baseCasilla.transform.SetParent(transform, false);
        baseCasilla.transform.localPosition = centro;

        // Cuatro niveles para un efecto de juguete 3D mas vistoso.
        CrearPlaca(baseCasilla.transform, "Sombra_suelo", 0.325f, 0.325f,
            0.018f, -0.002f, "#452080");
        CrearPlaca(
            baseCasilla.transform,
            "Sombra",
            0.313f,
            0.313f,
            0.045f,
            0.012f,
            "#42217C"
        );
        // Borde blanco.
        CrearPlaca(
            baseCasilla.transform,
            "Marco_blanco",
            0.305f,
            0.305f,
            0.040f,
            0.033f,
            "#FFFFFF"
        );

        // Superficie de color.
        CrearPlaca(
            baseCasilla.transform,
            "Superficie_color",
            0.278f,
            0.278f,
            0.025f,
            0.050f,
            Paleta[numero - 1]
        );
        Transform superficie = baseCasilla.transform.Find("Superficie_color");
        if (superficie != null)
        {
            Renderer r = superficie.GetComponent<Renderer>();
            superficies[numero] = r;
            coloresOriginales[numero] = ColorDe(Paleta[numero - 1]);
        }


        // Numero.
        CrearTexto3D(
            baseCasilla.transform,
            numero.ToString(),
            0.075f,
            2.25f,
            0.095f,
            "#401776"
        );
        // Detalle pequeno que no tapa el numero.
        CrearTexto3D(baseCasilla.transform, "★", 0.082f, 0.75f, 0.072f,
            "#FFFFFF", new Vector3(0.094f, 0f, 0.085f));
    }

    /// <summary>
    /// Genera una señal de inicio o meta en la posición indicada.
    /// </summary>
    /// <param name="texto">Texto de la señal.</param>
    /// <param name="z">Coordenada longitudinal.</param>
    /// <param name="color">Color HTML del fondo.</param>
    private void CrearSenal(
        string texto,
        float z,
        string color)
    {
        GameObject senal = new GameObject(
            "Senal_" + texto
        );

        senal.transform.SetParent(transform, false);
        senal.transform.localPosition =
            new Vector3(0f, 0f, z);

        CrearPlaca(
            senal.transform,
            "Fondo",
            0.52f,
            0.16f,
            0.027f,
            0.017f,
            color
        );
        CrearTexto3D(
          senal.transform,
          texto,
          0.040f,
          1.2f,
          0.095f,
          "#FFFFFF"
      );
    }

    /// <summary>
    /// Crea una etiqueta TextMeshPro tridimensional sobre su objeto padre.
    /// </summary>
    private void CrearTexto3D(
        Transform padre,
        string contenido,
        float altura,
        float tamFuente,
        float escala,
        string color, Vector3 desplazamiento = default(Vector3))
    {
        GameObject textoGO = new GameObject(
            "Texto_" + contenido,
            typeof(RectTransform),
            typeof(TextMeshPro)
        );
        textoGO.transform.SetParent(padre, false);

        textoGO.transform.localPosition =
            new Vector3(0f, altura, 0f) + desplazamiento;

        textoGO.transform.localRotation =
            Quaternion.Euler(90f, 0f, 0f);

        textoGO.transform.localScale =
            Vector3.one * escala;

        TextMeshPro tmp =
            textoGO.GetComponent<TextMeshPro>();

        tmp.text = contenido;
        tmp.fontSize = tamFuente;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = ColorDe(color);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        textoGO.GetComponent<RectTransform>().sizeDelta =
            new Vector2(3.3f, 2f);
    }
    /// <summary>
    /// Construye una placa de malla redondeada y registra sus recursos.
    /// </summary>
    private void CrearPlaca(
        Transform padre,
        string nombre,
        float ancho,
        float largo,
        float grosor,
        float altura,
        string color)
    {
        GameObject placa = new GameObject(
            nombre,
            typeof(MeshFilter),
            typeof(MeshRenderer)
        );

        placa.transform.SetParent(padre, false);

        placa.transform.localPosition =
            new Vector3(0f, altura, 0f);

        Mesh malla = MallaRedondeada(
            ancho,
            largo,
            grosor,
            Mathf.Min(ancho, largo) * 0.18f
        );
        placa.GetComponent<MeshFilter>().sharedMesh =
           malla;

        placa.GetComponent<MeshRenderer>().sharedMaterial =
            NuevoMaterial(ColorDe(color));

        mallas.Add(malla);
    }

    /// <summary>
    /// Genera vértices y triángulos de una placa con esquinas redondeadas.
    /// </summary>
    /// <returns>Malla tridimensional generada.</returns>
    private static Mesh MallaRedondeada(
        float ancho,
        float largo,
        float grosor,
        float radio)
    {
        List<Vector2> borde = new List<Vector2>();

        float hx = ancho * 0.5f;
        float hz = largo * 0.5f;

        float[] centrosX =
        {
            hx - radio,
            -hx + radio,
            -hx + radio,
            hx - radio
        };
        float[] centrosZ =
      {
            hz - radio,
            hz - radio,
            -hz + radio,
            -hz + radio
        };

        float[] angulos =
        {
            0f,
            90f,
            180f,
            270f
        };
        const int pasos = 5;

        for (int esquina = 0; esquina < 4; esquina++)
        {
            for (int p = 0; p <= pasos; p++)
            {
                float rad =
                    (angulos[esquina] + p * 90f / pasos)
                    * Mathf.Deg2Rad;

                borde.Add(
                    new Vector2(
                        centrosX[esquina] +
                        Mathf.Cos(rad) * radio,

                        centrosZ[esquina] +
                        Mathf.Sin(rad) * radio
                    )
                );
            }
        }
        List<Vector3> vertices =
           new List<Vector3> { Vector3.zero };

        List<int> triangulos =
            new List<int>();

        for (int i = 0; i < borde.Count; i++)
        {
            Vector2 b = borde[i];

            vertices.Add(
                new Vector3(b.x, 0f, b.y)
            );

            vertices.Add(
                new Vector3(b.x, -grosor, b.y)
            );
        }
        for (int i = 0; i < borde.Count; i++)
        {
            int sig = (i + 1) % borde.Count;

            int arriba = 1 + i * 2;
            int abajo = arriba + 1;

            int arribaSig = 1 + sig * 2;
            int abajoSig = arribaSig + 1;

            triangulos.Add(0);
            triangulos.Add(arribaSig);
            triangulos.Add(arriba);

            triangulos.Add(arriba);
            triangulos.Add(arribaSig);
            triangulos.Add(abajo);

            triangulos.Add(arribaSig);
            triangulos.Add(abajoSig);
            triangulos.Add(abajo);
        }

        Mesh mesh = new Mesh
        {
            name = "Casilla3D_BordesRedondeados"
        };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangulos, 0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// Crea un material usando el primer shader disponible y conserva su referencia.
    /// </summary>
    /// <returns>Material creado.</returns>
    private Material NuevoMaterial(Color color)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        materiales.Add(material);

        return material;
    }

    /// <summary>
    /// Convierte un color HTML en un valor Color de Unity.
    /// </summary>
    /// <returns>Color resultante de interpretar el código HTML.</returns>
    private static Color ColorDe(string html)
    {
        ColorUtility.TryParseHtmlString(
            html,
            out Color color
        );

        return color;
    }

    /// <summary>
    /// Libera los materiales y las mallas creados dinámicamente por el tablero.
    /// </summary>
    private void OnDestroy()
    {
        foreach (Material material in materiales)
        {
            if (material != null)
                Destroy(material);
        }
        foreach (Mesh mesh in mallas)
        {
            if (mesh != null)
                Destroy(mesh);
        }
    }
}






