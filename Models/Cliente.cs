using System.ComponentModel.DataAnnotations;

namespace Gimnasio.Models;

public class Cliente
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    [Display(Name = "Fecha de nacimiento")]
    [DataType(DataType.Date)]
    public DateTime FechaNacimiento { get; set; }

    [Display(Name = "Apto médico")]
    public string? AptoMedico { get; set; }

    public ICollection<Membresia> Membresias { get; set; } = new List<Membresia>();
    public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
}