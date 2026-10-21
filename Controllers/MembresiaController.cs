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
public class MembresiaController : Controller
{
    private const int TamanoPagina = 10;

    private static readonly Expression<Func<Membresia, MembresiaViewModel>> Proyeccion = m => new MembresiaViewModel
    {
        Id = m.Id,
        ClienteId = m.ClienteId,
        NombreCliente = m.Cliente.Usuario.Nombre + " " + m.Cliente.Usuario.Apellido,
        PlanId = m.PlanId,
        NombrePlan = m.Plan.Nombre,
        FechaInicio = m.FechaInicio,
        FechaFin = m.FechaFin,
        Estado = m.Estado
    };

    private readonly ContextoDatos _contexto;

    public MembresiaController(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var total = await _contexto.Membresias.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));

        if (pagina < 1)
        {
            pagina = 1;
        }
        else if (pagina > totalPaginas)
        {
            pagina = totalPaginas;
        }

        var membresias = await _contexto.Membresias
            .OrderByDescending(m => m.Id)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(Proyeccion)
            .ToListAsync();

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;

        return View(membresias);
    }

    // Lo llama el buscador de la pantalla con ajax. Devuelve hasta 10 clientes.
    [HttpGet]
    public async Task<IActionResult> BuscarClientes(string? termino)
    {
        termino = (termino ?? "").Trim();

        var clientes = await _contexto.Clientes
            .Where(c => c.Usuario.Nombre.Contains(termino) || c.Usuario.Apellido.Contains(termino))
            .OrderBy(c => c.Usuario.Apellido)
            .ThenBy(c => c.Usuario.Nombre)
            .Take(10)
            .Select(c => new
            {
                id = c.Id,
                nombre = c.Usuario.Nombre + " " + c.Usuario.Apellido
            })
            .ToListAsync();

        return Json(clientes);
    }

    public async Task<IActionResult> Detalles(int id)
    {
        var modelo = await _contexto.Membresias
            .Where(m => m.Id == id)
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
        await CargarPlanesAsync();
        return View(new MembresiaViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(MembresiaViewModel modelo)
    {
        await ValidarAsync(modelo);

        if (!ModelState.IsValid)
        {
            await CargarPlanesAsync();
            return View(modelo);
        }

        var plan = await _contexto.Planes.FindAsync(modelo.PlanId);

        var membresia = new Membresia
        {
            ClienteId = modelo.ClienteId!.Value,
            PlanId = modelo.PlanId!.Value,
            FechaInicio = modelo.FechaInicio!.Value,
            FechaFin = modelo.FechaInicio.Value.AddDays(plan!.DuracionDias),
            Estado = EstadoMembresia.Activa
        };

        _contexto.Membresias.Add(membresia);
        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var modelo = await _contexto.Membresias
            .Where(m => m.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();

        if (modelo == null)
        {
            return NotFound();
        }

        await CargarPlanesAsync();
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, MembresiaViewModel modelo)
    {
        if (id != modelo.Id)
        {
            return BadRequest();
        }

        await ValidarAsync(modelo);

        if (!ModelState.IsValid)
        {
            await CargarPlanesAsync();
            return View(modelo);
        }

        var membresia = await _contexto.Membresias.FindAsync(id);
        var plan = await _contexto.Planes.FindAsync(modelo.PlanId);

        if (membresia == null)
        {
            return NotFound();
        }

        membresia.ClienteId = modelo.ClienteId!.Value;
        membresia.PlanId = modelo.PlanId!.Value;
        membresia.FechaInicio = modelo.FechaInicio!.Value;
        membresia.FechaFin = modelo.FechaInicio.Value.AddDays(plan!.DuracionDias);
        membresia.Estado = modelo.Estado;

        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var modelo = await _contexto.Membresias
            .Where(m => m.Id == id)
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
        var membresia = await _contexto.Membresias.FindAsync(id);

        if (membresia == null)
        {
            return NotFound();
        }

        // No se borra una membresia con pagos, se perderia el historial de cobros
        if (await _contexto.Pagos.AnyAsync(p => p.MembresiaId == id))
        {
            ModelState.AddModelError("", "No se puede eliminar la membresia porque tiene pagos registrados.");

            var modelo = await _contexto.Membresias
                .Where(m => m.Id == id)
                .Select(Proyeccion)
                .FirstAsync();

            return View("Eliminar", modelo);
        }

        _contexto.Membresias.Remove(membresia);
        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarAsync(MembresiaViewModel modelo)
    {
        // El cliente tiene que salir de la busqueda, no alcanza con escribir el nombre
        if (modelo.ClienteId != null && !await _contexto.Clientes.AnyAsync(c => c.Id == modelo.ClienteId))
        {
            ModelState.AddModelError(nameof(modelo.ClienteId), "Elegi un cliente de la lista.");
        }

        if (modelo.PlanId != null && !await _contexto.Planes.AnyAsync(p => p.Id == modelo.PlanId))
        {
            ModelState.AddModelError(nameof(modelo.PlanId), "El plan elegido no existe.");
        }
    }

    private async Task CargarPlanesAsync()
    {
        var planes = await _contexto.Planes.OrderBy(p => p.Nombre).ToListAsync();

        ViewBag.Planes = planes
            .Select(p => new SelectListItem($"{p.Nombre} ({p.DuracionDias} días)", p.Id.ToString()))
            .ToList();
    }
}
