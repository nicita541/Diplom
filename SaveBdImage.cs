using ClassLibrary2;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Diplom
{
    internal class SaveBdImage
    {
        Dictionary<int,List<SignPlacementCard>> stackPanels = new Dictionary<int, List<SignPlacementCard>>();
        StackPanel sp;
        TextBlock sign;
        TextBlock signSaved;

        public SaveBdImage(StackPanel sp, TextBlock sign, TextBlock signSaved)
        {
            this.sp = sp;
            this.sign = sign;
            this.signSaved = signSaved;
        }

        public void AddStackPanel(int trek_id)
        {
            sp.Dispatcher.Invoke(() =>
            {
                if (!stackPanels.ContainsKey(trek_id))
                    stackPanels[trek_id] = new List<SignPlacementCard>();

                var card = new SignPlacementCard
                {
                    Title = $"Новый Знак {trek_id}",
                    Status = "READY",
                    Line1 = $"sign_id: ? | picket_id: {AiVideoProcessingWindow.id_picket}",
                    Line2 = "distance_m: ? | visibility_percent: ?",
                    Line3 = "sign_condition: ? | berm_condition: ?"
                };

                stackPanels[trek_id].Add(card);
                sp.Children.Add(card);
                sign.Text = (int.Parse(sign.Text) + 1).ToString();

            });

        }

        public void PenStackPanel(int trek_id, imageSign imgSign)
        {
            BitmapImage bestPhoto = AnalizImage.GetBestPhoto(imgSign.img);
            sp.Dispatcher.Invoke(() =>
            {
                stackPanels[trek_id][0].Status = "PENDING";
                stackPanels[trek_id][0].ImageSource = bestPhoto;
            });

            var (sign_id, signheight, distance_m, visibility_percent, sign_condition, berm_condition) = AnalizImage.randimStats(bestPhoto);

            sp.Dispatcher.Invoke(() =>
            {
                stackPanels[trek_id][0].Line1 = $"sign_id: {sign_id} | picket_id: {AiVideoProcessingWindow.id_picket}";
                stackPanels[trek_id][0].Line2 = $"distance_m: {distance_m} | visibility_percent: {visibility_percent}";
                stackPanels[trek_id][0].Line3 = $"sign_condition: {sign_condition} | berm_condition: {berm_condition}";
            });

            //TODO Сделать расмотрение знака
            SaveStackPanel(trek_id, sign_id, signheight, distance_m, visibility_percent, sign_condition, berm_condition, ConvertNew.BitmapImageToBytes(bestPhoto));
        }


        public void SaveStackPanel(int trek_id ,int signid, double signheight, int distancem, int visibilitypercent, string signcondition, string bermcondition, byte[] photo)
        {

            using(var db = new dataBase())
            {
                var signEntity = new SignPlacement
                {
                    picket_id = AiVideoProcessingWindow.id_picket.Value,
                    sign_id = signid,
                    distance_m = distancem,
                    berm_condition = bermcondition,
                    sign_height = signheight,
                    sign_condition = signcondition,
                    visibility_percent = visibilitypercent,
                    photo = photo
                };
                db.SignPlacement.Add(signEntity);
                db.SaveChanges();
            }

            //TODO Сохранить знак в БД
            sp.Dispatcher.Invoke(() =>
            {
                stackPanels[trek_id][0].Status = "SAVED";
                signSaved.Text = (int.Parse(signSaved.Text) + 1).ToString();
            });

        }

    }

}
