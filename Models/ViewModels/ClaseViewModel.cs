using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.ViewModels;

public class ClaseViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(500)]
    public string? Descripcion { get; set; }

    [Display(Name = "Cupo máximo")]
    [Range(1, 500, ErrorMessage = "El cupo tiene que estar entre 1 y 500.")]
    public int CupoMaximo { get; set; }

    public int? EmpleadoId { get; set; }

    [Display(Name = "Entrenador")]
    public string? NombreEntrenador { get; set; }
}
