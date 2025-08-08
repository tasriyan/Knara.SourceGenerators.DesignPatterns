using System;

namespace Demo.Decorator.DotNet4
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Builder Pattern Generator Demo For .NET 4.8.1 ===\n");
            
            // Example 1: User Service with a bunch of decorators and fluid API usage
            UserServiceDemo.FluidApiUsageDemo().GetAwaiter().GetResult();

            // Example 2: Email Service with one base class and fluid API usage
            EmailServiceDemo.FluidApiUsageDemo().GetAwaiter().GetResult();
        }
    }
} 