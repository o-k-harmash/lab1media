
using System.Drawing;
using System.Runtime.InteropServices;
using static BmpFormat;

public class BmpImage
{
    public BmpFormat.BitmapFileHeader FileHeader;
    public BmpFormat.BitmapInfoHeader InfoHeader;
    public BmpFormat.RgbPixel[,] Pixels; // 2D-массив пикселей [y, x]

    public void Log()
    {
        // Логирование File Header
        Console.WriteLine("=== Bitmap File Header ===");
        Console.WriteLine($"Type: {(char)(FileHeader.bfType & 0xFF)}{(char)(FileHeader.bfType >> 8)}");
        Console.WriteLine($"Size: {FileHeader.bfSize}");
        Console.WriteLine($"Reserved1: {FileHeader.bfReserved1}");
        Console.WriteLine($"Reserved2: {FileHeader.bfReserved2}");
        Console.WriteLine($"Offset to Pixel Data: {FileHeader.bfOffBits}");

        // Логирование Info Header
        Console.WriteLine("\n=== Bitmap Info Header ===");
        Console.WriteLine($"Header Size: {InfoHeader.biSize}");
        Console.WriteLine($"Width: {InfoHeader.biWidth}");
        Console.WriteLine($"Height: {InfoHeader.biHeight}");
        Console.WriteLine($"Planes: {InfoHeader.biPlanes}");
        Console.WriteLine($"Bit Count: {InfoHeader.biBitCount}");
        Console.WriteLine($"Compression: {InfoHeader.biCompression}");
        Console.WriteLine($"Image Size: {InfoHeader.biSizeImage}");
        Console.WriteLine($"X Pixels Per Meter: {InfoHeader.biXPelsPerMeter}");
        Console.WriteLine($"Y Pixels Per Meter: {InfoHeader.biYPelsPerMeter}");
        Console.WriteLine($"Colors Used: {InfoHeader.biClrUsed}");
        Console.WriteLine($"Important Colors: {InfoHeader.biClrImportant}");
    }

    public static BmpImage Load(string path)
    {
        var bmp = new BmpImage();

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        // Читаем File Header
        bmp.FileHeader = ReadStruct<BmpFormat.BitmapFileHeader>(br);

        if (bmp.FileHeader.bfType != 0x4D42)
            throw new InvalidDataException("Not a valid BMP file (missing BM header).");

        // Читаем Info Header
        bmp.InfoHeader = ReadStruct<BmpFormat.BitmapInfoHeader>(br);

        if (bmp.InfoHeader.biBitCount != 24 || bmp.InfoHeader.biCompression != 0)
            throw new NotSupportedException("Only uncompressed 24-bit BMP is supported.");

        int width = bmp.InfoHeader.biWidth;
        int height = Math.Abs(bmp.InfoHeader.biHeight);
        int padding = (4 - (width * 3) % 4) % 4;

        // Переходим к пикселям
        fs.Seek(bmp.FileHeader.bfOffBits, SeekOrigin.Begin);

        bmp.Pixels = new BmpFormat.RgbPixel[height, width];

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                bmp.Pixels[y, x] = ReadStruct<BmpFormat.RgbPixel>(br);
            }
            br.BaseStream.Seek(padding, SeekOrigin.Current); // скип паддинга
        }

        return bmp;
    }

    public void Save(string path)
    {
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        WriteStruct(bw, FileHeader);
        WriteStruct(bw, InfoHeader);

        int width = InfoHeader.biWidth;
        int height = Math.Abs(InfoHeader.biHeight);
        int padding = (4 - (width * 3) % 4) % 4;
        byte[] padBytes = new byte[padding];

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                WriteStruct(bw, Pixels[y, x]);
            }
            bw.Write(padBytes);
        }
    }

    public BmpImage Crop(Rectangle rect)
    {
        int sourceWidth = Pixels.GetLength(1);
        int sourceHeight = Pixels.GetLength(0);

        // Корректируем прямоугольник, если он выходит за границы
        int x = Math.Max(0, rect.X);
        int y = Math.Max(0, rect.Y);
        int width = Math.Min(rect.Width, sourceWidth - x);
        int height = Math.Min(rect.Height, sourceHeight - y);

        // Создаем новую матрицу
        RgbPixel[,] cropped = new RgbPixel[height, width];

        // Копируем пиксели
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                cropped[row, col] = Pixels[y + row, x + col];
            }
        }

        // Создаём копию с новыми параметрами
        var copy = new BmpImage
        {
            FileHeader = new BitmapFileHeader
            {
                bfType = this.FileHeader.bfType,
                bfSize = this.FileHeader.bfSize,
                bfReserved1 = this.FileHeader.bfReserved1,
                bfReserved2 = this.FileHeader.bfReserved2,
                bfOffBits = this.FileHeader.bfOffBits
            },
            InfoHeader = new BitmapInfoHeader
            {
                biSize = this.InfoHeader.biSize,
                biPlanes = this.InfoHeader.biPlanes,
                biBitCount = this.InfoHeader.biBitCount,
                biCompression = this.InfoHeader.biCompression,
                biXPelsPerMeter = this.InfoHeader.biXPelsPerMeter,
                biYPelsPerMeter = this.InfoHeader.biYPelsPerMeter,
                biClrImportant = this.InfoHeader.biClrImportant,
                biClrUsed = this.InfoHeader.biClrUsed,

                biWidth = width,
                biHeight = height,
                biSizeImage = (uint)(width * height * 3) // без паддингов
            },
            Pixels = cropped
        };

        copy.FileHeader.bfSize = (uint)(14 + copy.InfoHeader.biSize + copy.InfoHeader.biSizeImage);

        return copy;
    }


    private static T ReadStruct<T>(BinaryReader reader) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] data = reader.ReadBytes(size);
        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    private static void WriteStruct<T>(BinaryWriter writer, T value) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] buffer = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(value, ptr, false);
            Marshal.Copy(ptr, buffer, 0, size);
            writer.Write(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}