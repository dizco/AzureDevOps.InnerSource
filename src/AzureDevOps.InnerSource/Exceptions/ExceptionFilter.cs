using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AzureDevOps.InnerSource.Exceptions;

public class ExceptionFilter : IExceptionFilter
{
	public void OnException(ExceptionContext context)
	{
		switch (context.Exception)
		{
			case RepositoryNotAllowedException _:
				context.Result = new BadRequestResult();
				context.ExceptionHandled = true;
				break;
		}
	}
}