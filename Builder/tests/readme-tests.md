## 1. **BuilderTests.cs** - Complete Unit Test Suite
- Tests all four entities (User, ProjectConfiguration, ApiClientConfig, DatabaseConfig)
- Covers required field validation, custom validation methods, and error scenarios
- Tests collection operations (add, clear, count, range operations)
- Validates fluent interface and method chaining
- Tests `ToBuilder()` extension methods and `From()` static methods
- Includes theory-based tests for edge cases (age validation)
- Compares builder output vs direct instantiation

## 2. **BuilderDemo.cs** - Interactive Console Demo
- Real-world usage examples for each entity
- Demonstrates error handling with try-catch blocks
- Shows the benefits of the builder pattern (validation, immutability, fluency)
- Performance comparison between builder and direct instantiation
- JSON serialization comparison

## 3. **PerformanceBenchmark.cs** - Performance Analysis
- Benchmarks 100,000 object creations for each entity type
- Measures creation time differences (ticks per object)
- Memory allocation analysis
- Warm-up phase to account for JIT compilation
- Performance ratio calculations

## Key Test Scenarios Covered:

✅ **Required Field Validation** - Tests missing required fields throw exceptions  
✅ **Custom Validation** - Tests age validation (0-150) and timeout validation  
✅ **Collection Operations** - Add, AddRange, Clear, Count properties  
✅ **Custom Setter Names** - `WithPoolSize()` instead of `WithMaxPoolSize()`  
✅ **Default Values** - Tests attribute-specified defaults work correctly  
✅ **Fluent Interface** - Method chaining returns correct builder instance  
✅ **Immutability** - Builder creates new instances, doesn't modify existing  
✅ **Error Messages** - Proper exception messages for validation failures  
✅ **Extension Methods** - `ToBuilder()` creates builder from existing instance