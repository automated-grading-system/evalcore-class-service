using Class.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Class.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.Succeeded)
        {
            return StatusCode(result.StatusCode, ApiResponse<T>.Ok(result.Value!));
        }

        return StatusCode(result.StatusCode, ApiResponse<T>.Fail(result.Error!));
    }
}
