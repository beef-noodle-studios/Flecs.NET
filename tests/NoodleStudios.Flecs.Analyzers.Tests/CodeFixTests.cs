using System.Threading.Tasks;

namespace NoodleStudios.Flecs.Analyzers.Tests;

public sealed class CodeFixTests
{
    [Test]
    public Task AddsSequentialLayoutAndImport_WhenMissing() => Verify.CodeFix(
        """
        using NoodleStudios.Flecs;

        public ref struct {|NSFA001:Fixme|} : IAspect
        {
            public ref Position Pos;
        }
        """,
        """
        using NoodleStudios.Flecs;
        using System.Runtime.InteropServices;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct Fixme : IAspect
        {
            public ref Position Pos;
        }
        """);

    [Test]
    public Task RewritesNonSequentialLayout_PreservingImport() => Verify.CodeFix(
        """
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Auto)]
        public ref struct {|NSFA001:Fixme|} : IAspect
        {
            public Entity Entity;
        }
        """,
        """
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct Fixme : IAspect
        {
            public Entity Entity;
        }
        """);
}
