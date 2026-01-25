namespace Tickflo.Web.Utils;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

public static class ImageCompressor
{
    public static void CompressAndSave(Stream input, string outputPath, int maxWidth = 256, int maxHeight = 256, long quality = 75L)
    {
        using var image = Image.Load(input);
        var width = image.Width;
        var height = image.Height;
        if (width > maxWidth || height > maxHeight)
        {
            var scale = Math.Min((float)maxWidth / width, (float)maxHeight / height);
            width = (int)(width * scale);
            height = (int)(height * scale);
            image.Mutate(x => x.Resize(width, height));
        }
        var encoder = new JpegEncoder { Quality = (int)quality };
        image.Save(outputPath, encoder);
    }

    public static void CompressAndSave(Stream input, Stream output, int maxWidth = 256, int maxHeight = 256, long quality = 75L)
    {
        using var image = Image.Load(input);
        var width = image.Width;
        var height = image.Height;
        if (width > maxWidth || height > maxHeight)
        {
            var scale = Math.Min((float)maxWidth / width, (float)maxHeight / height);
            width = (int)(width * scale);
            height = (int)(height * scale);
            image.Mutate(x => x.Resize(width, height));
        }
        var encoder = new JpegEncoder { Quality = (int)quality };
        image.SaveAsJpeg(output, encoder);
    }
}
