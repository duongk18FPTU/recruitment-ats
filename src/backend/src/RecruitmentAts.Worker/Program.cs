using RecruitmentAts.Application;
using RecruitmentAts.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var host = builder.Build();
host.Run();
