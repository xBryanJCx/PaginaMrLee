using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Models;

namespace MrLee.Web.Services;

public sealed class OperatingIncomeService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public OperatingIncomeService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<string> GenerateNumberAsync()
    {
        var prefix = $"ING-{DateTime.Today:yyyyMM}";
        var count = await _db.OperatingIncomes.CountAsync(i => i.Number.StartsWith(prefix));
        var seq = count + 1;
        string number;
        do
        {
            number = $"{prefix}-{seq:0000}";
            seq++;
        }
        while (await _db.OperatingIncomes.AnyAsync(i => i.Number == number));

        return number;
    }

    public async Task<bool> IsPeriodClosedAsync(DateTime incomeDate)
    {
        return await _db.AccountingPeriods.AnyAsync(p => p.Year == incomeDate.Year && p.Month == incomeDate.Month && p.Status == AccountingPeriodStatus.Cerrado);
    }

    public async Task EnsureOpenPeriodAsync(DateTime incomeDate)
    {
        if (await IsPeriodClosedAsync(incomeDate))
            throw new InvalidOperationException("El período contable está cerrado para la fecha seleccionada.");
    }

    public async Task ClosePeriodAsync(int year, int month, string notes, int? userId, string userEmail)
    {
        var period = await _db.AccountingPeriods.FirstOrDefaultAsync(p => p.Year == year && p.Month == month);
        if (period == null)
        {
            period = new AccountingPeriod
            {
                Year = year,
                Month = month,
                Status = AccountingPeriodStatus.Cerrado,
                ClosedAtUtc = DateTime.UtcNow,
                ClosedByUserId = userId,
                ClosedByEmail = userEmail ?? "",
                Notes = notes ?? ""
            };
            _db.AccountingPeriods.Add(period);
        }
        else
        {
            period.Status = AccountingPeriodStatus.Cerrado;
            period.ClosedAtUtc = DateTime.UtcNow;
            period.ClosedByUserId = userId;
            period.ClosedByEmail = userEmail ?? "";
            period.Notes = notes ?? "";
        }

        await _db.SaveChangesAsync();
    }

    public async Task ReopenPeriodAsync(long id)
    {
        var period = await _db.AccountingPeriods.FirstAsync(p => p.Id == id);
        period.Status = AccountingPeriodStatus.Abierto;
        period.ClosedAtUtc = null;
        period.ClosedByUserId = null;
        period.ClosedByEmail = "";
        await _db.SaveChangesAsync();
    }

    public async Task<OperatingIncomeAttachment> SaveAttachmentAsync(long incomeId, IFormFile file, int? userId, string userEmail)
    {
        var root = Path.Combine(_env.WebRootPath, "uploads", "operating-income", incomeId.ToString());
        Directory.CreateDirectory(root);

        var ext = Path.GetExtension(file.FileName);
        var stored = $"{Guid.NewGuid():N}{ext}";
        var full = Path.Combine(root, stored);

        await using (var stream = File.Create(full))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new OperatingIncomeAttachment
        {
            OperatingIncomeId = incomeId,
            OriginalFileName = file.FileName,
            StoredFileName = stored,
            RelativePath = $"/uploads/operating-income/{incomeId}/{stored}",
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            CreatedByUserId = userId,
            CreatedByEmail = userEmail ?? ""
        };

        _db.OperatingIncomeAttachments.Add(attachment);
        await _db.SaveChangesAsync();
        return attachment;
    }
}
