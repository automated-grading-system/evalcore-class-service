using Class.Application.Dto;
using Class.Domain.Entities;

namespace Class.Application.Mapping;

internal static class ClassMappings
{
    public static ClassDto ToDto(this ClassroomClass classroomClass)
    {
        return new ClassDto(
            classroomClass.Id,
            classroomClass.Name,
            classroomClass.Description,
            classroomClass.CreatedBy,
            classroomClass.CreatedAt,
            classroomClass.UpdatedAt);
    }

    public static ClassStudentDto ToDto(this ClassStudent classStudent)
    {
        return new ClassStudentDto(
            classStudent.Id,
            classStudent.ClassId,
            classStudent.StudentId,
            classStudent.JoinedAt);
    }

    public static ClassMemberDto ToMemberDto(this ClassStudent classStudent)
    {
        return new ClassMemberDto(
            classStudent.Id,
            classStudent.ClassId,
            classStudent.StudentId,
            null,
            null,
            classStudent.JoinedAt);
    }
}
