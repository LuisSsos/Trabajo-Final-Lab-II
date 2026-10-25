using System.Linq.Expressions;
using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.ViewModels;
using Gimnasio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Authorize(Roles = Roles.Administrador + "," + Roles.Recepcionista)]
public class PagoController : Controller
{
    private const int TamanoPagina = 10;
    private static readonly string[] ExtensionesComprobante = { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
    private const long TamanoMaximoComprobante = 5 * 1024 * 1024; // 5 MB

    private static readonly Expression<Func<Pago, PagoViewModel>> Proyeccion = p => new PagoViewModel
    {
        Id = p.Id,
        MembresiaId = p.MembresiaId,
        NombreCliente = p.Membresia.Cliente.Usuario.Nombre + " " + p.Membresia.Cliente.Usuario.Apellido,
        NombrePlan = p.Membresia.Plan.Nombre,
        FechaFinMembresia = p.Membresia.FechaFin,
        Monto = p.Monto,
        Fecha = p.Fecha,
        Metodo = p.Metodo,
        Comprobante = p.Comprobante
    };

    private readonly ContextoDatos _contexto;
    private readonly ServicioArchivos _archivos;

    public PagoController(ContextoDatos contexto, ServicioArchivos archivos)
    {
        _contexto = contexto;
        _archivos = archivos;
    }

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var total = await _contexto.Pagos.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));

        if (pagina < 1)
        {
            pagina = 1;
        }
        else if (pagina > totalPaginas)
        {
            pagina = totalPaginas;
        }

        var pagos = await _contexto.Pagos
            .OrderByDescending(p => p.Id)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(Proyeccion)
            .ToListAsync();

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;

        return View(pagos);
    }

    // Lo llama el buscador de la pantalla con ajax. Busca membresias por nombre del cliente.
    [HttpGet]
    public async Task<IActionResult> BuscarMembresias(string? termino)
    {
        termino = (termino ?? "").Trim();

        var membresias = await _contexto.Membresias
            .Where(m => m.Cliente.Usuario.Nombre.Contains(termino) || m.Cliente.Usuario.Apellido.Contains(termino))
            .OrderByDescending(m => m.Id)
            .Take(10)
            .Select(m => new
            {
                m.Id,
                Cliente = m.Cliente.Usuario.Nombre + " " + m.Cliente.Usuario.Apellido,
                Plan = m.Plan.Nombre,
                m.FechaFin
            })
            .ToListAsync();

        var resultado = membresias.Select(m => new
        {
            id = m.Id,
            nombre = Describir(m.Cliente, m.Plan, m.FechaFin)
        });

        return Json(resultado);
    }

    public async Task<IActionResult> Detalles(int id)
    {
        var modelo = await _contexto.Pagos
            .Where(p => p.Id == id)
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
        return View(new PagoViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(PagoViewModel modelo)
    {
        await ValidarAsync(modelo);

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var pago = new Pago
        {
            MembresiaId = modelo.MembresiaId!.Value,
            Monto = modelo.Monto!.Value,
            Fecha = modelo.Fecha!.Value,
            Metodo = modelo.Metodo
        };

        if (modelo.ArchivoComprobante != null && modelo.ArchivoComprobante.Length > 0)
        {
            pago.Comprobante = await _archivos.GuardarAsync(modelo.ArchivoComprobante, "comprobantes");
        }

        _contexto.Pagos.Add(pago);
        await _contexto.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var modelo = await _contexto.Pagos
            .Where(p => p.Id == id)
            .Select(Proyeccion)
            .FirstOrDefaultAsync();

        if (modelo == null)
        {
            return NotFound();
        }

        modelo.DescripcionMembresia = Describir(modelo.NombreCliente, modelo.NombrePlan, modelo.FechaFinMembresia);

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, PagoViewModel modelo)
    {
        if (id != modelo.Id)
        {
            return BadRequest();
        }

        await ValidarAsync(modelo);

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var pago = await _contexto.Pagos.FindAsync(id);

        if (pago == null)
        {
            return NotFound();
        }

        var comprobanteAnterior = pago.Comprobante;

        if (modelo.ArchivoComprobante != null && modelo.ArchivoComprobante.Length > 0)
        {
            pago.Comprobante = await _archivos.GuardarAsync(modelo.ArchivoComprobante, "comprobantes");
        }

        pago.MembresiaId = modelo.MembresiaId!.Value;
        pago.Monto = modelo.Monto!.Value;
        pago.Fecha = modelo.Fecha!.Value;
        pago.Metodo = modelo.Metodo;

        await _contexto.SaveChangesAsync();

        // Si se subio un comprobante nuevo, el anterior ya no sirve
        if (pago.Comprobante != comprobanteAnterior)
        {
            _archivos.Borrar(comprobanteAnterior);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var modelo = await _contexto.Pagos
            .Where(p => p.Id == id)
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
        var pago = await _contexto.Pagos.FindAsync(id);

        if (pago == null)
        {
            return NotFound();
        }

        _contexto.Pagos.Remove(pago);
        await _contexto.SaveChangesAsync();

        _archivos.Borrar(pago.Comprobante);

        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarAsync(PagoViewModel modelo)
    {
        // La membresia tiene que salir de la busqueda, no alcanza con escribir el nombre
        if (modelo.MembresiaId != null && !await _contexto.Membresias.AnyAsync(m => m.Id == modelo.MembresiaId))
        {
            ModelState.AddModelError(nameof(modelo.MembresiaId), "Elegi una membresia de la lista.");
        }

        var archivo = modelo.ArchivoComprobante;

        if (archivo != null && archivo.Length > 0)
        {
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (!ExtensionesComprobante.Contains(extension))
            {
                ModelState.AddModelError(nameof(modelo.ArchivoComprobante), "El comprobante tiene que ser una imagen JPG, PNG, WEBP o un PDF.");
            }
            else if (archivo.Length > TamanoMaximoComprobante)
            {
                ModelState.AddModelError(nameof(modelo.ArchivoComprobante), "El comprobante no puede pesar mas de 5 MB.");
            }
        }
    }

    private static string Describir(string? cliente, string? plan, DateTime? fin)
    {
        return $"{cliente} - {plan} (vence {fin?.ToString("dd/MM/yyyy")})";
    }
}
