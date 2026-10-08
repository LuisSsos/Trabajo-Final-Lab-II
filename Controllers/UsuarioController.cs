using Gimnasio.Models;
using Gimnasio.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

[Authorize(Roles = Roles.Administrador)]
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
            ModelState.AddModelError(nameof(modelo.Password), "Tenes que ingresar una contrasena.");
        }

        if (modelo.Rol != Roles.Administrador && modelo.Rol != Roles.Empleado && modelo.Rol != Roles.Cliente)
        {
            ModelState.AddModelError(nameof(modelo.Rol), "Elegi un rol.");
        }

        if (modelo.Rol == Roles.Cliente)
        {
            if (modelo.FechaNacimiento == null)
            {
                ModelState.AddModelError(nameof(modelo.FechaNacimiento), "Tenes que ingresar la fecha de nacimiento del cliente.");
            }
            else if (modelo.FechaNacimiento.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(modelo.FechaNacimiento), "La fecha de nacimiento no puede ser posterior a hoy.");
            }
            else if (modelo.FechaNacimiento.Value.Date < DateTime.Today.AddYears(-100))
            {
                ModelState.AddModelError(nameof(modelo.FechaNacimiento), "La fecha de nacimiento no parece valida.");
            }
        }

        if (modelo.Rol == Roles.Empleado && !Cargos.Todos.Contains(modelo.Cargo ?? ""))
        {
            ModelState.AddModelError(nameof(modelo.Cargo), "Elegi un cargo de la lista.");
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

        if (modelo.Rol == Roles.Cliente)
        {
            usuario.Cliente = new Cliente { FechaNacimiento = modelo.FechaNacimiento!.Value };
        }
        else if (modelo.Rol == Roles.Empleado)
        {
            usuario.Empleado = new Empleado { Cargo = modelo.Cargo! };
        }

        var resultado = await _userManager.CreateAsync(usuario, modelo.Password ?? "");

        if (resultado.Succeeded)
        {
            var resultadoRol = await _userManager.AddToRoleAsync(usuario, modelo.Rol);

            if (resultadoRol.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }

            await _userManager.DeleteAsync(usuario);

            foreach (var error in resultadoRol.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(modelo);
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