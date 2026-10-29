using NUnit.Framework;
using PlayerSystem.Weapons;

public class WeaponHudStateTests
{
    [Test]
    public void UltimateUsesEnergyCapForFillAndCostForReadiness()
    {
        var entry = WeaponHudEntry.Ultimate("Ultimate", 60, 100, 60);
        Assert.That(entry.Ready, Is.True);
        Assert.That(entry.Fill, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(entry.Threshold, Is.EqualTo(0.6f).Within(0.001f));
        Assert.That(entry.Value, Is.EqualTo("60 / 100"));
    }

    [Test]
    public void ReadyChargeRemainsUsableWhileNextChargeRefills()
    {
        var entry = WeaponHudEntry.Skill("Skill", 1, 4, 3, 6);
        Assert.That(entry.Ready, Is.True);
        Assert.That(entry.Charges, Is.EqualTo(1));
        Assert.That(entry.Fill, Is.EqualTo(0.5f));
        Assert.That(entry.Status, Does.Contain("3.0"));
    }

    [Test]
    public void EmptySkillIsNotReadyAndFullSkillHasFullGauge()
    {
        Assert.That(WeaponHudEntry.Skill("Skill", 0, 4, 6, 6).Ready, Is.False);
        Assert.That(WeaponHudEntry.Skill("Skill", 4, 4, 6, 6).Fill, Is.EqualTo(1));
    }

    [Test]
    public void WeaponRestrictionsOverrideAvailableResources()
    {
        var entry = WeaponHudEntry.Ultimate("Ultimate", 100, 100, 60, "No targets");
        Assert.That(entry.Ready, Is.False);
        Assert.That(entry.Status, Is.EqualTo("No targets"));
        Assert.That(WeaponHudEntry.Skill("Skill", 4, 4, 0, 6, "Aiming").Ready, Is.False);
    }

    [Test]
    public void ActiveUltimateIsDistinctFromReadyAndRetainsRemainingEnergy()
    {
        var entry = WeaponHudEntry.Ultimate("Ultimate", 40, 100, 60, "Active", true);
        Assert.That(entry.Active, Is.True);
        Assert.That(entry.Ready, Is.False);
        Assert.That(entry.Fill, Is.EqualTo(0.4f).Within(0.001f));
    }

    [Test]
    public void ResetRemovesPreviousWeaponsExtraRowsAndIcon()
    {
        var state = new WeaponHudState { Title = "Previous", Enabled = false };
        state.Entries.Add(WeaponHudEntry.Skill("Skill", 1, 1, 0, 6));
        state.Clear();
        Assert.That(state.Entries, Is.Empty);
        Assert.That(state.Title, Is.Empty);
        Assert.That(state.Enabled, Is.True);
    }
}
