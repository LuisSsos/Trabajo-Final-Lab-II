using System.Linq.Expressions;
using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class ClaseController : Controller
{
    private const int TamanoPagina = 10;

    private static readonly Expression<Func<Clase, ClaseViewModel>> Proyeccion = c => new ClaseViewModel
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Descripcion = c.Descripcion,
        CupoMaximo = c.CupoMaximo,
        EmpleadoId = c.EmpleadoId,
        NombreEntrenador = c.Empleado.Usuario.Nombre + " " + c.Empleado.Usuario.Apellido
    };

    private readonly ContextoDatos _contexto;

    public ClaseController(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var total = await _contexto.Clases.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));

        if (pagina < 1)
        {
            pagina = 1;
        }
        else if (pagina > totalPaginas)
        {
            pagina = totalPaginas;
        }

        var clases = await _contexto.Clases
            .OrderBy(c => c.Id)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(Proyeccion)
            .ToListAsync();

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;

        return View(clases);
    }

    // Lo llama el buscador de la pantalla con ajax.
    [HttpGet]
    public async Task<IActionResult> BuscarEntrenadores(string? termino)
    {
        termino = (termino ?? "").Trim();

        var entrenadores = await _contexto.Empleados
            .Where(e => e.Cargo == Roles.Entrenador)
            .Where(e => e.Usuario.Nombre.Contains(termino) || e.Usuario.Apellido.Contains(termino))
            .OrderBy(e => e.Usuario.Apellido)
            .ThenBy(e => e.Usuario.Nombre)
            .Take(10)
            .Select(e => new
            {
                id = e.Id,
                nombre = e.Usuario.Nombre + " " + e.Usuario.Apellido
            })
            .ToListAsync();

        return Json(entrenadores);
    }

    public async Task<IActionResult> Detalles(int id)
    {
        var modelo = await _contexto.Clases
            .Where(c => c.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();

        if (modelo == null)
        {
            return NotFound();
        }

        return View(modelo);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View(new ClaseViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ClaseViewModel modelo)
    {
        await ValidarEntrenadorAsync(modelo);

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var clase = new Clase
        {
            Nombre = modelo.Nombre,
            Descripcion = modelo.Descripcion,
            CupoMaximo = modelo.CupoMaximo,
            EmpleadoId = modelo.EmpleadoId!.Value
        };

        _contexto.Clases.Add(clase);
        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var modelo = await _contexto.Clases
            .Where(c => c.Id == id)
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
    public async Task<IActionResult> Editar(int id, ClaseViewModel modelo)
    {
        if (id != modelo.Id)
        {
            return BadRequest();
        }

        await ValidarEntrenadorAsync(modelo);

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var clase = await _contexto.Clases.FindAsync(id);

        if (clase == null)
        {
            return NotFound();
        }

        clase.Nombre = modelo.Nombre;
        clase.Descripcion = modelo.Descripcion;
        clase.CupoMaximo = modelo.CupoMaximo;
        clase.EmpleadoId = modelo.EmpleadoId!.Value;

        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var modelo = await _contexto.Clases
            .Where(c => c.Id == id)
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
        var clase = await _contexto.Clases.FindAsync(id);

        if (clase == null)
        {
            return NotFound();
        }

        // si se borra una clase con horarios, se pierden sus turnos
        if (await _contexto.Horarios.AnyAsync(h => h.ClaseId == id))
        {
            ModelState.AddModelError("", "No se puede eliminar la clase porque tiene horarios asociados.");

            var modelo = await _contexto.Clases
                .Where(c => c.Id == id)
                .Select(Proyeccion)
                .FirstAsync();

            return View("Eliminar", modelo);
        }

        _contexto.Clases.Remove(clase);
        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // el entrenador tiene que aparecer en la busqueda, no alcanza con poner el nombre.
    private async Task ValidarEntrenadorAsync(ClaseViewModel modelo)
    {
        var esEntrenador = await _contexto.Empleados
            .AnyAsync(e => e.Id == modelo.EmpleadoId && e.Cargo == Roles.Entrenador);

        if (!esEntrenador)
        {
            ModelState.AddModelError(nameof(modelo.EmpleadoId), "Elegi un entrenador de la lista.");
        }
    }
}
