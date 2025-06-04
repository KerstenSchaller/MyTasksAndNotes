using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MyTasksAndNotes
{
    public static class ImageConvert
    {

        public static System.Drawing.Image ConvertWpfImageToDrawingImage(System.Windows.Controls.Image imageControl)
        {
            BitmapSource bitmapSource = imageControl.Source as BitmapSource;
            if (bitmapSource == null)
                return null;

            using (MemoryStream ms = new MemoryStream())
            {
                // Encode BitmapSource to PNG in memory stream
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);

                // Create System.Drawing.Image from stream
                System.Drawing.Image drawingImage = System.Drawing.Image.FromStream(ms);
                return (System.Drawing.Image)drawingImage.Clone(); // Clone to detach stream
            }
        }

        public static System.Windows.Controls.Image ConvertDrawingImageToWpfImage(System.Drawing.Image drawingImage)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                drawingImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Freeze to make it cross-thread accessible

                System.Windows.Controls.Image wpfImage = new System.Windows.Controls.Image();
                wpfImage.Source = bitmapImage;
                return wpfImage;
            }
        }
    }

    public static class ImageExtensions
    {
        public static System.Windows.Controls.Image toWPF(this System.Drawing.Image img)
        {
            return ImageConvert.ConvertDrawingImageToWpfImage(img);
        }

        public static System.Drawing.Image toSystemDrawing(this System.Windows.Controls.Image img)
        {
            return ImageConvert.ConvertWpfImageToDrawingImage(img);
        }

    }
}
