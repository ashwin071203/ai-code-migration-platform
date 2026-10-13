using MigrationPlatform.Data;
using MigrationPlatform.MigrationEngine.Implementations;
using MigrationPlatform.MigrationEngine.Interfaces;
using MigrationPlatform.Services;
using Microsoft.EntityFrameworkCore;

using MigrationPlatform.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<MigrationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IMigrationProviderFactory, MigrationProviderFactory>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
