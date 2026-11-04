namespace RecruitmentAts.Application.Lookups;

/// <summary>Table <c>departments</c>.</summary>
public sealed class Department
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
