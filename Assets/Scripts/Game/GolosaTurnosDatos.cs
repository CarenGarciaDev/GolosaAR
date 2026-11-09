using System;
using System.Collections.Generic;
using UnityEngine;

// Datos de los participantes; no altera los datos existentes del modo Individual.
/// <summary>
/// Mantiene y persiste el estado de una partida por turnos sin modificar el modo individual.
/// </summary>
public static class GolosaTurnosDatos
{
    [Serializable]
    public class Jugador
    {
        public string nombre;
        public string categoria;
        public string avatarId;
        public float tiempoSegundos;
        public int puntos;
        public bool finalizo;
    }
    [Serializable] private class Partida { public int indice; public List<Jugador> jugadores = new List<Jugador>(); }

    private const string Clave = "Golosa.PorTurnos.Configuracion";
    private static Partida actual;
    public static bool Activa => actual != null && actual.jugadores != null && actual.jugadores.Count >= 2;
    public static int Indice => Activa ? actual.indice : -1;
    public static int Total => Activa ? actual.jugadores.Count : 0;
    public static Jugador Actual => Activa && Indice >= 0 && Indice < Total ? actual.jugadores[Indice] : null;
    public static List<Jugador> Jugadores => Activa ? actual.jugadores : new List<Jugador>();

    /// <summary>
    /// Valida e inicia una partida de dos a cuatro participantes.
    /// </summary>
    /// <param name="jugadores">Participantes configurados para la partida.</param>
    public static void Iniciar(List<Jugador> jugadores)
    {
        if (jugadores == null || jugadores.Count < 2 || jugadores.Count > 4)
            throw new ArgumentException("Por turnos requiere entre 2 y 4 jugadores.");
        actual = new Partida { indice = 0, jugadores = jugadores };
        PlayerPrefs.SetString("Golosa.ModoJuego", "Por turnos");
        AplicarJugadorActual();
        Guardar();
    }
    /// <summary>
    /// Copia el nombre, categoría y avatar del jugador activo a PlayerPrefs.
    /// </summary>
    public static void AplicarJugadorActual()
    {
        var j = Actual;
        if (j == null) return;
        PlayerPrefs.SetString("Golosa.Nombre", j.nombre);
        PlayerPrefs.SetString("Golosa.Categoria", j.categoria);
        PlayerPrefs.SetString("Golosa.AvatarId", j.avatarId);
        PlayerPrefs.Save();
    }
    /// <summary>
    /// Registra el resultado del turno y avanza al siguiente participante si existe.
    /// </summary>
    /// <param name="segundos">Duración del turno.</param>
    /// <param name="puntuacion">Puntos obtenidos.</param>
    /// <returns>Verdadero cuando hay otro jugador pendiente.</returns>
    public static bool RegistrarMetaYAvanzar(float segundos, int puntuacion)
    {
        if (!Activa) return false;
        var j = Actual;
        if (j == null || j.finalizo) return false;
        j.finalizo = true;
        j.tiempoSegundos = Mathf.Max(0f, segundos);
        j.puntos = puntuacion;
        bool haySiguiente = actual.indice + 1 < actual.jugadores.Count;
        if (haySiguiente) { actual.indice++; AplicarJugadorActual(); }
        Guardar();
        return haySiguiente;
    }
    /// <summary>
    /// Serializa la partida actual en las preferencias persistentes.
    /// </summary>
    public static void Guardar() { if (Activa) PlayerPrefs.SetString(Clave, JsonUtility.ToJson(actual)); PlayerPrefs.Save(); }
    /// <summary>
    /// Intenta restaurar una partida guardada cuando todavía no hay una activa.
    /// </summary>
    public static void Recuperar()
    {
        if (Activa || !PlayerPrefs.HasKey(Clave)) return;
        try { actual = JsonUtility.FromJson<Partida>(PlayerPrefs.GetString(Clave)); }
        catch (Exception e) { Debug.LogWarning("No se pudo recuperar partida por turnos: " + e.Message); actual = null; }
    }
    /// <summary>
    /// Descarta el estado de la partida por turnos y su copia persistida.
    /// </summary>
    public static void Cancelar()
    {
        actual = null;
        PlayerPrefs.DeleteKey(Clave);
        PlayerPrefs.Save();
    }
}
