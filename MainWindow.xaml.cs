using Microsoft.UI.Xaml.Input;

namespace photos
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            this.MainFrame.Navigate(typeof(MainPage));

            if (this.Content != null)
            {
                this.Content.KeyDown += Content_KeyDown;
            }
        }

        private void Content_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                Environment.Exit(0);
            }
        }
    }
}
