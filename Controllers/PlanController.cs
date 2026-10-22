using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Authorize(Roles = Roles.Administrador  + "," + Roles.Recepcionista)]
public class PlanController : Controller
{
    private const int TamanoPagina = 10;

    private readonly ContextoDatos _contexto;

    public PlanController(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var total = await _contexto.Planes.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));

        if (pagina < 1)
        {
            pagina = 1;
        }
        else if (pagina > totalPaginas)
        {
            pagina = totalPaginas;
        }

        var planes = await _contexto.Planes
            .OrderBy(p => p.Id)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .ToListAsync();

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;

        return View(planes);
    }

    public async Task<IActionResult> Detalles(int id)
    {
        var plan = await _contexto.Planes.FindAsync(id);

        if (plan == null)
        {
            return NotFound();
        }

        return View(plan);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View(new Plan());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear([Bind("Nombre,Precio,DuracionDias")] Plan plan)
    {
        if (!ModelState.IsValid)
        {
            return View(plan);
        }

        _contexto.Planes.Add(plan);
        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var plan = await _contexto.Planes.FindAsync(id);

        if (plan == null)
        {
            return NotFound();
        }

        return View(plan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, [Bind("Id,Nombre,Precio,DuracionDias")] Plan plan)
    {
        if (id != plan.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(plan);
        }

        var existente = await _contexto.Planes.FindAsync(id);

        if (existente == null)
        {
            return NotFound();
        }

        existente.Nombre = plan.Nombre;
        existente.Precio = plan.Precio;
        existente.DuracionDias = plan.DuracionDias;

        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var plan = await _contexto.Planes.FindAsync(id);

        if (plan == null)
        {
            return NotFound();
        }

        return View(plan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id)
    {
        var plan = await _contexto.Planes.FindAsync(id);

        if (plan == null)
        {
            return NotFound();
        }

        // No se puede borrar un plan que ya tiene membresias, se perderia el historial
        if (await _contexto.Membresias.AnyAsync(m => m.PlanId == id))
        {
            ModelState.AddModelError("", "No se puede eliminar el plan porque tiene membresias asociadas.");
            return View("Eliminar", plan);
        }

        _contexto.Planes.Remove(plan);
        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
