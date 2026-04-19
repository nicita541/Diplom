using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

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
                    Line1 = "sign_id: ? | picket_id: ?",
                    Line2 = "distance_m: ? | visibility_percent: ?",
                    Line3 = "sign_condition: ? | berm_condition: ?"
                };

                stackPanels[trek_id].Add(card);
                sp.Children.Add(card);
                sign.Text = (int.Parse(sign.Text) + 1).ToString();

            });

        }

        public void PenStackPanel(int trek_id)
        {
            sp.Dispatcher.Invoke(() =>
            {
                stackPanels[trek_id][0].Status = "PENDING";
            });

            //TODO Сделать расмотрение знака
            SaveStackPanel(trek_id);
        }

        public void SaveStackPanel(int trek_id)
        {

            //TODO Сохранить знак в БД
            sp.Dispatcher.Invoke(() =>
            {
                stackPanels[trek_id][0].Status = "SAVED";
                signSaved.Text = (int.Parse(signSaved.Text) + 1).ToString();
            });

        }

    }

}
