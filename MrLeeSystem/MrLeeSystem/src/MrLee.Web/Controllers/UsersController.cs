using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MrLee.Web.Data;
using MrLee.Web.Models;
using MrLee.Web.Security;
using MrLee.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace MrLee.Web.Controllers;

[Authorize(Policy = PermissionCatalog.USERS_VIEW)]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordService _pwd;
    private readonly AuditService _audit;

    public UsersController(AppDbContext db, PasswordService pwd, AuditService audit)
    {
        _db = db;
        _pwd = pwd;
        _audit = audit;
    }

    public async Task<IActionResult> Index(string? q = null)
    {
        var users = _db.Users.Include(u => u.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            users = users.Where(u => u.FullName.Contains(q) || u.Email.Contains(q));

        var list = await users.OrderBy(u => u.FullName).ToListAsync();
        ViewBag.Query = q ?? "";
        return View(list);
    }

    [Authorize(Policy = PermissionCatalog.USERS_MANAGE)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = await _db.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        return View(new UserEditVm());
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.USERS_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserEditVm vm)
    {
        ViewBag.Roles = await _db.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        if (!ModelState.IsValid) return View(vm);

        if (await _db.Users.AnyAsync(u => u.Email == vm.Email))
        {
            ModelState.AddModelError(nameof(vm.Email), "Este correo ya existe.");
            return View(vm);
        }

        var user = new AppUser
        {
            FullName = vm.FullName.Trim(),
            Email = vm.Email.Trim().ToLowerInvariant(),
            RoleId = vm.RoleId,
            IsActive = true,
            PasswordHash = _pwd.HashPassword(vm.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "USER.CREATE", "AppUser", user.Id.ToString(),
            new { user.Email, user.RoleId });

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionCatalog.USERS_MANAGE)]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        ViewBag.Roles = await _db.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();

        var vm = new UserEditVm
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = user.RoleId,
            IsActive = user.IsActive
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.USERS_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditVm vm)
    {
        ViewBag.Roles = await _db.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        if (!ModelState.IsValid) return View(vm);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == vm.Id);
        if (user == null) return NotFound();

        user.FullName = vm.FullName.Trim();
        user.Email = vm.Email.Trim().ToLowerInvariant();
        user.RoleId = vm.RoleId;
        user.IsActive = vm.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "USER.EDIT", "AppUser", user.Id.ToString(),
            new { user.Email, user.RoleId, user.IsActive });

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.USERS_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "USER.TOGGLE", "AppUser", user.Id.ToString(),
            new { user.Email, user.IsActive });

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionCatalog.USERS_MANAGE)]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        return View(new ResetPasswordVm { UserId = user.Id, Email = user.Email });
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.USERS_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == vm.UserId);
        if (user == null) return NotFound();

        user.PasswordHash = _pwd.HashPassword(vm.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(User.GetUserId(), User.GetEmail(), "USER.RESET_PASSWORD", "AppUser", user.Id.ToString(),
            new { user.Email });

        TempData["Msg"] = "Contraseña restablecida.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.USERS_MANAGE)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.GetUserId();
        var currentUserEmail = User.GetEmail();

        if (currentUserId == id)
        {
            TempData["Error"] = "No puedes eliminar tu propio usuario mientras tienes una sesion activa.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            await _db.ActionLogs
                .Where(log => log.ActorUserId == user.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(log => log.ActorUserId, (int?)null));

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(currentUserId, currentUserEmail, "USER.DELETE", "AppUser", user.Id.ToString(),
                new { user.Email });

            await tx.CommitAsync();

            TempData["Msg"] = "Usuario eliminado correctamente.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "No se pudo eliminar el usuario porque todavia tiene informacion relacionada en el sistema.";
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = PermissionCatalog.USERS_AUDIT)]
    public async Task<IActionResult> Audit()
    {
        var logs = await _db.ActionLogs
            .OrderByDescending(l => l.AtUtc)
            .Take(500)
            .ToListAsync();
        return View(logs);
    }
}

public class UserEditVm
{
    public int Id { get; set; }

    [Required]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    // Only required on Create
    [MinLength(8)]
    public string Password { get; set; } = "Admin123!";
}

public class ResetPasswordVm
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = "";
}
