using CV.Api.Data;
using CV.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CV.Tests;

public class CvStoreTests
{
    private static CvDbContext NewContext() =>
        new(new DbContextOptionsBuilder<CvDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static CvDto SampleCv() => new()
    {
        Header = new HeaderDto { Name = "Test Person", Title = "Engineer" },
        Summary = "Summary",
        Skills =
        [
            new SkillDto { Name = "C#", SinceYear = 2012 },
            new SkillDto { Name = "SQL", SinceYear = 2013 },
        ],
        CurrentFocus = ["AI"],
        Tools = ["Git", "Docker"],
        Experience =
        [
            new JobDto
            {
                Company = "First Co", Role = "Senior", Location = "City", Period = "2024 — Present",
                Responsibilities = ["Did things", "Did more things"],
                Projects = [new ProjectDto { Name = "Proj", Tech = "C#", Scope = "Solo", Details = ["d1", "d2"] }],
            },
            new JobDto { Company = "Second Co", Role = "Dev", Location = "City", Period = "2020 — 2024" },
        ],
        Education = [new EducationDto { Institution = "Uni", Qualification = "BSc", Period = "2008 — 2010" }],
    };

    [Fact]
    public async Task GetAsync_returns_null_when_empty()
    {
        await using var db = NewContext();
        var store = new CvStore(db);

        (await store.GetAsync()).Should().BeNull();
    }

    [Fact]
    public async Task ReplaceAsync_then_GetAsync_roundtrips_the_document()
    {
        await using var db = NewContext();
        var store = new CvStore(db);

        await store.ReplaceAsync(SampleCv());
        var cv = await store.GetAsync();

        cv.Should().NotBeNull();
        cv!.Header.Name.Should().Be("Test Person");
        cv.Skills.Should().HaveCount(2);
        cv.Tools.Should().Equal("Git", "Docker");
        cv.Experience.Should().HaveCount(2);
        cv.Experience[0].Projects.Single().Details.Should().Equal("d1", "d2");
    }

    [Fact]
    public async Task ReplaceAsync_preserves_ordering()
    {
        await using var db = NewContext();
        var store = new CvStore(db);

        await store.ReplaceAsync(SampleCv());
        var cv = await store.GetAsync();

        cv!.Experience.Select(e => e.Company).Should().Equal("First Co", "Second Co");
        cv.Skills.Select(s => s.Name).Should().Equal("C#", "SQL");
    }

    [Fact]
    public async Task ReplaceAsync_overwrites_previous_data()
    {
        await using var db = NewContext();
        var store = new CvStore(db);

        await store.ReplaceAsync(SampleCv());
        await store.ReplaceAsync(SampleCv() with { Summary = "Replaced" });

        var cv = await store.GetAsync();
        cv!.Summary.Should().Be("Replaced");
        db.Profiles.Count().Should().Be(1);
        db.Jobs.Count().Should().Be(2);
    }

    [Fact]
    public async Task SeedIfEmptyAsync_only_seeds_when_empty()
    {
        await using var db = NewContext();
        var store = new CvStore(db);

        await store.SeedIfEmptyAsync(SampleCv());
        await store.SeedIfEmptyAsync(SampleCv() with { Summary = "Should not apply" });

        var cv = await store.GetAsync();
        cv!.Summary.Should().Be("Summary");
    }
}
