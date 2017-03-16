using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace XamarinImageFactory.Tools
{
    public class ImageFileManipulator
    {
        public async Task<StorageFile> ResizeImageAsync(StorageFile sourceFile, StorageFile destinationFile, Size scaledSize)
        {
            using (var sourceStream = await sourceFile.OpenAsync(FileAccessMode.Read))
            {
                var decoder = await BitmapDecoder.CreateAsync(sourceStream);

                var transform = new BitmapTransform()
                {
                    ScaledHeight = (uint)scaledSize.Height,
                    ScaledWidth = (uint)scaledSize.Width,
                    InterpolationMode = BitmapInterpolationMode.Fant
                };

                var pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Rgba8,
                    BitmapAlphaMode.Premultiplied,
                    transform,
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.DoNotColorManage);

                using (var destinationStream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var bitmapEncoderId = GetBitmapEncoderIdBasedOnExtension(sourceFile.Name);
                    var encoder = await BitmapEncoder.CreateAsync(bitmapEncoderId, destinationStream);

                    var detachedPixelData = pixelData.DetachPixelData();
                    var dpi = 96;

                    encoder.SetPixelData(
                        BitmapPixelFormat.Rgba8,
                        BitmapAlphaMode.Premultiplied,
                        (uint)scaledSize.Width,
                        (uint)scaledSize.Height,
                        dpi,
                        dpi,
                        detachedPixelData);

                    await encoder.FlushAsync();
                }

                return destinationFile;
            }
        }

        private Guid GetBitmapEncoderIdBasedOnExtension(string filename)
        {
            var bitmapEncoder = BitmapEncoder.PngEncoderId;

            var extension = Path.GetExtension(filename);
            switch (extension)
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
    }
}
