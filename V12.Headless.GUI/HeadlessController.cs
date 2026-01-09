namespace V12.Headless.GUI;

using Godot;
public class HeadlessController
{
    public static Control GenerateUI()
    {
        Control root = new Control();
        root.Name = "HeadlessController";
        
        TabContainer TC = new TabContainer();
        TC.AddChild(GenerateTCPages());



        return root;
    }
}
