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
        public WindowsAssetsResult CreateUWPIAssetsResult()
        {
            var result = new WindowsAssetsResult();

            result.Image100 = new ImageResult();
            result.Image125 = new ImageResult();
            result.Image150 = new ImageResult();
            result.Image200 = new ImageResult();
            result.Image400 = new ImageResult();

            return result;
        }
    }
}
