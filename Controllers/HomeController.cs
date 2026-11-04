using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Gimnasio.Models;
using Microsoft.AspNetCore.Authorization;
using Gimnasio.Data;
using Gimnasio.Models.ViewModels;

using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

public class HomeController : Controller
{
    private readonly ContextoDatos _contexto;

    public HomeController(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var inicioSemanaActual = DateTime.Today.AddDays(
            -(((int)DateTime.Today.DayOfWeek + 6) % 7));

        var inicioPeriodo = inicioSemanaActual;
        var finPeriodo = inicioSemanaActual.AddDays(21);

        // Obtenemos la cantidad de inscriptos de cada horario durante ese período.
        var horariosDatos = await _contexto.Horarios
            .Select(horario => new
            {
                horario.Id,
                NombreClase = horario.Clase.Nombre,
                NombreEntrenador =
                    horario.Clase.Empleado.Usuario.Nombre + " " +
                    horario.Clase.Empleado.Usuario.Apellido,
                horario.DiaSemana,
                horario.HoraInicio,
                horario.HoraFin,
                CupoMaximo = horario.Clase.CupoMaximo,

                TotalInscriptos = horario.Turnos.Count(turno =>
                    (turno.Estado == EstadoTurno.Reservado ||
                     turno.Estado == EstadoTurno.Asistio) &&
                    turno.Fecha >= inicioPeriodo &&
                    turno.Fecha < finPeriodo)
            })
            .ToListAsync();

        // Calculamos el promedio semanal y ordenamos los horarios.
        var horariosDestacados = horariosDatos
            .Select(horario => new HorarioDestacadoViewModel
            {
                HorarioId = horario.Id,
                NombreClase = horario.NombreClase,
                NombreEntrenador = horario.NombreEntrenador,
                DiaSemana = horario.DiaSemana,
                HoraInicio = horario.HoraInicio,
                HoraFin = horario.HoraFin,
                CupoMaximo = horario.CupoMaximo,
                PromedioInscriptos = horario.TotalInscriptos / 3m
            })
            .OrderByDescending(horario => horario.PromedioInscriptos)
            .ThenBy(horario => horario.NombreClase)
            .ThenBy(horario => horario.DiaSemana)
            .ThenBy(horario => horario.HoraInicio)
            .Take(5)
            .ToList();

        return View(horariosDestacados);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
