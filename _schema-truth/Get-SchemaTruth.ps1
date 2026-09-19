<#
.SYNOPSIS
    Exports the complete schema of a SQL Server database to a structured JSON file.

.DESCRIPTION
    Connects to a SQL Server instance and extracts tables, columns, primary keys,
    foreign keys, indexes, check constraints, default constraints, views, stored
    procedures, scalar/table functions, and user-defined types. Outputs a single
    JSON file that serves as the "schema truth" snapshot for a given environment.

.PARAMETER Database
    The name of the SQL Server database to inspect.

.PARAMETER User
    The SQL Server login username. SQL Authentication is used.

.PARAMETER Label
    A friendly label for the environment (e.g. prod, staging, dev).

.PARAMETER Server
    The SQL Server host. Defaults to localhost.

.PARAMETER Password
    The password for SQL Authentication. If omitted, you will be prompted.

.PARAMETER OutputDir
    Directory to write the JSON output file. Defaults to the script's own directory.

.EXAMPLE
    .\Get-SchemaTruth.ps1 -Database priority_konx -User konx_admin -Label prod
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Database,

    [Parameter(Mandatory)]
    [string]$User,

    [Parameter(Mandatory)]
    [string]$Label,

    [string]$Server = "localhost",

    [string]$Password,

    [string]$OutputDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Resolve output directory ──────────────────────────────────────────────────
if (-not $OutputDir) {
    $OutputDir = $PSScriptRoot
}
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# ── Prompt for password if not supplied ───────────────────────────────────────
if (-not $Password) {
    $securePass = Read-Host "Password for $User@$Server/$Database" -AsSecureString
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePass)
    )
}

# ── Connect ───────────────────────────────────────────────────────────────────
$connectionString = "Server=$Server;Database=$Database;User Id=$User;Password=$Password;TrustServerCertificate=True;Connection Timeout=30;"

Write-Host "[$Label] Connecting to $Server/$Database as $User ..." -ForegroundColor Cyan

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
try {
    $connection.Open()
    Write-Host "[$Label] Connected." -ForegroundColor Green
}
catch {
    Write-Error "Failed to connect: $_"
    return
}

# ── Helper: run a query and return rows as PSObjects ──────────────────────────
function Invoke-SchemaQuery {
    param([string]$Sql)
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = 120
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $table = New-Object System.Data.DataTable
    [void]$adapter.Fill($table)
    $cmd.Dispose()
    return $table
}

# ── 1. Schemas ────────────────────────────────────────────────────────────────
Write-Host "[$Label] Extracting schemas ..."
$schemasData = Invoke-SchemaQuery @"
SELECT
    s.name          AS [SchemaName],
    p.name          AS [Owner]
FROM sys.schemas s
JOIN sys.database_principals p ON s.principal_id = p.principal_id
WHERE s.name NOT IN ('sys','INFORMATION_SCHEMA','guest')
ORDER BY s.name;
"@

# ── 2. Tables & Columns ──────────────────────────────────────────────────────
Write-Host "[$Label] Extracting tables & columns ..."
$columnsData = Invoke-SchemaQuery @"
SELECT
    SCHEMA_NAME(t.schema_id)    AS [Schema],
    t.name                      AS [Table],
    c.name                      AS [Column],
    c.column_id                 AS [OrdinalPosition],
    tp.name                     AS [DataType],
    c.max_length                AS [MaxLength],
    c.precision                 AS [Precision],
    c.scale                     AS [Scale],
    c.is_nullable               AS [IsNullable],
    c.is_identity               AS [IsIdentity],
    c.is_computed               AS [IsComputed],
    cc.definition               AS [ComputedDefinition],
    dc.definition               AS [DefaultValue]
FROM sys.tables t
JOIN sys.columns c       ON c.object_id = t.object_id
JOIN sys.types   tp      ON tp.user_type_id = c.user_type_id
LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
WHERE t.is_ms_shipped = 0
ORDER BY SCHEMA_NAME(t.schema_id), t.name, c.column_id;
"@

# Group columns into tables
$tables = @{}
foreach ($row in $columnsData.Rows) {
    $key = "$($row.Schema).$($row.Table)"
    if (-not $tables.ContainsKey($key)) {
        $tables[$key] = [ordered]@{
            schema  = $row.Schema
            name    = $row.Table
            columns = [System.Collections.ArrayList]::new()
        }
    }
    $col = [ordered]@{
        name               = $row.Column
        ordinalPosition    = [int]$row.OrdinalPosition
        dataType           = $row.DataType
        maxLength          = if ($row.MaxLength -is [DBNull]) { $null } else { [int]$row.MaxLength }
        precision          = if ($row.Precision -is [DBNull]) { $null } else { [int]$row.Precision }
        scale              = if ($row.Scale -is [DBNull])     { $null } else { [int]$row.Scale }
        isNullable         = [bool]$row.IsNullable
        isIdentity         = [bool]$row.IsIdentity
        isComputed         = [bool]$row.IsComputed
        computedDefinition = if ($row.ComputedDefinition -is [DBNull]) { $null } else { $row.ComputedDefinition }
        defaultValue       = if ($row.DefaultValue -is [DBNull]) { $null } else { $row.DefaultValue }
    }
    [void]$tables[$key].columns.Add($col)
}

# ── 3. Primary Keys ──────────────────────────────────────────────────────────
Write-Host "[$Label] Extracting primary keys ..."
$pkData = Invoke-SchemaQuery @"
SELECT
    SCHEMA_NAME(t.schema_id) AS [Schema],
    t.name                   AS [Table],
    kc.name                  AS [ConstraintName],
    c.name                   AS [Column],
    ic.key_ordinal           AS [KeyOrdinal]
FROM sys.key_constraints kc
JOIN sys.tables t           ON t.object_id = kc.parent_object_id
JOIN sys.index_columns ic   ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
JOIN sys.columns c          ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE kc.type = 'PK'
ORDER BY SCHEMA_NAME(t.schema_id), t.name, ic.key_ordinal;
"@

$primaryKeys = @{}
foreach ($row in $pkData.Rows) {
    $key = $row.ConstraintName
    if (-not $primaryKeys.ContainsKey($key)) {
        $primaryKeys[$key] = [ordered]@{
            constraintName = $row.ConstraintName
            schema         = $row.Schema
            table          = $row.Table
            columns        = [System.Collections.ArrayList]::new()
        }
    }
    [void]$primaryKeys[$key].columns.Add($row.Column)
}

# ── 4. Foreign Keys ──────────────────────────────────────────────────────────
Write-Host "[$Label] Extracting foreign keys ..."
$fkData = Invoke-SchemaQuery @"
SELECT
    fk.name                                    AS [ConstraintName],
    SCHEMA_NAME(tp.schema_id)                  AS [ParentSchema],
    tp.name                                    AS [ParentTable],
    cp.name                                    AS [ParentColumn],
    SCHEMA_NAME(tr.schema_id)                  AS [ReferencedSchema],
    tr.name                                    AS [ReferencedTable],
    cr.name                                    AS [ReferencedColumn],
    fk.delete_referential_action_desc          AS [OnDelete],
    fk.update_referential_action_desc          AS [OnUpdate]
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.tables tp               ON tp.object_id = fk.parent_object_id
JOIN sys.columns cp              ON cp.object_id = fkc.parent_object_id AND cp.column_id = fkc.parent_column_id
JOIN sys.tables tr               ON tr.object_id = fk.referenced_object_id
JOIN sys.columns cr              ON cr.object_id = fkc.referenced_object_id AND cr.column_id = fkc.referenced_column_id
ORDER BY fk.name;
"@

$foreignKeys = @{}
foreach ($row in $fkData.Rows) {
    $key = $row.ConstraintName
    if (-not $foreignKeys.ContainsKey($key)) {
        $foreignKeys[$key] = [ordered]@{
            constraintName   = $row.ConstraintName
            parentSchema     = $row.ParentSchema
            parentTable      = $row.ParentTable
            parentColumns    = [System.Collections.ArrayList]::new()
            referencedSchema = $row.ReferencedSchema
            referencedTable  = $row.ReferencedTable
            referencedColumns = [System.Collections.ArrayList]::new()
            onDelete         = $row.OnDelete
            onUpdate         = $row.OnUpdate
        }
    }
    [void]$foreignKeys[$key].parentColumns.Add($row.ParentColumn)
    [void]$foreignKeys[$key].referencedColumns.Add($row.ReferencedColumn)
}

# ── 5. Indexes ────────────────────────────────────────────────────────────────
Write-Host "[$Label] Extracting indexes ..."
$indexData = Invoke-SchemaQuery @"
SELECT
    SCHEMA_NAME(t.schema_id) AS [Schema],
    t.name                   AS [Table],
    i.name                   AS [IndexName],
    i.type_desc              AS [IndexType],
    i.is_unique              AS [IsUnique],
    i.is_primary_key         AS [IsPrimaryKey],
    c.name                   AS [Column],
    ic.key_ordinal           AS [KeyOrdinal],
    ic.is_included_column    AS [IsIncluded],
    ic.is_descending_key     AS [IsDescending]
FROM sys.indexes i
JOIN sys.tables t          ON t.object_id = i.object_id
JOIN sys.index_columns ic  ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c         ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE t.is_ms_shipped = 0
  AND i.name IS NOT NULL
ORDER BY SCHEMA_NAME(t.schema_id), t.name, i.name, ic.key_ordinal;
"@

$indexes = @{}
foreach ($row in $indexData.Rows) {
    $key = "$($row.Schema).$($row.Table).$($row.IndexName)"
    if (-not $indexes.ContainsKey($key)) {
        $indexes[$key] = [ordered]@{
            indexName    = $row.IndexName
            schema       = $row.Schema
            table        = $row.Table
            indexType    = $row.IndexType
            isUnique     = [bool]$row.IsUnique
            isPrimaryKey = [bool]$row.IsPrimaryKey
            keyColumns   = [System.Collections.ArrayList]::new()
            includedColumns = [System.Collections.ArrayList]::new()
        }
    }
    if ([bool]$row.IsIncluded) {
        [void]$indexes[$key].includedColumns.Add($row.Column)
    }
    else {
        $colEntry = [ordered]@{
            name         = $row.Column
            isDescending = [bool]$row.IsDescending
        }
        [void]$indexes[$key].keyColumns.Add($colEntry)
    }
}

# ── 6. Check Constraints ─────────────────────────────────────────────────────
Write-Host "[$Label] Extracting check constraints ..."
$checkData = Invoke-SchemaQuery @"
SELECT
    SCHEMA_NAME(t.schema_id) AS [Schema],
    t.name                   AS [Table],
    cc.name                  AS [ConstraintName],
    cc.definition            AS [Definition]
FROM sys.check_constraints cc
JOIN sys.tables t ON t.object_id = cc.parent_object_id
ORDER BY SCHEMA_NAME(t.schema_id), t.name, cc.name;
"@

# ── 7. Views ─────────────────────────────────────────────────────────────────
Write-Host "[$Label] Extracting views ..."
$viewsData = Invoke-SchemaQuery @"
SELECT
    SCHEMA_NAME(v.schema_id) AS [Schema],
    v.name                   AS [Name],
    m.definition             AS [Definition]
FROM sys.views v
JOIN sys.sql_modules m ON m.object_id = v.object_id
WHERE v.is_ms_shipped = 0
ORDER BY SCHEMA_NAME(v.schema_id), v.name;
"@

# ── 8. Stored Procedures ─────────────────────────────────────────────────────
Write-Host "[$Label] Extracting stored procedures ..."
$procsData = Invoke-SchemaQuery @"
SELECT
    SCHEMA_NAME(p.schema_id) AS [Schema],
    p.name                   AS [Name],
    m.definition             AS [Definition]
FROM sys.procedures p
JOIN sys.sql_modules m ON m.object_id = p.object_id
WHERE p.is_ms_shipped = 0
ORDER BY SCHEMA_NAME(p.schema_id), p.name;
"@

# ── 9. Functions ──────────────────────────────────────────────────────────────
Write-Host "[$Label] Extracting functions ..."
$funcsData = Invoke-SchemaQuery @"
SELECT
    SCHEMA_NAME(o.schema_id) AS [Schema],
    o.name                   AS [Name],
    o.type_desc              AS [FunctionType],
    m.definition             AS [Definition]
FROM sys.objects o
JOIN sys.sql_modules m ON m.object_id = o.object_id
WHERE o.type IN ('FN','IF','TF')
  AND o.is_ms_shipped = 0
ORDER BY SCHEMA_NAME(o.schema_id), o.name;
"@

# ── 10. User-Defined Types ───────────────────────────────────────────────────
Write-Host "[$Label] Extracting user-defined types ..."
$udtData = Invoke-SchemaQuery @"
SELECT
    SCHEMA_NAME(t.schema_id) AS [Schema],
    t.name                   AS [Name],
    bt.name                  AS [BaseType],
    t.max_length             AS [MaxLength],
    t.precision              AS [Precision],
    t.scale                  AS [Scale],
    t.is_nullable            AS [IsNullable]
FROM sys.types t
JOIN sys.types bt ON bt.user_type_id = t.system_type_id
WHERE t.is_user_defined = 1
ORDER BY SCHEMA_NAME(t.schema_id), t.name;
"@

# ── Close connection ──────────────────────────────────────────────────────────
$connection.Close()
$connection.Dispose()
Write-Host "[$Label] Connection closed." -ForegroundColor Green

# ── Assemble output ──────────────────────────────────────────────────────────
$timestamp = Get-Date -Format "yyyy-MM-ddTHH-mm-ss"

$output = [ordered]@{
    metadata = [ordered]@{
        database   = $Database
        server     = $Server
        label      = $Label
        user       = $User
        exportedAt = (Get-Date -Format "o")
        exportedBy = "$env:USERNAME@$env:COMPUTERNAME"
    }

    schemas = @(foreach ($r in $schemasData.Rows) {
        [ordered]@{ name = $r.SchemaName; owner = $r.Owner }
    })

    tables = @($tables.Values)

    primaryKeys = @($primaryKeys.Values)

    foreignKeys = @($foreignKeys.Values)

    indexes = @($indexes.Values)

    checkConstraints = @(foreach ($r in $checkData.Rows) {
        [ordered]@{
            schema         = $r.Schema
            table          = $r.Table
            constraintName = $r.ConstraintName
            definition     = $r.Definition
        }
    })

    views = @(foreach ($r in $viewsData.Rows) {
        [ordered]@{
            schema     = $r.Schema
            name       = $r.Name
            definition = $r.Definition
        }
    })

    storedProcedures = @(foreach ($r in $procsData.Rows) {
        [ordered]@{
            schema     = $r.Schema
            name       = $r.Name
            definition = $r.Definition
        }
    })

    functions = @(foreach ($r in $funcsData.Rows) {
        [ordered]@{
            schema       = $r.Schema
            name         = $r.Name
            functionType = $r.FunctionType
            definition   = $r.Definition
        }
    })

    userDefinedTypes = @(foreach ($r in $udtData.Rows) {
        [ordered]@{
            schema     = $r.Schema
            name       = $r.Name
            baseType   = $r.BaseType
            maxLength  = if ($r.MaxLength -is [DBNull]) { $null } else { [int]$r.MaxLength }
            precision  = if ($r.Precision -is [DBNull]) { $null } else { [int]$r.Precision }
            scale      = if ($r.Scale -is [DBNull])     { $null } else { [int]$r.Scale }
            isNullable = [bool]$r.IsNullable
        }
    })
}

# ── Write JSON ────────────────────────────────────────────────────────────────
$fileName = "${Label}_${Database}_${timestamp}.json"
$filePath = Join-Path $OutputDir $fileName

$output | ConvertTo-Json -Depth 10 | Set-Content -Path $filePath -Encoding UTF8

# ── Summary ───────────────────────────────────────────────────────────────────
$tableCount = $tables.Count
$colCount   = ($tables.Values | ForEach-Object { $_.columns.Count } | Measure-Object -Sum).Sum
$pkCount    = $primaryKeys.Count
$fkCount    = $foreignKeys.Count
$idxCount   = $indexes.Count
$viewCount  = $viewsData.Rows.Count
$procCount  = $procsData.Rows.Count
$funcCount  = $funcsData.Rows.Count
$udtCount   = $udtData.Rows.Count

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor DarkCyan
Write-Host "║  Schema Truth — $Label" -ForegroundColor DarkCyan
Write-Host "║  Database: $Database @ $Server" -ForegroundColor DarkCyan
Write-Host "╠══════════════════════════════════════════════════════════╣" -ForegroundColor DarkCyan
Write-Host "║  Tables:              $tableCount" -ForegroundColor White
Write-Host "║  Columns:             $colCount" -ForegroundColor White
Write-Host "║  Primary Keys:        $pkCount" -ForegroundColor White
Write-Host "║  Foreign Keys:        $fkCount" -ForegroundColor White
Write-Host "║  Indexes:             $idxCount" -ForegroundColor White
Write-Host "║  Check Constraints:   $($checkData.Rows.Count)" -ForegroundColor White
Write-Host "║  Views:               $viewCount" -ForegroundColor White
Write-Host "║  Stored Procedures:   $procCount" -ForegroundColor White
Write-Host "║  Functions:           $funcCount" -ForegroundColor White
Write-Host "║  User-Defined Types:  $udtCount" -ForegroundColor White
Write-Host "╠══════════════════════════════════════════════════════════╣" -ForegroundColor DarkCyan
Write-Host "║  Output: $fileName" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor DarkCyan
Write-Host ""
