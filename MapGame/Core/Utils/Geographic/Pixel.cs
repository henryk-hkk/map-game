using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Utils.Geographic
{
    public class Pixel
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Pixel(int x, int y) { X = x; Y = y; }
    }
}
