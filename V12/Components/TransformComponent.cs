using Godot;
using V12.Components;
using V12.Types;

namespace V12.Components
{
    public class TransformComponent : Component
    {
        public DoubleVector3 Position { get; set; }
        public DoubleVector3 Rotation { get; set; }
        public DoubleVector3 Scale { get; set; }

        public override string Category => "Transform";

        public TransformComponent() : base("Transform", "Handles position, rotation, and scale")
        {
            Position = new DoubleVector3(0, 0, 0, 1);
            Rotation = new DoubleVector3(0, 0, 0, 0);
            Scale = new DoubleVector3(1, 1, 1, 1);
        }
        public override Node3D RunComponentLogic()
        {
            throw new NotImplementedException("Transform component doesn't have logic yet");
        }
        public override Control GenerateUI()
        {
            // Simple UI generation example - in practice, create controls for editing
            var ui = new Control();
            // Add UI elements here (e.g., labels, inputs)
            return ui;
        }
    }
}