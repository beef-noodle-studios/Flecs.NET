using static Flecs.NET.Bindings.flecs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleStudios.Flecs.Core;

/// <summary>
///     Class containing static wrapper methods that call directly into the
///     C Flecs Api.
/// </summary>
public static class Ecs
{
    /// <summary>
    ///     Make a pair ID.
    /// </summary>
    /// <param name="relationship">
    ///     The first element of the pair.
    /// </param>
    /// <param name="target">
    ///     The target of the pair.
    /// </param>
    /// <returns>
    ///     A pair ID.
    /// </returns>
    public static Id MakePair(Id relationship, Id target)
    {
        var pairId = ecs_make_pair(relationship, target);
        return pairId;
    }
}
