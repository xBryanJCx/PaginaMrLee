using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrLee.Web.Models;
using MrLee.Web.Security;
using MrLee.Web.Services;

namespace MrLee.Web.Controllers
{
    [Authorize(Policy = PermissionCatalog.RRHH_MANAGE)]
    public class DocumentosExpedienteController : Controller
    {
        private readonly DocumentoExpedienteService _svc;
        private readonly EmpleadoService _empSvc;
        private readonly AuditService _audit;

        public DocumentosExpedienteController(DocumentoExpedienteService svc, EmpleadoService empSvc, AuditService audit)
        { _svc = svc; _empSvc = empSvc; _audit = audit; }

        private int ActorId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : 0;
        private string ActorEmail => User.Identity?.Name ?? "";

        public async Task<IActionResult> Index(int empleadoId, TipoDocumentoExpediente? tipo = null)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();
            ViewBag.Empleado = emp;
            ViewBag.Tipo = tipo;
            return View(await _svc.ListarAsync(empleadoId, tipo));
        }

        public async Task<IActionResult> Subir(int empleadoId)
        {
            var emp = await _empSvc.FindAsync(empleadoId);
            if (emp == null) return NotFound();
            return View(new DocumentoFormVm { EmpleadoId = emp.Id, NombreEmpleado = $"{emp.Nombre} {emp.Apellido}" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Subir(DocumentoFormVm vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var (ok, error, _) = await _svc.SubirAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View(vm); }
            TempData["Msg"] = "Documento subido correctamente.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        public async Task<IActionResult> Eliminar(int id)
        {
            var doc = await _svc.FindAsync(id);
            if (doc == null) return NotFound();
            return View(new EliminarDocumentoVm { DocumentoId = id, EmpleadoId = doc.EmpleadoId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(EliminarDocumentoVm vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var (ok, error) = await _svc.EliminarAsync(vm, ActorId, ActorEmail);
            if (!ok) { ModelState.AddModelError("", error); return View(vm); }
            TempData["Msg"] = "Documento eliminado.";
            return RedirectToAction(nameof(Index), new { empleadoId = vm.EmpleadoId });
        }

        [Authorize(Policy = PermissionCatalog.RRHH_VIEW)]
        public async Task<IActionResult> Descargar(int id)
        {
            var doc = await _svc.FindAsync(id);
            if (doc == null || doc.IsDeleted) return NotFound();
            await _audit.LogAsync(ActorId, ActorEmail, "DESCARGAR_DOCUMENTO", "DocumentoExpediente",
                id.ToString(), new { doc.DocumentoTipo });
            return Redirect(doc.DocumentoUrl);
        }
    }
}
