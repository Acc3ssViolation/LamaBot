param (
    [Parameter(Mandatory=$True)]
    [string]$MigrationName
)

& dotnet ef migrations add $MigrationName --context ApplicationDbContext
