using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TamQuoc3D.Core.Constants;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.DeckBuilder;

/// <summary>
/// HeroFilter - Thanh cong cu loc va sap xep danh sach tuong theo he nguyen to, vi tri/vai tro va tieu chi sap xep.
/// </summary>
[GlobalClass]
public partial class HeroFilter : HBoxContainer
{
    [Signal]
    public delegate void FilterChangedEventHandler();

    public enum SortMode
    {
        PowerDesc,
        NameAsc,
        Element,
        RarityDesc
    }

    private OptionButton _elementDropdown;
    private OptionButton _roleDropdown;
    private OptionButton _sortDropdown;

    public HeroData.ElementType SelectedElement { get; private set; } = HeroData.ElementType.None;
    public BattlePosition? SelectedRole { get; private set; } = null;
    public SortMode CurrentSortMode { get; private set; } = SortMode.PowerDesc;

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 10);
        SetupDropdowns();
    }

    private void SetupDropdowns()
    {
        // 1. Element Filter
        var elemLabel = new Label { Text = "Hệ:" };
        elemLabel.AddThemeFontSizeOverride("font_size", 11);
        elemLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.8f, 0.7f));
        AddChild(elemLabel);

        _elementDropdown = new OptionButton();
        _elementDropdown.AddItem("Tất cả", 0);
        _elementDropdown.AddItem("🔥 Hỏa", 1);
        _elementDropdown.AddItem("💧 Thủy", 2);
        _elementDropdown.AddItem("🌪️ Phong", 3);
        _elementDropdown.AddItem("✨ Quang", 4);
        _elementDropdown.AddItem("🌑 Ám", 5);
        _elementDropdown.ItemSelected += OnElementSelected;
        AddChild(_elementDropdown);

        // 2. Role Filter
        var roleLabel = new Label { Text = "Vị trí:" };
        roleLabel.AddThemeFontSizeOverride("font_size", 11);
        roleLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.8f, 0.7f));
        AddChild(roleLabel);

        _roleDropdown = new OptionButton();
        _roleDropdown.AddItem("Tất cả", 0);
        _roleDropdown.AddItem("⚔️ Tiền quân", 1);
        _roleDropdown.AddItem("🛡️ Trung quân", 2);
        _roleDropdown.AddItem("🏹 Hậu quân", 3);
        _roleDropdown.ItemSelected += OnRoleSelected;
        AddChild(_roleDropdown);

        // 3. Sort Filter
        var sortLabel = new Label { Text = "Xếp:" };
        sortLabel.AddThemeFontSizeOverride("font_size", 11);
        sortLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.8f, 0.7f));
        AddChild(sortLabel);

        _sortDropdown = new OptionButton();
        _sortDropdown.AddItem("Lực Chiến ↓", 0);
        _sortDropdown.AddItem("Tên A-Z", 1);
        _sortDropdown.AddItem("Phẩm (UR→R)", 2);
        _sortDropdown.AddItem("Hệ nguyên tố", 3);
        _sortDropdown.ItemSelected += OnSortSelected;
        AddChild(_sortDropdown);
    }

    private void OnElementSelected(long index)
    {
        SelectedElement = index switch
        {
            1 => HeroData.ElementType.Fire,
            2 => HeroData.ElementType.Water,
            3 => HeroData.ElementType.Wind,
            4 => HeroData.ElementType.Light,
            5 => HeroData.ElementType.Dark,
            _ => HeroData.ElementType.None
        };
        EmitSignal(SignalName.FilterChanged);
    }

    private void OnRoleSelected(long index)
    {
        SelectedRole = index switch
        {
            1 => BattlePosition.Front,
            2 => BattlePosition.Mid,
            3 => BattlePosition.Back,
            _ => null
        };
        EmitSignal(SignalName.FilterChanged);
    }

    private void OnSortSelected(long index)
    {
        CurrentSortMode = index switch
        {
            1 => SortMode.NameAsc,
            2 => SortMode.RarityDesc,
            3 => SortMode.Element,
            _ => SortMode.PowerDesc
        };
        EmitSignal(SignalName.FilterChanged);
    }

    public List<HeroData> ApplyFilter(IEnumerable<HeroData> heroes)
    {
        var result = heroes.AsEnumerable();

        if (SelectedElement != HeroData.ElementType.None)
        {
            result = result.Where(h => h.Element == SelectedElement);
        }

        if (SelectedRole.HasValue)
        {
            result = result.Where(h => h.Position == SelectedRole.Value);
        }

        result = CurrentSortMode switch
        {
            SortMode.NameAsc => result.OrderBy(h => h.HeroName),
            SortMode.RarityDesc => result.OrderByDescending(h => (int)h.Rarity).ThenByDescending(HeroCard.CalculateCombatPower),
            SortMode.Element => result.OrderBy(h => (int)h.Element).ThenByDescending(HeroCard.CalculateCombatPower),
            _ => result.OrderByDescending(HeroCard.CalculateCombatPower)
        };

        return result.ToList();
    }
}
