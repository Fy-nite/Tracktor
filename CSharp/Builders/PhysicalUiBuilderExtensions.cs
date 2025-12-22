#nullable enable
using System;
using Trakction.PhysicalUI;

namespace Trakction.Builders;

public static class PhysicalUiBuilderExtensions
{
    public static NodeBuilder<TParent> AddPhysicalButton3D<TParent>(
        this NodeBuilder<TParent> parent,
        string name,
        Action<NodeBuilder<PhysicalButton3D>>? configure = null)
        where TParent : Godot.Node
        => parent.AddChild(name, configure);

    public static NodeBuilder<TParent> AddPhysicalScrollBox3D<TParent>(
        this NodeBuilder<TParent> parent,
        string name,
        Action<NodeBuilder<PhysicalScrollBox3D>>? configure = null)
        where TParent : Godot.Node
        => parent.AddChild(name, configure);

    public static NodeBuilder<TParent> AddPhysicalTextInput3D<TParent>(
        this NodeBuilder<TParent> parent,
        string name,
        Action<NodeBuilder<PhysicalTextInput3D>>? configure = null)
        where TParent : Godot.Node
        => parent.AddChild(name, configure);
}
