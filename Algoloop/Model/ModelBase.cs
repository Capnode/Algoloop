/*
 * Copyright 2018 Capnode AB
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
using System.ComponentModel;
using System.Reflection;

namespace Algoloop.Model
{
    [Serializable]
    abstract public class ModelBase
    {
        protected void SetBrowsable(string property, bool value)
        {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(this.GetType())[property];
            BrowsableAttribute attribute = (BrowsableAttribute)descriptor.Attributes[typeof(BrowsableAttribute)];
            FieldInfo fieldToChange = attribute.GetType().GetField("browsable", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldToChange.SetValue(attribute, value);
        }

        protected void SetReadonly(string property, bool value)
        {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(this.GetType())[property];
            ReadOnlyAttribute attribute = (ReadOnlyAttribute)descriptor.Attributes[typeof(ReadOnlyAttribute)];
            FieldInfo fieldToChange = attribute.GetType().GetField("isReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldToChange.SetValue(attribute, value);
        }

        protected void SetTitle(string property, string value)
        {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(this.GetType())[property];
            DisplayNameAttribute attribute = (DisplayNameAttribute)descriptor.Attributes[typeof(DisplayNameAttribute)];
            FieldInfo fieldToChange = attribute.GetType().GetField("_displayName", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldToChange.SetValue(attribute, value);
        }

        protected void SetDescription(string property, string value)
        {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(this.GetType())[property];
            DescriptionAttribute attribute = (DescriptionAttribute)descriptor.Attributes[typeof(DescriptionAttribute)];
            FieldInfo fieldToChange = attribute.GetType().GetField("description", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldToChange.SetValue(attribute, value);
        }

        protected void SetValue(string property, bool value)
        {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(this.GetType())[property];
            descriptor.SetValue(this, value);
        }

        protected bool GetValue(string property)
        {
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(this.GetType())[property];
            return (bool) descriptor.GetValue(this);
        }
    }
}
