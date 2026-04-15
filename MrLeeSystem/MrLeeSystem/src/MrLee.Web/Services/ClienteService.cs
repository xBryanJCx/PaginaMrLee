using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Models;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace MrLee.Web.Services
{
    public class ClienteService
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;
        private readonly PasswordService _pwd;
        private readonly EmailService _email;
        private readonly IHttpContextAccessor _http;

        public ClienteService(AppDbContext db, AuditService audit, PasswordService pwd,
            EmailService email, IHttpContextAccessor http)
        {
            _db = db;
            _audit = audit;
            _pwd = pwd;
            _email = email;
            _http = http;
        }

        private string Ip => _http.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "";
        private string Ua => _http.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "";

        //  Helpers 
        private static string GenerarToken() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

        private static bool ValidarIdentificacion(TipoIdentificacionCliente tipo, string id)
        {
            return tipo switch
            {
                TipoIdentificacionCliente.CEDULA => Regex.IsMatch(id, @"^\d{9}$"),
                TipoIdentificacionCliente.DIMEX => Regex.IsMatch(id, @"^\d{11,12}$"),
                TipoIdentificacionCliente.PASAPORTE => Regex.IsMatch(id, @"^[A-Z0-9]{6,15}$", RegexOptions.IgnoreCase),
                _ => false
            };
        }

        public Task<Cliente?> FindByIdAsync(int id) =>
            _db.Clientes.Include(c => c.Direcciones).FirstOrDefaultAsync(c => c.Id == id);

        public Task<Cliente?> FindByEmailAsync(string email) =>
            _db.Clientes.Include(c => c.Direcciones)
                .FirstOrDefaultAsync(c => c.Email == email.Trim().ToLowerInvariant());

        //  Registro 
        public async Task<(bool Ok, string Error)> RegistrarAsync(RegistroClienteVm vm)
        {
            var email = vm.Email.Trim().ToLowerInvariant();

            if (await _db.Clientes.AnyAsync(c => c.Email == email))
                return (false, "Este correo ya está registrado.");

            if (!ValidarIdentificacion(vm.TipoIdentificacion, vm.Identificacion.Trim()))
                return (false, "El número de identificación no tiene el formato correcto.");

            var token = GenerarToken();
            var cliente = new Cliente
            {
                Nombre = vm.Nombre.Trim(),
                Apellido = vm.Apellido.Trim(),
                Email = email,
                PasswordHash = _pwd.HashPassword(vm.Password),
                TipoIdentificacion = vm.TipoIdentificacion,
                Identificacion = vm.Identificacion.Trim(),
                Telefono = vm.Telefono.Trim(),
                TokenVerificacion = token,
                TokenExpiraUtc = DateTime.UtcNow.AddHours(24),
                EmailVerificado = false,
                IsActive = true
            };

            _db.Clientes.Add(cliente);
            await _db.SaveChangesAsync();

            // Enviar correo verificación
            try
            {
                var link = $"{_http.HttpContext!.Request.Scheme}://{_http.HttpContext.Request.Host}/Portal/VerificarEmail?token={Uri.EscapeDataString(token)}";
                var html = $@"<div style='font-family:Arial,sans-serif;font-size:14px'>
<h2>¡Bienvenido a Mr Lee! 🥐</h2>
<p>Hola <strong>{cliente.Nombre}</strong>, verificá tu correo haciendo click en el enlace:</p>
<p><a href='{link}' style='background:#EAA636;color:#fff;padding:10px 20px;border-radius:6px;text-decoration:none'>Verificar correo</a></p>
<p>El enlace expira en 24 horas.</p></div>";
                await _email.SendAsync(email, "Verificá tu cuenta – Mr Lee", html);
            }
            catch { /* no bloquear si falla el SMTP en dev */ }

            await _audit.LogAsync(null, email, "CLIENTE.REGISTRO", "Cliente", cliente.Id.ToString(),
                new { email, Ip, Ua });

            return (true, "");
        }

        //  Verificar email 
        public async Task<(bool Ok, string Error)> VerificarEmailAsync(string token)
        {
            var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.TokenVerificacion == token);
            if (cliente == null) return (false, "Enlace inválido.");
            if (cliente.TokenExpiraUtc < DateTime.UtcNow) return (false, "El enlace expiró. Solicitá uno nuevo.");

            cliente.EmailVerificado = true;
            cliente.TokenVerificacion = "";
            cliente.TokenExpiraUtc = null;
            await _db.SaveChangesAsync();

            return (true, "");
        }

        //  Login 
        public async Task<(bool Ok, string Error, Cliente? Cliente)> LoginAsync(LoginClienteVm vm)
        {
            var email = vm.Email.Trim().ToLowerInvariant();
            var cliente = await FindByEmailAsync(email);

            if (cliente == null || !cliente.IsActive || cliente.DadoDeBaja)
                return (false, "Credenciales inválidas.", null);

            if (cliente.LockoutEndUtc.HasValue && cliente.LockoutEndUtc.Value > DateTime.UtcNow)
                return (false, "Cuenta bloqueada temporalmente. Intentá en unos minutos.", null);

            if (!_pwd.Verify(vm.Password, cliente.PasswordHash))
            {
                cliente.FailedLoginCount++;
                if (cliente.FailedLoginCount >= 5)
                {
                    cliente.LockoutEndUtc = DateTime.UtcNow.AddMinutes(15);
                    cliente.FailedLoginCount = 0;
                }
                await _db.SaveChangesAsync();
                return (false, "Credenciales inválidas.", null);
            }

            cliente.FailedLoginCount = 0;
            cliente.LockoutEndUtc = null;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(null, email, "CLIENTE.LOGIN", "Cliente", cliente.Id.ToString(),
                new { email, Ip, Ua });

            return (true, "", cliente);
        }

        //  Editar perfil 
        public async Task<(bool Ok, string Error)> EditarPerfilAsync(int clienteId, PerfilClienteVm vm)
        {
            var cliente = await FindByIdAsync(clienteId);
            if (cliente == null) return (false, "Cliente no encontrado.");

            // Correo duplicado
            if (!string.IsNullOrWhiteSpace(vm.EmailNuevo))
            {
                var emailNuevo = vm.EmailNuevo.Trim().ToLowerInvariant();
                if (await _db.Clientes.AnyAsync(c => c.Email == emailNuevo && c.Id != clienteId))
                    return (false, "Ese correo ya está en uso por otra cuenta.");
            }

            cliente.Nombre = vm.Nombre.Trim();
            cliente.Apellido = vm.Apellido.Trim();
            cliente.Telefono = vm.Telefono.Trim();
            cliente.UpdatedAtUtc = DateTime.UtcNow;

            // Dirección principal
            var dir = cliente.Direcciones.FirstOrDefault(d => d.EsPrincipal)
                      ?? new DireccionCliente { ClienteId = clienteId, EsPrincipal = true };

            dir.Provincia = vm.Provincia.Trim();
            dir.Canton = vm.Canton.Trim();
            dir.Distrito = vm.Distrito.Trim();
            dir.Direccion = vm.Direccion.Trim();
            dir.CodPostal = vm.CodPostal.Trim();
            dir.Lat = vm.Lat;
            dir.Lng = vm.Lng;
            dir.UpdatedAtUtc = DateTime.UtcNow;

            if (dir.Id == 0) _db.DireccionesCliente.Add(dir);

            await _db.SaveChangesAsync();
            await _audit.LogAsync(clienteId, cliente.Email, "CLIENTE.EDITAR_PERFIL", "Cliente",
                clienteId.ToString(), new { Ip, Ua });

            return (true, "");
        }

        //  Recuperar contraseña 
        public async Task SolicitarRecuperacionAsync(string email)
        {
            var norm = email.Trim().ToLowerInvariant();
            var cliente = await FindByEmailAsync(norm);

            // Respuesta genérica siempre (no revelar si existe)
            await _audit.LogAsync(null, norm, "CLIENTE.RECUPERACION_SOLICITADA", "Cliente",
                cliente?.Id.ToString() ?? "0", new { Ip, Ua });

            if (cliente == null || !cliente.IsActive) return;

            var token = GenerarToken();
            cliente.TokenRecuperacion = token;
            cliente.TokenRecupExpiraUtc = DateTime.UtcNow.AddHours(2);
            await _db.SaveChangesAsync();

            try
            {
                var link = $"{_http.HttpContext!.Request.Scheme}://{_http.HttpContext.Request.Host}/Portal/RestablecerPassword?token={Uri.EscapeDataString(token)}";
                var html = $@"<div style='font-family:Arial,sans-serif;font-size:14px'>
<h2>Restablecer contraseña – Mr Lee</h2>
<p>Hola <strong>{cliente.Nombre}</strong>, solicitaste cambiar tu contraseña.</p>
<p><a href='{link}' style='background:#EAA636;color:#fff;padding:10px 20px;border-radius:6px;text-decoration:none'>Restablecer contraseña</a></p>
<p>El enlace expira en 2 horas. Si no lo solicitaste ignorá este mensaje.</p></div>";
                await _email.SendAsync(norm, "Restablecer contraseña – Mr Lee", html);
            }
            catch { }
        }

        public async Task<(bool Ok, string Error)> RestablecerPasswordAsync(RestablecerPasswordClienteVm vm)
        {
            var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.TokenRecuperacion == vm.Token);
            if (cliente == null) return (false, "Enlace inválido.");
            if (cliente.TokenRecupExpiraUtc < DateTime.UtcNow) return (false, "El enlace expiró. Solicitá uno nuevo.");

            cliente.PasswordHash = _pwd.HashPassword(vm.PasswordNueva);
            cliente.TokenRecuperacion = "";
            cliente.TokenRecupExpiraUtc = null;
            cliente.FailedLoginCount = 0;
            cliente.LockoutEndUtc = null;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(cliente.Id, cliente.Email, "CLIENTE.RESTABLECER_PASSWORD",
                "Cliente", cliente.Id.ToString(), new { Ip, Ua });

            try
            {
                await _email.SendAsync(cliente.Email, "Contraseña actualizada – Mr Lee",
                "<p>Tu contraseña fue actualizada correctamente. Si no fuiste vos contactanos.</p>");
            }
            catch { }

            return (true, "");
        }

        //  Preferencias notificación 
        public async Task GuardarPreferenciasAsync(int clienteId, PreferenciasNotificacionVm vm)
        {
            var cliente = await _db.Clientes.FindAsync(clienteId);
            if (cliente == null) return;

            cliente.NotiEmail = vm.NotiEmail;
            cliente.NotiSms = vm.NotiSms;
            cliente.NotiWhatsapp = vm.NotiWhatsapp;

            if (TimeSpan.TryParse(vm.HoraSilencioInicio, out var inicio))
                cliente.HoraSilencioInicio = inicio;
            if (TimeSpan.TryParse(vm.HoraSilencioFin, out var fin))
                cliente.HoraSilencioFin = fin;

            await _db.SaveChangesAsync();
        }

        public async Task RestablecerPreferenciasAsync(int clienteId)
        {
            var cliente = await _db.Clientes.FindAsync(clienteId);
            if (cliente == null) return;
            cliente.NotiEmail = true; cliente.NotiSms = false; cliente.NotiWhatsapp = false;
            cliente.HoraSilencioInicio = null; cliente.HoraSilencioFin = null;
            await _db.SaveChangesAsync();
        }

        //  Desactivar / Reactivar cuenta 
        public async Task<(bool Ok, string Error)> DesactivarAsync(int clienteId, DesactivarCuentaVm vm)
        {
            var cliente = await FindByIdAsync(clienteId);
            if (cliente == null) return (false, "No encontrado.");
            if (!_pwd.Verify(vm.Password, cliente.PasswordHash)) return (false, "Contraseña incorrecta.");

            cliente.DadoDeBaja = true;
            cliente.IsActive = false;
            cliente.FechaBaja = DateTime.UtcNow;
            cliente.MotivoBaja = vm.MotivoBaja.Trim();

            var token = GenerarToken();
            cliente.TokenReactivacion = token;
            cliente.TokenReactivacionExpiraUtc = DateTime.UtcNow.AddDays(30);
            await _db.SaveChangesAsync();

            try
            {
                var link = $"{_http.HttpContext!.Request.Scheme}://{_http.HttpContext.Request.Host}/Portal/Reactivar?token={Uri.EscapeDataString(token)}";
                await _email.SendAsync(cliente.Email, "Cuenta desactivada – Mr Lee",
                    $"<p>Tu cuenta fue desactivada. Si querés reactivarla dentro de 30 días: <a href='{link}'>Reactivar cuenta</a></p>");
            }
            catch { }

            await _audit.LogAsync(clienteId, cliente.Email, "CLIENTE.DESACTIVAR", "Cliente",
                clienteId.ToString(), new { vm.MotivoBaja, Ip, Ua });

            return (true, "");
        }

        public async Task<(bool Ok, string Error)> ReactivarAsync(string token)
        {
            var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.TokenReactivacion == token);
            if (cliente == null) return (false, "Enlace inválido.");
            if (cliente.TokenReactivacionExpiraUtc < DateTime.UtcNow)
                return (false, "El período de reactivación expiró. Contactá soporte.");

            cliente.IsActive = true;
            cliente.DadoDeBaja = false;
            cliente.FechaBaja = null;
            cliente.TokenReactivacion = "";
            cliente.TokenReactivacionExpiraUtc = null;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(cliente.Id, cliente.Email, "CLIENTE.REACTIVAR", "Cliente",
                cliente.Id.ToString(), new { Ip, Ua });

            return (true, "");
        }

        //  Mis pedidos 
        public async Task<List<Order>> MisPedidosAsync(int clienteId, DateTime? desde,
            DateTime? hasta, OrderStatus? estado)
        {
            // Los pedidos se vinculan por email del cliente
            var cliente = await _db.Clientes.FindAsync(clienteId);
            if (cliente == null) return new();

            var q = _db.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Where(o => o.CustomerName == $"{cliente.Nombre} {cliente.Apellido}" ||
                            o.CustomerPhone == cliente.Telefono);

            if (desde.HasValue) q = q.Where(o => o.CreatedAtUtc >= desde.Value);
            if (hasta.HasValue) q = q.Where(o => o.CreatedAtUtc <= hasta.Value.AddDays(1));
            if (estado.HasValue) q = q.Where(o => o.Status == estado.Value);

            return await q.OrderByDescending(o => o.CreatedAtUtc).ToListAsync();
        }
    }
}
