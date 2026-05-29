using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     A compiled query whose per-row iteration binds the byref fields of an
///     aspect <typeparamref name="TAspect"/> to the matched row's component
///     values. Use the untyped <see cref="Flecs.Query"/> accessible through
///     <see cref="Untyped"/> for lifetime management.
/// </summary>
public unsafe readonly struct Query<TAspect> where TAspect : struct, IAspect, allows ref struct
{
    private readonly Query _inner;
    private readonly int[] _slotToTermIndex;
    private readonly byte[] _slotToFieldIndex;
    private readonly AspectDescriptor _descriptor;

    internal Query(Query inner, int[] slotToTermIndex, AspectDescriptor descriptor)
    {
        _inner = inner;
        _slotToTermIndex = slotToTermIndex;
        _descriptor = descriptor;

        // An accessor's flecs field index can differ from its term index, because
        // OR-group members collapse to one field. The per-row binder reads the
        // field, so resolve and cache the field index for each accessor slot now,
        // from the term it seeded into the built query.
        _slotToFieldIndex = new byte[slotToTermIndex.Length];
        ecs_term_t* terms = (ecs_term_t*)(&inner.Handle->terms);
        for (int slot = 0; slot < _slotToTermIndex.Length; slot++)
            _slotToFieldIndex[slot] = terms[slotToTermIndex[slot]].field_index;
    }

    /// <summary>
    ///     The underlying untyped query. Pass it to
    ///     <see cref="World.DestroyQuery(Query)"/> to free a cached or uncached
    ///     typed query, or to APIs that take a plain <see cref="Flecs.Query"/>.
    /// </summary>
    public Query Untyped => _inner;

    /// <summary>
    ///     The entity this query is backed by, or <see cref="Flecs.Entity.None"/> if
    ///     it has none. Mirrors <see cref="Query.Entity"/> on the untyped query.
    /// </summary>
    public Entity Entity => _inner.Entity;

    /// <summary>
    ///     For each accessor slot (indexed by its position among accessor slots in
    ///     the aspect's descriptor), the index of the term that was seeded for it
    ///     at build time. 
    /// </summary>
    internal ReadOnlySpan<int> SlotToTermIndex => _slotToTermIndex;

    /// <summary>
    ///     Begin iterating the matched rows, binding each row's components into a
    ///     fresh <typeparamref name="TAspect"/>.
    /// </summary>
    public Enumerator GetEnumerator()
    {
        GuardHandle();
        return new Enumerator(_inner.World, _inner.Handle, _descriptor, _slotToFieldIndex, _slotToTermIndex);
    }

    // Iterating a null-handle query aborts in the native layer.
    [Conditional("DEBUG")]
    private void GuardHandle()
    {
        if (_inner.Handle == null)
            throw new InvalidOperationException(
                "This query has no handle. Build it with BuildCached/BuildUncached/BuildDisposable before iterating.");
    }

    /// <summary>
    ///     Iterates the rows matched by a <see cref="Query{TAspect}"/>, binding each
    ///     row's components into <see cref="Current"/>.
    /// </summary>
    /// <remarks>
    ///     The iterator holds native resources that are released when iteration runs
    ///     to completion, or by <see cref="Dispose"/> on an early <c>break</c>.
    ///     <c>foreach</c> calls <see cref="Dispose"/> automatically.
    /// </remarks>
    public ref struct Enumerator
    {
        private ecs_iter_t _it;
        private TAspect _aspect;

        private readonly AspectDescriptor _descriptor;
        private readonly byte[] _slotToFieldIndex;
        private readonly int[] _slotToTermIndex;

        private ScratchBuffer _scratch;

        private int _row;
        private int _count;
        private bool _finished;

        internal Enumerator(
            ecs_world_t* world,
            ecs_query_t* handle,
            AspectDescriptor descriptor,
            byte[] slotToFieldIndex,
            int[] slotToTermIndex)
        {
            Debug.Assert(descriptor.FieldSlotCount <= FLECS_TERM_COUNT_MAX);

            _descriptor = descriptor;
            _slotToFieldIndex = slotToFieldIndex;
            _slotToTermIndex = slotToTermIndex;

            // Zero the aspect so its ref fields read as null until a row binds them.
            // A row is always bound before Current is read.
            _aspect = default!;

            _it = ecs_query_iter(world, handle);
            _row = 0;
            _count = 0;
            _finished = false;
        }

        /// <summary>
        ///     The aspect bound to the current row.
        /// </summary>
        [UnscopedRef]
        public ref readonly TAspect Current => ref _aspect;

        public bool MoveNext()
        {
            if (_finished)
                return false;

            if (_row < _count)
            {
                BindRow();
                _row++;
                return true;
            }

            while (true)
            {
                if (!ecs_query_next((ecs_iter_t*)Unsafe.AsPointer(ref _it)))
                {
                    _finished = true;
                    return false;
                }

                _count = _it.count;
                if (_count == 0)
                    continue;

                ResolveTable();
                _row = 0;
                BindRow();
                _row++;
                return true;
            }
        }

        /// <summary>
        ///     Release the iterator's native resources if iteration has not already
        ///     finalized them. Safe to call more than once.
        /// </summary>
        public void Dispose()
        {
            if (_finished)
                return;

            ecs_iter_fini((ecs_iter_t*)Unsafe.AsPointer(ref _it));
            _finished = true;
        }

        // Resolve each accessor slot's storage shape once per matched table
        private void ResolveTable()
        {
            var it = (ecs_iter_t*)Unsafe.AsPointer(ref _it);

            foreach (AspectSlot slot in _descriptor.Slots)
            {
                if (slot.Kind != AspectSlotKind.ComponentAccessor)
                    continue;

                int ordinal = slot.SeedTermIndex;
                byte fieldIndex = _slotToFieldIndex[ordinal];
                uint mask = 1u << fieldIndex;

                bool isSparse = (_it.row_fields & mask) != 0;
                bool isShared = !ecs_field_is_self(it, fieldIndex);
                bool present = !slot.Optional
                    || ((_it.query->data_fields & mask) != 0 && ecs_field_is_set(it, fieldIndex));

                int size = 0;
                void* fieldBase = null;
                if (present)
                {
                    size = (int)ecs_field_size(it, fieldIndex);
                    if (!isSparse)
                        fieldBase = ecs_field_w_size(it, size, fieldIndex);
                }

                ref SlotScratch s = ref _scratch[ordinal];
                s.FieldIndex = fieldIndex;
                s.IsSparse = isSparse;
                s.IsShared = isShared;
                s.Present = present;
                s.Size = size;
                s.Base = fieldBase;

                GuardAccessorSize(slot, size);
                GuardInOutMatch(slot, ordinal);
                GuardIncidentalSharedWrite(slot, isShared, present);
            }
        }

        private void BindRow()
        {
            var it = (ecs_iter_t*)Unsafe.AsPointer(ref _it);
            byte* p = (byte*)Unsafe.AsPointer(ref _aspect);

            foreach (AspectSlot slot in _descriptor.Slots)
            {
                switch (slot.Kind)
                {
                    case AspectSlotKind.Entity:
                        *(ulong*)(p + slot.Offset) = _it.entities[_row];
                        break;

                    case AspectSlotKind.TableView:
                        // A TableView field is just the iterator pointer. Bind it here,
                        // not in the constructor: GetEnumerator returns the enumerator by
                        // value, so the iterator moves into the foreach frame and only the
                        // address taken here, after that copy, stays valid for the read.
                        *(void**)(p + slot.Offset) = it;
                        break;

                    case AspectSlotKind.ComponentAccessor:
                    {
                        ref SlotScratch s = ref _scratch[slot.SeedTermIndex];
                        void* target;
                        if (!s.Present)
                            target = null; // unmatched optional binds a null reference
                        else if (s.IsSparse)
                            target = ecs_field_at_w_size(it, s.Size, s.FieldIndex, _row);
                        else if (s.IsShared)
                            target = s.Base;
                        else
                            target = (byte*)s.Base + (nint)_row * s.Size;
                        *(void**)(p + slot.Offset) = target;
                        break;
                    }
                }
            }
        }

        // A non-optional accessor whose field carries no data is a declaration bug
        // (e.g. a ref to a tag). 
        [Conditional("DEBUG")]
        private static void GuardAccessorSize(AspectSlot slot, int size) =>
            Debug.Assert(size > 0 || slot.Optional,
                $"Aspect field '{slot.Name}' resolved to a zero-size field but is not optional.");

        // A consumer can override a writable accessor's term with .In() after seeding.
        // That would silently hand back a read-only column through a ref field, so
        // assert the term still agrees with the field's ref kind.
        [Conditional("DEBUG")]
        private void GuardInOutMatch(AspectSlot slot, int ordinal)
        {
            ecs_term_t* terms = (ecs_term_t*)(&_it.query->terms);
            ecs_term_t* term = &terms[_slotToTermIndex[ordinal]];
            Debug.Assert(!(slot.RefKind == AspectRefKind.InOut && term->inout == (short)EcsIn),
                $"Aspect field '{slot.Name}' is writable but its term was overridden to In().");
        }

        // A writable self-sourced field can land on a table where the component is
        // inherited from a base. Writing through it would mutate the shared base for
        // every inheriting row, almost never what the caller meant. Explicitly
        // sourced fields ([Up*], [Singleton]) want the shared shape, so exempt them.
        [Conditional("DEBUG")]
        private static void GuardIncidentalSharedWrite(AspectSlot slot, bool isShared, bool present)
        {
            if (slot.RefKind == AspectRefKind.InOut
                && slot.Sourcing == SourcingKind.Self
                && !slot.Self
                && isShared
                && present)
            {
                throw new InvalidOperationException(
                    $"Writing field '{slot.Name}' on a table where '{slot.ComponentType!.Name}' is " +
                    "inherited would mutate the source. Mark the field [Self] (or call .Self() on the " +
                    "term) to exclude shared-source matches, change the field to ref readonly, or split " +
                    "into two queries.");
            }
        }

        // Per accessor slot, the resolved storage shape for the current table.
        private struct SlotScratch
        {
            public void* Base;
            public int Size;
            public byte FieldIndex;
            public bool IsSparse;
            public bool IsShared;
            public bool Present;
        }

        // flecs caps a query at FLECS_TERM_COUNT_MAX terms, so an aspect has at most
        // that many accessor slots. 
        [InlineArray(FLECS_TERM_COUNT_MAX)]
        private struct ScratchBuffer
        {
            private SlotScratch _element0;
        }
    }
}

/// <summary>
///     An owning, self-freeing wrapper around an uncached
///     <see cref="Query{TAspect}"/>. Free it with <c>using</c>. Mirrors
///     <see cref="DisposableQuery"/> for typed queries.
/// </summary>
public unsafe ref struct DisposableQuery<TAspect> where TAspect : struct, IAspect, allows ref struct
{
    private readonly Query<TAspect> _query;

    private bool _disposed;

    internal DisposableQuery(Query<TAspect> query) => _query = query;

    /// <summary>
    ///     The wrapped typed query. Iterate the <see cref="DisposableQuery{TAspect}"/>
    ///     directly, or use this to pass the typed query to APIs that take a
    ///     <see cref="Query{TAspect}"/>. Do not free it separately, the wrapper
    ///     owns it.
    /// </summary>
    public Query<TAspect> Query => _query;

    /// <inheritdoc cref="Query{TAspect}.GetEnumerator"/>
    public Query<TAspect>.Enumerator GetEnumerator() => _query.GetEnumerator();

    /// <summary>
    ///     Free the underlying query.
    /// </summary>
    public void Dispose()
    {
        if (_disposed || _query.Untyped.Handle == null)
            return;

        _disposed = true;
        ecs_query_fini(_query.Untyped.Handle);
    }
}
