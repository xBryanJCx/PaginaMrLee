using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace MrLee.Web.Models
{
    // Empleado VM 
    public class EmpleadoFormVm
    {
        public int? EmpleadoId { get; set; }

        [Required(ErrorMessage = "El código es obligatorio.")]
        [StringLength(20)]
        public string Codigo { get; set; } = "";

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100)]
        public string Apellido { get; set; } = "";

        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [StringLength(30)]
        public string Identificacion { get; set; } = "";

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [StringLength(200)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]{7,20}$", ErrorMessage = "Formato de teléfono inválido.")]
        [StringLength(20)]
        public string Telefono { get; set; } = "";

        [Required(ErrorMessage = "La fecha de ingreso es obligatoria.")]
        public DateTime FechaIngreso { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Seleccione un puesto.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un puesto válido.")]
        public int PuestoId { get; set; }

        [Required(ErrorMessage = "El salario es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El salario debe ser mayor a cero.")]
        public decimal SalarioBase { get; set; }

        public TipoContrato TipoContrato { get; set; } = TipoContrato.INDEFINIDO;
        public Jornada Jornada { get; set; } = Jornada.COMPLETA;

        [Required(ErrorMessage = "Seleccione una sucursal.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione una sucursal válida.")]
        public int SucursalId { get; set; }

        public EstadoEmpleado Estado { get; set; } = EstadoEmpleado.ACTIVO;

        [StringLength(500)]
        public string Observaciones { get; set; } = "";

        // Listas para dropdowns
        public List<SelectListItem> Puestos { get; set; } = new();
        public List<SelectListItem> Sucursales { get; set; } = new();
    }

    public class EmpleadoListVm
    {
        public List<Empleado> Items { get; set; } = new();
        public string? Criterio { get; set; }
        public EstadoEmpleado? Estado { get; set; }
        public int? SucursalId { get; set; }
        public string OrdenarPor { get; set; } = "NOMBRE";
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 15;
        public int TotalItems { get; set; }
        public int TotalPaginas => (int)Math.Ceiling((double)TotalItems / TamanoPagina);
        public List<SelectListItem> Sucursales { get; set; } = new();
    }

    public class CambioEstadoVm
    {
        public int EmpleadoId { get; set; }
        public string NombreCompleto { get; set; } = "";
        public EstadoEmpleado EstadoActual { get; set; }
        public EstadoEmpleado NuevoEstado { get; set; }

        [Required(ErrorMessage = "El motivo es obligatorio.")]
        [StringLength(300)]
        public string MotivoCambio { get; set; } = "";

        public DateTime? FechaSalida { get; set; }
    }

    //  Vacaciones VM 
    public class SolicitudVacacionFormVm
    {
        public int? SolicitudId { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        public string NombreEmpleado { get; set; } = "";
        public int DiasDisponibles { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        public DateTime FechaFin { get; set; } = DateTime.Today;

        [Range(1, 365, ErrorMessage = "Días solicitados debe ser mayor a cero.")]
        public int DiasSolicitados { get; set; } = 1;

        public TipoDiaVacacion TipoDia { get; set; } = TipoDiaVacacion.DIA;

        [StringLength(500)]
        public string Observaciones { get; set; } = "";
    }

    public class RevisionVacacionVm
    {
        public int SolicitudId { get; set; }
        public string NombreEmpleado { get; set; } = "";
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int DiasSolicitados { get; set; }

        [Required]
        public EstadoSolicitudVacacion Decision { get; set; }

        [StringLength(300)]
        public string MotivoRechazo { get; set; } = "";
    }

    //  INCAPACIDADES
    public class IncapacidadFormVm
    {
        public int? IncapacidadId { get; set; }

        [Required] public int EmpleadoId { get; set; }
        public string NombreEmpleado { get; set; } = "";

        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "La fecha de fin es obligatoria.")]
        public DateTime FechaFin { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
        [StringLength(500)]
        public string Diagnostico { get; set; } = "";

        public TipoDocumentoIncapacidad TipoDocumento { get; set; } = TipoDocumentoIncapacidad.ORDEN;

        [StringLength(500)]
        public string DocumentoUrl { get; set; } = "";

        [Required(ErrorMessage = "El centro médico es obligatorio.")]
        [StringLength(200)]
        public string CentroMedico { get; set; } = "";

        [StringLength(50)]
        public string NumeroOrden { get; set; } = "";

        public EstadoIncapacidad Estado { get; set; } = EstadoIncapacidad.REGISTRADA;

        [StringLength(500)]
        public string Observaciones { get; set; } = "";
    }

    //  DOCUMENTOS

    public class DocumentoFormVm
    {
        public int? DocumentoId { get; set; }

        [Required] public int EmpleadoId { get; set; }
        public string NombreEmpleado { get; set; } = "";

        [Required(ErrorMessage = "Seleccione el tipo de documento.")]
        public TipoDocumentoExpediente DocumentoTipo { get; set; }

        [Required(ErrorMessage = "Adjunte un archivo.")]
        public IFormFile? Archivo { get; set; }

        [StringLength(500)]
        public string DocumentoUrl { get; set; } = "";

        public int Version { get; set; } = 1;
    }

    public class EliminarDocumentoVm
    {
        [Required] public int DocumentoId { get; set; }
        public int EmpleadoId { get; set; }

        [Required(ErrorMessage = "El motivo de eliminación es obligatorio.")]
        [StringLength(300)]
        public string MotivoEliminacion { get; set; } = "";
    }


    //  DIRECCIONES

    public class DireccionFormVm
    {
        public int? DireccionId { get; set; }

        [Required] public int EmpleadoId { get; set; }
        public string NombreEmpleado { get; set; } = "";

        public TipoDireccion Tipo { get; set; } = TipoDireccion.DOMICILIO;

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        [StringLength(100)] public string Provincia { get; set; } = "";

        [Required(ErrorMessage = "El cantón es obligatorio.")]
        [StringLength(100)] public string Canton { get; set; } = "";

        [Required(ErrorMessage = "El distrito es obligatorio.")]
        [StringLength(100)] public string Distrito { get; set; } = "";

        [Required(ErrorMessage = "La dirección exacta es obligatoria.")]
        [StringLength(300)] public string Direccion { get; set; } = "";

        [StringLength(10)] public string CodigoPostal { get; set; } = "";
        public decimal? Lat { get; set; }
        public decimal? Lon { get; set; }
        public bool EsPrincipal { get; set; } = false;
    }

    //  CONTACTOS DE EMERGENCIA
    public class ContactoFormVm
    {
        public int? ContactoId { get; set; }

        [Required] public int EmpleadoId { get; set; }
        public string NombreEmpleado { get; set; } = "";

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150)] public string Nombre { get; set; } = "";

        public Parentesco Parentesco { get; set; } = Parentesco.OTRO;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]{7,20}$", ErrorMessage = "Formato inválido.")]
        [StringLength(20)] public string Telefono { get; set; } = "";

        [StringLength(20)] public string TelefonoAlt { get; set; } = "";

        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [StringLength(200)] public string Email { get; set; } = "";

        public bool EsPrincipal { get; set; } = false;
    }

    public class EliminarRegistroVm
    {
        [Required] public int Id { get; set; }
        public int EmpleadoId { get; set; }
        public string TipoRegistro { get; set; } = ""; // "DIRECCION" | "CONTACTO"

        [Required(ErrorMessage = "El motivo es obligatorio.")]
        [StringLength(300)]
        public string Motivo { get; set; } = "";
    }

    //  CUENTAS BANCARIAS
    public class CuentaBancariaFormVm
    {
        public int? CuentaId { get; set; }

        [Required] public int EmpleadoId { get; set; }
        public string NombreEmpleado { get; set; } = "";

        [Required(ErrorMessage = "El banco es obligatorio.")]
        [StringLength(150)] public string Banco { get; set; } = "";

        public TipoCuenta TipoCuenta { get; set; } = TipoCuenta.AHORROS;
        public MonedaCuenta Moneda { get; set; } = MonedaCuenta.CRC;

        [Required(ErrorMessage = "El número de cuenta es obligatorio.")]
        [StringLength(50)] public string NumeroCuenta { get; set; } = "";

        [StringLength(30)] public string Iban { get; set; } = "";

        public bool EsPrincipal { get; set; } = false;
        public EstadoCuenta Estado { get; set; } = EstadoCuenta.ACTIVA;
    }

    public class InactivarCuentaVm
    {
        [Required] public int CuentaId { get; set; }
        public int EmpleadoId { get; set; }

        [Required(ErrorMessage = "El motivo es obligatorio.")]
        [StringLength(300)]
        public string Motivo { get; set; } = "";
    }

    //  MOVIMIENTOS LABORALES
    public class MovimientoFormVm
    {
        public int? MovimientoId { get; set; }

        [Required] public int EmpleadoId { get; set; }
        public string NombreEmpleado { get; set; } = "";

        [Required(ErrorMessage = "Seleccione el nuevo puesto.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione un puesto válido.")]
        public int PuestoIdNuevo { get; set; }

        [Required(ErrorMessage = "Seleccione la nueva sucursal.")]
        [Range(1, int.MaxValue, ErrorMessage = "Seleccione una sucursal válida.")]
        public int SucursalIdNueva { get; set; }

        [Required(ErrorMessage = "La fecha de vigencia es obligatoria.")]
        public DateTime VigenciaDesde { get; set; } = DateTime.Today;

        public DateTime? VigenciaHasta { get; set; }

        [Required(ErrorMessage = "El motivo es obligatorio.")]
        [StringLength(400)]
        public string Motivo { get; set; } = "";

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Puestos { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Sucursales { get; set; } = new();
    }

    public class AnularMovimientoVm
    {
        [Required] public int MovimientoId { get; set; }
        public int EmpleadoId { get; set; }

        [Required(ErrorMessage = "El motivo de anulación es obligatorio.")]
        [StringLength(400)]
        public string MotivoAnulacion { get; set; } = "";
    }
}
