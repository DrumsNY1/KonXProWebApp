Verify local EF Core model consistency against LocalDB without executing unapproved migrations.
Command:
dotnet ef migrations has-pending-model-changes --context db_9f8bee_konxdevContext --project .\KonXProWebApp.csproj
dotnet ef migrations list --context db_9f8bee_konxdevContext --project .\KonXProWebApp.csproj
