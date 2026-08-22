using FirebirdAdmin.Presentation.Wpf.Resources;
using FluentAssertions;

namespace FirebirdAdmin.Ui.Tests;

public sealed class ResourceTests
{
    [Fact]
    public void AppStrings_ShouldResolveVisibleShellStrings()
    {
        AppStrings.AppName.Should().Be("Firebird Admin");
        AppStrings.ConnectionContextEmpty.Should().NotBeNullOrWhiteSpace();
        AppStrings.WorkspacePlaceholder.Should().Contain("Sprint 1");
    }
}
