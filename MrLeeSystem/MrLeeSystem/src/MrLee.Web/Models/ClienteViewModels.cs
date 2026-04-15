using System.ComponentModel.DataAnnotations;

namespace MrLee.Web.Models
{
    //  Registro 
    public class RegistroClienteVm
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100)]
        public string Apellido { get; set; } = "";

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [StringLength(200)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "Mínimo 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$",
            ErrorMessage = "La contraseña debe tener al menos una mayúscula, una minúscula, un número y un símbolo.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Confirme la contraseña.")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string PasswordConf { get; set; } = "";

        [Required(ErrorMessage = "Seleccione el tipo de identificación.")]
        public TipoIdentificacionCliente TipoIdentificacion { get; set; } = TipoIdentificacionCliente.CEDULA;

        [Required(ErrorMessage = "La identificación es obligatoria.")]
        [StringLength(30)]
        public string Identificacion { get; set; } = "";

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]{7,20}$", ErrorMessage = "Formato de teléfono inválido.")]
        [StringLength(20)]
        public string Telefono { get; set; } = "";

        [Range(typeof(bool), "true", "true", ErrorMessage = "Debe aceptar los términos y condiciones.")]
        public bool AceptaTerminos { get; set; } = false;
    }

    //  Login cliente 
    public class LoginClienteVm
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato inválido.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; } = false;
    }

    //  Perfil 
    public class PerfilClienteVm
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100)]
        public string Apellido { get; set; } = "";

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]{7,20}$", ErrorMessage = "Formato inválido.")]
        [StringLength(20)]
        public string Telefono { get; set; } = "";

        [EmailAddress(ErrorMessage = "Formato inválido.")]
        [StringLength(200)]
        public string EmailNuevo { get; set; } = "";

        // Dirección principal
        [StringLength(100)] public string Provincia { get; set; } = "";
        [StringLength(100)] public string Canton { get; set; } = "";
        [StringLength(100)] public string Distrito { get; set; } = "";
        [StringLength(300)] public string Direccion { get; set; } = "";
        [StringLength(10)] public string CodPostal { get; set; } = "";
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
    }

    //  Recuperar contraseña 
    public class RecuperarPasswordClienteVm
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato inválido.")]
        public string Email { get; set; } = "";
    }

    public class RestablecerPasswordClienteVm
    {
        [Required] public string Token { get; set; } = "";

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [MinLength(8, ErrorMessage = "Mínimo 8 caracteres.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z\d]).{8,}$",
            ErrorMessage = "Debe tener mayúscula, minúscula, número y símbolo.")]
        [DataType(DataType.Password)]
        public string PasswordNueva { get; set; } = "";

        [Required(ErrorMessage = "Confirme la contraseña.")]
        [Compare(nameof(PasswordNueva), ErrorMessage = "No coinciden.")]
        [DataType(DataType.Password)]
        public string PasswordConf { get; set; } = "";
    }

    //  Preferencias notificación 
    public class PreferenciasNotificacionVm
    {
        public bool NotiEmail { get; set; } = true;
        public bool NotiSms { get; set; } = false;
        public bool NotiWhatsapp { get; set; } = false;
        public string HoraSilencioInicio { get; set; } = "";
        public string HoraSilencioFin { get; set; } = "";
    }

    //  Desactivar cuenta 
    public class DesactivarCuentaVm
    {
        [Required(ErrorMessage = "Confirme su contraseña.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [StringLength(300)]
        public string MotivoBaja { get; set; } = "";
    }

    //  Pedidos cliente 
    public class MisPedidosVm
    {
        public List<Order> Pedidos { get; set; } = new();
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public OrderStatus? Estado { get; set; }
    }
}
