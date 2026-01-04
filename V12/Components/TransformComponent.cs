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
        public WorldElementSceneSyncer? SceneSyncer { get; set; } = WorldManager.CurrentSyncer;
        public override string Category => "Transform";

        public TransformComponent()
        {
            Position = new DoubleVector3(0, 0, 0, 0);
            Rotation = new DoubleVector3(0, 0, 0, 0);
            Scale = new DoubleVector3(1, 1, 1, 1);
            // slot will be assigned when the syncer attaches the component node
        }
        public override void OnAttach() {}
        public override Control GenerateUI()
        {
            return ComponentUIGenerator.GenerateAutoUI(this);
        }

        // Called each tick to update the slot from component values (or vice versa).
        public void OnUpdate(float delta)
        {
            if (attachedElement == null) return;
            attachedElement.sceneElement.Position = new Vector3((float)Position.x, (float)Position.y, (float)Position.z);
            attachedElement.sceneElement.Rotation = new Vector3((float)Rotation.x, (float)Rotation.y, (float)Rotation.z);
            attachedElement.sceneElement.Scale = new Vector3((float)Scale.x, (float)Scale.y, (float)Scale.z);
        }
        // Called each update to sync the slot from component values (or vice versa).
        // public override void OnUpdate()
        // {
        //     if (_slot == null) return;
        //     // push component values to the node
        //     _slot.Position = new Vector3((float)Position.x, (float)Position.y, (float)Position.z);
        //     _slot.Rotation = new Vector3((float)Rotation.x, (float)Rotation.y, (float)Rotation.z);
        //     _slot.Scale = new Vector3((float)Scale.x, (float)Scale.y, (float)Scale.z);
        // }

       
    }
}