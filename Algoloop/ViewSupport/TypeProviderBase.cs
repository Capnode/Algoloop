/***********************************************************************
Copyright 2018 CodeX Enterprises LLC

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

Major Changes:
04/2018    1.0     Initial release (Joel Champagne)
***********************************************************************/
using Algoloop.ViewModel;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Algoloop.ViewSupport
{
    /// <summary>
    /// This class provides two primary services: a) stores additional properties established at run-time, b) supports inspection of said data using ICustomTypeProvider.
    /// Why do we pass in Type explicitly - why not just infer the type based on value.GetType()? What if value is null, as for a Nullable<int>? We'll be explicit.
    /// An attempt has been made to write this not dependent on CodexMicroORM - but you can get largely identical functionality by using CodexMicroORM.
    /// </summary>
    public class TypeProviderBase : ViewModelBase, ICustomTypeProvider
    {
        private Dictionary<string, (Type type, object value)> _extra = new Dictionary<string, (Type type, object value)>();

        public void SetPropertyValue(string propName, Type type, object value)
        {
            var prop = this.GetType().GetProperty(propName);

            if (prop != null)
            {
                prop.SetValue(this, value);
                return;
            }

            if (type != null)
            {
                value = ParseNullable(value, type);
            }

            if (_extra.TryGetValue(propName, out var val))
            {
                if (type == null)
                {
                    value = ParseNullable(value, val.type);
                }

                _extra[propName] = (val.type, value);
            }
            else
            {
                if (value == null)
                {
                    _extra[propName] = (type ?? typeof(object), value);
                }
                else
                {
                    _extra[propName] = (type ?? value.GetType(), value);
                }
            }
        }

        public void SetPropertyValue(string propName, object value)
        {
            SetPropertyValue(propName, null, value);
        }

        public object GetPropertyValue(string propName)
        {
            var prop = this.GetType().GetProperty(propName);

            if (prop != null)
            {
                return prop.GetValue(this);
            }

            if (_extra.TryGetValue(propName, out var val))
            {
                return val.value;
            }

            throw new ArgumentException("propName is not a valid property.");
        }

        public Type GetCustomType()
        {
            return new CustomType(this);
        }

        private object ParseNullable(object value, Type knownType)
        {
            if (knownType == null)
                throw new ArgumentNullException("knownType");

            if (value == null || value.GetType() != knownType)
            {
                bool isnullable = knownType.IsGenericType && knownType.GetGenericTypeDefinition() == typeof(Nullable<>);

                if (value == null)
                {
                    if (isnullable)
                    {
                        value = Activator.CreateInstance(knownType);
                    }
                }
                else
                {
                    if (isnullable)
                    {
                        value = Activator.CreateInstance(knownType, Convert.ChangeType(value, Nullable.GetUnderlyingType(knownType)));
                    }
                    else
                    {
                        value = Convert.ChangeType(value, knownType);
                    }
                }
            }

            return value;
        }

        /// <summary>
        /// This private class is used by ICustomTypeProvider to provide property info for both CLR properties and property bag properties supported by CEF.
        /// </summary>
        private class CustomPropertyInfoHelper : PropertyInfo
        {
            private string _name;
            private Type _type;

            public CustomPropertyInfoHelper(string name, Type type)
            {
                _name = name;
                _type = type;
            }

            public override PropertyAttributes Attributes
            {
                get { throw new NotImplementedException(); }
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override MethodInfo[] GetAccessors(bool nonPublic)
            {
                throw new NotImplementedException();
            }

            public override MethodInfo GetGetMethod(bool nonPublic)
            {
                return null;
            }

            public override ParameterInfo[] GetIndexParameters()
            {
                return null;
            }

            public override MethodInfo GetSetMethod(bool nonPublic)
            {
                return null;
            }

            public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
            {
                return ((TypeProviderBase)obj).GetPropertyValue(_name);
            }

            public override Type PropertyType
            {
                get { return _type; }
            }

            public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, System.Globalization.CultureInfo culture)
            {
                ((TypeProviderBase)obj).SetPropertyValue(_name, value);
            }

            public override Type DeclaringType
            {
                get { throw new NotImplementedException(); }
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                return new object[] { };
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                return new object[] { };
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override string Name
            {
                get { return _name; }
            }

            public override Type ReflectedType
            {
                get { throw new NotImplementedException(); }
            }
        }

        private class CustomType : Type
        {
            TypeProviderBase _linked;
            Type _baseType;

            public CustomType(TypeProviderBase source)
            {
                _baseType = source.GetType();
                _linked = source;
            }

            public override Assembly Assembly
            {
                get { return _baseType.Assembly; }
            }

            public override string AssemblyQualifiedName
            {
                get { return _baseType.AssemblyQualifiedName; }
            }

            public override Type BaseType
            {
                get { return _baseType.BaseType; }
            }

            public override string FullName
            {
                get { return _baseType.FullName; }
            }

            public override Guid GUID
            {
                get { return _baseType.GUID; }
            }

            protected override TypeAttributes GetAttributeFlagsImpl()
            {
                return _baseType.Attributes;
            }

            protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
            {
                return _baseType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
            }

            public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
            {
                return _baseType.GetConstructors(bindingAttr);
            }

            public override Type GetElementType()
            {
                return _baseType.GetElementType();
            }

            public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
            {
                return _baseType.GetEvent(name, bindingAttr);
            }

            public override EventInfo[] GetEvents(BindingFlags bindingAttr)
            {
                return _baseType.GetEvents(bindingAttr);
            }

            public override FieldInfo GetField(string name, BindingFlags bindingAttr)
            {
                return _baseType.GetField(name, bindingAttr);
            }

            public override FieldInfo[] GetFields(BindingFlags bindingAttr)
            {
                return _baseType.GetFields(bindingAttr);
            }

            public override Type GetInterface(string name, bool ignoreCase)
            {
                return _baseType.GetInterface(name, ignoreCase);
            }

            public override Type[] GetInterfaces()
            {
                return _baseType.GetInterfaces();
            }

            public override RuntimeTypeHandle TypeHandle => _baseType.TypeHandle;

            public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
            {
                return _baseType.GetMembers(bindingAttr);
            }

            protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
            {
                throw new NotImplementedException();
            }

            public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
            {
                return _baseType.GetMethods(bindingAttr);
            }

            public override Type GetNestedType(string name, BindingFlags bindingAttr)
            {
                return _baseType.GetNestedType(name, bindingAttr);
            }

            public override Type[] GetNestedTypes(BindingFlags bindingAttr)
            {
                return _baseType.GetNestedTypes(bindingAttr);
            }

            public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
            {
                PropertyInfo[] clrProperties = _baseType.GetProperties(bindingAttr);

                var allCustomProps = from a in _linked._extra where !(from b in clrProperties where a.Key == b.Name select b).Any() select new CustomPropertyInfoHelper(a.Key, a.Value.type);

                if (clrProperties != null)
                {
                    return clrProperties.Concat(allCustomProps).ToArray();
                }
                else
                {
                    return allCustomProps?.ToArray();
                }
            }

            protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
            {
                // Look for the CLR property with this name first.
                PropertyInfo propertyInfo = (from prop in GetProperties(bindingAttr) where prop.Name == name select prop).FirstOrDefault();
                if (propertyInfo == null)
                {
                    // If the CLR property was not found, return a custom property
                    if (_linked._extra.TryGetValue(name, out var val))
                    {
                        return new CustomPropertyInfoHelper(name, val.type);
                    }
                }
                return propertyInfo;
            }

            protected override bool HasElementTypeImpl()
            {
                throw new NotImplementedException();
            }

            public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, System.Globalization.CultureInfo culture, string[] namedParameters)
            {
                return _baseType.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
            }

            protected override bool IsArrayImpl()
            {
                throw new NotImplementedException();
            }

            protected override bool IsByRefImpl()
            {
                throw new NotImplementedException();
            }

            protected override bool IsCOMObjectImpl()
            {
                throw new NotImplementedException();
            }

            protected override bool IsPointerImpl()
            {
                throw new NotImplementedException();
            }

            protected override bool IsPrimitiveImpl()
            {
                return _baseType.IsPrimitive;
            }

            public override Module Module
            {
                get { return _baseType.Module; }
            }

            public override string Namespace
            {
                get { return _baseType.Namespace; }
            }

            public override Type UnderlyingSystemType
            {
                get { return _baseType.UnderlyingSystemType; }
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                return _baseType.GetCustomAttributes(attributeType, inherit);
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                return _baseType.GetCustomAttributes(inherit);
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                return _baseType.IsDefined(attributeType, inherit);
            }

            public override string Name
            {
                get { return _baseType.Name; }
            }
        }
    }
}
