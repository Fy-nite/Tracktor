using Godot;
using V12.Extensions;
using V12.Interfaces;
using V12.Types;

namespace V12.Components
{
    public class TransformComponent : Component
    {
        public DoubleVector3 Position { get; set; }
        public DoubleVector3 Rotation { get; set; }
        public DoubleVector3 Scale { get; set; }
        public WorldElementSceneSyncer SceneSyncer { get; set; } = WorldManager.CurrentSyncer;
        public Node3D _slot;
        public override string Category => "Transform";

        public TransformComponent()
        {
            Position = new DoubleVector3(0, 0, 0, 1);
            Rotation = new DoubleVector3(0, 0, 0, 0);
            Scale = new DoubleVector3(1, 1, 1, 1);
            // slot will be assigned when the syncer attaches the component node
        }
        public override void OnAttach() {}
        //public override Node3D GenComponent()
        //{

        //}
        public override Control GenerateUI()
        {
            // Simple UI generation example - in practice, create controls for editing
            var ui = new Control();
            // Add UI elements here (e.g., labels, inputs)
            return ui;
        }

        // Called by the syncer when the scene node for this component is assigned.
        public void OnSceneNodeAssigned(Node? node)
        {
            _slot = node as Node3D;
            if (_slot != null)
            {
                // initialize transform from component data
                _slot.Position = new Vector3((float)Position.x, (float)Position.y, (float)Position.z);
                _slot.Rotation = new Vector3((float)Rotation.x, (float)Rotation.y, (float)Rotation.z);
                _slot.Scale = new Vector3((float)Scale.x, (float)Scale.y, (float)Scale.z);
            }
        }

        public void OnSceneNodeRemoved()
        {
            _slot = null;
        }

        // Called each tick to update the slot from component values (or vice versa).
        public void OnTick(float delta)
        {
            if (_slot == null) return;
            // push component values to the node
            _slot.Position = new Vector3((float)Position.x, (float)Position.y, (float)Position.z);
            _slot.Rotation = new Vector3((float)Rotation.x, (float)Rotation.y, (float)Rotation.z);
            _slot.Scale = new Vector3((float)Scale.x, (float)Scale.y, (float)Scale.z);
        }

       
    }
}