using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace KonXProWebApp.Functions.Tests.Conventions;

/// <summary>
/// Convention guard: every test class in this assembly must declare exactly one
/// Category trait, and anything that drives Playwright must be categorized E2E and
/// declare the environment it needs. CI selects tests by these traits, so an
/// unlabelled class would silently stop running -- this fails the build instead.
///
/// Deliberately duplicated into each test project rather than shared from a common
/// folder: the root KonXProWebApp.csproj sits at the repository root and only
/// excludes known sibling folders via explicit &lt;Compile Remove&gt; items, so a new
/// top-level shared folder would be compiled into the web app itself (see CLAUDE.md,
/// "Solution layout"). Reflection is assembly-local, so each copy guards its own
/// project and runs wherever that project's tests run, including locally.
/// </summary>
[Trait("Category", "Unit")]
public class TestCategoryConventionTests
{
    private static readonly HashSet<string> AllowedCategories =
        new(StringComparer.Ordinal) { "Unit", "Component", "Integration", "E2E" };

    private static readonly HashSet<string> AllowedE2EScopes =
        new(StringComparer.Ordinal) { "Anonymous", "SeededUser", "ExternalCredentials", "Manual" };

    private static bool IsFactOrTheoryAttribute(Type attributeType)
    {
        for (var cur = attributeType; cur != null; cur = cur.BaseType)
        {
            if (cur.Name == "FactAttribute" || cur.Name == "TheoryAttribute")
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTestMethod(MethodInfo method) =>
        method.GetCustomAttributesData().Any(a => IsFactOrTheoryAttribute(a.AttributeType));

    private static IReadOnlyList<Type> TestClasses()
    {
        var assembly = typeof(TestCategoryConventionTests).Assembly;

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).Select(t => t!).ToArray();
        }

        const BindingFlags MethodFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        return types
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetMethods(MethodFlags).Any(IsTestMethod))
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<KeyValuePair<string, string>> TraitsOf(Type type) =>
        type.GetCustomAttributesData()
            .Where(a => a.AttributeType.Name == "TraitAttribute" && a.ConstructorArguments.Count == 2)
            .Select(a => new KeyValuePair<string, string>(
                a.ConstructorArguments[0].Value as string ?? string.Empty,
                a.ConstructorArguments[1].Value as string ?? string.Empty))
            .ToList();

    private static bool IsPlaywrightType(Type? type)
    {
        if (type == null)
        {
            return false;
        }

        if (type.Namespace != null && type.Namespace.StartsWith("Microsoft.Playwright", StringComparison.Ordinal))
        {
            return true;
        }

        return type.IsGenericType && type.GetGenericArguments().Any(IsPlaywrightType);
    }

    private static bool UsesPlaywright(Type type)
    {
        for (var cur = type.BaseType; cur != null; cur = cur.BaseType)
        {
            if (IsPlaywrightType(cur))
            {
                return true;
            }
        }

        const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.DeclaredOnly;

        if (type.GetFields(MemberFlags).Any(f => IsPlaywrightType(f.FieldType)))
        {
            return true;
        }

        if (type.GetProperties(MemberFlags).Any(p => IsPlaywrightType(p.PropertyType)))
        {
            return true;
        }

        if (type.GetConstructors(MemberFlags).SelectMany(c => c.GetParameters())
                .Any(p => IsPlaywrightType(p.ParameterType)))
        {
            return true;
        }

        return type.GetMethods(MemberFlags)
            .Any(m => IsPlaywrightType(m.ReturnType) || m.GetParameters().Any(p => IsPlaywrightType(p.ParameterType)));
    }

    // Guards the guard: if reflection ever stops finding test classes, every other
    // check below would pass vacuously. This makes that condition fail loudly.
    [Fact]
    public void AssemblyExposesTestClassesToInspect()
    {
        Assert.True(
            TestClasses().Count > 0,
            $"Convention guard found no test classes in {typeof(TestCategoryConventionTests).Assembly.GetName().Name}. " +
            "The guard cannot pass vacuously -- fix test discovery before relying on it.");
    }

    [Fact]
    public void EveryTestClassDeclaresExactlyOneKnownCategory()
    {
        var offenders = new List<string>();

        foreach (var type in TestClasses())
        {
            var categories = TraitsOf(type)
                .Where(t => string.Equals(t.Key, "Category", StringComparison.Ordinal))
                .Select(t => t.Value)
                .ToList();

            if (categories.Count == 0)
            {
                offenders.Add($"{type.FullName} -- no [Trait(\"Category\", ...)]");
            }
            else if (categories.Count > 1)
            {
                offenders.Add($"{type.FullName} -- {categories.Count} Category traits ({string.Join(", ", categories)}); expected exactly one");
            }
            else if (!AllowedCategories.Contains(categories[0]))
            {
                offenders.Add($"{type.FullName} -- unknown Category \"{categories[0]}\"");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Test classes must declare exactly one known Category trait " +
            $"({string.Join(", ", AllowedCategories.OrderBy(c => c, StringComparer.Ordinal))}). Offenders:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void PlaywrightClassesAreCategorizedE2EWithAScope()
    {
        var offenders = new List<string>();

        foreach (var type in TestClasses().Where(UsesPlaywright))
        {
            var traits = TraitsOf(type);

            var categories = traits
                .Where(t => string.Equals(t.Key, "Category", StringComparison.Ordinal))
                .Select(t => t.Value)
                .ToList();

            if (categories.Count != 1 || !string.Equals(categories[0], "E2E", StringComparison.Ordinal))
            {
                offenders.Add($"{type.FullName} -- drives Playwright but Category is [{string.Join(", ", categories)}]; expected exactly \"E2E\"");
            }

            var scopes = traits
                .Where(t => string.Equals(t.Key, "E2E", StringComparison.Ordinal))
                .Select(t => t.Value)
                .ToList();

            if (scopes.Count != 1)
            {
                offenders.Add($"{type.FullName} -- drives Playwright but declares {scopes.Count} [Trait(\"E2E\", ...)] scopes; expected exactly one");
            }
            else if (!AllowedE2EScopes.Contains(scopes[0]))
            {
                offenders.Add($"{type.FullName} -- unknown E2E scope \"{scopes[0]}\"");
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Playwright test classes must be Category=E2E and declare one E2E scope " +
            $"({string.Join(", ", AllowedE2EScopes.OrderBy(s => s, StringComparer.Ordinal))}). Offenders:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }
}
