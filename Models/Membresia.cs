using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Membresia
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public int PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    [Display(Name = "Fecha de inicio")]
    [DataType(DataType.Date)]
    public DateTime FechaInicio { get; set; }

    [Display(Name = "Fecha de fin")]
    [DataType(DataType.Date)]
    public DateTime FechaFin { get; set; }

    public EstadoMembresia Estado { get; set; } = EstadoMembresia.Activa;

    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}