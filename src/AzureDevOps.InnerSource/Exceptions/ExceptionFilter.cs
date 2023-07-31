using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AzureDevOps.InnerSource.Exceptions;

public class ExceptionFilter : IExceptionFilter
{
	private readonly ILogger<ExceptionFilter> _logger;

	public ExceptionFilter(ILogger<ExceptionFilter> logger)
	{
		_logger = logger;
	}

	public void OnException(ExceptionContext context)
	{
		LogException(context.Exception);

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

	private void LogException(Exception exception)
	{
		_logger.LogError(exception, "An unhandled exception occurred");
	}
}