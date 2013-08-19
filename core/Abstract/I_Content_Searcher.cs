﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;
using puck.core.Base;


namespace puck.core.Abstract
{
    public interface I_Content_Searcher
    {
        IList<Dictionary<string, string>> Query(Query query);
        IList<Dictionary<string, string>> Query(string query);
        IList<Dictionary<string, string>> Query(string query,string typeName);
        IList<T> Query<T>(string query) where T:BaseModel;
        IList<T> QueryNoCast<T>(string query) where T:BaseModel;
        IList<T> Query<T>(string query,Filter filter,Sort sort,out int total,int limit,int skip) where T:BaseModel;
        IList<T> QueryNoCast<T>(string query,Filter filter,Sort sort,out int total,int limit,int skip) where T:BaseModel;
        IList<T> Get<T>(int limit);
        IList<T> Get<T>();
    }
}
