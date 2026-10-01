using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Pago
{
    public int Id { get; set; }

    public int MembresiaId { get; set; }
    public Membresia Membresia { get; set; } = null!;

    [Range(0.01, 1000000)]
    [DataType(DataType.Currency)]
    public decimal Monto { get; set; }

    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Display(Name = "Método")]
    public MetodoPago Metodo { get; set; }

    public string? Comprobante { get; set; }
}