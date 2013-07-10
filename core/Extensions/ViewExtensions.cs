using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Globalization;
using puck.core.Constants;
using puck.core.Abstract;
using Ninject;
using Newtonsoft.Json;
using puck.core.Controllers;
namespace puck.core.Extensions
{
    public static class ViewExtensions
    {
        public static T PuckEditorSettings<T>(this WebViewPage page) {
            var repo = PuckCache.PuckRepo;

            var modelType = page.ViewBag.Level0Type as Type;
            if (modelType == null)
                return default(T);
            var settingsType = typeof(T);
            var propertyName = ModelMetadata.FromStringExpression("", page.ViewData).PropertyName;
            var type = modelType;
            while (type != typeof(object)) {
                var key = string.Concat(settingsType.AssemblyQualifiedName, ":", type.AssemblyQualifiedName, ":", propertyName);
                var meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.EditorSettings && x.Key.Equals(key)).FirstOrDefault();
                if (meta == null) {
                    key = string.Concat(settingsType.AssemblyQualifiedName, ":", type.AssemblyQualifiedName, ":");
                    meta = repo.GetPuckMeta().Where(x => x.Name == DBNames.EditorSettings && x.Key.Equals(key)).FirstOrDefault();
                }
                if (meta != null)
                {
                    var data = JsonConvert.DeserializeObject(meta.Value, settingsType);
                    return data == null ? default(T) : (T)data;
                }
                type = type.BaseType;
            }
            return default(T);            
        }
        public static T GetModel<T>(this WebViewPage page) {
            var instance = Activator.CreateInstance(typeof(T));
            var controller = page.ViewContext.Controller as BaseController;
            if (controller.TryUpdateModelDynamic(instance))
                return (T)instance;
            else 
                return default(T);
        }
        private static List<Type> NonIntNumbers = new List<Type> { typeof(Decimal),typeof(Single),typeof(Double),typeof(Decimal?),typeof(Single?),typeof(Double?)};
        public static MvcHtmlString Input(this HtmlHelper htmlHelper, string type) {
            var value = (htmlHelper.ViewData.ModelMetadata.Model??"").ToString();
            var mtype = htmlHelper.ViewData.ModelMetadata.ModelType;
            var d = new Dictionary<string, object>();
            if (NonIntNumbers.Contains(mtype)) {
                d.Add("step","any");
            }
            if (htmlHelper.ViewData.ModelMetadata.HideSurroundingHtml)
            {
                return htmlHelper.Hidden("", value);
            }
            else if (htmlHelper.ViewData.ModelMetadata.IsReadOnly)
            {
                d.Add("disabled", "disabled");
                return htmlHelper.Input("", value, type, d);
            }
            else {
                return htmlHelper.Input("", value, type, d);
            }
        }
        public static MvcHtmlString Input(this HtmlHelper htmlHelper, string name, object value,string type, IDictionary<string, object> htmlAttributes)
        {
            return InputHelper(htmlHelper,
                               type,
                               metadata: null,
                               name: name,
                               value: value,
                               useViewData: (value == null),
                               isChecked: false,
                               setId: true,
                               isExplicitValue: true,
                               format: null,
                               htmlAttributes: htmlAttributes);
        }

        // Helper methods

        private static MvcHtmlString InputHelper(HtmlHelper htmlHelper, string inputType, ModelMetadata metadata, string name, object value, bool useViewData, bool isChecked, bool setId, bool isExplicitValue, string format, IDictionary<string, object> htmlAttributes)
        {
            inputType = inputType.ToLower();
            string fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            if (String.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException("variable is null or empty", "name");
            }
            
            TagBuilder tagBuilder = new TagBuilder("input");
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.MergeAttribute("type", inputType);
            tagBuilder.MergeAttribute("name", fullName, true);

            string valueParameter = htmlHelper.FormatValue(value, format);
            bool usedModelState = false;

            switch (inputType)
            {
                case "checkbox":
                    bool? modelStateWasChecked = htmlHelper.GetModelStateValue(fullName, typeof(bool)) as bool?;
                    if (modelStateWasChecked.HasValue)
                    {
                        isChecked = modelStateWasChecked.Value;
                        usedModelState = true;
                    }
                    goto case "radio";
                case "radio":
                    if (!usedModelState)
                    {
                        string modelStateValue = htmlHelper.GetModelStateValue(fullName, typeof(string)) as string;
                        if (modelStateValue != null)
                        {
                            isChecked = String.Equals(modelStateValue, valueParameter, StringComparison.Ordinal);
                            usedModelState = true;
                        }
                    }
                    if (!usedModelState && useViewData)
                    {
                        isChecked = htmlHelper.EvalBoolean(fullName);
                    }
                    if (isChecked)
                    {
                        tagBuilder.MergeAttribute("checked", "checked");
                    }
                    tagBuilder.MergeAttribute("value", valueParameter, isExplicitValue);
                    break;
                case "password":
                    if (value != null)
                    {
                        tagBuilder.MergeAttribute("value", valueParameter, isExplicitValue);
                    }
                    break;
                default:
                    string attemptedValue = (string)htmlHelper.GetModelStateValue(fullName, typeof(string));
                    tagBuilder.MergeAttribute("value", attemptedValue ?? ((useViewData) ? htmlHelper.EvalString(fullName, format) : valueParameter), isExplicitValue);
                    break;
            }

            if (setId)
            {
                tagBuilder.GenerateId(fullName);
            }

            // If there are any errors for a named field, we add the css attribute.
            ModelState modelState;
            if (htmlHelper.ViewData.ModelState.TryGetValue(fullName, out modelState))
            {
                if (modelState.Errors.Count > 0)
                {
                    tagBuilder.AddCssClass(HtmlHelper.ValidationInputCssClassName);
                }
            }

            tagBuilder.MergeAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(name, metadata));

            if (inputType == "checkbox")
            {
                // Render an additional <input type="hidden".../> for checkboxes. This
                // addresses scenarios where unchecked checkboxes are not sent in the request.
                // Sending a hidden input makes it possible to know that the checkbox was present
                // on the page when the request was submitted.
                StringBuilder inputItemBuilder = new StringBuilder();
                inputItemBuilder.Append(tagBuilder.ToString(TagRenderMode.SelfClosing));

                TagBuilder hiddenInput = new TagBuilder("input");
                hiddenInput.MergeAttribute("type", HtmlHelper.GetInputTypeString(InputType.Hidden));
                hiddenInput.MergeAttribute("name", fullName);
                hiddenInput.MergeAttribute("value", "false");
                inputItemBuilder.Append(hiddenInput.ToString(TagRenderMode.SelfClosing));
                return MvcHtmlString.Create(inputItemBuilder.ToString());
            }
            return tagBuilder.ToMvcHtmlString(TagRenderMode.SelfClosing);
        }
        private static MvcHtmlString ToMvcHtmlString(this TagBuilder tagBuilder, TagRenderMode renderMode)
        {
            return new MvcHtmlString(tagBuilder.ToString(renderMode));
        }
        private static string EvalString(this HtmlHelper htmlHelper, string key)
        {
            return Convert.ToString(htmlHelper.ViewData.Eval(key), CultureInfo.CurrentCulture);
        }
        private static string EvalString(this HtmlHelper htmlHelper,string key, string format)
        {
            return Convert.ToString(htmlHelper.ViewData.Eval(key, format), CultureInfo.CurrentCulture);
        }
        private static bool EvalBoolean(this HtmlHelper htmlHelper, string key)
        {
            return Convert.ToBoolean(htmlHelper.ViewData.Eval(key), CultureInfo.InvariantCulture);
        }
        private static object GetModelStateValue(this HtmlHelper htmlHelper, string key, Type destinationType)
        {
            ModelState modelState;
            if (htmlHelper.ViewData.ModelState.TryGetValue(key, out modelState))
            {
                if (modelState.Value != null)
                {
                    return modelState.Value.ConvertTo(destinationType, null /* culture */);
                }
            }
            return null;
        }
    }
}
