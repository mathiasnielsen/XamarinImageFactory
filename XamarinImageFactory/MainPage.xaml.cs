using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace XamarinImageFactory
{
    public sealed partial class MainPage : Page
    {
        public MainPage(Frame frame)
        {
            this.InitializeComponent();
            this.ShellSplitView.Content = frame;

            HomeRadioButton.IsChecked = true;
        }

        private void OnMenuButtonClicked(object sender, RoutedEventArgs e)
        {
            ShellSplitView.IsPaneOpen = !ShellSplitView.IsPaneOpen;
            ((RadioButton)sender).IsChecked = false;
        }

        private void OnHomeButtonChecked(object sender, RoutedEventArgs e)
        {
            ShellSplitView.IsPaneOpen = false;
            if (ShellSplitView.Content != null)
                ((Frame)ShellSplitView.Content).Navigate(typeof(HomePage));
        }

        private void OnIconButtonChecked(object sender, RoutedEventArgs e)
        {
            ShellSplitView.IsPaneOpen = false;
            if (ShellSplitView.Content != null)
                ((Frame)ShellSplitView.Content).Navigate(typeof(IconsPage));
        }

        private void OnSettingsButtonChecked(object sender, RoutedEventArgs e)
        {
            ShellSplitView.IsPaneOpen = false;
            if (ShellSplitView.Content != null)
                ((Frame)ShellSplitView.Content).Navigate(typeof(SettingsPage));
        }
    }
}
