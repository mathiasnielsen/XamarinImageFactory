using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XamarinImageFactory.Models;

namespace XamarinImageFactory.Factories
{
    public class WindowsImageFactory
    {
        public WindowsAssetsResult CreateWindowsAssetsResult()
        {
            var result = new WindowsAssetsResult();

            result.Image100 = new ImageResult();
            result.Image140 = new ImageResult();
            result.Image180 = new ImageResult();
            result.Image240 = new ImageResult();

            return result;
        }

        public WindowsTilesResult CreateWindowsTilesResult()
        {
            var result = new WindowsTilesResult();

            result.Image100 = new ImageResult();
            result.Image125 = new ImageResult();
            result.Image150 = new ImageResult();
            result.Image200 = new ImageResult();
            result.Image400 = new ImageResult();

            return result;
        }
    }
}
