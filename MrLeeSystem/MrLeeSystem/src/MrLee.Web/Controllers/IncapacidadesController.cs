using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrLee.Web.Models;
using MrLee.Web.Security;
using MrLee.Web.Services;

namespace MrLee.Web.Controllers
{
    [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
    public class IncapacidadesController : Controller
    {
        private readonly IncapacidadService _svc;
        private readonly EmpleadoService _empSvc;

        public IncapacidadesController(IncapacidadService svc, EmpleadoService empSvc)
        { _svc = svc; _empSvc = empSvc; }

        private int ActorId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        private string ActorEmail => User.Identity?.Name ?? "";

        public async Task<IActionResult> Index(int empleadoId)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();
            ViewBag.Empleado = emp;
            return View(await _svc.ListarPorEmpleadoAsync(empleadoId));
        }

        public async Task<IActionResult> Crear(int empleadoId)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();
            return View("Form", new IncapacidadFormVm
            {
                EmpleadoId = emp.Id,
                NombreEmpleado = $"{emp.Nombre} {emp.Apellido}",
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(IncapacidadFormVm vm)
        {
            if (!ModelState.IsValid) return View("Form", vm);
            var (ok, error) = await _svc.CrearAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("Form", vm); }
            TempData["Msg"] = "Incapacidad registrada correctamente.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        public async Task<IActionResult> Editar(int id)
        {
            var inc = await _svc.FindAsync(id);
            if (inc == null) return NotFound();
            return View("Form", new IncapacidadFormVm
            {
                IncapacidadId = inc.Id,
                EmpleadoId = inc.EmpleadoId,
                NombreEmpleado = $"{inc.Empleado.Nombre} {inc.Empleado.Apellido}",
                FechaInicio = inc.FechaInicio,
                FechaFin = inc.FechaFin,
                Diagnostico = inc.Diagnostico,
                TipoDocumento = inc.TipoDocumento,
                DocumentoUrl = inc.DocumentoUrl,
                CentroMedico = inc.CentroMedico,
                NumeroOrden = inc.NumeroOrden,
                Estado = inc.Estado,
                Observaciones = inc.Observaciones
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(IncapacidadFormVm vm)
        {
            if (!ModelState.IsValid) return View("Form", vm);
            var (ok, error) = await _svc.EditarAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("Form", vm); }
            TempData["Msg"] = "Incapacidad actualizada.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }
    }
}
