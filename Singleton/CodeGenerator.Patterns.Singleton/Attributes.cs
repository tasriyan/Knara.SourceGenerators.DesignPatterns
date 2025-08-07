using System;

namespace CodeGenerator.Patterns.Singleton;

[AttributeUsage(AttributeTargets.Class)]
public class SingletonAttribute : Attribute
{
    /// <summary>Strategy for singleton implementation</summary>
    public SingletonStrategy Strategy { get; set; } = SingletonStrategy.LockFree;
}