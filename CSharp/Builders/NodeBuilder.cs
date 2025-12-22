#nullable enable
using Godot;
using System;

namespace Trakction.Builders;

public sealed class NodeBuilder<T> where T : Node
{
    public T Node { get; }

    public NodeBuilder(T node)
    {
        Node = node;
    }

    public NodeBuilder<T> Configure(Action<T> configure)
    {
        configure(Node);
        return this;
    }

    public NodeBuilder<T> AddChild<TChild>(string name, Action<NodeBuilder<TChild>>? configure = null)
        where TChild : Node, new()
    {
        var child = new TChild { Name = name };
        Node.AddChild(child);

        var builder = new NodeBuilder<TChild>(child);
        configure?.Invoke(builder);
        return this;
    }

    public NodeBuilder<TChild> AddChildBuilder<TChild>(string name, Action<NodeBuilder<TChild>>? configure = null)
        where TChild : Node, new()
    {
        var child = new TChild { Name = name };
        Node.AddChild(child);

        var builder = new NodeBuilder<TChild>(child);
        configure?.Invoke(builder);
        return builder;
    }
}
