namespace CV.Api.Data;

/// <summary>
/// Single-row profile holding header, summary, personal info, and the flat
/// skill-tag style lists. One CV = one profile. The Id is database-generated;
/// the store keeps a single row by replacing it wholesale on each write.
/// </summary>
public class CvProfile
{
    public int Id { get; set; }

    // Header
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Location { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string? LinkedInUrl { get; set; }
    public string? GitHubUrl { get; set; }

    public string Summary { get; set; } = "";

    // Personal information
    public string FullNames { get; set; } = "";
    public string DateOfBirth { get; set; } = "";
    public string Nationality { get; set; } = "";
    public string Languages { get; set; } = "";
    public bool DriversLicence { get; set; }
    public bool Passport { get; set; }

    // Stored as JSON columns (EF Core 8 primitive collections)
    public List<string> CurrentFocus { get; set; } = [];
    public List<string> Tools { get; set; } = [];
}

public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SinceYear { get; set; }
    public int SortOrder { get; set; }
}

public class Job
{
    public int Id { get; set; }
    public string Company { get; set; } = "";
    public string Role { get; set; } = "";
    public string Location { get; set; } = "";
    public string Period { get; set; } = "";
    public string? Description { get; set; }
    public List<string> Responsibilities { get; set; } = [];
    public int SortOrder { get; set; }
    public List<Project> Projects { get; set; } = [];
}

public class Project
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job? Job { get; set; }
    public string Name { get; set; } = "";
    public string Tech { get; set; } = "";
    public string Scope { get; set; } = "";
    public List<string> Details { get; set; } = [];
    public int SortOrder { get; set; }
}

public class Education
{
    public int Id { get; set; }
    public string Institution { get; set; } = "";
    public string Qualification { get; set; } = "";
    public string? Location { get; set; }
    public string Period { get; set; } = "";
    public int SortOrder { get; set; }
}
