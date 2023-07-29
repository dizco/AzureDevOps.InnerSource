using System.Security.Claims;

namespace AzureDevOps.InnerSource.Services;

public interface IPrincipalService
{
	Principal GetPrincipal();
}

public class PrincipalService : IPrincipalService
{
	private readonly IHttpContextAccessor _httpContextAccessor;

	private HttpContext Context => _httpContextAccessor.HttpContext ?? throw new ArgumentNullException(nameof(HttpContext));

	public PrincipalService(IHttpContextAccessor httpContextAccessor)
	{
		_httpContextAccessor = httpContextAccessor;
	}
	public Principal GetPrincipal()
	{
		// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-context?view=aspnetcore-7.0
		
		var principal = new Principal
		{
			Id = Context.User.FindFirstValue("sub") ?? throw new Exception("Expected to find a sub claim"),
			Email = Context.User.FindFirstValue("email")
		};
		return principal;
	}
}