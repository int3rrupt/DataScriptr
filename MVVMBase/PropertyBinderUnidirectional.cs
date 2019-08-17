using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace MVVMBase
{
    public class PropertyBinderUnidirectional<TSource> where TSource : INotifyPropertyChanged
    {
        private TSource _source;
        //private Dictionary<string, PropertyInfo> _sourceProperties;
        private Dictionary<string, HashSet<PropertyBindingInfo>> _bindings;
        //private Dictionary<string, HashSet<string>> _bindings;
        //private Dictionary<string, PropertyInfo> _destProperties;

        public PropertyBinderUnidirectional(TSource source)
        {
            //this._sourceProperties = new Dictionary<string, PropertyInfo>();
            this._bindings = new Dictionary<string, HashSet<PropertyBindingInfo>>();
            this._source = source;
            source.PropertyChanged += Source_PropertyChanged;
        }

        private void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.UpdateDestinationProperty(e.PropertyName);
        }

        public void AddBinding<TDest, TDestMember>(TDest dest, Expression<Func<TDest, TDestMember>> destSelectorExpression, Expression<Func<TSource, TDestMember>> sourceSelectorExpression)
        {
            // Get property info
            PropertyInfo destinationPropertyInfo = this.GetPropertyInfo(destSelectorExpression);
            PropertyInfo sourcePropertyInfo = this.GetPropertyInfo(sourceSelectorExpression);

            // Store property info
            //this._destProperties.Add(destinationPropertyInfo.Name, destinationPropertyInfo);
            //PropertyInfo existingSourcePropertyInfo;
            //if (!this._sourceProperties.TryGetValue(sourcePropertyInfo.Name, out existingSourcePropertyInfo))
            //{
            //    this._sourceProperties.Add(sourcePropertyInfo.Name, sourcePropertyInfo);
            //}

            // Store binding information
            HashSet<PropertyBindingInfo> destinationPropertyBindingInfoCollection;
            if (this._bindings.TryGetValue(sourcePropertyInfo.Name, out destinationPropertyBindingInfoCollection))
            {
                destinationPropertyBindingInfoCollection.Add(new PropertyBindingInfo(destinationPropertyInfo, dest));
            }
            else
            {
                destinationPropertyBindingInfoCollection = new HashSet<PropertyBindingInfo>();
                destinationPropertyBindingInfoCollection.Add(new PropertyBindingInfo(destinationPropertyInfo, dest));
                this._bindings.Add(sourcePropertyInfo.Name, destinationPropertyBindingInfoCollection);
            }

            //// Destination Property
            //MemberExpression memberEx = destSelectorExpression.Body as MemberExpression;
            //if (memberEx == null) throw new ArgumentException($"Expression '{destSelectorExpression.ToString()}' does not refer to a property.");
            //PropertyInfo propertyInfo = memberEx.Member as PropertyInfo;
            //if (propertyInfo == null) throw new ArgumentException($"Expression '{destSelectorExpression.ToString()}' does not refer to a property.");

            //PropertyDescriptor propDesc = TypeDescriptor.GetProperties(dest)[propertyInfo.Name];
        }

        private PropertyInfo GetPropertyInfo(LambdaExpression lambdaExpression)
        {
            MemberExpression memberExpression = lambdaExpression.Body as MemberExpression;
            if (memberExpression == null) throw new ArgumentException($"Expression '{lambdaExpression.ToString()}' does not refer to a property.");
            PropertyInfo propertyInfo = memberExpression.Member as PropertyInfo;
            if (propertyInfo == null) throw new ArgumentException($"Expression '{lambdaExpression.ToString()}' does not refer to a property.");
            return propertyInfo;
        }

        private void UpdateDestinationProperty(string sourcePropertyName)
        {
            HashSet<PropertyBindingInfo> destinationPropertyBindingInfoCollection;
            if (this._bindings.TryGetValue(sourcePropertyName, out destinationPropertyBindingInfoCollection))
            {
                foreach (PropertyBindingInfo destinationPropertyBindingInfo in destinationPropertyBindingInfoCollection)
                {
                    object sourcePropertyValue = typeof(TSource).GetProperty(sourcePropertyName).GetValue(this._source);
                    destinationPropertyBindingInfo.SetValue(sourcePropertyValue);
                }
            }
        }

        private void BuildProps(Type type)
        {
            //foreach (PropertyInfo property in type.Get())
            //{

            //}
        }
    }
}