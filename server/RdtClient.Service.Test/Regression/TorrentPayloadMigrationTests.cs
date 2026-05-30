using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using RdtClient.Data.Data;
using RdtClient.Data.Enums;
using RdtClient.Data.Models.Data;

namespace RdtClient.Service.Test.Regression;

public class TorrentPayloadMigrationTests : IAsyncLifetime
{
    private readonly String _databasePath = Path.Combine(Path.GetTempPath(), $"rdt-client-payload-migration-{Guid.NewGuid():N}.sqlite");

    [Fact]
    public async Task Migrate_ShouldBackfillPayloadTable_AndDropLegacyColumn()
    {
        await using var migrationContext = CreateContext();
        var migrations = migrationContext.Database.GetMigrations().ToList();

        Assert.True(migrations.Count >= 2);

        var previousMigration = migrations[^2];
        var migrator = migrationContext.GetService<IMigrator>();

        await migrator.MigrateAsync(previousMigration);

        var torrentId = Guid.NewGuid();
        const String payload = "magnet:?xt=urn:btih:legacy-hash";

        await migrationContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO Torrents (
                TorrentId,
                Hash,
                DownloadAction,
                FinishedAction,
                FinishedActionDelay,
                HostDownloadAction,
                DownloadMinSize,
                DownloadClient,
                Added,
                Type,
                FileOrMagnet,
                IsFile,
                RetryCount,
                DownloadRetryAttempts,
                TorrentRetryAttempts,
                DeleteOnError,
                Lifetime,
                RdName
            )
            VALUES (
                {0},
                {1},
                {2},
                {3},
                {4},
                {5},
                {6},
                {7},
                {8},
                {9},
                {10},
                {11},
                {12},
                {13},
                {14},
                {15},
                {16},
                {17}
            );
            """,
            torrentId,
            "legacy-hash",
            (Int32)TorrentDownloadAction.DownloadAll,
            (Int32)TorrentFinishedAction.None,
            0,
            (Int32)TorrentHostDownloadAction.DownloadAll,
            0,
            (Int32)DownloadClient.Bezzad,
            DateTimeOffset.UtcNow,
            (Int32)DownloadType.Torrent,
            payload,
            false,
            0,
            0,
            0,
            0,
            0,
            "Legacy Torrent");

        var migrationsAssembly = migrationContext.GetService<IMigrationsAssembly>();
        var modelDiffer = migrationContext.GetService<IMigrationsModelDiffer>();
        var designTimeModel = migrationContext.GetService<IDesignTimeModel>();
        var modelRuntimeInitializer = migrationContext.GetService<IModelRuntimeInitializer>();
        var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;

        Assert.NotNull(snapshotModel);

        var initializedSnapshotModel = modelRuntimeInitializer.Initialize(snapshotModel!);

        var pendingOperations = modelDiffer.GetDifferences(initializedSnapshotModel.GetRelationalModel(), designTimeModel.Model.GetRelationalModel())
                                           .Select(m => m switch
                                           {
                                               DropForeignKeyOperation drop => $"{drop.GetType().Name}:{drop.Table}.{drop.Name}",
                                               AddForeignKeyOperation add => $"{add.GetType().Name}:{add.Table}.{add.Name}:delete={add.OnDelete}",
                                               _ => m.GetType().Name
                                           })
                                           .ToList();

        Assert.True(pendingOperations.Count == 0, $"Pending operations: {String.Join(", ", pendingOperations)}");

        await migrator.MigrateAsync();

        await using var verificationContext = CreateContext();
        var torrentData = new TorrentData(verificationContext);
        var torrent = await torrentData.GetById(torrentId);

        Assert.NotNull(torrent);
        Assert.NotNull(torrent!.Payload);
        Assert.Equal(payload, torrent.Payload!.Content);

        await using var command = verificationContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA table_info('Torrents');";

        if (command.Connection!.State != System.Data.ConnectionState.Open)
        {
            await command.Connection.OpenAsync();
        }

        var columns = new List<String>();

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }

        Assert.DoesNotContain("FileOrMagnet", columns);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        return Task.CompletedTask;
    }

    private DataContext CreateContext()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            ForeignKeys = true
        }.ToString();

        var options = new DbContextOptionsBuilder<DataContext>()
                      .UseSqlite(connectionString)
                      .Options;

        return new(options);
    }
}
