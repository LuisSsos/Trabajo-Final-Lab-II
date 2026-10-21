using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.ViewModels;

public class MembresiaViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Elegí un cliente de la lista.")]
    public int? ClienteId { get; set; }

    [Display(Name = "Cliente")]
    public string? NombreCliente { get; set; }

    [Display(Name = "Plan")]
    [Required(ErrorMessage = "Elegí un plan.")]
    public int? PlanId { get; set; }

    public string? NombrePlan { get; set; }

    [Display(Name = "Fecha de inicio")]
    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime? FechaInicio { get; set; } = DateTime.Today;

    // Se calcula sola: fecha de inicio + duracion del plan
    [Display(Name = "Fecha de fin")]
    public DateTime? FechaFin { get; set; }

    public EstadoMembresia Estado { get; set; } = EstadoMembresia.Activa;
}
