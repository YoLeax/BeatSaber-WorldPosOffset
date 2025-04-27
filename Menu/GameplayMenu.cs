using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using UnityEngine;
using HMUI;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.Attributes;
using IPA.Logging;
using ModestTree;
using Zenject;


namespace WorldPosOffset.Menu;

internal class GameplayMenu : IInitializable, ITickable, IDisposable, INotifyPropertyChanged
{
    private const string MenuName = "World Pos Offset";
    private const string ResourcePath = "WorldPosOffset.Menu.gameplayMenu.bsml";
    private const float DefaultAlpha = 0.4f;
    private const float SelectedAlpha = 1.0f;
    private const float ResetWaitSeconds = 2.0f;

    private enum Axis
    {
        None,
        LeftX,
        LeftY,
        LeftZ,
        RightX,
        RightY,
        RightZ,
    }

    private enum Side
    {
        None = 0,
        Left = 1,
        Right = 2,
        Both = 3,
    }

    private enum ModifyType
    {
        Delta,
        Target,
    }

    private enum ResetState
    {
        Default,
        StartConfirming,
        Confirming,
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    private readonly PluginConfig _cfg;
    private Axis _curSelectedAxis = Axis.None;
    private ResetState _resetL_State = ResetState.Default;
    private ResetState _resetCur_State = ResetState.Default;
    private ResetState _resetR_State = ResetState.Default;
    private float _resetL_WaitUntil = 0.0f;
    private float _resetCur_WaitUntil = 0.0f;
    private float _resetR_WaitUntil = 0.0f;

    public GameplayMenu(PluginConfig pluginConfig)
    {
        _cfg = pluginConfig;
    }
    
    // Zenject will call IInitializable.Initialize for any menu bindings when the main menu loads for the first
    // time or when the game restarts internally, such as when settings are applied.
    public void Initialize()
    {
        GameplaySetup.instance.AddTab(MenuName, ResourcePath, this);
        
        for (int i = 0; i < PluginConfig.MaxPresetCount; ++i)
        {
            PresetOptions.Add($"Preset {i + 1}");
        }
    }

    public void Tick()
    {
        ResetTick(ref _resetL_State, ref _resetL_WaitUntil, nameof(ResetL_Text));
        ResetTick(ref _resetCur_State, ref _resetCur_WaitUntil, nameof(ResetCur_Text));
        ResetTick(ref _resetR_State, ref _resetR_WaitUntil, nameof(ResetR_Text));
    }

    public void Dispose()
    {
        if (GameplaySetup.instance != null)
        {
            _cfg.Changed();
            GameplaySetup.instance.RemoveTab(MenuName);
        }
    }

    #region Top
    [UIValue("enabled")] private bool Enabled => _cfg.Enabled;
    [UIAction("on_enabled_change")]
    private void OnEnabledChange(bool value)
    {
        _cfg.Enabled = value;
    }

    [UIValue("cur_preset")] private string CurPreset => PresetOptions[_cfg.CurPresetIndex].ToString();
    [UIAction("on_preset_change")]
    private void OnPresetChange(string value)
    {
        _cfg.CurPresetIndex = PresetOptions.IndexOf(value);
        RefreshSelections(Side.Both);
    }
    [UIValue("preset_options")] private List<object> PresetOptions = new();
    #endregion

    #region Axis Selection
    [UIValue("left_x_text")] private string LeftX_Text => NumFormatter(_cfg.CurPreset.LeftOffset.x);
    [UIValue("left_y_text")] private string LeftY_Text => NumFormatter(_cfg.CurPreset.LeftOffset.y);
    [UIValue("left_z_text")] private string LeftZ_Text => NumFormatter(_cfg.CurPreset.LeftOffset.z);
    [UIValue("right_x_text")] private string RightX_Text => NumFormatter(_cfg.CurPreset.RightOffset.x);
    [UIValue("right_y_text")] private string RightY_Text => NumFormatter(_cfg.CurPreset.RightOffset.y);
    [UIValue("right_z_text")] private string RightZ_Text => NumFormatter(_cfg.CurPreset.RightOffset.z);
    private string NumFormatter(float value) => (value * 100).ToString("F1");

    [UIComponent("left_x_bg")] private readonly ImageView LeftX_BG = null!;
    [UIComponent("left_y_bg")] private readonly ImageView LeftY_BG = null!;
    [UIComponent("left_z_bg")] private readonly ImageView LeftZ_BG = null!;
    [UIComponent("right_x_bg")] private readonly ImageView RightX_BG = null!;
    [UIComponent("right_y_bg")] private readonly ImageView RightY_BG = null!;
    [UIComponent("right_z_bg")] private readonly ImageView RightZ_BG = null!;
    private ImageView? GetSelectionBG(Axis axis)
    {
        switch (axis)
        {
            case Axis.LeftX: return LeftX_BG;
            case Axis.LeftY: return LeftY_BG;
            case Axis.LeftZ: return LeftZ_BG;
            case Axis.RightX: return RightX_BG;
            case Axis.RightY: return RightY_BG;
            case Axis.RightZ: return RightZ_BG;
            default: return null;
        }
    }

    [UIAction("left_x_click")] private void LeftX_Click() => OnSelectionClick(Axis.LeftX);
    [UIAction("left_y_click")] private void LeftY_Click() => OnSelectionClick(Axis.LeftY);
    [UIAction("left_z_click")] private void LeftZ_Click() => OnSelectionClick(Axis.LeftZ);
    [UIAction("right_x_click")] private void RightX_Click() => OnSelectionClick(Axis.RightX);
    [UIAction("right_y_click")] private void RightY_Click() => OnSelectionClick(Axis.RightY);
    [UIAction("right_z_click")] private void RightZ_Click() => OnSelectionClick(Axis.RightZ);
    private void OnSelectionClick(Axis axis)
    {
        if (axis == _curSelectedAxis)
        {
            Select(axis, false);
            _curSelectedAxis = Axis.None;
        }
        else
        {
            Select(_curSelectedAxis, false);
            Select(axis, true);
            _curSelectedAxis = axis;
        }
        NotifyPropertyChanged(nameof(AnyAxisSelected));
    }
    private void Select(Axis axis, bool isSelect)
    {
        ImageView? iv = GetSelectionBG(axis);
        if (iv == null) return;

        Color c = iv.color;
        c.a = isSelect ? SelectedAlpha : DefaultAlpha;
        iv.color = c;
    }

    [UIValue("any_axis_selected")] private bool AnyAxisSelected => _curSelectedAxis != Axis.None;
    #endregion

    #region Operation Button
    [UIValue("reset_l_text")] private string ResetL_Text => _resetL_State == ResetState.Default ? "Reset L" : "SURE?";
    [UIValue("reset_cur_text")] private string ResetCur_Text => _resetCur_State == ResetState.Default ? "Reset" : "SURE?";
    [UIValue("reset_r_text")] private string ResetR_Text => _resetR_State == ResetState.Default ? "Reset R" : "SURE?";

    [UIAction("reset_l_click")]
    private void ResetL_Click()
    {
        if (_resetL_State == ResetState.Default)
        {
            _resetL_State = ResetState.StartConfirming;
            NotifyPropertyChanged(nameof(ResetL_Text));
        }
        else
        {
            _cfg.CurPreset.LeftOffset = Vector3.zero;
            RefreshSelections(Side.Left);
            _resetL_State = ResetState.Default;
            NotifyPropertyChanged(nameof(ResetL_Text));
        }
    }
    [UIAction("reset_cur_click")]
    private void ResetCur_Click()
    {
        if (_curSelectedAxis == Axis.None) return;
        
        if (_resetCur_State == ResetState.Default)
        {
            _resetCur_State = ResetState.StartConfirming;
            NotifyPropertyChanged(nameof(ResetCur_Text));
        }
        else
        {
            ModifyAxis(_curSelectedAxis, ModifyType.Target, 0.0f);
            _resetCur_State = ResetState.Default;
            NotifyPropertyChanged(nameof(ResetCur_Text));
        }
    }
    [UIAction("reset_r_click")]
    private void ResetR_Click()
    {
        if (_resetR_State == ResetState.Default)
        {
            _resetR_State = ResetState.StartConfirming;
            NotifyPropertyChanged(nameof(ResetR_Text));
        }
        else
        {
            _cfg.CurPreset.RightOffset = Vector3.zero;
            RefreshSelections(Side.Right);
            _resetR_State = ResetState.Default;
            NotifyPropertyChanged(nameof(ResetR_Text));
        }
    }
    private void ResetTick(ref ResetState state, ref float waitUntil, string propertyName)
    {
        if (state == ResetState.StartConfirming)
        {
            waitUntil = Time.time + ResetWaitSeconds;
            state = ResetState.Confirming;
        }
        else if (state == ResetState.Confirming)
        {
            if (Time.time > waitUntil)
            {
                state = ResetState.Default;
                NotifyPropertyChanged(propertyName);
            }
        }
    }

    [UIAction("minus_0_1")] private void Minus_0_1() => ModifyAxis(_curSelectedAxis, ModifyType.Delta, -0.001f);
    [UIAction("add_0_1")] private void Add_0_1() => ModifyAxis(_curSelectedAxis, ModifyType.Delta, 0.001f);
    [UIAction("minus_0_5")] private void Minus_0_5() => ModifyAxis(_curSelectedAxis, ModifyType.Delta, -0.005f);
    [UIAction("add_0_5")] private void Add_0_5() => ModifyAxis(_curSelectedAxis, ModifyType.Delta, 0.005f);
    [UIAction("minus_1")] private void Minus_1() => ModifyAxis(_curSelectedAxis, ModifyType.Delta, -0.01f);
    [UIAction("add_1")] private void Add_1() => ModifyAxis(_curSelectedAxis, ModifyType.Delta, 0.01f);
    [UIAction("minus_5")] private void Minus_5() => ModifyAxis(_curSelectedAxis, ModifyType.Delta, -0.05f);
    [UIAction("add_5")] private void Add_5() => ModifyAxis(_curSelectedAxis, ModifyType.Delta, 0.05f);
    [UIAction("minus_10")] private void Minus_10() => ModifyAxis(_curSelectedAxis, ModifyType.Delta, -0.1f);
    [UIAction("add_10")] private void Add_10() => ModifyAxis(_curSelectedAxis, ModifyType.Delta, 0.1f);
    #endregion

    private void ModifyAxis(Axis axis, ModifyType modify, float value)
    {
        Vector3 leftOffset = _cfg.CurPreset.LeftOffset;
        Vector3 rightOffset = _cfg.CurPreset.RightOffset;
        switch (axis)
        {
            case Axis.LeftX:
                if (modify == ModifyType.Delta) leftOffset.x += value;
                else if (modify == ModifyType.Target) leftOffset.x = value;
                _cfg.CurPreset.LeftOffset = leftOffset;
                NotifyPropertyChanged(nameof(LeftX_Text));
                break;
            case Axis.LeftY: 
                if (modify == ModifyType.Delta) leftOffset.y += value;
                else if (modify == ModifyType.Target) leftOffset.y = value;
                _cfg.CurPreset.LeftOffset = leftOffset;
                NotifyPropertyChanged(nameof(LeftY_Text));
                break;
            case Axis.LeftZ: 
                if (modify == ModifyType.Delta) leftOffset.z += value;
                else if (modify == ModifyType.Target) leftOffset.z = value;
                _cfg.CurPreset.LeftOffset = leftOffset;
                NotifyPropertyChanged(nameof(LeftZ_Text));
                break;
            case Axis.RightX: 
                if (modify == ModifyType.Delta) rightOffset.x += value;
                else if (modify == ModifyType.Target) rightOffset.x = value;
                _cfg.CurPreset.RightOffset = rightOffset;
                NotifyPropertyChanged(nameof(RightX_Text));
                break;
            case Axis.RightY: 
                if (modify == ModifyType.Delta) rightOffset.y += value;
                else if (modify == ModifyType.Target) rightOffset.y = value;
                _cfg.CurPreset.RightOffset = rightOffset;
                NotifyPropertyChanged(nameof(RightY_Text));
                break;
            case Axis.RightZ: 
                if (modify == ModifyType.Delta) rightOffset.z += value;
                else if (modify == ModifyType.Target) rightOffset.z = value;
                _cfg.CurPreset.RightOffset = rightOffset;
                NotifyPropertyChanged(nameof(RightZ_Text));
                break;
        }
    }
    
    private void RefreshSelections(Side side)
    {
        if (side.HasFlag(Side.Left))
        {
            NotifyPropertyChanged(nameof(LeftX_Text));
            NotifyPropertyChanged(nameof(LeftY_Text));
            NotifyPropertyChanged(nameof(LeftZ_Text));
        }
        if (side.HasFlag(Side.Right))
        {
            NotifyPropertyChanged(nameof(RightX_Text));
            NotifyPropertyChanged(nameof(RightY_Text));
            NotifyPropertyChanged(nameof(RightZ_Text));
        }
    }
    
    private void NotifyPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}