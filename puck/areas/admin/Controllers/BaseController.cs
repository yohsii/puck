using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace puck.core.Controllers
{
    public class BaseController : Controller
    {
        internal static bool IsPropertyAllowed(string propertyName, string[] includeProperties, string[] excludeProperties)
        {
            // We allow a property to be bound if its both in the include list AND not in the exclude list.
            // An empty include list implies all properties are allowed.
            // An empty exclude list implies no properties are disallowed.
            bool includeProperty = (includeProperties == null) || (includeProperties.Length == 0) || includeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
            bool excludeProperty = (excludeProperties != null) && excludeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
            return includeProperty && !excludeProperty;
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model) where TModel : class
        {
            return TryUpdateModelDynamic(model, null, null, null, ValueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix) where TModel : class
        {
            return TryUpdateModelDynamic(model, prefix, null, null, ValueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string[] includeProperties) where TModel : class
        {
            return TryUpdateModelDynamic(model, null, includeProperties, null, ValueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties) where TModel : class
        {
            return TryUpdateModelDynamic(model, prefix, includeProperties, null, ValueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) where TModel : class
        {
            return TryUpdateModelDynamic(model, prefix, includeProperties, excludeProperties, ValueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, IValueProvider valueProvider) where TModel : class
        {
            return TryUpdateModelDynamic(model, null, null, null, valueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix, IValueProvider valueProvider) where TModel : class
        {
            return TryUpdateModelDynamic(model, prefix, null, null, valueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string[] includeProperties, IValueProvider valueProvider) where TModel : class
        {
            return TryUpdateModelDynamic(model, null, includeProperties, null, valueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, IValueProvider valueProvider) where TModel : class
        {
            return TryUpdateModelDynamic(model, prefix, includeProperties, null, valueProvider);
        }

        protected internal bool TryUpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties, IValueProvider valueProvider) where TModel : class
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            if (valueProvider == null)
            {
                throw new ArgumentNullException("valueProvider");
            }

            Predicate<string> propertyFilter = propertyName => IsPropertyAllowed(propertyName, includeProperties, excludeProperties);
            IModelBinder binder = Binders.GetBinder(model.GetType());

            ModelBindingContext bindingContext = new ModelBindingContext()
            {
                ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => model, model.GetType()),
                ModelName = prefix,
                ModelState = ModelState,
                PropertyFilter = propertyFilter,
                ValueProvider = valueProvider
            };
            binder.BindModel(ControllerContext, bindingContext);
            return ModelState.IsValid;
        }


        protected internal void UpdateModelDynamic<TModel>(TModel model) where TModel : class
        {
            UpdateModelDynamic(model, null, null, null, ValueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix) where TModel : class
        {
            UpdateModelDynamic(model, prefix, null, null, ValueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string[] includeProperties) where TModel : class
        {
            UpdateModelDynamic(model, null, includeProperties, null, ValueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties) where TModel : class
        {
            UpdateModelDynamic(model, prefix, includeProperties, null, ValueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) where TModel : class
        {
            UpdateModelDynamic(model, prefix, includeProperties, excludeProperties, ValueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, IValueProvider valueProvider) where TModel : class
        {
            UpdateModelDynamic(model, null, null, null, valueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix, IValueProvider valueProvider) where TModel : class
        {
            UpdateModelDynamic(model, prefix, null, null, valueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string[] includeProperties, IValueProvider valueProvider) where TModel : class
        {
            UpdateModelDynamic(model, null, includeProperties, null, valueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, IValueProvider valueProvider) where TModel : class
        {
            UpdateModelDynamic(model, prefix, includeProperties, null, valueProvider);
        }

        protected internal void UpdateModelDynamic<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties, IValueProvider valueProvider) where TModel : class
        {
            bool success = TryUpdateModelDynamic(model, prefix, includeProperties, excludeProperties, valueProvider);
            if (!success)
            {
                string message = String.Format("The model of type '{0}' could not be updated.", model.GetType().FullName);
                throw new InvalidOperationException(message);
            }
        }
    }
}
