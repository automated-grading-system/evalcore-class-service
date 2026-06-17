using Class.Application.Abstractions;
using Class.Application.Common;
using Class.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Class.Api.Controllers;

[Route("api/classes")]
public sealed class ClassesController : ApiControllerBase
{
    private readonly IClassService _classService;
    private readonly ILabService _labService;

    public ClassesController(IClassService classService, ILabService labService)
    {
        _classService = classService;
        _labService = labService;
    }

    [HttpPost]
    [Authorize(Roles = "lecturer")]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _classService.CreateClassAsync(request, cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = "lecturer,admin")]
    public async Task<IActionResult> ListClasses([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        return ToActionResult(await _classService.ListClassesAsync(query, cancellationToken));
    }

    [HttpGet("search")]
    [Authorize(Roles = "student,lecturer,admin")]
    public async Task<IActionResult> SearchClasses([FromQuery] string? name, [FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        return ToActionResult(await _classService.SearchClassesAsync(name, query, cancellationToken));
    }

    [HttpGet("my")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> MyClasses([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        return ToActionResult(await _classService.ListMyClassesAsync(query, cancellationToken));
    }

    [HttpGet("{classId:guid}")]
    [Authorize(Roles = "student,lecturer,admin")]
    public async Task<IActionResult> GetClass(Guid classId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _classService.GetClassAsync(classId, cancellationToken));
    }

    [HttpPut("{classId:guid}")]
    [Authorize(Roles = "lecturer,admin")]
    public async Task<IActionResult> UpdateClass(Guid classId, [FromBody] UpdateClassRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _classService.UpdateClassAsync(classId, request, cancellationToken));
    }

    [HttpDelete("{classId:guid}")]
    [Authorize(Roles = "lecturer,admin")]
    public async Task<IActionResult> DeleteClass(Guid classId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _classService.DeleteClassAsync(classId, cancellationToken));
    }

    [HttpPost("{classId:guid}/join")]
    [Authorize(Roles = "student")]
    public async Task<IActionResult> JoinClass(Guid classId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _classService.JoinClassAsync(classId, cancellationToken));
    }

    [HttpGet("{classId:guid}/members")]
    [Authorize(Roles = "lecturer,admin")]
    public async Task<IActionResult> ListMembers(Guid classId, [FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        return ToActionResult(await _classService.ListMembersAsync(classId, query, cancellationToken));
    }

    [HttpPost("{classId:guid}/labs")]
    [Authorize(Roles = "lecturer,admin")]
    public async Task<IActionResult> CreateLab(Guid classId, [FromBody] CreateLabRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _labService.CreateLabAsync(classId, request, cancellationToken));
    }

    [HttpGet("{classId:guid}/labs")]
    [Authorize(Roles = "student,lecturer,admin")]
    public async Task<IActionResult> ListLabs(Guid classId, [FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        return ToActionResult(await _labService.ListLabsAsync(classId, query, cancellationToken));
    }
}
