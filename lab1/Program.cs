using System.Drawing;

// var bmp = BmpImage.Load("hornyline.bmp");
// // var cropped = bmp.Crop(new Rectangle(350, 200, 800, 800));
// // var binaryed = cropped.BinaryConversion();
// var hornyline = bmp.DeleteSomeLines();

// hornyline.Save("horny.bmp");

/*
    Принцип работы захардкоженные эталоные вектора хранящие внутри параметры вида кол вертикальных черных пикселей и кол горизонтальных по каждому ряду строке.
    По задаче типо эвристические характеристики или типо того.
    Не понятно каким образом решать вопрос скейлинга изображения если эталон n на n а вход m на m, относительными величинами 
    или усреднением или еще чем-то или метод сравнивания изменять.
    
    Заданы эталоны 7х5 вида 
     [0] = new DigitTemplate
        {
            HorizontalProfile = new[] { 4, 2, 2, 2, 2, 2, 4 },
            VerticalProfile = new[] { 6, 1, 1, 1, 6 }
        },
    
    и изображения 7х5 которые считаются подготовленными удаленные линии бахромы заполненные пустоты выровняные бинаризированные цвета и так далее
    и просто сравнивается с каждым эталоном через сумму разниц величин и находится тот у кого сумма минимальна
    на практике как-то 7 и 0 определяет и пойдет
*/
var bmpImage = BmpImage.Load("precalculated-digit7-60+pixels.bmp");

var binarizedMatrix = bmpImage.GetBinarizedColorPixelMatrix(bmpImage.Pixels);
var resizedMatrix = bmpImage.ResizeBinaryNearest(binarizedMatrix, 32, 32);

var horizontalDensity = bmpImage.GetNormalizedBlackPixelDensityPerRow(resizedMatrix);
var verticalDensity = bmpImage.GetNormalizedBlackPixelDensityPerColumn(resizedMatrix);

for (int i = 0; i < verticalDensity.Length; i++)
    Console.WriteLine(verticalDensity[i]);

// Объединяем в один входной вектор
var inputVector = horizontalDensity.Concat(verticalDensity).ToArray(); // [64]

var neuralNet = new NeuralNetwork(); // из предыдущего сообщения

// Создаем one-hot метку для цифры 7
var label = new double[10];
label[7] = 1.0;

// Обучаем на этом одном примере
var dataset = new List<(double[], double[])> { (inputVector, label) };
neuralNet.Train(dataset, 10000); // 10 тыс эпох для стабильности

// Проверяем результат
var result = neuralNet.FeedForward(inputVector);
int predicted = Array.IndexOf(result, result.Max());

Console.WriteLine($"Ожидалось: 7, Предсказано: {predicted}, Вероятности: {string.Join(", ", result.Select(x => x.ToString("F2")))}");

// for (int i = 0; i < verticalDensity.Length; i++)
//     Console.WriteLine(verticalDensity[i]);

// Сравнение с шаблонами (по евклиду, по Чебышеву, по корреляции — не важно пока)
// int bestMatch = DigitTemplates.Templates
//     .OrderBy(t => DigitTemplates.Distance(inputHor, t.Value.HorizontalProfile) + DigitTemplates.Distance(inputVer, t.Value.VerticalProfile))
//     .First().Key;

// Console.WriteLine($"bestMatch index - {bestMatch}");

// public class DigitTemplate
// {
//     public int[] HorizontalProfile; // по строкам
//     public int[] VerticalProfile;   // по столбцам
// }

// public static class DigitTemplates
// {
//     public static int Distance(int[] a, int[] b)
//     {
//         int len = Math.Min(a.Length, b.Length);
//         int sum = 0;
//         for (int i = 0; i < len; i++)
//             sum += Math.Abs(a[i] - b[i]);

//         return sum;
//     }

//     public static Dictionary<int, DigitTemplate> Templates = new()
//     {
//         [0] = new DigitTemplate
//         {
//             HorizontalProfile = new[] { 4, 2, 2, 2, 2, 2, 4 },
//             VerticalProfile = new[] { 6, 1, 1, 1, 6 }
//         },
//         [1] = new DigitTemplate
//         {
//             HorizontalProfile = new[] { 1, 2, 1, 1, 1, 1, 3 },
//             VerticalProfile = new[] { 1, 2, 1, 1, 6 }
//         },
//         [2] = new DigitTemplate
//         {
//             HorizontalProfile = new[] { 4, 2, 1, 2, 1, 2, 5 },
//             VerticalProfile = new[] { 6, 1, 1, 2, 5 }
//         },
//         [3] = new DigitTemplate
//         {
//             HorizontalProfile = new[] { 4, 2, 1, 3, 1, 2, 4 },
//             VerticalProfile = new[] { 6, 1, 2, 1, 6 }
//         },
//         [4] = new DigitTemplate
//         {
//             HorizontalProfile = new[] { 2, 2, 2, 4, 1, 1, 1 },
//             VerticalProfile = new[] { 2, 2, 2, 6, 2 }
//         },
//         [5] = new DigitTemplate
//         {
//             HorizontalProfile = new[] { 5, 1, 1, 4, 1, 1, 5 },
//             VerticalProfile = new[] { 6, 1, 1, 1, 6 }
//         },
//         [6] = new DigitTemplate
//         {
//             HorizontalProfile = new[] { 4, 1, 1, 4, 2, 2, 4 },
//             VerticalProfile = new[] { 6, 1, 1, 2, 4 }
//         },
//         [7] = new DigitTemplate
//         {
//             HorizontalProfile = new[] { 5, 1, 1, 1, 1, 1, 1 },
//             VerticalProfile = new[] { 6, 1, 1, 1, 1 }
//         },
//         [8] = new DigitTemplate
//         {
//             HorizontalProfile = new[] { 4, 2, 2, 4, 2, 2, 4 },
//             VerticalProfile = new[] { 6, 1, 2, 1, 6 }
//         },
//         [9] = new DigitTemplate
//         {
//             HorizontalProfile = new[] { 4, 2, 2, 4, 1, 1, 4 },
//             VerticalProfile = new[] { 6, 1, 2, 1, 6 }
//         },
//     };
// }