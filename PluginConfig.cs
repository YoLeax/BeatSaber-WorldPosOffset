using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace WorldPosOffset;

internal class PluginConfig
{
    // Members must be 'virtual' if you want BSIPA to detect a value change and save the config automatically
    // You can assign a default value to be used when the config is first created by assigning one after '=' 
    // examples:
    // public virtual bool FeatureEnabled { get; set; } = true;
    // public virtual int NumValue { get; set; } = 42;
    // public virtual Color TheColor { get; set; } = new Color(0.12f, 0.34f, 0.56f);

    public const int MaxPresetCount = 5;

    public virtual bool Enabled { get; set; } = false;
    public virtual int CurPresetIndex { get; set; } = 0;

    [UseConverter(typeof(ListConverter<Preset>))]
    [NonNullable]
    public virtual List<Preset> Presets { get; set; } = new();

    /*
    /// <summary>
    /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
    /// </summary>
    public virtual void OnReload() { }
    */

    /// <summary>
    /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
    /// </summary>
    public virtual void Changed() { }

    /*
    /// <summary>
    /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
    /// </summary>
    public virtual void CopyFrom(PluginConfig other) { }
    */

    internal void Init()
    {
        if (CurPresetIndex < 0) CurPresetIndex = 0;
        if (CurPresetIndex >= MaxPresetCount) CurPresetIndex = MaxPresetCount - 1;
        
        int presetCount = Presets.Count;
        int index = 0;
        while (index < MaxPresetCount && index < presetCount)
        {
            if (Presets[index] == null) Presets[index] = new Preset();
            ++index;
        }
        while (index < MaxPresetCount)
        {
            Presets.Add(new Preset());
            ++index;
        }
    }

    public Preset CurPreset => Presets[CurPresetIndex];
    
    public class Preset
    {
        public virtual Vector3 LeftOffset { get; set; } = Vector3.zero;
        public virtual Vector3 RightOffset { get; set; } = Vector3.zero;
    }
}