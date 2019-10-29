using System;

namespace Yoda
{
    public abstract class ControllerBase : IDisposable
    {
        public void Dispose()
        {
            GC.Collect(GC.GetGeneration(this), GCCollectionMode.Forced, true);
        }
    }
}