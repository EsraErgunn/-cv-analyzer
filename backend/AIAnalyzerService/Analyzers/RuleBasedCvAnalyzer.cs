using System.Text.RegularExpressions;
using Shared.Events;
using Shared.Skills;

namespace AIAnalyzerService.Analyzers;

/// <summary>
/// Deterministik, kural tabanlı ATS analizi.
/// Puan: beceri eşleşmesi %60, deneyim %15, eğitim %10, iletişim bilgisi %10, uygun uzunluk %5.
/// </summary>
public partial class RuleBasedCvAnalyzer : ICvAnalyzer
{
    public string Name => "rule-based";

    public Task<CvAnalysis> AnalyzeAsync(CvParsedEvent cv, CancellationToken cancellationToken) =>
        Task.FromResult(Analyze(cv));

    public CvAnalysis Analyze(CvParsedEvent cv)
    {
        var cvSkills = cv.Skills.Count > 0 ? cv.Skills : SkillCatalog.FindSkills(cv.RawText);
        var jobSkills = SkillCatalog.FindSkills(cv.JobDescription);

        var cvSkillSet = new HashSet<string>(cvSkills, StringComparer.OrdinalIgnoreCase);
        var matched = jobSkills.Where(cvSkillSet.Contains).ToList();
        var missing = jobSkills.Where(s => !cvSkillSet.Contains(s)).ToList();

        // İlanda tanınan beceri yoksa CV'deki beceri çeşitliliğine göre kısmi puan verilir.
        double skillRatio = jobSkills.Count > 0
            ? (double)matched.Count / jobSkills.Count
            : Math.Min(cvSkills.Count / 10.0, 1.0) * 0.7;

        var hasExperience = cv.Experience.Count > 0;
        var hasEducation = cv.Education.Count > 0;
        var hasContact = EmailRegex().IsMatch(cv.RawText) || PhoneRegex().IsMatch(cv.RawText);
        var wordCount = cv.RawText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var goodLength = wordCount is >= 200 and <= 1200;

        var score = skillRatio * 60
            + (hasExperience ? 15 : 0)
            + (hasEducation ? 10 : 0)
            + (hasContact ? 10 : 0)
            + (goodLength ? 5 : 0);

        var suggestions = new List<string>();

        if (missing.Count > 0)
            suggestions.Add($"İlanda istenen şu becerileri (sahipseniz) CV'nize açıkça ekleyin: {string.Join(", ", missing)}.");
        if (!hasExperience)
            suggestions.Add("\"Deneyim\" / \"Experience\" başlıklı ayrı bir bölüm ekleyin; ATS sistemleri standart başlıkları arar.");
        if (!hasEducation)
            suggestions.Add("\"Eğitim\" / \"Education\" başlıklı bir bölüm ekleyin.");
        if (!hasContact)
            suggestions.Add("E-posta ve telefon gibi iletişim bilgilerini CV'nin en üstüne ekleyin.");
        if (wordCount < 200)
            suggestions.Add("CV çok kısa; projelerinizi ve sorumluluklarınızı ölçülebilir sonuçlarla detaylandırın.");
        if (wordCount > 1200)
            suggestions.Add("CV çok uzun; ilanla ilgisiz kısımları çıkararak 1-2 sayfaya indirin.");
        if (suggestions.Count == 0)
            suggestions.Add("CV ilanla iyi örtüşüyor; başarılarınızı sayısal sonuçlarla (%, süre, kullanıcı sayısı) güçlendirin.");

        var summary = jobSkills.Count > 0
            ? $"İlandaki {jobSkills.Count} becerinin {matched.Count} tanesi CV'de bulundu."
            : "İlanda tanınan bir teknik beceri bulunamadı; puan CV'nin genel yapısına göre hesaplandı.";

        return new CvAnalysis(
            AtsScore: (int)Math.Round(Math.Clamp(score, 0, 100)),
            MatchedSkills: matched,
            MissingSkills: missing,
            Suggestions: suggestions,
            Summary: summary,
            ImprovedCvText: string.Empty);
    }

    [GeneratedRegex(@"[\w.+-]+@[\w-]+\.[\w.-]+")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\+?\d[\d\s()-]{8,}\d")]
    private static partial Regex PhoneRegex();
}
