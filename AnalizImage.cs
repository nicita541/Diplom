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
    internal static class AnalizImage
    {
        //TODO не уверен в качестве выбора фото, возможно стоит добавить дополнительные критерии (например, освещенность, контрастность и т.д.)
        public static BitmapImage GetBestPhoto(List<BitmapImage> photos)
        {
            if (photos == null || photos.Count == 0)
                return null;

            BitmapImage bestImage = null;
            double bestScore = double.MinValue;

            foreach (var photo in photos)
            {
                double score = GetImageQualityScore(photo);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestImage = photo;
                }
            }

            return bestImage;
        }

        private static double GetImageQualityScore(BitmapImage image)
        {
            if (image == null)
                return double.MinValue;

            using (Mat mat = ConvertNew.BitmapImageToMat(image))
            using (Mat gray = new Mat())
            using (Mat laplacian = new Mat())
            {
                Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.Laplacian(gray, laplacian, MatType.CV_64F);

                Cv2.MeanStdDev(laplacian, out Scalar mean, out Scalar stddev);

                double sharpness = stddev.Val0 * stddev.Val0;
                return sharpness;
            }
        }

        
        //TODO заменить на реальную логику анализа изображения
        public static (int sign_id, double signheight, int distance_m, int visibility_percent, string sign_condition, string berm_condition) randimStats(BitmapImage image)
        {
            Random random = new Random();
            return (3, random.NextDouble(), random.Next(100), random.Next(100), "GOOD", "GOOD");
        }


    }
}
