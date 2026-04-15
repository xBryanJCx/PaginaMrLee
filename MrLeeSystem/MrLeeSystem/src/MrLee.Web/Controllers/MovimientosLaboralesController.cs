using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrLee.Web.Models;
using MrLee.Web.Security;
using MrLee.Web.Services;

namespace MrLee.Web.Controllers
{
    [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
    public class MovimientosLaboralesController : Controller
    {
        private readonly MovimientoLaboralService _svc;
        private readonly EmpleadoService _empSvc;

        public MovimientosLaboralesController(MovimientoLaboralService svc, EmpleadoService empSvc)
        { _svc = svc; _empSvc = empSvc; }

        private int ActorId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        private string ActorEmail => User.Identity?.Name ?? "";

        public async Task<IActionResult> Index(int empleadoId)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();
            ViewBag.Empleado = emp;
            return View(await _svc.HistorialAsync(empleadoId));
        }

        private async Task<MovimientoFormVm> PopularDropdownsAsync(MovimientoFormVm vm)
        {
            var puestos = await _empSvc.GetPuestosActivosAsync();
            var sucursales = await _empSvc.GetSucursalesActivasAsync();
            vm.Puestos = puestos.Select(p => new SelectListItem(p.Nombre, p.Id.ToString())).ToList();
            vm.Sucursales = sucursales.Select(s => new SelectListItem(s.Nombre, s.Id.ToString())).ToList();
            return vm;
        }

        public async Task<IActionResult> Crear(int empleadoId)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();
            var vm = new MovimientoFormVm
            {
                EmpleadoId = emp.Id,
                NombreEmpleado = $"{emp.Nombre} {emp.Apellido}",
                PuestoIdNuevo = emp.PuestoId,
                SucursalIdNueva = emp.SucursalId,
                VigenciaDesde = DateTime.Today
            };
            return View("Form", await PopularDropdownsAsync(vm));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(MovimientoFormVm vm)
        {
            if (!ModelState.IsValid) return View("Form", await PopularDropdownsAsync(vm));
            var (ok, error) = await _svc.CrearAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("Form", await PopularDropdownsAsync(vm)); }
            TempData["Msg"] = "Movimiento registrado.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        public async Task<IActionResult> Editar(int id)
        {
            var mov = await _svc.FindAsync(id);
            if (mov == null) return NotFound();
            var emp = await _empSvc.FindAsync(mov.EmpleadoId);
            var vm = new MovimientoFormVm
            {
                MovimientoId = mov.Id,
                EmpleadoId = mov.EmpleadoId,
                NombreEmpleado = $"{emp?.Nombre} {emp?.Apellido}",
                PuestoIdNuevo = mov.PuestoIdNuevo,
                SucursalIdNueva = mov.SucursalIdNueva,
                VigenciaDesde = mov.VigenciaDesde,
                VigenciaHasta = mov.VigenciaHasta,
                Motivo = mov.Motivo
            };
            return View("Form", await PopularDropdownsAsync(vm));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(MovimientoFormVm vm)
        {
            if (!ModelState.IsValid) return View("Form", await PopularDropdownsAsync(vm));
            var (ok, error) = await _svc.EditarAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("Form", await PopularDropdownsAsync(vm)); }
            TempData["Msg"] = "Movimiento actualizado.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        public async Task<IActionResult> Anular(int id)
        {
            var mov = await _svc.FindAsync(id);
            if (mov == null) return NotFound();
            return View(new AnularMovimientoVm { MovimientoId = id, EmpleadoId = mov.EmpleadoId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Anular(AnularMovimientoVm vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var (ok, error) = await _svc.AnularAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View(vm); }
            TempData["Msg"] = "Movimiento anulado.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }
    }
}
