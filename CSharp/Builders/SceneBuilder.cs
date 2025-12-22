#nullable enable
using Godot;
using System;

namespace Trakction.Builders;

/// <summary>
/// Small, mod-friendly scene graph builder.
/// This is intentionally minimal: it creates Nodes, configures them, and parents them.
/// </summary>
public sealed class SceneBuilder
{
    private Node? _root;

    public Node RootNode
        => _root ?? throw new InvalidOperationException("SceneBuilder: Root() must be called before Build().");

    public NodeBuilder<T> Root<T>(string name, Action<NodeBuilder<T>>? configure = null)
        where T : Node, new()
    {
        var node = new T { Name = name };
        _root = node;

        var builder = new NodeBuilder<T>(node);
        configure?.Invoke(builder);
        return builder;
    }

    public Node Build()
        => RootNode;
}
