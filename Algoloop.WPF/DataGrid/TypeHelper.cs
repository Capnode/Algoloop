/*
 * Copyright 2019 Capnode AB
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Algoloop.WPF.DataGrid
{
    public class TypeHelper
    {
        public static object ValueConvertor(Type type, string value)
        {

            if (type == typeof(byte) || type == typeof(byte?))
            {
                byte x;
                if (byte.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(sbyte) || type == typeof(sbyte?))
            {
                sbyte x;
                if (sbyte.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(short) || type == typeof(short?))
            {
                short x;
                if (short.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(ushort) || type == typeof(ushort?))
            {
                ushort x;
                if (ushort.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(int) || type == typeof(int?))
            {
                int x;
                if (int.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(uint) || type == typeof(uint?))
            {
                uint x;
                if (uint.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(long) || type == typeof(long?))
            {
                long x;
                if (long.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(ulong) || type == typeof(ulong?))
            {
                ulong x;
                if (ulong.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(float) || type == typeof(float?))
            {
                float x;
                if (float.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(double) || type == typeof(double?))
            {
                double x;
                if (double.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(decimal) || type == typeof(decimal?))
            {
                decimal x;
                if (decimal.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(char) || type == typeof(char?))
            {
                char x;
                if (char.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(bool) || type == typeof(bool?))
            {
                bool x;
                if (bool.TryParse(value, out x))
                    return x;
                else
                    return null;
            }
            return null;
        }

        public static bool IsValueType(Type type)
        {
            return type == typeof(byte) ||
                    type == typeof(sbyte) ||
                    type == typeof(short) ||
                    type == typeof(ushort) ||
                    type == typeof(int) ||
                    type == typeof(uint) ||
                    type == typeof(long) ||
                    type == typeof(ulong) ||
                    type == typeof(float) ||
                    type == typeof(double) ||
                    type == typeof(decimal) ||
                     type == typeof(bool) ||
                    type == typeof(char);
        }

        public static bool IsNullable(Type type)
        {
            if (type == null) return false;
            //reference types are always nullable
            if (!type.IsValueType) return true;
            //if it's Nullable<int> it IsValueType, but also IsGenericType
            return IsNullableType(type);
        }

        private static bool IsNullableType(Type type)
        {
            return (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>)));
        }

        public static bool IsNumbericType(Type p)
        {
            bool result = false;
            result = result || p == typeof(int);
            result = result || p == typeof(decimal);
            result = result || p == typeof(float);
            result = result || p == typeof(int?);
            result = result || p == typeof(decimal?);
            result = result || p == typeof(float?);
            return result;
        }

        public static bool IsStringType(Type p)
        {
            return !IsNumbericType(p);
        }

        public static bool IsDateTimeType(Type p)
        {
            return p == typeof(DateTime);
        }

    }
}
