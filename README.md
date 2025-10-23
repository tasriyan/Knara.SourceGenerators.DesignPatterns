# Legacy Modernization Code Generators
[![Build and Test](https://github.com/tasriyan/Knara.SourceGenerators.DesignPatterns/actions/workflows/build.yml/badge.svg)](https://github.com/tasriyan/Knara.SourceGenerators.DesignPatterns/actions/workflows/build.yml)
[![Publish NuGet Packages](https://github.com/tasriyan/Knara.SourceGenerators.DesignPatterns/actions/workflows/publish.yml/badge.svg)](https://github.com/tasriyan/Knara.SourceGenerators.DesignPatterns/actions/workflows/publish.yml)
[![Framework](https://img.shields.io/badge/.NET-Standard%202.0%20%7C%20.NET%209.0-blue)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/language-C%23-239120.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)

A collection of C# source generators designed to introduce modern design patterns and practices to legacy .NET Framework applications. These generators automatically create boilerplate code for common patterns, enabling teams to adopt proven architectural practices without requiring deep expertise in their implementation.

## Project Intent

As a systems architect tasked with modernizing a large legacy application, I encountered a team with limited experience in modern design patterns and coding practices. Rather than spending months on training or risking hand-rolled implementations of complex patterns, I created these source generators to:

- **Eliminate boilerplate** for common design patterns
- **Enforce correct implementations** of thread-safe and validated code  
- **Bridge legacy constraints** with modern development practices
- **Enable gradual modernization** without breaking existing functionality
- **Provide teaching tools** through generated code examples and validation warnings

The goal was to make advanced patterns accessible to developers who hadn't yet mastered them, while maintaining code quality and reducing the risk of bugs in production systems.

## Why Source Generation?

I was inspired to pursue source generation after completing Mel Grubb's excellent course [From Zero to Hero: Source Generators in C#](https://courses.dometrain.com/courses/take/from-zero-to-hero-source-generators-in-csharp) on Dometrain. 

Source generation offered distinct advantages over traditional libraries for this modernization effort:

- **Compile-time safety** with no runtime reflection overhead
- **Transparent generated code** that teams can inspect and learn from
- **No external dependencies** to complicate legacy application deployments
- **Educational value** through diagnostic warnings and code examples
- **Performance benefits** over reflection-based alternatives

## Generators Overview

### [Mediator Pattern Generator](./Mediator/README.md)
**Purpose**: Dual-pattern mediator supporting both CQRS-style development and legacy method retrofitting

**Value**: Enables gradual adoption of mediator patterns without breaking existing service layer architecture. Provides compile-time performance advantages over MediatR through direct method dispatch.

**Best for**: Teams transitioning from direct service calls to centralized request handling, CQRS adoption, legacy code modernization.

### [Singleton Pattern Generator](./Singleton/README.md)  
**Purpose**: Thread-safe singleton implementations with multiple performance strategies

**Value**: Prevents concurrency bugs common in manual singleton implementations. Essential for .NET Framework applications without dependency injection containers.

**Best for**: Legacy applications needing shared state management, teams unfamiliar with thread-safety concerns, performance-critical singleton scenarios.

### [Builder Pattern Generator](./Builder/README.md)
**Purpose**: Fluent builder APIs for complex object construction with validation

**Value**: Provides modern object construction patterns for frameworks lacking `init` properties and nullable reference types. Enables immutable object creation with comprehensive validation.

**Best for**: Legacy .NET Framework applications, complex configuration objects, teams needing consistent object construction patterns.

### [Decorator Pattern Generator](./Decorator/README.md)
**Purpose**: Fluent decorator composition APIs

**Value**: Simplifies decorator pattern implementation and composition (though primarily a technical exercise rather than solving a critical business need).

**Best for**: Applications heavily using cross-cutting concerns, teams frequently implementing decorator patterns.

## Target Environment

These generators were specifically designed for:

- **.NET Framework 4.x** applications without modern language features
- **Legacy enterprise systems** requiring gradual modernization  
- **Teams with mixed skill levels** needing consistent pattern implementations
- **Risk-averse environments** where big-bang rewrites aren't feasible
- **Performance-sensitive applications** where reflection overhead matters

## Modernization Philosophy

Rather than requiring wholesale architectural changes, these generators enable **incremental modernization**:

1. **Start small** - Add generators to new features first
2. **Retrofit gradually** - Apply legacy-compatible patterns to existing code
3. **Learn through examples** - Generated code serves as teaching material
4. **Validate continuously** - Diagnostic warnings catch common mistakes
5. **Measure improvement** - Performance and maintainability gains become evident

## Performance Characteristics

All generators prioritize **compile-time work** over runtime overhead:

- **No reflection** - Direct method calls and static dispatch
- **Minimal allocations** - Efficient code generation patterns
- **Type safety** - Compile-time validation prevents runtime errors
- **Diagnostic feedback** - Early warning of potential issues

This approach is particularly valuable for legacy applications where performance regression is unacceptable.

## Getting Started

1. Choose the generator that addresses your most pressing modernization need
2. Follow the specific README for detailed setup and usage instructions
3. Instruction on how to integrate the generator into your legacy project can be found in this [guide](./dotnet-legacy-guide.md)

### License
MIT License. Copyright 2025 Tatyana Asriyan

---
