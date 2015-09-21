using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace CRMEntityBase
{
    public static class Extensions
    {
        public static bool ObjectEquals(this object target, object compare)
        {
            if (target == null || compare == null)
                return false;

            Type targetType = target.GetType();

            if (compare.GetType() != targetType)
                return false;

            foreach (System.Reflection.PropertyInfo pi in targetType.GetProperties())
            {
                if (pi.CanRead && !pi.GetCustomAttributes(typeof(IgnoreEquals), false).Any())
                {
                    object targetValue = pi.GetValue(target, null);
                    object compareValue = pi.GetValue(compare, null);

                    if (targetValue is DateTime && compareValue is DateTime)
                    {
                        if (!Data.DateEquals((DateTime)targetValue, (DateTime)compareValue))
                            return false;
                    }
                    else if (targetValue is System.Collections.IList && targetValue.GetType().IsGenericType)
                    {
                        if (!Data.ListEquals((IList)targetValue, (IList)compareValue))
                            return false;
                    }
                    else if (targetValue is System.Collections.IDictionary && targetValue.GetType().IsGenericType)
                    {
                        if (!Data.DictionaryEquals((IDictionary)targetValue, (IDictionary)compareValue))
                            return false;
                    }
                    else if (targetValue is System.Byte[])
                    {
                        if (!Data.ByteArrayEquals(targetValue as Byte[], compareValue as Byte[]))
                            return false;
                    }
                    else if (!object.Equals(targetValue, compareValue))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
