using Godot;
using System.Text;
using V12.Elements;
using V12.Interfaces;

namespace V12.Types
{
    public class Element : IWorldElement, IExist
    {
        // optional unique identifier used for network sync and matching
        public string Id { get; set; } = string.Empty;
        public IWorldElement ParentElement { get; set; }
        public List<IWorldElement> ChildElements { get; set; } = new();
        public event Action<IWorldElement> OnElementAdded;
        public event Action<IWorldElement> OnElementRemoved;
        public event Action<IWorldElement> OnRootChanged;
        public event Action<IComponent> OnComponentAdded;
        public event Action<IComponent> OnComponentRemoved;
        public EventManager eventManager { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public int Level { get; set; }
        public Node3D? sceneElement { get; set; }

        private DoubleVector3 _position;
        public DoubleVector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                OnElementChanged?.Invoke(this);
            }
        }
        private DoubleVector3 _rotation;
        public DoubleVector3 Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                OnElementChanged?.Invoke(this);
            }
        }
        private DoubleVector3 _scale;
        public DoubleVector3 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                OnElementChanged?.Invoke(this);
            }
        }
        public void OnChanged() { OnElementChanged?.Invoke(this); }
        public List<IComponent> Components { get; set; } = new();

        public event Action<IWorldElement> OnElementChanged;

        public Element()
        {
            eventManager = new(this);
            if (Position == null) Position = new DoubleVector3(0, 0, 0, 0);
            if (Rotation == null) Rotation = new DoubleVector3(0, 0, 0, 0);
            if (Scale == null) Scale = new DoubleVector3(1, 1, 1, 0);
        }
        public Element(string Name)
        {
            eventManager = new(this);
            this.Name = Name;
            if (Position == null) Position = new DoubleVector3(0, 0, 0, 0);
            if (Rotation == null) Rotation = new DoubleVector3(0, 0, 0, 0);
            if (Scale == null) Scale = new DoubleVector3(1, 1, 1, 0);
        }
        public Element Initialize()
        {
            foreach (var comp in Components)
            {
                comp.attachedElement = this;
                comp.OnAttach();
            }
            return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Element Name: {Name}");
            sb.AppendLine($"Parent Element: {(ParentElement != null ? ParentElement.Name : "None")}");
            sb.AppendLine($"Number of Child Elements: {ChildElements.Count}");
            return sb.ToString();
        }

        public void AddChild(IWorldElement childElement)
        {
            ChildElements.Add(childElement);
            childElement.ParentElement = this;
            // Ensure the entire subtree has correct ParentElement references
            SetParentsRecursively(childElement);
            GD.Print($"[Element] AddChildElement: Added '{childElement.Name}' to parent '{this.Name}'");
            OnElementAdded?.Invoke(childElement);
        }
        public Element GetChild(string name)
        {
            return ChildElements.FirstOrDefault(e => e.Name == name) as Element;
        }
        public Element FindChild(string name)
        {
            foreach (Element child in ChildElements)
            {
                if (child.Name == name)
                    return child as Element;
                var foundInDescendant = child.FindChild(name);
                if (foundInDescendant != null)
                    return foundInDescendant as Element;
            }
            return null;
        }
        private void SetParentsRecursively(IWorldElement node)
        {
            // Ensure components on this node have correct parent reference
            foreach (var comp in node.Components)
            {
                comp.attachedElement = node;
            }

            foreach (var child in node.ChildElements)
            {
                child.ParentElement = node;
                SetParentsRecursively(child);
            }
        }

        public IWorldElement GetChildElementByName(string name)
        {
            return ChildElements.FirstOrDefault(e => e.Name == name);
        }

        public void RemoveChildElement(IWorldElement childElement)
        {
            if (ChildElements.Remove(childElement))
            {
                childElement.ParentElement = null;
                OnElementRemoved?.Invoke(childElement);
            }
        }

        public void RemoveChildElementByName(string name)
        {
            var childElement = GetChildElementByName(name);
            if (childElement != null)
            {
                RemoveChildElement(childElement);
            }
        }

        public List<IWorldElement> GetAllDescendants()
        {
            List<IWorldElement> descendants = new List<IWorldElement>();
            foreach (var child in ChildElements)
            {
                descendants.Add(child);
                descendants.AddRange(child.GetAllDescendants());
            }
            return descendants;
        }

        public void AddComponent(IComponent component)
        {
            Components.Add(component);
            component.attachedElement = this;
            GD.Print($"[Element] AddComponent: Added component '{component.GetType().Name}' to element '{this.Name}'");
            OnComponentAdded?.Invoke(component);
            component.OnAttach();
        }

        public void RemoveComponent(IComponent component)
        {
            if (Components.Remove(component))
            {
                OnComponentRemoved?.Invoke(component);
            }
        }

        public void AddChildElement(Node3D Node)
        {
            // convert Node3D to Element and add
            Element element = new Element { Name = Node.Name };
            AddChild(element);

        }


    }
}
