using System.Globalization;
using WebApplication1.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<OpcionesCuadro>(builder.Configuration.GetSection(OpcionesCuadro.Seccion));

// Servicios: conexiones a las bases de datos (BigQuery por ODBC y SQL Server) y cálculo del cuadro de mando
builder.Services.AddSingleton<ServicioBigQuery>();
builder.Services.AddSingleton<ServicioSqlServer>();
builder.Services.AddSingleton<AlmacenCuadro>();
builder.Services.AddSingleton<ServicioTablero>();
builder.Services.AddSingleton<ServicioExportable>();
builder.Services.AddHostedService<ActualizadorCuadro>();

var app = builder.Build();

var cultura = CultureInfo.GetCultureInfo("es-ES");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
