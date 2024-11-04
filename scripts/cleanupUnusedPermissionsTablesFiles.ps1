$permissionsTablesFiles = Get-ChildItem -Recurse -Filter permissions | Get-ChildItem -Recurse -Filter *.md | Select-Object -ExpandProperty FullName

# Get all markdown files excluding those in the permissions directory
$docsFiles = Get-ChildItem -Recurse -Filter *.md -Exclude */permissions/*

$resolvedPermissionsTablesFiles = New-Object System.Collections.Generic.HashSet[string]

foreach ($docFile in $docsFiles) {
    $content = Get-Content -Path $docFile.FullName -ErrorAction Stop
    if ($content -cmatch "\[!INCLUDE \[permissions-table\]") {
        # Matches this in the referencing files [!INCLUDE [permissions-table](../includes/permissions/user-list-calendars-permissions.md)]
        $pathMatches = [regex]::Matches($content, "\(([^\)]+)\)"); # Gets the path portion of a MD link
        foreach ($pathMatch in $pathMatches) {
            $permissionsTablePath = $pathMatch.Groups[1].Value;
            if ($permissionsTablePath -match ".*permissions.*" -and 
                $permissionsTablePath -cnotmatch ".*:\/\/.*" -and 
                $permissionsTablePath.EndsWith('.md', 'OrdinalIgnoreCase')) { # Checks that the permissions table is in a "permissions" folder and that it's not a link "://"
                
                # Resolve path and add to HashSet
                $permissionsTableFullPath = (Resolve-Path -LiteralPath (Join-Path $docFile.DirectoryName $permissionsTablePath) -ErrorAction SilentlyContinue).Path
                if ($permissionsTableFullPath) {
                    $resolvedPermissionsTablesFiles.Add($permissionsTableFullPath) | Out-Null
                }
            }
        }
    }
}

# Remove unused permissions table markdown files
foreach ($candidateForDeletion in $permissionsTablesFiles) {
    if (-not $resolvedPermissionsTablesFiles.Contains($candidateForDeletion)) {
        Remove-Item -Path $candidateForDeletion -Verbose -ErrorAction SilentlyContinue
    }
}