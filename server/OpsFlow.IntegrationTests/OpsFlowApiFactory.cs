using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpsFlow.Infrastructure.Persistence;

namespace OpsFlow.IntegrationTests;

public sealed class OpsFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"opsflow-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OpsFlowDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<OpsFlowDbContext>>();
            services.AddDbContext<OpsFlowDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
