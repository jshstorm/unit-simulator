using System;

namespace ReferenceModels.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SheetColumnAttribute : Attribute
    {
        public string HeaderName { get; }

        public SheetColumnAttribute(string headerName)
        {
            HeaderName = headerName ?? throw new ArgumentNullException(nameof(headerName));
        }
    }
}