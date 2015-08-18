﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Subsurface
{
    class GUITickBox : GUIComponent
    {
        GUIFrame box;
        GUITextBlock text;

        public delegate bool OnSelectedHandler(object obj);
        public OnSelectedHandler OnSelected;

        private bool selected;

        public bool Selected
        {
            get { return selected; }
            set 
            { 
                if (value == selected) return;
                selected = value;
                state = (selected) ? ComponentState.Selected : ComponentState.None;
            }
        }

        public bool Enabled
        {
            get;
            set;
        }

        public GUITickBox(Rectangle rect, string label, Alignment alignment, GUIComponent parent)
            : base(null)
        {
            if (parent != null)
                parent.AddChild(this);

            box = new GUIFrame(rect, Color.DarkGray, null, this);
            box.HoverColor = Color.Gray;
            box.SelectedColor = Color.DarkGray;

            text = new GUITextBlock(new Rectangle(rect.X + 40, rect.Y, 200, 30), label, Color.Transparent, Color.White, Alignment.TopLeft, null, this);

            Enabled = true;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (!Enabled) return;

            if (box.Rect.Contains(PlayerInput.GetMouseState.Position))
            {
                box.State = ComponentState.Hover;

                if (PlayerInput.GetMouseState.LeftButton == ButtonState.Pressed)
                {
                    box.State = ComponentState.Selected;                    
                }


                if (PlayerInput.LeftButtonClicked())
                {
                    Selected = !Selected;
                    if (OnSelected != null) OnSelected(this);
                }
            }
            else
            {
                box.State = ComponentState.None;
            }
            
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawChildren(spriteBatch);

            if (Selected)
            {
                GUI.DrawRectangle(spriteBatch, new Rectangle(box.Rect.X + 2, box.Rect.Y + 2, box.Rect.Width - 4, box.Rect.Height - 4), Color.Green * 0.8f, true);
            }
        }
    }
}