using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Gimnasio.Models;

public class Usuario : IdentityUser<int>
{
    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Apellido { get; set; } = string.Empty;

    public string? Avatar { get; set; }

    public Empleado? Empleado { get; set; }
    public Cliente? Cliente { get; set; }

    public string NombreCompleto => $"{Nombre} {Apellido}";
}