using XamarinImageFactory.Models;

namespace XamarinImageFactory.Factories
{
    public class IOSImageFactory
    {
        public IOSAssetsResult CreateIOSResult()
        {
            var result = new IOSAssetsResult();

            result.Normal = new ImageResult();
            result.Twice = new ImageResult();
            result.Triple = new ImageResult();

            return result;
        }
    }
}
