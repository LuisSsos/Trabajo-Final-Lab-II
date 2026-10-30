using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Gimnasio.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Gimnasio.Controllers;

[ApiController]
[Route("api/login")]
public class ApiLoginController : ControllerBase
{
    private readonly UserManager<Usuario> _userManager;
    private readonly IConfiguration _configuracion;

    public ApiLoginController(UserManager<Usuario> userManager, IConfiguration configuracion)
    {
        _userManager = userManager;
        _configuracion = configuracion;
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginApi datos)
    {
        var usuario = await _userManager.FindByEmailAsync(datos.Email);

        if (usuario == null || !await _userManager.CheckPasswordAsync(usuario, datos.Password))
        {
            return Unauthorized("Email o contraseña incorrectos.");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email!)
        };

        var roles = await _userManager.GetRolesAsync(usuario);

        foreach (var rol in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, rol));
        }

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuracion["JwtClave"]!));
        var credenciales = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credenciales);

        var textoToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new { token = textoToken });
    }
}

public class LoginApi
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}