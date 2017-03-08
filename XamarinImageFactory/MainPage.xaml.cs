using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using XamarinImageFactory.Common;

namespace XamarinImageFactory
{
    public sealed partial class MainPage : Page
    {
        private StorageFolder _folder;
        private string _fileName;
        private StorageFile _file;

        private StorageFile _lowResult;
        private StorageFile _mediumResult;
        private StorageFile _highResult;

        public MainPage()
        {
            this.InitializeComponent();

            PrepareUIElements();
        }

        private void PrepareUIElements()
        {
            PickButton.Click += OnPickButtonClicked;
            CreateImagesButton.Click += OnCreateImagesButtonClicked;

            LowQualityCheckbox.Checked += OnLowQualitySelected;
            MediumQualityCheckbox.Checked += OnMediumQualitySelected;
            HighQualityCheckbox.Checked += OnHighQualitySelected;
        }

        private async void OnLowQualitySelected(object sender, RoutedEventArgs e)
        {
            await SetResultImageAsync(_lowResult);
        }

        private async void OnMediumQualitySelected(object sender, RoutedEventArgs e)
        {
            await SetResultImageAsync(_mediumResult);
        }

        private async void OnHighQualitySelected(object sender, RoutedEventArgs e)
        {
            await SetResultImageAsync(_highResult);
        }

        private async Task SetResultImageAsync(StorageFile resultFile)
        {
            if (resultFile != null)
            {
                var img = new BitmapImage();
                img = await LoadImage(resultFile);
                ResultImage.Source = img;

                ResultFileInfoTextBlock.Text = await GetImageFileInfoAsync(resultFile, img);
            }
        }

        private async Task<string> GetImageFileInfoAsync(StorageFile file, BitmapImage bitmap)
        {
            var name = file.Name;
            var properties = await file.GetBasicPropertiesAsync();
            var understandableSize = SizeSuffix((long)properties.Size, 2);

            var width = bitmap.PixelWidth;
            var height = bitmap.PixelHeight;

            return $"Name: {name}\nImageSize: {width}x{height}\nFileSize: {understandableSize}";
        }

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }

        private async void OnCreateImagesButtonClicked(object sender, RoutedEventArgs e)
        {
            _folder = await CreateFolderForImagesAsync();
            if (_folder == null)
            {
                // Cancelled.
                return;
            }

            ProgressView.Visibility = Visibility.Visible;
            await StartCreatingImages();

            SetInitialResult();
            _folder = null;

            ProgressView.Visibility = Visibility.Collapsed;
        }

        private void SetInitialResult()
        {
            if (MediumQualityCheckbox.IsChecked == true)
            {
                OnMediumQualitySelected(this, null);
            }
            else
            {
                MediumQualityCheckbox.IsChecked = true;
            }
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

                FileInfoTextBlock.Text = await GetImageFileInfoAsync(_file, img);
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

        private string GetFileName()
        {
            var fileName = string.IsNullOrWhiteSpace(FolderNameTextBox.Text) ? Guid.NewGuid().ToString() : FolderNameTextBox.Text;
            return $"{fileName}.png";
        }

        private async Task StartCreatingImages()
        {
            _fileName = GetFileName();

            var mainFile = await _folder.CreateFileAsync(_fileName, CreationCollisionOption.ReplaceExisting);

            await SaveStorageFileAsync(mainFile, _file);

            if (AndroidCheckBox.IsChecked == true)
            {
                await CreateAndroidFilesAsync(_fileName, _folder);
            }
        }

        private async Task CreateAndroidFilesAsync(string fileName, StorageFolder folder)
        {
            var androidFolder = await folder.CreateFolderAsync("Android", CreationCollisionOption.ReplaceExisting);

            _lowResult = await CreateAndroidFileAsync(androidFolder, fileName, ImageTypes.LDPI);
            await CreateAndroidFileAsync(androidFolder, fileName, ImageTypes.MDPI);
            _mediumResult = await CreateAndroidFileAsync(androidFolder, fileName, ImageTypes.HDPI);
            await CreateAndroidFileAsync(androidFolder, fileName, ImageTypes.XHDPI);
            await CreateAndroidFileAsync(androidFolder, fileName, ImageTypes.XXHDPI);
            _highResult = await CreateAndroidFileAsync(androidFolder, fileName, ImageTypes.XXXHDPI);
        }

        private async Task<StorageFile> CreateAndroidFileAsync(StorageFolder androidFolder, string fileName, ImageTypes imageType)
        {
            var imageTypeName = ImageTypes.GetName(typeof(ImageTypes), imageType);
            var androidDrawableFolder = await androidFolder.CreateFolderAsync(imageTypeName, CreationCollisionOption.ReplaceExisting);
            var androidImageFile = await androidDrawableFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            await ResizeImageAsync(_file, androidImageFile, imageType);
            await SaveStorageFileAsync(androidImageFile);

            return androidImageFile;
        }

        private async Task SaveStorageFileAsync(StorageFile destinationFile, StorageFile file)
        {
            byte[] buffer;
            Stream stream = await file.OpenStreamForReadAsync();
            buffer = new byte[stream.Length];
            await stream.ReadAsync(buffer, 0, (int)stream.Length);

            await FileIO.WriteBytesAsync(destinationFile, buffer);
        }

        private async Task SaveStorageFileAsync(StorageFile file)
        {
            byte[] buffer;
            Stream stream = await file.OpenStreamForReadAsync();
            buffer = new byte[stream.Length];
            await stream.ReadAsync(buffer, 0, (int)stream.Length);

            await FileIO.WriteBytesAsync(file, buffer);
        }

        private async Task<Size> GetAndroidImageSizeAsycn(ImageTypes imageType)
        {
            var imageProperties = await _file.Properties.GetImagePropertiesAsync();
            var imageSize = new Size(imageProperties.Width, imageProperties.Height);

            var size = Size.Empty;
            var factor = 1.0f;
            switch (imageType)
            {
                case ImageTypes.LDPI:
                    factor = 0.75f / 4.0f;
                    break;

                case ImageTypes.MDPI:
                    factor = 1.0f / 4.0f;
                    break;

                case ImageTypes.HDPI:
                    factor = 1.5f / 4.0f;
                    break;

                case ImageTypes.XHDPI:
                    factor = 2.0f / 4.0f;
                    break;

                case ImageTypes.XXHDPI:
                    factor = 3.0f / 4.0f;
                    break;

                case ImageTypes.XXXHDPI:
                    factor = 4.0f / 4.0f;
                    break;
            }

            size.Height = imageSize.Height * factor;
            size.Width = imageSize.Width * factor;

            return size;
        }

        private async Task<StorageFile> ResizeImageAsync(StorageFile sourceFile, StorageFile destinationFile, ImageTypes imageType)
        {
            using (var sourceStream = await sourceFile.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(sourceStream);

                var scaledSize = await GetAndroidImageSizeAsycn(imageType);
                BitmapTransform transform = new BitmapTransform() { ScaledHeight = (uint)scaledSize.Height, ScaledWidth = (uint)scaledSize.Width };
                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Rgba8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.DoNotColorManage);

                using (var destinationStream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var bitmapEncoderId = GetBitmapEncoderIdBasedOnExtension(sourceFile.Name);
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(bitmapEncoderId, destinationStream);

                    var detachedPixelData = pixelData.DetachPixelData();
                    encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Premultiplied, (uint)scaledSize.Width, (uint)scaledSize.Height, 96, 96, detachedPixelData);
                    await encoder.FlushAsync();
                }

                return destinationFile;
            }
        }

        private Guid GetBitmapEncoderIdBasedOnExtension(string filename)
        {
            var bitmapEncoder = BitmapEncoder.PngEncoderId;

            var extension = Path.GetExtension(filename);
            switch(extension)
            {
                case ".png":
                    bitmapEncoder = BitmapEncoder.PngEncoderId;
                    break;

                case ".jpg":
                    bitmapEncoder = BitmapEncoder.JpegEncoderId;
                    break;
            }

            return bitmapEncoder;
        }

        private async Task SaveFileAsync(StorageFolder folder, string filename)
        {
            await folder.CreateFileAsync(filename);
        }

        private async Task<StorageFolder> CreateFolderForImagesAsync()
        {
            var picFolder = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            var yourFolder = await picFolder.RequestAddFolderAsync();

            return yourFolder;
        }
    }
}
