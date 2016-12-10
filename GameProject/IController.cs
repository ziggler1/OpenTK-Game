﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public interface IController
    {
        Size CanvasSize { get; }
        IInput Input {get;}
    }
}