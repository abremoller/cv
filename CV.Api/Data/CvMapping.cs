using CV.Shared;

namespace CV.Api.Data;

public static class CvMapping
{
    public static CvDto ToDto(this CvProfile p, IEnumerable<Skill> skills,
        IEnumerable<Job> jobs, IEnumerable<Education> education) => new()
    {
        Header = new HeaderDto
        {
            Name = p.Name,
            Title = p.Title,
            Location = p.Location,
            Phone = p.Phone,
            Email = p.Email,
            LinkedInUrl = p.LinkedInUrl,
            GitHubUrl = p.GitHubUrl,
        },
        Summary = p.Summary,
        PersonalInfo = new PersonalInfoDto
        {
            FullNames = p.FullNames,
            DateOfBirth = p.DateOfBirth,
            Nationality = p.Nationality,
            Languages = p.Languages,
            DriversLicence = p.DriversLicence,
            Passport = p.Passport,
        },
        CurrentFocus = [.. p.CurrentFocus],
        Tools = [.. p.Tools],
        Skills = [.. skills.OrderBy(s => s.SortOrder).Select(s => new SkillDto { Name = s.Name, SinceYear = s.SinceYear })],
        Experience = [.. jobs.OrderBy(j => j.SortOrder).Select(j => new JobDto
        {
            Company = j.Company,
            Role = j.Role,
            Location = j.Location,
            Period = j.Period,
            Description = j.Description,
            Responsibilities = [.. j.Responsibilities],
            Projects = [.. j.Projects.OrderBy(pr => pr.SortOrder).Select(pr => new ProjectDto
            {
                Name = pr.Name,
                Tech = pr.Tech,
                Scope = pr.Scope,
                Details = [.. pr.Details],
            })],
        })],
        Education = [.. education.OrderBy(e => e.SortOrder).Select(e => new EducationDto
        {
            Institution = e.Institution,
            Qualification = e.Qualification,
            Location = e.Location,
            Period = e.Period,
        })],
    };

    public static CvProfile ToProfileEntity(this CvDto dto) => new()
    {
        Id = 1,
        Name = dto.Header.Name,
        Title = dto.Header.Title,
        Location = dto.Header.Location,
        Phone = dto.Header.Phone,
        Email = dto.Header.Email,
        LinkedInUrl = dto.Header.LinkedInUrl,
        GitHubUrl = dto.Header.GitHubUrl,
        Summary = dto.Summary,
        FullNames = dto.PersonalInfo.FullNames,
        DateOfBirth = dto.PersonalInfo.DateOfBirth,
        Nationality = dto.PersonalInfo.Nationality,
        Languages = dto.PersonalInfo.Languages,
        DriversLicence = dto.PersonalInfo.DriversLicence,
        Passport = dto.PersonalInfo.Passport,
        CurrentFocus = [.. dto.CurrentFocus],
        Tools = [.. dto.Tools],
    };

    public static List<Skill> ToSkillEntities(this CvDto dto) =>
        [.. dto.Skills.Select((s, i) => new Skill { Name = s.Name, SinceYear = s.SinceYear, SortOrder = i })];

    public static List<Job> ToJobEntities(this CvDto dto) =>
        [.. dto.Experience.Select((j, i) => new Job
        {
            Company = j.Company,
            Role = j.Role,
            Location = j.Location,
            Period = j.Period,
            Description = j.Description,
            Responsibilities = [.. j.Responsibilities],
            SortOrder = i,
            Projects = [.. j.Projects.Select((p, pi) => new Project
            {
                Name = p.Name,
                Tech = p.Tech,
                Scope = p.Scope,
                Details = [.. p.Details],
                SortOrder = pi,
            })],
        })];

    public static List<Education> ToEducationEntities(this CvDto dto) =>
        [.. dto.Education.Select((e, i) => new Education
        {
            Institution = e.Institution,
            Qualification = e.Qualification,
            Location = e.Location,
            Period = e.Period,
            SortOrder = i,
        })];
}
