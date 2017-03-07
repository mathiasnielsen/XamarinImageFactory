using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace XamarinImageFactory
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            PrepareUIElements();
        }

        private void PrepareUIElements()
        {
            PickButton.Click += OnPickButtonClicked;
        }

        private async void OnPickButtonClicked(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
            }
            else
            {
                // cancelled
            }
        }

        private async Task SaveFilesAsync(List<StorageFile> files)
        {
            var folder = CreateFolderForImagesAsync();

            SaveFileAsync(folder, files)
        }

        private async Task SaveFileAsync(StorageFolder folder, string filename)
        {
            await folder.CreateFileAsync(filename);
        }

        private async Task<StorageFolder> CreateFolderForImagesAsync()
        {
            var folderName = string.IsNullOrWhiteSpace(FolderNameTextBox.Text) ? Guid.NewGuid().ToString() : FolderNameTextBox.Text;
            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);

            return folder;
        }
    }
}
