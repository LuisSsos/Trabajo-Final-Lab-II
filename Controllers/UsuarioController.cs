using Gimnasio.Models;
using Gimnasio.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gimnasio.Services;

namespace Gimnasio.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class UsuarioController : Controller
{
    private const int TamanoPagina = 10;
    private static readonly string[] ExtensionesAvatar = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long TamanoMaximoAvatar = 2 * 1024 * 1024; 

    private readonly UserManager<Usuario> _userManager;
    private readonly ServicioArchivos _archivos;

    public UsuarioController(UserManager<Usuario> userManager, ServicioArchivos archivos)
    {
        _userManager = userManager;
        _archivos = archivos;
    }

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var total = await _userManager.Users.CountAsync();
        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));

        if (pagina < 1)
        {
            pagina = 1;
        }
        else if (pagina > totalPaginas)
        {
            pagina = totalPaginas;
        }

        var usuarios = await _userManager.Users
            .OrderBy(u => u.Id)
            .Skip((pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .Select(u => new UsuarioViewModel
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email ?? string.Empty,
                Telefono = u.PhoneNumber,
                Avatar = u.Avatar
            })
            .ToListAsync();

        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;

        return View(usuarios);
    }

    // Controla que el avatar sea una imagen y no pese mucho
    private void ValidarAvatar(IFormFile? archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            return;
        }

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

        if (!ExtensionesAvatar.Contains(extension))
        {
            ModelState.AddModelError(nameof(UsuarioViewModel.ArchivoAvatar), "El avatar tiene que ser una imagen JPG, PNG o WEBP.");
        }
        else if (archivo.Length > TamanoMaximoAvatar)
        {
            ModelState.AddModelError(nameof(UsuarioViewModel.ArchivoAvatar), "El avatar no puede pesar mas de 2 MB.");
        }
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

        ValidarAvatar(modelo.ArchivoAvatar);

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

        if (modelo.ArchivoAvatar != null && modelo.ArchivoAvatar.Length > 0)
        {
            usuario.Avatar = await _archivos.GuardarAsync(modelo.ArchivoAvatar, "avatares");
        }

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
            _archivos.Borrar(usuario.Avatar);

            foreach (var error in resultadoRol.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(modelo);
        }

        _archivos.Borrar(usuario.Avatar);

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

        
        ModelState.Remove(nameof(modelo.Rol));

        ValidarAvatar(modelo.ArchivoAvatar);

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = await _userManager.FindByIdAsync(id.ToString());

        if (usuario == null)
        {
            return NotFound();
        }

        var avatarAnterior = usuario.Avatar;

        if (modelo.ArchivoAvatar != null && modelo.ArchivoAvatar.Length > 0)
        {
            usuario.Avatar = await _archivos.GuardarAsync(modelo.ArchivoAvatar, "avatares");
        }

        usuario.Nombre = modelo.Nombre;
        usuario.Apellido = modelo.Apellido;
        usuario.Email = modelo.Email;
        usuario.UserName = modelo.Email;
        usuario.PhoneNumber = modelo.Telefono;

        var resultado = await _userManager.UpdateAsync(usuario);

        if (resultado.Succeeded)
        {
            // Si se subio un avatar nuevo, el anterior ya no sirve
            if (usuario.Avatar != avatarAnterior)
            {
                _archivos.Borrar(avatarAnterior);
            }

            return RedirectToAction(nameof(Index));
        }

        if (usuario.Avatar != avatarAnterior)
        {
            _archivos.Borrar(usuario.Avatar);
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
            _archivos.Borrar(usuario.Avatar);
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