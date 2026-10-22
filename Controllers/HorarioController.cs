using System.Linq.Expressions;
using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Authorize(Roles = Roles.Administrador + "," + Roles.Recepcionista)]
public class HorarioController : Controller
{
    private const int TamanoPagina = 10;

    private static readonly Expression<Func<Horario, HorarioViewModel>> Proyeccion = h => new HorarioViewModel
    {
        Id = h.Id,
        ClaseId = h.ClaseId,
        NombreClase = h.Clase.Nombre,
        DiaSemana = h.DiaSemana,
        HoraInicio = h.HoraInicio,
        HoraFin = h.HoraFin
    };

    private readonly ContextoDatos _contexto;

    public HorarioController(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var total = await _contexto.Horarios.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));

        if (pagina < 1)
        {
            pagina = 1;
        }
        else if (pagina > totalPaginas)
        {
            pagina = totalPaginas;
        }

        var horarios = await _contexto.Horarios
            .OrderBy(h => h.Id)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(Proyeccion)
            .ToListAsync();

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;

        return View(horarios);
    }

    public async Task<IActionResult> Detalles(int id)
    {
        var modelo = await _contexto.Horarios
            .Where(h => h.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();

        if (modelo == null)
        {
            return NotFound();
        }

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarListasAsync();
        return View(new HorarioViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(HorarioViewModel modelo)
    {
        await ValidarAsync(modelo);

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(modelo);
        }

        var horario = new Horario
        {
            ClaseId = modelo.ClaseId!.Value,
            DiaSemana = modelo.DiaSemana,
            HoraInicio = modelo.HoraInicio!.Value,
            HoraFin = modelo.HoraFin!.Value
        };

        _contexto.Horarios.Add(horario);
        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var modelo = await _contexto.Horarios
            .Where(h => h.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();

        if (modelo == null)
        {
            return NotFound();
        }

        await CargarListasAsync();
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, HorarioViewModel modelo)
    {
        if (id != modelo.Id)
        {
            return BadRequest();
        }

        await ValidarAsync(modelo);

        if (!ModelState.IsValid)
        {
            await CargarListasAsync();
            return View(modelo);
        }

        var horario = await _contexto.Horarios.FindAsync(id);

        if (horario == null)
        {
            return NotFound();
        }

        horario.ClaseId = modelo.ClaseId!.Value;
        horario.DiaSemana = modelo.DiaSemana;
        horario.HoraInicio = modelo.HoraInicio!.Value;
        horario.HoraFin = modelo.HoraFin!.Value;

        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var modelo = await _contexto.Horarios
            .Where(h => h.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();

        if (modelo == null)
        {
            return NotFound();
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id)
    {
        var horario = await _contexto.Horarios.FindAsync(id);

        if (horario == null)
        {
            return NotFound();
        }

        // No se borra un horario con turnos reservados
        if (await _contexto.Turnos.AnyAsync(t => t.HorarioId == id))
        {
            ModelState.AddModelError("", "No se puede eliminar el horario porque tiene turnos reservados.");

            var modelo = await _contexto.Horarios
                .Where(h => h.Id == id)
                .Select(Proyeccion)
                .FirstAsync();

            return View("Eliminar", modelo);
        }

        _contexto.Horarios.Remove(horario);
        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarAsync(HorarioViewModel modelo)
    {
        if (modelo.ClaseId != null && !await _contexto.Clases.AnyAsync(c => c.Id == modelo.ClaseId))
        {
            ModelState.AddModelError(nameof(modelo.ClaseId), "La clase elegida no existe.");
        }

        if (modelo.HoraInicio != null && modelo.HoraFin != null)
        {
            // 00:00 como hora de fin vale como medianoche (ej: de 22:00 a 00:00)
            var terminaAMedianoche = modelo.HoraFin == TimeSpan.Zero && modelo.HoraInicio > TimeSpan.Zero;

            if (!terminaAMedianoche && modelo.HoraFin <= modelo.HoraInicio)
            {
                ModelState.AddModelError(nameof(modelo.HoraFin), "La hora de fin tiene que ser posterior a la de inicio.");
            }
        }
    }

    // Arma las listas desplegables de clases y dias
    private async Task CargarListasAsync()
    {
        var clases = await _contexto.Clases.OrderBy(c => c.Nombre).ToListAsync();

        ViewBag.Clases = clases
            .Select(c => new SelectListItem(c.Nombre, c.Id.ToString()))
            .ToList();

        ViewBag.Dias = HorarioViewModel.OrdenDias
            .Select(d => new SelectListItem(HorarioViewModel.NombreDia(d), ((int)d).ToString()))
            .ToList();
    }
}
