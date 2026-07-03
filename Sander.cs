using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Sander
{
    public sealed class SanderMod : IEntitySystem
    {
        public IEnumerable<Type>? UpdatesAfter => null;
        public IEnumerable<Type>? UpdatesBefore => null;
        public bool UpdatesOutsidePrediction => false;

        public void Initialize()
        {
            Logger.Info("[Sander] Mod initialized");
        }

        public void Shutdown()
        {
            Logger.Info("[Sander] Mod shutdown");
        }

        public void Update(float frameTime)
        {
        }

        public void FrameUpdate(float frameTime)
        {
        }
    }
}