using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecruitmentAts.Application.Abstractions.Time;

namespace RecruitmentAts.Infrastructure.Tests.Time;

public class SystemClockTests
{
    [Fact]
    public void AddInfrastructure_registers_a_clock_that_returns_utc()
    {
        using var provider = new ServiceCollection()
            .AddInfrastructure(new ConfigurationBuilder().Build())
            .BuildServiceProvider();

        var now = provider.GetRequiredService<IClock>().UtcNow;

        Assert.Equal(TimeSpan.Zero, now.Offset);
    }
}
