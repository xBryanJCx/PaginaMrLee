namespace MrLee.Web.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public List<RolePermission> RolePermissions { get; set; } = new();
}

public class Permission
{
    public int Id { get; set; }
    public string Code { get; set; } = ""; 
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public List<RolePermission> RolePermissions { get; set; } = new();
}

public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = default!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}

public class AppUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int FailedLoginCount { get; set; } = 0;
    public DateTime? LockoutEndUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = false;
    public DateTime? TemporaryPasswordIssuedUtc { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public class ActionLog
{
    public long Id { get; set; }
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
    public int? ActorUserId { get; set; }
    public string ActorEmail { get; set; } = "";
    public string Action { get; set; } = "";      
    public string Entity { get; set; } = "";      
    public string EntityId { get; set; } = "";    
    public string DetailJson { get; set; } = "{}";
    public string IpAddress { get; set; } = "";
}
