# SimpleMicroservicesDemo
Simple Microservices Demo. A very simple system with a Customer and a Order service. When a new order is created, the OrderService ask the Customer fore the creditmax. A new order is only accepted is it is below credit max.



## Agenda

1. Create OrderService and CustomerServices using a Onion template (KbrOnionTemplate-Core-8.bat)
2. Implement CustomerService using a MSSSQL database with seeded data
3. Implement OrderService using a MSSSQL database. And using a domain service to check creditmax. The domain service uses a CustomerProxy (HttpClient) to talk to the CustomerService.
4. Run in docker
5. Add Https
6. Add API Gateway - YARP



### Chaper 1: Create OrderService and CustomerServices using a Onion template

Branch: CH-01

CustomerService:

```powershell
KbrOnionTemplate-Core-8.bat
Enter application name: CustomerService
```

cd..

OrderService

```powershell
KbrOnionTemplate-Core-8.bat
Enter application name: OrderService
```



Create a empty solution to include both services.

The solution looks like this:

![](Images\CH-01-Folders-01.jpg)



### Chapter 2: Implement CustomerService using a MSSSQL database with seeded data

Branch: CH-02

Crosscut projekt created

I de enkelte projekter implementeres: DependencyInjection som udfylder IoC med klasser fra dette projekt.

Domain - Customer oprettet.

Infrastructure:

- CustomerContext oprettet
- DependencyInjection udfyldt
- DB connection string: CustomerDbConnection som er i API projektet appsettings.json filen



Query oprettes: 

```csharp
namespace CustomerService.Application.Query;

public interface ICustomerQuery
{
    CustomerDto
        GetCustomer(int id);
}
```



Interface implementeres i infrastructure og oprettes i DependencyInjection.



API Endpoint oprettes i program.cs:

```csharp
app.MapGet("/Customer/{id}", (int id, ICustomerQuery query) =>
{
var result = query.GetCustomer(id);
    return result;
});
```





Det sikres at databasen er oprettet - og hvis ikke så opret den:

```csharp
    public CustomerContext(DbContextOptions<CustomerContext> options) : base(options)
    {
        Database.EnsureCreated();
    }
```

Seed af data:

```csharp
namespace CustomerService.Infrastructure.Database;

public class SeedDatabase
{
    public static void UpdateDatabase(IServiceProvider ioc)
    {
        using (var serviceScope = ioc.CreateScope())
        {
            var db = serviceScope.ServiceProvider.GetRequiredService<CustomerContext>();
            if (!db.Customers.Any())
            {
                db.Customers.Add(Customer.Create(1000));
                db.SaveChanges();
            }
        }
    }
}
```



i Program.cs:

```csharp
var app = builder.Build();
SeedDatabase.UpdateDatabase(app.Services);
```



### Chapter 3: Implement OrderService using a MSSSQL database. And using a domain service to check creditmax. The domain service uses a CustomerProxy (HttpClient) to talk to the CustomerService.

Branch: CH-03

I de enkelte projekter implementeres: DependencyInjection som udfylder IoC med klasser fra dette projekt.

Domain - Order oprettet.

```csharp
public class Order : EntityBase
{
    private Order()
    {
    }

    protected Order(int customerId, double orderAmount, ICustomerProxy customerService)
    {
        CustomerId = customerId;
        OrderAmount = orderAmount;
        if (!IsWithinCreditLimit(customerService)) throw new Exception("Order amount exceeds customer credit limit");
    }

    public int CustomerId { get; protected set; }
    public double OrderAmount { get; protected set; }

    public static Order Create(int customerId, double orderAmount, IServiceProvider serviceProvider)
    {
        var customerService = serviceProvider.GetRequiredService<ICustomerProxy>();


        return new Order(customerId, orderAmount, customerService);
    }

    protected bool IsWithinCreditLimit(ICustomerProxy customerService)
    {
        var creditLimit = customerService.GetCustomerCreditLimit(CustomerId).Result;
        return OrderAmount <= creditLimit;
    }
}
```



Infrastructure:

- OrderContext oprettet
- DependencyInjection udfyldt
- DB connection string: OrderDbConnection som er i API projektet appsettings.json filen



CustomerProxy

```csharp
public class CustomerProxy : ICustomerProxy
{
    private readonly HttpClient _client;
    private readonly ILogger<CustomerProxy> _logger;

    public CustomerProxy(HttpClient client, ILogger<CustomerProxy> logger)
    {
        _client = client;
        _logger = logger;
    }

    async Task<double> ICustomerProxy.GetCustomerCreditLimit(int customerId)
    {
        var requestUri = $"/AddressHandler/{customerId}";
        var response = await _client.GetFromJsonAsync<double>(requestUri);

        return response;
    }
}
```



IoC opsætning til CustomerProxy

```csharp
        // External services
        services.AddHttpClient<ICustomerProxy, CustomerProxy>(client =>
        {
            var uri = configuration.GetSection("ExternalServices:Customer:Uri").Value;
            Debug.Assert(string.Empty != null, "String.Empty != null");
            client.BaseAddress = new Uri(uri ?? string.Empty);
        });
```



I Application:

OrderCommand oprettes

OrderRepository oprettes i Infrastructure



I API endpoint oprettes:

```csharp
app.MapPost("/Order", (OrderDto orderDto, IOrderCommand command) =>
    {
        var data = new CreateOrderCommandDto(orderDto.CustomerId, orderDto.OrderAmount);
        command.CreateOrder(date);
        return Results.Created();
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();

public record OrderDto(
    int CustomerId,
    double OrderAmount);
```





### Chapter 4: Run in docker

Make Dockerfiles

Make Docker Compose file

Changes in Program.cs files

```csharp
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();


```



CustomerService.Api - add Docker Orchestration support

OrderService.Api - add Docker Orchestration support

Add SQL image:

```yaml
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


```



```yaml
services:
  customerservice.api:
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      ASPNETCORE_HTTP_PORTS: 8080
      ASPNETCORE_HTTPS_PORTS: 8081
      "ConnectionStrings:CustomerDbConnection": "Server=mssql;Database=CustomerDb;User=sa;Password=Password1234!;MultipleActiveResultSets=true;TrustServerCertificate=true"
    ports:
      - "18080:8080"
      - "18081:8081"
    volumes:
      - ${APPDATA}/Microsoft/UserSecrets:/home/app/.microsoft/usersecrets:ro
      - ${APPDATA}/ASP.NET/Https:/home/app/.aspnet/https:ro
    depends_on:
      - mssql
  
  orderservice.api:
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      ASPNETCORE_HTTP_PORTS: 8080
      ASPNETCORE_HTTPS_PORTS: 8081
      "ConnectionStrings:OrderDbConnection": "Server=mssql;Database=OrderDb;User=sa;Password=Password1234!;MultipleActiveResultSets=true;TrustServerCertificate=true"
      "ExternalServices:Customer:Uri": "http://customerservice.api:8080"
    ports:
      - "28080:8080"
      - "28081:8081"
    volumes:
      - ${APPDATA}/Microsoft/UserSecrets:/home/app/.microsoft/usersecrets:ro
      - ${APPDATA}/ASP.NET/Https:/home/app/.aspnet/https:ro
    depends_on:
      - mssql

  mssql:
    restart: always
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "Password1234!"
    ports:
      - 11433:1433 

```

### Chapter 5: Add Https.

Branch: CH-05-After

Opret folderen: 

```powershell
C:\Users\%USERPROFILE%\.aspnet\https
```

Hvor du erstatter %USERPROFILE% med dit lokale brugernavn  - f.eks. kbr8776

Fra: https://www.yogihosting.com/docker-compose-aspnet-core/#generate

```powershell
dotnet dev-certs https -ep %USERPROFILE%\.aspnet\https\aspnetapp.pfx -p mypass123
```

Hvor du erstatter %USERPROFILE% med dit lokale brugernavn  - f.eks. kbr8776

```powershell
dotnet dev-certs https --trust
```

Denne efterfølgende del er ikke nødvendig,vi bruger docker-compose.
yml filen til at sætte certifikatet op.

*Opret folderen https i både CustomerService.Api og OrderService.Api projekterne.*

*Kopier filerne fra %USERPROFILE%\.aspnet\https til de to projekter.*

*I docker-compose.yml filen tilføjes:*
```yaml
    environment:
      ASPNETCORE_Kestrel__Certificates__Default__Password: "mypass123"
      ASPNETCORE_Kestrel__Certificates__Default__Path: "/https/aspnetapp.pfx"
```



### Chapter 6: Add API Gateway - YARP

Branch: CH-06-After

Instruktion: How To Build an API Gateway for Microservices with YARP https://www.youtube.com/watch?v=UidT7YYu97s&t=52s

Og: https://microsoft.github.io/reverse-proxy/articles/getting-started.html

Add new solution folder: ApiGateway

Add new project: ASP.NET Core Web API - projectname: Gateway.Api

Location: SimpleMicroservicesDemo\src\ApiGateway

Add docker orchestration support

Add YARP (nuget)

Gateway.Api.csproj indeholder nu:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>4a3796f8-74cc-47d2-b406-3b23ee9ccd26</UserSecretsId>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
    <DockerfileContext>..\..</DockerfileContext><DockerComposeProjectPath>..\..\docker-compose.dcproj</DockerComposeProjectPath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.10" />
    <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.21.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.9.0" />
    <PackageReference Include="Yarp.ReverseProxy" Version="2.2.0" />
  </ItemGroup>

</Project>
```



Tilret Program.cs:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
var app = builder.Build();
app.MapReverseProxy();
app.Run();
```

Tilret appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ReverseProxy": {
    "Routes": {
      "customers-route": {
        "ClusterId": "customers-cluster",
        "Match": {
          "Path": "/customers/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "{**catch-all}"
          }
        ]
      },
      "orders-route": {
        "ClusterId": "orders-cluster",
        "Match": {
          "Path": "/orders/{**catch-all}"
        },
        "Transforms": [
          {
            "PathPattern": "{**catch-all}"
          }
        ]
      }
    },
    "Clusters": {
      "customers-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://customerservice.api:8080"
          }
        }
      }
    },
    "orders-cluster": {
      "Destinations": {
        "destination1": {
          "Address": "http://orderservice.api:8080"
        }
      }
    }

  }
}
```



Ny docker compose:



```yaml
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



```yaml
services:
  customerservice.api:
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      ASPNETCORE_HTTP_PORTS: 8080
      ASPNETCORE_HTTPS_PORTS: 8081
      "ConnectionStrings:CustomerDbConnection": "Server=mssql;Database=CustomerDb;User=sa;Password=Password1234!;MultipleActiveResultSets=true;TrustServerCertificate=true"
    ports:
      - "18080:8080"
      - "18081:8081"
    volumes:
      - ${APPDATA}/Microsoft/UserSecrets:/home/app/.microsoft/usersecrets:ro
      - ${APPDATA}/ASP.NET/Https:/home/app/.aspnet/https:ro
    depends_on:
      - mssql
  
  orderservice.api:
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      ASPNETCORE_HTTP_PORTS: 8080
      ASPNETCORE_HTTPS_PORTS: 8081
      "ConnectionStrings:OrderDbConnection": "Server=mssql;Database=OrderDb;User=sa;Password=Password1234!;MultipleActiveResultSets=true;TrustServerCertificate=true"
      "ExternalServices:Customer:Uri": "http://customerservice.api:8080"
      # ASPNETCORE_Kestrel__Certificates__Default__Password: "mypass123"
      # ASPNETCORE_Kestrel__Certificates__Default__Path: "/https/aspnetapp.pfx"
    ports:
      - "28080:8080"
      - "28081:8081"
    volumes:
      - ${APPDATA}/Microsoft/UserSecrets:/home/app/.microsoft/usersecrets:ro
      - ${APPDATA}/ASP.NET/Https:/home/app/.aspnet/https:ro
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
      ASPNETCORE_ENVIRONMENT: "Development"
      ASPNETCORE_HTTP_PORTS: 8080
      ASPNETCORE_HTTPS_PORTS: 8081
      # ASPNETCORE_Kestrel__Certificates__Default__Password: "mypass123"
      # ASPNETCORE_Kestrel__Certificates__Default__Path: "/https/aspnetapp.pfx"
    ports:
      - "38080:8080"
      - "38081:8081"
    volumes:
      - ${APPDATA}/Microsoft/UserSecrets:/home/app/.microsoft/usersecrets:ro
      - ${APPDATA}/ASP.NET/Https:/home/app/.aspnet/https:ro
```



