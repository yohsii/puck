using puck.core.Abstract;
using puck.core.Helpers;
using puck.ViewModels;
using StackExchange.Profiling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace puck.Controllers
{
    public class DebuggingController : Controller
    {
        I_Puck_Repository r;
        public DebuggingController(I_Puck_Repository r)
        {
            this.r = r;
        }
        public ActionResult RepublishAll() {
            using (MiniProfiler.Current.Step("save models"))
            {
                ApiHelper.RePublishEntireSite2();
            }
            return View("~/views/home/test.cshtml");
        }
        public ActionResult QueryTest()
        {
            var count = 0;
            var dict = new Dictionary<string, bool>();
            using (MiniProfiler.Current.Step("save models"))
            {
                var rootGuid = Guid.Parse("d2710660-29f0-4fe3-b288-91f294cf45ab");
                for (var i = 0; i < 7; i++) {
                    var model1 = ApiHelper.Create<Page>(rootGuid, "en-gb","testnode"+count.ToString(),published:true);
                    model1 = PopulatePageModel(model1,count);
                    ApiHelper.SaveContent(model1);
                    dict.Add(count.ToString(), true);
                    count++;
                    
                    for (var j = 0; j < 10; j++) {
                        var model2 = ApiHelper.Create<Page>(model1.Id, "en-gb", "testnode" + count.ToString(), published: true);
                        model2 = PopulatePageModel(model2, count);
                        ApiHelper.SaveContent(model2);
                        dict.Add(count.ToString(), true);
                        count++;

                        for (var k = 0; k < 100; k++)
                        {
                            var model3 = ApiHelper.Create<Page>(model2.Id, "en-gb", "testnode" + count.ToString(), published: true);
                            model3 = PopulatePageModel(model3, count);
                            ApiHelper.SaveContent(model3);
                            dict.Add(count.ToString(), true);
                            count++;

                        }
                    }
                }
            }
            
            return View("~/views/home/test.cshtml");
        }
        public Page PopulatePageModel(Page model,int index) {
            model.MainContent = index + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications."
                + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications."
                + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications."
                + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications."
                + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications."
                + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications."
                + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications."
                + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications."
                + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications."
                + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications."
                + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications."
                + "Getting started ASP.NET MVC gives you a powerful, patterns-based way to build dynamic websites that enables a clean separation of concerns and gives you full control over markup for enjoyable, agile development. Get more libraries NuGet is a free Visual Studio extension that makes it easy to add, remove, and update libraries and tools in Visual Studio projects. Web Hosting You can easily find a web hosting company that offers the right mix of features and price for your applications.";
            model.MetaDescription = index+" puck cms. mvc style content management";
            model.Title = "title " + index;
            return model;
        }
        public ActionResult Test2()
        {
            //var affected = ApiHelper.UpdateDescendantPaths("/home/","/home1/");
            var max = 7000;

            for(var i = 0; i < max; i++)
            {

                var str = "";

            }

            return View("~/views/home/test.cshtml");
        }
        public ActionResult Test()
        {
            string aqn = "puck.core.Base.BaseModel, puck.core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            var aqn1 = puck.core.debugging.GetType(aqn)?.AssemblyQualifiedName ?? "nope1";
            var aqn2 = Type.GetType(aqn)?.AssemblyQualifiedName ?? "nope2";
            return Content($"{aqn1}<br/>{aqn2}");
        }
    }
}