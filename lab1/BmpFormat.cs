
using System.Runtime.InteropServices;

//Версія 3
public static class BmpFormat
{
    // Константы размеров
    public const int FileHeaderSize = 14;
    public const int InfoHeaderSize = 40; // BITMAPINFOHEADER

    // BMP Header (14 байт)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BitmapFileHeader
    {
        public ushort bfType;        // Сигнатура: должно быть 'BM' (0x4D42)
        public uint bfSize;          // Размер всего файла
        public ushort bfReserved1;   // Зарезервировано (0)
        public ushort bfReserved2;   // Зарезервировано (0)
        public uint bfOffBits;       // Смещение до начала пиксельных данных
    }

    // DIB Header: BITMAPINFOHEADER (40 байт)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BitmapInfoHeader
    {
        public uint biSize;            // Размер структуры (40 байт)
        public int biWidth;            // Ширина изображения
        public int biHeight;           // Высота изображения (отрицательная — означает top-down)
        public ushort biPlanes;        // Кол-во цветовых плоскостей (1)
        public ushort biBitCount;      // Глубина цвета (1, 4, 8, 16, 24 или 32)
        public uint biCompression;     // Тип сжатия (0 = BI_RGB, без сжатия)
        public uint biSizeImage;       // Размер изображения в байтах (может быть 0 при BI_RGB)
        public int biXPelsPerMeter;    // Горизонтальное разрешение
        public int biYPelsPerMeter;    // Вертикальное разрешение
        public uint biClrUsed;         // Кол-во используемых цветов (0 — все)
        public uint biClrImportant;    // Важные цвета (0 — все)
    }

    // Пример вспомогательной структуры пикселя для 24-битного BMP
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RgbPixel
    {
        public byte Blue;
        public byte Green;
        public byte Red;
    }
}