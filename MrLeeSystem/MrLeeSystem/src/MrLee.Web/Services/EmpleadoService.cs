using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Models;

namespace MrLee.Web.Services
{
    public class EmpleadoService
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;

        public EmpleadoService(AppDbContext db, AuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        //Catálogos
        public Task<List<Puesto>> GetPuestosActivosAsync() =>
            _db.Puestos.Where(p => p.IsActive).OrderBy(p => p.Nombre).ToListAsync();

        public Task<List<Sucursal>> GetSucursalesActivasAsync() =>
            _db.Sucursales.Where(s => s.IsActive).OrderBy(s => s.Nombre).ToListAsync();

        public Task<Puesto?> FindPuestoAsync(int id) =>
            _db.Puestos.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        public Task<Sucursal?> FindSucursalAsync(int id) =>
            _db.Sucursales.FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

        //Listado paginado
        public async Task<(List<Empleado> Items, int Total)> ListarAsync(
            string? criterio, EstadoEmpleado? estado, int? sucursalId,
            string ordenarPor, int pagina, int tamanoPagina)
        {
            var q = _db.Empleados
                .Include(e => e.Puesto)
                .Include(e => e.Sucursal)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(criterio))
            {
                var c = criterio.Trim().ToLower();
                q = q.Where(e =>
                    e.Nombre.ToLower().Contains(c) ||
                    e.Apellido.ToLower().Contains(c) ||
                    e.Codigo.ToLower().Contains(c) ||
                    e.Identificacion.ToLower().Contains(c) ||
                    e.Email.ToLower().Contains(c));
            }

            if (estado.HasValue)
                q = q.Where(e => e.Estado == estado.Value);

            if (sucursalId.HasValue)
                q = q.Where(e => e.SucursalId == sucursalId.Value);

            q = ordenarPor == "FECHA_INGRESO"
                ? q.OrderByDescending(e => e.FechaIngreso)
                : q.OrderBy(e => e.Apellido).ThenBy(e => e.Nombre);

            var total = await q.CountAsync();
            var items = await q.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToListAsync();

            return (items, total);
        }

        //Detalle
        public Task<Empleado?> FindAsync(int id) =>
            _db.Empleados
                .Include(e => e.Puesto)
                .Include(e => e.Sucursal)
                .FirstOrDefaultAsync(e => e.Id == id);

        // Crear
        public async Task<(bool Ok, string Error)> CrearAsync(EmpleadoFormVm vm, int actorId, string actorEmail)
        {
            // Validar catálogos
            if (await FindPuestoAsync(vm.PuestoId) == null)
                return (false, "El puesto seleccionado no existe o está inactivo.");
            if (await FindSucursalAsync(vm.SucursalId) == null)
                return (false, "La sucursal seleccionada no existe o está inactiva.");

            // Bloquear identificación duplicada
            if (await _db.Empleados.AnyAsync(e => e.Identificacion == vm.Identificacion))
                return (false, $"Ya existe un empleado con identificación '{vm.Identificacion}'.");

            // Fecha de ingreso no futura
            if (vm.FechaIngreso.Date > DateTime.Today)
                return (false, "La fecha de ingreso no puede ser futura.");

            var empleado = new Empleado
            {
                Codigo = vm.Codigo.Trim(),
                Nombre = vm.Nombre.Trim(),
                Apellido = vm.Apellido.Trim(),
                Identificacion = vm.Identificacion.Trim(),
                Email = vm.Email.Trim(),
                Telefono = vm.Telefono.Trim(),
                FechaIngreso = vm.FechaIngreso,
                PuestoId = vm.PuestoId,
                SalarioBase = vm.SalarioBase,
                TipoContrato = vm.TipoContrato,
                Jornada = vm.Jornada,
                SucursalId = vm.SucursalId,
                Estado = EstadoEmpleado.ACTIVO,
                Observaciones = vm.Observaciones.Trim(),
                DiasVacacionDisponibles = 0,
                CreatedByUserId = actorId,
                CreatedByEmail = actorEmail
            };

            _db.Empleados.Add(empleado);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(actorId, actorEmail, "CREAR", "Empleado", empleado.Id.ToString(),
                new { empleado.Codigo, empleado.Nombre, empleado.Apellido, empleado.Identificacion });

            return (true, "");
        }

        //Editar
        public async Task<(bool Ok, string Error)> EditarAsync(EmpleadoFormVm vm, int actorId, string actorEmail)
        {
            if (vm.EmpleadoId is null) return (false, "ID inválido.");

            var empleado = await FindAsync(vm.EmpleadoId.Value);
            if (empleado == null) return (false, "Empleado no encontrado.");

            // Catálogos
            if (await FindPuestoAsync(vm.PuestoId) == null)
                return (false, "El puesto seleccionado no existe o está inactivo.");
            if (await FindSucursalAsync(vm.SucursalId) == null)
                return (false, "La sucursal seleccionada no existe o está inactiva.");

            // Identificación duplicada en otro registro
            if (await _db.Empleados.AnyAsync(e => e.Identificacion == vm.Identificacion && e.Id != empleado.Id))
                return (false, $"Ya existe otro empleado con identificación '{vm.Identificacion}'.");

            empleado.Nombre = vm.Nombre.Trim();
            empleado.Apellido = vm.Apellido.Trim();
            empleado.Identificacion = vm.Identificacion.Trim();
            empleado.Email = vm.Email.Trim();
            empleado.Telefono = vm.Telefono.Trim();
            empleado.PuestoId = vm.PuestoId;
            empleado.SalarioBase = vm.SalarioBase;
            empleado.TipoContrato = vm.TipoContrato;
            empleado.Jornada = vm.Jornada;
            empleado.SucursalId = vm.SucursalId;
            empleado.Observaciones = vm.Observaciones.Trim();
            empleado.UpdatedAtUtc = DateTime.UtcNow;
            empleado.UpdatedByUserId = actorId;
            empleado.UpdatedByEmail = actorEmail;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(actorId, actorEmail, "EDITAR", "Empleado", empleado.Id.ToString(),
                new { empleado.Nombre, empleado.Apellido, empleado.Email, empleado.PuestoId, empleado.SucursalId });

            return (true, "");
        }

        // Cambio de estado 
        public async Task<(bool Ok, string Error)> CambiarEstadoAsync(CambioEstadoVm vm, int actorId, string actorEmail)
        {
            var empleado = await FindAsync(vm.EmpleadoId);
            if (empleado == null) return (false, "Empleado no encontrado.");

            // Estado inconsistente
            if (empleado.Estado == vm.NuevoEstado)
                return (false, $"El empleado ya se encuentra en estado '{vm.NuevoEstado}'.");

            empleado.Estado = vm.NuevoEstado;
            empleado.MotivoCambioEstado = vm.MotivoCambio.Trim();
            empleado.UpdatedAtUtc = DateTime.UtcNow;
            empleado.UpdatedByUserId = actorId;
            empleado.UpdatedByEmail = actorEmail;

            if (vm.NuevoEstado == EstadoEmpleado.INACTIVO)
                empleado.FechaSalida = vm.FechaSalida ?? DateTime.Today;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(actorId, actorEmail,
                vm.NuevoEstado == EstadoEmpleado.ACTIVO ? "REACTIVAR" : "INACTIVAR",
                "Empleado", empleado.Id.ToString(),
                new { vm.NuevoEstado, vm.MotivoCambio, empleado.FechaSalida });

            return (true, "");
        }

        // Exportar CSV
        public async Task<byte[]> ExportarCsvAsync(
            string? criterio, EstadoEmpleado? estado, int? sucursalId,
            string[] colsVisibles)
        {
            var (items, _) = await ListarAsync(criterio, estado, sucursalId, "NOMBRE", 1, 10000);

            var allCols = new Dictionary<string, Func<Empleado, string>>
            {
                ["Codigo"] = e => e.Codigo,
                ["Nombre"] = e => e.Nombre,
                ["Apellido"] = e => e.Apellido,
                ["Identificacion"] = e => e.Identificacion,
                ["Email"] = e => e.Email,
                ["Telefono"] = e => e.Telefono,
                ["FechaIngreso"] = e => e.FechaIngreso.ToString("dd/MM/yyyy"),
                ["Puesto"] = e => e.Puesto?.Nombre ?? "",
                ["SalarioBase"] = e => e.SalarioBase.ToString("N2"),
                ["TipoContrato"] = e => e.TipoContrato.ToString(),
                ["Jornada"] = e => e.Jornada.ToString(),
                ["Sucursal"] = e => e.Sucursal?.Nombre ?? "",
                ["Estado"] = e => e.Estado.ToString(),
            };

            var cols = colsVisibles.Length > 0
                ? colsVisibles.Where(c => allCols.ContainsKey(c)).ToArray()
                : allCols.Keys.ToArray();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Join(",", cols));

            foreach (var e in items)
                sb.AppendLine(string.Join(",", cols.Select(c => $"\"{allCols[c](e)}\"")));

            return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
