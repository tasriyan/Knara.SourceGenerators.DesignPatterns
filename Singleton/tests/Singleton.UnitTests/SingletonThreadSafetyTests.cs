using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Shouldly;
using Singleton.UnitTests.UseCases;

namespace Singleton.UnitTests;

public class SingletonThreadSafetyTests
{
    private const int ThreadCount = 50;
    private const int IterationsPerThread = 100;

    [Fact]
    public async Task ConfigurationManager_LockFree_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new ConcurrentBag<ConfigurationManager>();
        var barrier = new Barrier(ThreadCount);
        var tasks = new Task[ThreadCount];

        // Act - Multiple threads accessing singleton concurrently
        for (int i = 0; i < ThreadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    var instance = ConfigurationManager.Instance;
                    instances.Add(instance);

                    // Perform operations to stress the singleton
                    instance.SetSetting($"TestKey_{Thread.CurrentThread.ManagedThreadId}_{j}", $"Value_{j}");
                    _ = instance.GetSetting("Environment");
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        instances.Count.ShouldBe(ThreadCount * IterationsPerThread);
        var uniqueInstances = instances.Distinct().ToList();
        uniqueInstances.ShouldHaveSingleItem("All threads should get the same ConfigurationManager instance");

        // Verify singleton functionality works
        var singleton = ConfigurationManager.Instance;
        singleton.GetSetting("Environment").ShouldBe("Production");
    }

    [Fact]
    public async Task Logger_EagerSingleton_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new ConcurrentBag<Logger>();
        var barrier = new Barrier(ThreadCount);
        var tasks = new Task[ThreadCount];

        // Act - Concurrent access to eager singleton
        for (int i = 0; i < ThreadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    var instance = Logger.Instance;
                    instances.Add(instance);

                    // Perform logging operations
                    instance.Log($"Test message from thread {Thread.CurrentThread.ManagedThreadId}, iteration {j}");
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        instances.Count.ShouldBe(ThreadCount * IterationsPerThread);
        var uniqueInstances = instances.Distinct().ToList();
        uniqueInstances.ShouldHaveSingleItem("All threads should get the same Logger instance");
    }

    [Fact]
    public async Task CacheManager_LockFree_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new ConcurrentBag<CacheManager>();
        var barrier = new Barrier(ThreadCount);
        var tasks = new Task[ThreadCount];

        // Act - Concurrent cache operations
        for (int i = 0; i < ThreadCount; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    var instance = CacheManager.Instance;
                    instances.Add(instance);

                    // Perform cache operations
                    var key = $"key_{threadIndex}_{j}";
                    var value = $"value_{threadIndex}_{j}";

                    instance.Set(key, value, TimeSpan.FromMinutes(5));
                    _ = instance.Get<string>(key);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        instances.Count.ShouldBe(ThreadCount * IterationsPerThread);
        var uniqueInstances = instances.Distinct().ToList();
        uniqueInstances.ShouldHaveSingleItem("All threads should get the same CacheManager instance");

        // Verify cache functionality
        var cache = CacheManager.Instance;
        cache.Set("test", "value", TimeSpan.FromMinutes(1));
        cache.Get<string>("test").ShouldBe("value");
    }

    [Fact]
    public async Task DbConnectionPool_FactoryBased_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new ConcurrentBag<DbConnectionPool>();
        var barrier = new Barrier(ThreadCount);
        var tasks = new Task[ThreadCount];

        // Act - Concurrent connection pool access
        for (int i = 0; i < ThreadCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    var instance = DbConnectionPool.Instance;
                    instances.Add(instance);

                    // Perform connection pool operations
                    var connection = instance.GetConnection();
                    instance.ReturnConnection(connection);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        instances.Count.ShouldBe(ThreadCount * IterationsPerThread);
        var uniqueInstances = instances.Distinct().ToList();
        uniqueInstances.ShouldHaveSingleItem("All threads should get the same DbConnectionPool instance");
    }

    [Fact]
    public async Task MetricsCollector_LockFree_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new ConcurrentBag<MetricsCollector>();
        var barrier = new Barrier(ThreadCount);
        var tasks = new Task[ThreadCount];

        // Act - Concurrent metrics operations
        for (int i = 0; i < ThreadCount; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    var instance = MetricsCollector.Instance;
                    instances.Add(instance);

                    // Perform metrics operations
                    instance.IncrementCounter($"counter_{threadIndex}");
                    instance.SetGauge($"gauge_{threadIndex}", threadIndex * j);
                    _ = instance.GetCounter($"counter_{threadIndex}");
                    _ = instance.GetGauge($"gauge_{threadIndex}");
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        instances.Count.ShouldBe(ThreadCount * IterationsPerThread);
        var uniqueInstances = instances.Distinct().ToList();
        uniqueInstances.ShouldHaveSingleItem("All threads should get the same MetricsCollector instance");

        // Verify metrics functionality
        var metrics = MetricsCollector.Instance;
        metrics.IncrementCounter("test");
        metrics.GetCounter("test").ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task UserRepository_DoubleCheckedLocking_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new ConcurrentBag<Repository<UserEntity>>();
        var barrier = new Barrier(ThreadCount);
        var tasks = new Task[ThreadCount];

        // Act - Concurrent user repository operations
        for (int i = 0; i < ThreadCount; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    var instance = Repository<UserEntity>.Instance;
                    instances.Add(instance);

                    // Perform repository operations with UserEntity
                    var user = new UserEntity 
                    { 
                        Id = threadIndex * 1000 + j, 
                        Name = $"User_{threadIndex}_{j}" 
                    };

                    instance.Add(user);
                    _ = instance.GetAll();
                    _ = instance.FindById(u => u.Id == user.Id);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        instances.Count.ShouldBe(ThreadCount * IterationsPerThread);
        var uniqueInstances = instances.Distinct().ToList();
        uniqueInstances.ShouldHaveSingleItem("All threads should get the same Repository<UserEntity> instance");

        // Verify repository functionality
        var userRepo = Repository<UserEntity>.Instance;
        userRepo.GetAll().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task CustomerRepository_DoubleCheckedLocking_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new ConcurrentBag<Repository<CustomerEntity>>();
        var barrier = new Barrier(ThreadCount);
        var tasks = new Task[ThreadCount];

        // Act - Concurrent customer repository operations
        for (int i = 0; i < ThreadCount; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    var instance = Repository<CustomerEntity>.Instance;
                    instances.Add(instance);

                    // Perform repository operations with CustomerEntity
                    var customer = new CustomerEntity 
                    { 
                        Id = threadIndex * 1000 + j,
                        Addresses = new List<AddressEntity>
                        {
                            new AddressEntity 
                            { 
                                Id = threadIndex * 1000 + j + 1, 
                                Street = $"{threadIndex} Main St", 
                                City = "TestCity",
                                State = "TS",
                                ZipCode = $"{j:D5}"
                            }
                        }
                    };

                    instance.Add(customer);
                    _ = instance.GetAll();
                    _ = instance.FindById(c => c.Id == customer.Id);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        instances.Count.ShouldBe(ThreadCount * IterationsPerThread);
        var uniqueInstances = instances.Distinct().ToList();
        uniqueInstances.ShouldHaveSingleItem("All threads should get the same Repository<CustomerEntity> instance");

        // Verify repository functionality
        var customerRepo = Repository<CustomerEntity>.Instance;
        customerRepo.GetAll().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task AddressRepository_DoubleCheckedLocking_ShouldBeThreadSafe()
    {
        // Arrange
        var instances = new ConcurrentBag<Repository<AddressEntity>>();
        var barrier = new Barrier(ThreadCount);
        var tasks = new Task[ThreadCount];

        // Act - Concurrent address repository operations
        for (int i = 0; i < ThreadCount; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    var instance = Repository<AddressEntity>.Instance;
                    instances.Add(instance);

                    // Perform repository operations with AddressEntity
                    var address = new AddressEntity 
                    { 
                        Id = threadIndex * 1000 + j,
                        Street = $"{threadIndex * j} Test Street",
                        City = $"City_{threadIndex}",
                        State = $"ST{threadIndex % 50:D2}",
                        ZipCode = $"{j:D5}"
                    };

                    instance.Add(address);
                    _ = instance.GetAll();
                    _ = instance.FindById(a => a.Id == address.Id);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        instances.Count.ShouldBe(ThreadCount * IterationsPerThread);
        var uniqueInstances = instances.Distinct().ToList();
        uniqueInstances.ShouldHaveSingleItem("All threads should get the same Repository<AddressEntity> instance");

        // Verify repository functionality
        var addressRepo = Repository<AddressEntity>.Instance;
        addressRepo.GetAll().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GenericRepositories_ShouldMaintainSeparateInstancesPerEntityType()
    {
        // Arrange
        var userInstances = new ConcurrentBag<Repository<UserEntity>>();
        var customerInstances = new ConcurrentBag<Repository<CustomerEntity>>();
        var addressInstances = new ConcurrentBag<Repository<AddressEntity>>();
        var barrier = new Barrier(ThreadCount * 3);
        var tasks = new Task[ThreadCount * 3];

        // Act - Test UserEntity repository
        for (int i = 0; i < ThreadCount; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    var instance = Repository<UserEntity>.Instance;
                    userInstances.Add(instance);

                    instance.Add(new UserEntity { Id = threadIndex * 1000 + j, Name = $"User_{threadIndex}_{j}" });
                }
            });
        }

        // Act - Test CustomerEntity repository
        for (int i = ThreadCount; i < ThreadCount * 2; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    var instance = Repository<CustomerEntity>.Instance;
                    customerInstances.Add(instance);

                    instance.Add(new CustomerEntity { Id = threadIndex * 1000 + j, Addresses = new List<AddressEntity>() });
                }
            });
        }

        // Act - Test AddressEntity repository
        for (int i = ThreadCount * 2; i < ThreadCount * 3; i++)
        {
            int threadIndex = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    var instance = Repository<AddressEntity>.Instance;
                    addressInstances.Add(instance);

                    instance.Add(new AddressEntity { Id = threadIndex * 1000 + j, Street = $"Street_{threadIndex}_{j}" });
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert
        var uniqueUserRepos = userInstances.Distinct().ToList();
        var uniqueCustomerRepos = customerInstances.Distinct().ToList();
        var uniqueAddressRepos = addressInstances.Distinct().ToList();

        uniqueUserRepos.ShouldHaveSingleItem("All threads should get the same Repository<UserEntity> instance");
        uniqueCustomerRepos.ShouldHaveSingleItem("All threads should get the same Repository<CustomerEntity> instance");
        uniqueAddressRepos.ShouldHaveSingleItem("All threads should get the same Repository<AddressEntity> instance");

        // Verify they are different instances for different entity types
        object.ReferenceEquals(uniqueUserRepos[0], uniqueCustomerRepos[0])
            .ShouldBeFalse("Repository<UserEntity> and Repository<CustomerEntity> should be different instances");
        object.ReferenceEquals(uniqueUserRepos[0], uniqueAddressRepos[0])
            .ShouldBeFalse("Repository<UserEntity> and Repository<AddressEntity> should be different instances");
        object.ReferenceEquals(uniqueCustomerRepos[0], uniqueAddressRepos[0])
            .ShouldBeFalse("Repository<CustomerEntity> and Repository<AddressEntity> should be different instances");

        // Verify repository functionality with different entity types
        var userRepo = Repository<UserEntity>.Instance;
        var customerRepo = Repository<CustomerEntity>.Instance;
        var addressRepo = Repository<AddressEntity>.Instance;

        userRepo.GetAll().ShouldNotBeEmpty();
        customerRepo.GetAll().ShouldNotBeEmpty();
        addressRepo.GetAll().ShouldNotBeEmpty();

        userRepo.GetAll().ShouldAllBe(u => u is UserEntity);
        customerRepo.GetAll().ShouldAllBe(c => c is CustomerEntity);
        addressRepo.GetAll().ShouldAllBe(a => a is AddressEntity);
    }

    [Fact]
    public async Task AllSingletons_ShouldHandleConcurrentInitialization()
    {
        // This test verifies that all singletons can be initialized concurrently without issues
        var barrier = new Barrier(8); // One for each singleton type
        var tasks = new Task[8];

        tasks[0] = Task.Run(() =>
        {
            barrier.SignalAndWait();
            var instance = ConfigurationManager.Instance;
            instance.ShouldNotBeNull();
        });

        tasks[1] = Task.Run(() =>
        {
            barrier.SignalAndWait();
            var instance = Logger.Instance;
            instance.ShouldNotBeNull();
        });

        tasks[2] = Task.Run(() =>
        {
            barrier.SignalAndWait();
            var instance = CacheManager.Instance;
            instance.ShouldNotBeNull();
        });

        tasks[3] = Task.Run(() =>
        {
            barrier.SignalAndWait();
            var instance = DbConnectionPool.Instance;
            instance.ShouldNotBeNull();
        });

        tasks[4] = Task.Run(() =>
        {
            barrier.SignalAndWait();
            var instance = MetricsCollector.Instance;
            instance.ShouldNotBeNull();
        });

        tasks[5] = Task.Run(() =>
        {
            barrier.SignalAndWait();
            var instance = Repository<UserEntity>.Instance;
            instance.ShouldNotBeNull();
        });

        tasks[6] = Task.Run(() =>
        {
            barrier.SignalAndWait();
            var instance = Repository<CustomerEntity>.Instance;
            instance.ShouldNotBeNull();
        });

        tasks[7] = Task.Run(() =>
        {
            barrier.SignalAndWait();
            var instance = Repository<AddressEntity>.Instance;
            instance.ShouldNotBeNull();
        });

        await Task.WhenAll(tasks);

        // All singletons should be accessible after concurrent initialization
        ConfigurationManager.Instance.ShouldNotBeNull();
        Logger.Instance.ShouldNotBeNull();
        CacheManager.Instance.ShouldNotBeNull();
        DbConnectionPool.Instance.ShouldNotBeNull();
        MetricsCollector.Instance.ShouldNotBeNull();
        Repository<UserEntity>.Instance.ShouldNotBeNull();
        Repository<CustomerEntity>.Instance.ShouldNotBeNull();
        Repository<AddressEntity>.Instance.ShouldNotBeNull();
    }

    [Fact]
    public async Task Singletons_ShouldMaintainStateConsistency()
    {
        // Test that singleton state remains consistent across threads
        var barrier = new Barrier(ThreadCount);
        var tasks = new Task[ThreadCount];
        var expectedCounterValue = ThreadCount * IterationsPerThread;

        for (int i = 0; i < ThreadCount; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                barrier.SignalAndWait();

                for (int j = 0; j < IterationsPerThread; j++)
                {
                    // Test metrics consistency
                    MetricsCollector.Instance.IncrementCounter("global_counter");

                    // Test cache consistency
                    CacheManager.Instance.Set($"thread_{threadId}_item_{j}", j, TimeSpan.FromMinutes(1));

                    // Test configuration consistency
                    ConfigurationManager.Instance.SetSetting($"thread_{threadId}_setting", $"value_{j}");

                    // Test repository consistency
                    Repository<UserEntity>.Instance.Add(new UserEntity { Id = threadId * 1000 + j, Name = $"User_{threadId}_{j}" });
                }
            });
        }

        await Task.WhenAll(tasks);

        // Verify state consistency
        var metrics = MetricsCollector.Instance;
        metrics.GetCounter("global_counter").ShouldBe(expectedCounterValue);

        var config = ConfigurationManager.Instance;
        config.GetSetting("Environment").ShouldBe("Production"); // Original setting should remain

        var userRepo = Repository<UserEntity>.Instance;
        userRepo.GetAll().Count.ShouldBe(expectedCounterValue);
    }

    [Fact]
    public void SingletonStrategies_ShouldHaveExpectedPerformanceCharacteristics()
    {
        var iterations = 100000;

        // Warm up all singletons
        _ = ConfigurationManager.Instance; // LockFree
        _ = Logger.Instance; // Eager
        _ = CacheManager.Instance; // LockFree
        _ = DbConnectionPool.Instance; // LockFree with Factory
        _ = MetricsCollector.Instance; // LockFree
        _ = Repository<UserEntity>.Instance; // DoubleCheckedLocking
        _ = Repository<CustomerEntity>.Instance; // DoubleCheckedLocking
        _ = Repository<AddressEntity>.Instance; // DoubleCheckedLocking

        // Measure access times
        var configTime = MeasureAccessTime(() => ConfigurationManager.Instance, iterations);
        var loggerTime = MeasureAccessTime(() => Logger.Instance, iterations);
        var cacheTime = MeasureAccessTime(() => CacheManager.Instance, iterations);
        var poolTime = MeasureAccessTime(() => DbConnectionPool.Instance, iterations);
        var metricsTime = MeasureAccessTime(() => MetricsCollector.Instance, iterations);
        var userRepoTime = MeasureAccessTime(() => Repository<UserEntity>.Instance, iterations);
        var customerRepoTime = MeasureAccessTime(() => Repository<CustomerEntity>.Instance, iterations);
        var addressRepoTime = MeasureAccessTime(() => Repository<AddressEntity>.Instance, iterations);

        // Eager singleton (Logger) should be fastest for repeated access
        loggerTime.ShouldBeLessThanOrEqualTo(configTime);
        loggerTime.ShouldBeLessThanOrEqualTo(cacheTime);
        loggerTime.ShouldBeLessThanOrEqualTo(poolTime);
        loggerTime.ShouldBeLessThanOrEqualTo(metricsTime);
        loggerTime.ShouldBeLessThanOrEqualTo(userRepoTime);
        loggerTime.ShouldBeLessThanOrEqualTo(customerRepoTime);
        loggerTime.ShouldBeLessThanOrEqualTo(addressRepoTime);

        // All should complete within reasonable time
        configTime.TotalMilliseconds.ShouldBeLessThan(1000);
        loggerTime.TotalMilliseconds.ShouldBeLessThan(1000);
        cacheTime.TotalMilliseconds.ShouldBeLessThan(1000);
        poolTime.TotalMilliseconds.ShouldBeLessThan(1000);
        metricsTime.TotalMilliseconds.ShouldBeLessThan(1000);
        userRepoTime.TotalMilliseconds.ShouldBeLessThan(1000);
        customerRepoTime.TotalMilliseconds.ShouldBeLessThan(1000);
        addressRepoTime.TotalMilliseconds.ShouldBeLessThan(1000);
    }

    private static TimeSpan MeasureAccessTime<T>(Func<T> accessor, int iterations)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            _ = accessor();
        }

        sw.Stop();
        return sw.Elapsed;
    }
}