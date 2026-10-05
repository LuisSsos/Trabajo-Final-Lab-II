using Gimnasio.Data;
using Gimnasio.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var cadenaConexion = builder.Configuration.GetConnectionString("Conexion");

builder.Services.AddDbContext<ContextoDatos>(opciones =>
    opciones.UseMySql(cadenaConexion, ServerVersion.AutoDetect(cadenaConexion)));

builder.Services.AddIdentity<Usuario, IdentityRole<int>>(opciones =>
{
    opciones.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ContextoDatos>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.LoginPath = "/Cuenta/Login";
    opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();