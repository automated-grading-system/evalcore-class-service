using Class.Application.Abstractions;
using Class.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Class.Api.Controllers;

[Route("api/labs")]
public sealed class LabsController : ApiControllerBase
{
    private readonly ILabService _labService;

    public LabsController(ILabService labService)
    {
        _labService = labService;
    }

    [HttpGet("{labId:guid}")]
    [Authorize(Roles = "student,lecturer,admin")]
    public async Task<IActionResult> GetLab(Guid labId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _labService.GetLabAsync(labId, cancellationToken));
    }

    [HttpPut("{labId:guid}")]
    [Authorize(Roles = "lecturer,admin")]
    public async Task<IActionResult> UpdateLab(Guid labId, [FromBody] UpdateLabRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _labService.UpdateLabAsync(labId, request, cancellationToken));
    }

    [HttpDelete("{labId:guid}")]
    [Authorize(Roles = "lecturer,admin")]
    public async Task<IActionResult> DeleteLab(Guid labId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _labService.DeleteLabAsync(labId, cancellationToken));
    }

    [HttpPost("{labId:guid}/assets/complete")]
    [Authorize(Roles = "lecturer,admin")]
    public async Task<IActionResult> CompleteAssets(Guid labId, [FromBody] CompleteLabAssetsRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await _labService.CompleteAssetsAsync(labId, request, cancellationToken));
    }

    [HttpGet("{labId:guid}/assets/requirement")]
    [Authorize(Roles = "student,lecturer,admin")]
    public async Task<IActionResult> GetRequirementUrl(Guid labId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _labService.GetRequirementUrlAsync(labId, cancellationToken));
    }

    [HttpGet("{labId:guid}/assets/collection")]
    [Authorize(Roles = "lecturer,admin")]
    public async Task<IActionResult> GetCollectionUrl(Guid labId, CancellationToken cancellationToken)
    {
        return ToActionResult(await _labService.GetCollectionUrlAsync(labId, cancellationToken));
    }
}
