param(
    [Parameter(Mandatory=$true)]
    [string]$trxFolderPath,
    [Parameter(Mandatory=$false)]
    [string]$txtOutputFolderPath
)

Import-Module ImportExcel

if (!(Test-Path $trxFolderPath))
{
    Write-Error "Folder not found at $trxFolderPath";
    exit
}

#if $txtOutputFolderPath is not set, use current directory
if (-not $txtOutputFolderPath)
{
    $txtOutputFolderPath = $PSScriptRoot
}

$outcomeLocal = "Failed"  # Only process failed tests

# create a dicionary to cache workload owners
$workloadOwnersCache = @{}

#Create 4 lists each mapping to category "methodNotFound", "pathNotFound","invalidStart", "other"
$methodNotFound = @()
$pathNotFound = @()
$invalidStart = @()
$other = @()

$files = Get-ChildItem -Path $trxFolderPath -Filter *.trx
$SpecificErrorPattern = "/home/vsts/work/1/a/Snippets/"
foreach ($trxFilePath in $files){
    Write-Host "Processing file $trxFilePath"
    [xml]$xmlContent = Get-Content $trxFilePath

    $sourcefile = $trxFilePath.Name.Split("TestResults")[0].Trim()

    $resultsContent = $xmlContent.TestRun.Results.UnitTestResult |
      Where-Object { $_.outcome -eq $outcomeLocal } |
      Select-Object -property startTime, testName, @{label="methodAndEndpoint"; Expression={$_.InnerText.Split("Host:")[0].Split("Original HTTP Snippet:")[1].Trim().Split(" ")}}, @{label='specificError'; expression={$_.InnerText.Split($SpecificErrorPattern)[1].Split(" at ")[0].trim()}} |
      Select-Object -Property startTime, testName, @{label="sourceFile"; Expression={$sourceFile}}, @{label="method"; expression={$_.methodAndEndpoint[0]}}, @{label="endpoint"; Expression={$_.methodAndEndpoint[1]}}, specificError |
      Sort-Object

    # Get Script getWorkloadOwner.ps1
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
    $getWorkloadOwnersScript = Join-Path $scriptDir "getWorkloadOwner.ps1"
    . $getWorkloadOwnersScript
    foreach ($result in $resultsContent)
    {
        # find endpoint using getWorkloadOwner.ps1
        $version = $result.endpoint.Contains("v1.0") ? "v1.0" : "beta"
        $endpoint = $result.endpoint.Split($version+"/")[1].Trim()
        Write-Host "Fetching Workload owner for endpoint ${endpoint}"
        # remove params from endpoint and save as var cacheKey
        $cacheKey = $endpoint.Split("?")[0].Trim()
        if ($workloadOwnersCache.ContainsKey($cacheKey))
        {
            $owner = $workloadOwnersCache[$cacheKey]
            write-host "Using cached value $owner for $cacheKey"
        }
        else
        {
            $owner = Get-WorkloadOwner -Endpoint $endpoint -IsFullUri $false -GraphApiVersion $version
            $workloadOwnersCache[$cacheKey] = $owner
        }
        # add owner to result
        $result | Add-Member -MemberType NoteProperty -Name "owner" -Value $owner

        # assign result to appropriate category
        $specificError = $result.specificError
        if ($specificError -match "HTTP Method .*")
        {
            $methodNotFound += $result
        }
        elseif ($specificError -match "Path segment .*")
        {
            $pathNotFound += $result
        }
        elseif ($specificError -match ".* is an invalid start")
        {
            $invalidStart += $result
        }
        else
        {
            $other += $result
        }
    }

}

$excelOutputPath = Join-Path $txtOutputFolderPath "reportDevX.xlsx"
$methodNotFound | Export-Excel -Path $excelOutputPath -WorksheetName "MethodNotFound" -AutoSize
$pathNotFound | Export-Excel -Path $excelOutputPath -WorksheetName "PathNotFound" -AutoSize
$invalidStart | Export-Excel -Path $excelOutputPath -WorksheetName "InvalidStart" -AutoSize
$other | Export-Excel -Path $excelOutputPath -WorksheetName "Other" -AutoSize

Write-Host "Categorization complete. Output saved to $excelOutputPath"
