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


    [HttpGet]
    public async Task<IActionResult> ListarAjax(int pagina = 1)
    {
        var total = await _contexto.Clases.CountAsync();
        var totalPaginas = Math.Max(
            1,
            (int)Math.Ceiling(total / (double)TamanoPagina)
        );

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

        return Json(new
        {
            items = clases,
            paginaActual = pagina,
            totalPaginas
        });
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
            var errores = ModelState
                .Where(campo => campo.Value?.Errors.Count > 0)
                .ToDictionary(
                    campo => campo.Key,
                    campo => campo.Value!.Errors
                        .Select(error => error.ErrorMessage)
                        .ToArray()
                );

            return BadRequest(new
            {
                success = false,
                mensaje = "Revisá los datos ingresados.",
                errores
            });
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

        return Json(new
        {
            success = true,
            mensaje = "La clase se creó correctamente.",
            redirectUrl = Url.Action(nameof(Index))
        });
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
            return BadRequest(new
            {
                success = false,
                mensaje = "El identificador de la clase no coincide."
            });
        }

        await ValidarEntrenadorAsync(modelo);

        if (!ModelState.IsValid)
        {
            var errores = ModelState
                .Where(campo => campo.Value?.Errors.Count > 0)
                .ToDictionary(
                    campo => campo.Key,
                    campo => campo.Value!.Errors
                        .Select(error => error.ErrorMessage)
                        .ToArray()
                );

            return BadRequest(new
            {
                success = false,
                mensaje = "Revisá los datos ingresados.",
                errores
            });
        }

        var clase = await _contexto.Clases.FindAsync(id);

        if (clase == null)
        {
            return NotFound(new
            {
                success = false,
                mensaje = "No se encontró la clase que intentás editar."
            });
        }

        clase.Nombre = modelo.Nombre;
        clase.Descripcion = modelo.Descripcion;
        clase.CupoMaximo = modelo.CupoMaximo;
        clase.EmpleadoId = modelo.EmpleadoId!.Value;

        await _contexto.SaveChangesAsync();

        return Json(new
        {
            success = true,
            mensaje = "La clase se actualizó correctamente.",
            redirectUrl = Url.Action(nameof(Index))
        });
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
        bool esAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        var clase = await _contexto.Clases.FindAsync(id);

        if (clase == null)
        {
            if (esAjax)
            {
                return NotFound(new
                {
                    success = false,
                    mensaje = "No se encontró la clase que intentás eliminar."
                });
            }

            return NotFound();
        }

        var horarios = await _contexto.Horarios
            .Where(h => h.ClaseId == id)
            .ToListAsync();

        _contexto.Horarios.RemoveRange(horarios);
        _contexto.Clases.Remove(clase);

        await _contexto.SaveChangesAsync();

        if (esAjax)
        {
            return Json(new
            {
                success = true,
                mensaje = "La clase y sus horarios asociados se eliminaron correctamente.",
                id
            });
        }

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
