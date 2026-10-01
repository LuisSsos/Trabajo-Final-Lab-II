using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Turno
{
    public int Id { get; set; }

    public int HorarioId { get; set; }
    public Horario Horario { get; set; } = null!;

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; }

    public EstadoTurno Estado { get; set; } = EstadoTurno.Reservado;
}