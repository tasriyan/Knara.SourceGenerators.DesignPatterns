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
        Console.WriteLine("\n[1]. Lazy Singleton:");
        ConfigurationManager.LogMessage("Lazy singleton (ConfigurationManager) is not initialized yet.");
        var config1 = ConfigurationManager.Instance;
        var config2 = ConfigurationManager.Instance;
        Console.WriteLine($"Same instance: {ReferenceEquals(config1, config2)}");
        Console.WriteLine($"Environment: {config1.GetSetting("Environment")}");
        
        // 2. Eager singleton - already initialized
        Console.WriteLine("\n[2]. Eager Singleton:");
        Logger.LogMessage("Eager singleton (Logger) is already initialized at startup.");
        var logger = Logger.Instance; // Already initialized at startup
        logger.Log("Application started");

        // 3. Lock-free singleton for high performance
        Console.WriteLine("\n[3]. Lock-Free Singleton:");
        var metrics = MetricsCollector.Instance;
        
        // Simulate concurrent metric updates
        var tasks = Enumerable.Range(0, 1000).Select(i => Task.Run(() =>
        {
            metrics.IncrementCounter("requests");
            metrics.SetGauge("active_users", Random.Shared.NextDouble() * 100);
        }));
        
        await Task.WhenAll(tasks);
        Console.WriteLine($"Total requests: {metrics.GetCounter("requests")}");
        
        // 4. Generic singleton
        Console.WriteLine("\n[4]. Generic Repository:");
        var userRepo = Repository<UserEntity>.Instance;
        var productRepo = Repository<OrderEntity>.Instance;
        
        userRepo.Add(new UserEntity { Name = "Alice" });
        productRepo.Add(new OrderEntity { OrderDate = DateTime.Now });
        
        Console.WriteLine($"User repo items: {userRepo.GetAll().Count}");
        Console.WriteLine($"Product repo items: {productRepo.GetAll().Count}");
        Console.WriteLine($"Different instances: {!ReferenceEquals(userRepo, productRepo)}");
        
        // 5. Double-checking with lock singleton
        Console.WriteLine("\n[5]. Double-Checking with Lock Singleton:");
        var dbPool1 = DbConnectionPool.Instance;
        var dbPool2 = DbConnectionPool.Instance;
        Console.WriteLine($"Same instance: {ReferenceEquals(dbPool1, dbPool2)}");
        var connection = dbPool1.GetConnection();
        Console.WriteLine($"Got connection: {connection != null}");
        dbPool1.ReturnConnection(connection);
    }
}