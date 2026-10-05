using Gimnasio.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Gimnasio.Controllers;

public class CuentaController : Controller
{
    private readonly SignInManager<Usuario> _signInManager;

    public CuentaController(SignInManager<Usuario> signInManager)
    {
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        string email,
        string password,
        string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "El email y la contraseña son obligatorios.";
            return View();
        }

        var resultado = await _signInManager.PasswordSignInAsync(
            email,
            password,
            false,
            false);

        if (resultado.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Email o contraseña incorrectos.";

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccesoDenegado()
    {
        return View();
    }
}