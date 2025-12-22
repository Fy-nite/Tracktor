#nullable enable
using System;
using Trakction.Interaction;
using Trakction.PhysicalUI;

namespace Trakction.Builders;

public static class InteractionBuilderExtensions
{
    public static NodeBuilder<PhysicalButton3D> WithText(this NodeBuilder<PhysicalButton3D> b, string text)
        => b.Configure(btn => btn.Text = text);

    public static NodeBuilder<PhysicalButton3D> OnPressed(this NodeBuilder<PhysicalButton3D> b, Action<InteractionContext> handler)
        => b.Configure(btn => btn.OnPressed = handler);

    public static NodeBuilder<PhysicalScrollBox3D> OnScrolled(this NodeBuilder<PhysicalScrollBox3D> b, Action<InteractionContext, float> handler)
        => b.Configure(sb => sb.OnScrolled = handler);

    public static NodeBuilder<PhysicalTextInput3D> WithText(this NodeBuilder<PhysicalTextInput3D> b, string text)
        => b.Configure(ti => ti.Text = text);

    public static NodeBuilder<PhysicalTextInput3D> OnActivated(this NodeBuilder<PhysicalTextInput3D> b, Action<InteractionContext> handler)
        => b.Configure(ti => ti.OnActivated = handler);

    public static NodeBuilder<PhysicalTextInput3D> OnSubmitted(this NodeBuilder<PhysicalTextInput3D> b, Action<InteractionContext, string> handler)
        => b.Configure(ti => ti.OnSubmitted = handler);
}
