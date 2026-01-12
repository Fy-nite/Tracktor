
using V12.Interfaces;
using V12.Types;

namespace V12.World.Templates
{
    public class SuperFLAT
    {
        public int Width { get; set; } = 10;
        public int Height { get; set; } = 10;
        public double BlockSize { get; set; } = 1.0;

        public SuperFLAT()
        {

        }

        public void GenerateWorld(IWorldElement rootElement)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Height; z++)
                {
                    var block = new Element { Name = $"Block_{x}_{z}", Type = "Block" };
                    block.Position = new DoubleVector3(x * BlockSize, 0, z * BlockSize, 1);
                    block.Scale = new DoubleVector3(BlockSize, BlockSize, BlockSize, 1);
                    var transformComp = new TransformComponent();
                    transformComp.Position = block.Position;
                    transformComp.Scale = block.Scale;
                    block.AddComponent(transformComp);
                    rootElement.AddChild(block);
                }
            }
        }
    }
}
