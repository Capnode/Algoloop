using System;
using System.Linq;
using System.Reflection;

namespace Algoloop.ViewSupport
{
    partial class CustomTypeHelper<T>
    {
        internal class CustomType : Type
        {
            private readonly Type _baseType;
            
            public CustomType(Type delegatingType)
            {
                if (delegatingType == null)
                    throw new ArgumentNullException("delegatingType");
                _baseType = delegatingType;
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
                throw new NotImplementedException();
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
                return _baseType.GetProperties(bindingAttr)
                    .Concat(CustomProperties).ToArray();
            }

            protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
            {
                // Look for the CLR property with this name first.
                return GetProperties(bindingAttr).FirstOrDefault(prop => prop.Name == name)
                    // and then for a custom property
                    ?? CustomProperties.FirstOrDefault(prop => prop.Name == name);
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

            public override bool ContainsGenericParameters
            {
                get { return _baseType.ContainsGenericParameters; }
            }

            public override System.Collections.Generic.IEnumerable<CustomAttributeData> CustomAttributes
            {
                get { return _baseType.CustomAttributes; }
            }

            public override MethodBase DeclaringMethod
            {
                get { return _baseType.DeclaringMethod; }
            }

            public override Type DeclaringType
            {
                get { return _baseType.DeclaringType; }
            }

            public override bool Equals(Type o)
            {
                return _baseType == o;
            }

            public override Type[] FindInterfaces(TypeFilter filter, object filterCriteria)
            {
                return _baseType.FindInterfaces(filter, filterCriteria);
            }

            public override MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria)
            {
                return _baseType.FindMembers(memberType, bindingAttr, filter, filterCriteria);
            }

            public override GenericParameterAttributes GenericParameterAttributes
            {
                get { return _baseType.GenericParameterAttributes; }
            }

            public override int GenericParameterPosition
            {
                get { return _baseType.GenericParameterPosition; }
            }

            public override Type[] GenericTypeArguments
            {
                get { return _baseType.GenericTypeArguments; }
            }

            public override int GetArrayRank()
            {
                return _baseType.GetArrayRank();
            }

            public override System.Collections.Generic.IList<CustomAttributeData> GetCustomAttributesData()
            {
                return _baseType.GetCustomAttributesData();
            }

            public override MemberInfo[] GetDefaultMembers()
            {
                return _baseType.GetDefaultMembers();
            }

            public override string GetEnumName(object value)
            {
                return _baseType.GetEnumName(value);
            }

            public override string[] GetEnumNames()
            {
                return _baseType.GetEnumNames();
            }

            public override Type GetEnumUnderlyingType()
            {
                return _baseType.GetEnumUnderlyingType();
            }

            public override Array GetEnumValues()
            {
                return _baseType.GetEnumValues();
            }

            public override EventInfo[] GetEvents()
            {
                return _baseType.GetEvents();
            }

            public override Type[] GetGenericArguments()
            {
                return _baseType.GetGenericArguments();
            }

            public override Type[] GetGenericParameterConstraints()
            {
                return _baseType.GetGenericParameterConstraints();
            }

            public override Type GetGenericTypeDefinition()
            {
                return _baseType.GetGenericTypeDefinition();
            }

            public override InterfaceMapping GetInterfaceMap(Type interfaceType)
            {
                return _baseType.GetInterfaceMap(interfaceType);
            }

            public override MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
            {
                return _baseType.GetMember(name, bindingAttr);
            }

            public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
            {
                return _baseType.GetMember(name, type, bindingAttr);
            }

            public override bool IsAssignableFrom(Type c)
            {
                return _baseType.IsAssignableFrom(c);
            }

            public override bool IsConstructedGenericType
            {
                get { return _baseType.IsConstructedGenericType; }
            }

            public override bool IsEnum
            {
                get { return _baseType.IsEnum; }
            }

            public override bool IsEnumDefined(object value)
            {
                return _baseType.IsEnumDefined(value);
            }

            public override bool IsEquivalentTo(Type other)
            {
                return _baseType.IsEquivalentTo(other);
            }

            public override bool IsGenericParameter
            {
                get { return _baseType.IsGenericParameter; }
            }

            public override bool IsGenericType
            {
                get { return _baseType.IsGenericType; }
            }

            public override bool IsGenericTypeDefinition
            {
                get { return _baseType.IsGenericTypeDefinition; }
            }

            public override bool IsInstanceOfType(object o)
            {
                return _baseType.IsInstanceOfType(o);
            }

            public override bool IsSerializable
            {
                get { return _baseType.IsSerializable; }
            }

            public override bool IsSubclassOf(Type c)
            {
                return _baseType.IsSubclassOf(c);
            }

            public override RuntimeTypeHandle TypeHandle
            {
                get { return _baseType.TypeHandle; }
            }

            public override Type MakeArrayType()
            {
                return _baseType.MakeArrayType();
            }

            public override Type MakeArrayType(int rank)
            {
                return _baseType.MakeArrayType(rank);
            }

            public override Type MakeByRefType()
            {
                return _baseType.MakeByRefType();
            }

            public override Type MakeGenericType(params Type[] typeArguments)
            {
                return _baseType.MakeGenericType(typeArguments);
            }

            public override Type MakePointerType()
            {
                return _baseType.MakePointerType();
            }

            public override MemberTypes MemberType
            {
                get { return _baseType.MemberType; }
            }

            public override int MetadataToken
            {
                get { return _baseType.MetadataToken; }
            }

            public override Type ReflectedType
            {
                get { return _baseType.ReflectedType; }
            }

            public override System.Runtime.InteropServices.StructLayoutAttribute StructLayoutAttribute
            {
                get { return _baseType.StructLayoutAttribute; }
            }

            public override string ToString()
            {
                return _baseType.ToString();
            }
        }
    }
}