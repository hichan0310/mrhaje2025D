using NUnit.Framework;
using PlayerSystem.Weapons;

public class WeaponRulesTests
{
    [TestCase(1, new[] { 5 })]
    [TestCase(2, new[] { 3, 2 })]
    [TestCase(3, new[] { 2, 2, 1 })]
    [TestCase(4, new[] { 2, 1, 1, 1 })]
    [TestCase(5, new[] { 1, 1, 1, 1, 1 })]
    [TestCase(9, new[] { 1, 1, 1, 1, 1 })]
    public void FiveGunsAreDistributedToHealthRankedTargets(int targets, int[] expected)
    {
        CollectionAssert.AreEqual(expected, WeaponRules.AllocateShotguns(targets));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void NoTargetReceivesNoGuns(int targets)
    {
        Assert.That(WeaponRules.AllocateShotguns(targets), Is.Empty);
    }

    [Test]
    public void AttackSpeedAndUltimateRampMultiplyFireRate()
    {
        Assert.That(WeaponRules.FireInterval(0.12f, 2f, 3f), Is.EqualTo(0.02f).Within(0.0001f));
        Assert.That(WeaponRules.FireInterval(0.12f, 1f, 2f), Is.EqualTo(0.06f).Within(0.0001f));
        Assert.That(WeaponRules.FireInterval(0.12f, 1000f, 1000f), Is.EqualTo(0.02f));
    }
    [TestCase(1, 5)]
    [TestCase(2, 6)]
    [TestCase(5.75f, 9)]
    [TestCase(0, 5)]
    public void UltimateGunCountIncludesOnlyBonusProjectiles(float rate, int expected)
    {
        Assert.That(WeaponRules.ShotgunCount(rate), Is.EqualTo(expected));
    }

    [TestCase(2, 7, new[] { 4, 3 })]
    [TestCase(4, 6, new[] { 2, 2, 1, 1 })]
    [TestCase(8, 7, new[] { 1, 1, 1, 1, 1, 1, 1 })]
    [TestCase(1, 8, new[] { 8 })]
    public void AdditionalGunsStillFavorHigherHealthTargets(int targets, int guns, int[] expected)
    {
        CollectionAssert.AreEqual(expected, WeaponRules.AllocateShotguns(targets, guns));
    }

    [TestCase(8)]
    [TestCase(16)]
    public void NormalShotgunVolleyEnergyDoesNotScaleWithPelletCount(int pellets)
    {
        Assert.That(WeaponRules.ShotgunPelletEnergy(false, pellets, .5f, 6) * pellets,
            Is.EqualTo(.5f).Within(.0001f));
        Assert.That(WeaponRules.ShotgunPelletEnergy(true, pellets, .5f, 6), Is.EqualTo(6));
    }

    [TestCase(1, 1)]
    [TestCase(2, 2)]
    [TestCase(2.9f, 2)]
    [TestCase(-1, 1)]
    public void GunKnifeHasOneBaseProjectileWithoutTestBonus(float rate, int expected)
    {
        Assert.That(WeaponRules.ProjectileCount(rate), Is.EqualTo(expected));
    }
}
