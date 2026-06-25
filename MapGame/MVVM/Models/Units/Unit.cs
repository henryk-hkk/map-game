using System;
using System.Collections.Generic;
using System.Text;
using MapGame.MVVM.Models;

namespace MapGame.MVVM.Models.Units
{
    public abstract class Unit {
        public Country Owner { get; protected set; }
    }
}
