using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using XamarinImageFactory.Common;

namespace XamarinImageFactory.Entities
{
    public class ImageResult
    {
        public StorageFile ImageFile { get; set; }

        public ImageTypes ImageType { get; set; }
    }
}
