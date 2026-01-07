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

            // components - try to create concrete component instances when possible so clients can reconstruct behavior
            foreach (var c in dto.Components)
            {
                IComponent created = CreateComponentFromProto(c, dto);
                if (created != null)
                {
                    el.AddComponent(created);
                }
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

        // Attempt to instantiate concrete component types by name using reflection.
        // Falls back to Network.NetworkComponent when unknown.
        private static IComponent CreateComponentFromProto(ComponentProto c, FullElementDto parent)
        {
            try
            {
                var compName = (c.Name ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(compName))
                {
                    return new V12.Headless.Network.NetworkComponent { Name = string.Empty, Description = c.Description, Category = c.Category };
                }

                // Normalize simple names (e.g. "DesktopPlayer") and try to find a matching type
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Type? found = null;

                // Try full-name match first
                foreach (var asm in assemblies)
                {
                    try
                    {
                        var t = asm.GetType(compName, false, true);
                        if (t != null && typeof(IComponent).IsAssignableFrom(t)) { found = t; break; }
                    }
                    catch { }
                }

                // If not found by full name, try matching by short type name within loaded assemblies
                if (found == null)
                {
                    var shortName = compName.Contains('.') ? compName.Substring(compName.LastIndexOf('.') + 1) : compName;
                    foreach (var asm in assemblies)
                    {
                        Type[] types;
                        try { types = asm.GetTypes(); } catch { continue; }
                        foreach (var t in types)
                        {
                            if (!typeof(IComponent).IsAssignableFrom(t)) continue;
                            if (string.Equals(t.Name, shortName, StringComparison.OrdinalIgnoreCase))
                            {
                                found = t; break;
                            }
                            // also allow matching on full name endings like "V12.Components.DesktopPlayer"
                            if (t.FullName != null && t.FullName.EndsWith("." + shortName, StringComparison.OrdinalIgnoreCase))
                            {
                                found = t; break;
                            }
                        }
                        if (found != null) break;
                    }
                }

                if (found != null)
                {
                    // instantiate
                    var instance = Activator.CreateInstance(found) as IComponent;
                    if (instance != null)
                    {
                        // set common properties if available
                        try
                        {
                            var pd = found.GetProperty("Description");
                            if (pd != null && pd.CanWrite) pd.SetValue(instance, c.Description);
                        }
                        catch { }
                        try
                        {
                            var pc = found.GetProperty("Category");
                            if (pc != null && pc.CanWrite) pc.SetValue(instance, c.Category);
                        }
                        catch { }

                        // handle addons recursively
                        try
                        {
                            foreach (var addonProto in c.Addons)
                            {
                                var child = CreateComponentFromProto(addonProto, parent);
                                if (child != null) instance.AddChildAddon(child);
                            }
                        }
                        catch { }

                        return instance;
                    }
                }

                // fallback to generic network component
                return new V12.Headless.Network.NetworkComponent { Name = c.Name ?? string.Empty, Description = c.Description, Category = c.Category };
            }
            catch
            {
                return new V12.Headless.Network.NetworkComponent { Name = c.Name ?? string.Empty, Description = c.Description, Category = c.Category };
            }
        }
    }
}
