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
using System.Linq.Expressions;

namespace Capnode.Wpf.DataGrid.Internal
{
    internal static class ExpressionHelper
    {
        public static MethodCallExpression ToString(Expression prop)
        {
            return Expression.Call(prop, typeof(object).GetMethod("ToString", Type.EmptyTypes));
        }

        public static MethodCallExpression ToLower(MethodCallExpression stringProp)
        {
            return Expression.Call(stringProp, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
        }

        public static BinaryExpression NotNull(Expression prop)
        {
            return Expression.NotEqual(prop, Expression.Constant(null));
        }

        public static Predicate<object> GenerateGeneric(Expression prop, ConstantExpression val, Type type, ParameterExpression objParam, string methodName)
        {
            if (TypeHelper.IsNullable(type))
            {
                var containsExpression = Expression.Call(ToLower(ToString(prop)), methodName, null, ToLower(ToString(val)));
                var exp = Expression.AndAlso(NotNull(prop), containsExpression);
                Expression<Func<object, bool>> equalfunction = Expression.Lambda<Func<object, bool>>(exp, objParam);
                return new Predicate<object>(equalfunction.Compile());
            }
            else
            {
                var exp = Expression.Call(ToLower(ToString(prop)), methodName, null, ToLower(ToString(val)));
                Expression<Func<object, bool>> equalfunction = Expression.Lambda<Func<object, bool>>(exp, objParam);
                return new Predicate<object>(equalfunction.Compile());
            }
        }

        public static Predicate<object> GenerateEquals(Expression prop, string value, Type type, ParameterExpression objParam)
        {
            BinaryExpression equalExpresion = null;
            if (TypeHelper.IsValueType(type))
            {
                object equalTypedInput = TypeHelper.ValueConverter(type, value);
                if (equalTypedInput != null)
                {
                    var equalValue = Expression.Constant(equalTypedInput, type);
                    equalExpresion = Expression.Equal(prop, equalValue);
                }
            }
            else
            {
                var toStringExp = Expression.Equal(ToLower(ToString(prop)), ToLower(ToString(Expression.Constant(value))));
                if (!TypeHelper.IsDateTimeType(type) && TypeHelper.IsNullable(type))
                    equalExpresion = Expression.AndAlso(NotNull(prop), toStringExp);
                else
                    equalExpresion = toStringExp;


            }
            if (equalExpresion != null)
            {
                Expression<Func<object, bool>> equalfunction = Expression.Lambda<Func<object, bool>>(equalExpresion, objParam);
                return new Predicate<object>(equalfunction.Compile());
            }
            else
                return null;
        }

        public static Predicate<object> GenerateNotEquals(Expression prop, string value, Type type, ParameterExpression objParam)
        {
            BinaryExpression notEqualExpresion = null;
            if (TypeHelper.IsValueType(type))
            {
                object equalTypedInput = TypeHelper.ValueConverter(type, value);
                if (equalTypedInput != null)
                {
                    var equalValue = Expression.Constant(equalTypedInput, type);
                    notEqualExpresion = Expression.NotEqual(prop, equalValue);
                }
            }
            else
            {
                var toStringExp = Expression.NotEqual(ToLower(ToString(prop)), ToLower(ToString(Expression.Constant(value))));
                notEqualExpresion = Expression.AndAlso(NotNull(prop), toStringExp);

            }
            if (notEqualExpresion != null)
            {
                Expression<Func<object, bool>> equalfunction = Expression.Lambda<Func<object, bool>>(notEqualExpresion, objParam);
                return new Predicate<object>(equalfunction.Compile());
            }
            else
                return null;
        }

        public static Predicate<object> GenerateGreaterThanEqual(Expression prop, string value, Type type, ParameterExpression objParam)
        {
            object typedInput = TypeHelper.ValueConverter(type, value);
            if (typedInput != null)
            {
                var greaterThanEqualValue = Expression.Constant(typedInput, type);
                var greaterThanEqualExpresion = Expression.GreaterThanOrEqual(prop, greaterThanEqualValue);
                Expression<Func<object, bool>> greaterThanEqualfunction = Expression.Lambda<Func<object, bool>>(greaterThanEqualExpresion, objParam);
                return new Predicate<object>(greaterThanEqualfunction.Compile());
            }
            else
            {
                return null;
            }
        }

        public static Predicate<object> GenerateLessThanEqual(Expression prop, string value, Type type, ParameterExpression objParam)
        {
            object typedInput = TypeHelper.ValueConverter(type, value);
            if (typedInput != null)
            {
                var lessThanEqualValue = Expression.Constant(typedInput, type);
                var lessThanEqualExpresion = Expression.LessThanOrEqual(prop, lessThanEqualValue);
                Expression<Func<object, bool>> lessThanEqualfunction = Expression.Lambda<Func<object, bool>>(lessThanEqualExpresion, objParam);
                return new Predicate<object>(lessThanEqualfunction.Compile());
            }
            else
            {
                return null;
            }
        }
        public static Predicate<object> GenerateLessThan(Expression prop, string value, Type type, ParameterExpression objParam)
        {
            object typedInput = TypeHelper.ValueConverter(type, value);
            if (typedInput != null)
            {
                var lessThan = Expression.Constant(typedInput, type);
                var lessThanExpresion = Expression.LessThan(prop, lessThan);
                Expression<Func<object, bool>> lessThanfunction = Expression.Lambda<Func<object, bool>>(lessThanExpresion, objParam);
                return new Predicate<object>(lessThanfunction.Compile());
            }
            else
            {
                return null;
            }
        }
        public static Predicate<object> GenerateGreaterThan(Expression prop, string value, Type type, ParameterExpression objParam)
        {
            object typedInput = TypeHelper.ValueConverter(type, value);
            if (typedInput != null)
            {
                var greaterThanValue = Expression.Constant(typedInput, type);
                var greaterThanExpresion = Expression.GreaterThan(prop, greaterThanValue);
                Expression<Func<object, bool>> greaterThanfunction = Expression.Lambda<Func<object, bool>>(greaterThanExpresion, objParam);
                return new Predicate<object>(greaterThanfunction.Compile());
            }
            else
            {
                return null;
            }
        }
    }
}
