using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MrLee.Web.Models
{
    //  Enums 
    public enum TipoIdentificacionCliente { CEDULA = 1, DIMEX = 2, PASAPORTE = 3 }

    //  Cliente 
    public class Cliente
    {
        public int Id { get; set; }

        [StringLength(100)] public string Nombre { get; set; } = "";
        [StringLength(100)] public string Apellido { get; set; } = "";
        [StringLength(200)] public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";

        public TipoIdentificacionCliente TipoIdentificacion { get; set; } = TipoIdentificacionCliente.CEDULA;
        [StringLength(30)] public string Identificacion { get; set; } = "";
        [StringLength(20)] public string Telefono { get; set; } = "";

        public bool EmailVerificado { get; set; } = false;
        public bool TelefonoVerificado { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Token de verificación de email
        [StringLength(200)] public string TokenVerificacion { get; set; } = "";
        public DateTime? TokenExpiraUtc { get; set; }

        // Token recuperación contraseña
        [StringLength(200)] public string TokenRecuperacion { get; set; } = "";
        public DateTime? TokenRecupExpiraUtc { get; set; }

        // Seguridad
        public int FailedLoginCount { get; set; } = 0;
        public DateTime? LockoutEndUtc { get; set; }

        // Baja lógica
        public bool DadoDeBaja { get; set; } = false;
        public DateTime? FechaBaja { get; set; }
        [StringLength(300)] public string MotivoBaja { get; set; } = "";
        [StringLength(200)] public string TokenReactivacion { get; set; } = "";
        public DateTime? TokenReactivacionExpiraUtc { get; set; }

        // Preferencias notificación
        public bool NotiEmail { get; set; } = true;
        public bool NotiSms { get; set; } = false;
        public bool NotiWhatsapp { get; set; } = false;
        public TimeSpan? HoraSilencioInicio { get; set; }
        public TimeSpan? HoraSilencioFin { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        [NotMapped]
        public DireccionCliente? DireccionPrincipal { get; set; }
        public List<DireccionCliente> Direcciones { get; set; } = new();
    }

    //  Dirección del cliente 
    public class DireccionCliente
    {
        public int Id { get; set; }

        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = default!;

        [StringLength(100)] public string Provincia { get; set; } = "";
        [StringLength(100)] public string Canton { get; set; } = "";
        [StringLength(100)] public string Distrito { get; set; } = "";
        [StringLength(300)] public string Direccion { get; set; } = "";
        [StringLength(10)] public string CodPostal { get; set; } = "";
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public bool EsPrincipal { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
