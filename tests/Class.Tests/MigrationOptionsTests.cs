using Class.Infrastructure.Migrations;
using FluentAssertions;
using Xunit;

namespace Class.Tests;

/// <summary>
/// Unit tests for MigrationOptions parsing logic.
/// Tests ShouldApplyMigrations() by exercising its inputs through environment and raw config values.
/// No real database or EF context is required.
/// </summary>
public sealed class MigrationOptionsTests
{
    [Fact]
    public void Development_environment_defaults_to_true_when_variable_is_absent()
    {
        var apply = MigrationOptions.Resolve(environment: "Development", rawValue: null);

        apply.Should().BeTrue();
    }

    [Fact]
    public void Production_environment_defaults_to_false_when_variable_is_absent()
    {
        var apply = MigrationOptions.Resolve(environment: "Production", rawValue: null);

        apply.Should().BeFalse();
    }

    [Fact]
    public void Staging_environment_defaults_to_false_when_variable_is_absent()
    {
        var apply = MigrationOptions.Resolve(environment: "Staging", rawValue: null);

        apply.Should().BeFalse();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("  true  ")]
    public void AUTO_APPLY_MIGRATIONS_true_enables_regardless_of_environment(string raw)
    {
        var applyDev = MigrationOptions.Resolve(environment: "Development", rawValue: raw);
        var applyProd = MigrationOptions.Resolve(environment: "Production", rawValue: raw);

        applyDev.Should().BeTrue();
        applyProd.Should().BeTrue();
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData("0")]
    [InlineData("no")]
    public void AUTO_APPLY_MIGRATIONS_false_disables_regardless_of_environment(string raw)
    {
        var applyDev = MigrationOptions.Resolve(environment: "Development", rawValue: raw);
        var applyProd = MigrationOptions.Resolve(environment: "Production", rawValue: raw);

        applyDev.Should().BeFalse();
        applyProd.Should().BeFalse();
    }

    [Fact]
    public void Empty_string_is_treated_as_absent_and_falls_back_to_environment_default()
    {
        var applyDev = MigrationOptions.Resolve(environment: "Development", rawValue: "");
        var applyProd = MigrationOptions.Resolve(environment: "Production", rawValue: "  ");

        applyDev.Should().BeTrue();
        applyProd.Should().BeFalse();
    }
}
