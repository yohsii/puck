﻿using System;
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

        public void AddMeta(PuckMeta meta) {
            repo.PuckMeta.Add(meta);
            //repo.SaveChanges();
            //return meta.ID;
        }

        public void DeleteMeta(PuckMeta meta) {
            repo.PuckMeta.Remove(meta);
            //repo.SaveChanges();
        }
        public void DeleteRevision(PuckRevision revision) {
            repo.PuckRevision.Remove(revision);
        }
        public void AddRevision(PuckRevision revision) {
            repo.PuckRevision.Add(revision);
        }
        public IQueryable<PuckRevision> GetPuckRevision() {
            return repo.PuckRevision;
        }
        public IQueryable<PuckRevision> CurrentRevisionsByPath(string path) {
            var pathCount = path.Count(x => x == '/');
            var results = repo.PuckRevision
                .Where(x => x.Path.ToLower().StartsWith(path.ToLower()) && x.Path.Length - x.Path.Replace("/", "").Length == pathCount && x.Current);
            return results;
        }
        public IQueryable<PuckRevision> CurrentRevisionChildren(string path)
        {
            if (!path.EndsWith("/"))
                path = path + "/";
            return CurrentRevisionsByPath(path);
        }
        public IQueryable<PuckRevision> CurrentRevisionDescendants(string path)
        {
            if (!path.EndsWith("/"))
                path = path + "/";
            var results = repo.PuckRevision
                .Where(x => x.Path.ToLower().StartsWith(path.ToLower()) && x.Current);
            return results;
        }
        public IQueryable<PuckRevision> CurrentRevisionVariants(Guid id,string variant)
        {
            var results = repo.PuckRevision
                .Where(x => x.Id==id && x.Current && !x.Variant.ToLower().Equals(variant.ToLower()));
            return results;
        }
        public PuckRevision CurrentRevision(Guid id, string variant)
        {
            var results = repo.PuckRevision
                .Where(x => x.Id == id && x.Current && x.Variant.ToLower().Equals(variant.ToLower()));
            return results.FirstOrDefault();
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
            //repo.SaveChanges();
        }

        public void SaveChanges() {
            repo.SaveChanges();
        }
    }
}
