using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrLee.Web.Models;
using MrLee.Web.Security;
using MrLee.Web.Services;

namespace MrLee.Web.Controllers
{
    [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
    public class CuentasBancariasController : Controller
    {
        private readonly CuentaBancariaService _svc;
        private readonly EmpleadoService _empSvc;

        public CuentasBancariasController(CuentaBancariaService svc, EmpleadoService empSvc)
        { _svc = svc; _empSvc = empSvc; }

        private int ActorId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        private string ActorEmail => User.Identity?.Name ?? "";

        public async Task<IActionResult> Index(int empleadoId, EstadoCuenta? estado = null)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();
            ViewBag.Empleado = emp;
            ViewBag.Estado = estado;
            return View(await _svc.ListarAsync(empleadoId, estado));
        }

        public async Task<IActionResult> Crear(int empleadoId)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();
            return View("Form", new CuentaBancariaFormVm { EmpleadoId = emp.Id, NombreEmpleado = $"{emp.Nombre} {emp.Apellido}" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CuentaBancariaFormVm vm)
        {
            if (!ModelState.IsValid) return View("Form", vm);
            var (ok, error) = await _svc.GuardarAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("Form", vm); }
            TempData["Msg"] = "Cuenta bancaria registrada.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        public async Task<IActionResult> Editar(int id)
        {
            var c = await _svc.FindAsync(id);
            if (c == null) return NotFound();
            var emp = await _empSvc.FindAsync(c.EmpleadoId);
            return View("Form", new CuentaBancariaFormVm
            {
                CuentaId = c.Id,
                EmpleadoId = c.EmpleadoId,
                NombreEmpleado = $"{emp?.Nombre} {emp?.Apellido}",
                Banco = c.Banco,
                TipoCuenta = c.TipoCuenta,
                Moneda = c.Moneda,
                NumeroCuenta = c.NumeroCuenta,
                Iban = c.Iban,
                EsPrincipal = c.EsPrincipal,
                Estado = c.Estado
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(CuentaBancariaFormVm vm)
        {
            if (!ModelState.IsValid) return View("Form", vm);
            var (ok, error) = await _svc.GuardarAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("Form", vm); }
            TempData["Msg"] = "Cuenta actualizada.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        public async Task<IActionResult> Inactivar(int id)
        {
            var c = await _svc.FindAsync(id);
            if (c == null) return NotFound();
            return View(new InactivarCuentaVm { CuentaId = id, EmpleadoId = c.EmpleadoId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Inactivar(InactivarCuentaVm vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var (ok, error) = await _svc.InactivarAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View(vm); }
            TempData["Msg"] = "Cuenta inactivada.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }
    }
}
