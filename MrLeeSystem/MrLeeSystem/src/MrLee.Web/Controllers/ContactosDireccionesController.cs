using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrLee.Web.Models;
using MrLee.Web.Security;
using MrLee.Web.Services;

namespace MrLee.Web.Controllers
{
    [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
    public class ContactosDireccionesController : Controller
    {
        private readonly ContactosDireccionesService _svc;
        private readonly EmpleadoService _empSvc;

        public ContactosDireccionesController(ContactosDireccionesService svc, EmpleadoService empSvc)
        { _svc = svc; _empSvc = empSvc; }

        private int ActorId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        private string ActorEmail => User.Identity?.Name ?? "";

        //  Listado combinado 
        public async Task<IActionResult> Index(int empleadoId, bool incluirEliminados = false)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();
            ViewBag.Empleado = emp;
            ViewBag.IncluirEliminados = incluirEliminados;
            ViewBag.Direcciones = await _svc.ListarDireccionesAsync(empleadoId, incluirEliminados);
            ViewBag.Contactos = await _svc.ListarContactosAsync(empleadoId, incluirEliminados);
            return View();
        }

        //  Direcciones 
        public async Task<IActionResult> CrearDireccion(int empleadoId)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();
            return View("DireccionForm", new DireccionFormVm { EmpleadoId = emp.Id, NombreEmpleado = $"{emp.Nombre} {emp.Apellido}" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearDireccion(DireccionFormVm vm)
        {
            if (!ModelState.IsValid) return View("DireccionForm", vm);
            var (ok, error) = await _svc.GuardarDireccionAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("DireccionForm", vm); }
            TempData["Msg"] = "Dirección guardada.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        public async Task<IActionResult> EditarDireccion(int id)
        {
            var dir = await _svc.FindDireccionAsync(id);
            if (dir == null) return NotFound();
            var emp = await _empSvc.FindAsync(dir.EmpleadoId);
            return View("DireccionForm", new DireccionFormVm
            {
                DireccionId = dir.Id,
                EmpleadoId = dir.EmpleadoId,
                NombreEmpleado = $"{emp?.Nombre} {emp?.Apellido}",
                Tipo = dir.Tipo,
                Provincia = dir.Provincia,
                Canton = dir.Canton,
                Distrito = dir.Distrito,
                Direccion = dir.Direccion,
                CodigoPostal = dir.CodigoPostal,
                Lat = dir.Lat,
                Lon = dir.Lon,
                EsPrincipal = dir.EsPrincipal
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarDireccion(DireccionFormVm vm)
        {
            if (!ModelState.IsValid) return View("DireccionForm", vm);
            var (ok, error) = await _svc.GuardarDireccionAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("DireccionForm", vm); }
            TempData["Msg"] = "Dirección actualizada.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        public async Task<IActionResult> EliminarDireccion(int id)
        {
            var dir = await _svc.FindDireccionAsync(id);
            if (dir == null) return NotFound();
            return View("EliminarRegistro", new EliminarRegistroVm { Id = id, EmpleadoId = dir.EmpleadoId, TipoRegistro = "DIRECCION" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarDireccion(EliminarRegistroVm vm)
        {
            if (!ModelState.IsValid) return View("EliminarRegistro", vm);
            var (ok, error) = await _svc.EliminarDireccionAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("EliminarRegistro", vm); }
            TempData["Msg"] = "Dirección eliminada.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        //  Contactos 
        public async Task<IActionResult> CrearContacto(int empleadoId)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();
            return View("ContactoForm", new ContactoFormVm { EmpleadoId = emp.Id, NombreEmpleado = $"{emp.Nombre} {emp.Apellido}" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearContacto(ContactoFormVm vm)
        {
            if (!ModelState.IsValid) return View("ContactoForm", vm);
            var (ok, error) = await _svc.GuardarContactoAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("ContactoForm", vm); }
            TempData["Msg"] = "Contacto guardado.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        public async Task<IActionResult> EditarContacto(int id)
        {
            var con = await _svc.FindContactoAsync(id);
            if (con == null) return NotFound();
            var emp = await _empSvc.FindAsync(con.EmpleadoId);
            return View("ContactoForm", new ContactoFormVm
            {
                ContactoId = con.Id,
                EmpleadoId = con.EmpleadoId,
                NombreEmpleado = $"{emp?.Nombre} {emp?.Apellido}",
                Nombre = con.Nombre,
                Parentesco = con.Parentesco,
                Telefono = con.Telefono,
                TelefonoAlt = con.TelefonoAlt,
                Email = con.Email,
                EsPrincipal = con.EsPrincipal
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarContacto(ContactoFormVm vm)
        {
            if (!ModelState.IsValid) return View("ContactoForm", vm);
            var (ok, error) = await _svc.GuardarContactoAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("ContactoForm", vm); }
            TempData["Msg"] = "Contacto actualizado.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        public async Task<IActionResult> EliminarContacto(int id)
        {
            var con = await _svc.FindContactoAsync(id);
            if (con == null) return NotFound();
            return View("EliminarRegistro", new EliminarRegistroVm { Id = id, EmpleadoId = con.EmpleadoId, TipoRegistro = "CONTACTO" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarContacto(EliminarRegistroVm vm)
        {
            if (!ModelState.IsValid) return View("EliminarRegistro", vm);
            var (ok, error) = await _svc.EliminarContactoAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View("EliminarRegistro", vm); }
            TempData["Msg"] = "Contacto eliminado.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }
    }
}
