using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Plan
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Nombre { get; set; } = string.Empty;

    [Range(0, 1000000)]
    [DataType(DataType.Currency)]
    public decimal Precio { get; set; }

    [Display(Name = "Duración (días)")]
    [Range(1, 3650)]
    public int DuracionDias { get; set; }

    public ICollection<Membresia> Membresias { get; set; } = new List<Membresia>();
}