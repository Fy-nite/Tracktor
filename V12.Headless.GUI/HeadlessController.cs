namespace V12.Headless.GUI;

using Godot;
public class HeadlessController
{
    public static Control GenerateUI()
    {
        Control root = new Control();
        root.Name = "HeadlessController";
        Label label = new Label();
        label.Text = "Headless Controller Active";
        root.AddChild(label);
        return root;
    }
}
