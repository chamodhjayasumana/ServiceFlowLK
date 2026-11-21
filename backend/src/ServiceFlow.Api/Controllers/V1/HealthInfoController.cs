using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ServiceFlow.Api.Controllers.V1;

[ApiController]
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/info")]
public sealed class HealthInfoController : ControllerBase
{
    [HttpGet]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(typeof(ApiInfoResponse), StatusCodes.Status200OK)]
    public ActionResult<ApiInfoResponse> Get()
    {
        return Ok(new ApiInfoResponse(
            Name: "ServiceFlow LK API",
            Version: "1.0",
            Environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"));
    }
}

public sealed record ApiInfoResponse(string Name, string Version, string Environment);
