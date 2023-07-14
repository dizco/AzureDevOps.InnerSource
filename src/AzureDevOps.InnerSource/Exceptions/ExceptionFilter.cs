using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AzureDevOps.InnerSource.Exceptions;

public class ExceptionFilter : IExceptionFilter
{
	public void OnException(ExceptionContext context)
	{
		switch (context.Exception)
		{
			case ValidationException _:
				context.Result = new BadRequestResult();
				context.ExceptionHandled = true;
				break;
			case RepositoryNotAllowedException _:
				context.Result = new BadRequestResult();
				context.ExceptionHandled = true;
				break;
		}
	}
}