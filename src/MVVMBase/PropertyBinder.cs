using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace MVVMBase
{
    public class PropertyBinder<TSource> where TSource : INotifyPropertyChanged
    {
        private TSource source;
        private Dictionary<string, string> _bindings;
        private PropertyDescriptorCollection _sourceProperties;
        private PropertyDescriptorCollection _destProperties;

        public PropertyBinder(TSource source)
        {
            source.PropertyChanged += Source_PropertyChanged;
        }

        private void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.UpdateDestinationProperty(e.PropertyName);
        }

        private void Destination_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.UpdateSourceProperty(e.PropertyName);
        }

        public void AddBinding<TDest, TDestMember>(TDest dest, Expression<Func<TDest, TDestMember>> destSelectorExpression, Expression<Func<TSource, TDestMember>> sourceSelectorExpression) where TDest : INotifyPropertyChanged
        {
            MemberExpression memberEx = destSelectorExpression.Body as MemberExpression;
            if (memberEx == null) throw new ArgumentException($"Expression '{destSelectorExpression.ToString()}' does not refer to a property.");
            PropertyInfo propertyInfo = memberEx.Member as PropertyInfo;
            if (propertyInfo == null) throw new ArgumentException($"Expression '{destSelectorExpression.ToString()}' does not refer to a property.");

            PropertyDescriptor propDesc = TypeDescriptor.GetProperties(dest)[propertyInfo.Name];
        }

        private void UpdateSourceProperty(string propertyName)
        {
            //PropertyDescriptor property = this._notifyProperties[propertyName];
            // property.GetValue
            //string boundPropertyName;
            //if (this._bindings.TryGetValue(propertyName, out boundPropertyName))
            //{
            //    PropertyInfo property = typeof(TConsume).GetProperty(boundPropertyName);

            //}
        }

        private void UpdateDestinationProperty(string propertyName)
        { }
    }
}