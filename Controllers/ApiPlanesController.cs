using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Controllers;

[ApiController]
[Route("api/planes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ApiPlanesController : ControllerBase
{
    private readonly ContextoDatos _contexto;

    public ApiPlanesController(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var planes = await _contexto.Planes
            .Select(p => new { p.Id, p.Nombre, p.Precio, p.DuracionDias })
            .ToListAsync();

        return Ok(planes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Ver(int id)
    {
        var plan = await _contexto.Planes
            .Where(p => p.Id == id)
            .Select(p => new { p.Id, p.Nombre, p.Precio, p.DuracionDias })
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound();
        }

        return Ok(plan);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Administrador, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Crear(Plan plan)
    {
        _contexto.Planes.Add(plan);
        await _contexto.SaveChangesAsync();

        return Ok(new { plan.Id, plan.Nombre, plan.Precio, plan.DuracionDias });
    }
}