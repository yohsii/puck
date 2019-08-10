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
using Newtonsoft.Json;
using puck.core.State;
namespace puck.core.Concrete
{
    public class Dispatcher:I_Task_Dispatcher
    {
        public Dispatcher() {
            this.QueuedTasks = new HashSet<int>();
            HostingEnvironment.RegisterObject(this);
            CatchUp=PuckCache.TaskCatchUp;
            Start();
        }
        private HashSet<int> QueuedTasks { get; set; }
        System.Timers.Timer tmr;
        private static object lck= new object();
        int lock_wait = 100;
        int interval = 2000;
        public bool CatchUp {get;set;}
        public List<BaseTask> Tasks { get ;set;}
        private CancellationTokenSource groupTokenSource;
        public event EventHandler<DispatchEventArgs> TaskEnd;
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
            
            if (TaskEnd != null)
                TaskEnd(this, new DispatchEventArgs() { Task = t });
            
        }
        public void HandleTaskEnd(object s, DispatchEventArgs e){
            QueuedTasks.Remove(e.Task.ID);
            if (!e.Task.Recurring)
            {
                Tasks.Remove(e.Task);
            }
            if ((PuckCache.UpdateTaskLastRun && !e.Task.Recurring) || (PuckCache.UpdateRecurringTaskLastRun && e.Task.Recurring))
            {
                var repo = PuckCache.PuckRepo;
                var taskMeta = repo.GetPuckMeta().Where(x => x.Name == DBNames.Tasks && x.ID == e.Task.ID).FirstOrDefault();
                if (taskMeta != null)
                {
                    taskMeta.Value = JsonConvert.SerializeObject(e.Task);
                    repo.SaveChanges();
                    repo = null;
                }
            }
        }
        public void Dispatch(object sender, EventArgs e) { 
            //dispatch registered tasks, if dispatching already, return. * shouldn't happen if sensible interval set
            bool taken=false;
            try
            {
                ((System.Timers.Timer)sender).Stop();
                Monitor.TryEnter(lck, lock_wait, ref taken);
                if (!taken)
                    return;

                foreach (var t in Tasks) {
                    if (ShouldRunNow(t)&&!QueuedTasks.Contains(t.ID))
                    {
                        QueuedTasks.Add(t.ID);
                        HostingEnvironment.QueueBackgroundWorkItem(ct =>
                        {
                            t.DoRun(ct);
                        });
                    }
                    /*System.Threading.Tasks.Task.Factory.StartNew(() => { t.Run(groupTokenSource.Token); t.LastRun = DateTime.Now; }, groupTokenSource.Token)
                            .ContinueWith(x=>OnTaskEnd(t));
                    */
                };
            }
            finally {
                if (taken)
                    Monitor.Exit(lck);
                ((System.Timers.Timer)sender).Start();
            }
        }
        public bool ShouldRunNow(BaseTask t) {
            return (t.Recurring && ((t.LastRun == null && DateTime.Now > t.RunOn) || (t.LastRun.HasValue && DateTime.Now > t.LastRun.Value.AddSeconds(t.IntervalSeconds))))
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
