using System;

namespace Algoloop.ViewSupport
{
    // Abstracts DataGridColumn in view-model layer.
    public class ItemProperty
    {
        public Type PropertyType { get; private set; }
        public string Name { get; private set; }
        public bool IsReadOnly { get; private set; }

        public ItemProperty(Type propertyType, string name, bool isReadOnly)
        {
            this.PropertyType = propertyType;
            this.Name = name;
            this.IsReadOnly = isReadOnly;
        }
    }
}
