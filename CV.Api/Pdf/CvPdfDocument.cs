using CV.Shared;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CV.Api.Pdf;

/// <summary>
/// Renders a <see cref="CvDto"/> to a PDF using QuestPDF — a browserless, code-first
/// engine (SkiaSharp under the hood). No headless Chrome / wkhtmltopdf required, so it
/// runs identically on the server and locally. Layout mirrors the web app's style.
/// </summary>
public static class CvPdfDocument
{
    // Palette mirrors CV.Web/wwwroot/css/app.css
    private const string Navy = "#1a1a2e";
    private const string Accent = "#2563eb";
    private const string AccentLight = "#dbeafe";
    private const string TextColor = "#1f2937";
    private const string Secondary = "#6b7280";
    private const string Light = "#9ca3af";
    private const string BorderColor = "#e5e7eb";
    private const string SidebarBg = "#f8fafc";
    private const string CardBg = "#f9fafb";
    private const string White = "#ffffff";

    private const float SidebarWidth = 190f;

    public static byte[] Render(CvDto cv)
    {
        var year = DateTime.UtcNow.Year;
        var maxYears = cv.Skills.Count == 0 ? 1 : Math.Max(1, cv.Skills.Max(s => year - s.SinceYear));

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor(TextColor).LineHeight(1.3f));

                // Paint the sidebar tint full-height on every page so it stays consistent
                // where the (shorter) sidebar content ends but experience continues.
                page.Background().Row(row =>
                {
                    row.ConstantItem(SidebarWidth).Background(SidebarBg);
                    row.RelativeItem().Background(White);
                });

                page.Content().Column(col =>
                {
                    Header(col, cv);
                    col.Item().Row(body =>
                    {
                        body.ConstantItem(SidebarWidth).PaddingHorizontal(16).PaddingTop(16).PaddingBottom(20)
                            .Column(side => Sidebar(side, cv, year, maxYears));
                        body.RelativeItem().PaddingHorizontal(24).PaddingTop(16).PaddingBottom(20)
                            .Column(main => MainContent(main, cv));
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void Header(ColumnDescriptor col, CvDto cv)
    {
        col.Item().Background(Navy).PaddingVertical(22).PaddingHorizontal(26).Row(h =>
        {
            h.RelativeItem().Column(c =>
            {
                c.Item().Text(cv.Header.Name).FontSize(22).Bold().FontColor(White);
                if (!string.IsNullOrWhiteSpace(cv.Header.Title))
                    c.Item().PaddingTop(2).Text(cv.Header.Title).FontSize(11).FontColor("#cbd5e1");
            });
            h.ConstantItem(210).AlignRight().Column(c =>
            {
                if (!string.IsNullOrWhiteSpace(cv.Header.Location))
                    c.Item().Text(cv.Header.Location).FontSize(8.5f).FontColor("#e2e8f0");
                if (!string.IsNullOrWhiteSpace(cv.Header.Email))
                    c.Item().PaddingTop(2).Hyperlink($"mailto:{cv.Header.Email}").Text(cv.Header.Email).FontSize(8.5f).FontColor("#93c5fd");
                if (!string.IsNullOrWhiteSpace(cv.Header.LinkedInUrl))
                    c.Item().PaddingTop(2).Hyperlink(cv.Header.LinkedInUrl!).Text(ShortUrl(cv.Header.LinkedInUrl!)).FontSize(8.5f).FontColor("#93c5fd");
                if (!string.IsNullOrWhiteSpace(cv.Header.GitHubUrl))
                    c.Item().PaddingTop(2).Hyperlink(cv.Header.GitHubUrl!).Text(ShortUrl(cv.Header.GitHubUrl!)).FontSize(8.5f).FontColor("#93c5fd");
            });
        });
    }

    private static void Sidebar(ColumnDescriptor col, CvDto cv, int year, int maxYears)
    {
        var pi = cv.PersonalInfo;
        Section(col, "Personal Information");
        InfoRow(col, "Full Names", pi.FullNames);
        InfoRow(col, "Nationality", pi.Nationality);
        InfoRow(col, "Languages", pi.Languages);
        InfoRow(col, "Driver's Licence", pi.DriversLicence ? "Yes" : "No");
        InfoRow(col, "Passport", pi.Passport ? "Yes" : "No");

        if (cv.Skills.Count > 0)
        {
            Section(col, "Skills");
            foreach (var s in cv.Skills)
                SkillBar(col, s.Name, Math.Max(0, year - s.SinceYear), maxYears);
        }

        if (cv.CurrentFocus.Count > 0)
        {
            Section(col, "Current Focus");
            col.Item().Inlined(inl =>
            {
                inl.Spacing(4);
                foreach (var f in cv.CurrentFocus)
                    inl.Item().Background(Navy).PaddingHorizontal(6).PaddingVertical(3)
                       .Text(f).FontSize(7.5f).FontColor(White);
            });
        }

        if (cv.Tools.Count > 0)
        {
            Section(col, "Tools");
            col.Item().Inlined(inl =>
            {
                inl.Spacing(4);
                foreach (var t in cv.Tools)
                    inl.Item().Background(AccentLight).PaddingHorizontal(5).PaddingVertical(2)
                       .Text(t).FontSize(7f).FontColor(Accent);
            });
        }
    }

    private static void MainContent(ColumnDescriptor col, CvDto cv)
    {
        Section(col, "Experience");
        foreach (var j in cv.Experience)
            JobBlock(col, j);

        if (cv.Education.Count > 0)
        {
            Section(col, "Education");
            foreach (var e in cv.Education)
                EduBlock(col, e);
        }
    }

    private static void JobBlock(ColumnDescriptor col, JobDto j)
    {
        col.Item().PaddingTop(10).Column(c =>
        {
            c.Item().Row(r =>
            {
                r.RelativeItem().Text(j.Company).FontSize(12).Bold().FontColor(TextColor);
                r.AutoItem().PaddingLeft(8).Text(j.Period).FontSize(8).FontColor(Light);
            });
            c.Item().Text(t =>
            {
                t.Span(j.Role).FontSize(9).Italic().FontColor(Secondary);
                if (!string.IsNullOrWhiteSpace(j.Location))
                    t.Span($" — {j.Location}").FontSize(9).Italic().FontColor(Secondary);
            });

            if (!string.IsNullOrWhiteSpace(j.Description))
                c.Item().PaddingTop(4).Text(j.Description).FontSize(8.5f).FontColor(Secondary);

            if (j.Responsibilities.Count > 0)
                c.Item().PaddingTop(4).Column(b =>
                {
                    foreach (var r in j.Responsibilities) Bullet(b, r);
                });

            if (j.Projects.Count > 0)
                c.Item().PaddingTop(6).Column(pcol =>
                {
                    pcol.Spacing(6);
                    foreach (var p in j.Projects) ProjectBlock(pcol, p);
                });
        });
    }

    private static void ProjectBlock(ColumnDescriptor col, ProjectDto p)
    {
        col.Item().ShowEntire().Background(CardBg).BorderLeft(3).BorderColor(Accent).Padding(8).Column(c =>
        {
            c.Item().Row(r =>
            {
                r.RelativeItem().Text(p.Name).SemiBold().FontSize(9).FontColor(TextColor);
                if (!string.IsNullOrWhiteSpace(p.Tech))
                    r.AutoItem().PaddingLeft(8).Text(p.Tech).FontSize(7.5f).FontColor(Light);
            });
            if (!string.IsNullOrWhiteSpace(p.Scope))
                c.Item().Text(p.Scope).Italic().FontSize(8).FontColor(Secondary);
            if (p.Details.Count > 0)
                c.Item().PaddingTop(2).Column(b =>
                {
                    foreach (var d in p.Details) Bullet(b, d);
                });
        });
    }

    private static void EduBlock(ColumnDescriptor col, EducationDto e)
    {
        col.Item().PaddingTop(8).Column(c =>
        {
            c.Item().Row(r =>
            {
                r.RelativeItem().Text(e.Institution).SemiBold().FontSize(10).FontColor(TextColor);
                r.AutoItem().PaddingLeft(8).Text(e.Period).FontSize(8).FontColor(Light);
            });
            if (!string.IsNullOrWhiteSpace(e.Qualification))
                c.Item().Text(e.Qualification).Italic().FontSize(8.5f).FontColor(Secondary);
            if (!string.IsNullOrWhiteSpace(e.Location))
                c.Item().Text(e.Location).FontSize(8).FontColor(Light);
        });
    }

    private static void Bullet(ColumnDescriptor col, string text)
    {
        col.Item().PaddingBottom(2).Row(r =>
        {
            r.ConstantItem(10).Text("•").FontColor(Accent).FontSize(8.5f);
            r.RelativeItem().Text(text).FontSize(8.5f).FontColor(TextColor);
        });
    }

    private static void SkillBar(ColumnDescriptor col, string name, int years, int maxYears)
    {
        var frac = Math.Clamp(years / (float)maxYears, 0.06f, 1f);
        col.Item().PaddingBottom(5).Column(s =>
        {
            s.Item().Row(r =>
            {
                r.RelativeItem().Text(name).FontSize(8.5f).SemiBold().FontColor(TextColor);
                r.AutoItem().PaddingLeft(6).Text($"{years} yrs").FontSize(7.5f).FontColor(Light);
            });
            s.Item().PaddingTop(2).Height(3).Background(BorderColor).Row(bar =>
            {
                bar.RelativeItem(frac).Background(Accent);
                if (frac < 1f) bar.RelativeItem(1f - frac);
            });
        });
    }

    private static void InfoRow(ColumnDescriptor col, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        col.Item().PaddingBottom(3).Row(r =>
        {
            r.ConstantItem(66).Text(label).FontSize(7.8f).FontColor(Secondary);
            r.RelativeItem().Text(value).FontSize(7.8f).FontColor(TextColor);
        });
    }

    private static string ShortUrl(string url) =>
        url.Replace("https://", "").Replace("http://", "").Replace("www.", "").TrimEnd('/');

    private static void Section(ColumnDescriptor col, string title)
    {
        col.Item().PaddingTop(15).BorderBottom(1.5f).BorderColor(Accent).PaddingBottom(5)
           .Text(title.ToUpperInvariant()).FontSize(9).Bold().FontColor(Accent);
    }
}
