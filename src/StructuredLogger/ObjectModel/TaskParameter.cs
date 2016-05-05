﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.Build.Logging.StructuredLogger
{
    /// <summary>
    /// Abstract base class for task input / output parameters (can be ItemGroups)
    /// </summary>
    public abstract class TaskParameter : ILogNode
    {
        protected bool collapseSingleItem = true;
        public string ItemAttributeName { get; set; }
        protected readonly List<Item> items = new List<Item>();
        public string Name { get; set; }

        protected TaskParameter()
        {
            ItemAttributeName = "Include";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskParameter"/> class.
        /// </summary>
        /// <param name="message">The message from the logging system.</param>
        /// <param name="prefix">The prefix parsed out (e.g. 'Output Item(s): ').</param>
        /// <param name="collapseSingleItem">If set to <c>true</c>, will collapse the node to a single item when possible.</param>
        /// <param name="itemAttributeName">Name of the item 'Include' attribute.</param>
        protected TaskParameter(string message, string prefix, bool collapseSingleItem = true, string itemAttributeName = "Include")
        {
            this.collapseSingleItem = collapseSingleItem;
            this.ItemAttributeName = itemAttributeName ?? "Include";

            string name;
            foreach (var item in ItemGroupParser.ParseItemList(message, prefix, out name))
            {
                AddItem(item);
            }

            Name = name;
        }

        public void AddItem(Item item)
        {
            items.Add(item);
        }

        /// <summary>
        /// Saves the task parameter node to XML XElement.
        /// </summary>
        /// <param name="parentElement">The parent element.</param>
        public void SaveToElement(XElement parentElement)
        {
            XElement element = new XElement(Name);
            parentElement.Add(element);

            if (collapseSingleItem && items.Count == 1 && !items[0].Metadata.Any())
            {
                element.Add(items[0].Text);
            }
            else
            {
                foreach (var item in items)
                {
                    item.SaveToElement(element, ItemAttributeName, collapseSingleItem);
                }
            }
        }

        /// <summary>
        /// Creates a concrete Task Parameter type based on the message logging message.
        /// </summary>
        /// <param name="message">The message string from the logger.</param>
        /// <param name="prefix">The prefix to the message string.</param>
        /// <returns>Concrete task parameter node.</returns>
        public static TaskParameter Create(string message, string prefix)
        {
            switch (prefix)
            {
                case StructuredLogger.OutputItemsMessagePrefix:
                    return new OutputItem(message, prefix);
                case StructuredLogger.TaskParameterMessagePrefix:
                    return new InputParameter(message, prefix);
                case StructuredLogger.OutputPropertyMessagePrefix:
                    return new OutputProperty(message, prefix);
                case StructuredLogger.ItemGroupIncludeMessagePrefix:
                    return new ItemGroup(message, prefix, "Include");
                case StructuredLogger.ItemGroupRemoveMessagePrefix:
                    return new ItemGroup(message, prefix, "Remove");
                default:
                    throw new UnknownTaskParameterPrefixException(prefix);
            }
        }

        public IEnumerable<Item> Items => items;
    }
}
