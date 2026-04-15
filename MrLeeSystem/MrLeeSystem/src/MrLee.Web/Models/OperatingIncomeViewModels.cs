using System.ComponentModel.DataAnnotations;

namespace MrLee.Web.Models;

public class OperatingIncomeEditVm
{
    public long Id { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime IncomeDate { get; set; } = DateTime.Today;

    [Required]
    public OperatingIncomeType Type { get; set; } = OperatingIncomeType.Venta;

    [Required, StringLength(150)]
    public string Category { get; set; } = "Ventas";

    [Required, StringLength(200)]
    public string SourceName { get; set; } = "";

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Reference { get; set; }

    [Display(Name = "Monto")]
    [Range(typeof(decimal), "0.01", "999999999.99",
        ErrorMessage = "El monto debe ser mayor o igual a 0.01",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal Amount { get; set; }

    public bool IsVoided { get; set; }
}

public class OperatingIncomeSummaryRowVm
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class ClosePeriodVm
{
    [Range(2020, 2100)]
    public int Year { get; set; } = DateTime.Today.Year;

    [Range(1, 12)]
    public int Month { get; set; } = DateTime.Today.Month;

    public string? Notes { get; set; }
}
