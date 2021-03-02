using System;
using System.Collections.Generic;
using System.Text;

namespace ActivityReader
{
    class BaseActivity
    {
        public string Iatiidentifier { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public BaseSector Sector { get; set; }
    }
}
