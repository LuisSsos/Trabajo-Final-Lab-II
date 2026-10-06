using Gimnasio.Models;
using Microsoft.AspNetCore.Identity;

namespace Gimnasio.Data;

public static class SemillaDatos
{
    public static async Task InicializarAsync(IServiceProvider servicios)
    {
        var gestorRoles = servicios.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var gestorUsuarios = servicios.GetRequiredService<UserManager<Usuario>>();

        foreach (var rol in new[] { Roles.Administrador, Roles.Empleado, Roles.Cliente })
        {
            if (!await gestorRoles.RoleExistsAsync(rol))
                await gestorRoles.CreateAsync(new IdentityRole<int>(rol));
        }

        if (await gestorUsuarios.FindByEmailAsync("admin@gimnasio.com") == null)
        {
            var admin = new Usuario
            {
                UserName = "admin@gimnasio.com",
                Email = "admin@gimnasio.com",
                Nombre = "Admin",
                Apellido = "Sistema",
                EmailConfirmed = true
            };

            var resultado = await gestorUsuarios.CreateAsync(admin, "Admin123");
            if (resultado.Succeeded)
                await gestorUsuarios.AddToRoleAsync(admin, Roles.Administrador);
        }
    }
}