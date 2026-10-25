using System.Text.RegularExpressions;
using Gimnasio.Data;
using Gimnasio.Models;
using Gimnasio.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[Authorize(Roles = Roles.Administrador + "," + Roles.Recepcionista)]
public class IngresoController : Controller
{
    // Cuando faltan estos dias o menos para el vencimiento, el cartel pasa a amarillo
    private const int DiasAviso = 5;

    private readonly ContextoDatos _contexto;

    public IngresoController(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new IngresoViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string? dni)
    {
        dni = (dni ?? "").Trim();

        var modelo = new IngresoViewModel { Consultado = true };

        if (!Regex.IsMatch(dni, @"^\d{7,9}$"))
        {
            return View(Rojo(modelo, "DNI no válido", "Ingresá solo números, sin puntos."));
        }

        var cliente = await _contexto.Clientes
            .Include(c => c.Usuario)
            .FirstOrDefaultAsync(c => c.Dni == dni);

        if (cliente == null)
        {
            return View(Rojo(modelo, "No puede ingresar", "No hay ningún cliente registrado con ese DNI."));
        }

        modelo.Persona = new UsuarioViewModel
        {
            Nombre = cliente.Usuario.Nombre,
            Apellido = cliente.Usuario.Apellido,
            Avatar = cliente.Usuario.Avatar
        };

        var hoy = DateTime.Today;

        var vigente = await _contexto.Membresias
            .Where(m => m.ClienteId == cliente.Id
                     && m.Estado == EstadoMembresia.Activa
                     && m.FechaInicio <= hoy
                     && m.FechaFin >= hoy)
            .OrderByDescending(m => m.FechaFin)
            .FirstOrDefaultAsync();

        if (vigente == null)
        {
            var ultima = await _contexto.Membresias
                .Where(m => m.ClienteId == cliente.Id)
                .OrderByDescending(m => m.FechaFin)
                .FirstOrDefaultAsync();

            if (ultima == null)
            {
                return View(Rojo(modelo, "No puede ingresar", "No tiene ninguna membresía. Pasar por recepción."));
            }

            if (ultima.Estado == EstadoMembresia.Cancelada)
            {
                return View(Rojo(modelo, "No puede ingresar", "Su membresía fue cancelada. Pasar por recepción."));
            }

            if (ultima.FechaInicio > hoy)
            {
                return View(Rojo(modelo, "No puede ingresar", $"Su membresía empieza el {ultima.FechaInicio:dd/MM/yyyy}."));
            }

            return View(Rojo(modelo, "No puede ingresar", $"Su membresía venció el {ultima.FechaFin:dd/MM/yyyy}. Pasar por recepción."));
        }

        // El plan tiene que estar pagado por completo: la suma de los pagos tiene que alcanzar el precio
        var precio = await _contexto.Planes
            .Where(p => p.Id == vigente.PlanId)
            .Select(p => p.Precio)
            .FirstAsync();

        var pagado = await _contexto.Pagos
            .Where(p => p.MembresiaId == vigente.Id)
            .SumAsync(p => (decimal?)p.Monto) ?? 0;

        if (pagado < precio)
        {
            return View(Rojo(modelo, "No puede ingresar",
                $"Debe ${precio - pagado:N2} de su plan (pagó ${pagado:N2} de ${precio:N2}). Pasar por recepción."));
        }

        var diasRestantes = (vigente.FechaFin.Date - hoy).Days;

        if (diasRestantes <= DiasAviso)
        {
            modelo.Color = "warning";
            modelo.Titulo = "Ingreso correcto";
            modelo.Mensaje = $"Bienvenido/a, {cliente.Usuario.Nombre}. " + diasRestantes switch
            {
                0 => "Tu plan vence hoy.",
                1 => "Tu plan vence mañana.",
                _ => $"Tu plan vence en {diasRestantes} días."
            };

            return View(modelo);
        }

        modelo.Color = "success";
        modelo.Titulo = "Ingreso correcto";
        modelo.Mensaje = $"Bienvenido/a, {cliente.Usuario.Nombre}.";

        return View(modelo);
    }

    private static IngresoViewModel Rojo(IngresoViewModel modelo, string titulo, string mensaje)
    {
        modelo.Color = "danger";
        modelo.Titulo = titulo;
        modelo.Mensaje = mensaje;
        return modelo;
    }
}
