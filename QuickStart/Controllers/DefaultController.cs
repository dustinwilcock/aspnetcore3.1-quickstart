using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace QuickStart.Controllers
{
	[ApiController]
	[Route("")]
	public class DefaultController : ControllerBase
	{
		private readonly ILogger<DefaultController> _logger;

		public DefaultController(ILogger<DefaultController> logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		[HttpGet]
		public object Get()
		{
			var responseObject = new
			{
				Status = "Up"
			};
			_logger.LogInformation($"Status pinged: {responseObject.Status}");
			return responseObject;
		}
	}
}
