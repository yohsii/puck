using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using puck.core.Entities;

namespace puck.core.Abstract
{
    public interface I_Puck_Repository
    {
        IQueryable<GeneratedModel> GetGeneratedModel();
        IQueryable<GeneratedProperty> GetGeneratedProperty();
        IQueryable<GeneratedAttribute> GetGeneratedAttribute();
        
        void AddGeneratedModel(GeneratedModel gm);
        void AddGeneratedProperty(GeneratedProperty gm);
        void AddGeneratedAttribute(GeneratedAttribute gm);
        
        void DeleteGeneratedModel(GeneratedModel gm);
        void DeleteGeneratedProperty(GeneratedProperty gm);
        void DeleteGeneratedAttribute(GeneratedAttribute gm);
        
        IQueryable<PuckMeta> GetPuckMeta();
        void DeleteMeta(string name, string key, string value);
        void DeleteMeta(PuckMeta meta);
        void AddMeta(PuckMeta meta);
        IQueryable<PuckRevision> GetPuckRevision();
        void DeleteRevision(PuckRevision meta);
        void AddRevision(PuckRevision meta);
        IQueryable<PuckRevision> CurrentRevisionsByPath(string path);
        IQueryable<PuckRevision> CurrentRevisionsByDirectory(string path);
        IQueryable<PuckRevision> CurrentRevisionParent(string path);
        IQueryable<PuckRevision> CurrentRevisionAncestors(string path);
        IQueryable<PuckRevision> CurrentRevisionDescendants(string path);
        IQueryable<PuckRevision> CurrentRevisionChildren(string path);
        IQueryable<PuckRevision> CurrentRevisionVariants(Guid id, string variant);
        PuckRevision CurrentRevision(Guid id, string variant);
        void SaveChanges();        
    }
}
