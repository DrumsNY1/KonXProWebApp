namespace KonXProWebApp.Data;

/// <summary>
/// The five permit-intel dashboard views. Defined here once so Program.cs's startup DDL and the
/// entitlement tests in KonXProWebApp.Integration.Tests run the exact same SQL — these views are
/// the enforcement point for what each subscription tier can see (see REMEDIATION.md), so a test
/// asserting against a hand-copied version of this SQL would prove nothing about what actually
/// gets deployed. They are excluded from EF migrations (mapped via ToView in
/// db_9f8bee_konxdevContext) and re-created idempotently by CREATE OR ALTER VIEW here instead.
/// </summary>
public static class TierViewDefinitions
{
    public static IReadOnlyList<(string Name, string CreateOrAlterSql)> Views { get; } =
    [
        ("vwFreeTierDashboard", """
            CREATE OR ALTER VIEW dbo.vwFreeTierDashboard AS
              SELECT JobNum, Borough, ISNULL(HouseNum, '') + ' ' + ISNULL(StreetName, '') AS Street, LatestActionDate, JobType AS ProjectType, JobDescription, Gisntaname AS Neighborhood
              FROM dbo.DOBJobFilings;
            """),

        ("vwBasicTierDashboard", """
            CREATE OR ALTER VIEW dbo.vwBasicTierDashboard AS
              SELECT JobNum, Borough, HouseNum, StreetName AS Street, LatestActionDate, JobType AS ProjectType, JobDescription, Gisntaname AS Neighborhood
              FROM dbo.DOBJobFilings;
            """),

        ("vwMidTierDashboard", """
            CREATE OR ALTER VIEW dbo.vwMidTierDashboard AS
              SELECT JobNum, Borough, HouseNum, StreetName AS Street, LatestActionDate, JobType AS ProjectType, InitialCost AS EstimatedCost, JobDescription, Gisntaname AS Neighborhood
              FROM dbo.DOBJobFilings;
            """),

        ("vwHighTierDashboard", """
            CREATE OR ALTER VIEW dbo.vwHighTierDashboard AS
              SELECT JobNum, Borough, HouseNum, StreetName AS Street, LatestActionDate, JobType AS ProjectType, InitialCost AS EstimatedCost, JobDescription, Gisntaname AS Neighborhood
              FROM dbo.DOBJobFilings;
            """),

        ("vwDemoDisplay", """
            CREATE OR ALTER VIEW dbo.vwDemoDisplay AS
              SELECT 'Sample Content' AS Content, 'Sample Summary' AS Summary, CAST(GETDATE() AS datetime2) AS CompletionDate;
            """),
    ];
}
