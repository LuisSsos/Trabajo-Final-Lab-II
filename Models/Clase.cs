using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Clase
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Descripción")]
    [StringLength(500)]
    public string? Descripcion { get; set; }

    public int EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;

    [Display(Name = "Cupo máximo")]
    [Range(1, 500)]
    public int CupoMaximo { get; set; }

    public ICollection<Horario> Horarios { get; set; } = new List<Horario>();
}