﻿using StatisticsAnalysisTool.Common;
using StatisticsAnalysisTool.Localization;
using StatisticsAnalysisTool.Models;
using StatisticsAnalysisTool.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace StatisticsAnalysisTool.DamageMeter;
public sealed class DamageMeterSnapshotFragment : BaseViewModel
{
    private string _causerMainHandItemUniqueName;
    private long _damage;
    private double _dps;
    private long _heal;
    private double _hps;
    private long _takenDamage;
    private string _takenDamageShortString;
    private double _takenDamageInPercent;
    private double _takenDamagePercentage;
    private DamageMeterStyleFragmentType _damageMeterStyleFragmentType;
    private Visibility _spellsContainerVisibility = Visibility.Collapsed;

    public DamageMeterSnapshotFragment(DamageMeterFragment damageMeterFragment)
    {
        Name = damageMeterFragment.Name;
        CauserGuid = damageMeterFragment.CauserGuid;
        CombatTime = damageMeterFragment.CombatTime;
        Damage = damageMeterFragment.Damage;
        Dps = damageMeterFragment.Dps;
        DamageInPercent = damageMeterFragment.DamageInPercent;
        DamagePercentage = damageMeterFragment.DamagePercentage;
        Heal = damageMeterFragment.Heal;
        Hps = damageMeterFragment.Hps;
        HealInPercent = damageMeterFragment.HealInPercent;
        HealPercentage = damageMeterFragment.HealPercentage;
        TakenDamage = damageMeterFragment.TakenDamage;
        TakenDamageInPercent = damageMeterFragment.TakenDamageInPercent;
        TakenDamagePercentage = damageMeterFragment.TakenDamagePercentage;
        CauserMainHandItemUniqueName = damageMeterFragment.CauserMainHand?.UniqueName ?? string.Empty;
        OverhealedPercentageOfTotalHealing = damageMeterFragment.OverhealedPercentageOfTotalHealing;
        Spells = damageMeterFragment.Spells.Select(x => new SpellsSnapshotFragment()
        {
            SpellIndex = x.SpellIndex,
            ItemIndex = x.ItemIndex,
            UniqueName = x.UniqueName,
            DamageHealValue = x.DamageHealValue,
            DamageHealShortString = x.DamageHealShortString,
            Target = x.Target,
            Category = x.Category,
            Ticks = x.Ticks,
            DamageInPercent = x.DamageInPercent,
            DamagePercentage = x.DamagePercentage,
            HealthChangeType = x.HealthChangeType
        }).ToList();
    }

    public DamageMeterSnapshotFragment()
    {
    }

    public string Name { get; init; }
    public Guid CauserGuid { get; init; }
    public bool IsDamageMeterShowing { get; set; } = true;
    public TimeSpan CombatTime { get; init; }

    #region Damage

    public long Damage
    {
        get => _damage;
        set
        {
            _damage = value;
            DamageShortString = _damage.ToShortNumberString();
        }
    }

    public string DamageShortString { get; set; }

    public double Dps
    {
        get => _dps;
        set
        {
            _dps = value;
            DpsString = _dps.ToShortNumberString();
        }
    }

    public string DpsString { get; set; }

    public double DamageInPercent { get; set; }

    public double DamagePercentage { get; set; }

    #endregion

    #region Heal

    public long Heal
    {
        get => _heal;
        set
        {
            _heal = value;
            HealShortString = _heal.ToShortNumberString();
        }
    }

    public string HealShortString { get; private set; }

    public string HpsString { get; private set; }

    public double Hps
    {
        get => _hps;
        set
        {
            _hps = value;
            HpsString = _hps.ToShortNumberString();
        }
    }

    public double HealInPercent { get; set; }

    public double HealPercentage { get; set; }

    public double OverhealedPercentageOfTotalHealing { get; set; }

    #endregion

    #region Spells

    public List<SpellsSnapshotFragment> Spells { get; init; } = new ();

    public Visibility SpellsContainerVisibility
    {
        get => _spellsContainerVisibility;
        set
        {
            _spellsContainerVisibility = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Take Damage

    public long TakenDamage
    {
        get => _takenDamage;
        set
        {
            _takenDamage = value;
            TakenDamageShortString = _damage.ToShortNumberString();
            OnPropertyChanged();
        }
    }

    public string TakenDamageShortString
    {
        get => _takenDamageShortString;
        private set
        {
            _takenDamageShortString = value;
            OnPropertyChanged();
        }
    }

    public double TakenDamageInPercent
    {
        get => _takenDamageInPercent;
        set
        {
            _takenDamageInPercent = value;
            OnPropertyChanged();
        }
    }

    public double TakenDamagePercentage
    {
        get => _takenDamagePercentage;
        set
        {
            _takenDamagePercentage = value;
            OnPropertyChanged();
        }
    }

    #endregion

    public DamageMeterStyleFragmentType DamageMeterStyleFragmentType
    {
        get => _damageMeterStyleFragmentType;
        set
        {
            _damageMeterStyleFragmentType = value;
            OnPropertyChanged();
        }
    }

    private void PerformShowSpells(object value)
    {
        SpellsContainerVisibility = SpellsContainerVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private ICommand _showSpells;
    public ICommand ShowSpells => _showSpells ??= new CommandHandler(PerformShowSpells, true);

    public string TranslationCombatTime => LocalizationController.Translation("COMBAT_TIME");
    public static string TranslationDmgPercent => LocalizationController.Translation("DMG_PERCENT");
    public static string TranslationName => LocalizationController.Translation("NAME");
    public static string TranslationDamageHeal => LocalizationController.Translation("DAMAGE_HEAL");
    public static string TranslationTicks => LocalizationController.Translation("TICKS");

    public string CauserMainHandItemUniqueName
    {
        get => _causerMainHandItemUniqueName;
        set
        {
            _causerMainHandItemUniqueName = value;
            var item = ItemController.GetItemByUniqueName(_causerMainHandItemUniqueName);
            ShopSubCategory = CategoryController.ShopSubCategoryToShopSubCategoryString(item?.ShopSubCategory1 ?? Common.ShopSubCategory.Unknown);
        }
    }

    public Item CauserMainHand => ItemController.GetItemByUniqueName(CauserMainHandItemUniqueName);
    public string ShopSubCategory { get; set; }
}