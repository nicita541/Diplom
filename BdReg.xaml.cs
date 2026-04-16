using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClassLibrary2;

namespace Diplom
{
    public partial class BdReg : Window
    {
        public static string CurrentRoute = "-";
        public static string CurrentDirection = "-";
        public static string CurrentPicket = "-";

        public static int? CurrentRouteId = null;
        public static int? CurrentDirectionId = null;
        public static int? CurrentPicketId = null;

        private bool _isRestoring = false;

        public BdReg()
        {
            InitializeComponent();
            LoadRoutes();
            RestoreIdsFromNames();
            RestoreSelection();
        }

        private void LoadRoutes()
        {
            using (var db = new dataBase())
            {
                ComboBoxRoute.ItemsSource = db.Route
                    .OrderBy(r => r.code)
                    .ToList();
            }
        }

        private void LoadDirections(int routeId)
        {
            using (var db = new dataBase())
            {
                ComboBoxDirection.ItemsSource = db.Direction
                    .Where(d => d.route_id == routeId)
                    .OrderBy(d => d.direction_type)
                    .ToList();
            }
        }

        private void LoadPickets(int directionId)
        {
            using (var db = new dataBase())
            {
                ComboBoxPicket.ItemsSource = db.Picket
                    .Where(p => p.direction_id == directionId)
                    .OrderBy(p => p.picket_number)
                    .ToList();
            }
        }

        private void RestoreIdsFromNames()
        {
            try
            {
                BdClass bd = new BdClass();

                if (!string.IsNullOrWhiteSpace(CurrentRoute) && CurrentRoute != "-")
                {
                    CurrentRouteId = bd.GetRouteIdByCode(CurrentRoute);
                }

                if (CurrentRouteId.HasValue &&
                    !string.IsNullOrWhiteSpace(CurrentDirection) &&
                    CurrentDirection != "-")
                {
                    CurrentDirectionId = bd.GetDirectionIdByRouteAndName(CurrentRouteId.Value, CurrentDirection);
                }

                if (CurrentDirectionId.HasValue &&
                    !string.IsNullOrWhiteSpace(CurrentPicket) &&
                    CurrentPicket != "-")
                {
                    int picketNumber;
                    if (int.TryParse(CurrentPicket, out picketNumber))
                    {
                        CurrentPicketId = bd.GetPicketIdByDirectionAndNumber(CurrentDirectionId.Value, picketNumber);
                    }
                }
            }
            catch
            {
                CurrentRouteId = null;
                CurrentDirectionId = null;
                CurrentPicketId = null;
            }
        }

        private void RestoreSelection()
        {
            _isRestoring = true;

            try
            {
                if (CurrentRouteId.HasValue)
                {
                    ComboBoxRoute.SelectedValue = CurrentRouteId.Value;
                    LoadDirections(CurrentRouteId.Value);
                }

                if (CurrentDirectionId.HasValue)
                {
                    ComboBoxDirection.SelectedValue = CurrentDirectionId.Value;
                    LoadPickets(CurrentDirectionId.Value);
                }

                if (CurrentPicketId.HasValue)
                {
                    ComboBoxPicket.SelectedValue = CurrentPicketId.Value;
                }
            }
            finally
            {
                _isRestoring = false;
            }
        }

        private void ComboBoxRoute_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedRoute = ComboBoxRoute.SelectedItem as Route;
            if (selectedRoute == null)
                return;

            CurrentRouteId = selectedRoute.id;
            CurrentRoute = selectedRoute.code;

            LoadDirections(selectedRoute.id);

            if (!_isRestoring)
            {
                CurrentDirectionId = null;
                CurrentDirection = "-";
                CurrentPicketId = null;
                CurrentPicket = "-";

                ComboBoxDirection.SelectedIndex = -1;
                ComboBoxPicket.ItemsSource = null;
                ComboBoxPicket.SelectedIndex = -1;
            }
        }

        private void ComboBoxDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedDirection = ComboBoxDirection.SelectedItem as Direction;
            if (selectedDirection == null)
                return;

            CurrentDirectionId = selectedDirection.id;
            CurrentDirection = selectedDirection.direction_type;

            LoadPickets(selectedDirection.id);

            if (!_isRestoring)
            {
                CurrentPicketId = null;
                CurrentPicket = "-";
                ComboBoxPicket.SelectedIndex = -1;
            }
        }

        private void ComboBoxPicket_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPicket = ComboBoxPicket.SelectedItem as Picket;
            if (selectedPicket == null)
                return;

            CurrentPicketId = selectedPicket.id;
            CurrentPicket = selectedPicket.picket_number.ToString();
        }

        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            CreatePathWindow window = new CreatePathWindow();
            window.Owner = this;

            if (window.ShowDialog() == true)
            {
                LoadRoutes();
                RestoreIdsFromNames();
                RestoreSelection();
            }
        }
    }
}