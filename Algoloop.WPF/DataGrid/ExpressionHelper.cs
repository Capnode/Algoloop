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

namespace Algoloop.WPF.DataGrid
{
    public class ExpressionHelper
    {
        public static MethodCallExpression ToString(System.Linq.Expressions.Expression prop)
        {
            return System.Linq.Expressions.Expression.Call(prop, typeof(object).GetMethod("ToString", System.Type.EmptyTypes));
        }

        public static MethodCallExpression ToLower(MethodCallExpression stringProp)
        {
            return System.Linq.Expressions.Expression.Call(stringProp, typeof(string).GetMethod("ToLower", System.Type.EmptyTypes));
        }

        public static BinaryExpression NotNull(System.Linq.Expressions.Expression prop)
        {
            return System.Linq.Expressions.Expression.NotEqual(prop, System.Linq.Expressions.Expression.Constant(null));
        }

        public static Predicate<object> GenerateGeneric(MemberExpression prop, ConstantExpression val, Type type, ParameterExpression objParam, string methodName)
        {
            if (TypeHelper.IsNullable(type))
            {
                var containsExpression = System.Linq.Expressions.Expression.Call(ExpressionHelper.ToLower(ExpressionHelper.ToString(prop)), methodName, null, ExpressionHelper.ToLower(ExpressionHelper.ToString(val)));
                var exp = System.Linq.Expressions.Expression.AndAlso(ExpressionHelper.NotNull(prop), containsExpression);
                Expression<Func<object, bool>> equalfunction = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(exp, objParam);
                return new Predicate<object>(equalfunction.Compile());
            }
            else
            {
                var exp = System.Linq.Expressions.Expression.Call(ExpressionHelper.ToLower(ExpressionHelper.ToString(prop)), methodName, null, ExpressionHelper.ToLower(ExpressionHelper.ToString(val)));
                Expression<Func<object, bool>> equalfunction = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(exp, objParam);
                return new Predicate<object>(equalfunction.Compile());
            }
        }

        public static Predicate<object> GenerateEquals(MemberExpression prop, string value, Type type, ParameterExpression objParam)
        {
            BinaryExpression equalExpresion = null;
            if (TypeHelper.IsValueType(type))
            {
                object equalTypedInput = TypeHelper.ValueConvertor(type, value);
                if (equalTypedInput != null)
                {
                    var equalValue = System.Linq.Expressions.Expression.Constant(equalTypedInput, type);
                    equalExpresion = System.Linq.Expressions.Expression.Equal(prop, equalValue);
                }
            }
            else
            {
                var toStringExp = System.Linq.Expressions.Expression.Equal(ToLower(ToString(prop)), ExpressionHelper.ToLower(ExpressionHelper.ToString(System.Linq.Expressions.Expression.Constant(value))));
                if (!TypeHelper.IsDateTimeType(type) && TypeHelper.IsNullable(type))
                    equalExpresion = System.Linq.Expressions.Expression.AndAlso(ExpressionHelper.NotNull(prop), toStringExp);
                else
                    equalExpresion = toStringExp;


            }
            if (equalExpresion != null)
            {
                Expression<Func<object, bool>> equalfunction = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(equalExpresion, objParam);
                return new Predicate<object>(equalfunction.Compile());
            }
            else
                return null;
        }

        public static Predicate<object> GenerateNotEquals(MemberExpression prop, string value, Type type, ParameterExpression objParam)
        {
            BinaryExpression notEqualExpresion = null;
            if (TypeHelper.IsValueType(type))
            {
                object equalTypedInput = TypeHelper.ValueConvertor(type, value);
                if (equalTypedInput != null)
                {
                    var equalValue = System.Linq.Expressions.Expression.Constant(equalTypedInput, type);
                    notEqualExpresion = System.Linq.Expressions.Expression.NotEqual(prop, equalValue);
                }
            }
            else
            {
                var toStringExp = System.Linq.Expressions.Expression.NotEqual(ToLower(ToString(prop)), ExpressionHelper.ToLower(ExpressionHelper.ToString(System.Linq.Expressions.Expression.Constant(value))));
                notEqualExpresion = System.Linq.Expressions.Expression.AndAlso(ExpressionHelper.NotNull(prop), toStringExp);

            }
            if (notEqualExpresion != null)
            {
                Expression<Func<object, bool>> equalfunction = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(notEqualExpresion, objParam);
                return new Predicate<object>(equalfunction.Compile());
            }
            else
                return null;
        }

        public static Predicate<object> GenerateGreaterThanEqual(MemberExpression prop, string value, Type type, ParameterExpression objParam)
        {
            object typedInput = TypeHelper.ValueConvertor(type, value);
            if (typedInput != null)
            {
                var greaterThanEqualValue = System.Linq.Expressions.Expression.Constant(typedInput, type);
                var greaterThanEqualExpresion = System.Linq.Expressions.Expression.GreaterThanOrEqual(prop, greaterThanEqualValue);
                Expression<Func<object, bool>> greaterThanEqualfunction = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(greaterThanEqualExpresion, objParam);
                return new Predicate<object>(greaterThanEqualfunction.Compile());
            }
            else
            {
                return null;
            }
        }

        public static Predicate<object> GenerateLessThanEqual(MemberExpression prop, string value, Type type, ParameterExpression objParam)
        {
            object typedInput = TypeHelper.ValueConvertor(type, value);
            if (typedInput != null)
            {
                var lessThanEqualValue = System.Linq.Expressions.Expression.Constant(typedInput, type);
                var lessThanEqualExpresion = System.Linq.Expressions.Expression.LessThanOrEqual(prop, lessThanEqualValue);
                Expression<Func<object, bool>> lessThanEqualfunction = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(lessThanEqualExpresion, objParam);
                return new Predicate<object>(lessThanEqualfunction.Compile());
            }
            else
            {
                return null;
            }
        }
        public static Predicate<object> GenerateLessThan(MemberExpression prop, string value, Type type, ParameterExpression objParam)
        {
            object typedInput = TypeHelper.ValueConvertor(type, value);
            if (typedInput != null)
            {
                var lessThan = System.Linq.Expressions.Expression.Constant(typedInput, type);
                var lessThanExpresion = System.Linq.Expressions.Expression.LessThan(prop, lessThan);
                System.Linq.Expressions.Expression<Func<object, bool>> lessThanfunction = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(lessThanExpresion, objParam);
                return new Predicate<object>(lessThanfunction.Compile());
            }
            else
            {
                return null;
            }
        }
        public static Predicate<object> GenerateGreaterThan(MemberExpression prop, string value, Type type, ParameterExpression objParam)
        {
            object typedInput = TypeHelper.ValueConvertor(type, value);
            if (typedInput != null)
            {
                var greaterThanValue = System.Linq.Expressions.Expression.Constant(typedInput, type);
                var greaterThanExpresion = System.Linq.Expressions.Expression.GreaterThan(prop, greaterThanValue);
                Expression<Func<object, bool>> greaterThanfunction = System.Linq.Expressions.Expression.Lambda<Func<object, bool>>(greaterThanExpresion, objParam);
                return new Predicate<object>(greaterThanfunction.Compile());
            }
            else
            {
                return null;
            }
        }
    }
}
