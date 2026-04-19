using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Diplom
{
    internal static class ConvertNew
    {
        public static byte[] BitmapImageToBytes(BitmapImage image)
        {
            if (image == null)
                return null;

            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        public static Mat BitmapImageToMat(BitmapImage image)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                byte[] bytes = ms.ToArray();
                return Cv2.ImDecode(bytes, ImreadModes.Color);
            }
        }

        public static BitmapImage MatToBitmapImage(Mat mat)
        {
            Cv2.ImEncode(".bmp", mat, out byte[] buffer);

            using (var ms = new MemoryStream(buffer))
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }
    }
}
