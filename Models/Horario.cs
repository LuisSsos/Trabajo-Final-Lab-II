using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Horario
{
    public int Id { get; set; }

    public int ClaseId { get; set; }
    public Clase Clase { get; set; } = null!;

    [Display(Name = "Día")]
    public DayOfWeek DiaSemana { get; set; }

    [Display(Name = "Hora de inicio")]
    [DataType(DataType.Time)]
    public TimeSpan HoraInicio { get; set; }

    [Display(Name = "Hora de fin")]
    [DataType(DataType.Time)]
    public TimeSpan HoraFin { get; set; }

    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}