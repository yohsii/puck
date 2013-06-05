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
namespace puck.core.Concrete
{
    public class Dispatcher:I_Task_Dispatcher
    {
        public Dispatcher() {
            HostingEnvironment.RegisterObject(this);
        }
        System.Timers.Timer tmr;
        private static object lck= new object();
        int wait = 100;
        int interval = 1000;
        private static List<I_Puck_Task> taskList = new List<I_Puck_Task>();
        public static List<I_Puck_Task> Tasks { get { return taskList; } }
        private static CancellationTokenSource groupTokenSource;
        public static CancellationTokenSource GroupTokenSource { get{return groupTokenSource;} }
        public void Start() {
            //set task list
            taskList = ApiHelper.Tasks;
            //setup global cancel token for all tasks
            groupTokenSource = new CancellationTokenSource();
            //set up timer
            tmr = new System.Timers.Timer();
            tmr.Interval = interval;
            tmr.Elapsed += Dispatch;
            tmr.Enabled = true;
            tmr.AutoReset = true;
        }
        public void AddTasks(List<I_Puck_Task> tasks) {
            //add extra tasks to list
            taskList.AddRange(tasks);            
        }
        public void OnTaskEnd(I_Puck_Task t){
            //remove one off events that have already occurred - signified by lastrun having a value and recurring flag being false
            if (!t.Recurring && t.LastRun.HasValue) {
                taskList.Remove(t);
            }
        }
        public void Dispatch(object sender, EventArgs e) { 
            //dispatch registered tasks, if dispatching already, return. * shouldn't happen if sensible interval set
            bool taken=false;
            try
            {
                Monitor.TryEnter(lck, wait, ref taken);
                if (!taken)
                    return;

                foreach (var t in taskList) {
                    if(ShouldRun(t))
                        System.Threading.Tasks.Task.Factory.StartNew(()=>t.Run(groupTokenSource.Token),groupTokenSource.Token)
                            .ContinueWith(x=>OnTaskEnd(t));
                };
            }
            finally {
                if (taken)
                    Monitor.Exit(lck);
            }
        }
        public bool ShouldRun(I_Puck_Task t) {
            if (!t.Recurring)
            {
                if (DateTime.Now >= t.RunOn && t.LastRun == null)
                    return true;
                else return false;
            }
            else{
                if (t.LastRun.HasValue && DateTime.Now >= t.LastRun.Value.AddSeconds(t.IntervalSeconds))
                    return true;
                else return false;
            }            
        }
        public void Stop(bool immediate) {
            tmr.Stop();
            groupTokenSource.Cancel();
            taskList.Clear();
            HostingEnvironment.UnregisterObject(this);
        }

    }
}
