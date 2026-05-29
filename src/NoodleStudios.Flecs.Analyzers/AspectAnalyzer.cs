using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NoodleStudios.Flecs.Analyzers;

/// <summary>
///     Flags aspect types whose shape the runtime descriptor would reject, so the
///     mistake surfaces at compile time instead of on first query build. 
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AspectAnalyzer : DiagnosticAnalyzer
{
    private const string Category = "NoodleStudios.Flecs.Aspects";

    internal static readonly DiagnosticDescriptor MissingSequentialLayout = new(
        "NSFA001",
        "Aspect must have sequential layout",
        "Aspect '{0}' must be declared with [StructLayout(LayoutKind.Sequential)]",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An aspect's fields are bound by offset, so the type must opt out of the runtime's freedom to reorder fields by declaring sequential layout.");

    internal static readonly DiagnosticDescriptor InvalidField = new(
        "NSFA002",
        "Invalid aspect field",
        "Aspect field '{0}' is not a valid aspect slot. An aspect field must be a ref or ref readonly to an unmanaged component value type, a by-value Entity, or a by-value TableView.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every aspect field maps to a pointer-width slot bound per row. Bind components by ref, and bind Entity and TableView by value.");

    internal static readonly DiagnosticDescriptor MisplacedAccessorAttribute = new(
        "NSFA003",
        "Accessor-only attribute on a non-accessor field",
        "Aspect field '{0}' is a {1} slot but carries the accessor-only attribute [{2}]. Move the attribute to a component accessor field.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Sourcing and optionality attributes refine a component accessor term. The Entity and TableView slots carry no term, so they cannot be refined.");

    internal static readonly DiagnosticDescriptor DuplicateTerm = new(
        "NSFA004",
        "Duplicate aspect term",
        "Aspect field '{0}' duplicates the term of field '{1}'. The two fields match the same component with the same sourcing.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Two accessor fields with the same component type and the same sourcing seed identical terms.");

    internal static readonly DiagnosticDescriptor ConflictingSourcing = new(
        "NSFA005",
        "Conflicting aspect sourcing attributes",
        "Aspect field '{0}' combines mutually exclusive sourcing attributes. Use at most one of [Up], [UpAncestorsFirst], [UpDescendantsFirst], [Singleton], and do not combine [Self] with any of them.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A field is sourced exactly one way. [Self] restricts a self-sourced term and is mutually exclusive with the explicit sourcing attributes.");

    internal static readonly DiagnosticDescriptor NotRefStruct = new(
        "NSFA006",
        "Aspect must be a ref struct",
        "Type '{0}' implements IAspect but is not a ref struct. Declare it as a ref struct.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An aspect holds ref fields into row storage that must not escape the iteration, so it must be a ref struct.");

    internal static readonly DiagnosticDescriptor TagAccessor = new(
        "NSFA007",
        "Tag cannot be an aspect accessor field",
        "Aspect field '{0}' is a ref to '{1}', which has no instance fields and registers as a zero-storage tag. A tag carries no data to bind.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A tag is matching-only. It has no readable data, so it cannot be bound as a component accessor. Match it with a [With] attribute on the aspect, or test it with Has, instead of declaring an accessor field.");

    internal static readonly DiagnosticDescriptor SparseTag = new(
        "NSFA008",
        "Tag cannot be marked [Sparse]",
        "Type '{0}' has no instance fields and registers as a zero-storage tag, so it cannot be marked [Sparse]. Sparse storage requires component data.",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Flecs can only place a component with data in a sparse set. A field-less tag has no storage, so marking it [Sparse] aborts at registration. Add a field to make it a real component, or remove [Sparse].");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            MissingSequentialLayout,
            InvalidField,
            MisplacedAccessorAttribute,
            DuplicateTerm,
            ConflictingSourcing,
            NotRefStruct,
            TagAccessor,
            SparseTag);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(static start =>
        {
            var known = KnownSymbols.TryCreate(start.Compilation);
            if (known is null)
            {
                // IAspect is absent, so this compilation does not reference the
                // library. Stay inert.
                return;
            }

            start.RegisterSymbolAction(known.AnalyzeNamedType, SymbolKind.NamedType);
        });
    }

    /// <summary>
    ///     The well-known symbols the analyzer matches against, resolved once per
    ///     compilation. 
    /// </summary>
    private sealed class KnownSymbols
    {
        private readonly INamedTypeSymbol _aspect;
        private readonly INamedTypeSymbol _entity;
        private readonly INamedTypeSymbol _tableView;
        private readonly INamedTypeSymbol _structLayout;
        private readonly INamedTypeSymbol _optional;
        private readonly INamedTypeSymbol _self;
        private readonly INamedTypeSymbol _up;
        private readonly INamedTypeSymbol _upAncestorsFirst;
        private readonly INamedTypeSymbol _upDescendantsFirst;
        private readonly INamedTypeSymbol _singleton;

        private readonly INamedTypeSymbol? _sparse;
        private readonly ImmutableArray<INamedTypeSymbol> _accessorOnlyAttributes;
        private readonly ImmutableArray<INamedTypeSymbol> _sourcingAttributes;

        private KnownSymbols(
            INamedTypeSymbol aspect,
            INamedTypeSymbol entity,
            INamedTypeSymbol tableView,
            INamedTypeSymbol structLayout,
            INamedTypeSymbol optional,
            INamedTypeSymbol self,
            INamedTypeSymbol up,
            INamedTypeSymbol upAncestorsFirst,
            INamedTypeSymbol upDescendantsFirst,
            INamedTypeSymbol singleton,
            INamedTypeSymbol? sparse)
        {
            _aspect = aspect;
            _entity = entity;
            _tableView = tableView;
            _structLayout = structLayout;
            _optional = optional;
            _self = self;
            _up = up;
            _upAncestorsFirst = upAncestorsFirst;
            _upDescendantsFirst = upDescendantsFirst;
            _singleton = singleton;
            _sparse = sparse;
            _accessorOnlyAttributes = ImmutableArray.Create(
                optional, self, up, upAncestorsFirst, upDescendantsFirst, singleton);
            _sourcingAttributes = ImmutableArray.Create(
                up, upAncestorsFirst, upDescendantsFirst, singleton);
        }

        public static KnownSymbols? TryCreate(Compilation compilation)
        {
            INamedTypeSymbol? aspect = compilation.GetTypeByMetadataName("NoodleStudios.Flecs.IAspect");
            if (aspect is null)
                return null;

            INamedTypeSymbol? entity = compilation.GetTypeByMetadataName("NoodleStudios.Flecs.Entity");
            INamedTypeSymbol? tableView = compilation.GetTypeByMetadataName("NoodleStudios.Flecs.TableView");
            INamedTypeSymbol? structLayout = compilation.GetTypeByMetadataName(
                "System.Runtime.InteropServices.StructLayoutAttribute");
            INamedTypeSymbol? optional = compilation.GetTypeByMetadataName("NoodleStudios.Flecs.OptionalAttribute");
            INamedTypeSymbol? self = compilation.GetTypeByMetadataName("NoodleStudios.Flecs.SelfAttribute");
            INamedTypeSymbol? up = compilation.GetTypeByMetadataName("NoodleStudios.Flecs.UpAttribute");
            INamedTypeSymbol? upAncestorsFirst = compilation.GetTypeByMetadataName(
                "NoodleStudios.Flecs.UpAncestorsFirstAttribute");
            INamedTypeSymbol? upDescendantsFirst = compilation.GetTypeByMetadataName(
                "NoodleStudios.Flecs.UpDescendantsFirstAttribute");
            INamedTypeSymbol? singleton = compilation.GetTypeByMetadataName("NoodleStudios.Flecs.SingletonAttribute");
            INamedTypeSymbol? sparse = compilation.GetTypeByMetadataName("NoodleStudios.Flecs.SparseAttribute");

            if (entity is null || tableView is null || structLayout is null || optional is null
                || self is null || up is null || upAncestorsFirst is null || upDescendantsFirst is null
                || singleton is null)
            {
                return null;
            }

            return new KnownSymbols(
                aspect, entity, tableView, structLayout, optional, self, up,
                upAncestorsFirst, upDescendantsFirst, singleton, sparse);
        }

        public void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (type.TypeKind is not (TypeKind.Struct or TypeKind.Class))
                return;

            ReportSparseTag(context, type);

            if (!type.AllInterfaces.Contains(_aspect, SymbolEqualityComparer.Default))
                return;

            if (!type.IsRefLikeType)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    NotRefStruct, type.Locations[0], type.Name));

                if (type.TypeKind == TypeKind.Class)
                    return;
            }

            AnalyzeLayout(context, type);
            AnalyzeFields(context, type);
        }

        private void AnalyzeLayout(SymbolAnalysisContext context, INamedTypeSymbol type)
        {
            AttributeData? layout = type.GetAttributes().FirstOrDefault(
                a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _structLayout));

            // LayoutKind.Sequential is the boxed int 0 in the attribute's first
            // constructor argument.
            bool sequential = layout is { ConstructorArguments.Length: > 0 }
                && layout.ConstructorArguments[0].Value is int kind
                && kind == 0;

            if (!sequential)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingSequentialLayout, type.Locations[0], type.Name));
            }
        }

        private void AnalyzeFields(SymbolAnalysisContext context, INamedTypeSymbol type)
        {
            var accessors = new System.Collections.Generic.List<AccessorTerm>();

            foreach (IFieldSymbol field in type.GetMembers().OfType<IFieldSymbol>())
            {
                if (field.IsStatic || field.IsConst || field.IsImplicitlyDeclared)
                    continue;

                SlotKind slot = Classify(field);
                switch (slot)
                {
                    case SlotKind.Entity:
                        ReportMisplacedAttributes(context, field, "Entity");
                        break;
                    case SlotKind.TableView:
                        ReportMisplacedAttributes(context, field, "TableView");
                        break;
                    case SlotKind.Accessor:
                        AnalyzeAccessor(context, field, accessors);
                        break;
                    default:
                        context.ReportDiagnostic(Diagnostic.Create(
                            InvalidField, field.Locations[0], field.Name));
                        break;
                }
            }

            ReportDuplicateTerms(context, accessors);
        }

        private SlotKind Classify(IFieldSymbol field)
        {
            ITypeSymbol fieldType = field.Type;
            bool isEntity = SymbolEqualityComparer.Default.Equals(fieldType, _entity);
            bool isTableView = SymbolEqualityComparer.Default.Equals(fieldType, _tableView);

            if (field.RefKind == RefKind.None)
            {
                if (isEntity)
                    return SlotKind.Entity;
                if (isTableView)
                    return SlotKind.TableView;
                return SlotKind.Invalid;
            }

            if (fieldType.IsValueType && fieldType.IsUnmanagedType && !isEntity && !isTableView)
                return SlotKind.Accessor;

            return SlotKind.Invalid;
        }

        private void ReportMisplacedAttributes(SymbolAnalysisContext context, IFieldSymbol field, string slotKind)
        {
            foreach (AttributeData attr in field.GetAttributes())
            {
                if (attr.AttributeClass is null)
                    continue;

                if (_accessorOnlyAttributes.Contains(attr.AttributeClass, SymbolEqualityComparer.Default))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MisplacedAccessorAttribute,
                        AttributeLocation(attr, field),
                        field.Name,
                        slotKind,
                        TrimAttributeSuffix(attr.AttributeClass.Name)));
                }
            }
        }

        private void AnalyzeAccessor(
            SymbolAnalysisContext context,
            IFieldSymbol field,
            System.Collections.Generic.List<AccessorTerm> accessors)
        {
            bool hasSelf = false;
            bool optional = false;
            int sourcingCount = 0;
            SourcingKind sourcing = SourcingKind.Self;
            ITypeSymbol? relationship = null;

            foreach (AttributeData attr in field.GetAttributes())
            {
                INamedTypeSymbol? attrClass = attr.AttributeClass;
                if (attrClass is null)
                    continue;

                if (SymbolEqualityComparer.Default.Equals(attrClass, _self))
                {
                    hasSelf = true;
                }
                else if (SymbolEqualityComparer.Default.Equals(attrClass, _optional))
                {
                    optional = true;
                }
                else if (SymbolEqualityComparer.Default.Equals(attrClass, _singleton))
                {
                    sourcingCount++;
                    sourcing = SourcingKind.Singleton;
                    relationship = null;
                }
                else if (SymbolEqualityComparer.Default.Equals(attrClass, _up))
                {
                    sourcingCount++;
                    sourcing = SourcingKind.Up;
                    relationship = RelationshipOf(attr);
                }
                else if (SymbolEqualityComparer.Default.Equals(attrClass, _upAncestorsFirst))
                {
                    sourcingCount++;
                    sourcing = SourcingKind.UpAncestorsFirst;
                    relationship = RelationshipOf(attr);
                }
                else if (SymbolEqualityComparer.Default.Equals(attrClass, _upDescendantsFirst))
                {
                    sourcingCount++;
                    sourcing = SourcingKind.UpDescendantsFirst;
                    relationship = RelationshipOf(attr);
                }
            }

            if (sourcingCount > 1 || (hasSelf && sourcingCount > 0))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ConflictingSourcing, field.Locations[0], field.Name));
                return;
            }

            if (IsTagShaped(field.Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    TagAccessor, field.Locations[0], field.Name, field.Type.Name));
                return;
            }

            accessors.Add(new AccessorTerm(field, field.Type, sourcing, relationship, field.RefKind, optional));
        }

        private void ReportSparseTag(SymbolAnalysisContext context, INamedTypeSymbol type)
        {
            if (_sparse is null || type.TypeKind != TypeKind.Struct || !IsTagShaped(type))
                return;

            bool sparse = type.GetAttributes().Any(
                a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _sparse));
            if (sparse)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SparseTag, type.Locations[0], type.Name));
            }
        }

        // Mirrors ComponentId<T>.IsTag: a struct with no instance fields and no
        // opaque [StructLayout(Size > 1)] registers as a zero-storage tag. The
        // compiler emits a metadata size of 1 for every field-less struct, so the
        // runtime rule treats size 1 as "no opaque storage". Only an explicit size
        // above 1 marks intentional storage, so the threshold here is > 1 to match.
        // Auto-property backing fields are instance fields, so they are
        // intentionally not filtered out here.
        private bool IsTagShaped(ITypeSymbol type)
        {
            if (type.TypeKind != TypeKind.Struct)
                return false;

            foreach (ISymbol member in type.GetMembers())
            {
                if (member is IFieldSymbol { IsStatic: false, IsConst: false })
                    return false;
            }

            foreach (AttributeData attr in type.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _structLayout))
                    continue;

                foreach (var named in attr.NamedArguments)
                {
                    if (named.Key == "Size" && named.Value.Value is int size && size > 1)
                        return false;
                }
            }

            return true;
        }

        private void ReportDuplicateTerms(
            SymbolAnalysisContext context,
            System.Collections.Generic.List<AccessorTerm> accessors)
        {
            for (int i = 0; i < accessors.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (accessors[i].SameTermAs(accessors[j]))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DuplicateTerm,
                            accessors[i].Field.Locations[0],
                            accessors[i].Field.Name,
                            accessors[j].Field.Name));
                        break;
                    }
                }
            }
        }

        private static ITypeSymbol? RelationshipOf(AttributeData attr) =>
            attr.ConstructorArguments.Length > 0
                ? attr.ConstructorArguments[0].Value as ITypeSymbol
                : null;

        private static Location AttributeLocation(AttributeData attr, IFieldSymbol fallback) =>
            attr.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? fallback.Locations[0];

        private static string TrimAttributeSuffix(string name) =>
            name.EndsWith("Attribute", System.StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "Attribute".Length)
                : name;

        private enum SlotKind
        {
            Invalid,
            Entity,
            TableView,
            Accessor,
        }

        private enum SourcingKind
        {
            Self,
            Up,
            UpAncestorsFirst,
            UpDescendantsFirst,
            Singleton,
        }

        private readonly struct AccessorTerm
        {
            public AccessorTerm(
                IFieldSymbol field,
                ITypeSymbol component,
                SourcingKind sourcing,
                ITypeSymbol? relationship,
                RefKind refKind,
                bool optional)
            {
                Field = field;
                _component = component;
                _sourcing = sourcing;
                _relationship = relationship;
                _refKind = refKind;
                _optional = optional;
            }

            public IFieldSymbol Field { get; }

            private readonly ITypeSymbol _component;
            private readonly SourcingKind _sourcing;
            private readonly ITypeSymbol? _relationship;
            private readonly RefKind _refKind;
            private readonly bool _optional;

            public bool SameTermAs(AccessorTerm other) =>
                _sourcing == other._sourcing
                && _refKind == other._refKind
                && _optional == other._optional
                && SymbolEqualityComparer.Default.Equals(_component, other._component)
                && SymbolEqualityComparer.Default.Equals(_relationship, other._relationship);
        }
    }
}
