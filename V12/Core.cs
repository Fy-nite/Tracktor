using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Godot;
namespace V12
{
    public class V12Engine
    {
        public List<V12World> Worlds { get; set; } = new();
        public List<Action> LoopCallbacks { get; set; }
        public bool IsEngineRunning { get; set; } = false;
        public double Delta;

        public V12Engine()
        {
            LoopCallbacks = new List<Action>();
        }

        public void Update(double delta)
        {
            Delta = delta;
            // Update all worlds
            foreach (var world in Worlds)
            {
                //GD.Print(world.Name);
                world.Update(delta);
            }
            // Execute all registered loop callbacks for the engine frame update
            foreach (var callback in LoopCallbacks)
            {
                callback();
            }
        }

        public void Run()
        {
            GD.Print("Engine started");
            IsEngineRunning = true;
        }

        public void Stop()
        {
            IsEngineRunning = false;
        }
    }
}
