﻿
//
//  Button Class
//
//  A Text object that provides event handlers for when
//  the user both hovers and clicks in the region of the
//  GameObject's collision body.
//

using System;
using System.IO;

namespace Breakout.Utility
{
    class Button : Text
    {
        // event callbacks
        protected Action onHover;
        protected Action onClick;

        private bool clicked;
        private bool hovered;

        private bool sound;
        private Stream soundFile;

        private bool enabled;

        public Button(float x, float y, string text, Action onClick = null, Action onHover = null, bool enable = true, Stream soundFile = null)
            : base(x, y, text)
        {
            // initalize fields
            clicked = hovered = false;
            enabled = enable;

            if (!(soundFile is null))
            {
                sound = true;
                this.soundFile = soundFile;
            }
            else sound = false;

            this.onHover = onHover is null ? () => Console.WriteLine("hovered!") : onHover;
            this.onClick = onClick is null ? () => Console.WriteLine("clicked!") : onClick;
        }

        public override void Update()
        {
            if (enabled)
            {
                // call hover event handler
                if (isHovered())
                {
                    // if not yet hovered
                    if (!hovered)
                    {
                        hovered = true;
                        onHover();
                    }
                }
                else hovered = false;

                // call clicked event hander
                if (isClicked())
                {
                    // if not yet clicked
                    if (!clicked)
                    {
                        if (sound) BreakoutGame.PlaySound(soundFile);
                        clicked = true;
                        onClick();
                    }
                }
                else clicked = false;
            }
        }

        // enabling and disabling of button
        public bool Enable() => enabled = true;
        public bool Disable() => enabled = false;

        // checks if mouse is within the button AABB
        private bool isHovered()
            => Screen.MouseX / BreakoutGame.Scale > X
            && Screen.MouseX / BreakoutGame.Scale < X + Width
            && Screen.MouseY / BreakoutGame.Scale > Y
            && Screen.MouseY / BreakoutGame.Scale < Y + Height;

        // check if the mouse is hovering and clicking
        private bool isClicked()
            => isHovered() && Screen.MouseDown;
    }
}
