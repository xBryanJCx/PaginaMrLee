using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Models;

namespace MrLee.Web.Services
{
    public class IncapacidadService
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;

        public IncapacidadService(AppDbContext db, AuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        public Task<List<Incapacidad>> ListarPorEmpleadoAsync(int empleadoId) =>
            _db.Incapacidades
                .Where(i => i.EmpleadoId == empleadoId)
                .OrderByDescending(i => i.FechaInicio)
                .ToListAsync();

        public Task<Incapacidad?> FindAsync(int id) =>
            _db.Incapacidades.Include(i => i.Empleado).FirstOrDefaultAsync(i => i.Id == id);

        private async Task<bool> HaySolapamientoAsync(int empleadoId, DateTime inicio, DateTime fin, int? excluirId = null)
        {
            // Solapamiento con otras incapacidades
            var solapaInc = await _db.Incapacidades.AnyAsync(i =>
                i.EmpleadoId == empleadoId &&
                i.Estado != EstadoIncapacidad.FINALIZADA &&
                (!excluirId.HasValue || i.Id != excluirId.Value) &&
                i.FechaInicio <= fin && i.FechaFin >= inicio);

            if (solapaInc) return true;

            // Solapamiento con vacaciones aprobadas
            var solapaVac = await _db.SolicitudesVacacion.AnyAsync(v =>
                v.EmpleadoId == empleadoId &&
                v.Estado == EstadoSolicitudVacacion.APROBADA &&
                v.FechaInicio <= fin && v.FechaFin >= inicio);

            return solapaVac;
        }

        public async Task<(bool Ok, string Error)> CrearAsync(IncapacidadFormVm vm, int actorId, string actorEmail)
        {
            if (vm.FechaFin < vm.FechaInicio)
                return (false, "La fecha de fin no puede ser anterior a la fecha de inicio.");

            if (await HaySolapamientoAsync(vm.EmpleadoId, vm.FechaInicio, vm.FechaFin))
                return (false, "El rango de fechas se solapa con una incapacidad vigente o vacaciones aprobadas.");

            var inc = new Incapacidad
            {
                EmpleadoId = vm.EmpleadoId,
                FechaInicio = vm.FechaInicio,
                FechaFin = vm.FechaFin,
                Diagnostico = vm.Diagnostico.Trim(),
                TipoDocumento = vm.TipoDocumento,
                DocumentoUrl = vm.DocumentoUrl.Trim(),
                CentroMedico = vm.CentroMedico.Trim(),
                NumeroOrden = vm.NumeroOrden.Trim(),
                Estado = EstadoIncapacidad.REGISTRADA,
                Observaciones = vm.Observaciones.Trim(),
                CreatedByUserId = actorId,
                CreatedByEmail = actorEmail
            };

            _db.Incapacidades.Add(inc);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(actorId, actorEmail, "CREAR", "Incapacidad", inc.Id.ToString(),
                new { inc.EmpleadoId, inc.FechaInicio, inc.FechaFin, inc.CentroMedico });

            return (true, "");
        }

        public async Task<(bool Ok, string Error)> EditarAsync(IncapacidadFormVm vm, int actorId, string actorEmail)
        {
            if (vm.IncapacidadId is null) return (false, "ID inválido.");

            var inc = await FindAsync(vm.IncapacidadId.Value);
            if (inc == null) return (false, "Incapacidad no encontrada.");

            if (vm.FechaFin < vm.FechaInicio)
                return (false, "La fecha de fin no puede ser anterior a la fecha de inicio.");

            if (await HaySolapamientoAsync(vm.EmpleadoId, vm.FechaInicio, vm.FechaFin, vm.IncapacidadId))
                return (false, "El rango de fechas se solapa con otra incapacidad o vacaciones aprobadas.");

            inc.FechaInicio = vm.FechaInicio;
            inc.FechaFin = vm.FechaFin;
            inc.Diagnostico = vm.Diagnostico.Trim();
            inc.TipoDocumento = vm.TipoDocumento;
            inc.CentroMedico = vm.CentroMedico.Trim();
            inc.NumeroOrden = vm.NumeroOrden.Trim();
            inc.Estado = vm.Estado;
            inc.Observaciones = vm.Observaciones.Trim();
            inc.UpdatedAtUtc = DateTime.UtcNow;
            inc.UpdatedByUserId = actorId;
            inc.UpdatedByEmail = actorEmail;

            if (!string.IsNullOrWhiteSpace(vm.DocumentoUrl))
                inc.DocumentoUrl = vm.DocumentoUrl.Trim();

            await _db.SaveChangesAsync();

            var accion = vm.Estado == EstadoIncapacidad.FINALIZADA ? "FINALIZAR" : "EDITAR";
            await _audit.LogAsync(actorId, actorEmail, accion, "Incapacidad", inc.Id.ToString(),
                new { inc.Estado, inc.FechaFin });

            return (true, "");
        }
    }
}
