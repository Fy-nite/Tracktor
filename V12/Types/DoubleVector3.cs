using System;
using System.Collections.Generic;
using System.Text;

namespace V12.Types
{
    public class DoubleVector3
    {
        public double x;
        public double y;
        public double z;
        public double w;
        public DoubleVector3(double x, double y, double z, double w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"X: {x}");
            sb.AppendLine($"Y: {y}");
            sb.AppendLine($"Z: {z}");
            sb.AppendLine($"W: {w}");
            return sb.ToString();
        }
        public override int GetHashCode()
        {
            return x.GetHashCode();
        }
    }
}
