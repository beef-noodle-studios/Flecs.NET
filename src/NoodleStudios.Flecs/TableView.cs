using System.Diagnostics;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     A view over one matched table during query iteration: the entities in the
///     table and the query's fields for those entities. 
/// </summary>
/// <remarks>
///     <para>
///         A query matches a set of tables. Iterating the query yields one
///         <see cref="TableView"/> per table. Storage shape is resolved
///         per table: the same query can yield a table where a field is
///         owned (dense, one value per row), another where it is shared (a single
///         inherited value for the whole table), and another where it is sparse
///         (stored outside the table). Pick the accessor that matches the shape, or
///         use <see cref="GetField{T}(int)"/> which dispatches on the shape for you.
///     </para>
///     <para>
///         A field can also be addressed by its position (its flecs field index,
///         OR-group members collapse to a single shared field) with the int field
///         accessor overloads
///         (<see cref="GetFieldSpan{T}(int)"/> and siblings). This is the only way to
///         read two terms that share an id, for example the same component sourced
///         from the entity and from its parent. <see cref="GetFieldId(int)"/> and
///         <see cref="GetFieldTarget(int)"/> report the concrete id flecs matched at a
///         field, which answers what a wildcard or traversal term resolved to.
///     </para>
///     <para>
///         Validation mirrors flecs's own native debug/release behavior. In a Debug build,
///         misusing an accessor throws <see cref="InvalidOperationException"/>. These checks
///         compile away in Release, where the same misuse is undefined behavior,
///         exactly as it is when calling the C API directly. 
///
///         The <c>Has*</c>/<c>Is*</c> predicates are the exception: they are always-on
///         and answer <c>false</c> for a field the query does not select, in both
///         configurations.
///     </para>
/// </remarks>
public unsafe readonly ref struct TableView
{
    private readonly ecs_iter_t* _it;

    internal TableView(ecs_iter_t* it) => _it = it;

    /// <summary>
    ///     The number of entities (rows) in this table.
    /// </summary>
    public int Count => _it->count;

    /// <summary>
    ///     The entities in this table, one per row. Indices line up with the rows
    ///     of every owned field span and with the <c>row</c> argument to
    ///     <see cref="GetField{T}(int)"/>.
    /// </summary>
    public ReadOnlySpan<Entity> Entities
    {
        get
        {
            Debug.Assert(sizeof(Entity) == sizeof(ulong));
            return new ReadOnlySpan<Entity>(_it->entities, _it->count);
        }
    }

    // --- Owned dense spans ---

    /// <summary>
    ///     Get the column of component <typeparamref name="T"/> for this table as a
    ///     span with one element per row. Use this for an owned (dense) field.
    /// </summary>
    /// <remarks>
    ///     Only valid for an owned field that carries data. A sparse or shared
    ///     field, a tag, or an unmatched optional is misuse that throws in Debug 
    ///     and is undefined behavior in Release. 
    ///
    ///     Use <see cref="GetField{T}(int)"/> for the shape-agnostic path, or
    ///     <see cref="GetFieldSpanMut{T}()"/> to write.
    /// </remarks>
    public ReadOnlySpan<T> GetFieldSpan<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        return new ReadOnlySpan<T>(FieldPtr<T>(idx, isSparse, isShared, hasData, requireWritable: false), _it->count);
    }

    /// <inheritdoc cref="GetFieldSpan{T}()"/>
    public ReadOnlySpan<T> GetFieldSpan<T>(Id id) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        return new ReadOnlySpan<T>(FieldPtr<T>(idx, isSparse, isShared, hasData, requireWritable: false), _it->count);
    }

    /// <summary>
    ///     Get the writable column of component <typeparamref name="T"/> for this table
    ///     as a span with one element per row. Use this for an owned (dense) field you
    ///     intend to modify.
    /// </summary>
    /// <remarks>
    ///     As <see cref="GetFieldSpan{T}()"/>, but the field must also be writable: a
    ///     field selected with <see cref="QueryBuilder.In"/> is read-only, and asking for
    ///     a mutable span of it throws in Debug and is undefined behavior in Release. Read
    ///     it with <see cref="GetFieldSpan{T}()"/> instead.
    /// </remarks>
    public Span<T> GetFieldSpanMut<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        return new Span<T>(FieldPtr<T>(idx, isSparse, isShared, hasData, requireWritable: true), _it->count);
    }

    /// <inheritdoc cref="GetFieldSpanMut{T}()"/>
    public Span<T> GetFieldSpanMut<T>(Id id) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        return new Span<T>(FieldPtr<T>(idx, isSparse, isShared, hasData, requireWritable: true), _it->count);
    }

    /// <summary>
    ///     Get the read-only owned column of component <typeparamref name="T"/> if this
    ///     table has it with data, for example an optional term that matches this table.
    /// </summary>
    /// <returns>
    ///     True and the span if the field is present with data on this table;
    ///     otherwise false and an empty span. A sparse or shared field, or a wrong
    ///     <typeparamref name="T"/> for a present field, is misuse that throws in Debug
    ///     and is undefined behavior in Release.
    /// </returns>
    public bool TryGetFieldSpan<T>(out ReadOnlySpan<T> span) where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        bool ok = TryFieldPtr<T>(idx, isSparse, isShared, hasData, requireWritable: false, out T* ptr);
        span = ok ? new ReadOnlySpan<T>(ptr, _it->count) : default;
        return ok;
    }

    /// <inheritdoc cref="TryGetFieldSpan{T}(out ReadOnlySpan{T})"/>
    public bool TryGetFieldSpan<T>(Id id, out ReadOnlySpan<T> span) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        bool ok = TryFieldPtr<T>(idx, isSparse, isShared, hasData, requireWritable: false, out T* ptr);
        span = ok ? new ReadOnlySpan<T>(ptr, _it->count) : default;
        return ok;
    }

    /// <summary>
    ///     As <see cref="TryGetFieldSpan{T}(out ReadOnlySpan{T})"/>, but returns a writable
    ///     span. A present field selected with <see cref="QueryBuilder.In"/> is read-only,
    ///     and asking for a mutable span of it throws in Debug (undefined behavior in Release).
    /// </summary>
    public bool TryGetFieldSpanMut<T>(out Span<T> span) where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        bool ok = TryFieldPtr<T>(idx, isSparse, isShared, hasData, requireWritable: true, out T* ptr);
        span = ok ? new Span<T>(ptr, _it->count) : default;
        return ok;
    }

    /// <inheritdoc cref="TryGetFieldSpanMut{T}(out Span{T})"/>
    public bool TryGetFieldSpanMut<T>(Id id, out Span<T> span) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        bool ok = TryFieldPtr<T>(idx, isSparse, isShared, hasData, requireWritable: true, out T* ptr);
        span = ok ? new Span<T>(ptr, _it->count) : default;
        return ok;
    }

    // --- Universal per-row accessor ---

    /// <summary>
    ///     Get a read-only reference to component <typeparamref name="T"/> for a single
    ///     row, dispatching on the field's storage shape: owned reads the row, sparse
    ///     reads it out of the sparse set, and shared reads the single inherited value.
    /// </summary>
    /// <remarks>
    ///     The field must be selected by the query, carry data, and match
    ///     <typeparamref name="T"/>'s size, and <paramref name="row"/> must be in
    ///     <c>[0, Count)</c>. Otherwise this throws in Debug and is undefined behavior in
    ///     Release. Use <see cref="GetFieldMut{T}(int)"/> to write.
    /// </remarks>
    public ref readonly T GetField<T>(int row) where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        return ref Field<T>(idx, isSparse, isShared, hasData, row, requireWritable: false);
    }

    /// <inheritdoc cref="GetField{T}(int)"/>
    public ref readonly T GetField<T>(Id id, int row) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        return ref Field<T>(idx, isSparse, isShared, hasData, row, requireWritable: false);
    }

    /// <summary>
    ///     Get a writable reference to component <typeparamref name="T"/> for a single
    ///     row, dispatching on the field's storage shape as <see cref="GetField{T}(int)"/>
    ///     does.
    /// </summary>
    /// <remarks>
    ///     Writing through a shared field mutates the base entity, and therefore every
    ///     entity that inherits the value. As <see cref="GetField{T}(int)"/>, but the field
    ///     must also be writable: a field selected with <see cref="QueryBuilder.In"/> is
    ///     read-only and writing to it throws in Debug (undefined behavior in Release).
    /// </remarks>
    public ref T GetFieldMut<T>(int row) where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        return ref Field<T>(idx, isSparse, isShared, hasData, row, requireWritable: true);
    }

    /// <inheritdoc cref="GetFieldMut{T}(int)"/>
    public ref T GetFieldMut<T>(Id id, int row) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        return ref Field<T>(idx, isSparse, isShared, hasData, row, requireWritable: true);
    }

    // --- Shared (inherited) singleton ---

    /// <summary>
    ///     Get a read-only reference to the single shared value of component
    ///     <typeparamref name="T"/> for this table as inherited from a base entity.
    ///     One value backs the whole table.
    /// </summary>
    /// <remarks>
    ///     Only valid when the field is shared on this table. An owned or sparse
    ///     field, a field with no data, or a size mismatch is misuse (Debug throw /
    ///     Release UB). Use <see cref="HasSharedField{T}()"/> to test the shape
    ///     first when a query can match both owned and inherited tables, or
    ///     <see cref="GetSharedFieldMut{T}()"/> to write.
    /// </remarks>
    public ref readonly T GetSharedField<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        return ref SharedField<T>(idx, isSparse, isShared, hasData, requireWritable: false);
    }

    /// <inheritdoc cref="GetSharedField{T}()"/>
    public ref readonly T GetSharedField<T>(Id id) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        return ref SharedField<T>(idx, isSparse, isShared, hasData, requireWritable: false);
    }

    /// <summary>
    ///     Get a writable reference to the single shared value of component
    ///     <typeparamref name="T"/> for this table. Writing through it mutates the base
    ///     entity, and therefore every entity that inherits the value.
    /// </summary>
    /// <remarks>
    ///     As <see cref="GetSharedField{T}()"/>, but the field must also be writable: a
    ///     field selected with <see cref="QueryBuilder.In"/> is read-only and writing to it
    ///     throws in Debug (undefined behavior in Release).
    /// </remarks>
    public ref T GetSharedFieldMut<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        return ref SharedField<T>(idx, isSparse, isShared, hasData, requireWritable: true);
    }

    /// <inheritdoc cref="GetSharedFieldMut{T}()"/>
    public ref T GetSharedFieldMut<T>(Id id) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        return ref SharedField<T>(idx, isSparse, isShared, hasData, requireWritable: true);
    }

    // --- Predicates ---

    /// <summary>
    ///     Test whether this table has readable data for component
    ///     <typeparamref name="T"/>, the query selects it and it carries data on
    ///     this table. Returns false for a field the query does not select, an
    ///     unmatched optional, a <c>None()</c>/excluded term, or a tag.
    /// </summary>
    public bool HasField<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out _, out _, out bool hasData);
        return idx >= 0 && hasData;
    }

    /// <inheritdoc cref="HasField{T}()"/>
    public bool HasField(Id id)
    {
        int idx = Resolve(id, out _, out _, out bool hasData);
        return idx >= 0 && hasData;
    }

    /// <summary>
    ///     Test whether this table has a shared (inherited or sourced) binding
    ///     for component <typeparamref name="T"/>. Returns false for a field
    ///     the query does not select, a field that is self-bound on this table,
    ///     or an unmatched optional.
    /// </summary>
    public bool HasSharedField<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out _, out bool isShared, out bool hasData);
        return idx >= 0 && hasData && isShared;
    }

    /// <inheritdoc cref="HasSharedField{T}()"/>
    public bool HasSharedField(Id id)
    {
        int idx = Resolve(id, out _, out bool isShared, out bool hasData);
        return idx >= 0 && hasData && isShared;
    }

    /// <summary>
    ///     Test whether this table has a self-owned binding for component
    ///     <typeparamref name="T"/>. Returns false for a field the query does
    ///     not select, a field that is shared (inherited or sourced) on this
    ///     table, or an unmatched optional.
    /// </summary>
    public bool HasSelfField<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out _, out bool isShared, out bool hasData);
        return idx >= 0 && hasData && !isShared;
    }

    /// <inheritdoc cref="HasSelfField{T}()"/>
    public bool HasSelfField(Id id)
    {
        int idx = Resolve(id, out _, out bool isShared, out bool hasData);
        return idx >= 0 && hasData && !isShared;
    }

    /// <summary>
    ///     Test whether this table has a sparse binding for component
    ///     <typeparamref name="T"/>, stored in a sparse set rather than in the
    ///     table. Returns false for a field the query does not select or an
    ///     unmatched optional.
    /// </summary>
    public bool HasSparseField<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out _, out bool hasData);
        return idx >= 0 && hasData && isSparse;
    }

    /// <inheritdoc cref="HasSparseField{T}()"/>
    public bool HasSparseField(Id id)
    {
        int idx = Resolve(id, out bool isSparse, out _, out bool hasData);
        return idx >= 0 && hasData && isSparse;
    }

    // --- Positional ---

    /// <summary>
    ///     Get the owned (dense) column at field <paramref name="field"/> as a span with
    ///     one element per row. Fields are numbered by flecs field index, which matches
    ///     the order terms were added except that OR-group members collapse to a single
    ///     field. This is the only way to read one of two terms that share an id.
    /// </summary>
    /// <remarks>
    ///     Only valid for an owned field that carries data and matches 
    ///     <typeparamref name="T"/>'s size. A field index outside <c>[0, field count)</c>
    ///     throws in Debug and is undefined behavior in Release.
    /// </remarks>
    public ReadOnlySpan<T> GetFieldSpan<T>(int field) where T : unmanaged
    {
        DebugFieldBounds(field);
        Shape(field, out bool isSparse, out bool isShared, out bool hasData);
        return new ReadOnlySpan<T>(FieldPtr<T>(field, isSparse, isShared, hasData, requireWritable: false), _it->count);
    }

    /// <summary>
    ///     Get the writable owned (dense) column at field <paramref name="field"/> as a
    ///     span with one element per row.
    /// </summary>
    /// <remarks>
    ///     Field must be writable. 
    /// </remarks>
    public Span<T> GetFieldSpanMut<T>(int field) where T : unmanaged
    {
        DebugFieldBounds(field);
        Shape(field, out bool isSparse, out bool isShared, out bool hasData);
        return new Span<T>(FieldPtr<T>(field, isSparse, isShared, hasData, requireWritable: true), _it->count);
    }

    /// <summary>
    ///     Get the read-only owned column at field <paramref name="field"/> if it is
    ///     present with data on this table.
    /// </summary>
    public bool TryGetFieldSpan<T>(int field, out ReadOnlySpan<T> span) where T : unmanaged
    {
        DebugFieldBounds(field);
        Shape(field, out bool isSparse, out bool isShared, out bool hasData);
        bool ok = TryFieldPtr<T>(field, isSparse, isShared, hasData, requireWritable: false, out T* ptr);
        span = ok ? new ReadOnlySpan<T>(ptr, _it->count) : default;
        return ok;
    }

    /// <summary>
    ///     Same as <see cref="TryGetFieldSpan{T}(int, out ReadOnlySpan{T})"/>, but returns a
    ///     writable span.
    /// </summary>
    /// <remarks>
    ///     Field must be writable.
    /// </remarks>
    public bool TryGetFieldSpanMut<T>(int field, out Span<T> span) where T : unmanaged
    {
        DebugFieldBounds(field);
        Shape(field, out bool isSparse, out bool isShared, out bool hasData);
        bool ok = TryFieldPtr<T>(field, isSparse, isShared, hasData, requireWritable: true, out T* ptr);
        span = ok ? new Span<T>(ptr, _it->count) : default;
        return ok;
    }

    /// <summary>
    ///     Get a read-only reference to the value at field <paramref name="field"/> for a
    ///     single <paramref name="row"/>, dispatching on the field's storage shape as
    ///     <see cref="GetField{T}(int)"/> does.
    /// </summary>
    /// <remarks>
    ///     The field must be selected, carry data, and match <typeparamref name="T"/>'s size, 
    ///     and <paramref name="row"/> must be in <c>[0, Count)</c>. 
    /// </remarks>
    public ref readonly T GetFieldAt<T>(int field, int row) where T : unmanaged
    {
        DebugFieldBounds(field);
        Shape(field, out bool isSparse, out bool isShared, out bool hasData);
        return ref Field<T>(field, isSparse, isShared, hasData, row, requireWritable: false);
    }

    /// <summary>
    ///     Get a writable reference to the value at field <paramref name="field"/> for a
    ///     single <paramref name="row"/>. Same as <see cref="GetFieldAt{T}(int, int)"/>, but
    ///     the field must also be writable.
    /// </summary>
    public ref T GetFieldAtMut<T>(int field, int row) where T : unmanaged
    {
        DebugFieldBounds(field);
        Shape(field, out bool isSparse, out bool isShared, out bool hasData);
        return ref Field<T>(field, isSparse, isShared, hasData, row, requireWritable: true);
    }

    /// <summary>
    ///     Get a read-only reference to the single shared (inherited) value at field
    ///     <paramref name="field"/>. Same as <see cref="GetSharedField{T}()"/> but 
    ///     addressed by index. This is how an up-sourced term's value is read.
    /// </summary>
    /// <remarks>
    ///     Only valid when the field is shared on this table. An owned or sparse field, a
    ///     field with no data, a size mismatch, or a field index outside
    ///     <c>[0, field count)</c> throws in Debug and is undefined behavior in Release.
    /// </remarks>
    public ref readonly T GetSharedField<T>(int field) where T : unmanaged
    {
        DebugFieldBounds(field);
        Shape(field, out bool isSparse, out bool isShared, out bool hasData);
        return ref SharedField<T>(field, isSparse, isShared, hasData, requireWritable: false);
    }

    /// <summary>
    ///     Get a writable reference to the single shared (inherited) value at field
    ///     <paramref name="field"/>. Same as <see cref="GetSharedField{T}(int)"/>, but 
    ///     the field must also be writable. A non-self source (traversal or a fixed
    ///     <see cref="QueryBuilder.Source(Entity)"/>) is read-only by default. Mark the term
    ///     <see cref="QueryBuilder.InOut()"/> or <see cref="QueryBuilder.Out()"/> to write
    ///     through it. Otherwise this throws in Debug (undefined behavior in Release).
    /// </summary>
    public ref T GetSharedFieldMut<T>(int field) where T : unmanaged
    {
        DebugFieldBounds(field);
        Shape(field, out bool isSparse, out bool isShared, out bool hasData);
        return ref SharedField<T>(field, isSparse, isShared, hasData, requireWritable: true);
    }

    /// <summary>
    ///     Test whether the field at <paramref name="field"/> carries readable data on
    ///     this table. Returns false for an unmatched optional, a <c>None()</c>/excluded
    ///     term, a tag, or a field index outside <c>[0, field count)</c>.
    /// </summary>
    public bool HasField(int field)
    {
        if ((uint)field >= (uint)_it->field_count)
            return false;
        Shape(field, out _, out _, out bool hasData);
        return hasData;
    }

    /// <summary>
    ///     Test whether the field at <paramref name="field"/> is shared (inherited from a
    ///     base or sourced from another entity) on this table rather than owned per row.
    ///     Returns false for a field index outside <c>[0, field count)</c>.
    /// </summary>
    public bool IsFieldShared(int field)
    {
        if ((uint)field >= (uint)_it->field_count)
            return false;
        Shape(field, out _, out bool isShared, out _);
        return isShared;
    }

    /// <summary>
    ///     Test whether the field at <paramref name="field"/> is stored in a sparse set
    ///     rather than in the table. Returns false for a field index outside
    ///     <c>[0, field count)</c>.
    /// </summary>
    public bool IsFieldSparse(int field)
    {
        if ((uint)field >= (uint)_it->field_count)
            return false;
        Shape(field, out bool isSparse, out _, out _);
        return isSparse;
    }

    /// <summary>
    ///     Get the concrete id flecs matched at field <paramref name="field"/> on this
    ///     table. For a wildcard or traversal term this is the resolved id, which can
    ///     differ per table (e.g. <c>(Likes, Apples)</c> for a <c>(Likes, *)</c> term).
    /// </summary>
    public Id GetFieldId(int field)
    {
        DebugFieldBounds(field);
        return new Id(_it->ids[field]);
    }

    /// <summary>
    ///     Get the alive target entity of the pair matched at field
    ///     <paramref name="field"/>. This is the answer to "what did this wildcard relationship
    ///     match" (e.g. <c>Apples</c> for a matched <c>(Likes, *)</c>).
    /// </summary>
    /// <remarks>
    ///     Requires a concretely matched pair field: the field index must be in
    ///     <c>[0, field count)</c> and the matched id must be a pair (both Debug throw /
    ///     Release UB). Behavior on an unmatched-optional wildcard field, where the
    ///     matched id may still hold a wildcard target, is undefined.
    /// </remarks>
    public Entity GetFieldTarget(int field)
    {
        DebugFieldBounds(field);
        Id id = new(_it->ids[field]);
        DebugFieldIsPair(id);
        return new Entity(new Id(ecs_get_alive(_it->real_world, id.Second)));
    }

    /// <summary>
    ///     Get the entity that supplied the data for field <paramref name="field"/>
    ///     on this table, for example the ancestor an <c>Up</c>-traversed term
    ///     resolved to or the singleton a fixed-source term reads from. Returns
    ///     <see cref="Entity.None"/> for a self-bound field or when the query has
    ///     no shared sources at all (the underlying sources array is null).
    /// </summary>
    /// <remarks>
    ///     The field index must be in <c>[0, field count)</c>, otherwise this
    ///     throws in Debug and is undefined behavior in Release.
    /// </remarks>
    public Entity GetFieldSource(int field)
    {
        DebugFieldBounds(field);
        ulong* sources = _it->sources;
        return sources == null ? Entity.None : new Entity(new Id(sources[field]));
    }

    /// <summary>
    ///     Get the alive target entity of the pair matched at the field that
    ///     binds component <typeparamref name="T"/>, applying
    ///     <paramref name="mode"/> when more than one slot could match.
    ///     Returns <see cref="Entity.None"/> if no slot for
    ///     <typeparamref name="T"/> matches the requested shape.
    /// </summary>
    /// <remarks>
    ///     The matched id at the resolved slot must be a pair, otherwise this
    ///     throws in Debug and is undefined behavior in Release.
    /// </remarks>
    public Entity GetFieldTarget<T>(FieldScanMode mode = FieldScanMode.FirstMatch) where T : unmanaged
    {
        int idx = FindField<T>(mode);
        return idx < 0 ? Entity.None : GetFieldTarget(idx);
    }

    /// <inheritdoc cref="GetFieldTarget{T}(FieldScanMode)"/>
    public Entity GetFieldTarget(Id id, FieldScanMode mode = FieldScanMode.FirstMatch)
    {
        int idx = FindField(id, mode);
        return idx < 0 ? Entity.None : GetFieldTarget(idx);
    }

    /// <summary>
    ///     Get the entity that supplied the data for component
    ///     <typeparamref name="T"/>'s field, applying <paramref name="mode"/>
    ///     when more than one slot could match. Returns <see cref="Entity.None"/>
    ///     for a self-bound resolution, when the query has no shared sources
    ///     at all, or when no slot for <typeparamref name="T"/> matches the
    ///     requested shape.
    /// </summary>
    public Entity GetFieldSource<T>(FieldScanMode mode = FieldScanMode.FirstMatch) where T : unmanaged
    {
        int idx = FindField<T>(mode);
        return idx < 0 ? Entity.None : GetFieldSource(idx);
    }

    /// <inheritdoc cref="GetFieldSource{T}(FieldScanMode)"/>
    public Entity GetFieldSource(Id id, FieldScanMode mode = FieldScanMode.FirstMatch)
    {
        int idx = FindField(id, mode);
        return idx < 0 ? Entity.None : GetFieldSource(idx);
    }

    // --- Field lookup by id / type ---

    /// <summary>
    ///     Find the field index for component <typeparamref name="T"/> on this
    ///     table, applying <paramref name="mode"/> when more than one slot
    ///     could match. Returns -1 if the component is not registered in this
    ///     world, no slot carries its id, the matching slot is an unmatched
    ///     optional (not set on this table), or no slot matches the requested
    ///     shape.
    /// </summary>
    /// <remarks>
    ///     Use the returned index with any of the positional accessors
    ///     (<see cref="GetFieldSpan{T}(int)"/>, <see cref="GetFieldAt{T}(int, int)"/>,
    ///     <see cref="GetSharedField{T}(int)"/>, and friends) to read the slot
    ///     in the shape the mode selected for. The type-keyed accessors on
    ///     <see cref="TableView"/> always use <see cref="FieldScanMode.FirstMatch"/>.
    /// </remarks>
    public int FindField<T>(FieldScanMode mode = FieldScanMode.FirstMatch) where T : unmanaged
    {
        if (!ComponentId<T>.TryGetId(_it->real_world, out Id id))
            return -1;
        return FindField(id, mode);
    }

    /// <inheritdoc cref="FindField{T}(FieldScanMode)"/>
    public int FindField(Id id, FieldScanMode mode = FieldScanMode.FirstMatch)
    {
        int fieldCount = _it->field_count;
        ulong target = id.Value;
        int firstMatch = -1;

        for (int i = 0; i < fieldCount; i++)
        {
            if (_it->ids[i] != target)
                continue;

            // An unmatched optional keeps its id in the slot but is not set on this table. 
            if (!ecs_field_is_set(_it, (byte)i))
                continue;

            bool isSelf = ecs_field_is_self(_it, (byte)i);
            switch (mode)
            {
                case FieldScanMode.FirstMatch:
                    return i;
                case FieldScanMode.SelfOnly:
                    if (isSelf)
                        return i;
                    break;
                case FieldScanMode.SharedOnly:
                    if (!isSelf)
                        return i;
                    break;
                case FieldScanMode.PreferSelf:
                    if (isSelf)
                        return i;
                    if (firstMatch < 0)
                        firstMatch = i;
                    break;
            }
        }

        return mode == FieldScanMode.PreferSelf ? firstMatch : -1;
    }

    // --- Internal ---

    /// <summary>
    ///     Resolve a component type to its field index without registering it.
    /// </summary>
    private int Resolve<T>(out bool isSparse, out bool isShared, out bool hasData)
        where T : unmanaged
    {
        if (!ComponentId<T>.TryGetId(_it->real_world, out Id id))
        {
            isSparse = false;
            isShared = false;
            hasData = false;
            return -1;
        }

        return Resolve(id, out isSparse, out isShared, out hasData);
    }

    /// <summary>
    ///     Resolve a matched id to its field index and storage shape. On a miss return
    ///     -1 with everything false, computing no shape so that no native field call
    ///     ever receives an invalid index (a managed -1 would marshal to byte 255 and
    ///     read out of bounds). Shape is only meaningful for a found field.
    /// </summary>
    private int Resolve(Id id, out bool isSparse, out bool isShared, out bool hasData)
    {
        isSparse = false;
        isShared = false;
        hasData = false;

        // Locate leniently: a field selected by the query but unmatched here still 
        // has a valid index. The by-type accessors report that absence through hasData,
        // distinct from "not selected by this query at all" (index -1). Public FindField,
        // by contrast, hides an unmatched optional so its returned index always addresses 
        // real data.
        static unsafe int FieldIndexOf(ecs_iter_t* it, ulong target)
        {
            int fieldCount = it->field_count;
            for (int i = 0; i < fieldCount; i++)
                if (it->ids[i] == target)
                    return i;
            return -1;
        }

        int idx = FieldIndexOf(_it, id.Value);
        if (idx < 0)
            return -1;

        Shape(idx, out isSparse, out isShared, out hasData);
        return idx;
        //
    }


    /// <summary>
    ///     Compute the storage shape (sparse/shared/has-data) of the field at
    ///     <paramref name="field"/>.
    /// </summary>
    /// <remarks>
    ///     Out-of-range index is undefined behavior. Callers reaching here from a
    ///     user-supplied index guard with <see cref="DebugFieldBounds"/> first.
    /// </remarks>
    private void Shape(int field, out bool isSparse, out bool isShared, out bool hasData)
    {
        var bidx = (byte)field;
        uint mask = 1u << field;
        isSparse = (_it->row_fields & mask) != 0;
        isShared = !ecs_field_is_self(_it, bidx);
        hasData = (_it->query->data_fields & mask) != 0 && ecs_field_is_set(_it, bidx);
    }

    // The Mut accessors pass requireWritable: true, which adds the read-only guard; the
    // read-only accessors pass false and widen the returned ref/pointer to a readonly view.
    private T* FieldPtr<T>(int idx, bool isSparse, bool isShared, bool hasData, bool requireWritable)
        where T : unmanaged
    {
        DebugIsField(idx);
        DebugOwned(isSparse, isShared);
        DebugHasData(hasData);
        DebugSize<T>(idx);
        DebugType<T>(idx);
        if (requireWritable)
            DebugWritable(idx);
        return (T*)ecs_field_w_size(_it, sizeof(T), (byte)idx);
    }

    private bool TryFieldPtr<T>(int idx, bool isSparse, bool isShared, bool hasData, bool requireWritable, out T* ptr)
        where T : unmanaged
    {
        DebugIsField(idx);
        DebugOwned(isSparse, isShared);
        if (!hasData)
        {
            ptr = null;
            return false;
        }

        DebugSize<T>(idx);
        DebugType<T>(idx);
        if (requireWritable)
            DebugWritable(idx);
        ptr = (T*)ecs_field_w_size(_it, sizeof(T), (byte)idx);
        return true;
    }

    private ref T Field<T>(int idx, bool isSparse, bool isShared, bool hasData, int row, bool requireWritable)
        where T : unmanaged
    {
        DebugIsField(idx);
        DebugRow(row);
        DebugHasData(hasData);
        DebugSize<T>(idx);
        DebugType<T>(idx);
        if (requireWritable)
            DebugWritable(idx);

        if (isSparse)
            return ref *(T*)ecs_field_at_w_size(_it, sizeof(T), (byte)idx, row);

        var ptr = (T*)ecs_field_w_size(_it, sizeof(T), (byte)idx);
        return ref (isShared ? ref ptr[0] : ref ptr[row]);
    }

    private ref T SharedField<T>(int idx, bool isSparse, bool isShared, bool hasData, bool requireWritable)
        where T : unmanaged
    {
        DebugIsField(idx);
        DebugShared(isShared, isSparse);
        DebugHasData(hasData);
        DebugSize<T>(idx);
        DebugType<T>(idx);
        if (requireWritable)
            DebugWritable(idx);
        return ref ((T*)ecs_field_w_size(_it, sizeof(T), (byte)idx))[0];
    }

    [Conditional("DEBUG")]
    private static void DebugIsField(int idx)
    {
        if (idx < 0)
            throw new InvalidOperationException("The field is not selected by this query.");
    }

    [Conditional("DEBUG")]
    private static void DebugOwned(bool isSparse, bool isShared)
    {
        if (isSparse)
            throw new InvalidOperationException(
                "The field is sparse. Access it per row with GetField(row)/GetFieldMut(row).");
        if (isShared)
            throw new InvalidOperationException(
                "The field is shared on this table. Access it with GetSharedField/GetSharedFieldMut or per row with GetField(row)/GetFieldMut(row).");
    }

    [Conditional("DEBUG")]
    private static void DebugShared(bool isShared, bool isSparse)
    {
        if (!isShared)
            throw new InvalidOperationException(
                "The field is not shared on this table. Use GetFieldSpan/GetFieldSpanMut or GetField(row)/GetFieldMut(row).");
        if (isSparse)
            throw new InvalidOperationException(
                "The field is sparse. Access it per row with GetField(row)/GetFieldMut(row).");
    }

    [Conditional("DEBUG")]
    private static void DebugHasData(bool hasData)
    {
        if (!hasData)
            throw new InvalidOperationException(
                "The field has no data on this table (a tag, an unmatched optional, or a None()/excluded term).");
    }

    [Conditional("DEBUG")]
    private void DebugRow(int row)
    {
        if ((uint)row >= (uint)_it->count)
            throw new InvalidOperationException(
                $"Row {row} is out of range for a table with {_it->count} rows.");
    }

    [Conditional("DEBUG")]
    private void DebugFieldBounds(int field)
    {
        if ((uint)field >= (uint)_it->field_count)
            throw new InvalidOperationException(
                $"Field {field} is out of range for a query with {_it->field_count} fields.");
    }

    [Conditional("DEBUG")]
    private static void DebugFieldIsPair(Id id)
    {
        if (!id.IsPair)
            throw new InvalidOperationException(
                "The field is not a pair, so it has no target. GetFieldTarget is for a matched pair field, e.g. a (Likes, *) wildcard match.");
    }

    [Conditional("DEBUG")]
    private void DebugSize<T>(int idx) where T : unmanaged
    {
        int actual = (int)ecs_field_size(_it, (byte)idx);
        if (sizeof(T) != actual)
            throw new InvalidOperationException(
                $"Component type '{typeof(T).Name}' is {sizeof(T)} bytes but the field stores {actual} bytes.");
    }

    [Conditional("DEBUG")]
    private void DebugType<T>(int idx) where T : unmanaged
    {
        bool registered = ComponentId<T>.TryGetId(_it->real_world, out Id expected);
        ulong actual = ecs_get_typeid(_it->real_world, ecs_field_id(_it, (byte)idx));
        if (expected.Value == actual)
            return;

        throw new InvalidOperationException(registered
            ? $"Component type '{typeof(T).Name}' does not match the matched field's type."
            : $"Component type '{typeof(T).Name}' is not registered in this world, so it cannot match the field.");
    }

    [Conditional("DEBUG")]
    private void DebugWritable(int idx)
    {
        // ecs_field_is_readonly takes a FIELD index but indexes query->terms (the TERM
        // array). OR members share a field_index, so a term after an OR group is misread.
        // Map field->term ourselves (the first term at this field_index) and replicate the
        // read-only test against that term. Mirrors flecs.c ecs_field_is_readonly.
        ecs_term_t* terms = (ecs_term_t*)(&_it->query->terms);
        int termCount = _it->query->term_count;
        for (int t = 0; t < termCount; t++)
        {
            ecs_term_t* term = &terms[t];
            if (term->field_index != idx)
                continue;

            bool isReadonly =
                term->inout == (short)EcsIn ||
                (term->inout == (short)EcsInOutDefault &&
                 (!ecs_term_match_this(term) || (term->src.id & EcsSelf) == 0));

            if (isReadonly)
                throw new InvalidOperationException(
                    "The field is read-only (e.g. an In() term). Read it with GetFieldSpan, GetField(row), or GetSharedField instead of the Mut variant.");
            return;
        }
    }
}
