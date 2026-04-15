using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Models;
using MrLee.Web.Security;
using MrLee.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace MrLee.Web.Controllers;

[Authorize(Policy = PermissionCatalog.ORD_VIEW)]
public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly OrderService _orders;
    private readonly AuditService _audit;

    public OrdersController(AppDbContext db, OrderService orders, AuditService audit)
    {
        _db = db;
        _orders = orders;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? q = null, OrderStatus? status = null)
    {
        var orders = _db.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            orders = orders.Where(o => o.TrackingNumber.Contains(q) || o.CustomerName.Contains(q) || o.CustomerPhone.Contains(q));

        if (status.HasValue)
            orders = orders.Where(o => o.Status == status.Value);

        var list = await orders.OrderByDescending(o => o.CreatedAtUtc).Take(300).ToListAsync();
        ViewBag.Query = q ?? "";
        ViewBag.Status = status;
        return View(list);
    }

    [Authorize(Policy = PermissionCatalog.ORD_MANAGE)]
    public async Task<IActionResult> Create()
    {
        await PopulateCreateLookupsAsync();

        var vm = new OrderCreateVm();
        EnsureAtLeastOneItem(vm);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ORD_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderCreateVm vm)
    {
        await PopulateCreateLookupsAsync();
        EnsureAtLeastOneItem(vm);
        await AutofillSelectedCustomerAsync(vm);

        if (!ModelState.IsValid) return View(vm);

        var tracking = await _orders.GenerateTrackingNumberAsync();

        var order = new Order
        {
            TrackingNumber = tracking,
            CustomerName = vm.CustomerName.Trim(),
            CustomerPhone = vm.CustomerPhone.Trim(),
            DeliveryAddress = vm.DeliveryAddress.Trim(),
            Notes = vm.Notes?.Trim() ?? "",
            Status = OrderStatus.Recibido
        };

        // build items (ignore empty lines)
        var lines = vm.Items.Where(i => i.ProductId.HasValue && i.Quantity > 0).ToList();
        if (lines.Count == 0)
        {
            ModelState.AddModelError("", "Agregue al menos un producto al pedido.");
            return View(vm);
        }

        var productIds = lines.Select(l => l.ProductId!.Value).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        foreach (var l in lines)
        {
            var p = products.First(x => x.Id == l.ProductId!.Value);
            order.Items.Add(new OrderItem
            {
                ProductId = p.Id,
                Quantity = l.Quantity,
                UnitPrice = p.UnitPrice
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        await _orders.AppendHistoryAsync(order.Id, OrderStatus.Recibido, "Pedido creado", User.GetUserId(), User.GetEmail());

        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "ORD.CREATE", "Order", order.Id.ToString(),
            new { order.TrackingNumber, order.CustomerName, items = lines.Count });

        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    public async Task<IActionResult> Details(long id)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.History)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        order.History = order.History.OrderByDescending(h => h.AtUtc).ToList();
        return View(order);
    }

    [Authorize(Policy = PermissionCatalog.ORD_STATUS)]
    public async Task<IActionResult> UpdateStatus(long id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        return View(new UpdateStatusVm { OrderId = id, CurrentStatus = order.Status, NewStatus = order.Status });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ORD_STATUS)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UpdateStatusVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        await _orders.UpdateStatusAsync(vm.OrderId, vm.NewStatus, vm.Comment ?? "", User.GetUserId(), User.GetEmail());

        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "ORD.STATUS", "Order", vm.OrderId.ToString(),
            new { vm.NewStatus, vm.Comment });

        return RedirectToAction(nameof(Details), new { id = vm.OrderId });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.ORD_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var order = await _db.Orders.Include(o => o.Items).Include(o => o.History).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        _db.OrderItems.RemoveRange(order.Items);
        _db.OrderStatusHistory.RemoveRange(order.History);
        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "ORD.DELETE", "Order", id.ToString(),
            new { order.TrackingNumber });

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCreateLookupsAsync()
    {
        ViewBag.Products = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        ViewBag.Customers = await _db.Clientes
            .AsNoTracking()
            .Include(c => c.Direcciones)
            .Where(c => c.IsActive && !c.DadoDeBaja)
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.Apellido)
            .ToListAsync();
    }

    private async Task AutofillSelectedCustomerAsync(OrderCreateVm vm)
    {
        if (!vm.RegisteredCustomerId.HasValue)
            return;

        var customer = await _db.Clientes
            .AsNoTracking()
            .Include(c => c.Direcciones)
            .FirstOrDefaultAsync(c => c.Id == vm.RegisteredCustomerId.Value && c.IsActive && !c.DadoDeBaja);

        if (customer == null)
            return;

        if (string.IsNullOrWhiteSpace(vm.CustomerName))
            vm.CustomerName = BuildCustomerFullName(customer);

        if (string.IsNullOrWhiteSpace(vm.CustomerPhone))
            vm.CustomerPhone = customer.Telefono.Trim();

        if (string.IsNullOrWhiteSpace(vm.DeliveryAddress))
            vm.DeliveryAddress = BuildCustomerPrimaryAddress(customer);

        ModelState.Remove(nameof(OrderCreateVm.CustomerName));
        ModelState.Remove(nameof(OrderCreateVm.CustomerPhone));
        ModelState.Remove(nameof(OrderCreateVm.DeliveryAddress));
        TryValidateModel(vm);
    }

    private static void EnsureAtLeastOneItem(OrderCreateVm vm)
    {
        vm.Items ??= new List<OrderLineVm>();

        if (vm.Items.Count == 0)
            vm.Items.Add(new OrderLineVm());
    }

    private static string BuildCustomerFullName(Cliente customer) =>
        $"{customer.Nombre} {customer.Apellido}".Trim();

    private static string BuildCustomerPrimaryAddress(Cliente customer)
    {
        var address = customer.Direcciones.FirstOrDefault(d => d.EsPrincipal)
            ?? customer.Direcciones.FirstOrDefault();

        if (address == null)
            return string.Empty;

        var parts = new[]
        {
            address.Direccion,
            address.Distrito,
            address.Canton,
            address.Provincia
        };

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part.Trim()));
    }
}

public class OrderCreateVm
{
    public int? RegisteredCustomerId { get; set; }

    [Required]
    public string CustomerName { get; set; } = "";

    [Required]
    public string CustomerPhone { get; set; } = "";

    [Required]
    public string DeliveryAddress { get; set; } = "";

    public string? Notes { get; set; }

    public List<OrderLineVm> Items { get; set; } = new()
    {
        new OrderLineVm()
    };
}

public class OrderLineVm
{
    public int? ProductId { get; set; }

    [Range(0, 999999)]
    public decimal Quantity { get; set; } = 0m;
}

public class UpdateStatusVm
{
    public long OrderId { get; set; }

    public OrderStatus CurrentStatus { get; set; }

    [Required]
    public OrderStatus NewStatus { get; set; }

    public string? Comment { get; set; }
}
