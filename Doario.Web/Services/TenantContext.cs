namespace Doario.Web.Services;

public class TenantContext
{
    private readonly IHttpContextAccessor _accessor;

    public TenantContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid TenantId
    {
        get
        {
            var val = _accessor.HttpContext?.Items["TenantId"];
            if (val is Guid id) return id;
            throw new InvalidOperationException(
                "TenantId not resolved. User may not belong to a registered organisation.");
        }
    }

    public string TenantName =>
        _accessor.HttpContext?.Items["TenantName"] as string ?? string.Empty;

    public bool IsResolved =>
        _accessor.HttpContext?.Items["TenantId"] is Guid;
}