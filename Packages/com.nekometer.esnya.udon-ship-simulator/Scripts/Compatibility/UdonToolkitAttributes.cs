using System;
using UnityEngine;

namespace UdonToolkit
{
    // Minimal local compatibility layer for the attribute subset this package uses.
    // These are inspector hints only; they intentionally preserve compilation without
    // requiring the external UdonToolkit package.
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class ListViewAttribute : PropertyAttribute
    {
        public string Group { get; }

        public ListViewAttribute(string group)
        {
            Group = group;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class PopupAttribute : PropertyAttribute
    {
        public string Source { get; }
        public string Target { get; }
        public bool AllowEmpty { get; }

        public PopupAttribute(string source)
        {
            Source = source;
        }

        public PopupAttribute(string source, string target)
        {
            Source = source;
            Target = target;
        }

        public PopupAttribute(string source, string target, bool allowEmpty)
        {
            Source = source;
            Target = target;
            AllowEmpty = allowEmpty;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class HelpBoxAttribute : PropertyAttribute
    {
        public string Text { get; }

        public HelpBoxAttribute(string text)
        {
            Text = text;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SectionHeaderAttribute : PropertyAttribute
    {
        public string Header { get; }

        public SectionHeaderAttribute(string header)
        {
            Header = header;
        }
    }
}
