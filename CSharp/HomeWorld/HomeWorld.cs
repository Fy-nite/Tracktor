using Godot;
using Traktor.Core.Assets.Materials;

namespace traktion.HomeWorld;
using Trakction.Builders;
using Traktion;

public partial class HomeWorld : Node3D
{
    public override void _Ready()
    {
        var world = GenerateWorld();
        world.AddToGroup("net_sync");
        AddChild(world);
        AddToGroup("net_sync");
    }
    public static Node GenerateWorld()
    {
        var sb = new SceneBuilder();
        sb.Root<Node3D>("MyRoot", r =>
        {
            r.AddChild<CsgBox3D>("BOX", b =>
            {
                b.Configure(mi =>
                {
                //     mi.Material = TemplateMaterial.CreateTemplateMaterial();
                    mi.Position = new Vector3(0, 0, 0);
                    mi.Scale = new Vector3(5, 0.1f, 5);
                    
                });
                b.Configure(mi => mi.SetMeta("net_factory", "CsgBox3D"));
            });
        });

        return sb.Build();
    }
}
