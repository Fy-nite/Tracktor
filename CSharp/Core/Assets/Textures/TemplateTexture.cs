using Godot;

namespace Traktor.Core.Assets.Textures;

public class TemplateTexture
{
    public static ImageTexture CreateTemplateTexture(int width, int height)
    {
        var image = Image.CreateEmpty(width, height,false, Image.Format.Rgb8);

        // Fill the image with a simple pattern (e.g., checkerboard)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = ((x / 10 + y / 10) % 2 == 0) ? Colors.White : Colors.Black;
                image.SetPixel(x, y, color);
            }
        }
        var texture = ImageTexture.CreateFromImage(image);
        return texture;
    }
}