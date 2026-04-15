using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Security;
using MrLee.Web.Services;
using MrLee.Web.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "MrLee.Auth";
        options.SlidingExpiration = true;
    })
    .AddCookie("ClienteCookie", options =>
    {
        options.LoginPath = "/Portal/Login";
        options.AccessDeniedPath = "/Portal/Login";
        options.Cookie.Name = "MrLee.ClienteAuth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(4);
    });


builder.Services.AddAuthorization(options =>
{
    // Permission-based policies
    foreach (var p in PermissionCatalog.All)
    {
        options.AddPolicy(p, policy =>
            policy.Requirements.Add(new PermissionRequirement(p)));
    }
});

// Scoped (por request)
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<OperatingIncomeService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<EmpleadoService>();
builder.Services.AddScoped<VacacionService>();
builder.Services.AddScoped<IncapacidadService>();
builder.Services.AddScoped<DocumentoExpedienteService>();
builder.Services.AddScoped<ContactosDireccionesService>();
builder.Services.AddScoped<CuentaBancariaService>();
builder.Services.AddScoped<MovimientoLaboralService>();
builder.Services.AddScoped<ClienteService>();

var app = builder.Build();

// Ensure DB + seed (admin/roles/perms) on startup (safe for dev/demo)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await SeedData.EnsureSeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var mustChange = string.Equals(context.User.FindFirst("MustChangePassword")?.Value, "true", StringComparison.OrdinalIgnoreCase);
        var path = context.Request.Path.Value ?? "";
        var allow = path.StartsWith("/Account/ChangeTemporaryPassword", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/img", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/theme", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase);

        if (mustChange && !allow)
        {
            context.Response.Redirect("/Account/ChangeTemporaryPassword");
            return;
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
