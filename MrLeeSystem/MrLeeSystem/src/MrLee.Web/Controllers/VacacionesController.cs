using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrLee.Web.Models;
using MrLee.Web.Security;
using MrLee.Web.Services;

namespace MrLee.Web.Controllers
{
    [Authorize]
    public class VacacionesController : Controller
    {
        private readonly VacacionService _svc;
        private readonly EmpleadoService _empSvc;

        public VacacionesController(VacacionService svc, EmpleadoService empSvc)
        {
            _svc = svc;
            _empSvc = empSvc;
        }

        private int ActorId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        private string ActorEmail => User.Identity?.Name ?? "";

        //  Solicitar 
        [Authorize(Policy = PermissionCatalog.RRHH_VACACIONES)]
        public async Task<IActionResult> Solicitar(int empleadoId)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();

            var vm = new SolicitudVacacionFormVm
            {
                EmpleadoId = emp.Id,
                NombreEmpleado = $"{emp.Nombre} {emp.Apellido}",
                DiasDisponibles = emp.DiasVacacionDisponibles,
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCatalog.RRHH_VACACIONES)]
        public async Task<IActionResult> Solicitar(SolicitudVacacionFormVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var (ok, error) = await _svc.CrearSolicitudAsync(vm, ActorId, ActorEmail);
            if (!ok)
            {
                ModelState.AddModelError("", error);
                return View(vm);
            }

            TempData["Msg"] = "Solicitud de vacaciones enviada correctamente.";
            return RedirectToAction("Historial", new { empleadoId = vm.EmpleadoId });
        }

        //  Historial 
        [Authorize(Policy = PermissionCatalog.RRHH_VACACIONES)]
        public async Task<IActionResult> Historial(int empleadoId, DateTime? desde, DateTime? hasta)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();

            var items = await _svc.HistorialAsync(empleadoId, desde, hasta);
            ViewBag.Empleado = emp;
            ViewBag.Desde = desde;
            ViewBag.Hasta = hasta;
            return View(items);
        }

        //  Cancelar
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCatalog.RRHH_VACACIONES)]
        public async Task<IActionResult> Cancelar(int solicitudId, int empleadoId)
        {
            var (ok, error) = await _svc.CancelarAsync(solicitudId, ActorId, ActorEmail);
            TempData[ok ? "Msg" : "Error"] = ok ? "Solicitud cancelada." : error;
            return RedirectToAction("Historial", new { empleadoId });
        }

        //  Pendientes (supervisor)
        [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
        public async Task<IActionResult> Pendientes()
        {
            var items = await _svc.PendientesAsync();
            return View(items);
        }

        // Revisar (supervisor)
        [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
        public async Task<IActionResult> Revisar(int id)
        {
            var sol = (await _svc.PendientesAsync()).FirstOrDefault(s => s.Id == id);
            if (sol == null) return NotFound();

            var vm = new RevisionVacacionVm
            {
                SolicitudId = sol.Id,
                NombreEmpleado = $"{sol.Empleado.Nombre} {sol.Empleado.Apellido}",
                FechaInicio = sol.FechaInicio,
                FechaFin = sol.FechaFin,
                DiasSolicitados = sol.DiasSolicitados
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
        public async Task<IActionResult> Revisar(RevisionVacacionVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var (ok, error) = await _svc.RevisarAsync(vm, ActorId, ActorEmail);
            if (!ok)
            {
                ModelState.AddModelError("", error);
                return View(vm);
            }

            TempData["Msg"] = $"Solicitud {vm.Decision}.";
            return RedirectToAction(nameof(Pendientes));
        }
    }
}
