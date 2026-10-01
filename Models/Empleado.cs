using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Empleado
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    [Required, StringLength(80)]
    public string Cargo { get; set; } = string.Empty;

    public ICollection<Clase> Clases { get; set; } = new List<Clase>();
}