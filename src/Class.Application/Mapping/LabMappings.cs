using Class.Application.Dto;
using Class.Domain.Entities;

namespace Class.Application.Mapping;

internal static class LabMappings
{
    public static LabDto ToDto(this Lab lab)
    {
        return new LabDto(
            lab.Id,
            lab.ClassId,
            lab.Title,
            lab.Description,
            lab.RequirementObjectKey,
            lab.CollectionObjectKey,
            lab.Status,
            lab.Deadline,
            lab.CreatedBy,
            lab.CreatedAt,
            lab.UpdatedAt,
            lab.AssetsCompletedAt);
    }
}
