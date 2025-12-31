using System;
using System.Collections.Generic;
using System.Text;

namespace V12.Interfaces
{
    public class IRefElement
    {
        public string RefName { get; set; }
        public string RefType { get; set; }
        public long   RefId   { get; set; }
        public IRefElement() { }
        public IRefElement(string refName, string refType)
        {
            RefName = refName;
            RefType = refType;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Ref Element Name: {RefName}");
            sb.AppendLine($"Ref Element Type: {RefType}");
            return sb.ToString();
        }
    }
}
