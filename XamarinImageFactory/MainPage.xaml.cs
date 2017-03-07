using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace XamarinImageFactory
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StorageFile _file;

        public MainPage()
        {
            this.InitializeComponent();

            PrepareUIElements();
        }

        private void PrepareUIElements()
        {
            PickButton.Click += OnPickButtonClicked;
            CreateImagesButton.Click += OnCreateImagesButtonClicked;
        }

        private async void OnCreateImagesButtonClicked(object sender, RoutedEventArgs e)
        {
            await StartCreatingImages();
        }

        private async void OnPickButtonClicked(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            _file = await picker.PickSingleFileAsync();
            if (_file != null)
            {
                // Application now has read/write access to the picked file
                var img = new BitmapImage();
                img = await LoadImage(_file);
                MainImage.Source = img;
            }
            else
            {
                // cancelled
            }
        }

        private static async Task<BitmapImage> LoadImage(StorageFile file)
        {
            var bitmapImage = new BitmapImage();
            FileRandomAccessStream stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);

            bitmapImage.SetSource(stream);

            return bitmapImage;
        }

        private async Task StartCreatingImages()
        {
            var folder = await CreateFolderForImagesAsync();

            if (AndroidCheckBox.IsChecked == true)
            {
                var androidFolder = await folder.CreateFolderAsync("Android");
                var androidImageFile = await androidFolder.CreateFileAsync("test.png");
                await ResizeImageAsync(_file, androidImageFile);
            }
        }

        private async Task ResizeImageAsync(StorageFile sourceFile, StorageFile destinationFile)
        {
            using (var sourceStream = await sourceFile.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(sourceStream);
                BitmapTransform transform = new BitmapTransform() { ScaledHeight = 80, ScaledWidth = 80 };
                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Rgba8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.DoNotColorManage);

                using (var destinationStream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, destinationStream);

                    var detachedPixelData = pixelData.DetachPixelData();
                    encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied, 80, 80, 96, 96, detachedPixelData);
                    await encoder.FlushAsync();
                }

                var test = destinationFile;
            }
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
