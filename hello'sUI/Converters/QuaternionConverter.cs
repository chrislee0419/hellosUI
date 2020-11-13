using IPA.Config.Data;
using IPA.Config.Stores;
using System;
using UnityEngine;

namespace HUI.Converters
{
    // basically a straight rip of Auros' Vector3 converter adapted to Quaternion
    // https://github.com/Auros/SiraUtil/blob/master/SiraUtil/Converters/Vector3Converter.cs
    public class QuaternionConverter : ValueConverter<Quaternion>
    {
        public override Quaternion FromValue(Value value, object parent)
        {
            if (value is Map map)
            {
                map.TryGetValue("w", out Value w);
                map.TryGetValue("x", out Value x);
                map.TryGetValue("y", out Value y);
                map.TryGetValue("z", out Value z);

                Quaternion quaternion = new Quaternion();
                if (w is FloatingPoint floatW)
                    quaternion.z = Convert.ToSingle(floatW);
                if (x is FloatingPoint floatX)
                    quaternion.x = Convert.ToSingle(floatX);
                if (y is FloatingPoint floatY)
                    quaternion.y = Convert.ToSingle(floatY);
                if (z is FloatingPoint floatZ)
                    quaternion.z = Convert.ToSingle(floatZ);

                return quaternion;
            }
            throw new ArgumentException("Value cannot be parsed into a Quaternion", nameof(value));
        }

        public override Value ToValue(Quaternion obj, object parent)
        {
            var map = Value.Map();
            map.Add("w", Value.Float(Convert.ToDecimal(obj.w)));
            map.Add("x", Value.Float(Convert.ToDecimal(obj.x)));
            map.Add("y", Value.Float(Convert.ToDecimal(obj.y)));
            map.Add("z", Value.Float(Convert.ToDecimal(obj.z)));
            return map;
        }
    }
}
