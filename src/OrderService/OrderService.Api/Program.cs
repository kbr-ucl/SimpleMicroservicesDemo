using OrderService.Application;
using OrderService.Application.Command;
using OrderService.Application.Command.CommandDto;
using OrderService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Application and Infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // OBS !!!


app.MapPost("/Order", (OrderDto orderDto, IOrderCommand command) =>
    {
        var data = new CreateOrderCommandDto(orderDto.CustomerId, orderDto.OrderAmount);
        command.CreateOrder(data);
        return Results.Created();
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();

public record OrderDto(
    int CustomerId,
    double OrderAmount);