using System;
using System.Collections;
using System.Collections.ObjectModel;

namespace SwitchableProperties
{
    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class SwitchablePropertiesSettings
    {
        public ObservableCollection<SwitchableProperty> Properties { get; set; }
    }
}