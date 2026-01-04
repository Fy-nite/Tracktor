using V12.Elements;
using V12.Types;

namespace V12.Interfaces
{
    public interface IWorldElement
    {
        IWorldElement ParentElement { get; set; }
        List<IWorldElement> ChildElements { get; set; }
        event Action<IWorldElement> OnElementAdded;
        event Action<IWorldElement> OnElementRemoved;
        event Action<IWorldElement> OnRootChanged;
        event Action<IWorldElement> OnElementChanged;
        event Action<IComponent> OnComponentAdded;
        event Action<IComponent> OnComponentRemoved;
        public EventManager eventManager { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        string Type { get; set; }
        int Level { get; set; }
        DoubleVector3 Position { get; set; }
        DoubleVector3 Rotation { get; set; }
        DoubleVector3 Scale { get; set; }
        List<IComponent> Components { get; set; }

        public void OnChanged();
        void AddChildElement(IWorldElement childElement);
        IWorldElement GetChildElementByName(string name);
        void RemoveChildElement(IWorldElement childElement);
        void RemoveChildElementByName(string name);
        List<IWorldElement> GetAllDescendants();
        void AddComponent(IComponent component);
        void RemoveComponent(IComponent component);
    }
}
