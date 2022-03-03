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

namespace Capnode.Wpf.DataGrid.Internal
{
    internal static class TypeHelper
    {
        public static object ValueConverter(Type type, string value)
        {

            if (type == typeof(byte) || type == typeof(byte?))
            {
                if (byte.TryParse(value, out byte x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(sbyte) || type == typeof(sbyte?))
            {
                if (sbyte.TryParse(value, out sbyte x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(short) || type == typeof(short?))
            {
                if (short.TryParse(value, out short x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(ushort) || type == typeof(ushort?))
            {
                if (ushort.TryParse(value, out ushort x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(int) || type == typeof(int?))
            {
                if (int.TryParse(value, out int x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(uint) || type == typeof(uint?))
            {
                if (uint.TryParse(value, out uint x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(long) || type == typeof(long?))
            {
                if (long.TryParse(value, out long x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(ulong) || type == typeof(ulong?))
            {
                if (ulong.TryParse(value, out ulong x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(float) || type == typeof(float?))
            {
                if (float.TryParse(value, out float x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(double) || type == typeof(double?))
            {
                if (double.TryParse(value, out double x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(decimal) || type == typeof(decimal?))
            {
                if (decimal.TryParse(value, out decimal x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(char) || type == typeof(char?))
            {
                if (char.TryParse(value, out char x))
                    return x;
                else
                    return null;
            }
            else if (type == typeof(bool) || type == typeof(bool?))
            {
                if (bool.TryParse(value, out bool x))
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
