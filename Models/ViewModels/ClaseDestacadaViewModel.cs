namespace Gimnasio.Models.ViewModels;

public class ClaseDestacadaViewModel
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int CupoMaximo { get; set; }

    public int CantidadReservas { get; set; }
}