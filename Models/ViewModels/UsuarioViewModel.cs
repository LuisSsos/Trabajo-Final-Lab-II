using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.ViewModels;

public class UsuarioViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(80)]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El teléfono no es válido.")]
    public string? Telefono { get; set; }

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    public string? Avatar { get; set; }

    public string Rol { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? FechaNacimiento { get; set; }

    [StringLength(80)]
    public string? Cargo { get; set; }
}