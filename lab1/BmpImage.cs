
using System.Drawing;
using System.Runtime.InteropServices;
using static BmpFormat;

public class BmpImage
{
    public BmpFormat.BitmapFileHeader FileHeader;
    public BmpFormat.BitmapInfoHeader InfoHeader;
    public BmpFormat.RgbPixel[,] Pixels; // 2D-массив пикселей [y, x]

    public int[] CountBlackPixelsByRow()
    {
        int width = InfoHeader.biWidth;
        int height = Math.Abs(InfoHeader.biHeight);
        int[] rowCounts = new int[height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsBlack(Pixels[y, x]))
                    rowCounts[y]++;
            }
        }

        return rowCounts;
    }

    public int[] CountBlackPixelsByColumn()
    {
        int width = InfoHeader.biWidth;
        int height = Math.Abs(InfoHeader.biHeight);
        int[] colCounts = new int[width];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (IsBlack(Pixels[y, x]))
                    colCounts[x]++;
            }
        }

        return colCounts;
    }

    public bool IsBlack(RgbPixel pixel)
    {
        return pixel.Red == 0 && pixel.Blue == 0 && pixel.Green == 0;
    }

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

    public static bool[,] GetBinaryNeighborhood(RgbPixel[,] pixels, int row, int col)
    {
        int height = pixels.GetLength(0);
        int width = pixels.GetLength(1);

        bool[,] result = new bool[3, 3];

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                int y = row + dy;
                int x = col + dx;
                int ry = dy + 1;
                int rx = dx + 1;

                if (y < 0 || y >= height || x < 0 || x >= width)
                {
                    result[ry, rx] = false;
                }
                else
                {
                    RgbPixel p = pixels[y, x];
                    double gray = 0.299 * p.Red + 0.587 * p.Green + 0.114 * p.Blue;
                    result[ry, rx] = gray < 128;
                }
            }
        }

        return result;
    }

    public BmpImage DeleteSomeLines()
    {
        int sourceWidth = Pixels.GetLength(1);
        int sourceHeight = Pixels.GetLength(0);

        // Создаем новую матрицу
        RgbPixel[,] cropped = new RgbPixel[sourceHeight, sourceWidth];

        // Копируем пиксели
        for (int row = 0; row < sourceHeight; row++)
        {
            for (int col = 0; col < sourceWidth; col++)
            {
                var grid3x3 = GetBinaryNeighborhood(Pixels, row, col);
                var gВ = !grid3x3[0, 1] && grid3x3[2, 1] && grid3x3[1, 1] && (!grid3x3[0, 0] && grid3x3[1, 2] || grid3x3[1, 0] && grid3x3[1, 2] || !grid3x3[0, 2] && grid3x3[1, 0]);
                if (gВ)
                {
                    cropped[row, col] = new RgbPixel { Red = 255, Blue = 255, Green = 255 };
                }
            }
        }

        // Создаём копию с новыми параметрами
        var copy = new BmpImage
        {
            FileHeader = BmpFormat.CopyFileHeader(FileHeader),
            InfoHeader = BmpFormat.CopyInfoHeader(InfoHeader),
            Pixels = cropped
        };

        return copy;
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
            FileHeader = BmpFormat.CopyFileHeader(FileHeader),
            InfoHeader = BmpFormat.CopyInfoHeader(InfoHeader),
            Pixels = cropped
        };

        copy.InfoHeader.biWidth = width;
        copy.InfoHeader.biHeight = height;
        copy.InfoHeader.biSizeImage = (uint)(width * height * 3);// без паддингов

        copy.FileHeader.bfSize = (uint)(14 + copy.InfoHeader.biSize + copy.InfoHeader.biSizeImage);

        return copy;
    }

    public BmpImage GrayscaleConversion()
    {
        int sourceWidth = Pixels.GetLength(1);
        int sourceHeight = Pixels.GetLength(0);

        // Создаем новую матрицу
        RgbPixel[,] filtered = new RgbPixel[sourceHeight, sourceWidth];

        // Копируем пиксели
        for (int row = 0; row < sourceHeight; row++)
        {
            for (int col = 0; col < sourceWidth; col++)
            {
                var colorPixel = Pixels[row, col];
                byte Y = (byte)(0.299 * colorPixel.Red + 0.587 * colorPixel.Green + 0.114 * colorPixel.Blue);
                RgbPixel grayPixel = new RgbPixel { Red = Y, Green = Y, Blue = Y };
                filtered[row, col] = grayPixel;
            }
        }

        // Создаём копию с новыми параметрами
        var copy = new BmpImage
        {
            FileHeader = BmpFormat.CopyFileHeader(FileHeader),
            InfoHeader = BmpFormat.CopyInfoHeader(InfoHeader),
            Pixels = filtered
        };

        return copy;
    }

    public BmpImage BinaryConversion()
    {
        int sourceWidth = Pixels.GetLength(1);
        int sourceHeight = Pixels.GetLength(0);

        // Создаем новую матрицу
        RgbPixel[,] filtered = new RgbPixel[sourceHeight, sourceWidth];

        // Копируем пиксели
        for (int row = 0; row < sourceHeight; row++)
        {
            for (int col = 0; col < sourceWidth; col++)
            {
                var colorPixel = Pixels[row, col];
                byte Y = (byte)(0.299 * colorPixel.Red + 0.587 * colorPixel.Green + 0.114 * colorPixel.Blue);
                if (Y > 128)
                {
                    filtered[row, col] = new RgbPixel { Red = 255, Green = 255, Blue = 255 };
                }
                else
                {
                    filtered[row, col] = new RgbPixel { Red = 0, Green = 0, Blue = 0 };
                }
            }
        }

        // Создаём копию с новыми параметрами
        var copy = new BmpImage
        {
            FileHeader = BmpFormat.CopyFileHeader(FileHeader),
            InfoHeader = BmpFormat.CopyInfoHeader(InfoHeader),
            Pixels = filtered
        };

        return copy;
    }
}