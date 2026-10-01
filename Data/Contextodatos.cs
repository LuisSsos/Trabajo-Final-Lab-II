using Gimnasio.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Gimnasio.Data;

public class ContextoDatos : IdentityDbContext<Usuario, IdentityRole<int>, int>
{
    public ContextoDatos(DbContextOptions<ContextoDatos> opciones) : base(opciones)
    {
    }

    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<Membresia> Membresias => Set<Membresia>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Clase> Clases => Set<Clase>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<Turno> Turnos => Set<Turno>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Empleado).WithOne(e => e.Usuario)
            .HasForeignKey<Empleado>(e => e.UsuarioId);

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Cliente).WithOne(c => c.Usuario)
            .HasForeignKey<Cliente>(c => c.UsuarioId);

        modelBuilder.Entity<Plan>().Property(p => p.Precio).HasPrecision(12, 2);
        modelBuilder.Entity<Pago>().Property(p => p.Monto).HasPrecision(12, 2);

        modelBuilder.Entity<Turno>()
            .HasIndex(t => new { t.HorarioId, t.ClienteId, t.Fecha })
            .IsUnique();

        modelBuilder.Entity<Turno>()
            .HasOne(t => t.Cliente).WithMany(c => c.Turnos)
            .HasForeignKey(t => t.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}