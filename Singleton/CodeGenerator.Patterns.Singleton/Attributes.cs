using System;

namespace CodeGenerator.Patterns.Singleton;

[AttributeUsage(AttributeTargets.Class)]
public class SingletonAttribute : Attribute
{
    /// <summary>Strategy for singleton implementation</summary>
    public SingletonStrategy Strategy { get; set; } = SingletonStrategy.LockFree;
        
    /// <summary>Whether to use lazy initialization (ignored for Eager strategy)</summary>
    public bool LazyInitialization { get; set; } = true;
        
    /// <summary>Whether the singleton should be thread-safe</summary>
    public bool ThreadSafe { get; set; } = true;
        
    /// <summary>Whether to register the singleton in DI container</summary>
    public bool RegisterInDI { get; set; } = false;
        
    /// <summary>Whether to use a factory method instead of constructor</summary>
    public bool UseFactory { get; set; } = false;
        
    /// <summary>Name of the factory method (default: CreateInstance)</summary>
    public string FactoryMethodName { get; set; } = "CreateInstance";
}