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
        var clasesDestacadas = await _contexto.Clases
            .Select(clase => new ClaseDestacadaViewModel
            {
                Id = clase.Id,
                Nombre = clase.Nombre,
                Descripcion = clase.Descripcion,
                CupoMaximo = clase.CupoMaximo,
                CantidadReservas = clase.Horarios
                    .SelectMany(horario => horario.Turnos)
                    .Count(turno => turno.Estado == EstadoTurno.Reservado)
            })
            .OrderByDescending(clase => clase.CantidadReservas)
            .ThenBy(clase => clase.Nombre)
            .Take(5)
            .ToListAsync();

        return View(clasesDestacadas);
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
