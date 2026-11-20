using System.Reflection;
using ExpressionBesoin.Web.Hebergement;
using ExpressionBesoin.Web.Sante;

// Composition seule : aucune décision métier (adaptateurs minces, règles de travail §1).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(new Demarrage(TimeProvider.System.GetUtcNow()));
builder.Services.AddSingleton(new VersionApplication(
    typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion));

var app = builder.Build();

if (!app.Environment.IsLocalhost() && !app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();
app.MapSante();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

/// <summary>Point d'entrée exposé aux tests d'intégration (<c>WebApplicationFactory&lt;Program&gt;</c>).</summary>
public partial class Program;
