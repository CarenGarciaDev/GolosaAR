using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Representa los datos de una partida individual finalizada.
/// Sus campos públicos permiten que JsonUtility los serialice en el historial local.
/// </summary>
[Serializable]
public class GolosaPartidaGuardada
{
    /// <summary>Nombre del participante registrado al finalizar la partida.</summary>
    public string nombre;
    /// <summary>Fecha y hora local de finalización, expresadas como texto.</summary>
    public string fecha;
    /// <summary>Puntuación obtenida por el jugador.</summary>
    public int puntos;
    /// <summary>Duración de la partida, en segundos.</summary>
    public float tiempoSegundos;
    /// <summary>Estado o resultado registrado al finalizar la partida.</summary>
    public string resultado;
}

/// <summary>
/// Contenedor serializable del conjunto de partidas guardadas.
/// Se utiliza como estructura raíz del archivo JSON del historial.
/// </summary>
[Serializable]
public class GolosaHistorialDatos
{
    public List<GolosaPartidaGuardada> partidas = new List<GolosaPartidaGuardada>();
}

/// <summary>
/// Registro local de partidas individuales para Golosa AR.
/// No necesita GameObject ni referencias de Inspector.
/// Se conecta desde GolosaJuegoUI al finalizar cada partida.
/// </summary>
public static class GolosaRegistroLocal
{
    // Limita la cantidad de registros almacenados para evitar que el historial
    // crezca indefinidamente en el dispositivo.
    private const int MaxPartidas = 500;

    /// <summary>
    /// Obtiene la ruta completa del archivo JSON en el almacenamiento persistente
    /// asignado por Unity a la aplicación. El nombre del archivo es fijo.
    /// </summary>
    private static string RutaArchivo
    {
        get { return Path.Combine(Application.persistentDataPath, "golosa_partidas.json"); }
    }

    /// <summary>
    /// Agrega una partida al historial local y escribe los datos actualizados en disco.
    /// Normaliza el nombre y el resultado cuando están vacíos y evita tiempos negativos.
    /// Si se supera el límite de registros, descarta los más antiguos.
    /// </summary>
    /// <param name="nombre">Nombre del participante.</param>
    /// <param name="puntos">Puntuación final de la partida.</param>
    /// <param name="tiempoSegundos">Duración acumulada de la partida, en segundos.</param>
    /// <param name="resultado">Texto que describe el resultado obtenido.</param>
    public static void GuardarPartida(string nombre, int puntos, float tiempoSegundos, string resultado)
    {
        GolosaHistorialDatos datos = CargarDatos();
        datos.partidas.Add(new GolosaPartidaGuardada
        {
            nombre = string.IsNullOrWhiteSpace(nombre) ? "Jugador" : nombre.Trim(),
            fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            puntos = puntos,
            tiempoSegundos = Mathf.Max(0f, tiempoSegundos),
            resultado = string.IsNullOrEmpty(resultado) ? "Finalizada" : resultado
        });

        // La lista se conserva en orden de inserción: el inicio contiene
        // los registros más antiguos y se elimina primero al alcanzar el límite.
        if (datos.partidas.Count > MaxPartidas)
            datos.partidas.RemoveRange(0, datos.partidas.Count - MaxPartidas);

        GuardarDatos(datos);
    }

    /// <summary>
    /// Recupera las partidas guardadas en orden cronológico inverso,
    /// de la más reciente a la más antigua.
    /// </summary>
    /// <returns>Lista de partidas para mostrar en la pantalla Registro.</returns>
    public static List<GolosaPartidaGuardada> ObtenerHistorial()
    {
        List<GolosaPartidaGuardada> partidas = CargarDatos().partidas;
        partidas.Reverse();
        return partidas;
    }

    /// <summary>
    /// Recupera una clasificación de partidas ordenadas por puntuación descendente.
    /// Cuando dos registros tienen los mismos puntos, la fecha textual se utiliza
    /// como criterio secundario en orden ascendente.
    /// </summary>
    /// <param name="cantidad">Número máximo de registros solicitados; nunca se considera negativo.</param>
    /// <returns>Lista ordenada y limitada al número de posiciones indicado.</returns>
    public static List<GolosaPartidaGuardada> ObtenerClasificacion(int cantidad = 10)
    {
        List<GolosaPartidaGuardada> partidas = CargarDatos().partidas;
        partidas.Sort((a, b) =>
        {
            int diferencia = b.puntos.CompareTo(a.puntos);
            return diferencia != 0 ? diferencia : string.CompareOrdinal(a.fecha, b.fecha);
        });

        // Se recortan los registros que exceden la cantidad requerida
        // después de aplicar los criterios de clasificación.
        cantidad = Mathf.Max(0, cantidad);
        if (partidas.Count > cantidad)
            partidas.RemoveRange(cantidad, partidas.Count - cantidad);

        return partidas;
    }

    /// <summary>
    /// Lee y deserializa el archivo JSON del historial de partidas.
    /// Si aún no existe, contiene datos nulos o no se puede interpretar,
    /// devuelve un contenedor vacío para mantener disponible el registro.
    /// </summary>
    /// <returns>Datos del historial recuperados o una estructura vacía.</returns>
    private static GolosaHistorialDatos CargarDatos()
    {
        if (!File.Exists(RutaArchivo)) return new GolosaHistorialDatos();
        try
        {
            GolosaHistorialDatos datos = JsonUtility.FromJson<GolosaHistorialDatos>(
                File.ReadAllText(RutaArchivo));
            return datos != null && datos.partidas != null ? datos : new GolosaHistorialDatos();
        }
        catch (Exception ex)
        {
            // Se registra el error en la consola de Unity y se devuelve
            // un historial vacío sin interrumpir la ejecución del juego.
            Debug.LogError("Golosa AR: no se pudo leer historial: " + ex.Message);
            return new GolosaHistorialDatos();
        }
    }

    /// <summary>
    /// Serializa el historial y lo almacena en el archivo JSON persistente.
    /// Los errores de escritura se informan en la consola sin propagar excepciones.
    /// </summary>
    /// <param name="datos">Contenedor con las partidas que se van a guardar.</param>
    private static void GuardarDatos(GolosaHistorialDatos datos)
    {
        try
        {
            File.WriteAllText(RutaArchivo, JsonUtility.ToJson(datos, true));
        }
        catch (Exception ex)
        {
            Debug.LogError("Golosa AR: no se pudo guardar historial: " + ex.Message);
        }
    }
}
