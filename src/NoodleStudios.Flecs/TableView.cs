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
    ///     Use <see cref="GetField{T}(int)"/> for the shape-agnostic path.
    /// </remarks>
    public Span<T> GetFieldSpan<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        return FieldSpan<T>(idx, isSparse, isShared, hasData);
    }

    /// <inheritdoc cref="GetFieldSpan{T}()"/>
    public Span<T> GetFieldSpan<T>(Id id) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        return FieldSpan<T>(idx, isSparse, isShared, hasData);
    }

    /// <summary>
    ///     Get the owned column of component <typeparamref name="T"/> if this table
    ///     has it with data, for example an optional term that matches this table.
    /// </summary>
    /// <returns>
    ///     True and the span if the field is present with data on this table;
    ///     otherwise false and an empty span. A sparse or shared field, or a wrong
    ///     <typeparamref name="T"/> for a present field, is misuse that throws in Debug 
    ///     and is undefined behavior in Release. 
    /// </returns>
    public bool TryGetFieldSpan<T>(out Span<T> span) where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        return TryFieldSpan<T>(idx, isSparse, isShared, hasData, out span);
    }

    /// <inheritdoc cref="TryGetFieldSpan{T}(out Span{T})"/>
    public bool TryGetFieldSpan<T>(Id id, out Span<T> span) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        return TryFieldSpan<T>(idx, isSparse, isShared, hasData, out span);
    }

    // --- Universal per-row accessor ---

    /// <summary>
    ///     Get a reference to component <typeparamref name="T"/> for a single row,
    ///     dispatching on the field's storage shape: owned reads the row, sparse
    ///     reads it out of the sparse set, and shared reads the single inherited
    ///     value. 
    /// </summary>
    /// <remarks>
    ///     Writing through a shared field mutates the base entity, and therefore
    ///     every entity that inherits the value. The field must be selected by the
    ///     query, carry data, and match <typeparamref name="T"/>'s size, and
    ///     <paramref name="row"/> must be in <c>[0, Count)</c>. Otherwise this throws
    ///     in Debug and is undefined behavior in Release.
    /// </remarks>
    public ref T GetField<T>(int row) where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        return ref Field<T>(idx, isSparse, isShared, hasData, row);
    }

    /// <inheritdoc cref="GetField{T}(int)"/>
    public ref T GetField<T>(Id id, int row) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        return ref Field<T>(idx, isSparse, isShared, hasData, row);
    }

    // --- Shared (inherited) singleton ---

    /// <summary>
    ///     Get a reference to the single shared value of component
    ///     <typeparamref name="T"/> for this table as inherited from a base entity. 
    ///     One value backs the whole table.
    /// </summary>
    /// <remarks>
    ///     Only valid when the field is shared on this table. An owned or sparse
    ///     field, a field with no data, or a size mismatch is misuse (Debug throw /
    ///     Release UB). Use <see cref="IsFieldShared{T}()"/> to test the shape
    ///     first when a query can match both owned and inherited tables.
    /// </remarks>
    public ref T GetSharedField<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out bool isShared, out bool hasData);
        return ref SharedField<T>(idx, isSparse, isShared, hasData);
    }

    /// <inheritdoc cref="GetSharedField{T}()"/>
    public ref T GetSharedField<T>(Id id) where T : unmanaged
    {
        int idx = Resolve(id, out bool isSparse, out bool isShared, out bool hasData);
        return ref SharedField<T>(idx, isSparse, isShared, hasData);
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
    ///     Test whether component <typeparamref name="T"/> is shared (inherited from
    ///     a base) on this table rather than owned per row. Returns false for a
    ///     field the query does not select.
    /// </summary>
    public bool IsFieldShared<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out _, out bool isShared, out _);
        return idx >= 0 && isShared;
    }

    /// <inheritdoc cref="IsFieldShared{T}()"/>
    public bool IsFieldShared(Id id)
    {
        int idx = Resolve(id, out _, out bool isShared, out _);
        return idx >= 0 && isShared;
    }

    /// <summary>
    ///     Test whether component <typeparamref name="T"/> is stored in a sparse set
    ///     on this table rather than in the table itself. Returns false for a field
    ///     the query does not select.
    /// </summary>
    public bool IsFieldSparse<T>() where T : unmanaged
    {
        int idx = Resolve<T>(out bool isSparse, out _, out _);
        return idx >= 0 && isSparse;
    }

    /// <inheritdoc cref="IsFieldSparse{T}()"/>
    public bool IsFieldSparse(Id id)
    {
        int idx = Resolve(id, out bool isSparse, out _, out _);
        return idx >= 0 && isSparse;
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
    ///     Scan the table's matched ids for the field index. On a miss return -1 with 
    ///     everything false, computing nothing further so that no native field call 
    ///     ever receives an invalid index (a managed -1 would marshal to byte 255 and 
    ///     read out of bounds). Shape is only meaningful for a found field.
    /// </summary>
    private int Resolve(Id id, out bool isSparse, out bool isShared, out bool hasData)
    {
        isSparse = false;
        isShared = false;
        hasData = false;

        int fieldCount = _it->field_count;
        ulong target = id.Value;
        int idx = -1;
        for (int i = 0; i < fieldCount; i++)
        {
            if (_it->ids[i] == target)
            {
                idx = i;
                break;
            }
        }

        if (idx < 0)
            return -1;

        var bidx = (byte)idx;
        uint mask = 1u << idx;
        isSparse = (_it->row_fields & mask) != 0;
        isShared = !ecs_field_is_self(_it, bidx);

        hasData = (_it->query->data_fields & mask) != 0 && ecs_field_is_set(_it, bidx);
        return idx;
    }

    private Span<T> FieldSpan<T>(int idx, bool isSparse, bool isShared, bool hasData)
        where T : unmanaged
    {
        DbgIsField(idx);
        DbgOwned(isSparse, isShared);
        DbgHasData(hasData);
        DbgSize<T>(idx);
        return new Span<T>(ecs_field_w_size(_it, sizeof(T), (byte)idx), _it->count);
    }

    private bool TryFieldSpan<T>(int idx, bool isSparse, bool isShared, bool hasData, out Span<T> span)
        where T : unmanaged
    {
        DbgIsField(idx);
        DbgOwned(isSparse, isShared);
        if (!hasData)
        {
            span = default;
            return false;
        }

        DbgSize<T>(idx);
        span = new Span<T>(ecs_field_w_size(_it, sizeof(T), (byte)idx), _it->count);
        return true;
    }

    private ref T Field<T>(int idx, bool isSparse, bool isShared, bool hasData, int row)
        where T : unmanaged
    {
        DbgIsField(idx);
        DbgRow(row);
        DbgHasData(hasData);
        DbgSize<T>(idx);

        if (isSparse)
            return ref *(T*)ecs_field_at_w_size(_it, sizeof(T), (byte)idx, row);

        var ptr = (T*)ecs_field_w_size(_it, sizeof(T), (byte)idx);
        return ref (isShared ? ref ptr[0] : ref ptr[row]);
    }

    private ref T SharedField<T>(int idx, bool isSparse, bool isShared, bool hasData)
        where T : unmanaged
    {
        DbgIsField(idx);
        DbgShared(isShared, isSparse);
        DbgHasData(hasData);
        DbgSize<T>(idx);
        return ref ((T*)ecs_field_w_size(_it, sizeof(T), (byte)idx))[0];
    }

    [Conditional("DEBUG")]
    private static void DbgIsField(int idx)
    {
        if (idx < 0)
            throw new InvalidOperationException("The field is not selected by this query.");
    }

    [Conditional("DEBUG")]
    private static void DbgOwned(bool isSparse, bool isShared)
    {
        if (isSparse)
            throw new InvalidOperationException(
                "The field is sparse. Read it per row with GetField(row).");
        if (isShared)
            throw new InvalidOperationException(
                "The field is shared on this table. Read it with GetSharedField or per row with GetField(row).");
    }

    [Conditional("DEBUG")]
    private static void DbgShared(bool isShared, bool isSparse)
    {
        if (!isShared)
            throw new InvalidOperationException(
                "The field is not shared on this table. Use GetFieldSpan or GetField(row).");
        if (isSparse)
            throw new InvalidOperationException(
                "The field is sparse. Read it per row with GetField(row).");
    }

    [Conditional("DEBUG")]
    private static void DbgHasData(bool hasData)
    {
        if (!hasData)
            throw new InvalidOperationException(
                "The field has no data on this table (a tag, an unmatched optional, or a None()/excluded term).");
    }

    [Conditional("DEBUG")]
    private void DbgRow(int row)
    {
        if ((uint)row >= (uint)_it->count)
            throw new InvalidOperationException(
                $"Row {row} is out of range for a table with {_it->count} rows.");
    }

    [Conditional("DEBUG")]
    private void DbgSize<T>(int idx) where T : unmanaged
    {
        int actual = (int)ecs_field_size(_it, (byte)idx);
        if (sizeof(T) != actual)
            throw new InvalidOperationException(
                $"Component type '{typeof(T).Name}' is {sizeof(T)} bytes but the field stores {actual} bytes.");
    }
}
