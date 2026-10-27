
using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;

namespace Gimnasio.Controllers;

[Authorize(Roles = Roles.Administrador + "," + Roles.Recepcionista)]
public class TurnoController : Controller
{
    private readonly ContextoDatos _contexto;

    public TurnoController(ContextoDatos contexto)
    {
        _contexto = contexto;
    }


    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var turnos = await _contexto.Turnos
            .AsNoTracking()
            .Include(t => t.Cliente)
                .ThenInclude(c => c.Usuario)
            .Include(t => t.Horario)
                .ThenInclude(h => h.Clase)
            .OrderByDescending(t => t.Fecha)
            .ThenBy(t => t.Horario.HoraInicio)
            .ToListAsync();

        return View(turnos);
    }

    [HttpGet]
    public async Task<IActionResult> BuscarClientes(string? busqueda)
    {
        if (string.IsNullOrWhiteSpace(busqueda))
        {
            return Json(new { success = true, datos = new object[0] });
        }

        busqueda = busqueda.Trim();

        var clientes = await _contexto.Clientes
            .AsNoTracking()
            .Where(c =>
                c.Usuario.Nombre.Contains(busqueda) ||
                c.Usuario.Apellido.Contains(busqueda) ||
                c.Usuario.Email!.Contains(busqueda) ||
                c.Dni!.Contains(busqueda))
            .OrderBy(c => c.Usuario.Apellido)
            .ThenBy(c => c.Usuario.Nombre)
            .Take(10)
            .Select(c => new
            {
                id = c.Id,
                nombre = c.Usuario.Nombre,
                apellido = c.Usuario.Apellido,
                email = c.Usuario.Email,
                dni = c.Dni
            })
            .ToListAsync();

        return Json(new { success = true, datos = clientes });
    }


    [HttpGet]
    public async Task<IActionResult> ListarHorarios()
    {
        var horarios = await _contexto.Horarios
            .AsNoTracking()
            .OrderBy(h => h.Clase.Nombre)
            .ThenBy(h => h.DiaSemana)
            .ThenBy(h => h.HoraInicio)
            .Select(h => new
            {
                id = h.Id,
                claseId = h.ClaseId,
                clase = h.Clase.Nombre,
                diaSemana = h.DiaSemana.ToString(),
                horaInicio = h.HoraInicio.ToString(@"hh\:mm"),
                horaFin = h.HoraFin.ToString(@"hh\:mm"),
                cupoMaximo = h.Clase.CupoMaximo
            })
            .ToListAsync();

        return Json(new { success = true, datos = horarios });
    }


    [HttpGet]
    public async Task<IActionResult> VistaPreviaInscripcion(
        int clienteId,
        int horarioId)
    {
        if (clienteId <= 0 || horarioId <= 0)
            return BadRequest(new
            {
                success = false,
                mensaje = "Seleccioná un cliente y un horario válidos."
            });

        var clienteExiste = await _contexto.Clientes
            .AnyAsync(c => c.Id == clienteId);

        if (!clienteExiste)
            return BadRequest(new
            {
                success = false,
                mensaje = "El cliente seleccionado no existe."
            });

        var horario = await _contexto.Horarios
            .AsNoTracking()
            .Include(h => h.Clase)
            .FirstOrDefaultAsync(h => h.Id == horarioId);

        if (horario == null)
            return BadRequest(new
            {
                success = false,
                mensaje = "El horario seleccionado no existe."
            });

        var membresia = await _contexto.Membresias
            .AsNoTracking()
            .Where(m =>
                m.ClienteId == clienteId &&
                m.Estado == EstadoMembresia.Activa &&
                m.FechaInicio.Date <= DateTime.Today &&
                m.FechaFin.Date >= DateTime.Today)
            .OrderByDescending(m => m.FechaFin)
            .FirstOrDefaultAsync();

        if (membresia == null)
            return BadRequest(new
            {
                success = false,
                mensaje = "El cliente no tiene una membresía activa y vigente hoy."
            });

        var fechaInicio = membresia.FechaInicio.Date > DateTime.Today
            ? membresia.FechaInicio.Date
            : DateTime.Today;

        var fechaFin = membresia.FechaFin.Date;

        var turnosExistentes = await _contexto.Turnos
            .AsNoTracking()
            .Where(t =>
                t.HorarioId == horarioId &&
                t.Fecha >= fechaInicio &&
                t.Fecha <= fechaFin &&
                t.Estado == EstadoTurno.Reservado)
            .GroupBy(t => t.Fecha)
            .Select(g => new
            {
                Fecha = g.Key,
                Cantidad = g.Count()
            })
            .ToListAsync();

        var ocupacionPorFecha = turnosExistentes
            .ToDictionary(t => t.Fecha.Date, t => t.Cantidad);

        var fechas = new List<object>();

        for (var fecha = fechaInicio; fecha <= fechaFin; fecha = fecha.AddDays(1))
        {
            if (fecha.DayOfWeek != horario.DiaSemana)
                continue;

            var yaReservado = await _contexto.Turnos.AnyAsync(t =>
                t.ClienteId == clienteId &&
                t.HorarioId == horarioId &&
                t.Fecha == fecha &&
                t.Estado != EstadoTurno.Cancelado);

            var cantidad = ocupacionPorFecha.GetValueOrDefault(fecha.Date, 0);
            var disponible = cantidad < horario.Clase.CupoMaximo;

            fechas.Add(new
            {
                fecha = fecha.ToString("yyyy-MM-dd"),
                disponible = disponible && !yaReservado,
                cupoCompleto = !disponible,
                yaReservado,
                inscriptos = cantidad,
                cupoMaximo = horario.Clase.CupoMaximo
            });
        }

        return Ok(new
        {
            success = true,
            clase = horario.Clase.Nombre,
            diaSemana = horario.DiaSemana.ToString(),
            horaInicio = horario.HoraInicio.ToString(@"hh\:mm"),
            horaFin = horario.HoraFin.ToString(@"hh\:mm"),
            fechaInicio = fechaInicio.ToString("yyyy-MM-dd"),
            fechaFin = fechaFin.ToString("yyyy-MM-dd"),
            fechas
        });
    }

    [HttpGet]
    public IActionResult Crear()
    {
        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearTurnoViewModel modelo)
    {
        if (!ModelState.IsValid)
            return BadRequest(new
            {
                success = false,
                mensaje = "Completá todos los campos obligatorios."
            });

        var fecha = modelo.Fecha!.Value.Date;

        if (fecha < DateTime.Today)
            return BadRequest(new
            {
                success = false,
                mensaje = "No podés registrar un turno en una fecha pasada."
            });

        await using var transaccion =
            await _contexto.Database.BeginTransactionAsync();

        try
        {
            // Bloquea el horario para serializar las inscripciones
            // que intenten utilizarlo al mismo tiempo.
            var conexion = _contexto.Database.GetDbConnection();

            await using var comando = conexion.CreateCommand();

            comando.Transaction =
                _contexto.Database.CurrentTransaction!.GetDbTransaction();

            comando.CommandText =
                "SELECT Id FROM horarios WHERE Id = @id FOR UPDATE";

            var parametro = comando.CreateParameter();
            parametro.ParameterName = "@id";
            parametro.Value = modelo.HorarioId!.Value;
            comando.Parameters.Add(parametro);

            var resultado = await comando.ExecuteScalarAsync();

            if (resultado == null || resultado == DBNull.Value)
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El horario seleccionado no existe."
                });

            var horario = await _contexto.Horarios
                .Include(h => h.Clase)
                .FirstOrDefaultAsync(h => h.Id == modelo.HorarioId.Value);

            if (horario == null)
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El horario seleccionado no existe."
                });

            if (fecha.DayOfWeek != horario.DiaSemana)
                return BadRequest(new
                {
                    success = false,
                    mensaje = "La fecha elegida no corresponde al día de ese horario."
                });

            var tieneMembresiaVigente = await _contexto.Membresias
                .AnyAsync(m =>
                    m.ClienteId == modelo.ClienteId!.Value &&
                    m.Estado == EstadoMembresia.Activa &&
                    m.FechaInicio.Date <= fecha &&
                    m.FechaFin.Date >= fecha);

            if (!tieneMembresiaVigente)
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El cliente no tiene una membresía activa y vigente para la fecha seleccionada."
                });

            var cliente = await _contexto.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == modelo.ClienteId!.Value);

            if (cliente == null)
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El cliente seleccionado no existe."
                });

            var duplicado = await _contexto.Turnos.AnyAsync(t =>
                t.ClienteId == cliente.Id &&
                t.HorarioId == horario.Id &&
                t.Fecha == fecha &&
                t.Estado != EstadoTurno.Cancelado);

            if (duplicado)
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El cliente ya tiene una reserva para ese horario y fecha."
                });

            var inscriptos = await _contexto.Turnos.CountAsync(t =>
                t.HorarioId == horario.Id &&
                t.Fecha == fecha &&
                t.Estado == EstadoTurno.Reservado);

            if (inscriptos >= horario.Clase.CupoMaximo)
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El cupo de esta clase está completo para esa fecha."
                });

            var turno = new Turno
            {
                ClienteId = cliente.Id,
                HorarioId = horario.Id,
                Fecha = fecha,
                Estado = EstadoTurno.Reservado
            };

            _contexto.Turnos.Add(turno);

            await _contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return Ok(new
            {
                success = true,
                mensaje = "El turno se registró correctamente."
            });
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarInscripcion(int clienteId, int horarioId)
    {
        if (clienteId <= 0 || horarioId <= 0)
            return BadRequest(new
            {
                success = false,
                mensaje = "Seleccioná un cliente y un horario válidos."
            });

        await using var transaccion =
            await _contexto.Database.BeginTransactionAsync();

        try
        {
            // Bloquea el horario para evitar que dos inscripciones
            // simultáneas ocupen los mismos cupos.
            var conexion = _contexto.Database.GetDbConnection();

            await using var comando = conexion.CreateCommand();

            comando.Transaction =
                _contexto.Database.CurrentTransaction!.GetDbTransaction();

            comando.CommandText =
                "SELECT Id FROM horarios WHERE Id = @id FOR UPDATE";

            var parametro = comando.CreateParameter();
            parametro.ParameterName = "@id";
            parametro.Value = horarioId;
            comando.Parameters.Add(parametro);

            var resultado = await comando.ExecuteScalarAsync();

            if (resultado == null || resultado == DBNull.Value)
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El horario seleccionado no existe."
                });

            var horario = await _contexto.Horarios
                .Include(h => h.Clase)
                .FirstOrDefaultAsync(h => h.Id == horarioId);

            if (horario == null)
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El horario seleccionado no existe."
                });

            var membresia = await _contexto.Membresias
                .Where(m =>
                    m.ClienteId == clienteId &&
                    m.Estado == EstadoMembresia.Activa &&
                    m.FechaInicio.Date <= DateTime.Today &&
                    m.FechaFin.Date >= DateTime.Today)
                .OrderByDescending(m => m.FechaFin)
                .FirstOrDefaultAsync();

            if (membresia == null)
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El cliente no tiene una membresía activa y vigente hoy."
                });

            var clienteExiste = await _contexto.Clientes
                .AnyAsync(c => c.Id == clienteId);

            if (!clienteExiste)
                return BadRequest(new
                {
                    success = false,
                    mensaje = "El cliente seleccionado no existe."
                });

            var fechaInicio = DateTime.Today;
            var fechaFin = membresia.FechaFin.Date;

            var turnosExistentes = await _contexto.Turnos
                .Where(t =>
                    t.HorarioId == horarioId &&
                    t.Fecha >= fechaInicio &&
                    t.Fecha <= fechaFin)
            .ToListAsync();

            var fechasRegistradas = new List<string>();
            var fechasOmitidas = new List<string>();

            for (var fecha = fechaInicio; fecha <= fechaFin; fecha = fecha.AddDays(1))
            {
                if (fecha.DayOfWeek != horario.DiaSemana)
                    continue;

                var fechaTexto = fecha.ToString("yyyy-MM-dd");

                var turnoExistente = turnosExistentes.FirstOrDefault(t =>
                    t.ClienteId == clienteId &&
                    t.Fecha.Date == fecha);

                // Si ya existe una reserva para ese cliente y fecha,
                // se omite si no está cancelada.
                if (turnoExistente != null &&
                    turnoExistente.Estado != EstadoTurno.Cancelado)
                {
                    fechasOmitidas.Add(
                        $"{fechaTexto}: el cliente ya estaba inscripto.");
                    continue;
                }

                var inscriptos = turnosExistentes.Count(t =>
                    t.Fecha.Date == fecha &&
                    t.Estado == EstadoTurno.Reservado);

                if (inscriptos >= horario.Clase.CupoMaximo)
                {
                    fechasOmitidas.Add($"{fechaTexto}: cupo completo.");
                    continue;
                }

                if (turnoExistente != null)
                {
                    // Se reutiliza la reserva cancelada para evitar
                    // infringir el índice único de la base de datos.
                    turnoExistente.Estado = EstadoTurno.Reservado;
                }
                else
                {
                    var turno = new Turno
                    {
                        ClienteId = clienteId,
                        HorarioId = horarioId,
                        Fecha = fecha,
                        Estado = EstadoTurno.Reservado
                    };

                    _contexto.Turnos.Add(turno);
                    turnosExistentes.Add(turno);
                }

                fechasRegistradas.Add(fechaTexto);
            }

            await _contexto.SaveChangesAsync();
            await transaccion.CommitAsync();

            return Ok(new
            {
                success = true,
                mensaje = $"Inscripción finalizada. Reservas creadas: {fechasRegistradas.Count}. Fechas omitidas: {fechasOmitidas.Count}.",
                reservasCreadas = fechasRegistradas,
                fechasOmitidas
            });
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DarDeBaja(int turnoId)
    {
        if (turnoId <= 0)
            return BadRequest(new
            {
                success = false,
                mensaje = "La reserva seleccionada no es válida."
            });

        var turno = await _contexto.Turnos
            .FirstOrDefaultAsync(t => t.Id == turnoId);

        if (turno == null)
            return NotFound(new
            {
                success = false,
                mensaje = "La reserva no existe."
            });

        if (turno.Estado == EstadoTurno.Cancelado)
            return BadRequest(new
            {
                success = false,
                mensaje = "Esta reserva ya está cancelada."
            });

        if (turno.Estado == EstadoTurno.Asistio)
            return BadRequest(new
            {
                success = false,
                mensaje = "No se puede dar de baja una reserva a la que el cliente ya asistió."
            });

        turno.Estado = EstadoTurno.Cancelado;

        await _contexto.SaveChangesAsync();

        TempData["MensajeExito"] = "La reserva se dio de baja correctamente.";

        return RedirectToAction(nameof(Index));
    }

}
