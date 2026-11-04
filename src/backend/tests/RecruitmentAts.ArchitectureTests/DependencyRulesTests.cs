using System.Reflection;
using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace RecruitmentAts.ArchitectureTests;

/// <summary>Dependency rules from docs/09-folder-structure.md §3.7.</summary>
public class DependencyRulesTests
{
    private static readonly Assembly Application = typeof(RecruitmentAts.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(RecruitmentAts.Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Api = Assembly.Load("RecruitmentAts.Api");
    private static readonly Assembly Worker = Assembly.Load("RecruitmentAts.Worker");

    [Theory]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Npgsql")]
    [InlineData("MailKit")]
    [InlineData("RecruitmentAts.Infrastructure")]
    [InlineData("RecruitmentAts.Api")]
    [InlineData("RecruitmentAts.Worker")]
    public void Application_does_not_depend_on(string forbidden)
    {
        var result = Types.InAssembly(Application).ShouldNot().HaveDependencyOn(forbidden).GetResult();

        Assert.True(result.IsSuccessful, Violations(result));
    }

    [Theory]
    [InlineData("RecruitmentAts.Api")]
    [InlineData("RecruitmentAts.Worker")]
    public void Infrastructure_does_not_depend_on_hosts(string forbidden)
    {
        var result = Types.InAssembly(Infrastructure).ShouldNot().HaveDependencyOn(forbidden).GetResult();

        Assert.True(result.IsSuccessful, Violations(result));
    }

    [Theory]
    [InlineData("RecruitmentAts.Api")]
    [InlineData("RecruitmentAts.Worker")]
    public void Hosts_use_Infrastructure_only_in_the_composition_root(string host)
    {
        var result = Types.InAssembly(Host(host))
            .That().DoNotHaveName("Program")
            .ShouldNot().HaveDependencyOn("RecruitmentAts.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Violations(result));
    }

    [Theory]
    [InlineData("RecruitmentAts.Api", "RecruitmentAts.Worker")]
    [InlineData("RecruitmentAts.Worker", "RecruitmentAts.Api")]
    public void Hosts_do_not_depend_on_each_other(string host, string other)
    {
        var result = Types.InAssembly(Host(host)).ShouldNot().HaveDependencyOn(other).GetResult();

        Assert.True(result.IsSuccessful, Violations(result));
    }

    private static Assembly Host(string name) => name == "RecruitmentAts.Api" ? Api : Worker;

    private static string Violations(TestResult result) =>
        "Violations: " + string.Join(", ", result.FailingTypeNames ?? []);
}
