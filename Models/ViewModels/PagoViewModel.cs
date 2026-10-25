using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models.ViewModels;

public class PagoViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Elegí una membresía de la lista.")]
    public int? MembresiaId { get; set; }

    [Display(Name = "Membresía")]
    public string? DescripcionMembresia { get; set; }

    public string? NombreCliente { get; set; }

    public string? NombrePlan { get; set; }

    public DateTime? FechaFinMembresia { get; set; }

    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Range(0.01, 1000000, ErrorMessage = "El monto tiene que ser mayor a 0.")]
    public decimal? Monto { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime? Fecha { get; set; } = DateTime.Today;

    [Display(Name = "Método de pago")]
    public MetodoPago Metodo { get; set; }

    public string? Comprobante { get; set; }

    [Display(Name = "Comprobante")]
    public IFormFile? ArchivoComprobante { get; set; }
}
