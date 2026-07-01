using CV.Shared;
using Microsoft.EntityFrameworkCore;

namespace CV.Api.Data;

/// <summary>
/// Reads and replaces the single CV document. Writes are a full transactional
/// replace, which keeps the admin update operation simple and idempotent.
/// </summary>
public class CvStore(CvDbContext db)
{
    public async Task<CvDto?> GetAsync(CancellationToken ct = default)
    {
        var profile = await db.Profiles.AsNoTracking().FirstOrDefaultAsync(ct);
        if (profile is null) return null;

        var skills = await db.Skills.AsNoTracking().ToListAsync(ct);
        var jobs = await db.Jobs.AsNoTracking().Include(j => j.Projects).ToListAsync(ct);
        var education = await db.Education.AsNoTracking().ToListAsync(ct);

        return profile.ToDto(skills, jobs, education);
    }

    public async Task ReplaceAsync(CvDto dto, CancellationToken ct = default)
    {
        var supportsTx = db.Database.IsRelational();
        await using var tx = supportsTx ? await db.Database.BeginTransactionAsync(ct) : null;

        db.Projects.RemoveRange(db.Projects);
        db.Jobs.RemoveRange(db.Jobs);
        db.Skills.RemoveRange(db.Skills);
        db.Education.RemoveRange(db.Education);
        db.Profiles.RemoveRange(db.Profiles);
        await db.SaveChangesAsync(ct);

        db.Profiles.Add(dto.ToProfileEntity());
        db.Skills.AddRange(dto.ToSkillEntities());
        db.Jobs.AddRange(dto.ToJobEntities());
        db.Education.AddRange(dto.ToEducationEntities());
        await db.SaveChangesAsync(ct);

        if (tx is not null) await tx.CommitAsync(ct);
    }

    public async Task SeedIfEmptyAsync(CvDto dto, CancellationToken ct = default)
    {
        if (!await db.Profiles.AnyAsync(ct))
            await ReplaceAsync(dto, ct);
    }
}
