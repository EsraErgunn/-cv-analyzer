using Shared.Skills;

namespace CvAnalyzer.Tests;

public class SkillCatalogTests
{
    [Fact]
    public void FindSkills_MatchesSymbolNames()
    {
        var skills = SkillCatalog.FindSkills("5 yıl C#, .NET ve ASP.NET Core ile backend geliştirme; C++ bilgisi.");

        Assert.Contains("C#", skills);
        Assert.Contains(".NET", skills);
        Assert.Contains("ASP.NET Core", skills);
        Assert.Contains("C++", skills);
    }

    [Fact]
    public void FindSkills_DoesNotMatchInsideLongerWords()
    {
        var skills = SkillCatalog.FindSkills("JavaScript ve TypeScript ile frontend.");

        Assert.Contains("JavaScript", skills);
        Assert.DoesNotContain("Java", skills);
    }

    [Fact]
    public void FindSkills_ResolvesAliasesToCanonicalName()
    {
        var skills = SkillCatalog.FindSkills("postgres, k8s, golang, mikroservis mimarisi");

        Assert.Equal(new[] { "Go", "PostgreSQL", "Kubernetes", "Microservices" }, skills);
    }

    [Fact]
    public void FindSkills_EmptyText_ReturnsEmpty()
    {
        Assert.Empty(SkillCatalog.FindSkills("   "));
    }
}
