using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleStudios.Flecs.Facades;

public unsafe readonly ref struct FluentHasApi(ReadOnlyWorld world, Entity entity)
{
    private readonly ReadOnlyWorld _world = world;
    private readonly Entity _entity = entity;


}
