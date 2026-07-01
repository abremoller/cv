using CV.Shared;

namespace CV.Api.Data;

/// <summary>
/// Placeholder data only. Contains NO real personal information — it exists so a
/// fresh clone (or the in-memory dev/CI fallback) renders a believable demo CV.
/// Real data is loaded into SQL Server via the secured admin write endpoint.
/// </summary>
public static class SampleData
{
    public static CvDto Cv => new()
    {
        Header = new HeaderDto
        {
            Name = "Jordan Sample",
            Title = "Senior Software Engineer",
            Location = "Sample City, Country",
            Phone = "+00 00 000-0000",
            Email = "sample@example.com",
            LinkedInUrl = "https://www.linkedin.com/in/example",
            GitHubUrl = "https://github.com/example",
        },
        Summary = "Senior Software Engineer with 10+ years building enterprise solutions. " +
                  "This is placeholder demo content — replace it with real data via the admin API.",
        PersonalInfo = new PersonalInfoDto
        {
            FullNames = "Jordan Alex Sample",
            DateOfBirth = "1 January 1990",
            Nationality = "Sampleland",
            Languages = "English",
            DriversLicence = true,
            Passport = true,
        },
        CurrentFocus = ["LLM Integration", "AI-Assisted Development", "Cloud Architecture"],
        Tools = ["Visual Studio", "VS Code", "Git", "Docker", "Azure"],
        Skills =
        [
            new SkillDto { Name = "C#", SinceYear = 2014 },
            new SkillDto { Name = ".NET", SinceYear = 2014 },
            new SkillDto { Name = "SQL Server", SinceYear = 2015 },
            new SkillDto { Name = "TypeScript", SinceYear = 2020 },
            new SkillDto { Name = "React", SinceYear = 2021 },
        ],
        Experience =
        [
            new JobDto
            {
                Company = "Sample Company",
                Role = "Senior Software Engineer",
                Location = "Sample City (Hybrid)",
                Period = "January 2022 — Present",
                Responsibilities =
                [
                    "Placeholder responsibility — replace via the admin API",
                    "Built and maintained sample services and APIs",
                ],
                Projects =
                [
                    new ProjectDto
                    {
                        Name = "Sample Project",
                        Tech = "C#, .NET, SQL Server",
                        Scope = "Single resource project",
                        Details = ["Placeholder project detail", "Another placeholder detail"],
                    },
                ],
            },
        ],
        Education =
        [
            new EducationDto
            {
                Institution = "Sample University",
                Qualification = "BSc Computer Science",
                Location = "Sample City",
                Period = "2010 — 2013",
            },
        ],
    };
}
