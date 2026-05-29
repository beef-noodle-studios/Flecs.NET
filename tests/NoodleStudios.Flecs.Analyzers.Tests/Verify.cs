using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace NoodleStudios.Flecs.Analyzers.Tests;

/// <summary>
///     Test harness for the aspect analyzer and its code-fix. Each test embeds the
///     library surface the analyzer recognizes as a separate source file, so the
///     tests stand alone and don't build against the runtime library.
/// </summary>
internal static class Verify
{
    public const string Stubs = """
        using System;

        namespace NoodleStudios.Flecs
        {
            public interface IAspect { }

            public readonly struct Entity { }

            public ref struct TableView { }

            [AttributeUsage(AttributeTargets.Field)]
            public sealed class OptionalAttribute : Attribute { }

            [AttributeUsage(AttributeTargets.Field)]
            public sealed class SelfAttribute : Attribute { }

            [AttributeUsage(AttributeTargets.Field)]
            public sealed class SingletonAttribute : Attribute { }

            [AttributeUsage(AttributeTargets.Field)]
            public sealed class UpAttribute : Attribute
            {
                public UpAttribute() { }
                public UpAttribute(Type relationship) { }
            }

            [AttributeUsage(AttributeTargets.Field)]
            public sealed class UpAncestorsFirstAttribute : Attribute
            {
                public UpAncestorsFirstAttribute() { }
                public UpAncestorsFirstAttribute(Type relationship) { }
            }

            [AttributeUsage(AttributeTargets.Field)]
            public sealed class UpDescendantsFirstAttribute : Attribute
            {
                public UpDescendantsFirstAttribute() { }
                public UpDescendantsFirstAttribute(Type relationship) { }
            }

            [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
            public sealed class WithAttribute : Attribute
            {
                public WithAttribute(Type component) { }
            }

            [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
            public abstract class ComponentTraitAttribute : Attribute { }

            public sealed class SparseAttribute : ComponentTraitAttribute { }

            public struct Position { public float X; }
            public struct Velocity { public float X; }
            public struct Health { public int Value; }
            public struct Mass { public float Value; }
            public struct Color { public int Rgba; }
            public struct ManagedComponent { public string Name; }
            public struct Tag { }
        }
        """;

    private static readonly ReferenceAssemblies Net = ReferenceAssemblies.Net.Net80;

    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor) => new(descriptor);

    /// <summary>
    ///     Run the analyzer over <paramref name="source"/> alongside the stubs.
    /// </summary>
    public static async Task Analyzer(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<AspectAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = Net,
        };
        test.TestState.Sources.Add(("Stubs.cs", Stubs));
        test.TestState.Sources.Add(("Test.cs", source));
        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync();
    }

    /// <summary>
    ///     Run the analyzer over a source that does not define <c>IAspect</c>, to
    ///     confirm the analyzer stays inert outside the library and its consumers.
    /// </summary>
    public static async Task InertAnalyzer(string source)
    {
        var test = new CSharpAnalyzerTest<AspectAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = Net,
            TestCode = source,
        };
        await test.RunAsync();
    }

    /// <summary>
    ///     Verify the NSFA001 code-fix turns <paramref name="source"/> into
    ///     <paramref name="fixedSource"/>.
    /// </summary>
    public static async Task CodeFix(string source, string fixedSource)
    {
        var test = new CSharpCodeFixTest<AspectAnalyzer, AddStructLayoutCodeFixProvider, DefaultVerifier>
        {
            ReferenceAssemblies = Net,
        };
        test.TestState.Sources.Add(("Stubs.cs", Stubs));
        test.TestState.Sources.Add(("Test.cs", source));
        test.FixedState.Sources.Add(("Stubs.cs", Stubs));
        test.FixedState.Sources.Add(("Test.cs", fixedSource));
        await test.RunAsync();
    }
}
