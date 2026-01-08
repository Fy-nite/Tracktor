using Godot;
using System;
using System.Collections.Generic;

namespace V12.Bindings
{
    public partial class MainThreadDispatcher : Node
    {
        private readonly Queue<Action> _queue = new();

        public void EnqueueAction(Action action)
        {
            lock (_queue)
            {
                _queue.Enqueue(action);
            }
            // Schedule processing on the main thread
            CallDeferred("ProcessQueueActions");
        }

        public void ProcessQueueActions()
        {
            while (true)
            {
                Action? a = null;
                lock (_queue)
                {
                    if (_queue.Count == 0) break;
                    a = _queue.Dequeue();
                }
                if (a != null)
                {
                    try { a(); } catch (Exception ex) { GD.PrintErr($"[MainThreadDispatcher] ProcessQueue error: {ex.Message}"); }
                }
            }
        }
    }
}
