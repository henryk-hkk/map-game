using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace MapGame.Core.Utils.Geographic
{
    public class BorderPixelSegment
    {
        public Color Area1 { get; set; }
        public Color Area2 { get; set; }

        public List<int> PixelIndices = new List<int>();

        public Int32Rect BoundingBox;
    }
}