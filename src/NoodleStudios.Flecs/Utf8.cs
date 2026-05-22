using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace NoodleStudios.Flecs;

/// <summary>
///     Helpers for converting between managed strings and the null-terminated
///     UTF-8 <c>byte*</c> strings the native Flecs API uses.
/// </summary>
/// <remarks>
///     The bindings assembly disables runtime marshalling, so string conversion
///     must be performed explicitly. These helpers are used by cold paths only
///     (naming, lookup, component registration), so they favour clarity over
///     avoiding the per-call allocation.
/// </remarks>
internal static unsafe class Utf8
{
    /// <summary>
    ///     Convert a managed string to a newly allocated null-terminated UTF-8
    ///     buffer. The result must be released with <see cref="Free"/>.
    /// </summary>
    /// <returns>
    ///     A pointer to the UTF-8 buffer, or null if <paramref name="value"/> is
    ///     null.
    /// </returns>
    public static byte* Encode(string? value) => Utf8StringMarshaller.ConvertToUnmanaged(value);

    /// <summary>
    ///     Release a buffer returned by <see cref="Encode"/>.
    /// </summary>
    public static void Free(byte* ptr) => Utf8StringMarshaller.Free(ptr);

    /// <summary>
    ///     Convert a null-terminated UTF-8 buffer owned by Flecs into a managed
    ///     string. The buffer is not freed.
    /// </summary>
    /// <returns>
    ///     The decoded string, or null if <paramref name="ptr"/> is null.
    /// </returns>
    public static string? Decode(byte* ptr) => Marshal.PtrToStringUTF8((nint)ptr);
}
