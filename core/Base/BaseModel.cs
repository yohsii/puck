using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Web.Mvc;
using puck.core.Abstract;
using puck.core.Attributes;
namespace puck.core.Base
{
    public class BaseModel
    {
        public BaseModel() {
            Id = new Guid();
        }
        [HiddenInput(DisplayValue=false)]
        [ReadOnly(true)]
        [DefaultGUIDTransformer()]
        public Guid Id { get; private set; }

        [ReadOnly(true)]
        public string Path { get; set; }
        
        [DateTransformer()]        
        [ReadOnly(true)]
        public DateTime Created { get; set; }

        [ReadOnly(true)]
        public int Revision { get; set; }

        [HiddenInput(DisplayValue=false)]
        public string Variant { get; set; }
        
    }
}
