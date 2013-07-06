using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Abstract;
using System.Timers;
using System.Threading;
using puck.core.Helpers;
using System.Threading.Tasks;
using System.Web.Hosting;
using puck.core.Base;
using puck.core.Events;
using puck.core.Constants;
namespace puck.core.Concrete
{
    public class Dispatcher:I_Task_Dispatcher
    {
        public Dispatcher() {
            HostingEnvironment.RegisterObject(this);
            CatchUp=PuckCache.TaskCatchUp;
            Start();
        }
        System.Timers.Timer tmr;
        private static object lck= new object();
        int lock_wait = 100;
        int interval = 1000;
        public bool CatchUp {get;set;}
        public List<BaseTask> Tasks { get ;set;}
        private CancellationTokenSource groupTokenSource;
        public event EventHandler<DispatchEventArgs> TaskEnd;
        protected void OnTaskEnd(object s, DispatchEventArgs args) {
            if (TaskEnd != null)
                TaskEnd(s,args);
        }
        public CancellationTokenSource GroupTokenSource { get{return groupTokenSource;} }
        public void Start() {
            if (Tasks == null)
                Tasks = new List<BaseTask>();
            //setup global cancel token for all tasks
            groupTokenSource = new CancellationTokenSource();
            //set up timer
            tmr = new System.Timers.Timer();
            tmr.Interval = interval;
            tmr.Elapsed += Dispatch;
            tmr.Enabled = true;
            tmr.AutoReset = true;
        }
        public void OnTaskEnd(BaseTask t){
            //remove one off events
            if (!t.Recurring) {
                Tasks.Remove(t);
            }
            OnTaskEnd(this, new DispatchEventArgs() {Task=t });
        }
        public void Dispatch(object sender, EventArgs e) { 
            //dispatch registered tasks, if dispatching already, return. * shouldn't happen if sensible interval set
            bool taken=false;
            try
            {
                Monitor.TryEnter(lck, lock_wait, ref taken);
                if (!taken)
                    return;

                foreach (var t in Tasks) {
                    if(ShouldRunNow(t))
                        System.Threading.Tasks.Task.Factory.StartNew(() => { t.Run(groupTokenSource.Token); t.LastRun = DateTime.Now; }, groupTokenSource.Token)
                            .ContinueWith(x=>OnTaskEnd(t));
                };
            }
            finally {
                if (taken)
                    Monitor.Exit(lck);
            }
        }
        public bool ShouldRunNow(BaseTask t) {
            return (t.Recurring && ((t.LastRun == null && DateTime.Now > t.RunOn) || (t.LastRun.HasValue && DateTime.Now > t.LastRun.Value.AddSeconds(t.IntervalMinutes))))
                || (!t.Recurring && DateTime.Now > t.RunOn);                        
        }
        public bool CanRun(BaseTask t) {
            return (t.Recurring || (!t.Recurring && (t.RunOn>DateTime.Now ||(CatchUp&&t.LastRun==null))));
        }
        public void Stop(bool immediate) {
            tmr.Stop();
            groupTokenSource.Cancel();
            Tasks.Clear();
            HostingEnvironment.UnregisterObject(this);
        }

    }
}
