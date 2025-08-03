// See https://aka.ms/new-console-template for more information

using Demo.Singleton.ConsoleApp;

await SingletonUsageDemo.DemonstrateUsage();

// USAGE EXAMPLES
public static class SingletonUsageDemo
{
    public static async Task DemonstrateUsage()
    {
        Console.WriteLine("High-Performance Singleton Demo");
        Console.WriteLine("==================================");

        // 1. Basic singleton usage - lazy initialization
        Console.WriteLine("\n1. Basic Singleton:");
        var config1 = ConfigurationManager.Instance;
        var config2 = ConfigurationManager.Instance;
        Console.WriteLine($"Same instance: {ReferenceEquals(config1, config2)}");
        Console.WriteLine($"Environment: {config1.GetSetting("Environment")}");

        // 2. High-performance metrics collection
        Console.WriteLine("\n2. High-Performance Metrics:");
        var metrics = MetricsCollector.Instance;
        
        // Simulate concurrent metric updates
        var tasks = Enumerable.Range(0, 1000).Select(i => Task.Run(() =>
        {
            metrics.IncrementCounter("requests");
            metrics.SetGauge("active_users", Random.Shared.NextDouble() * 100);
        }));
        
        await Task.WhenAll(tasks);
        Console.WriteLine($"Total requests: {metrics.GetCounter("requests")}");

        // 3. Eager singleton - already initialized
        Console.WriteLine("\n3. Eager Singleton:");
        var logger = Logger.Instance; // Already initialized at startup
        logger.Log("Application started");

        // 4. Cache manager with expiry
        Console.WriteLine("\n4. Cache Manager:");
        var cache = CacheManager.Instance;
        cache.Set("user:123", new { Name = "John", Age = 30 }, TimeSpan.FromSeconds(10));
        var cachedUser = cache.Get<object>("user:123");
        Console.WriteLine($"Cached user: {cachedUser}");

        // 5. Generic singleton
        Console.WriteLine("\n5. Generic Repository:");
        var userRepo = Repository<User>.Instance;
        var productRepo = Repository<Product>.Instance;
        
        userRepo.Add(new User { Name = "Alice" });
        productRepo.Add(new Product { Name = "Widget" });
        
        Console.WriteLine($"User repo items: {userRepo.GetAll().Count}");
        Console.WriteLine($"Product repo items: {productRepo.GetAll().Count}");
        Console.WriteLine($"Different instances: {!ReferenceEquals(userRepo, productRepo)}");

        // 6. Factory-based singleton
        Console.WriteLine("\n6. Factory-Based Singleton:");
        var dbPool = DbConnectionPool.Instance;
        var connection = dbPool.GetConnection();
        Console.WriteLine($"Got connection: {connection != null}");
        dbPool.ReturnConnection(connection);
    }
    
    private class User
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private class Product
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
    }
}