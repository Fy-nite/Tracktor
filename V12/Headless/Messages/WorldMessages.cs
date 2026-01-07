using System;
using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace V12.Headless.Messages
{
    public sealed class RequestWorld : IMessage<RequestWorld>
    {
        public static MessageParser<RequestWorld> Parser { get; } = new(() => new RequestWorld());

        public RequestWorld() { }

        public RequestWorld(RequestWorld other) { }
        public RequestWorld Clone() => new(this);
        public bool Equals(RequestWorld? other) => ReferenceEquals(this, other);
        public override bool Equals(object? other) => Equals(other as RequestWorld);
        public override int GetHashCode() => 1;
        public override string ToString() => "RequestWorld";
        public MessageDescriptor Descriptor => throw new NotImplementedException();
        public void WriteTo(CodedOutputStream output) { }
        public int CalculateSize() => 0;
        public void MergeFrom(RequestWorld other) { }
        public void MergeFrom(CodedInputStream input) { uint tag; while ((tag = input.ReadTag()) != 0) { input.SkipLastField(); } }
    }

    public sealed class WorldDto : IMessage<WorldDto>
    {
        public static MessageParser<WorldDto> Parser { get; } = new(() => new WorldDto());

        public string Name { get; set; } = string.Empty;
        public bool IsLocalOnly { get; set; }
        public bool CanBeLeft { get; set; }
        public bool CanBeClosed { get; set; }
        public ElementDto Root { get; set; } = new ElementDto();
        // for compatibility: allow receiving full proto types as bytes in annotations

        public WorldDto() { }
        public WorldDto(WorldDto other)
        {
            Name = other.Name;
            IsLocalOnly = other.IsLocalOnly;
            CanBeLeft = other.CanBeLeft;
            CanBeClosed = other.CanBeClosed;
            Root = other.Root != null ? other.Root.Clone() : new ElementDto();
        }

        public WorldDto Clone() => new(this);

        public override bool Equals(object? other) => Equals(other as WorldDto);
        public bool Equals(WorldDto? other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(other, this)) return true;
            return Name == other.Name && IsLocalOnly == other.IsLocalOnly && CanBeLeft == other.CanBeLeft && CanBeClosed == other.CanBeClosed && (Root?.Equals(other.Root) ?? other.Root == null);
        }

        public override int GetHashCode()
        {
            int hash = 1;
            if (Name.Length != 0) hash ^= Name.GetHashCode();
            if (IsLocalOnly) hash ^= IsLocalOnly.GetHashCode();
            if (CanBeLeft) hash ^= CanBeLeft.GetHashCode();
            if (CanBeClosed) hash ^= CanBeClosed.GetHashCode();
            if (Root != null) hash ^= Root.GetHashCode();
            return hash;
        }

        public override string ToString() => $"World(Name={Name}, Root={Root})";

        public MessageDescriptor Descriptor => throw new NotImplementedException();

        public void WriteTo(CodedOutputStream output)
        {
            if (Name.Length != 0)
            {
                output.WriteRawTag(10);
                output.WriteString(Name);
            }
            if (IsLocalOnly)
            {
                output.WriteRawTag(16);
                output.WriteBool(IsLocalOnly);
            }
            if (CanBeLeft)
            {
                output.WriteRawTag(24);
                output.WriteBool(CanBeLeft);
            }
            if (CanBeClosed)
            {
                output.WriteRawTag(32);
                output.WriteBool(CanBeClosed);
            }
            if (Root != null)
            {
                output.WriteRawTag(42);
                output.WriteMessage(Root);
            }
        }

        public int CalculateSize()
        {
            int size = 0;
            if (Name.Length != 0) size += 1 + CodedOutputStream.ComputeStringSize(Name);
            if (IsLocalOnly) size += 1 + CodedOutputStream.ComputeBoolSize(IsLocalOnly);
            if (CanBeLeft) size += 1 + CodedOutputStream.ComputeBoolSize(CanBeLeft);
            if (CanBeClosed) size += 1 + CodedOutputStream.ComputeBoolSize(CanBeClosed);
            if (Root != null) size += 1 + CodedOutputStream.ComputeMessageSize(Root);
            return size;
        }

        public void MergeFrom(WorldDto other)
        {
            if (other == null) return;
            if (other.Name.Length != 0) Name = other.Name;
            if (other.IsLocalOnly) IsLocalOnly = other.IsLocalOnly;
            if (other.CanBeLeft) CanBeLeft = other.CanBeLeft;
            if (other.CanBeClosed) CanBeClosed = other.CanBeClosed;
            if (other.Root != null) Root = other.Root.Clone();
        }

        public void MergeFrom(CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    case 10:
                        Name = input.ReadString();
                        break;
                    case 16:
                        IsLocalOnly = input.ReadBool();
                        break;
                    case 24:
                        CanBeLeft = input.ReadBool();
                        break;
                    case 32:
                        CanBeClosed = input.ReadBool();
                        break;
                    case 42:
                        if (Root == null) Root = new ElementDto();
                        input.ReadMessage(Root);
                        break;
                    default:
                        input.SkipLastField();
                        break;
                }
            }
        }
    }
}
