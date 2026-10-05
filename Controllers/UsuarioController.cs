using Gimnasio.Models;
using Gimnasio.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Authorize]
public class UsuarioController : Controller
{
    private readonly UserManager<Usuario> _userManager;

    public UsuarioController(UserManager<Usuario> userManager)
    {
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        var usuarios = _userManager.Users
            .Select(u => new UsuarioViewModel
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email ?? string.Empty,
                Telefono = u.PhoneNumber,
                Avatar = u.Avatar
            })
            .ToList();

        return View(usuarios);
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(UsuarioViewModel modelo)
    {
        if (string.IsNullOrWhiteSpace(modelo.Password))
        {
            ModelState.AddModelError(
                nameof(modelo.Password),
                "La contraseña es obligatoria.");
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = new Usuario
        {
            UserName = modelo.Email,
            Email = modelo.Email,
            PhoneNumber = modelo.Telefono,
            Nombre = modelo.Nombre,
            Apellido = modelo.Apellido
        };

        var resultado = await _userManager.CreateAsync(
            usuario,
            modelo.Password ?? ""
        );

        if (resultado.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in resultado.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(modelo);
    }

    public async Task<IActionResult> Detalles(int id)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());

        if (usuario == null)
        {
            return NotFound();
        }

        var modelo = new UsuarioViewModel
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email ?? string.Empty,
            Telefono = usuario.PhoneNumber,
            Avatar = usuario.Avatar
        };

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());

        if (usuario == null)
        {
            return NotFound();
        }

        var modelo = new UsuarioViewModel
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email ?? string.Empty,
            Telefono = usuario.PhoneNumber,
            Avatar = usuario.Avatar
        };

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, UsuarioViewModel modelo)
    {
        if (id != modelo.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = await _userManager.FindByIdAsync(id.ToString());

        if (usuario == null)
        {
            return NotFound();
        }

        usuario.Nombre = modelo.Nombre;
        usuario.Apellido = modelo.Apellido;
        usuario.Email = modelo.Email;
        usuario.UserName = modelo.Email;
        usuario.PhoneNumber = modelo.Telefono;

        var resultado = await _userManager.UpdateAsync(usuario);

        if (resultado.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in resultado.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Eliminar(int id)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());

        if (usuario == null)
        {
            return NotFound();
        }

        var modelo = new UsuarioViewModel
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email ?? string.Empty,
            Telefono = usuario.PhoneNumber,
            Avatar = usuario.Avatar
        };

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id)
    {
        var usuario = await _userManager.FindByIdAsync(id.ToString());

        if (usuario == null)
        {
            return NotFound();
        }

        var resultado = await _userManager.DeleteAsync(usuario);

        if (resultado.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in resultado.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        var modelo = new UsuarioViewModel
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email ?? string.Empty,
            Telefono = usuario.PhoneNumber,
            Avatar = usuario.Avatar
        };

        return View("Eliminar", modelo);
    }
}