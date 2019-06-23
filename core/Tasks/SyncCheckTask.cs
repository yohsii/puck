using puck.core.Base;
using puck.core.Constants;
using puck.core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace puck.core.Tasks
{
    class SyncCheckTask:BaseTask
    {
        public SyncCheckTask() {
            this.Recurring = true;
            this.IntervalSeconds = 10;
            this.RunOn = DateTime.Now;
        }
        public override void Run(CancellationToken t)
        {
            base.Run(t);
            var repo = PuckCache.PuckRepo;
            var lastSyncMeta = repo.GetPuckMeta().Where(x=>x.Name==DBNames.SyncId && x.Key==ApiHelper.ServerName()).FirstOrDefault();
            if (lastSyncMeta == null) return;
            var syncId = int.Parse(lastSyncMeta.Value);
            if (repo.GetPuckInstruction().Any(x => x.Id > syncId && x.ServerName != ApiHelper.ServerName()))
                PuckCache.ShouldSync = true;
        }
    }
}
