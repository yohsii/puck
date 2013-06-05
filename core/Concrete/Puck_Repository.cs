using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using puck.core.Abstract;
using puck.core.Entities;
namespace puck.core.Concrete
{
    public class Puck_Repository : I_Puck_Repository
    {
        public PuckContext repo = new PuckContext();

        public IQueryable<PuckMeta> GetPuckMeta() {
            return repo.PuckMeta;
        }

        public int AddMeta(PuckMeta meta) {
            repo.PuckMeta.Add(meta);
            repo.SaveChanges();
            return meta.ID;
        }

        public void DeleteMeta(PuckMeta meta) {
            repo.PuckMeta.Remove(meta);
            repo.SaveChanges();
        }

        public void DeleteMeta(string name,string key,string value)
        {
            var metas = GetPuckMeta();
            if (!string.IsNullOrEmpty(name))
                metas = metas.Where(x => x.Name.Equals(name));
            if (!string.IsNullOrEmpty(key))
                metas = metas.Where(x => x.Key.Equals(key));
            if (!string.IsNullOrEmpty(value))
                metas = metas.Where(x => x.Value.Equals(value));
            
            foreach(var meta in metas.ToList()){
                repo.PuckMeta.Remove(meta);
            }
            repo.SaveChanges();
        }

        public void SaveChanges() {
            repo.SaveChanges();
        }
    }
}
