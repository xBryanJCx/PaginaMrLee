using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Models;
using MrLee.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MrLee.Web.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordService _pwd;
    private readonly AuditService _audit;
    private readonly EmailService _email;
    private readonly ILogger<AccountController> _logger;

    public AccountController(AppDbContext db, PasswordService pwd, AuditService audit, EmailService email, ILogger<AccountController> logger)
    {
        _db = db;
        _pwd = pwd;
        _audit = audit;
        _email = email;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginVm());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid) return View(vm);

        var normalizedEmail = vm.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        if (user == null)
        {
            ModelState.AddModelError("", "Credenciales inválidas.");
            return View(vm);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError("", "Usuario desactivado.");
            return View(vm);
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            ModelState.AddModelError("", "Usuario bloqueado temporalmente. Intente nuevamente más tarde.");
            return View(vm);
        }

        if (!_pwd.Verify(vm.Password, user.PasswordHash))
        {
            user.FailedLoginCount += 1;
            if (user.FailedLoginCount >= 5)
            {
                user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(15);
                user.FailedLoginCount = 0;
            }
            await _db.SaveChangesAsync();

            ModelState.AddModelError("", "Credenciales inválidas.");
            return View(vm);
        }

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        await _db.SaveChangesAsync();

        await SignInUserAsync(user);

        await _audit.LogAsync(user.Id, user.Email, "AUTH.LOGIN", "AppUser", user.Id.ToString(), new { user.Email, user.MustChangePassword });

        if (user.MustChangePassword)
        {
            TempData["Msg"] = "Debe cambiar la contraseña temporal antes de continuar.";
            return RedirectToAction(nameof(ChangeTemporaryPassword));
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordVm());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var normalizedEmail = vm.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.IsActive);

        if (user != null)
        {
            var temporaryPassword = _pwd.GenerateTemporaryPassword();
            user.PasswordHash = _pwd.HashPassword(temporaryPassword);
            user.MustChangePassword = true;
            user.TemporaryPasswordIssuedUtc = DateTime.UtcNow;
            user.FailedLoginCount = 0;
            user.LockoutEndUtc = null;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            try
            {
                var safeName = HtmlEncoder.Default.Encode(user.FullName);
                var safeEmail = HtmlEncoder.Default.Encode(user.Email);
                var safePwd = HtmlEncoder.Default.Encode(temporaryPassword);

                var html = $@"
<div style='font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#222'>
  <h2 style='margin-bottom:8px;'>Recuperación de acceso - Mr Lee System</h2>
  <p>Hola <strong>{safeName}</strong>,</p>
  <p>Se generó una contraseña temporal para tu cuenta.</p>
  <p><strong>Usuario:</strong> {safeEmail}<br/>
     <strong>Contraseña temporal:</strong> {safePwd}</p>
  <p>Al iniciar sesión el sistema te obligará a cambiarla inmediatamente.</p>
  <p>Si no solicitaste este cambio, informa al administrador.</p>
</div>";

                var text = $"Recuperación de acceso - Mr Lee System\n\nUsuario: {user.Email}\nContraseña temporal: {temporaryPassword}\n\nAl iniciar sesión debes cambiarla inmediatamente.";

                await _email.SendAsync(user.Email, "Mr Lee System - Contraseña temporal", html, text);

                await _audit.LogAsync(null, user.Email, "AUTH.FORGOT_PASSWORD", "AppUser", user.Id.ToString(), new { user.Email, Sent = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando correo de recuperación para {Email}", user.Email);
                ModelState.AddModelError("", "No se pudo enviar el correo. Revise la configuración SMTP y vuelva a intentar.");
                return View(vm);
            }
        }

        TempData["Msg"] = "Si el correo existe y está activo, se envió una contraseña temporal.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangeTemporaryPassword() => View(new ChangeTemporaryPasswordVm());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeTemporaryPassword(ChangeTemporaryPasswordVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        if (vm.NewPassword != vm.ConfirmPassword)
        {
            ModelState.AddModelError(nameof(vm.ConfirmPassword), "La confirmación no coincide.");
            return View(vm);
        }

        var userIdValue = User.FindFirstValue("UserId");
        if (!int.TryParse(userIdValue, out var userId))
            return RedirectToAction(nameof(Login));

        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return RedirectToAction(nameof(Login));

        if (_pwd.Verify(vm.NewPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(vm.NewPassword), "La nueva contraseña debe ser distinta a la temporal.");
            return View(vm);
        }

        user.PasswordHash = _pwd.HashPassword(vm.NewPassword);
        user.MustChangePassword = false;
        user.TemporaryPasswordIssuedUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(user.Id, user.Email, "AUTH.CHANGE_TEMP_PASSWORD", "AppUser", user.Id.ToString(), new { user.Email });

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await SignInUserAsync(user);

        TempData["Msg"] = "Contraseña actualizada correctamente.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue("UserId");
        await _audit.LogAsync(int.TryParse(userId, out var id) ? id : null, User.FindFirstValue(ClaimTypes.Email) ?? "",
            "AUTH.LOGOUT", "AppUser", userId ?? "");

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task SignInUserAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new("UserId", user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.Name),
            new("MustChangePassword", user.MustChangePassword ? "true" : "false")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}

public class LoginVm
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public class ForgotPasswordVm
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
    public string Email { get; set; } = "";
}

public class ChangeTemporaryPasswordVm
{
    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirme la nueva contraseña.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}