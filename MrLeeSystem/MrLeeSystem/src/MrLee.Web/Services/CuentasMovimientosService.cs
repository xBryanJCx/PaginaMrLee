using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Models;

namespace MrLee.Web.Services
{

    //  CUENTAS BANCARIAS

    public class CuentaBancariaService
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;

        public CuentaBancariaService(AppDbContext db, AuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        public Task<List<CuentaBancaria>> ListarAsync(int empleadoId, EstadoCuenta? estado = null) =>
            _db.CuentasBancarias
                .Where(c => c.EmpleadoId == empleadoId && (!estado.HasValue || c.Estado == estado.Value))
                .OrderByDescending(c => c.EsPrincipal)
                .ThenByDescending(c => c.CreatedAtUtc)
                .ToListAsync();

        public Task<CuentaBancaria?> FindAsync(int id) =>
            _db.CuentasBancarias.FirstOrDefaultAsync(c => c.Id == id);

        public async Task<(bool Ok, string Error)> GuardarAsync(CuentaBancariaFormVm vm, int actorId, string actorEmail)
        {
            // Verificar duplicado de número de cuenta para el mismo empleado
            var duplicado = await _db.CuentasBancarias.AnyAsync(c =>
                c.EmpleadoId == vm.EmpleadoId &&
                c.NumeroCuenta == vm.NumeroCuenta.Trim() &&
                (!vm.CuentaId.HasValue || c.Id != vm.CuentaId.Value));

            if (duplicado)
                return (false, "Ya existe una cuenta con ese número para este empleado.");

            CuentaBancaria cuenta;
            bool esNueva = !vm.CuentaId.HasValue;

            if (esNueva)
            {
                cuenta = new CuentaBancaria { EmpleadoId = vm.EmpleadoId, CreatedByUserId = actorId, CreatedByEmail = actorEmail };
                _db.CuentasBancarias.Add(cuenta);
            }
            else
            {
                cuenta = await FindAsync(vm.CuentaId!.Value) ?? throw new Exception("Cuenta no encontrada.");
                cuenta.UpdatedAtUtc = DateTime.UtcNow;
                cuenta.UpdatedByUserId = actorId;
                cuenta.UpdatedByEmail = actorEmail;
            }

            cuenta.Banco = vm.Banco.Trim();
            cuenta.TipoCuenta = vm.TipoCuenta;
            cuenta.Moneda = vm.Moneda;
            cuenta.NumeroCuenta = vm.NumeroCuenta.Trim();
            cuenta.Iban = vm.Iban.Trim();
            cuenta.EsPrincipal = vm.EsPrincipal;
            cuenta.Estado = EstadoCuenta.ACTIVA;

            if (vm.EsPrincipal)
            {
                var otras = await _db.CuentasBancarias
                    .Where(c => c.EmpleadoId == vm.EmpleadoId && c.EsPrincipal &&
                                (!vm.CuentaId.HasValue || c.Id != vm.CuentaId.Value))
                    .ToListAsync();
                otras.ForEach(c => c.EsPrincipal = false);
            }

            await _db.SaveChangesAsync();
            await _audit.LogAsync(actorId, actorEmail, esNueva ? "CREAR_CUENTA" : "EDITAR_CUENTA",
                "CuentaBancaria", cuenta.Id.ToString(), new { cuenta.Banco, cuenta.NumeroCuenta });

            return (true, "");
        }

        public async Task<(bool Ok, string Error)> InactivarAsync(InactivarCuentaVm vm, int actorId, string actorEmail)
        {
            var cuenta = await FindAsync(vm.CuentaId);
            if (cuenta == null) return (false, "Cuenta no encontrada.");
            if (cuenta.Estado == EstadoCuenta.INACTIVA) return (false, "La cuenta ya está inactiva.");

            cuenta.Estado = EstadoCuenta.INACTIVA;
            cuenta.MotivoInactivacion = vm.Motivo.Trim();
            cuenta.EsPrincipal = false;
            cuenta.UpdatedAtUtc = DateTime.UtcNow;
            cuenta.UpdatedByUserId = actorId;
            cuenta.UpdatedByEmail = actorEmail;

            await _db.SaveChangesAsync();
            await _audit.LogAsync(actorId, actorEmail, "INACTIVAR_CUENTA", "CuentaBancaria",
                cuenta.Id.ToString(), new { vm.Motivo });

            return (true, "");
        }
    }

    //  MOVIMIENTOS LABORALES

    public class MovimientoLaboralService
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;

        public MovimientoLaboralService(AppDbContext db, AuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        public Task<List<MovimientoLaboral>> HistorialAsync(int empleadoId) =>
            _db.MovimientosLaborales
                .Include(m => m.PuestoNuevo)
                .Include(m => m.SucursalNueva)
                .Where(m => m.EmpleadoId == empleadoId)
                .OrderByDescending(m => m.VigenciaDesde)
                .ToListAsync();

        public Task<MovimientoLaboral?> FindAsync(int id) =>
            _db.MovimientosLaborales
                .Include(m => m.PuestoNuevo)
                .Include(m => m.SucursalNueva)
                .FirstOrDefaultAsync(m => m.Id == id);

        public async Task<(bool Ok, string Error)> CrearAsync(MovimientoFormVm vm, int actorId, string actorEmail)
        {
            var empleado = await _db.Empleados.FindAsync(vm.EmpleadoId);
            if (empleado == null) return (false, "Empleado no encontrado.");

            // Guardar snapshot del estado anterior
            var puestoAnterior = empleado.PuestoId;
            var sucursalAnterior = empleado.SucursalId;

            // Cerrar movimiento vigente anterior si existe
            var vigente = await _db.MovimientosLaborales
                .Where(m => m.EmpleadoId == vm.EmpleadoId && m.Estado == EstadoMovimiento.VIGENTE)
                .FirstOrDefaultAsync();

            if (vigente != null)
            {
                vigente.Estado = EstadoMovimiento.CERRADO;
                vigente.VigenciaHasta = vm.VigenciaDesde.AddDays(-1);
                vigente.UpdatedAtUtc = DateTime.UtcNow;
            }

            var mov = new MovimientoLaboral
            {
                EmpleadoId = vm.EmpleadoId,
                PuestoIdNuevo = vm.PuestoIdNuevo,
                SucursalIdNueva = vm.SucursalIdNueva,
                PuestoIdAnterior = puestoAnterior,
                SucursalIdAnterior = sucursalAnterior,
                VigenciaDesde = vm.VigenciaDesde,
                VigenciaHasta = vm.VigenciaHasta,
                Motivo = vm.Motivo.Trim(),
                Estado = EstadoMovimiento.VIGENTE,
                CreatedByUserId = actorId,
                CreatedByEmail = actorEmail
            };

            _db.MovimientosLaborales.Add(mov);

            // Actualizar puesto y sucursal en el empleado
            empleado.PuestoId = vm.PuestoIdNuevo;
            empleado.SucursalId = vm.SucursalIdNueva;
            empleado.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(actorId, actorEmail, "CREAR_MOVIMIENTO", "MovimientoLaboral",
                mov.Id.ToString(), new { vm.PuestoIdNuevo, vm.SucursalIdNueva, vm.VigenciaDesde });

            return (true, "");
        }

        public async Task<(bool Ok, string Error)> EditarAsync(MovimientoFormVm vm, int actorId, string actorEmail)
        {
            if (vm.MovimientoId is null) return (false, "ID inválido.");

            var mov = await FindAsync(vm.MovimientoId.Value);
            if (mov == null) return (false, "Movimiento no encontrado.");
            if (mov.VigenciaDesde <= DateTime.Today)
                return (false, "Solo se pueden editar movimientos que aún no han iniciado.");

            mov.PuestoIdNuevo = vm.PuestoIdNuevo;
            mov.SucursalIdNueva = vm.SucursalIdNueva;
            mov.VigenciaDesde = vm.VigenciaDesde;
            mov.VigenciaHasta = vm.VigenciaHasta;
            mov.Motivo = vm.Motivo.Trim();
            mov.UpdatedAtUtc = DateTime.UtcNow;
            mov.UpdatedByUserId = actorId;
            mov.UpdatedByEmail = actorEmail;

            await _db.SaveChangesAsync();
            await _audit.LogAsync(actorId, actorEmail, "EDITAR_MOVIMIENTO", "MovimientoLaboral",
                mov.Id.ToString(), new { mov.PuestoIdNuevo, mov.SucursalIdNueva });

            return (true, "");
        }

        public async Task<(bool Ok, string Error)> AnularAsync(AnularMovimientoVm vm, int actorId, string actorEmail)
        {
            var mov = await FindAsync(vm.MovimientoId);
            if (mov == null) return (false, "Movimiento no encontrado.");
            if (mov.Estado == EstadoMovimiento.ANULADO) return (false, "El movimiento ya está anulado.");

            mov.Estado = EstadoMovimiento.ANULADO;
            mov.MotivoAnulacion = vm.MotivoAnulacion.Trim();
            mov.UpdatedAtUtc = DateTime.UtcNow;

            // Restituir puesto/sucursal anterior en el empleado si era el vigente
            if (mov.Estado == EstadoMovimiento.VIGENTE && mov.PuestoIdAnterior.HasValue)
            {
                var emp = await _db.Empleados.FindAsync(mov.EmpleadoId);
                if (emp != null)
                {
                    emp.PuestoId = mov.PuestoIdAnterior.Value;
                    emp.SucursalId = mov.SucursalIdAnterior ?? emp.SucursalId;
                    emp.UpdatedAtUtc = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            await _audit.LogAsync(actorId, actorEmail, "ANULAR_MOVIMIENTO", "MovimientoLaboral",
                mov.Id.ToString(), new { vm.MotivoAnulacion });

            return (true, "");
        }
    }
}
