using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Models;

namespace MrLee.Web.Services
{
    public class ContactosDireccionesService
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;

        public ContactosDireccionesService(AppDbContext db, AuditService audit)
        {
            _db = db;
            _audit = audit;
        }

        //Direcciones 
        public Task<List<DireccionEmpleado>> ListarDireccionesAsync(int empleadoId, bool incluirEliminados = false) =>
            _db.DireccionesEmpleado
                .Where(d => d.EmpleadoId == empleadoId && (incluirEliminados || !d.IsDeleted))
                .OrderByDescending(d => d.EsPrincipal)
                .ThenByDescending(d => d.CreatedAtUtc)
                .ToListAsync();

        public Task<DireccionEmpleado?> FindDireccionAsync(int id) =>
            _db.DireccionesEmpleado.FirstOrDefaultAsync(d => d.Id == id);

        public async Task<(bool Ok, string Error)> GuardarDireccionAsync(DireccionFormVm vm, int actorId, string actorEmail)
        {
            DireccionEmpleado dir;
            bool esNueva = !vm.DireccionId.HasValue;

            if (esNueva)
            {
                dir = new DireccionEmpleado { EmpleadoId = vm.EmpleadoId, CreatedByUserId = actorId, CreatedByEmail = actorEmail };
                _db.DireccionesEmpleado.Add(dir);
            }
            else
            {
                dir = await FindDireccionAsync(vm.DireccionId!.Value) ?? throw new Exception("Dirección no encontrada.");
                dir.UpdatedAtUtc = DateTime.UtcNow;
            }

            dir.Tipo = vm.Tipo;
            dir.Provincia = vm.Provincia.Trim();
            dir.Canton = vm.Canton.Trim();
            dir.Distrito = vm.Distrito.Trim();
            dir.Direccion = vm.Direccion.Trim();
            dir.CodigoPostal = vm.CodigoPostal.Trim();
            dir.Lat = vm.Lat;
            dir.Lon = vm.Lon;
            dir.EsPrincipal = vm.EsPrincipal;

            // Desmarcar principal anterior si aplica
            if (vm.EsPrincipal)
            {
                var otras = await _db.DireccionesEmpleado
                    .Where(d => d.EmpleadoId == vm.EmpleadoId && d.EsPrincipal && !d.IsDeleted &&
                                (!vm.DireccionId.HasValue || d.Id != vm.DireccionId.Value))
                    .ToListAsync();
                otras.ForEach(d => d.EsPrincipal = false);
            }

            await _db.SaveChangesAsync();
            await _audit.LogAsync(actorId, actorEmail, esNueva ? "CREAR_DIRECCION" : "EDITAR_DIRECCION",
                "DireccionEmpleado", dir.Id.ToString(), new { dir.Tipo, dir.EsPrincipal });

            return (true, "");
        }

        public async Task<(bool Ok, string Error)> EliminarDireccionAsync(EliminarRegistroVm vm, int actorId, string actorEmail)
        {
            var dir = await FindDireccionAsync(vm.Id);
            if (dir == null || dir.IsDeleted) return (false, "Dirección no encontrada.");

            dir.IsDeleted = true;
            dir.MotivoEliminacion = vm.Motivo.Trim();
            dir.DeletedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await _audit.LogAsync(actorId, actorEmail, "ELIMINAR_DIRECCION", "DireccionEmpleado",
                dir.Id.ToString(), new { vm.Motivo });

            return (true, "");
        }

        //Contactos de emergencia 
        public Task<List<ContactoEmergencia>> ListarContactosAsync(int empleadoId, bool incluirEliminados = false) =>
            _db.ContactosEmergencia
                .Where(c => c.EmpleadoId == empleadoId && (incluirEliminados || !c.IsDeleted))
                .OrderByDescending(c => c.EsPrincipal)
                .ThenByDescending(c => c.CreatedAtUtc)
                .ToListAsync();

        public Task<ContactoEmergencia?> FindContactoAsync(int id) =>
            _db.ContactosEmergencia.FirstOrDefaultAsync(c => c.Id == id);

        public async Task<(bool Ok, string Error)> GuardarContactoAsync(ContactoFormVm vm, int actorId, string actorEmail)
        {
            ContactoEmergencia con;
            bool esNuevo = !vm.ContactoId.HasValue;

            if (esNuevo)
            {
                con = new ContactoEmergencia { EmpleadoId = vm.EmpleadoId, CreatedByUserId = actorId, CreatedByEmail = actorEmail };
                _db.ContactosEmergencia.Add(con);
            }
            else
            {
                con = await FindContactoAsync(vm.ContactoId!.Value) ?? throw new Exception("Contacto no encontrado.");
                con.UpdatedAtUtc = DateTime.UtcNow;
            }

            con.Nombre = vm.Nombre.Trim();
            con.Parentesco = vm.Parentesco;
            con.Telefono = vm.Telefono.Trim();
            con.TelefonoAlt = vm.TelefonoAlt.Trim();
            con.Email = vm.Email.Trim();
            con.EsPrincipal = vm.EsPrincipal;

            if (vm.EsPrincipal)
            {
                var otros = await _db.ContactosEmergencia
                    .Where(c => c.EmpleadoId == vm.EmpleadoId && c.EsPrincipal && !c.IsDeleted &&
                                (!vm.ContactoId.HasValue || c.Id != vm.ContactoId.Value))
                    .ToListAsync();
                otros.ForEach(c => c.EsPrincipal = false);
            }

            await _db.SaveChangesAsync();
            await _audit.LogAsync(actorId, actorEmail, esNuevo ? "CREAR_CONTACTO" : "EDITAR_CONTACTO",
                "ContactoEmergencia", con.Id.ToString(), new { con.Nombre, con.Parentesco });

            return (true, "");
        }

        public async Task<(bool Ok, string Error)> EliminarContactoAsync(EliminarRegistroVm vm, int actorId, string actorEmail)
        {
            var con = await FindContactoAsync(vm.Id);
            if (con == null || con.IsDeleted) return (false, "Contacto no encontrado.");

            con.IsDeleted = true;
            con.MotivoEliminacion = vm.Motivo.Trim();
            con.DeletedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await _audit.LogAsync(actorId, actorEmail, "ELIMINAR_CONTACTO", "ContactoEmergencia",
                con.Id.ToString(), new { vm.Motivo });

            return (true, "");
        }
    }
}
