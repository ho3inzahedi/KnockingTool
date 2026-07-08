using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

var outputPath = args.Length > 1 ? args[1] : Path.Combine("Assets", "app.ico");
var sizes = new[] { 16, 32, 48, 64, 128, 256 };

List<Bitmap> images;
if (args.Length > 0 && File.Exists(args[0]))
{
    using var source = new Bitmap(args[0]);
    images = sizes.Select(size =>
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(source, new Rectangle(0, 0, size, size));
        return bitmap;
    }).ToList();
}
else
{
    images = sizes.Select(RenderIcon).ToList();
}

SaveIco(outputPath, images);
foreach (var image in images)
{
    image.Dispose();
}

Console.WriteLine($"Icon generated at {outputPath}");

static Bitmap RenderIcon(int size)
{
    var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var graphics = Graphics.FromImage(bitmap);
    graphics.SmoothingMode = SmoothingMode.AntiAlias;
    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
    graphics.Clear(Color.FromArgb(37, 99, 235));

    var scale = size / 256f;
    var centerX = size / 2f;
    using var white = new SolidBrush(Color.White);
    using var pen = new Pen(Color.White, 22f * scale)
    {
        StartCap = LineCap.Round,
        EndCap = LineCap.Round,
        LineJoin = LineJoin.Round
    };

    var trapezoid = new GraphicsPath();
    var topWidth = 92f * scale;
    var bottomWidth = 56f * scale;
    var topY = 54f * scale;
    var bottomY = 100f * scale;
    trapezoid.AddPolygon(
    [
        new PointF(centerX - topWidth / 2f, topY),
        new PointF(centerX + topWidth / 2f, topY),
        new PointF(centerX + bottomWidth / 2f, bottomY),
        new PointF(centerX - bottomWidth / 2f, bottomY)
    ]);
    graphics.FillPath(white, trapezoid);

    var ringRect = new RectangleF(centerX - 58f * scale, 104f * scale, 116f * scale, 116f * scale);
    graphics.DrawArc(pen, ringRect, 20, 140);

    var dotRadius = 12f * scale;
    graphics.FillEllipse(white, centerX - dotRadius, 214f * scale - dotRadius, dotRadius * 2f, dotRadius * 2f);

    return bitmap;
}

static void SaveIco(string outputPath, List<Bitmap> images)
{
    using var memory = new MemoryStream();
    using var writer = new BinaryWriter(memory);

    writer.Write((short)0);
    writer.Write((short)1);
    writer.Write((short)images.Count);

    var offset = 6 + 16 * images.Count;
    var pngChunks = new List<byte[]>();
    foreach (var image in images)
    {
        using var pngStream = new MemoryStream();
        image.Save(pngStream, ImageFormat.Png);
        var bytes = pngStream.ToArray();
        pngChunks.Add(bytes);
        var size = image.Width;
        writer.Write((byte)(size >= 256 ? 0 : size));
        writer.Write((byte)(size >= 256 ? 0 : size));
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(bytes.Length);
        writer.Write(offset);
        offset += bytes.Length;
    }

    foreach (var bytes in pngChunks)
    {
        writer.Write(bytes);
    }

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
    File.WriteAllBytes(outputPath, memory.ToArray());
}
