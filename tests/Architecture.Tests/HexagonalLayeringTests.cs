using NetArchTest.Rules;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Enforces the Ports &amp; Adapters (Hexagonal) dependency direction for every service:
/// Domain has no outward dependency, Application only depends on Domain (and shared
/// contracts), and neither depends on Infrastructure or Api.
/// </summary>
public sealed class HexagonalLayeringTests
{
    private static readonly string[] Services = ["Proposta", "Contratacao", "Analise"];

    public static IEnumerable<object[]> ServiceNames => Services.Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(ServiceNames))]
    public void Domain_Should_Not_Depend_On_Infrastructure_Or_Api(string service)
    {
        var domainAssembly = LoadAssembly($"{service}.Domain");

        var result = Types.InAssembly(domainAssembly)
            .Should()
            .NotHaveDependencyOnAny($"{service}.Infrastructure", $"{service}.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, DescribeFailures(result));
    }

    [Theory]
    [MemberData(nameof(ServiceNames))]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api(string service)
    {
        var applicationAssembly = LoadAssembly($"{service}.Application");

        var result = Types.InAssembly(applicationAssembly)
            .Should()
            .NotHaveDependencyOnAny($"{service}.Infrastructure", $"{service}.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, DescribeFailures(result));
    }

    [Theory]
    [MemberData(nameof(ServiceNames))]
    public void Application_Should_Not_Depend_Directly_On_Infrastructure_Frameworks(string service)
    {
        var applicationAssembly = LoadAssembly($"{service}.Application");

        var result = Types.InAssembly(applicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "MassTransit",
                "Microsoft.AspNetCore.Mvc",
                "Polly")
            .GetResult();

        Assert.True(result.IsSuccessful, DescribeFailures(result));
    }

    private static System.Reflection.Assembly LoadAssembly(string assemblyName) =>
        System.Reflection.Assembly.Load(assemblyName);

    private static string DescribeFailures(TestResult result) =>
        result.FailingTypes is null
            ? "Architecture rule violated."
            : "Architecture rule violated by: " + string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}

