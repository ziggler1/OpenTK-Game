﻿using System.Collections.Generic;
using Game.Portals;
using OpenTK;
using OpenTK.Graphics;
using Game.Common;
using Game.Models;

namespace Game.Rendering
{
    public interface IRenderLayer
    {
        List<IRenderable> Renderables { get; }
        List<IPortalRenderable> Portals { get; }
        ICamera2 Camera { get; }
        bool RenderPortalViews { get; }
    }

    public static class IRenderLayerEx
    {
        public static void DrawText(this IRenderLayer layer, Font font, Vector2 position, string text)
        {
            layer.Renderables.Add(new TextEntity(font, position, text));
        }

        public static void DrawRectangle(this IRenderLayer layer, Vector2 topLeft, Vector2 bottomRight, Color4 color = new Color4())
        {
            var renderable = new Renderable(new Transform2((topLeft + bottomRight) / 2));
            var plane = ModelFactory.CreatePlaneMesh(topLeft, bottomRight, color);
            renderable.Models.Add(new Model(plane));

            layer.Renderables.Add(renderable);
        }
    }
}
