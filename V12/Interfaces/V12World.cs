using Godot;
using V12.Interfaces;
using V12.Types;

namespace Tracktor
{
    
    public class V12World
    {
        public string Name { get; set; }
        public World3D? World3D { get; private set; }
        public Node3D? Viewport { get; private set; }
        public Element RootElement { get; private set; }
        public SceneGenerator? sceneGenerator { get; private set; }
        public List<Action> LoopCallbacks { get; set; } = new();
        public bool IsLocalOnly { get; set; } = false; // For worlds like UserSpace that aren't synced
        public bool CanBeLeft { get; set; } = true; // Whether the world can be switched away from
        public bool CanBeClosed { get; set; } = true; // Whether the world can be deleted/disposed
        public bool IsActive { get; private set; } = false;

        // If headless is true, avoid creating Godot engine objects (World3D/Node3D) so this class can be used
        // in non-Godot host processes (unit tests, headless servers).
        public V12World(string name, bool headless = false)
        {
            Name = name;
            RootElement = new Element { Name = "Root" };

            if (!headless)
            {
                World3D = new World3D();
                Viewport = new Node3D();
                sceneGenerator = new SceneGenerator(RootElement, Viewport);
            }
            else
            {
                World3D = null;
                Viewport = null;
                sceneGenerator = null;
            }
        }

        public void Activate(Node? parent = null)
        {
            if (IsActive) return;
            if (Viewport != null && parent != null && Viewport.GetParent() != parent)
            {
                parent.AddChild(Viewport);
            }
            // Register as current world
            WorldManager.SetCurrent(this);
            sceneGenerator?.RegenerateTree();
            // Make node visible and enable processing so the world becomes active
            InternalSetVisible(true);
            InternalSetProcessing(true);
            IsActive = true;
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            // Don't remove local-only worlds when deactivating; they should remain visible
            if (IsLocalOnly) return;
            // Hide and stop processing but keep node in the scene tree
            InternalSetVisible(false);
            InternalSetProcessing(false);
            IsActive = false;
            // If this world was the current, clear it
            if (WorldManager.CurrentWorld == this)
            {
                WorldManager.ClearCurrent();
            }
        }
        public static Element GenerateElementTree(Node3D node, Element parent)
        {
            // Create an Element for this node (customize as needed)
            Element element = new Element(node.Name);
            element.sceneElement = node;

            // Attach to parent
            parent.AddChild(element);

            // Recursively process children
            foreach (var child in node.GetChildren())
            {
                if (child is Node3D childNode3D)
                {
                    GenerateElementTree(childNode3D, element);
                }
            }

            return element;
        }
        // Internal helpers to toggle visibility and processing on the root viewport node.
        public void InternalSetVisible(bool visible)
        {
            try
            {
                if (Viewport != null) Viewport.Visible = visible;
            }
            catch
            {
                // If Viewport doesn't expose Visible directly, fall back to calling Show/Hide on Controls
                if (Viewport != null)
                {
                    if (visible)
                    {
                        (Viewport as Node)?.CallDeferred("show");
                    }
                    else
                    {
                        (Viewport as Node)?.CallDeferred("hide");
                    }
                }
            }
        }

        public void InternalSetProcessing(bool processing)
        {
            if (Viewport == null) return;
            Viewport.SetProcess(processing);
            Viewport.SetPhysicsProcess(processing);
            Viewport.SetProcessUnhandledInput(processing);
            Viewport.SetProcessUnhandledKeyInput(processing);
        }

        public void GenerateWorld()
        {

        }

        public void Update(double delta)
        {
            // World-specific update logic
            // Execute registered loop callbacks first
            foreach (var callback in LoopCallbacks)
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[V12World] Exception in LoopCallback: {ex.Message}");
                }
            }

            // Traverse element tree and call OnUpdate on every component
            void UpdateElementComponents(IWorldElement element)
            {
                if (element == null) return;
                foreach (var comp in element.Components)
                {
                    try
                    {
                        comp.OnUpdate(delta);
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"[V12World] Exception in component OnUpdate ({comp?.GetType().Name}): {ex.Message}");
                    }
                }
                foreach (var child in element.ChildElements)
                {
                    UpdateElementComponents(child);
                }
            }

            UpdateElementComponents(RootElement);
        }
    }
}