using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace MapGame.Core.Utils.Graphic
{
    public interface ITexturesGenerator
    {
        protected const int SdfScale = GraphicContext.SdfScale;
        public static abstract void Initialize();
        protected static abstract void RefreshDirtyRect(Int32Rect dirtyRect);
    }
}
