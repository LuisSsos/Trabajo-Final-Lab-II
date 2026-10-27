
using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class CrearTurnoViewModel
{
    [Required]
    [Display(Name = "Cliente")]
    public int? ClienteId { get; set; }

    [Required]
    [Display(Name = "Horario")]
    public int? HorarioId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateTime? Fecha { get; set; }
}
