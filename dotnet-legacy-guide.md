# How to include this Source Generator in a legacy .NET 4.+ project

**Source generators run at compile time inside Roslyn**. 
To use this generator in a legacy .NET Framework project you must make the project **SDK-style** (so MSBuild/Roslyn will load analyzers/generators) and then reference the generator assembly **as an analyzer** (via `ProjectReference` for local dev or `PackageReference` for NuGet consumers).
Below are the exact steps developers must follow.

---

## Prerequisites

* Visual Studio 2022 (v17+) **or** `dotnet` SDK 6+ installed on the build machine.
* The generator assembly targets **netstandard2.0** (typical for source generators).

---

## 1. Convert the legacy project to SDK-style

Replace the old `.csproj` top and build imports with the SDK format while keeping `TargetFramework` set to `net481` (or `net48`, etc).

**Old-style → Minimal SDK-style example**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net481</TargetFramework>
    <OutputType>Exe</OutputType>
    <RootNamespace>Demo.Builder.Dotnet4Plus</RootNamespace>
    <LangVersion>latest</LangVersion>

    <!-- keep your generator output settings if desired -->
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated\$(TargetFramework)</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

  <!-- Debug/Release property groups as needed -->
</Project>
```

> Keep any other project-specific properties you need (assembly name, debug settings, nullable, etc.). SDK-style projects will still target the .NET Framework runtime you need.

---

## 2. Add the generator to the project (two options)

### Option A: Local development (project reference)

Use this while developing the generator and the consumer in the same solution.

```xml
<ItemGroup>
  <ProjectReference Include="..\Path\To\YourGenerator\YourGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

`OutputItemType="Analyzer"` tells the compiler to load the generator at compile time; `ReferenceOutputAssembly="false"` prevents the analyzer DLL from being referenced by the runtime.

### Option B: NuGet package (production)

Package the generator so it places the generator DLL under `analyzers/dotnet/cs` in the NuGet package. Then consumers simply add:

```xml
<ItemGroup>
  <PackageReference Include="YourCompany.PipelineGenerator" Version="1.2.3" />
</ItemGroup>
```

The build system will load analyzers from the package and run the generator.

---

## 3. Make sure consumer code exposes necessary types/attributes

The generator expects consumers to have (or the generator can emit helpers):

* `PipelineGen.PipelineStepAttribute` : annotate step classes:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PipelineStepAttribute : Attribute
{
    public int Order { get; set; }
    public PipelineStepAttribute() { }
    public PipelineStepAttribute(int order) => Order = order;
}
```

* `IPipelineStep<TContext>` interface (if not using the generator-emitted helper):

```csharp
public interface IPipelineStep<TContext>
{
    Task InvokeAsync(TContext context, Func<TContext, Task> next);
}
```

Annotate step classes and implement that interface. The generator will discover classes with `[PipelineStep]` and produce the pipeline code.

---

## 4. Build & verify the generator ran

* Build with `dotnet build` or via Visual Studio 2022.
* Generated files usually appear in `obj\<Configuration>\<TargetFramework>\` or in the `CompilerGeneratedFilesOutputPath` you set (e.g. `Generated\net481\`).
* In Visual Studio, open the **Analyzer** node (or View → Other Windows → Analyzer) to confirm the generator/analyzer is loaded.

---

## 5. If you cannot convert to SDK-style

If you are *unable* to convert the project to SDK-style, source generators cannot be executed inside that project’s normal build. Two feasible workarounds:

1. **Separate generation project**

    * Keep the generator running in a SDK-style project that writes generated `.cs` files to a shared folder.
    * Add those generated `.cs` files to the legacy project (either as linked files or as a pre-build copy).

2. **Pre-generate during CI**

    * Run a separate build step on a machine with the SDK that produces generated sources, then check those into source control or copy them into the legacy project before building.

---

## Quick checklist for developers

1. Convert `.csproj` to SDK-style while keeping `TargetFramework` = `net48`/`net481`.
2. Add the generator:

    * Dev: `ProjectReference` with `OutputItemType="Analyzer"`.
    * Prod: `PackageReference` to the analyzer NuGet package.
3. Ensure the attribute/interface used by the generator is available (or rely on generator helpers).
4. Build with VS2022 or `dotnet` SDK 6+ and verify `.g.cs` files appear under `obj` or your `CompilerGeneratedFilesOutputPath`.
5. If you cannot convert, generate code separately and include it in the legacy project.

---
