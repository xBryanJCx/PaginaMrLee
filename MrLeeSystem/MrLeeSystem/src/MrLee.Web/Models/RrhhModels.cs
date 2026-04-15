using System.ComponentModel.DataAnnotations;

namespace MrLee.Web.Models
{
    public class Puesto
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string Nombre { get; set; } = "";

        [StringLength(300)]
        public string Descripcion { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }

    public class Sucursal
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string Nombre { get; set; } = "";

        [StringLength(200)]
        public string Direccion { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }

    // Enums 
    public enum TipoContrato { INDEFINIDO = 1, FIJO = 2, SERVICIOS = 3 }
    public enum Jornada { COMPLETA = 1, PARCIAL = 2 }
    public enum EstadoEmpleado { ACTIVO = 1, INACTIVO = 2 }
    public enum TipoDiaVacacion { DIA = 1, MEDIO_DIA = 2 }
    public enum EstadoSolicitudVacacion { SOLICITADA = 1, APROBADA = 2, RECHAZADA = 3, CANCELADA = 4 }

    //  Empleado 
    public class Empleado
    {
        public int Id { get; set; }

        [StringLength(20)]
        public string Codigo { get; set; } = "";

        [StringLength(100)]
        public string Nombre { get; set; } = "";

        [StringLength(100)]
        public string Apellido { get; set; } = "";

        [StringLength(30)]
        public string Identificacion { get; set; } = "";

        [StringLength(200)]
        public string Email { get; set; } = "";

        [StringLength(20)]
        public string Telefono { get; set; } = "";

        public DateTime FechaIngreso { get; set; }

        public int PuestoId { get; set; }
        public Puesto Puesto { get; set; } = default!;

        public decimal SalarioBase { get; set; }

        public TipoContrato TipoContrato { get; set; } = TipoContrato.INDEFINIDO;
        public Jornada Jornada { get; set; } = Jornada.COMPLETA;

        public int SucursalId { get; set; }
        public Sucursal Sucursal { get; set; } = default!;

        public EstadoEmpleado Estado { get; set; } = EstadoEmpleado.ACTIVO;

        [StringLength(500)]
        public string Observaciones { get; set; } = "";

        public int DiasVacacionDisponibles { get; set; } = 0;

        public DateTime? FechaSalida { get; set; }

        [StringLength(300)]
        public string MotivoCambioEstado { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public int? CreatedByUserId { get; set; }
        public string CreatedByEmail { get; set; } = "";
        public int? UpdatedByUserId { get; set; }
        public string UpdatedByEmail { get; set; } = "";

        public List<SolicitudVacacion> SolicitudesVacacion { get; set; } = new();
    }

    //  Vacaciones
    public class SolicitudVacacion
    {
        public int Id { get; set; }

        public int EmpleadoId { get; set; }
        public Empleado Empleado { get; set; } = default!;

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int DiasSolicitados { get; set; }

        public TipoDiaVacacion TipoDia { get; set; } = TipoDiaVacacion.DIA;

        [StringLength(500)]
        public string Observaciones { get; set; } = "";

        public EstadoSolicitudVacacion Estado { get; set; } = EstadoSolicitudVacacion.SOLICITADA;

        [StringLength(300)]
        public string MotivoRechazo { get; set; } = "";

        public int? RevisadoPorUserId { get; set; }
        public string RevisadoPorEmail { get; set; } = "";
        public DateTime? RevisadoAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }
        public string CreatedByEmail { get; set; } = "";
    }

    //  INCAPACIDADES

    public enum TipoDocumentoIncapacidad { ORDEN = 1, CERTIFICADO = 2 }
    public enum EstadoIncapacidad { REGISTRADA = 1, VIGENTE = 2, FINALIZADA = 3 }

    public class Incapacidad
    {
        public int Id { get; set; }

        public int EmpleadoId { get; set; }
        public Empleado Empleado { get; set; } = default!;

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        [StringLength(500)]
        public string Diagnostico { get; set; } = "";

        public TipoDocumentoIncapacidad TipoDocumento { get; set; } = TipoDocumentoIncapacidad.ORDEN;

        [StringLength(500)]
        public string DocumentoUrl { get; set; } = "";

        [StringLength(200)]
        public string CentroMedico { get; set; } = "";

        [StringLength(50)]
        public string NumeroOrden { get; set; } = "";

        public EstadoIncapacidad Estado { get; set; } = EstadoIncapacidad.REGISTRADA;

        [StringLength(500)]
        public string Observaciones { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public int? CreatedByUserId { get; set; }
        public string CreatedByEmail { get; set; } = "";
        public int? UpdatedByUserId { get; set; }
        public string UpdatedByEmail { get; set; } = "";
    }

    //  DOCUMENTOS DEL EXPEDIENTE
    public enum TipoDocumentoExpediente
    {
        CONTRATO = 1,
        IDENTIFICACION = 2,
        CONSTANCIA = 3,
        INCAPACIDAD = 4
    }

    public class DocumentoExpediente
    {
        public int Id { get; set; }

        public int EmpleadoId { get; set; }
        public Empleado Empleado { get; set; } = default!;

        public TipoDocumentoExpediente DocumentoTipo { get; set; }

        [StringLength(500)]
        public string DocumentoUrl { get; set; } = "";

        [StringLength(200)]
        public string NombreArchivo { get; set; } = "";

        public int Version { get; set; } = 1;

        public bool IsDeleted { get; set; } = false;

        [StringLength(300)]
        public string MotivoEliminacion { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAtUtc { get; set; }
        public int? CreatedByUserId { get; set; }
        public string CreatedByEmail { get; set; } = "";
        public int? DeletedByUserId { get; set; }
        public string DeletedByEmail { get; set; } = "";
    }

    //  DIRECCIONES
    public enum TipoDireccion { DOMICILIO = 1, CORRESPONDENCIA = 2 }

    public class DireccionEmpleado
    {
        public int Id { get; set; }

        public int EmpleadoId { get; set; }
        public Empleado Empleado { get; set; } = default!;

        public TipoDireccion Tipo { get; set; } = TipoDireccion.DOMICILIO;

        [StringLength(100)] public string Provincia { get; set; } = "";
        [StringLength(100)] public string Canton { get; set; } = "";
        [StringLength(100)] public string Distrito { get; set; } = "";
        [StringLength(300)] public string Direccion { get; set; } = "";
        [StringLength(10)] public string CodigoPostal { get; set; } = "";

        public decimal? Lat { get; set; }
        public decimal? Lon { get; set; }

        public bool EsPrincipal { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        [StringLength(300)]
        public string MotivoEliminacion { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public int? CreatedByUserId { get; set; }
        public string CreatedByEmail { get; set; } = "";
    }

    //  CONTACTOS DE EMERGENCIA
    public enum Parentesco
    {
        CONYUGUE = 1,
        HIJO = 2,
        PADRE = 3,
        MADRE = 4,
        HERMANO = 5,
        OTRO = 6
    }

    public class ContactoEmergencia
    {
        public int Id { get; set; }

        public int EmpleadoId { get; set; }
        public Empleado Empleado { get; set; } = default!;

        [StringLength(150)] public string Nombre { get; set; } = "";
        public Parentesco Parentesco { get; set; } = Parentesco.OTRO;
        [StringLength(20)] public string Telefono { get; set; } = "";
        [StringLength(20)] public string TelefonoAlt { get; set; } = "";
        [StringLength(200)] public string Email { get; set; } = "";

        public bool EsPrincipal { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        [StringLength(300)]
        public string MotivoEliminacion { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public int? CreatedByUserId { get; set; }
        public string CreatedByEmail { get; set; } = "";
    }

    //  CUENTAS BANCARIAS
    public enum TipoCuenta { AHORROS = 1, CORRIENTE = 2 }
    public enum MonedaCuenta { CRC = 1, USD = 2 }
    public enum EstadoCuenta { ACTIVA = 1, INACTIVA = 2 }

    public class CuentaBancaria
    {
        public int Id { get; set; }

        public int EmpleadoId { get; set; }
        public Empleado Empleado { get; set; } = default!;

        [StringLength(150)] public string Banco { get; set; } = "";
        public TipoCuenta TipoCuenta { get; set; } = TipoCuenta.AHORROS;
        public MonedaCuenta Moneda { get; set; } = MonedaCuenta.CRC;
        [StringLength(50)] public string NumeroCuenta { get; set; } = "";
        [StringLength(30)] public string Iban { get; set; } = "";

        public bool EsPrincipal { get; set; } = false;
        public EstadoCuenta Estado { get; set; } = EstadoCuenta.ACTIVA;

        [StringLength(300)]
        public string MotivoInactivacion { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public int? CreatedByUserId { get; set; }
        public string CreatedByEmail { get; set; } = "";
        public int? UpdatedByUserId { get; set; }
        public string UpdatedByEmail { get; set; } = "";
    }

    //  MOVIMIENTOS DE PUESTO / SUCURSAL
    public enum EstadoMovimiento { VIGENTE = 1, CERRADO = 2, ANULADO = 3 }

    public class MovimientoLaboral
    {
        public int Id { get; set; }

        public int EmpleadoId { get; set; }
        public Empleado Empleado { get; set; } = default!;

        public int PuestoIdNuevo { get; set; }
        public Puesto PuestoNuevo { get; set; } = default!;

        public int SucursalIdNueva { get; set; }
        public Sucursal SucursalNueva { get; set; } = default!;

        // Puesto/sucursal anterior (snapshot al cerrar)
        public int? PuestoIdAnterior { get; set; }
        public int? SucursalIdAnterior { get; set; }

        public DateTime VigenciaDesde { get; set; }
        public DateTime? VigenciaHasta { get; set; }

        public EstadoMovimiento Estado { get; set; } = EstadoMovimiento.VIGENTE;

        [StringLength(400)]
        public string Motivo { get; set; } = "";

        [StringLength(400)]
        public string MotivoAnulacion { get; set; } = "";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public int? CreatedByUserId { get; set; }
        public string CreatedByEmail { get; set; } = "";
        public int? UpdatedByUserId { get; set; }
        public string UpdatedByEmail { get; set; } = "";
    }
}
