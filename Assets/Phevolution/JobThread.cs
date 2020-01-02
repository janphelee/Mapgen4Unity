using System.Diagnostics;
using System;
using System.Threading;

namespace Phevolution
{
    public class JobThread
    {
        public IChanged[] config { get; protected set; }
        public int vInt(int i) => ((ChangeI)config[i]).v;
        public bool vBool(int i) => ((ChangeB)config[i]).v;
        public float vFloat(int i) => ((ChangeF)config[i]).v;
        public string vString(int i) => ((ChangeS)config[i]).v;

        protected bool working { get; private set; }
        protected bool waitJob { get; private set; }

        public void processAsync(Action<long> callback)
        {
            if (working)
            {
                waitJob = true;
                return;
            }

            working = true;
            waitJob = false;

            new Thread(new ThreadStart(() =>
            {
                process(t =>
                {
                    callback(t);
                    working = false;
                    if (waitJob)
                    {
                        beforeNextJob();
                        processAsync(callback);
                    }
                });
            })).Start();
        }

        protected virtual void beforeNextJob()
        {

        }

        protected virtual void process(Action<long> callback)
        {
            var watcher = new Stopwatch();
            watcher.Start();

            watcher.Stop();
            if (callback != null) callback.Invoke(watcher.ElapsedMilliseconds);
        }
    }
}