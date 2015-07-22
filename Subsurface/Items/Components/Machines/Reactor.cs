﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Subsurface.Networking;

namespace Subsurface.Items.Components
{
    class Reactor : Powered
    {
        //the rate at which the reactor is being run un
        //higher rates generate more power (and heat)
        float fissionRate;

        //the rate at which the heat is being dissipated
        float coolingRate;

        float temperature;

        //is automatic temperature control on
        //(adjusts the cooling rate automatically to keep the
        //amount of power generated balanced with the load)
        bool autoTemp;

        //the temperature after which fissionrate is automatically 
        //turned down and cooling increased
        float shutDownTemp;

        float meltDownTemp;

        //how much power is provided to the grid per 1 temperature unit
        float powerPerTemp;

        int graphSize = 25;

        float graphTimer;

        int updateGraphInterval = 500;

        float[] fissionRateGraph;
        float[] coolingRateGraph;
        float[] tempGraph;

        private PropertyTask powerUpTask;

        [Editable, HasDefaultValue(9500.0f, true)]
        public float MeltDownTemp
        {
            get { return meltDownTemp; }
            set 
            {
                meltDownTemp = Math.Max(0.0f, value);
            }
        }

        [Editable, HasDefaultValue(1.0f, true)]
        public float PowerPerTemp
        {
            get { return powerPerTemp; }
            set
            {
                powerPerTemp = Math.Max(0.0f, value);
            }
        }

        public float FissionRate
        {
            get { return fissionRate; }
            set { fissionRate = MathHelper.Clamp(value, 0.0f, 100.0f); }
        }

        public float CoolingRate
        {
            get { return coolingRate; }
            set { coolingRate = MathHelper.Clamp(value, 0.0f, 100.0f); }
        }

        public float Temperature
        {
            get { return temperature; }
            set { temperature = MathHelper.Clamp(value, 0.0f, 10000.0f); }
        }

        public bool IsRunning()
        {
            return (temperature > 0.0f);
        }

        public float ExtraCooling { get; set; }

        public float AvailableFuel { get; set; }

        public Reactor(Item item, XElement element)
            : base(item, element)
        {
            fissionRateGraph = new float[graphSize];
            coolingRateGraph = new float[graphSize];
            tempGraph = new float[graphSize];

            meltDownTemp = 9000.0f;

            powerPerTemp = 1.0f;

            isActive = true;
        }

        public override void Update(float deltaTime, Camera cam) 
        {
            //ApplyStatusEffects(ActionType.OnActive, deltaTime, null);
            
            fissionRate = Math.Min(fissionRate, AvailableFuel);
            
            float heat = 100 * fissionRate;
            float heatDissipation = 50 * coolingRate + ExtraCooling;

            float deltaTemp = (((heat - heatDissipation) * 5) - temperature) / 1000.0f;
            Temperature = temperature + deltaTemp;

            if (temperature > meltDownTemp)
            {
                MeltDown();
                return;
            }
            else if (temperature==0.0f)
            {
                if (powerUpTask==null || powerUpTask.IsFinished)
                {
                    powerUpTask = new PropertyTask(item, IsRunning, 50.0f, "Power up the reactor");
                }  
            }


            item.Condition -= temperature*deltaTime*0.00005f;

            if (temperature > shutDownTemp)
            {
                CoolingRate += 0.5f;
                FissionRate -= 0.5f;
            }
            else if (autoTemp)
            {

                float load = 0.0f;

                List<Connection> connections = item.Connections;
                if (connections!=null && connections.Count>0)
                {
                    foreach (Connection connection in connections)
                    {
                        foreach (Connection recipient in connection.Recipients)
                        {
                            Item it = recipient.Item as Item;
                            if (it == null) continue;

                            PowerTransfer pt = it.GetComponent<PowerTransfer>();
                            if (pt != null) load += pt.PowerLoad;
                        }
                    }
                }

                //foreach (MapEntity e in item.linkedTo)
                //{
                //    Item it = e as Item;
                //    if (it == null) continue;

                //    PowerTransfer pt = it.GetComponent<PowerTransfer>();
                //    if (pt != null) load += pt.PowerLoad;
                //}

                fissionRate = Math.Min(load / 200.0f, shutDownTemp);
                //float target = Math.Min(targetTemp, load);
                CoolingRate = coolingRate + (temperature - Math.Min(load, shutDownTemp) + deltaTemp)*0.1f;
            }

            //fission rate can't be lowered below a certain amount if the core is too hot
            FissionRate = Math.Max(fissionRate, heat / 200.0f);


            //the power generated by the reactor is equal to the temperature
            currPowerConsumption = -temperature*powerPerTemp;

            if (item.CurrentHull != null)
            {
                //the sound can be heard from 20 000 display units away when everything running at 100%
                item.CurrentHull.SoundRange += (coolingRate + fissionRate) * 100;
            }

            UpdateGraph(deltaTime);

            ExtraCooling = 0.0f;
            AvailableFuel = 0.0f;
        }

        public override void UpdateBroken(float deltaTime, Camera cam)
        {
            base.UpdateBroken(deltaTime, cam);

            Temperature -= deltaTime * 1000.0f;
            FissionRate -= deltaTime * 10.0f;
            CoolingRate -= deltaTime * 10.0f;

            currPowerConsumption = -temperature;

            UpdateGraph(deltaTime);

            ExtraCooling = 0.0f;
        }

        private void UpdateGraph(float deltaTime)
        {
            graphTimer += deltaTime * 1000.0f;

            if (graphTimer > updateGraphInterval)
            {
                UpdateGraph(fissionRateGraph, fissionRate);
                UpdateGraph(coolingRateGraph, coolingRate);
                UpdateGraph(tempGraph, temperature);
                graphTimer = 0.0f;
            }
        }

        private void MeltDown()
        {
            if (item.Condition <= 0.0f) return;
 
            new RepairTask(item, 60.0f, "Reactor meltdown!");
            item.Condition = 0.0f;
            fissionRate = 0.0f;
            coolingRate = 0.0f;

            new Explosion(item.SimPosition, 6.0f, 500.0f, 600.0f, 10.0f, 2.0f).Explode();

            //List<Structure> structureList = new List<Structure>();

            //float dist = 600.0f;
            //foreach (MapEntity entity in MapEntity.mapEntityList)
            //{
            //    Structure structure = entity as Structure;
            //    if (structure == null) continue;

            //    if (structure.HasBody && Vector2.Distance(structure.Position, item.Position)<dist*3.0f)
            //    {
            //        structureList.Add(structure);
            //    }
            //}

            //foreach (Structure structure in structureList)
            //{
            //    for (int i = 0; i < structure.SectionCount; i++)
            //    {
            //        float damage = dist - Vector2.Distance(structure.SectionPosition(i), item.Position);
            //        if (damage > 0.0f) structure.AddDamage(i, damage);
            //    }
            //}

            //if (item.currentHull!=null)
            //{
            //    item.currentHull.WaveVel[item.currentHull.GetWaveIndex(item.SimPosition)] = 100.0f;
            //}

            if (item.ContainedItems!=null)
            {
                foreach (Item containedItem in item.ContainedItems)
                {
                    if (containedItem == null) continue;
                    containedItem.Condition = 0.0f;
                }
            }


        }

        public override bool Pick(Character picker)
        {
            if (picker == null) return false;

            //picker.SelectedConstruction = (picker.SelectedConstruction==item) ? null : item;
            
            return true;
        }

        public override void Draw(SpriteBatch spriteBatch, bool editing)
        {
            base.Draw(spriteBatch);

            GUI.DrawRectangle(spriteBatch,
                new Vector2(item.Rect.X + item.Rect.Width / 2 - 6, -item.Rect.Y + 29),
                new Vector2(12, 42), Color.Black);

            if (temperature > 0)
                GUI.DrawRectangle(spriteBatch,
                    new Vector2(item.Rect.X + item.Rect.Width / 2 - 5, -item.Rect.Y + 30 + (40.0f * (1.0f - temperature / 10000.0f))),
                    new Vector2(10, 40 * (temperature / 10000.0f)), new Color(temperature / 10000.0f, 1.0f - (temperature / 10000.0f), 0.0f, 1.0f), true);
        }


        public override void DrawHUD(SpriteBatch spriteBatch, Character character)
        {
            isActive = true;

            int width = GuiFrame.Rect.Width, height = GuiFrame.Rect.Height;
            int x = GuiFrame.Rect.X;
            int y = GuiFrame.Rect.Y;

            GuiFrame.Draw(spriteBatch);

            float xOffset = (graphTimer / (float)updateGraphInterval);

            //GUI.DrawRectangle(spriteBatch, new Rectangle(x, y, width, height), Color.Black, true);

            spriteBatch.DrawString(GUI.Font, "Temperature: " + (int)temperature + " C", new Vector2(x + 30, y + 30), Color.White);
            DrawGraph(tempGraph, spriteBatch, x + 30, y + 50, 10000.0f, xOffset);

            y += 130;

            spriteBatch.DrawString(GUI.Font, "Fission rate: " + (int)fissionRate + " %", new Vector2(x + 30, y + 30), Color.White);
            DrawGraph(fissionRateGraph, spriteBatch, x + 30, y + 50, 100.0f, xOffset);

            if (GUI.DrawButton(spriteBatch, new Rectangle(x + 280, y + 30, 40, 40), "+", true)) FissionRate += 1.0f;
            if (GUI.DrawButton(spriteBatch, new Rectangle(x + 280, y + 80, 40, 40), "-", true)) FissionRate -= 1.0f;

            y += 130;

            spriteBatch.DrawString(GUI.Font, "Cooling rate: " + (int)coolingRate + " %", new Vector2(x + 30, y + 30), Color.White);
            DrawGraph(coolingRateGraph, spriteBatch, x + 30, y + 50, 100.0f, xOffset);

            if (GUI.DrawButton(spriteBatch, new Rectangle(x + 280, y + 30, 40, 40), "+", true)) CoolingRate += 1.0f;
            if (GUI.DrawButton(spriteBatch, new Rectangle(x + 280, y + 80, 40, 40), "-", true)) CoolingRate -= 1.0f;

            y = y - 260;

            spriteBatch.DrawString(GUI.Font, "Autotemp: " + ((autoTemp) ? "ON" : "OFF"), new Vector2(x + 400, y + 30), Color.White);
            if (GUI.DrawButton(spriteBatch, new Rectangle(x + 400, y + 60, 100, 40), ((autoTemp) ? "TURN OFF" : "TURN ON"))) autoTemp = !autoTemp;

            spriteBatch.DrawString(GUI.Font, "Max temperature: " + shutDownTemp, new Vector2(x + 400, y + 150), Color.White);
            if (GUI.DrawButton(spriteBatch, new Rectangle(x + 400, y + 180, 40, 40), "+", true)) shutDownTemp += 100.0f;
            if (GUI.DrawButton(spriteBatch, new Rectangle(x + 450, y + 180, 40, 40), "-", true)) shutDownTemp -= 100.0f;

            item.NewComponentEvent(this, true);
        }

        static void UpdateGraph<T>(IList<T> graph, T newValue)
        {
            for (int i = graph.Count - 1; i > 0; i--)
            {
                graph[i] = graph[i - 1];
            }
            graph[0] = newValue;
        }

        static void DrawGraph(IList<float> graph, SpriteBatch spriteBatch, int x, int y, float maxVal, float xOffset)
        {
            int width = 200;
            int height = 100;

            float lineWidth = (float)width / (float)(graph.Count - 2);
            float yScale = (float)height / maxVal;

            GUI.DrawRectangle(spriteBatch, new Rectangle(x, y, width, height), Color.White);

            Vector2 prevPoint = new Vector2(x, y + height - (graph[1] + (graph[0] - graph[1]) * xOffset) * yScale);

            float currX = x + ((xOffset - 1.0f) * lineWidth);

            for (int i = 1; i < graph.Count - 1; i++)
            {
                currX += lineWidth;

                Vector2 newPoint = new Vector2(currX, y + height - graph[i] * yScale);

                GUI.DrawLine(spriteBatch, prevPoint, newPoint, Color.White);

                prevPoint = newPoint;
            }

            Vector2 lastPoint = new Vector2(x + width,
                y + height - (graph[graph.Count - 1] + (graph[graph.Count - 2] - graph[graph.Count - 1]) * xOffset) * yScale);

            GUI.DrawLine(spriteBatch, prevPoint, lastPoint, Color.Red);
        }

        public override void FillNetworkData(NetworkEventType type, NetOutgoingMessage message)
        {
            message.Write(autoTemp);
            message.Write(temperature);
            message.Write(shutDownTemp);

            message.Write(coolingRate);
            message.Write(fissionRate);
        }

        public override void ReadNetworkData(NetworkEventType type, NetIncomingMessage message)
        {
            bool newAutoTemp;
            float newTemperature, newShutDownTemp;
            float newCoolingRate, newFissionRate;

            try
            {
                newAutoTemp = message.ReadBoolean();
                newTemperature = message.ReadFloat();
                newShutDownTemp = message.ReadFloat();

                newCoolingRate = message.ReadFloat();
                newFissionRate = message.ReadFloat();
            }

            catch { return; }

            autoTemp = newAutoTemp;
            Temperature = newTemperature;
            shutDownTemp = newShutDownTemp;

            CoolingRate = newCoolingRate;
            FissionRate = newFissionRate;
        }
    }
}