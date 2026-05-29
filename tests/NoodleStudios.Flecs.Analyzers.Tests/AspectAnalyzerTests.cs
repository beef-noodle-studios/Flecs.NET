using System.Threading.Tasks;

namespace NoodleStudios.Flecs.Analyzers.Tests;

public sealed class AspectAnalyzerTests
{
    [Test]
    public Task WellFormedAspect_ProducesNoDiagnostics() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [With(typeof(Velocity))]
        [StructLayout(LayoutKind.Sequential)]
        public ref struct Movement : IAspect
        {
            public Entity Entity;
            public TableView Table;
            public ref Position Pos;
            public ref readonly Velocity Vel;
            [Up] public ref Health Hp;
            [Singleton] public ref Mass M;
        }
        """);

    [Test]
    public Task SelfOnAccessor_ProducesNoDiagnostics() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct SelfAspect : IAspect
        {
            [Self] public ref Position Pos;
        }
        """);

    [Test]
    public Task SameComponentDifferentSourcing_IsNotDuplicate() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct Sourced : IAspect
        {
            public ref Position SelfPos;
            [Up] public ref Position UpPos;
        }
        """);

    [Test]
    public Task MissingStructLayout_ReportsNSFA001() => Verify.Analyzer("""
        using NoodleStudios.Flecs;

        public ref struct {|NSFA001:NoLayout|} : IAspect
        {
            public ref Position Pos;
        }
        """);

    [Test]
    public Task NonSequentialLayout_ReportsNSFA001() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Auto)]
        public ref struct {|NSFA001:AutoLayout|} : IAspect
        {
            public Entity Entity;
        }
        """);

    [Test]
    public Task ByValueComponentField_ReportsNSFA002() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct ByValue : IAspect
        {
            public int {|NSFA002:Bad|};
        }
        """);

    [Test]
    public Task RefToManagedComponent_ReportsNSFA002() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct RefManaged : IAspect
        {
            public ref ManagedComponent {|NSFA002:M|};
        }
        """);

    [Test]
    public Task RefToEntity_ReportsNSFA002() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct RefEntity : IAspect
        {
            public ref Entity {|NSFA002:E|};
        }
        """);

    [Test]
    public Task AccessorAttributeOnEntity_ReportsNSFA003() => Verify.Analyzer(
        // Importing System.Runtime.InteropServices here would make [Optional]
        // ambiguous with its BCL namesake, so qualify StructLayout instead.
        """
        using NoodleStudios.Flecs;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public ref struct OptOnEntity : IAspect
        {
            [{|#0:Optional|}] public Entity Ent;
        }
        """,
        Verify.Diagnostic(AspectAnalyzer.MisplacedAccessorAttribute)
            .WithLocation(0)
            .WithArguments("Ent", "Entity", "Optional"));

    [Test]
    public Task SourcingAttributeOnTableView_ReportsNSFA003() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct UpOnTable : IAspect
        {
            [{|NSFA003:Up|}] public TableView Table;
        }
        """);

    [Test]
    public Task DuplicateTerm_ReportsNSFA004() => Verify.Analyzer(
        """
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct Dupes : IAspect
        {
            public ref Velocity Dup1;
            public ref Velocity {|#0:Dup2|};
        }
        """,
        Verify.Diagnostic(AspectAnalyzer.DuplicateTerm)
            .WithLocation(0)
            .WithArguments("Dup2", "Dup1"));

    [Test]
    public Task SameComponentDifferentRefKind_IsNotDuplicate() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct ReadAndWrite : IAspect
        {
            public ref Velocity Write;
            public ref readonly Velocity Read;
        }
        """);

    [Test]
    public Task SameComponentDifferentOptionality_IsNotDuplicate() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct RequiredAndOptional : IAspect
        {
            public ref Velocity Required;
            [NoodleStudios.Flecs.Optional] public ref Velocity Maybe;
        }
        """);

    [Test]
    public Task DuplicateReadOnlyTerm_ReportsNSFA004() => Verify.Analyzer(
        """
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct DupesReadOnly : IAspect
        {
            public ref readonly Velocity Dup1;
            public ref readonly Velocity {|#0:Dup2|};
        }
        """,
        Verify.Diagnostic(AspectAnalyzer.DuplicateTerm)
            .WithLocation(0)
            .WithArguments("Dup2", "Dup1"));

    [Test]
    public Task SelfCombinedWithUp_ReportsNSFA005() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct SelfUp : IAspect
        {
            [Self][Up] public ref Position {|NSFA005:Pos|};
        }
        """);

    [Test]
    public Task TwoSourcingAttributes_ReportsNSFA005() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct UpSingleton : IAspect
        {
            [Up][Singleton] public ref Position {|NSFA005:Pos|};
        }
        """);

    [Test]
    public Task ClassImplementingIAspect_ReportsNSFA006() => Verify.Analyzer("""
        using NoodleStudios.Flecs;

        public class {|NSFA006:AspectClass|} : IAspect
        {
        }
        """);

    [Test]
    public Task OrdinaryStructImplementingIAspect_ReportsNSFA006() => Verify.Analyzer("""
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public struct {|NSFA006:PlainStruct|} : IAspect
        {
            public Entity Entity;
        }
        """);

    [Test]
    public Task TagAccessor_ReportsNSFA007() => Verify.Analyzer(
        """
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct TagRef : IAspect
        {
            public ref Tag {|#0:T|};
        }
        """,
        Verify.Diagnostic(AspectAnalyzer.TagAccessor)
            .WithLocation(0)
            .WithArguments("T", "Tag"));

    [Test]
    public Task ReadOnlyTagAccessor_ReportsNSFA007() => Verify.Analyzer(
        """
        using System.Runtime.InteropServices;
        using NoodleStudios.Flecs;

        [StructLayout(LayoutKind.Sequential)]
        public ref struct ReadOnlyTagRef : IAspect
        {
            public ref readonly Tag {|#0:T|};
        }
        """,
        Verify.Diagnostic(AspectAnalyzer.TagAccessor)
            .WithLocation(0)
            .WithArguments("T", "Tag"));

    [Test]
    public Task SparseTag_ReportsNSFA008() => Verify.Analyzer(
        """
        using NoodleStudios.Flecs;

        [Sparse]
        public struct {|#0:SparseMarker|} { }
        """,
        Verify.Diagnostic(AspectAnalyzer.SparseTag)
            .WithLocation(0)
            .WithArguments("SparseMarker"));

    [Test]
    public Task SparseComponentWithData_ProducesNoDiagnostics() => Verify.Analyzer("""
        using NoodleStudios.Flecs;

        [Sparse]
        public struct SparseData { public int Value; }
        """);

    [Test]
    public Task NoIAspectInCompilation_StaysInert() => Verify.InertAnalyzer("""
        public ref struct Loose
        {
            public int Value;
        }
        """);

    [Test]
    public Task MalformedAspect_TripsEveryFieldRule() => Verify.Analyzer("""
        using NoodleStudios.Flecs;

        public ref struct {|NSFA001:Messy|} : IAspect
        {
            public int {|NSFA002:Bad|};
            [{|NSFA003:Optional|}] public Entity Ent;
            [Self][Up] public ref Position {|NSFA005:Conflict|};
            public ref Velocity Dup1;
            public ref Velocity {|NSFA004:Dup2|};
        }
        """);
}
