using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XamarinImageFactory.Models;

namespace XamarinImageFactory.Factories
{
    public class AndroidImageFactory
    {
        public AndroidDrawablesResult CreateAndroidDrawables()
        {
            var drawableResults = new AndroidDrawablesResult();

            drawableResults.LDPI = new ImageResult();
            drawableResults.MDPI = new ImageResult();
            drawableResults.HDPI = new ImageResult();
            drawableResults.XHDPI = new ImageResult();
            drawableResults.XXHDPI = new ImageResult();
            drawableResults.XXXHDPI = new ImageResult();

            return drawableResults;
        }
    }
}
