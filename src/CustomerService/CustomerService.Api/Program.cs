using CustomerService.Application.Query;
using CustomerService.Infrastructure;
using CustomerService.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);



var app = builder.Build();
SeedDatabase.UpdateDatabase(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();



app.MapGet("/Customer/{id}", (int id, ICustomerQuery query) =>
{
var result = query.GetCustomer(id);
    return result;
});


app.Run();

