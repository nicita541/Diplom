using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Diplom
{
    public partial class SignPlacementCard : UserControl
    {
        public SignPlacementCard()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(SignPlacementCard), new PropertyMetadata("Размещение"));

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(string), typeof(SignPlacementCard), new PropertyMetadata("READY"));

        public string Line1
        {
            get => (string)GetValue(Line1Property);
            set => SetValue(Line1Property, value);
        }

        public static readonly DependencyProperty Line1Property =
            DependencyProperty.Register(nameof(Line1), typeof(string), typeof(SignPlacementCard), new PropertyMetadata(""));

        public string Line2
        {
            get => (string)GetValue(Line2Property);
            set => SetValue(Line2Property, value);
        }

        public static readonly DependencyProperty Line2Property =
            DependencyProperty.Register(nameof(Line2), typeof(string), typeof(SignPlacementCard), new PropertyMetadata(""));

        public string Line3
        {
            get => (string)GetValue(Line3Property);
            set => SetValue(Line3Property, value);
        }

        public static readonly DependencyProperty Line3Property =
            DependencyProperty.Register(nameof(Line3), typeof(string), typeof(SignPlacementCard), new PropertyMetadata(""));

        public ImageSource ImageSource
        {
            get => (ImageSource)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(SignPlacementCard), new PropertyMetadata(null));
    }
}
