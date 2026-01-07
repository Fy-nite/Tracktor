using System.Collections.Generic;
using V12.Interfaces;
using V12.Headless;
using Google.Protobuf;
using V12.Types;
using System;

namespace V12.Types.Models
{
    public static class WorldProtoMapper
    {
        // Produce the existing generated ElementDto (minimal) from an IWorldElement.
        // This uses the ElementDto type already generated in V12.Headless from chat.proto
        // and maps the basic fields (name, description, type, level) and children.
        public static ElementDto ToProto(this IWorldElement element)
        {
            if (element == null) return new ElementDto();
            var dto = new ElementDto
            {
                Name = element.Name ?? string.Empty,
                Description = element.Description ?? string.Empty,
                Type = element.Type ?? string.Empty,
                Level = element.Level
            };

            // ElementDto (from chat.proto) is minimal and doesn't include children.
            // For now we only map the single element's basic fields.
            return dto;
        }

        // Return serialized protobuf bytes for the world's root element (ElementDto)
        public static byte[] ToProtoBytes(this V12.V12World world)
        {
            if (world == null || world.RootElement == null) return Array.Empty<byte>();
            return world.RootElement.ToProto().ToByteArray();
        }

        // Optionally return JSON diagnostic string for the root element
        public static string ToProtoJson(this V12.V12World world)
        {
            if (world == null || world.RootElement == null) return string.Empty;
            return Google.Protobuf.JsonFormatter.Default.Format(world.RootElement.ToProto());
        }

        // Full proto mapping (engine -> proto)
        public static FullElementDto ToFullProto(this IWorldElement element)
        {
            if (element == null) return new FullElementDto();
            var dto = new FullElementDto
            {
                Name = element.Name ?? string.Empty,
                Description = element.Description ?? string.Empty,
                Type = element.Type ?? string.Empty,
                Level = element.Level,
                Id = element is Element e && !string.IsNullOrEmpty(e.Id) ? e.Id : System.Guid.NewGuid().ToString(),
                Position = new Vector4D { X = element.Position.x, Y = element.Position.y, Z = element.Position.z, W = element.Position.w },
                Rotation = new Vector4D { X = element.Rotation.x, Y = element.Rotation.y, Z = element.Rotation.z, W = element.Rotation.w },
                Scale = new Vector4D { X = element.Scale.x, Y = element.Scale.y, Z = element.Scale.z, W = element.Scale.w }
            };

            foreach (var comp in element.Components ?? new())
            {
                dto.Components.Add(comp.ToFullProto());
            }

            foreach (var child in element.ChildElements ?? new())
            {
                dto.Children.Add(child.ToFullProto());
            }

            return dto;
        }

        public static ComponentProto ToFullProto(this IComponent comp)
        {
            if (comp == null) return new ComponentProto();
            var dto = new ComponentProto
            {
                Name = comp.Name ?? string.Empty,
                Description = comp.Description ?? string.Empty,
                Category = comp.Category ?? string.Empty
            };
            foreach (var addon in comp.Addons ?? new()) dto.Addons.Add(addon.ToFullProto());
            return dto;
        }

        public static WorldProto ToFullProto(this V12.V12World world)
        {
            if (world == null) return new WorldProto();
            return new WorldProto
            {
                Name = world.Name ?? string.Empty,
                IsLocalOnly = world.IsLocalOnly,
                CanBeLeft = world.CanBeLeft,
                CanBeClosed = world.CanBeClosed,
                Root = world.RootElement.ToFullProto()
            };
        }

        // Proto -> Engine mapping
        public static V12.Types.DoubleVector3 ToDoubleVector(this Vector4D v)
        {
            if (v == null) return new V12.Types.DoubleVector3(0,0,0,0);
            return new V12.Types.DoubleVector3(v.X, v.Y, v.Z, v.W);
        }

        public static Element ToElement(this FullElementDto dto)
        {
            var el = new Element(dto.Name)
            {
                Description = dto.Description,
                Type = dto.Type,
                Level = dto.Level,
                Position = dto.Position != null ? dto.Position.ToDoubleVector() : new V12.Types.DoubleVector3(0,0,0,0),
                Rotation = dto.Rotation != null ? dto.Rotation.ToDoubleVector() : new V12.Types.DoubleVector3(0,0,0,0),
                Scale = dto.Scale != null ? dto.Scale.ToDoubleVector() : new V12.Types.DoubleVector3(1,1,1,0)
            };

            // apply id
            if (!string.IsNullOrEmpty(dto.Id))
            {
                el.Id = dto.Id;
            }

            // components
            foreach (var c in dto.Components)
            {
                var nc = new V12.Headless.Network.NetworkComponent
                {
                    Name = c.Name,
                    Description = c.Description,
                    Category = c.Category
                };
                el.AddComponent(nc);
            }

            // children
            foreach (var child in dto.Children)
            {
                var childEl = child.ToElement();
                el.AddChild(childEl);
            }

            // ensure components' attachedElement set
            foreach (var c in el.Components)
            {
                c.attachedElement = el;
            }

            return el;
        }
    }
}
