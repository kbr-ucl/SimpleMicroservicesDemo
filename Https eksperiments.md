### Chapter 6: Add Https.



## Setup Docker certificate

#### Ensure Folder exists (Replace kbr8776 with your user):

```powershell
C:\Users\kbr8776\.aspnet\https
```

#### Make certificate

Replace kbr8776 with your user.

```yaml
dotnet dev-certs https --clean

C:\Users\kbr8776\.aspnet\https

dotnet dev-certs https -ep "C:\Users\kbr8776\.aspnet\https\dockercertifikat.pfx" -p 1234

dotnet dev-certs https --trust
```



#### File: docker-compose 

```yaml
version: '3.4'

services:
  customerservice.api:
    image: ${DOCKER_REGISTRY-}customerserviceapi
    build:
      context: .
      dockerfile: CustomerService/CustomerService.Api/Dockerfile

  orderservice.api:
    image: ${DOCKER_REGISTRY-}orderserviceapi
    build:
      context: .
      dockerfile: OrderService/OrderService.Api/Dockerfile

  mssql:
    image: "mcr.microsoft.com/mssql/server:2019-latest"


  gateway.api:
    image: ${DOCKER_REGISTRY-}gatewayapi
    build:
      context: .
      dockerfile: ApiGateway/Gateway.Api/Dockerfile


```

#### Update docker-compose.override.yml

```yaml
services:
  customerservice.api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
      - ASPNETCORE_HTTPS_PORTS=8081
      - ConnectionStrings__CustomerDbConnection=Server=mssql;Database=CustomerDb;User=sa;Password=Password1234!;MultipleActiveResultSets=true;TrustServerCertificate=true
      - ASPNETCORE_Kestrel__Certificates__Default__Password=1234
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/dockercertifikat.pfx
    ports:
      - "18080:8080"
      - "18081:8081"
    volumes:
      # - ${APPDATA}/Microsoft/UserSecrets:/home/app/.microsoft/usersecrets:ro
      - ~/.aspnet/https:/https:ro
    depends_on:
      - mssql
  
  orderservice.api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
      - ASPNETCORE_HTTPS_PORTS=8081
      - ConnectionStrings:OrderDbConnection=Server=mssql;Database=OrderDb;User=sa;Password=Password1234!;MultipleActiveResultSets=true;TrustServerCertificate=true
      - ExternalServices__Customer__Uri=http://customerservice.api:8080
      - ASPNETCORE_Kestrel__Certificates__Default__Password=1234
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/dockercertifikat.pfx
    ports:
      - "28080:8080"
      - "28081:8081"
    volumes:
      # - ${APPDATA}/Microsoft/UserSecrets:/home/app/.microsoft/usersecrets:ro
      - ~/.aspnet/https:/https:ro
    depends_on:
      - mssql

  mssql:
    restart: always
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "Password1234!"
    ports:
      - 11433:1433 

  gateway.api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
      - ASPNETCORE_HTTPS_PORTS=8081
      - ASPNETCORE_Kestrel__Certificates__Default__Password=1234
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/dockercertifikat.pfx
    ports:
      - "38080:8080"
      - "38081:8081"
    volumes:
      # - ${APPDATA}/Microsoft/UserSecrets:/home/app/.microsoft/usersecrets:ro
      - ~/.aspnet/https:/https:ro

```



#### Turn off Https Redirection for all services but YARP

##### Gateway.Api

Program.cs

```c#
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseHttpsRedirection(); // OBS !!!

app.MapReverseProxy();
app.Run();
```

##### CustomerService.Api 

Program.cs

```c#
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

// app.UseHttpsRedirection(); // OBS !!!


app.MapGet("/Customer/{id}", (int id, ICustomerQuery query) =>
{
    var result = query.GetCustomer(id);
    return result;
});


app.Run();
```



##### OrderService.Api
Program.cs

```c#
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
```



## Notes used in the process

Branch: CH-06-After

Se: https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-8.0&tabs=visual-studio%2Clinux-sles

Som administrator:

```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```



### All platforms - certificate not trusted

Run the following commands:

.NET CLI



### Docker - certificate not trusted

- Delete the *C:\Users{USER}\AppData\Roaming\ASP.NET\Https* folder.
- Clean the solution. Delete the *bin* and *obj* folders.
- Restart the development tool. For example, Visual Studio or Visual Studio Code.



### Windows - certificate not trusted

- Check the certificates in the certificate store. There should be a `localhost` certificate with the `ASP.NET Core HTTPS development certificate` friendly name both under `Current User > Personal > Certificates` and `Current User > Trusted root certification authorities > Certificates`
- Remove all the found certificates from both Personal and Trusted root certification authorities. Do **not** remove the IIS Express localhost certificate.
- Run the following commands:

.NET CLI

```dotnetcli
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Close any browser instances open. Open a new browser window to app.

