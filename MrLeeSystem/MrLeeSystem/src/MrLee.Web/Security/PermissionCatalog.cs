namespace MrLee.Web.Security;

public static class PermissionCatalog
{
    // Keep codes short + consistent. Add more as you expand modules.
    public const string USERS_VIEW = "USR.VIEW";
    public const string USERS_MANAGE = "USR.MANAGE";
    public const string USERS_AUDIT = "USR.AUDIT";

    public const string INV_VIEW = "INV.VIEW";
    public const string INV_MANAGE = "INV.MANAGE";
    public const string INV_MOVEMENTS = "INV.MOVEMENTS";

    public const string ORD_VIEW = "ORD.VIEW";
    public const string ORD_MANAGE = "ORD.MANAGE";
    public const string ORD_STATUS = "ORD.STATUS";

    public const string ING_VIEW = "ING.VIEW";
    public const string ING_MANAGE = "ING.MANAGE";
    public const string ING_AUDIT = "ING.AUDIT";

    public const string RRHH_VIEW = "RRHH.VIEW";
    public const string RRHH_MANAGE = "RRHH.MANAGE";
    public const string RRHH_VACACIONES = "RRHH.VACACIONES";

    public static readonly string[] All = new[]
    {
        USERS_VIEW, USERS_MANAGE, USERS_AUDIT,
        INV_VIEW, INV_MANAGE, INV_MOVEMENTS,
        ORD_VIEW, ORD_MANAGE, ORD_STATUS,
        ING_VIEW, ING_MANAGE, ING_AUDIT,
        RRHH_VIEW, RRHH_MANAGE, RRHH_VACACIONES
    };
}
