namespace Gimnasio.Services;

// guarda y borra archivos subidos dentro de la carpeta wwwroot/uploads.
public class ServicioArchivos
{
    private readonly IWebHostEnvironment _entorno;

    public ServicioArchivos(IWebHostEnvironment entorno)
    {
        _entorno = entorno;
    }

    // Guarda el archivo con un nombre nuevo y devuelve la ruta para usar en el navegador.
    public async Task<string> GuardarAsync(IFormFile archivo, string carpeta)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var nombre = Guid.NewGuid().ToString("N") + extension;

        var carpetaFisica = Path.Combine(_entorno.WebRootPath, "uploads", carpeta);
        Directory.CreateDirectory(carpetaFisica);

        using var stream = new FileStream(Path.Combine(carpetaFisica, nombre), FileMode.Create);
        await archivo.CopyToAsync(stream);

        return $"/uploads/{carpeta}/{nombre}";
    }

    public void Borrar(string? ruta)
    {
        if (string.IsNullOrWhiteSpace(ruta))
        {
            return;
        }

        var rutaFisica = Path.Combine(_entorno.WebRootPath, ruta.TrimStart('/'));

        if (File.Exists(rutaFisica))
        {
            File.Delete(rutaFisica);
        }
    }
}