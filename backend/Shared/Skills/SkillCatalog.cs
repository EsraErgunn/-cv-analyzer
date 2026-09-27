using System.Text.RegularExpressions;

namespace Shared.Skills;

/// <summary>
/// Bilinen teknik/yetkinlik anahtar kelimeleri. Hem CV'den hem iş ilanından beceri çıkarmak için kullanılır.
/// Her kaydın ilk elemanı kanonik addır, diğerleri eş anlamlılarıdır.
/// </summary>
public static class SkillCatalog
{
    private static readonly string[][] Entries =
    [
        ["C#", "csharp", "c sharp"],
        [".NET", "dotnet", ".net core", ".net framework"],
        ["ASP.NET Core", "asp.net", "asp.net mvc", "asp.net web api"],
        ["Entity Framework", "ef core", "entity framework core"],
        ["Java"], ["Spring Boot", "spring"], ["Kotlin"],
        ["Python"], ["Django"], ["Flask"], ["FastAPI"],
        ["JavaScript", "js"], ["TypeScript", "ts"],
        ["Node.js", "nodejs", "node"], ["React", "react.js", "reactjs"],
        ["Angular"], ["Vue.js", "vue", "vuejs"], ["Next.js", "nextjs"],
        ["HTML"], ["CSS"], ["Tailwind", "tailwindcss"],
        ["Go", "golang"], ["Rust"], ["C++", "cpp"], ["PHP"], ["Ruby"], ["Swift"],
        ["SQL"], ["PostgreSQL", "postgres"], ["MySQL"], ["MS SQL Server", "sql server", "mssql"],
        ["MongoDB", "mongo"], ["Redis"], ["Elasticsearch"], ["Oracle"],
        ["RabbitMQ"], ["Kafka", "apache kafka"], ["gRPC"], ["GraphQL"], ["REST", "rest api", "restful"],
        ["Docker"], ["Kubernetes", "k8s"], ["Helm"], ["Terraform"], ["Ansible"],
        ["AWS", "amazon web services"], ["Azure", "microsoft azure"], ["GCP", "google cloud"],
        ["CI/CD", "ci cd", "continuous integration"], ["GitHub Actions"], ["Jenkins"], ["GitLab CI"],
        ["Git"], ["Linux"],
        ["Microservices", "microservice", "mikroservis", "mikroservisler"],
        ["Clean Architecture"], ["DDD", "domain driven design"], ["CQRS"],
        ["Event-Driven Architecture", "event driven", "event-driven"],
        ["Design Patterns", "tasarım desenleri"], ["SOLID"], ["TDD", "test driven development"],
        ["Unit Testing", "unit test", "birim test", "xunit", "nunit"],
        ["Machine Learning", "makine öğrenmesi", "ml"], ["Deep Learning", "derin öğrenme"],
        ["TensorFlow"], ["PyTorch"], ["Pandas"], ["NumPy"], ["OpenCV"], ["LLM", "large language models"],
        ["Agile", "çevik"], ["Scrum"], ["Jira"],
        ["English", "ingilizce"], ["Communication", "iletişim"], ["Teamwork", "takım çalışması"]
    ];

    private static readonly (string Name, Regex Pattern)[] CompiledEntries = Entries
        .Select(entry => (entry[0], BuildPattern(entry)))
        .ToArray();

    /// <summary>Metinde geçen becerilerin kanonik adlarını, katalog sırasıyla döner.</summary>
    public static List<string> FindSkills(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        return CompiledEntries
            .Where(e => e.Pattern.IsMatch(text))
            .Select(e => e.Name)
            .ToList();
    }

    private static Regex BuildPattern(string[] aliases)
    {
        // Harf/rakam sınırları kullanılır; \b "C#" veya ".NET" gibi sembollü adlarda çalışmaz.
        var alternatives = string.Join("|", aliases
            .OrderByDescending(a => a.Length)
            .Select(a => Regex.Escape(a).Replace(@"\ ", @"[\s\-]+")));

        return new Regex(
            $@"(?<![\p{{L}}\p{{N}}.#+]){"(?:" + alternatives + ")"}(?![\p{{L}}\p{{N}}#+])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
