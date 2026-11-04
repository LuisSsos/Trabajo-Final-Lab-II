
namespace Gimnasio.Models.ViewModels;

public class HorarioDestacadoViewModel
{
    public int HorarioId { get; set; }

    public string NombreClase { get; set; } = string.Empty;

    public string NombreEntrenador { get; set; } = string.Empty;

    public DayOfWeek DiaSemana { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFin { get; set; }

    public decimal PromedioInscriptos { get; set; }

    public int CupoMaximo { get; set; }

    public decimal PorcentajeOcupacion =>
        CupoMaximo > 0
            ? Math.Min(
                100m,
                Math.Round(PromedioInscriptos * 100m / CupoMaximo, 1))
            : 0;
}
