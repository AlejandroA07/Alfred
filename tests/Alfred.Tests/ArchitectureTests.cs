using System.Reflection;

namespace Alfred.Tests;

/// <summary>
/// Enforces the modular-monolith boundary: modules may depend on the SharedKernel,
/// never on each other. Cross-module needs go through domain events or public
/// interfaces registered in the API host.
/// </summary>
public class ArchitectureTests
{
    private static readonly string[] ModuleAssemblies =
    [
        "Alfred.Modules.Identity",
        "Alfred.Modules.Households",
        "Alfred.Modules.Finance",
        "Alfred.Modules.Reminders",
        "Alfred.Modules.Purchases",
        "Alfred.Modules.Calendar",
        "Alfred.Modules.Assistant",
        "Alfred.Modules.Notifications",
    ];

    public static TheoryData<string> ModuleNames() => [.. ModuleAssemblies];

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_does_not_reference_other_modules(string moduleName)
    {
        var assembly = Assembly.Load(moduleName);

        var forbidden = assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(n => n is not null && n.StartsWith("Alfred.Modules.", StringComparison.Ordinal))
            .ToList();

        Assert.True(forbidden.Count == 0,
            $"{moduleName} references other modules: {string.Join(", ", forbidden)}. " +
            "Modules must only depend on Alfred.SharedKernel.");
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_exists_in_solution(string moduleName)
    {
        Assert.NotNull(Assembly.Load(moduleName));
    }
}
