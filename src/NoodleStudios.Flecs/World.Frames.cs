using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

public unsafe partial struct World
{
    /// <summary>
    ///     Signal exit.
    /// </summary>
    /// <remarks>
    ///     This operation signals that the application should quit.
    ///     It will cause <see cref="Update(float)"/> to return false.
    /// </remarks>
    public void Quit()
    {
        ecs_quit(_handle);
    }

    /// <summary>
    ///     Return whether a quit has been requested.
    /// </summary>
    /// <returns>
    ///     Whether a quit has been requested.
    /// </returns>
    public bool ShouldQuit()
    {
        return ecs_should_quit(_handle);
    }

    /// <summary>
    ///     Progress a world.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This operation progresses the world by running all systems that
    ///         are both enabled and periodic on their matching entities.
    ///     </para>
    ///     <para>
    ///         An application can pass a delta_time into the function, which
    ///         is the time passed since the last frame. This value is passed
    ///         to systems so they can update entity values proportional to the
    ///         elapsed time since their last invocation.
    ///     </para>
    ///     <para>
    ///         When an application passes 0 to delta_time, ecs_progress() will
    ///         automatically measure the time passed since the last frame.
    ///         If an application does not use time management, it should pass
    ///         a non-zero value for delta_time (1.0 is recommended).
    ///         That way, no time will be wasted measuring the time.
    ///     </para>
    /// </remarks>
    /// <param name="deltaTime">
    ///     The time passed since the last frame.
    /// </param>
    /// <returns>
    ///     False if <see cref="Quit"/> has been called, true otherwise.
    /// </returns>
    public bool Update(float deltaTime)
    {
        return ecs_progress(_handle, deltaTime);
    }
}
