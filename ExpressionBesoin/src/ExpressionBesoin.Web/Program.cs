using System.Reflection;
using ExpressionBesoin.Web.Api;
using ExpressionBesoin.Web.Hebergement;
using ExpressionBesoin.Web.Sante;
using ExpressionBesoin.Web.Vite;

// Composition seule : aucune décision métier (adaptateurs minces, règles de travail §1).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(new Demarrage(TimeProvider.System.GetUtcNow()));
builder.Services.AddSingleton(new VersionApplication(
    typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion));
builder.Services.ConfigureHttpJsonOptions(options => ContratJson.Configurer(options.SerializerOptions));
builder.Services.AddSingleton<LecteurManifeste>();
builder.Services.AjouterFiches();

var app = builder.Build();

CompositionFiches.Initialiser(app.Services);

if (!app.Environment.IsLocalhost() && !app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapSante();
app.MapFiches();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

/// <summary>Point d'entrée exposé aux tests d'intégration (<c>WebApplicationFactory&lt;Program&gt;</c>).</summary>
public partial class Program;
