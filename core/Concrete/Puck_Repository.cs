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

        public IQueryable<PuckInstruction> GetPuckInstruction()
        {
            return repo.PuckInstruction;
        }

        public void AddPuckInstruction(PuckInstruction pi)
        {
            repo.PuckInstruction.Add(pi);
        }

        public void DeletePuckInstruction(PuckInstruction pi)
        {
            repo.PuckInstruction.Remove(pi);
        }
        public IQueryable<PuckUser> GetPuckUser()
        {
            return repo.Users;
        }

        public IQueryable<PuckMeta> GetPuckMeta() {
            return repo.PuckMeta;
        }

        public IQueryable<GeneratedModel> GetGeneratedModel()
        {
            return repo.GeneratedModel;
        }

        public IQueryable<GeneratedProperty> GetGeneratedProperty()
        {
            return repo.GeneratedProperty;
        }

        public IQueryable<GeneratedAttribute> GetGeneratedAttribute()
        {
            return repo.GeneratedAttribute;
        }
               
        
        public void AddGeneratedModel(GeneratedModel gm) {
            repo.GeneratedModel.Add(gm);            
        }

        public void AddGeneratedProperty(GeneratedProperty gp)
        {
            repo.GeneratedProperty.Add(gp);
        }

        public void AddGeneratedAttribute(GeneratedAttribute ga)
        {
            repo.GeneratedAttribute.Add(ga);
        }
                
        public void DeleteGeneratedModel(GeneratedModel gm)
        {
            repo.GeneratedModel.Remove(gm);
        }

        public void DeleteGeneratedProperty(GeneratedProperty gp)
        {
            repo.GeneratedProperty.Remove(gp);
        }

        public void DeleteGeneratedAttribute(GeneratedAttribute ga)
        {
            repo.GeneratedAttribute.Remove(ga);
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
            var results = repo.PuckRevision
                .Where(x => x.Path.ToLower().Equals(path.ToLower()) && x.Current);
            return results;
        }
        public IQueryable<PuckRevision> CurrentRevisionsByParentId(Guid parentId)
        {
            var results = repo.PuckRevision
                .Where(x => x.ParentId==parentId&&x.Current);
            return results;
        }
        public IQueryable<PuckRevision> CurrentRevisionsByDirectory(string path)
        {
            var pathCount = path.Count(x => x == '/');
            var results = repo.PuckRevision
                .Where(x => x.Path.ToLower().StartsWith(path.ToLower()) && x.Path.Length - x.Path.Replace("/", "").Length == pathCount && x.Current);
            return results;
        }
        public IQueryable<PuckRevision> CurrentRevisionAncestors(string path)
        {
            if (path.Count(x => x == '/') <= 1)
                return Enumerable.Empty<PuckRevision>().AsQueryable();
            var parentPaths = new string[path.Count(x => x == '/')-1];
            var i = 0;
            while (path.Count(x => x == '/') > 1)
            {
                path = path.Substring(0, path.LastIndexOf('/'));
                parentPaths[i] = path.ToLower();
                i++;
            }
            return repo.PuckRevision.Where(x =>parentPaths.Contains(x.Path.ToLower()) && x.Current);
        }
        public List<PuckRevision> CurrentRevisionAncestors(Guid id,bool includeSelf=false)
        {
            var currentRevision = repo.PuckRevision.FirstOrDefault(x=>x.Id==id&&x.Current);
            if (currentRevision.ParentId==Guid.Empty)
                return Enumerable.Empty<PuckRevision>().ToList();
            var results = new List<PuckRevision>();
            if(includeSelf)
                results.Add(currentRevision);
            while (currentRevision.ParentId!=Guid.Empty)
            {
                currentRevision = repo.PuckRevision.FirstOrDefault(x=>x.Id==currentRevision.ParentId&&x.Current);
                results.Add(currentRevision);                
            }
            results.Reverse();
            return results;
        }
        public IQueryable<PuckRevision> CurrentRevisionParent(Guid id)
        {
            var node = repo.PuckRevision.FirstOrDefault(x=>x.Id==id&&x.Current);
            if (node==null)
                return Enumerable.Empty<PuckRevision>().AsQueryable();
            return repo.PuckRevision.Where(x => x.Id==node.ParentId && x.Current);
        }
        public IQueryable<PuckRevision> CurrentRevisionParent(string path)
        {
            if (path.Count(x => x == '/') <= 1)
                return Enumerable.Empty<PuckRevision>().AsQueryable();
            string searchPath = path.Substring(0, path.LastIndexOf('/'));
            return repo.PuckRevision.Where(x => x.Path.ToLower().Equals(searchPath.ToLower()) && x.Current);
        }
        public IQueryable<PuckRevision> CurrentRevisionChildren(Guid id)
        {
            return repo.PuckRevision.Where(x => x.ParentId == id && x.Current);            
        }
        public IQueryable<PuckRevision> CurrentRevisionChildren(string path)
        {
            if (!path.EndsWith("/"))
                path = path + "/";
            return CurrentRevisionsByDirectory(path);
        }
        /*
        public IQueryable<PuckRevision> CurrentRevisionDescendants(string path)
        {
            if (!path.EndsWith("/"))
                path = path + "/";
            var results = repo.PuckRevision
                .Where(x => x.Path.ToLower().StartsWith(path.ToLower()) && x.Current);
            return results;
        }
        */
        public IQueryable<PuckRevision> CurrentRevisionDescendants(string idPath)
        {
            idPath = idPath += ",";
            var results = repo.PuckRevision
                .Where(x => x.IdPath.ToLower().StartsWith(idPath.ToLower()) && x.Current);
            return results;
        }
        public IQueryable<PuckRevision> PublishedDescendants(string idPath)
        {
            idPath = idPath += ",";
            var results = repo.PuckRevision
                .Where(x => x.IdPath.ToLower().StartsWith(idPath.ToLower()) && x.IsPublishedRevision);
            return results;
        }
        public IQueryable<PuckRevision> CurrentOrPublishedDescendants(string idPath)
        {
            idPath = idPath += ",";
            var results = repo.PuckRevision
                .Where(x => x.IdPath.ToLower().StartsWith(idPath.ToLower()) && ((x.Current&&x.HasNoPublishedRevision) || x.IsPublishedRevision));
            return results;
        }
        public IQueryable<PuckRevision> CurrentRevisionVariants(Guid id,string variant)
        {
            var results = repo.PuckRevision
                .Where(x => x.Id==id && x.Current && !x.Variant.ToLower().Equals(variant.ToLower()));
            return results;
        }
        public IQueryable<PuckRevision> PublishedRevisionVariants(Guid id, string variant)
        {
            var results = repo.PuckRevision
                .Where(x => x.Id == id && x.IsPublishedRevision && !x.Variant.ToLower().Equals(variant.ToLower()));
            return results;
        }
        public IQueryable<PuckRevision> PublishedRevisions(Guid id)
        {
            var results = repo.PuckRevision
                .Where(x => x.Id == id && x.IsPublishedRevision);
            return results;
        }
        public PuckRevision CurrentRevision(Guid id, string variant)
        {
            var results = repo.PuckRevision
                .Where(x => x.Id == id && x.Current && x.Variant.ToLower().Equals(variant.ToLower()));
            return results.FirstOrDefault();
        }
        public PuckRevision PublishedRevision(Guid id, string variant)
        {
            var results = repo.PuckRevision
                .Where(x => x.Id == id && x.IsPublishedRevision && x.Variant.ToLower().Equals(variant.ToLower()));
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
