using System;
using System.Reflection;

namespace MVVMBase
{
    public class PropertyBindingInfo : IEquatable<PropertyBindingInfo>
    {
        public PropertyInfo PropertyInfo { get; private set; }
        public object DestinationInstance { get; private set; }

        public PropertyBindingInfo(PropertyInfo propertyInfo, object destinationInstance)
        {
            this.PropertyInfo = propertyInfo;
            this.DestinationInstance = destinationInstance;
        }

        public bool Equals(PropertyBindingInfo other)
        {
            if (other == null) return false;

            return this.PropertyInfo == other.PropertyInfo && this.DestinationInstance == other.DestinationInstance;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;

            return this.Equals(obj as PropertyBindingInfo);
        }

        public override int GetHashCode()
        {
            return LeftRotate(this.PropertyInfo.GetHashCode(), 2) ^ this.DestinationInstance.GetHashCode();
        }

        private int LeftRotate(int value, int shiftAmount)
        {
            // Only use first 32 bits of shiftAmount
            shiftAmount = shiftAmount & 0x1F;

            // Convert value to unsigned int
            uint bits = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
            // Preserve bits that will be shifted out
            uint wrappedBits = bits >> (32 - shiftAmount);
            // Perform left shift, re-insert bits that were shifted out, and convert to int32
            return BitConverter.ToInt32(BitConverter.GetBytes((bits << shiftAmount) | wrappedBits), 0);
        }

        public void SetValue(object value)
        {
            this.PropertyInfo.SetValue(this.DestinationInstance, value);
        }
    }
}