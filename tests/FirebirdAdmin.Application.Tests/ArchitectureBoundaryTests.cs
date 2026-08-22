using System.Xml.Linq;
using FluentAssertions;

namespace FirebirdAdmin.Application.Tests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void Domain_ShouldNotReferenceOtherProjects()
    {
        var project = LoadProject("src", "FirebirdAdmin.Domain", "FirebirdAdmin.Domain.csproj");

        GetProjectReferences(project).Should().BeEmpty();
    }

    [Fact]
    public void Application_ShouldReferenceDomainOnly()
    {
        var project = LoadProject("src", "FirebirdAdmin.Application", "FirebirdAdmin.Application.csproj");

        GetProjectReferences(project).Should().ContainSingle()
            .Which.Should().EndWith(@"FirebirdAdmin.Domain\FirebirdAdmin.Domain.csproj");
    }

    [Fact]
    public void Application_ShouldNotTargetWpf()
    {
        var project = LoadProject("src", "FirebirdAdmin.Application", "FirebirdAdmin.Application.csproj");

        GetTargetFramework(project).Should().NotContain("-windows");
        project.ToString().Should().NotContain("UseWPF");
    }

    [Fact]
    public void Presentation_ShouldNotReferenceInfrastructure()
    {
        var project = LoadProject("src", "FirebirdAdmin.Presentation.Wpf", "FirebirdAdmin.Presentation.Wpf.csproj");

        GetProjectReferences(project).Should().NotContain(reference => reference.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
    }

    private static XDocument LoadProject(params string[] pathParts)
    {
        var root = FindRepositoryRoot();
        return XDocument.Load(Path.Combine([root, .. pathParts]));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FirebirdAdmin.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find FirebirdAdmin.sln.");
    }

    private static string GetTargetFramework(XDocument project)
    {
        return project.Descendants("TargetFramework").SingleOrDefault()?.Value
            ?? File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Directory.Build.props"));
    }

    private static IReadOnlyList<string> GetProjectReferences(XDocument project)
    {
        return project.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }
}
