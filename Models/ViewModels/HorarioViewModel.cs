using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.ViewModels;

public class HorarioViewModel
{
    // La semana arranca el lunes, DayOfWeek arranca el domingo
    public static readonly DayOfWeek[] OrdenDias =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    public static string NombreDia(DayOfWeek dia) => dia switch
    {
        DayOfWeek.Monday => "Lunes",
        DayOfWeek.Tuesday => "Martes",
        DayOfWeek.Wednesday => "Miércoles",
        DayOfWeek.Thursday => "Jueves",
        DayOfWeek.Friday => "Viernes",
        DayOfWeek.Saturday => "Sábado",
        _ => "Domingo"
    };

    public int Id { get; set; }

    [Display(Name = "Clase")]
    [Required(ErrorMessage = "Elegí una clase.")]
    public int? ClaseId { get; set; }

    public string? NombreClase { get; set; }

    [Display(Name = "Día")]
    public DayOfWeek DiaSemana { get; set; } = DayOfWeek.Monday;

    [Display(Name = "Hora de inicio")]
    [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
    public TimeSpan? HoraInicio { get; set; }

    [Display(Name = "Hora de fin")]
    [Required(ErrorMessage = "La hora de fin es obligatoria.")]
    public TimeSpan? HoraFin { get; set; }
}
