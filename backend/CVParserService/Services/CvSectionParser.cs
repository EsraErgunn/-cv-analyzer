using System.Text.RegularExpressions;
using Shared.Skills;

namespace CVParserService.Services;

public record ParsedCv(List<string> Skills, List<string> Experience, List<string> Education);

/// <summary>
/// Ham CV metnini başlıklara göre bölümlere ayırır (Türkçe ve İngilizce başlıklar desteklenir)
/// ve beceri kataloğu ile metindeki yetkinlikleri çıkarır.
/// </summary>
public class CvSectionParser
{
    private enum Section { None, Skills, Experience, Education, Other }

    private static readonly (Section Section, Regex Heading)[] Headings =
    [
        (Section.Skills, HeadingRegex("skills", "technical skills", "core competencies", "competencies",
            "technologies", "yetenekler", "beceriler", "yetkinlikler", "teknik beceriler", "teknolojiler")),
        (Section.Experience, HeadingRegex("experience", "work experience", "professional experience",
            "employment history", "deneyim", "iş deneyimi", "deneyimler", "profesyonel deneyim", "tecrübe")),
        (Section.Education, HeadingRegex("education", "academic background", "eğitim", "eğitim bilgileri",
            "öğrenim")),
        (Section.Other, HeadingRegex("projects", "projeler", "certificates", "certifications", "sertifikalar",
            "languages", "diller", "references", "referanslar", "summary", "profile", "about me", "özet",
            "hakkımda", "interests", "hobiler", "contact", "iletişim bilgileri"))
    ];

    private static readonly char[] BulletChars = ['•', '●', '▪', '◦', '-', '*', '–', '·'];

    public ParsedCv Parse(string rawText)
    {
        var sections = new Dictionary<Section, List<string>>
        {
            [Section.Skills] = new(),
            [Section.Experience] = new(),
            [Section.Education] = new()
        };

        var current = Section.None;

        foreach (var rawLine in rawText.Split('\n'))
        {
            var line = rawLine.Trim().TrimStart(BulletChars).Trim();
            if (line.Length == 0)
                continue;

            var heading = Headings.FirstOrDefault(h => h.Heading.IsMatch(line));
            if (heading.Heading != null)
            {
                current = heading.Section;
                continue;
            }

            if (sections.TryGetValue(current, out var lines))
                lines.Add(line);
        }

        // Beceriler bölümü bulunamasa bile katalogla tüm metinden çıkarılır.
        var skills = SkillCatalog.FindSkills(rawText);

        return new ParsedCv(skills, sections[Section.Experience], sections[Section.Education]);
    }

    private static Regex HeadingRegex(params string[] titles)
    {
        // Başlık satırı: yalnızca başlığın kendisi (ve isteğe bağlı ":") içeren kısa satır.
        var alternatives = string.Join("|", titles.Select(Regex.Escape));
        return new Regex($@"^(?:{alternatives})\s*:?$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
