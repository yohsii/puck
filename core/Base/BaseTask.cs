﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using puck.core.Abstract;
using puck.core.Attributes;
using Newtonsoft.Json;
using System.Web.Mvc;
using puck.core.Events;
using puck.core.Constants;
using puck.core.State;

namespace puck.core.Base
{
    public class BaseTask
    {
        [HiddenInput(DisplayValue=false)]
        public int ID { get; set; }
        public string Name { get; set; }
        public bool Recurring { get; set; }
        public int IntervalSeconds { get; set; }
        public DateTime RunOn { get; set; }
        [HiddenInput(DisplayValue = false)]
        public DateTime? LastRun { get; set; }

        private object lck = new object();
        int lock_wait = 100;
        public event EventHandler<DispatchEventArgs> TaskEnd;
        public void DoRun(CancellationToken t) {
            bool taken = false;
            try
            {
                Monitor.TryEnter(lck, lock_wait, ref taken);
                if (!taken)
                    return;
                Run(t);
                this.LastRun = DateTime.Now;
            }
            catch (Exception ex) {
                PuckCache.PuckLog.Log(ex);
            }
            finally
            {
                try {
                    if (TaskEnd != null)
                        TaskEnd(this, new DispatchEventArgs { Task = this });
                } catch (Exception ex) {
                    PuckCache.PuckLog.Log(ex);
                }
                if (taken)
                    Monitor.Exit(lck);
            }            
        }
        public virtual void Run(CancellationToken t){

        }        
    }
}
