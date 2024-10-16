# SimpleMicroservicesDemo
Simple Microservices Demo. A very simple system with a Customer and a Order service. When a new order is created, the OrderService ask the Customer fore the creditmax. A new order is only accepted is it is below credit max.



## Agenda

1. Create OrderService and CustomerServices using a Onion template (KbrOnionTemplate-Core-8.bat)
2. Implement CustomerService using a MSSSQL database with seeded data
3. Implement OrderService using a MSSSQL database. And using a domain service to check creditmax. The domain service uses a CustomerProxy (HttpClient) to talk to the CustomerService.
4. Run in docker



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
