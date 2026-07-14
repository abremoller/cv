using CV.Api.Data;
using CV.Shared;
using FluentAssertions;

namespace CV.Tests;

/// <summary>
/// Guards the DTO → entity mapping. In particular, entities must NOT carry an
/// explicit key: the in-memory provider tolerates one, but SQL Server rejects it
/// with "Cannot insert explicit value for identity column" (error 544). This test
/// runs provider-free so it catches the regression the in-memory API tests can't.
/// </summary>
public class CvMappingTests
{
    private static CvDto SampleDto() => new()
    {
        Header = new HeaderDto { Name = "N", Title = "T" },
        Skills = [new SkillDto { Name = "C#", SinceYear = 2012 }],
        Experience =
        [
            new JobDto
            {
                Company = "Co", Role = "R", Location = "L", Period = "P",
                Projects = [new ProjectDto { Name = "Proj", Tech = "C#", Scope = "S" }],
            },
        ],
        Education = [new EducationDto { Institution = "I", Qualification = "Q", Period = "P" }],
    };

    [Fact]
    public void Mapped_entities_leave_ids_unset_for_the_database_to_generate()
    {
        var dto = SampleDto();

        dto.ToProfileEntity().Id.Should().Be(0);
        dto.ToSkillEntities().Should().OnlyContain(s => s.Id == 0);

        var jobs = dto.ToJobEntities();
        jobs.Should().OnlyContain(j => j.Id == 0);
        jobs.SelectMany(j => j.Projects).Should().OnlyContain(p => p.Id == 0);

        dto.ToEducationEntities().Should().OnlyContain(e => e.Id == 0);
    }
}
