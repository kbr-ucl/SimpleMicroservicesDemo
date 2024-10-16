﻿using CustomerService.Application.Query;
using CustomerService.Infrastructure.Database;
using CustomerService.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        //services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<ICustomerQuery, CustomerQuery>();

        //// External services
        //services.AddHttpClient<IDawaProxy, DawaProxy>(client =>
        //{
        //    var uri = configuration.GetSection("ExternalServices:Dawa:Uri").Value;
        //    Debug.Assert(string.Empty != null, "String.Empty != null");
        //    client.BaseAddress = new Uri(uri ?? string.Empty);
        //});

        //services.AddHttpClient<IBookMyHomeProxy, BookMyHomeProxy>(client =>
        //{
        //    var uri = configuration.GetSection("ExternalServices:BookMyHome:Uri").Value;
        //    Debug.Assert(string.Empty != null, "String.Empty != null");
        //    client.BaseAddress = new Uri(uri ?? string.Empty);
        //});


        // Database
        // https://github.com/dotnet/SqlClient/issues/2239
        // https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/projects?tabs=dotnet-core-cli
        // Add-Migration InitialMigration -Context AddressContext -Project AddressManager.DatabaseMigration
        // Update-Database -Context AddressContext -Project AddressManager.DatabaseMigration
        services.AddDbContext<CustomerContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString
                    ("CustomerDbConnection"),
                x =>
                    x.MigrationsAssembly("AddressManager.DatabaseMigration")));


        return services;
    }
}