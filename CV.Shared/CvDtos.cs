namespace CV.Shared;

/// <summary>
/// The complete CV document returned by GET /api/cv and accepted by the
/// secured admin write endpoint. This is the single contract shared between
/// the API and the Blazor WASM client.
/// </summary>
public record CvDto
{
    public HeaderDto Header { get; init; } = new();
    public string Summary { get; init; } = "";
    public PersonalInfoDto PersonalInfo { get; init; } = new();
    public List<SkillDto> Skills { get; init; } = [];
    public List<string> CurrentFocus { get; init; } = [];
    public List<string> Tools { get; init; } = [];
    public List<JobDto> Experience { get; init; } = [];
    public List<EducationDto> Education { get; init; } = [];
}

public record HeaderDto
{
    public string Name { get; init; } = "";
    public string Title { get; init; } = "";
    public string Location { get; init; } = "";
    public string Phone { get; init; } = "";
    public string Email { get; init; } = "";
    public string? LinkedInUrl { get; init; }
    public string? GitHubUrl { get; init; }
}

public record PersonalInfoDto
{
    public string FullNames { get; init; } = "";
    public string DateOfBirth { get; init; } = "";
    public string Nationality { get; init; } = "";
    public string Languages { get; init; } = "";
    public bool DriversLicence { get; init; }
    public bool Passport { get; init; }
}

public record SkillDto
{
    public string Name { get; init; } = "";
    public int SinceYear { get; init; }
}

public record JobDto
{
    public string Company { get; init; } = "";
    public string Role { get; init; } = "";
    public string Location { get; init; } = "";
    public string Period { get; init; } = "";
    public string? Description { get; init; }
    public List<string> Responsibilities { get; init; } = [];
    public List<ProjectDto> Projects { get; init; } = [];
}

public record ProjectDto
{
    public string Name { get; init; } = "";
    public string Tech { get; init; } = "";
    public string Scope { get; init; } = "";
    public List<string> Details { get; init; } = [];
}

public record EducationDto
{
    public string Institution { get; init; } = "";
    public string Qualification { get; init; } = "";
    public string? Location { get; init; }
    public string Period { get; init; } = "";
}
