using Stride.Core;
using Stride.Engine;

namespace SmartIOT.Stride.Library;

[DataContract]
public class CubeComponent : EntityComponent
{
    public int CubeId { get; set; }
}
