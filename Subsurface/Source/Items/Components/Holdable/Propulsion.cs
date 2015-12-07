﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using FarseerPhysics;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Barotrauma.Particles;

namespace Barotrauma.Items.Components
{
    class Propulsion : ItemComponent
    {
        private float force;

        private string particles;

        private ParticlePrefab.DrawTargetType usableIn;
                
        [HasDefaultValue(0.0f, false)]
        public float Force
        {
            get { return force; }
            set { force = value; }
        }
        
        [HasDefaultValue("", false)]
        public string Particles
        {
            get { return particles; }
            set { particles = value; }
        }

        public Propulsion(Item item, XElement element)
            : base(item,element)
        {
            switch (ToolBox.GetAttributeString(element, "usablein", "air").ToLower())
            {
                case "air":
                    usableIn = ParticlePrefab.DrawTargetType.Air;
                    break;
                case "water":
                    usableIn = ParticlePrefab.DrawTargetType.Water;
                    break;
                default:
                    usableIn = ParticlePrefab.DrawTargetType.Both;
                    break;
            }
        }
        
        public override bool Use(float deltaTime, Character character = null)
        {
            if (character == null) return false;
            if (!character.IsKeyDown(InputType.Aim)) return false;

            if (item.InWater)
            {
                if (usableIn == ParticlePrefab.DrawTargetType.Air) return true;
            }
            else
            {
                if (usableIn == ParticlePrefab.DrawTargetType.Water) return true;
            }

            Vector2 dir = Vector2.Normalize(character.CursorPosition - character.Position);

            Vector2 propulsion = dir * force;

            if (character.AnimController.InWater) character.AnimController.TargetMovement = dir;

            if (item.body.Enabled && false)
            {
                item.body.ApplyForce(propulsion);
            }
            else
            {
                foreach (Limb limb in character.AnimController.Limbs)
                {
                    if (limb.WearingItem==null || limb.WearingItem.Item != item) continue;

                    limb.body.ApplyForce(propulsion);
                }

                if (character.SelectedItems[0] == item) character.AnimController.GetLimb(LimbType.RightHand).body.ApplyForce(propulsion);

                if (character.SelectedItems[1] == item) character.AnimController.GetLimb(LimbType.LeftHand).body.ApplyForce(propulsion);
            }

            if (!string.IsNullOrWhiteSpace(particles))
            {
                GameMain.ParticleManager.CreateParticle(particles, item.Position,
                    item.body.Rotation + ((item.body.Dir > 0.0f) ? 0.0f : MathHelper.Pi), 0.0f, item.CurrentHull);
            }

            return true;
        }
        
        public override void Draw(SpriteBatch spriteBatch, bool editing)
        {
            if (!IsActive) return;

            //Vector2 particleSpeed =  new Vector2(
            //    (float)Math.Cos(item.body.Rotation),
            //    (float)Math.Sin(item.body.Rotation)) *item.body.Dir * 0.1f;



            
            //Vector2 startPos = ConvertUnits.ToDisplayUnits(item.body.Position);
            //Vector2 endPos = ConvertUnits.ToDisplayUnits(pickedPosition);
            //endPos = new Vector2(endPos.X + Game1.localRandom.Next(-2, 2), endPos.Y + Game1.localRandom.Next(-2, 2));

            //GUI.DrawLine(spriteBatch, startPos, endPos, Color.Orange, 0.0f);

            IsActive = false;
        }


    }
}