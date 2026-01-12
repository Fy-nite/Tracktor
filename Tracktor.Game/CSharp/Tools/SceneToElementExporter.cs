using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Godot;
using V12.Components;
using V12.Interfaces;
using V12.Types;

namespace V12.Tools
{
    /// <summary>
    /// Converts a Godot scene (.tscn/.scn) into C# code that builds an Element tree with V12 components.
    /// Known node types are mapped to dedicated V12 components; unknown nodes fall back to GenericNodeComponent.
    /// </summary>
    public static class SceneToElementExporter
    {
        public static string ExportSceneToCode(Node rootInstance, string className = "GeneratedScene", string worldVariableName = "world")
        {
            
           
            if (rootInstance is not Node rootNode)
            {
                throw new InvalidOperationException("Scene root is not a Godot Node.");
            }

            var sb = new StringBuilder();
            sb.AppendLine("using Godot;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using V12;");
            sb.AppendLine("using V12.Interfaces;");
            sb.AppendLine("using V12.Types;");
            sb.AppendLine("using V12.Components;");
            sb.AppendLine("using V12.Components.VR;");
            sb.AppendLine();
            sb.AppendLine($"public static class {className}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static void Build(V12World {worldVariableName})");
            sb.AppendLine("    {");
            sb.AppendLine($"        if ({worldVariableName} == null) throw new System.ArgumentNullException(nameof({worldVariableName}));");
            sb.AppendLine("        var root = " + worldVariableName + ".RootElement;");
            sb.AppendLine();

            var counter = 0;
            GenerateElementCode(rootNode, "        ", "root", sb, ref counter);

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void GenerateElementCode(Node node, string indent, string parentVar, StringBuilder sb, ref int counter)
        {
            var varName = $"elem{counter++}";
            var (position, rotation, scale) = GetTransform(node);

            sb.AppendLine($"{indent}var {varName} = new Element");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    Name = \"{Escape(node.Name)}\",");
            sb.AppendLine($"{indent}    Position = new DoubleVector3({FormatDouble(position.X)}, {FormatDouble(position.Y)}, {FormatDouble(position.Z)}, 0d),");
            sb.AppendLine($"{indent}    Rotation = new DoubleVector3({FormatDouble(rotation.X)}, {FormatDouble(rotation.Y)}, {FormatDouble(rotation.Z)}, 0d),");
            sb.AppendLine($"{indent}    Scale = new DoubleVector3({FormatDouble(scale.X)}, {FormatDouble(scale.Y)}, {FormatDouble(scale.Z)}, 0d),");

            var components = BuildComponentInitializers(node, position, rotation, scale);
            if (components.Count > 0)
            {
                sb.AppendLine($"{indent}    Components = new List<IComponent>");
                sb.AppendLine($"{indent}    {{");
                for (int i = 0; i < components.Count; i++)
                {
                    var suffix = i == components.Count - 1 ? string.Empty : ",";
                    sb.AppendLine($"{indent}        {components[i]}{suffix}");
                }
                sb.AppendLine($"{indent}    }}");
            }
            else
            {
                sb.AppendLine($"{indent}    Components = new List<IComponent>()");
            }

            sb.AppendLine($"{indent}}};");
            sb.AppendLine($"{indent}{parentVar}.AddChildElement({varName});");
            sb.AppendLine();

            foreach (Node child in node.GetChildren())
            {
                GenerateElementCode(child, indent, varName, sb, ref counter);
            }
        }

        private static (Vector3 position, Vector3 rotation, Vector3 scale) GetTransform(Node node)
        {
            if (node is Node3D n3d)
            {
                return (n3d.Position, n3d.Rotation, n3d.Scale);
            }
            return (Vector3.Zero, Vector3.Zero, Vector3.One);
        }

        private static List<string> BuildComponentInitializers(Node node, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            var components = new List<string>();
            switch (node)
            {
                case CsgBox3D box:
                    components.Add(
                        $"new BoxMesh {{ Name = \"{Escape(node.Name)}\", Position = new Vector3({FormatFloat(position.X)}, {FormatFloat(position.Y)}, {FormatFloat(position.Z)}), Rotation = new Vector3({FormatFloat(rotation.X)}, {FormatFloat(rotation.Y)}, {FormatFloat(rotation.Z)}), Scale = new Vector3({FormatFloat(scale.X)}, {FormatFloat(scale.Y)}, {FormatFloat(scale.Z)}) }}");
                    break;
                case MeshInstance3D meshInstance:
                    var meshTypeName = meshInstance.Mesh != null ? $"{meshInstance.Mesh.GetType().FullName}, {meshInstance.Mesh.GetType().Assembly.GetName().Name}" : string.Empty;
                    var meshResourcePath = meshInstance.Mesh?.ResourcePath ?? string.Empty;
                    components.Add(
                        $"new MeshInstanceComponent {{ Name = \"{Escape(node.Name)}\", Position = new Vector3({FormatFloat(position.X)}, {FormatFloat(position.Y)}, {FormatFloat(position.Z)}), Rotation = new Vector3({FormatFloat(rotation.X)}, {FormatFloat(rotation.Y)}, {FormatFloat(rotation.Z)}), Scale = new Vector3({FormatFloat(scale.X)}, {FormatFloat(scale.Y)}, {FormatFloat(scale.Z)}), MeshResourcePath = \"{Escape(meshResourcePath)}\", MeshTypeName = \"{Escape(meshTypeName)}\" }}");
                    break;
                case CharacterBody3D characterBody:
                    components.Add(
                        $"new CharacterBodyComponent {{ Name = \"{Escape(node.Name)}\", Position = new Vector3({FormatFloat(position.X)}, {FormatFloat(position.Y)}, {FormatFloat(position.Z)}), Rotation = new Vector3({FormatFloat(rotation.X)}, {FormatFloat(rotation.Y)}, {FormatFloat(rotation.Z)}), Scale = new Vector3({FormatFloat(scale.X)}, {FormatFloat(scale.Y)}, {FormatFloat(scale.Z)}), FloorSnapLength = {FormatFloat(characterBody.FloorSnapLength)} }}");
                    break;
                case CollisionShape3D collisionShape:
                    var shapeTypeName = collisionShape.Shape != null ? $"{collisionShape.Shape.GetType().FullName}, {collisionShape.Shape.GetType().Assembly.GetName().Name}" : string.Empty;
                    var shapeResourcePath = collisionShape.Shape?.ResourcePath ?? string.Empty;
                    components.Add(
                        $"new CollisionShapeComponent {{ Name = \"{Escape(node.Name)}\", Position = new Vector3({FormatFloat(position.X)}, {FormatFloat(position.Y)}, {FormatFloat(position.Z)}), Rotation = new Vector3({FormatFloat(rotation.X)}, {FormatFloat(rotation.Y)}, {FormatFloat(rotation.Z)}), Scale = new Vector3({FormatFloat(scale.X)}, {FormatFloat(scale.Y)}, {FormatFloat(scale.Z)}), ShapeResourcePath = \"{Escape(shapeResourcePath)}\", ShapeTypeName = \"{Escape(shapeTypeName)}\" }}");
                    break;
                case RayCast3D rayCast:
                    components.Add(
                        $"new RayCastComponent {{ Name = \"{Escape(node.Name)}\", Position = new Vector3({FormatFloat(position.X)}, {FormatFloat(position.Y)}, {FormatFloat(position.Z)}), Rotation = new Vector3({FormatFloat(rotation.X)}, {FormatFloat(rotation.Y)}, {FormatFloat(rotation.Z)}), Scale = new Vector3({FormatFloat(scale.X)}, {FormatFloat(scale.Y)}, {FormatFloat(scale.Z)}), TargetPosition = new Vector3({FormatFloat(rayCast.TargetPosition.X)}, {FormatFloat(rayCast.TargetPosition.Y)}, {FormatFloat(rayCast.TargetPosition.Z)}), Enabled = {FormatBool(rayCast.Enabled)}, CollideWithAreas = {FormatBool(rayCast.CollideWithAreas)}, CollideWithBodies = {FormatBool(rayCast.CollideWithBodies)} }}");
                    break;
                case XROrigin3D:
                    components.Add(
                        $"new VROriginComponent {{ Name = \"{Escape(node.Name)}\" }}");
                    break;
                case XRCamera3D:
                    components.Add(
                        $"new VRCameraComponent {{ Name = \"{Escape(node.Name)}\" }}");
                    break;
                case XRController3D controller:
                    var tracker = controller.Tracker;
                    var nodeName = node.Name.ToString();
                    var isLeft = tracker == new StringName("left_hand") || tracker == new StringName("left") || nodeName.Contains("Left") || nodeName.Contains("left");
                    components.Add(
                        $"new VRControllerComponent {{ Name = \"{Escape(node.Name)}\", Hand = {FormatBool(isLeft)} }}");
                    break;
                case global::Godot.SubViewport:
                    components.Add(
                        $"new V12.Components.SubViewport {{ Name = \"{Escape(node.Name)}\" }}");
                    break;
                case DirectionalLight3D light:
                    components.Add(
                        $"new DirectionalLight {{ Name = \"{Escape(node.Name)}\", Position = new Vector3({FormatFloat(position.X)}, {FormatFloat(position.Y)}, {FormatFloat(position.Z)}), Rotation = new Vector3({FormatFloat(rotation.X)}, {FormatFloat(rotation.Y)}, {FormatFloat(rotation.Z)}), Scale = new Vector3({FormatFloat(scale.X)}, {FormatFloat(scale.Y)}, {FormatFloat(scale.Z)}), Energy = {FormatFloat(light.LightEnergy)}, Color = new Color({FormatFloat(light.LightColor.R)}, {FormatFloat(light.LightColor.G)}, {FormatFloat(light.LightColor.B)}, {FormatFloat(light.LightColor.A)}) }}");
                    break;
                default:
                    var typeName = $"{node.GetType().FullName}, {node.GetType().Assembly.GetName().Name}";
                    components.Add(
                        $"new GenericNodeComponent {{ Name = \"{Escape(node.Name)}\", NodeTypeName = \"{Escape(typeName)}\", Position = new Vector3({FormatFloat(position.X)}, {FormatFloat(position.Y)}, {FormatFloat(position.Z)}), Rotation = new Vector3({FormatFloat(rotation.X)}, {FormatFloat(rotation.Y)}, {FormatFloat(rotation.Z)}), Scale = new Vector3({FormatFloat(scale.X)}, {FormatFloat(scale.Y)}, {FormatFloat(scale.Z)}) }}");
                    break;
            }
            return components;
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture) + "d";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture) + "f";
        }

        private static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
