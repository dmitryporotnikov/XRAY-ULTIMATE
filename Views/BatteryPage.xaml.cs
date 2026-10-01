using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XRAY_ULTIMATE.Helpers;
using XRAY_ULTIMATE.Services;

namespace XRAY_ULTIMATE.Views;

public sealed partial class BatteryPage : Page
{
    private List<PowerPlanItem> _powerPlans = new();

    public BatteryPage()
    {
        this.InitializeComponent();
        this.Loaded += BatteryPage_Loaded;
    }

    private void BatteryPage_Loaded(object sender, RoutedEventArgs e)
    {
        var bat = SystemDiagnosticsService.Instance.CurrentReport.Battery;

        if (bat.IsBatteryPresent)
        {
            TxtBatteryHeader.Text = $"Battery: {bat.BatteryState}";
            TxtPowerSource.Text = $"Source: {bat.PowerLineStatus}";
            TxtChargePercent.Text = bat.BatteryLifePercent >= 0 ? $"{bat.BatteryLifePercent}%" : "N/A";
            PrgCharge.Value = bat.BatteryLifePercent >= 0 ? bat.BatteryLifePercent : 100;

            if (bat.EstimatedRunTimeSeconds > 0 && bat.EstimatedRunTimeSeconds < 72000)
            {
                var span = TimeSpan.FromSeconds(bat.EstimatedRunTimeSeconds);
                TxtRemainingTime.Text = $"Estimated Runtime: {span.Hours}h {span.Minutes}m";
            }
            else
            {
                TxtRemainingTime.Text = bat.PowerLineStatus;
            }

            TxtChemistry.Text = string.IsNullOrEmpty(bat.Chemistry) ? "Technology: Standard" : $"Technology: {bat.Chemistry}";

            double health = bat.BatteryHealthPercent > 0 ? bat.BatteryHealthPercent : 100;
            TxtHealthPercent.Text = $"Health: {health:F1}%";
            PrgHealth.Value = health;
            TxtFullCap.Text = bat.FullChargeCapacityMWh > 0 ? $"Full: {bat.FullChargeCapacityMWh} mWh" : "";
            TxtDesignCap.Text = bat.DesignCapacityMWh > 0 ? $"Design: {bat.DesignCapacityMWh} mWh" : "";
        }
        else
        {
            TxtBatteryHeader.Text = "Desktop Workstation (AC Line Power)";
            TxtPowerSource.Text = "No Battery Subsystem Installed";
            TxtChargePercent.Text = "AC";
            PrgCharge.Value = 100;
            TxtRemainingTime.Text = "Continuous AC Power";
            TxtChemistry.Text = "N/A";
            TxtHealthPercent.Text = "No Battery";
            PrgHealth.Value = 100;
            TxtFullCap.Text = "-";
            TxtDesignCap.Text = "-";
        }

        TxtPlanName.Text = string.IsNullOrEmpty(bat.ActivePowerScheme) ? "Balanced" : bat.ActivePowerScheme;
        LstBatteryProperties.ItemsSource = bat.Properties;

        LoadPowerPlans();
    }

    private void LoadPowerPlans()
    {
        _powerPlans = PowerPlanHelper.GetPowerPlans();
        CmbPowerPlans.ItemsSource = null;
        CmbPowerPlans.ItemsSource = _powerPlans.Select(p => p.DisplayText).ToList();

        var active = _powerPlans.FirstOrDefault(p => p.IsActive);
        if (active != null)
        {
            TxtPlanName.Text = active.Name;
            CmbPowerPlans.SelectedItem = active.DisplayText;
        }
    }

    private void BtnOpenPowerSettings_Click(object sender, RoutedEventArgs e)
    {
        PowerPlanHelper.OpenPowerSettings();
    }

    private void BtnApplyPlan_Click(object sender, RoutedEventArgs e)
    {
        int selectedIndex = CmbPowerPlans.SelectedIndex;
        if (selectedIndex >= 0 && selectedIndex < _powerPlans.Count)
        {
            var plan = _powerPlans[selectedIndex];
            bool success = PowerPlanHelper.SetActivePlan(plan.Guid);
            if (success)
            {
                IbPowerStatus.Title = "Power Plan Applied";
                IbPowerStatus.Message = $"Active power scheme successfully switched to '{plan.Name}'.";
                IbPowerStatus.Severity = InfoBarSeverity.Success;
                IbPowerStatus.IsOpen = true;

                LoadPowerPlans();
            }
            else
            {
                IbPowerStatus.Title = "Switch Failed";
                IbPowerStatus.Message = $"Could not switch to power plan '{plan.Name}'.";
                IbPowerStatus.Severity = InfoBarSeverity.Error;
                IbPowerStatus.IsOpen = true;
            }
        }
    }

    private void MenuRestoreHighPerf_Click(object sender, RoutedEventArgs e)
    {
        RestoreAndActivatePlan(PowerPlanHelper.StandardHighPerformance.Name, PowerPlanHelper.StandardHighPerformance.Guid);
    }

    private void MenuRestorePowerSaver_Click(object sender, RoutedEventArgs e)
    {
        RestoreAndActivatePlan(PowerPlanHelper.StandardPowerSaver.Name, PowerPlanHelper.StandardPowerSaver.Guid);
    }

    private void MenuRestoreUltimate_Click(object sender, RoutedEventArgs e)
    {
        RestoreAndActivatePlan(PowerPlanHelper.StandardUltimate.Name, PowerPlanHelper.StandardUltimate.Guid);
    }

    private void RestoreAndActivatePlan(string planName, string baseGuid)
    {
        if (PowerPlanHelper.RestoreStandardPlan(baseGuid, out string newGuid))
        {
            PowerPlanHelper.SetActivePlan(newGuid);
            IbPowerStatus.Title = "Plan Restored";
            IbPowerStatus.Message = $"'{planName}' scheme was restored and activated successfully.";
            IbPowerStatus.Severity = InfoBarSeverity.Success;
            IbPowerStatus.IsOpen = true;
            LoadPowerPlans();
        }
        else
        {
            IbPowerStatus.Title = "Restore Failed";
            IbPowerStatus.Message = $"Could not restore '{planName}' scheme on this system.";
            IbPowerStatus.Severity = InfoBarSeverity.Error;
            IbPowerStatus.IsOpen = true;
        }
    }
}
