using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Models;

namespace MrLee.Web.Services
{
    public class DocumentoExpedienteService
    {
        private readonly AppDbContext _db;
        private readonly AuditService _audit;
        private readonly IWebHostEnvironment _env;

        public DocumentoExpedienteService(AppDbContext db, AuditService audit, IWebHostEnvironment env)
        {
            _db = db;
            _audit = audit;
            _env = env;
        }

        public Task<List<DocumentoExpediente>> ListarAsync(int empleadoId, TipoDocumentoExpediente? tipo = null) =>
            _db.DocumentosExpediente
                .Where(d => d.EmpleadoId == empleadoId && !d.IsDeleted &&
                            (!tipo.HasValue || d.DocumentoTipo == tipo.Value))
                .OrderByDescending(d => d.CreatedAtUtc)
                .ToListAsync();

        public Task<DocumentoExpediente?> FindAsync(int id) =>
            _db.DocumentosExpediente.FirstOrDefaultAsync(d => d.Id == id);

        public async Task<(bool Ok, string Error, string Url)> SubirAsync(
            DocumentoFormVm vm, int actorId, string actorEmail)
        {
            if (vm.Archivo == null || vm.Archivo.Length == 0)
                return (false, "Debe adjuntar un archivo.", "");

            var ext = Path.GetExtension(vm.Archivo.FileName).ToLower();
            var permitidos = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx" };
            if (!permitidos.Contains(ext))
                return (false, "Tipo de archivo no permitido. Use PDF, JPG, PNG o DOCX.", "");

            if (vm.Archivo.Length > 10 * 1024 * 1024)
                return (false, "El archivo excede el tamaño máximo de 10 MB.", "");

            // Determinar versión
            var version = 1;
            var anterior = await _db.DocumentosExpediente
                .Where(d => d.EmpleadoId == vm.EmpleadoId &&
                            d.DocumentoTipo == vm.DocumentoTipo &&
                            !d.IsDeleted)
                .OrderByDescending(d => d.Version)
                .FirstOrDefaultAsync();

            if (anterior != null)
                version = anterior.Version + 1;

            // Guardar archivo en disco
            var carpeta = Path.Combine(_env.WebRootPath, "uploads", "expedientes", vm.EmpleadoId.ToString());
            Directory.CreateDirectory(carpeta);

            var nombreArchivo = $"{vm.DocumentoTipo}_{version}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
            var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                await vm.Archivo.CopyToAsync(stream);

            var url = $"/uploads/expedientes/{vm.EmpleadoId}/{nombreArchivo}";

            var doc = new DocumentoExpediente
            {
                EmpleadoId = vm.EmpleadoId,
                DocumentoTipo = vm.DocumentoTipo,
                DocumentoUrl = url,
                NombreArchivo = vm.Archivo.FileName,
                Version = version,
                CreatedByUserId = actorId,
                CreatedByEmail = actorEmail
            };

            _db.DocumentosExpediente.Add(doc);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(actorId, actorEmail, "SUBIR_DOCUMENTO", "DocumentoExpediente",
                doc.Id.ToString(),
                new { doc.DocumentoTipo, doc.Version, doc.NombreArchivo });

            return (true, "", url);
        }

        public async Task<(bool Ok, string Error)> EliminarAsync(EliminarDocumentoVm vm, int actorId, string actorEmail)
        {
            var doc = await FindAsync(vm.DocumentoId);
            if (doc == null || doc.IsDeleted) return (false, "Documento no encontrado.");

            doc.IsDeleted = true;
            doc.MotivoEliminacion = vm.MotivoEliminacion.Trim();
            doc.DeletedAtUtc = DateTime.UtcNow;
            doc.DeletedByUserId = actorId;
            doc.DeletedByEmail = actorEmail;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(actorId, actorEmail, "ELIMINAR_DOCUMENTO", "DocumentoExpediente",
                doc.Id.ToString(),
                new { doc.DocumentoTipo, doc.Version, vm.MotivoEliminacion });

            return (true, "");
        }
    }
}
