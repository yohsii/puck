using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using puck.core.Abstract;
using puck.areas.admin.Models;
using puck.core.Base;
using System.IO;
namespace puck.Transformers
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class PuckImageTransformer : Attribute, I_Property_Transformer<PuckImage, PuckImage>
    {
        public PuckImage Transform(BaseModel m,string propertyName,string ukey,PuckImage p)
        {
            try
            {
                if (p.File == null || string.IsNullOrEmpty(p.File.FileName))
                    return null;
            
                string filepath = string.Concat("~/Media/", m.Id, "/", m.Variant, "/", ukey, "_", p.File.FileName);
                string absfilepath =HttpContext.Current.Server.MapPath(filepath);
                new FileInfo(absfilepath).Directory.Create();
                p.File.SaveAs(absfilepath);
                p.Path = filepath.TrimStart('~');
                p.Size = p.File.InputStream.Length.ToString();
                p.Extension=Path.GetExtension(p.File.FileName);                
            }catch(Exception ex){
                
            }finally {
                p.File = null;
            }
            return p;
        }
    }    
}