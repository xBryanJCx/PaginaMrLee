using System.ComponentModel.DataAnnotations;

namespace MrLee.Web.Models;

public enum OperatingIncomeType
{
    Venta = 1,
    Cobro = 2,
    Otro = 3
}

public enum AccountingPeriodStatus
{
    Abierto = 1,
    Cerrado = 2
}

public class OperatingIncome
{
    public long Id { get; set; }

    [StringLength(30)]
    public string Number { get; set; } = "";

    public DateTime IncomeDate { get; set; } = DateTime.Today;
    public OperatingIncomeType Type { get; set; } = OperatingIncomeType.Venta;

    [StringLength(150)]
    public string Category { get; set; } = "Ventas";

    [StringLength(200)]
    public string SourceName { get; set; } = "";

    [StringLength(500)]
    public string Description { get; set; } = "";

    [StringLength(50)]
    public string Reference { get; set; } = "";

    public decimal Amount { get; set; }
    public bool IsVoided { get; set; } = false;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? VoidedAtUtc { get; set; }

    public int? CreatedByUserId { get; set; }
    public string CreatedByEmail { get; set; } = "";
    public int? UpdatedByUserId { get; set; }
    public string UpdatedByEmail { get; set; } = "";

    public List<OperatingIncomeAttachment> Attachments { get; set; } = new();
}

public class OperatingIncomeAttachment
{
    public long Id { get; set; }
    public long OperatingIncomeId { get; set; }
    public OperatingIncome OperatingIncome { get; set; } = default!;

    [StringLength(260)]
    public string OriginalFileName { get; set; } = "";

    [StringLength(300)]
    public string StoredFileName { get; set; } = "";

    [StringLength(400)]
    public string RelativePath { get; set; } = "";

    [StringLength(120)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }
    public string CreatedByEmail { get; set; } = "";
}

public class AccountingPeriod
{
    public long Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public AccountingPeriodStatus Status { get; set; } = AccountingPeriodStatus.Abierto;
    public DateTime? ClosedAtUtc { get; set; }
    public int? ClosedByUserId { get; set; }
    public string ClosedByEmail { get; set; } = "";

    [StringLength(300)]
    public string Notes { get; set; } = "";
}
