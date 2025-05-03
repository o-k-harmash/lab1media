using System.Drawing;

var bmp = BmpImage.Load("hornyline.bmp");
// var cropped = bmp.Crop(new Rectangle(350, 200, 800, 800));
// var binaryed = cropped.BinaryConversion();
var hornyline = bmp.DeleteSomeLines();

hornyline.Save("horny.bmp");

