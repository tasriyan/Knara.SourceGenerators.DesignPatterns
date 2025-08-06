using System.Collections.Generic;

namespace CodeGenerator.Patterns.Singleton;

public enum SingletonStrategy
{
    Lazy = 0,
    Eager = 1,
    LockFree = 2,
    DoubleCheckedLocking = 3
}

public class SingletonClassInfoResult
{
	public SingletonClassInfo? SingletonClassInfo { get; }
	public List<DiagnosticInfo> Diagnostics { get; }

	public SingletonClassInfoResult(SingletonClassInfo? singletonClassInfo, List<DiagnosticInfo> diagnostics)
	{
		SingletonClassInfo = singletonClassInfo;
		Diagnostics = diagnostics ?? [];
	}
}

public class SingletonClassInfo
{
    public string ClassName { get; }
    public string Namespace { get; }
    public SingletonStrategy Strategy { get; }
    public bool LazyInitialization { get; }
    public bool ThreadSafe { get; }
    public bool RegisterInDI { get; }
    public bool UseFactory { get; }
    public string FactoryMethodName { get; }
    public bool HasInitializeMethod { get; }
    public bool HasFactoryMethod { get; }
    public bool IsGeneric { get; }
    public List<string> TypeParameters { get; }
    public List<string> TypeConstraints { get; }
    
    public SingletonClassInfo(string className, 
                                string ns,
                                SingletonStrategy strategy,
                                bool lazyInitialization,
                                bool threadSafe,
                                bool registerInDI,
                                bool useFactory,
                                string factoryMethodName,
                                bool hasInitializeMethod,
                                bool hasFactoryMethod,
                                bool isGeneric,
                                List<string> typeParameters,
                                List<string> typeConstraints)
    {
        ClassName = className;
        Namespace = ns;
        Strategy = strategy;
        LazyInitialization = lazyInitialization;
        ThreadSafe = threadSafe;
        RegisterInDI = registerInDI;
        UseFactory = useFactory;
        FactoryMethodName = factoryMethodName;
        HasInitializeMethod = hasInitializeMethod;
        HasFactoryMethod = hasFactoryMethod;
        IsGeneric = isGeneric;
        TypeParameters = typeParameters ?? [];
        TypeConstraints = typeConstraints ?? [];
    }
}