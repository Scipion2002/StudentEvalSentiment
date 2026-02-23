using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentEvalSentiment.Migrations
{
    /// <inheritdoc />
    public partial class TopicClusterChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Create replacement table (NO IDENTITY) with the desired composite PK
            migrationBuilder.Sql(@"
        IF OBJECT_ID('dbo.TopicClusters__New', 'U') IS NOT NULL
            DROP TABLE dbo.TopicClusters__New;

        CREATE TABLE dbo.TopicClusters__New
        (
            TopicClusterId INT NOT NULL,
            TargetType NVARCHAR(50) NOT NULL,
            HumanLabel NVARCHAR(200) NOT NULL,
            Notes NVARCHAR(2000) NULL,
            CreatedUtc DATETIME2(7) NOT NULL,

            CONSTRAINT PK_TopicClusters__New PRIMARY KEY (TargetType, TopicClusterId)
        );

        INSERT INTO dbo.TopicClusters__New (TopicClusterId, TargetType, HumanLabel, Notes, CreatedUtc)
        SELECT TopicClusterId, TargetType, HumanLabel, Notes, CreatedUtc
        FROM dbo.TopicClusters;

        DROP TABLE dbo.TopicClusters;

        EXEC sp_rename 'dbo.TopicClusters__New', 'TopicClusters';
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate TopicClusters with IDENTITY TopicClusterId and single-column PK
            // (This is mainly for rollback; if you never rollback migrations, it's still good to have.)
            migrationBuilder.Sql(@"
        IF OBJECT_ID('dbo.TopicClusters__Old', 'U') IS NOT NULL
            DROP TABLE dbo.TopicClusters__Old;

        CREATE TABLE dbo.TopicClusters__Old
        (
            TopicClusterId INT IDENTITY(1,1) NOT NULL,
            TargetType NVARCHAR(50) NOT NULL,
            HumanLabel NVARCHAR(200) NOT NULL,
            Notes NVARCHAR(2000) NULL,
            CreatedUtc DATETIME2(7) NOT NULL,

            CONSTRAINT PK_TopicClusters__Old PRIMARY KEY (TopicClusterId)
        );

        -- Preserve old IDs (requires IDENTITY_INSERT)
        SET IDENTITY_INSERT dbo.TopicClusters__Old ON;

        INSERT INTO dbo.TopicClusters__Old (TopicClusterId, TargetType, HumanLabel, Notes, CreatedUtc)
        SELECT TopicClusterId, TargetType, HumanLabel, Notes, CreatedUtc
        FROM dbo.TopicClusters;

        SET IDENTITY_INSERT dbo.TopicClusters__Old OFF;

        DROP TABLE dbo.TopicClusters;

        EXEC sp_rename 'dbo.TopicClusters__Old', 'TopicClusters';
    ");
        }
    }
}
