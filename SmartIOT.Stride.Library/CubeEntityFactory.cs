using SmartIOT.Stride.Extensions;
using Stride.Core;
using Stride.Engine;
using System;

namespace SmartIOT.Stride.Library;

[DataContract]
public class CubeEntityFactory : EntityComponent
{
    public Prefab? Prefab { get; set; }

    public Entity CreateCubeEntity(int cubeId)
    {
        var entity = Prefab!.InstantiateWithRootEntity();

        var cube = entity.RecursiveGet<CubeComponent>() ?? throw new InvalidOperationException($"{typeof(CubeComponent)} not found in entity");
        cube.CubeId = cubeId;

        return entity;
    }
}