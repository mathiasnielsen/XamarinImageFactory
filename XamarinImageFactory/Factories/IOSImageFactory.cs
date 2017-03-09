using XamarinImageFactory.Models;

namespace XamarinImageFactory.Factories
{
    public class IOSImageFactory
    {
        public IOSAssetsResult CreateIOSResult()
        {
            var result = new IOSAssetsResult();

            result.Image100 = new ImageResult();
            result.Image200 = new ImageResult();
            result.Image300 = new ImageResult();

            return result;
        }
    }
}
