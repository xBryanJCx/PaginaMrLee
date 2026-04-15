using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrLee.Web.Models;
using MrLee.Web.Services;
using System.Security.Claims;

namespace MrLee.Web.Controllers
{
    public class PortalController : Controller
    {
        private readonly ClienteService _svc;

        public PortalController(ClienteService svc) => _svc = svc;

        private int ClienteId => int.TryParse(User.FindFirstValue("ClienteId"), out var id) ? id : 0;
        private string ClienteEmail => User.FindFirstValue("ClienteEmail") ?? "";

        //  Página principal del portal 
        [AllowAnonymous]
        public IActionResult Index() => View();

        //  Registro 
        [AllowAnonymous]
        public IActionResult Registro() => View(new RegistroClienteVm());

        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public async Task<IActionResult> Registro(RegistroClienteVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var (ok, error) = await _svc.RegistrarAsync(vm);
            if (!ok) { ModelState.AddModelError("", error); return View(vm); }

            TempData["Msg"] = "¡Cuenta creada! Revisá tu correo para verificar tu cuenta.";
            return RedirectToAction(nameof(Login));
        }

        //  Verificar email 
        [AllowAnonymous]
        public async Task<IActionResult> VerificarEmail(string token)
        {
            var (ok, error) = await _svc.VerificarEmailAsync(token);
            ViewBag.Ok = ok;
            ViewBag.Error = error;
            return View();
        }

        //  Login 
        [AllowAnonymous]
        public IActionResult Login() => View(new LoginClienteVm());

        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public async Task<IActionResult> Login(LoginClienteVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var (ok, error, cliente) = await _svc.LoginAsync(vm);
            if (!ok) { ModelState.AddModelError("", error); return View(vm); }

            var claims = new List<Claim>
        {
            new("ClienteId",    cliente!.Id.ToString()),
            new("ClienteEmail", cliente.Email),
            new(ClaimTypes.Name, $"{cliente.Nombre} {cliente.Apellido}"),
            new(ClaimTypes.Role, "Cliente")
        };

            var props = new AuthenticationProperties
            {
                IsPersistent = vm.RememberMe,
                ExpiresUtc = vm.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(4)
            };

            await HttpContext.SignInAsync("ClienteCookie",
                new ClaimsPrincipal(new ClaimsIdentity(claims, "ClienteCookie")), props);

            return RedirectToAction(nameof(Panel));
        }

        //  Cerrar sesión 
        [AllowAnonymous]
        [AcceptVerbs("GET", "POST")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout(string? returnUrl = "/")
        {
            await HttpContext.SignOutAsync("ClienteCookie");

            var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : "/";

            return RedirectToAction("Login", "Account", new { ReturnUrl = safeReturnUrl });
        }

        //  Panel principal del cliente 
        [Authorize(AuthenticationSchemes = "ClienteCookie")]
        public async Task<IActionResult> Panel()
        {
            var cliente = await _svc.FindByIdAsync(ClienteId);
            if (cliente == null) return RedirectToAction(nameof(Login));
            return View(cliente);
        }

        //  Perfil 
        [Authorize(AuthenticationSchemes = "ClienteCookie")]
        public async Task<IActionResult> Perfil()
        {
            var c = await _svc.FindByIdAsync(ClienteId);
            if (c == null) return RedirectToAction(nameof(Login));

            var dir = c.Direcciones.FirstOrDefault(d => d.EsPrincipal);
            return View(new PerfilClienteVm
            {
                Nombre = c.Nombre,
                Apellido = c.Apellido,
                Telefono = c.Telefono,
                Provincia = dir?.Provincia ?? "",
                Canton = dir?.Canton ?? "",
                Distrito = dir?.Distrito ?? "",
                Direccion = dir?.Direccion ?? "",
                CodPostal = dir?.CodPostal ?? "",
                Lat = dir?.Lat,
                Lng = dir?.Lng
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ClienteCookie")]
        public async Task<IActionResult> Perfil(PerfilClienteVm vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var (ok, error) = await _svc.EditarPerfilAsync(ClienteId, vm);
            if (!ok) { ModelState.AddModelError("", error); return View(vm); }
            TempData["Msg"] = "Perfil actualizado correctamente.";
            return RedirectToAction(nameof(Panel));
        }

        //  Mis pedidos 
        [Authorize(AuthenticationSchemes = "ClienteCookie")]
        public async Task<IActionResult> MisPedidos(DateTime? fechaInicio, DateTime? fechaFin, OrderStatus? estado)
        {
            var pedidos = await _svc.MisPedidosAsync(ClienteId, fechaInicio, fechaFin, estado);
            var vm = new MisPedidosVm
            {
                Pedidos = pedidos,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                Estado = estado
            };
            return View(vm);
        }

        [Authorize(AuthenticationSchemes = "ClienteCookie")]
        public async Task<IActionResult> DetallePedido(long id)
        {
            var cliente = await _svc.FindByIdAsync(ClienteId);
            if (cliente == null) return RedirectToAction(nameof(Login));

            var pedidos = await _svc.MisPedidosAsync(ClienteId, null, null, null);
            var pedido = pedidos.FirstOrDefault(p => p.Id == id);

            if (pedido == null)
            {
                // Intento de acceso a pedido ajeno — auditar
                TempData["Error"] = "No tenés acceso a ese pedido.";
                return RedirectToAction(nameof(MisPedidos));
            }

            return View(pedido);
        }

        //  Recuperar contraseña 
        [AllowAnonymous]
        public IActionResult RecuperarPassword() => View(new RecuperarPasswordClienteVm());

        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public async Task<IActionResult> RecuperarPassword(RecuperarPasswordClienteVm vm)
        {
            if (!ModelState.IsValid) return View(vm);
            await _svc.SolicitarRecuperacionAsync(vm.Email);
            TempData["Msg"] = "Si el correo existe, recibirás un enlace en breve.";
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult RestablecerPassword(string token) =>
            View(new RestablecerPasswordClienteVm { Token = token });

        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public async Task<IActionResult> RestablecerPassword(RestablecerPasswordClienteVm vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var (ok, error) = await _svc.RestablecerPasswordAsync(vm);
            if (!ok) { ModelState.AddModelError("", error); return View(vm); }
            TempData["Msg"] = "Contraseña actualizada. Ya podés iniciar sesión.";
            return RedirectToAction(nameof(Login));
        }

        //  Preferencias notificación 
        [Authorize(AuthenticationSchemes = "ClienteCookie")]
        public async Task<IActionResult> Preferencias()
        {
            var c = await _svc.FindByIdAsync(ClienteId);
            if (c == null) return RedirectToAction(nameof(Login));
            return View(new PreferenciasNotificacionVm
            {
                NotiEmail = c.NotiEmail,
                NotiSms = c.NotiSms,
                NotiWhatsapp = c.NotiWhatsapp,
                HoraSilencioInicio = c.HoraSilencioInicio?.ToString(@"hh\:mm") ?? "",
                HoraSilencioFin = c.HoraSilencioFin?.ToString(@"hh\:mm") ?? ""
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ClienteCookie")]
        public async Task<IActionResult> Preferencias(PreferenciasNotificacionVm vm)
        {
            await _svc.GuardarPreferenciasAsync(ClienteId, vm);
            TempData["Msg"] = "Preferencias guardadas.";
            return RedirectToAction(nameof(Preferencias));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ClienteCookie")]
        public async Task<IActionResult> RestablecerPreferencias()
        {
            await _svc.RestablecerPreferenciasAsync(ClienteId);
            TempData["Msg"] = "Preferencias restablecidas a los valores por defecto.";
            return RedirectToAction(nameof(Preferencias));
        }

        //  Desactivar / Reactivar cuenta 
        [Authorize(AuthenticationSchemes = "ClienteCookie")]
        public IActionResult DesactivarCuenta() => View(new DesactivarCuentaVm());

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "ClienteCookie")]
        public async Task<IActionResult> DesactivarCuenta(DesactivarCuentaVm vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var (ok, error) = await _svc.DesactivarAsync(ClienteId, vm);
            if (!ok) { ModelState.AddModelError("", error); return View(vm); }

            await HttpContext.SignOutAsync("ClienteCookie");
            TempData["Msg"] = "Tu cuenta fue desactivada. Tenés 30 días para reactivarla.";
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Reactivar(string token)
        {
            var (ok, error) = await _svc.ReactivarAsync(token);
            ViewBag.Ok = ok;
            ViewBag.Error = error;
            return View();
        }
    }
}
