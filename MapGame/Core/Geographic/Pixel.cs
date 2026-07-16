using System;
using System.Collections.Generic;
using System.Text;

namespace MapGame.Core.Geographic
{
    public class Pixel(int x, int y)
    {
        public int X { get; set; } = x; public int Y { get; set; } = y;
    }
}
