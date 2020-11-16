
/// <summary>
/// Segment of polity population migrating from one cell to another
/// </summary>
public class MigratingPolityPopulation : MigratingPopulation
{
    /// <summary>
    /// Constructs a new migrating polity population object
    /// </summary>
    /// <param name="world">world this object belongs to</param>
    /// <param name="prominencePercent">percentage of the source prominence to migrate</param>
    /// <param name="prominenceValueDelta">how much the prominence value should change</param>
    /// <param name="population">population to migrate</param>
    /// <param name="sourceGroup">the cell group this originates from</param>
    /// <param name="polity">the polity to which the migrating population belongs</param>
    /// <param name="targetCell">the cell group this migrates to</param>
    /// <param name="migrationDirection">the direction this group is exiting from the source</param>
    public MigratingPolityPopulation(
        World world,
        float prominencePercent,
        float prominenceValueDelta,
        int population,
        CellGroup sourceGroup,
        Polity polity,
        TerrainCell targetCell,
        Direction migrationDirection)
        : base (
            world,
            prominencePercent,
            prominenceValueDelta,
            population,
            sourceGroup,
            polity,
            targetCell,
            migrationDirection)
    {
    }

    /// <summary>
    /// Sets the object properties to use during a migration event
    /// </summary>
    /// <param name="prominencePercent">percentage of the source prominence to migrate</param>
    /// <param name="prominenceValueDelta">how much the prominence value should change</param>
    /// <param name="population">population to migrate</param>
    /// <param name="sourceGroup">the cell group this originates from</param>
    /// <param name="polity">the polity to which the migrating population belongs</param>
    /// <param name="targetCell">the cell group this migrates to</param>
    /// <param name="migrationDirection">the direction this group is moving out from the source</param>
    public void Set(
        float prominencePercent,
        float prominenceValueDelta,
        int population,
        CellGroup sourceGroup,
        Polity polity,
        TerrainCell targetCell,
        Direction migrationDirection)
    {
        SetInternal(
            prominencePercent,
            prominenceValueDelta,
            population,
            sourceGroup,
            polity,
            targetCell,
            migrationDirection);
    }

    protected override void ApplyProminenceChanges()
    {
        // 'unmerge' the culture of the population that is migrating out
        SourceGroup.Culture.UnmergeCulture(Culture, ProminencePercent);

        // decrease the prominence of the polity whose population is migrating out
        SourceGroup.AddPolityProminenceValueDelta(Polity, -_prominenceValueDelta);
    }

    protected override BufferCulture CreateMigratingCulture()
    {
        float isolationPreference =
            Polity.GetPreferenceValue(CulturalPreference.IsolationPreferenceId);

        BufferCulture culture = new BufferCulture(SourceGroup.Culture);

        // The resulting culture should approximate the polity's culture by it's level
        // of isolation. Meaning that highly isolated polity cultures are less susceptible
        // to influence from the other cultures within the same cell group
        culture.MergeCulture(Polity.Culture, isolationPreference);

        return culture;
    }

    protected override void MergeIntoGroup(CellGroup targetGroup)
    {
        float prominenceDelta = Population / (float)targetGroup.Population;

        float percentageOfPopulation = Population / (float)(targetGroup.Population + Population);

        if (!percentageOfPopulation.IsInsideRange(0, 1))
        {
            throw new System.Exception(
                "Percentage increase outside of range (0,1): " + percentageOfPopulation +
                " - Group: " + targetGroup.Id);
        }

        targetGroup.ChangePopulation(Population);

        targetGroup.Culture.MergeCulture(Culture, percentageOfPopulation);

        targetGroup.AddPolityProminenceValueDelta(Polity, prominenceDelta);

        targetGroup.TriggerInterference();
    }

    protected override CellGroup CreateGroupOnTarget()
    {
        return new CellGroup(this);
    }
}
