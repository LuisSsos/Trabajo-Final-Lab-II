namespace Gimnasio.Models.ViewModels;

public class IngresoViewModel
{
    public bool Consultado { get; set; }

    // success (verde), warning (amarillo) o danger (rojo)
    public string Color { get; set; } = "secondary";

    public string Titulo { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;

    public UsuarioViewModel? Persona { get; set; }
}
