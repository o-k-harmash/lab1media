using System.Drawing;

var bmp = BmpImage.Load("Безымянный.bmp");
var cropped = bmp.Crop(new Rectangle(350, 200, 800, 800));
cropped.Log();
cropped.Save("b.bmp");

