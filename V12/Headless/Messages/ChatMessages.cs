using System;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace V12.Headless.Messages
{
    // Minimal hand-written protobuf messages matching chat.proto
    public sealed class ChatMessage : IMessage<ChatMessage>
    {
        public static MessageParser<ChatMessage> Parser { get; } = new(() => new ChatMessage());

        public string User { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public ChatMessage()
        {
        }

        public ChatMessage(ChatMessage other)
        {
            User = other.User;
            Text = other.Text;
            Timestamp = other.Timestamp;
        }

        public ChatMessage Clone() => new(this);

        public override bool Equals(object? other) => Equals(other as ChatMessage);

        public bool Equals(ChatMessage? other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(other, this)) return true;
            return User == other.User && Text == other.Text && Timestamp == other.Timestamp;
        }

        public override int GetHashCode()
        {
            int hash = 1;
            if (User.Length != 0) hash ^= User.GetHashCode();
            if (Text.Length != 0) hash ^= Text.GetHashCode();
            if (Timestamp != 0L) hash ^= Timestamp.GetHashCode();
            return hash;
        }

        public override string ToString() => $"{User}: {Text} ({Timestamp})";

        public MessageDescriptor Descriptor => throw new NotImplementedException();

        public void WriteTo(CodedOutputStream output)
        {
            if (User.Length != 0)
            {
                output.WriteRawTag(10);
                output.WriteString(User);
            }
            if (Text.Length != 0)
            {
                output.WriteRawTag(18);
                output.WriteString(Text);
            }
            if (Timestamp != 0L)
            {
                output.WriteRawTag(24);
                output.WriteInt64(Timestamp);
            }
        }

        public int CalculateSize()
        {
            int size = 0;
            if (User.Length != 0)
            {
                size += 1 + CodedOutputStream.ComputeStringSize(User);
            }
            if (Text.Length != 0)
            {
                size += 1 + CodedOutputStream.ComputeStringSize(Text);
            }
            if (Timestamp != 0L)
            {
                size += 1 + CodedOutputStream.ComputeInt64Size(Timestamp);
            }
            return size;
        }

        public void MergeFrom(ChatMessage other)
        {
            if (other == null)
            {
                return;
            }
            if (other.User.Length != 0)
            {
                User = other.User;
            }
            if (other.Text.Length != 0)
            {
                Text = other.Text;
            }
            if (other.Timestamp != 0L)
            {
                Timestamp = other.Timestamp;
            }
        }

        public void MergeFrom(CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    case 10:
                        User = input.ReadString();
                        break;
                    case 18:
                        Text = input.ReadString();
                        break;
                    case 24:
                        Timestamp = input.ReadInt64();
                        break;
                    default:
                        input.SkipLastField();
                        break;
                }
            }
        }

        MessageDescriptor IMessage.Descriptor => throw new NotImplementedException();
    }

    public sealed class ElementDto : IMessage<ElementDto>
    {
        public static MessageParser<ElementDto> Parser { get; } = new(() => new ElementDto());

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Level { get; set; }
        public ElementDto()
        {
        }

        public ElementDto(ElementDto other)
        {
            Name = other.Name;
            Description = other.Description;
            Type = other.Type;
            Level = other.Level;
        }

        public ElementDto Clone() => new(this);

        public override bool Equals(object? other) => Equals(other as ElementDto);

        public bool Equals(ElementDto? other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(other, this)) return true;
            return Name == other.Name && Description == other.Description && Type == other.Type && Level == other.Level;
        }

        public override int GetHashCode()
        {
            int hash = 1;
            if (Name.Length != 0) hash ^= Name.GetHashCode();
            if (Description.Length != 0) hash ^= Description.GetHashCode();
            if (Type.Length != 0) hash ^= Type.GetHashCode();
            if (Level != 0) hash ^= Level.GetHashCode();
            return hash;
        }

        public override string ToString() => $"Element(Name={Name}, Type={Type}, Level={Level})";

        public MessageDescriptor Descriptor => throw new NotImplementedException();

        public void WriteTo(CodedOutputStream output)
        {
            if (Name.Length != 0)
            {
                output.WriteRawTag(10);
                output.WriteString(Name);
            }
            if (Description.Length != 0)
            {
                output.WriteRawTag(18);
                output.WriteString(Description);
            }
            if (Type.Length != 0)
            {
                output.WriteRawTag(26);
                output.WriteString(Type);
            }
            if (Level != 0)
            {
                output.WriteRawTag(32);
                output.WriteInt32(Level);
            }
        }

        public int CalculateSize()
        {
            int size = 0;
            if (Name.Length != 0)
            {
                size += 1 + CodedOutputStream.ComputeStringSize(Name);
            }
            if (Description.Length != 0)
            {
                size += 1 + CodedOutputStream.ComputeStringSize(Description);
            }
            if (Type.Length != 0)
            {
                size += 1 + CodedOutputStream.ComputeStringSize(Type);
            }
            if (Level != 0)
            {
                size += 1 + CodedOutputStream.ComputeInt32Size(Level);
            }
            return size;
        }

        public void MergeFrom(ElementDto other)
        {
            if (other == null)
            {
                return;
            }
            if (other.Name.Length != 0)
            {
                Name = other.Name;
            }
            if (other.Description.Length != 0)
            {
                Description = other.Description;
            }
            if (other.Type.Length != 0)
            {
                Type = other.Type;
            }
            if (other.Level != 0)
            {
                Level = other.Level;
            }
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
                    case 18:
                        Description = input.ReadString();
                        break;
                    case 26:
                        Type = input.ReadString();
                        break;
                    case 32:
                        Level = input.ReadInt32();
                        break;
                    default:
                        input.SkipLastField();
                        break;
                }
            }
        }
    }
}
