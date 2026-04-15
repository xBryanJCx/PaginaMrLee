using MrLee.Web.Data;
using MrLee.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MrLee.Web.Services
{
    public class VacacionService
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;

        public VacacionService(AppDbContext db, AuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        // Crear solicitud 
        public async Task<(bool Ok, string Error)> CrearSolicitudAsync(SolicitudVacacionFormVm vm, int actorId, string actorEmail)
        {
            var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == vm.EmpleadoId && e.Estado == EstadoEmpleado.ACTIVO);
            if (empleado == null)
                return (false, "Empleado no encontrado o inactivo.");

            if (vm.FechaFin < vm.FechaInicio)
                return (false, "La fecha de fin no puede ser anterior a la fecha de inicio.");

            // Saldo insuficiente
            if (vm.DiasSolicitados > empleado.DiasVacacionDisponibles)
                return (false, $"Saldo insuficiente. Disponible: {empleado.DiasVacacionDisponibles} día(s).");

            // Solapamiento con aprobadas o incapacidades (solicitudes APROBADAS)
            var solapamiento = await _db.SolicitudesVacacion.AnyAsync(s =>
                s.EmpleadoId == vm.EmpleadoId &&
                s.Estado == EstadoSolicitudVacacion.APROBADA &&
                s.FechaInicio <= vm.FechaFin &&
                s.FechaFin >= vm.FechaInicio);

            if (solapamiento)
                return (false, "Existe otra solicitud aprobada que se solapa con las fechas indicadas.");

            var solicitud = new SolicitudVacacion
            {
                EmpleadoId = vm.EmpleadoId,
                FechaInicio = vm.FechaInicio,
                FechaFin = vm.FechaFin,
                DiasSolicitados = vm.DiasSolicitados,
                TipoDia = vm.TipoDia,
                Observaciones = vm.Observaciones.Trim(),
                Estado = EstadoSolicitudVacacion.SOLICITADA,
                CreatedByUserId = actorId,
                CreatedByEmail = actorEmail
            };

            _db.SolicitudesVacacion.Add(solicitud);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(actorId, actorEmail, "SOLICITAR_VACACION", "SolicitudVacacion",
                solicitud.Id.ToString(),
                new { vm.EmpleadoId, vm.FechaInicio, vm.FechaFin, vm.DiasSolicitados });

            return (true, "");
        }

        // Aprobar / Rechazar 
        public async Task<(bool Ok, string Error)> RevisarAsync(RevisionVacacionVm vm, int actorId, string actorEmail)
        {
            var solicitud = await _db.SolicitudesVacacion
                .Include(s => s.Empleado)
                .FirstOrDefaultAsync(s => s.Id == vm.SolicitudId);

            if (solicitud == null) return (false, "Solicitud no encontrada.");
            if (solicitud.Estado != EstadoSolicitudVacacion.SOLICITADA)
                return (false, "Solo se pueden revisar solicitudes en estado SOLICITADA.");

            if (vm.Decision == EstadoSolicitudVacacion.RECHAZADA && string.IsNullOrWhiteSpace(vm.MotivoRechazo))
                return (false, "Debe indicar el motivo de rechazo.");

            solicitud.Estado = vm.Decision;
            solicitud.MotivoRechazo = vm.MotivoRechazo.Trim();
            solicitud.RevisadoPorUserId = actorId;
            solicitud.RevisadoPorEmail = actorEmail;
            solicitud.RevisadoAtUtc = DateTime.UtcNow;

            // Si aprueba, descontar días del saldo
            if (vm.Decision == EstadoSolicitudVacacion.APROBADA)
            {
                solicitud.Empleado.DiasVacacionDisponibles =
                    Math.Max(0, solicitud.Empleado.DiasVacacionDisponibles - solicitud.DiasSolicitados);
            }

            await _db.SaveChangesAsync();

            await _audit.LogAsync(actorId, actorEmail,
                vm.Decision == EstadoSolicitudVacacion.APROBADA ? "APROBAR_VACACION" : "RECHAZAR_VACACION",
                "SolicitudVacacion", solicitud.Id.ToString(),
                new { vm.Decision, vm.MotivoRechazo });

            return (true, "");
        }

        //  Cancelar 
        public async Task<(bool Ok, string Error)> CancelarAsync(int solicitudId, int actorId, string actorEmail)
        {
            var solicitud = await _db.SolicitudesVacacion.FindAsync(solicitudId);
            if (solicitud == null) return (false, "Solicitud no encontrada.");
            if (solicitud.Estado != EstadoSolicitudVacacion.SOLICITADA)
                return (false, "Solo se pueden cancelar solicitudes en estado SOLICITADA.");
            if (solicitud.FechaInicio <= DateTime.Today)
                return (false, "No se puede cancelar una solicitud cuyo período ya inició.");

            solicitud.Estado = EstadoSolicitudVacacion.CANCELADA;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(actorId, actorEmail, "CANCELAR_VACACION", "SolicitudVacacion",
                solicitudId.ToString(), new { solicitud.EmpleadoId });

            return (true, "");
        }

        //Historial
        public Task<List<SolicitudVacacion>> HistorialAsync(int empleadoId, DateTime? desde, DateTime? hasta) =>
            _db.SolicitudesVacacion
                .Include(s => s.Empleado)
                .Where(s => s.EmpleadoId == empleadoId &&
                            (!desde.HasValue || s.FechaInicio >= desde.Value) &&
                            (!hasta.HasValue || s.FechaFin <= hasta.Value))
                .OrderByDescending(s => s.FechaInicio)
                .ToListAsync();

        //Pendientes para supervisor
        public Task<List<SolicitudVacacion>> PendientesAsync() =>
            _db.SolicitudesVacacion
                .Include(s => s.Empleado)
                .Where(s => s.Estado == EstadoSolicitudVacacion.SOLICITADA)
                .OrderBy(s => s.FechaInicio)
                .ToListAsync();
    }
}
