﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using puck.core.Base;
using System.Web;
using System.Threading.Tasks;
using puck.core.Abstract;
using System.Web.Mvc;
using puck.core.Concrete;
using System.Text.RegularExpressions;
using puck.core.Models;
using puck.core.Constants;
using System.Globalization;
using Newtonsoft.Json;
using Ninject;
using puck.core.Entities;
using puck.core.Exceptions;
using puck.core.Events;
using System.Web.Security;
using System.Net.Mail;
namespace puck.core.Helpers
{
    public partial class ApiHelper
    {
        public static object RevisionToModel(PuckRevision revision)
        {
            try
            {
                var model = JsonConvert.DeserializeObject(revision.Value, ConcreteType(ApiHelper.GetType(revision.Type)));
                var mod = model as BaseModel;
                mod.Path = revision.Path; mod.SortOrder = revision.SortOrder; mod.NodeName = revision.NodeName; mod.Published = revision.Published;
                return model;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static BaseModel RevisionToBaseModel(PuckRevision revision)
        {
            try
            {
                var model = JsonConvert.DeserializeObject(revision.Value, ConcreteType(ApiHelper.GetType(revision.Type)));
                var mod = model as BaseModel;
                mod.Path = revision.Path; mod.SortOrder = revision.SortOrder; mod.NodeName = revision.NodeName; mod.Published = revision.Published;
                return mod;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static BaseModel RevisionToBaseModelCast(PuckRevision revision)
        {
            try
            {
                var model = JsonConvert.DeserializeObject(revision.Value, typeof(BaseModel));
                var mod = model as BaseModel;
                mod.Path = revision.Path; mod.SortOrder = revision.SortOrder; mod.NodeName = revision.NodeName; mod.Published = revision.Published;
                return mod;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static string DirOfPath(string s)
        {
            if (s.EndsWith("/"))
                return s;
            string result = s.Substring(0, s.LastIndexOf("/") + 1);
            return result;
        }
        public static string ToVirtualPath(string p)
        {
            Regex r = new Regex(Regex.Escape(HttpContext.Current.Server.MapPath("~/")), RegexOptions.Compiled);
            p = r.Replace(p, "~/", 1).Replace("\\", "/");
            return p;
        }

        private static string DoTypeChain(Type type, string chain = "")
        {
            chain += type.FullName + " ";
            if (type.BaseType != null && type.BaseType != typeof(Object))
                chain = DoTypeChain(type.BaseType, chain);
            return chain.TrimEnd();
        }
        public static string TypeChain(Type type, string chain = "")
        {
            var result = DoTypeChain(type, chain);
            foreach (var gen in PuckCache.IGeneratedToModel)
            {
                var t = ApiHelper.GetType(gen.Key);
                result = result.Replace(gen.Value.FullName, t.FullName);
            }
            return result;
        }
        public static List<Type> BaseTypes(Type start, List<Type> result = null, bool excludeSystemObject = true)
        {
            result = result ?? new List<Type>();
            if (start.BaseType == null)
                return result;
            if (start.BaseType != typeof(Object) || !excludeSystemObject)
                result.Add(start.BaseType);
            return BaseTypes(start.BaseType, result);
        }
        public static void SetCulture(string path = null)
        {
            if (path == null)
                path = HttpContext.Current.Request.Url.AbsolutePath;
        }
        public static List<Type> TaskTypes()
        {
            return FindDerivedClasses(typeof(BaseTask), null, false).ToList();
        }
        public static List<Type> EditorSettingTypes()
        {
            return FindDerivedClasses(typeof(I_Puck_Editor_Settings)).ToList();
        }
        public static Type GetType(string assemblyQualifiedName)
        {
            var result = Type.GetType(assemblyQualifiedName);
            if (result == null)
            {
                try
                {
                    //throws exception if type not found
                    result = Type.GetType(
                        assemblyQualifiedName,
                        (name) =>
                        {
                            return AppDomain.CurrentDomain.GetAssemblies().Where(z => z.FullName == name.FullName).FirstOrDefault();
                        },
                        null,
                        true
                    );
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return result;
        }
        public static Type ConcreteType(Type t)
        {
            Type result = null;
            if (t.IsInterface)
                result = PuckCache.IGeneratedToModel[t.AssemblyQualifiedName];
            else
                result = t;
            return result;
        }
        public static object CreateInstance(Type t)
        {
            Object result = Activator.CreateInstance(ConcreteType(t));
            return result;
        }
        public static IEnumerable<Type> FindDerivedClasses(Type baseType, List<Type> excluded = null, bool inclusive = false)
        {
            excluded = excluded ?? new List<Type>();
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes()).Where(x => (x != baseType || inclusive) && baseType.IsAssignableFrom(x) && !excluded.Contains(x));
            return types;
        }
        public static List<Type> GeneratedOptions()
        {
            var types = FindDerivedClasses(typeof(I_GeneratedOption)).ToList();
            return types;
        }
        public static void Email(string from,string to,string host,string subject,string body) {
            MailMessage mail = new MailMessage(from, to);
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = host;
            mail.Subject = subject;
            mail.Body = body;
            client.Send(mail);
        }
    }
}
