using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrLee.Web.Models;
using MrLee.Web.Security;
using MrLee.Web.Services;

namespace MrLee.Web.Controllers
{
    [Authorize]
    public class EmpleadosController : Controller
    {
        private readonly EmpleadoService _svc;
        private readonly AuditService _audit;

        public EmpleadosController(EmpleadoService svc, AuditService audit)
        {
            _svc = svc;
            _audit = audit;
        }

        //Helpers 
        private int ActorId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        private string ActorEmail => User.Identity?.Name ?? "";

        private async Task<EmpleadoFormVm> PopulateDropdownsAsync(EmpleadoFormVm vm)
        {
            var puestos = await _svc.GetPuestosActivosAsync();
            var sucursales = await _svc.GetSucursalesActivasAsync();

            vm.Puestos = puestos.Select(p => new SelectListItem(p.Nombre, p.Id.ToString())).ToList();
            vm.Sucursales = sucursales.Select(s => new SelectListItem(s.Nombre, s.Id.ToString())).ToList();
            return vm;
        }

        //  Listar
        [Authorize(Policy = PermissionCatalog.RRHH_VIEW)]
        public async Task<IActionResult> Index(string? criterio, EstadoEmpleado? estado,
            int? sucursalId, string ordenarPor = "NOMBRE", int pagina = 1)
        {
            var sucursales = await _svc.GetSucursalesActivasAsync();
            var (items, total) = await _svc.ListarAsync(criterio, estado, sucursalId, ordenarPor, pagina, 15);

            var vm = new EmpleadoListVm
            {
                Items = items,
                TotalItems = total,
                Criterio = criterio,
                Estado = estado,
                SucursalId = sucursalId,
                OrdenarPor = ordenarPor,
                Pagina = pagina,
                Sucursales = sucursales.Select(s => new SelectListItem(s.Nombre, s.Id.ToString())).ToList()
            };
            return View(vm);
        }

        //Ver detalle
        [Authorize(Policy = PermissionCatalog.RRHH_VIEW)]
        public async Task<IActionResult> Detalle(int id)
        {
            var empleado = await _svc.FindAsync(id);
            if (empleado == null) return NotFound();

            // Auditar intento de consulta
            await _audit.LogAsync(ActorId, ActorEmail, "VER_DETALLE", "Empleado", id.ToString(), new { });
            return View(empleado);
        }

        // Crear
        [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
        public async Task<IActionResult> Crear()
        {
            var vm = await PopulateDropdownsAsync(new EmpleadoFormVm());
            return View("Form", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
        public async Task<IActionResult> Crear(EmpleadoFormVm vm)
        {
            if (!ModelState.IsValid)
                return View("Form", await PopulateDropdownsAsync(vm));

            var (ok, error) = await _svc.CrearAsync(vm, ActorId, ActorEmail);
            if (!ok)
            {
                ModelState.AddModelError("", error);
                return View("Form", await PopulateDropdownsAsync(vm));
            }

            TempData["Msg"] = "Empleado creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        //Editar
        [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
        public async Task<IActionResult> Editar(int id)
        {
            var e = await _svc.FindAsync(id);
            if (e == null) return NotFound();

            var vm = new EmpleadoFormVm
            {
                EmpleadoId = e.Id,
                Codigo = e.Codigo,
                Nombre = e.Nombre,
                Apellido = e.Apellido,
                Identificacion = e.Identificacion,
                Email = e.Email,
                Telefono = e.Telefono,
                FechaIngreso = e.FechaIngreso,
                PuestoId = e.PuestoId,
                SalarioBase = e.SalarioBase,
                TipoContrato = e.TipoContrato,
                Jornada = e.Jornada,
                SucursalId = e.SucursalId,
                Estado = e.Estado,
                Observaciones = e.Observaciones
            };
            return View("Form", await PopulateDropdownsAsync(vm));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
        public async Task<IActionResult> Editar(EmpleadoFormVm vm)
        {
            if (!ModelState.IsValid)
                return View("Form", await PopulateDropdownsAsync(vm));

            var (ok, error) = await _svc.EditarAsync(vm, ActorId, ActorEmail);
            if (!ok)
            {
                ModelState.AddModelError("", error);
                return View("Form", await PopulateDropdownsAsync(vm));
            }

            TempData["Msg"] = "Empleado actualizado correctamente.";
            return RedirectToAction(nameof(Detalle), new { id = vm.EmpleadoId });
        }

        // Cambio de estado
        [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
        public async Task<IActionResult> CambiarEstado(int id)
        {
            var e = await _svc.FindAsync(id);
            if (e == null) return NotFound();

            var vm = new CambioEstadoVm
            {
                EmpleadoId = e.Id,
                NombreCompleto = $"{e.Nombre} {e.Apellido}",
                EstadoActual = e.Estado,
                NuevoEstado = e.Estado == EstadoEmpleado.ACTIVO
                    ? EstadoEmpleado.INACTIVO
                    : EstadoEmpleado.ACTIVO
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
        public async Task<IActionResult> CambiarEstado(CambioEstadoVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var (ok, error) = await _svc.CambiarEstadoAsync(vm, ActorId, ActorEmail);
            if (!ok)
            {
                ModelState.AddModelError("", error);
                return View(vm);
            }

            TempData["Msg"] = $"Estado actualizado a {vm.NuevoEstado}.";
            return RedirectToAction(nameof(Detalle), new { id = vm.EmpleadoId });
        }

        // Exportar CSV
        [Authorize(Policy = PermissionCatalog.RRHH_VIEW)]
        public async Task<IActionResult> Exportar(
            string? criterio, EstadoEmpleado? estado, int? sucursalId,
            string[] colsVisibles)
        {
            var bytes = await _svc.ExportarCsvAsync(criterio, estado, sucursalId, colsVisibles);
            return File(bytes, "text/csv", $"empleados_{DateTime.Today:yyyyMMdd}.csv");
        }
    }
}
