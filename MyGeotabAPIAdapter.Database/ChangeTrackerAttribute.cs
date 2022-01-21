using System;

namespace MyGeotabAPIAdapter.Database
{
    /// <summary>
    /// Identifies the property on a data model class that is to be used for change tracking.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ChangeTrackerAttribute : Attribute
    {
    }
}
