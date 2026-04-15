using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Models;
using MrLee.Web.Security;
using MrLee.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MrLee.Web.Controllers;

[Authorize(Policy = PermissionCatalog.ING_VIEW)]
public class OperatingIncomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly OperatingIncomeService _service;
    private readonly AuditService _audit;

    public OperatingIncomeController(AppDbContext db, OperatingIncomeService service, AuditService audit)
    {
        _db = db;
        _service = service;
        _audit = audit;
    }

    public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null, bool includeVoided = false, string? q = null)
    {
        var query = _db.OperatingIncomes.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(x => x.IncomeDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(x => x.IncomeDate <= to.Value.Date);
        if (!includeVoided)
            query = query.Where(x => !x.IsVoided);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Number.Contains(q) || x.SourceName.Contains(q) || x.Reference.Contains(q) || x.Description.Contains(q));

        var list = await query.OrderByDescending(x => x.IncomeDate).ThenByDescending(x => x.Id).Take(500).ToListAsync();
        ViewBag.From = from?.ToString("yyyy-MM-dd") ?? "";
        ViewBag.To = to?.ToString("yyyy-MM-dd") ?? "";
        ViewBag.Query = q ?? "";
        ViewBag.IncludeVoided = includeVoided;
        ViewBag.TotalVisible = list.Where(x => !x.IsVoided).Sum(x => x.Amount);
        ViewBag.VisibleCount = list.Count;
        ViewBag.ActiveCount = list.Count(x => !x.IsVoided);
        ViewBag.VoidedCount = list.Count(x => x.IsVoided);
        return View(list);
    }

    public async Task<IActionResult> Summary(DateTime? from = null, DateTime? to = null)
    {
        var f = (from ?? DateTime.Today.AddDays(-6)).Date;
        var t = (to ?? DateTime.Today).Date;

        var items = await _db.OperatingIncomes
            .AsNoTracking()
            .Where(x => !x.IsVoided && x.IncomeDate >= f && x.IncomeDate <= t)
            .GroupBy(x => x.IncomeDate.Date)
            .Select(g => new OperatingIncomeSummaryRowVm
            {
                Date = g.Key,
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        ViewBag.From = f.ToString("yyyy-MM-dd");
        ViewBag.To = t.ToString("yyyy-MM-dd");
        ViewBag.GrandTotal = items.Sum(x => x.Amount);
        ViewBag.RowCount = items.Count;
        return View(items);
    }

    [Authorize(Policy = PermissionCatalog.ING_MANAGE)]
    public IActionResult Create() => View(new OperatingIncomeEditVm { IncomeDate = DateTime.Today, Type = OperatingIncomeType.Venta, Category = "Ventas" });

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ING_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OperatingIncomeEditVm vm, List<IFormFile>? files)
    {
        if (!ModelState.IsValid) return View(vm);

        try
        {
            await _service.EnsureOpenPeriodAsync(vm.IncomeDate);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(vm.IncomeDate), ex.Message);
            return View(vm);
        }

        var entity = new OperatingIncome
        {
            Number = await _service.GenerateNumberAsync(),
            IncomeDate = vm.IncomeDate.Date,
            Type = vm.Type,
            Category = vm.Category.Trim(),
            SourceName = vm.SourceName.Trim(),
            Description = vm.Description?.Trim() ?? "",
            Reference = vm.Reference?.Trim() ?? "",
            Amount = vm.Amount,
            CreatedByUserId = User.GetUserId(),
            CreatedByEmail = User.GetEmail()
        };

        _db.OperatingIncomes.Add(entity);
        await _db.SaveChangesAsync();

        if (files != null)
        {
            foreach (var file in files.Where(f => f.Length > 0))
                await _service.SaveAttachmentAsync(entity.Id, file, User.GetUserId(), User.GetEmail());
        }

        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "ING.CREATE", "OperatingIncome", entity.Id.ToString(),
            new { entity.Number, entity.IncomeDate, entity.Amount, entity.Type, files = files?.Count(f => f.Length > 0) ?? 0 });

        TempData["Msg"] = "Ingreso operativo registrado.";
        return RedirectToAction(nameof(Details), new { id = entity.Id });
    }

    public async Task<IActionResult> Details(long id)
    {
        var income = await _db.OperatingIncomes
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (income == null) return NotFound();
        return View(income);
    }

    [Authorize(Policy = PermissionCatalog.ING_MANAGE)]
    public async Task<IActionResult> Edit(long id)
    {
        var income = await _db.OperatingIncomes.Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == id);
        if (income == null) return NotFound();

        var vm = new OperatingIncomeEditVm
        {
            Id = income.Id,
            IncomeDate = income.IncomeDate,
            Type = income.Type,
            Category = income.Category,
            SourceName = income.SourceName,
            Description = income.Description,
            Reference = income.Reference,
            Amount = income.Amount,
            IsVoided = income.IsVoided
        };
        ViewBag.Attachments = income.Attachments.OrderByDescending(x => x.CreatedAtUtc).ToList();
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ING_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OperatingIncomeEditVm vm, List<IFormFile>? files)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Attachments = await _db.OperatingIncomeAttachments.Where(x => x.OperatingIncomeId == vm.Id).OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
            return View(vm);
        }

        var income = await _db.OperatingIncomes.FirstOrDefaultAsync(x => x.Id == vm.Id);
        if (income == null) return NotFound();

        try
        {
            await _service.EnsureOpenPeriodAsync(income.IncomeDate);
            await _service.EnsureOpenPeriodAsync(vm.IncomeDate);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(vm.IncomeDate), ex.Message);
            ViewBag.Attachments = await _db.OperatingIncomeAttachments.Where(x => x.OperatingIncomeId == vm.Id).OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
            return View(vm);
        }

        income.IncomeDate = vm.IncomeDate.Date;
        income.Type = vm.Type;
        income.Category = vm.Category.Trim();
        income.SourceName = vm.SourceName.Trim();
        income.Description = vm.Description?.Trim() ?? "";
        income.Reference = vm.Reference?.Trim() ?? "";
        income.Amount = vm.Amount;
        income.UpdatedAtUtc = DateTime.UtcNow;
        income.UpdatedByUserId = User.GetUserId();
        income.UpdatedByEmail = User.GetEmail();

        await _db.SaveChangesAsync();

        if (files != null)
        {
            foreach (var file in files.Where(f => f.Length > 0))
                await _service.SaveAttachmentAsync(income.Id, file, User.GetUserId(), User.GetEmail());
        }

        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "ING.EDIT", "OperatingIncome", income.Id.ToString(),
            new { income.Number, income.IncomeDate, income.Amount, income.Type });

        TempData["Msg"] = "Ingreso operativo actualizado.";
        return RedirectToAction(nameof(Details), new { id = income.Id });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ING_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(long id, string? reason)
    {
        var income = await _db.OperatingIncomes.FirstOrDefaultAsync(x => x.Id == id);
        if (income == null) return NotFound();

        try
        {
            await _service.EnsureOpenPeriodAsync(income.IncomeDate);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Msg"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        income.IsVoided = true;
        income.VoidedAtUtc = DateTime.UtcNow;
        income.UpdatedAtUtc = DateTime.UtcNow;
        income.UpdatedByUserId = User.GetUserId();
        income.UpdatedByEmail = User.GetEmail();
        if (!string.IsNullOrWhiteSpace(reason))
            income.Description = string.IsNullOrWhiteSpace(income.Description) ? $"ANULADO: {reason}" : $"{income.Description} | ANULADO: {reason}";

        await _db.SaveChangesAsync();

        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "ING.VOID", "OperatingIncome", income.Id.ToString(),
            new { income.Number, reason });

        TempData["Msg"] = "Ingreso anulado.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionCatalog.ING_AUDIT)]
    public async Task<IActionResult> Trace(long id)
    {
        var income = await _db.OperatingIncomes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (income == null) return NotFound();

        ViewBag.Income = income;
        var logs = await _db.ActionLogs.AsNoTracking()
            .Where(x => x.Entity == "OperatingIncome" && x.EntityId == id.ToString())
            .OrderByDescending(x => x.AtUtc)
            .ToListAsync();
        return View(logs);
    }

    [Authorize(Policy = PermissionCatalog.ING_MANAGE)]
    public async Task<IActionResult> Periods()
    {
        var periods = await _db.AccountingPeriods.AsNoTracking().OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).ToListAsync();
        return View(periods);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ING_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClosePeriod(ClosePeriodVm vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Msg"] = "Datos inválidos para cerrar el período.";
            return RedirectToAction(nameof(Periods));
        }

        await _service.ClosePeriodAsync(vm.Year, vm.Month, vm.Notes ?? "", User.GetUserId(), User.GetEmail());
        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "ING.CLOSE_PERIOD", "AccountingPeriod", $"{vm.Year}-{vm.Month:00}", new { vm.Year, vm.Month, vm.Notes });
        TempData["Msg"] = "Período contable cerrado.";
        return RedirectToAction(nameof(Periods));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ING_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReopenPeriod(long id)
    {
        var p = await _db.AccountingPeriods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();

        await _service.ReopenPeriodAsync(id);
        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "ING.REOPEN_PERIOD", "AccountingPeriod", id.ToString(), new { p.Year, p.Month });
        TempData["Msg"] = "Período reabierto.";
        return RedirectToAction(nameof(Periods));
    }

    public async Task<IActionResult> Export(DateTime? from = null, DateTime? to = null, string format = "detail")
    {
        var query = _db.OperatingIncomes.AsNoTracking().Where(x => !x.IsVoided);
        if (from.HasValue) query = query.Where(x => x.IncomeDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(x => x.IncomeDate <= to.Value.Date);

        var sb = new StringBuilder();
        if (string.Equals(format, "summary", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("Fecha,CantidadRegistros,MontoTotal");
            var rows = await query.GroupBy(x => x.IncomeDate.Date)
                .Select(g => new { g.Key, Count = g.Count(), Total = g.Sum(x => x.Amount) })
                .OrderBy(x => x.Key)
                .ToListAsync();
            foreach (var row in rows)
                sb.AppendLine($"{row.Key:yyyy-MM-dd},{row.Count},{row.Total:0.00}");

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"resumen_ingresos_{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        sb.AppendLine("Numero,Fecha,Tipo,Categoria,Origen,Referencia,Monto");
        var list = await query.OrderBy(x => x.IncomeDate).ThenBy(x => x.Id).ToListAsync();
        foreach (var item in list)
            sb.AppendLine($"{Escape(item.Number)},{item.IncomeDate:yyyy-MM-dd},{item.Type},{Escape(item.Category)},{Escape(item.SourceName)},{Escape(item.Reference)},{item.Amount:0.00}");

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"ingresos_{DateTime.Now:yyyyMMddHHmmss}.csv");
    }

    private static string Escape(string? value)
    {
        value ??= "";
        value = value.Replace("\"", "\"\"");
        return $"\"{value}\"";
    }
}
