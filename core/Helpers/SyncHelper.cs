using puck.core.Abstract;
using puck.core.Base;
using puck.core.Constants;
using puck.core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace puck.core.Helpers
{
    public static class SyncHelper
    {
        private static object lck = new object();
        private static int lock_wait = 1;
        public static I_Puck_Repository Repo {get{return PuckCache.PuckRepo;}}
        public static I_Content_Indexer Indexer { get { return PuckCache.PuckIndexer; } }
        public static void InitializeSync() {
            var repo = Repo;
            var serverName = ApiHelper.ServerName();
            var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.SyncId && x.Key == serverName).FirstOrDefault();
            if (meta == null) {
                if (!PuckCache.IsRepublishingEntireSite)
                {
                    HostingEnvironment.QueueBackgroundWorkItem(ct => ApiHelper.RePublishEntireSite2());
                    PuckCache.IsRepublishingEntireSite = true;
                    PuckCache.IndexingStatus = "republish entire site task queued";
                }
                var newMeta = new PuckMeta();
                newMeta.Name = DBNames.SyncId;
                newMeta.Key = ApiHelper.ServerName();
                int? maxId = repo.GetPuckInstruction().Max(x => (int?)x.Id);
                newMeta.Value = (maxId ?? 0).ToString();
                repo.AddMeta(newMeta);
                repo.SaveChanges();
            }

        }
        public static void Sync(CancellationToken ct) {
            bool taken = false;
            try {
                Monitor.TryEnter(lck, lock_wait, ref taken);
                if (!taken)
                    return;
                var repo = Repo;
                var serverName = ApiHelper.ServerName();
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.SyncId && x.Key == serverName).FirstOrDefault();
                if (meta == null)
                    return;
                var syncId = int.Parse(meta.Value);
                var instructions = repo.GetPuckInstruction().Where(x => x.Id > syncId && x.ServerName != serverName).ToList();
                if (instructions.Count == 0)
                    return;
                //dosync
                var instructionTotal = 0;
                instructions.ForEach(x => instructionTotal += x.Count);
                if (instructionTotal > PuckCache.MaxSyncInstructions)
                {
                    //todo, update settings and republish entire site
                    if (!PuckCache.IsRepublishingEntireSite)
                    {
                        PuckCache.IsRepublishingEntireSite = true;
                        ApiHelper.RePublishEntireSite2();
                    }
                }
                else
                {
                    foreach (var instruction in instructions)
                    {
                        if (instruction.InstructionKey == InstructionKeys.RepublishSite)
                        {
                            if (!PuckCache.IsRepublishingEntireSite)
                            {
                                PuckCache.IsRepublishingEntireSite = true;
                                ApiHelper.RePublishEntireSite2();
                            }
                        }
                        else if (instruction.InstructionKey == InstructionKeys.Publish)
                        {
                            var toIndex = new List<BaseModel>();
                            //instruction detail holds comma separated list of ids and variants in format id:variant,id:variant
                            var idList = instruction.InstructionDetail.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var idAndVariant in idList) {
                                var idAndVariantArr = idAndVariant.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                                var id = Guid.Parse(idAndVariantArr[0]);
                                var variant = idAndVariantArr[1];
                                var publishedRevision = repo.PublishedRevision(id,variant);
                                if (publishedRevision != null) {
                                    var model = ApiHelper.RevisionToBaseModel(publishedRevision);
                                    toIndex.Add(model);
                                }
                            }
                            Indexer.Index(toIndex);
                        }

                    }
                }
                //update syncId
                var maxInstructionId = instructions.Max(x => x.Id);
                meta.Value = maxInstructionId.ToString();
                repo.SaveChanges();
            }
            catch (Exception ex) {
                PuckCache.PuckLog.Log(ex);
            }
            finally
            {
                if (taken)
                    Monitor.Exit(lck);
                PuckCache.IsSyncQueued = false;
            }            
        }

    }
}
