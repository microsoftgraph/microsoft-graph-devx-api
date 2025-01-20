using namespace System.Diagnostics.CodeAnalysis
[SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '', Scope='Function', Target='Get-WorkloadOwner')]
[SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '', Scope='Function', Target='Connect-Tenant')]
[SuppressMessageAttribute('PSUseSingularNouns', '', Scope='Function', Target='Get-AppSettings')]
param()

$Global:AppSettings = $null


<#
    Connect to the Raptor Tenant
#>
Function Connect-Tenant
{
    [CmdletBinding()]
    param(
        [Bool]$IsEducation = $false
    )
    $appToken = Get-DefaultAppApplicationToken -IsEducation $IsEducation
    $appToken = $appToken | ConvertTo-SecureString -AsPlainText -Force
    Connect-MgGraph -AccessToken $appToken | Out-Null
}

function Get-AppSettings ()
{
    if($null -ne $Global:AppSettings){
        return $AppSettings
    }
    # read app settings from Azure App Config
    $baseSettingsDirectory = $env:WorkingDirectory
    if (!$baseSettingsDirectory)
    {
        $baseSettingsDirectory = $env:TEMP
    }
    New-Item -Path $baseSettingsDirectory -Name "appSettings.json" -Force
    $appSettingsPath = Join-Path $baseSettingsDirectory "appSettings.json" -Resolve
    write-host "appsettings path: $appSettingsPath"
    # Support Reading Settings from a Custom Label, otherwise default to Development
    $settingsLabel = $env:RAPTOR_CONFIGLABEL
    if ([string]::IsNullOrWhiteSpace($settingsLabel))
    {
        $settingsLabel = "Development"
    }
    try {
        az login --identity -u $env:RAPTOR_CONFIGMANAGEDIDENTITY_ID #Pipeline login
        # Disable below to test locally
        # az login
        Write-Host "Login successful. Fetching AppSettings from Azure App Config."
    }
    catch {
        Write-Host "Failed to login using Managed Identity."
    }
    try {
        az appconfig kv export --endpoint $env:RAPTOR_CONFIGENDPOINT --auth-mode login --label $settingsLabel --destination file --path $appSettingsPath --format json --yes
        $appSettings = Get-Content $AppSettingsPath -Raw | ConvertFrom-Json
        Remove-Item $appSettingsPath
    }
    catch {
        Write-Host "Failed to fetch AppSettings from Azure App Config."
    }

    if (    !$appSettings.CertificateThumbprint `
            -or !$appSettings.ClientID `
            -or !$appSettings.Username `
            -or !$appSettings.Password `
            -or !$appSettings.TenantID)
    {
        Write-Error -ErrorAction Stop -Message "please provide CertificateThumbprint, ClientID, Username, Password and TenantID in appsettings.json"
    }
    $Global:AppSettings = $appSettings
    return $appSettings
}

function Get-CurrentDomain (
    [Parameter(Mandatory=$true)][String]$DomainUsername
)
{
    $domain = $DomainUsername.Split("@")[1]
    return $domain
}

function Get-DefaultAppApplicationToken
{
    param(
        $IsEducation = $false
    )
    $AppSettings = Get-AppSettings
    $grantType = "client_credentials"
    $tenantUsername = $IsEducation ? $AppSettings.EducationUsername : $AppSettings.Username
    $domain = Get-CurrentDomain -DomainUsername $tenantUsername
    $tokenEndpoint = "https://login.microsoftonline.com/$domain/oauth2/v2.0/token"
    $ScopeString = "https://graph.microsoft.com/.default" # Always use this for app permissions
    $clientID, $clientSecret = $IsEducation ? ($AppSettings.EducationClientID, $AppSettings.EducationClientSecret) : ($AppSettings.ClientID, $AppSettings.ClientSecret)
    $body = "grant_type=$grantType&client_id=$clientID&client_secret=$ClientSecret&scope=$($ScopeString)"
    try
    {
        $response = Invoke-RestMethod -Method Post -Uri $tokenEndpoint -Body $body -ContentType 'application/x-www-form-urlencoded'

        Write-Debug "Received Token with .default Scopes"
        return $response.access_token
    } catch
    {
        Write-Error $_
        throw
    }
}

# A function that takes an endpoint and returns its owner
function Get-WorkloadOwner {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)][string] $Endpoint,
        [bool] $IsFullUri=$true,
        [string] $GraphApiVersion="v1.0"
    )
    Connect-Tenant
    #ToDo delegated perms once issue with MFA is resolved
    # if ($Endpoint -contains "me/")
    # {
    #     $userToken = Get-DelegatedAppToken | ConvertTo-SecureString -AsPlainText -Force
    #     Connect-MgGraph -AccessToken $userToken.access_token | Out-Null
    # }
    $ownerEndpoint = $Endpoint.contains("?") ? ($Endpoint + "&`$whatif"):($Endpoint + "?`$whatif")
    $fullUrl = $IsFullUri ? $Uri : "https://graph.microsoft.com/$($GraphApiVersion)/$ownerEndpoint"
    try {
        $responseObject = Invoke-MgGraphRequest -Uri $fullUrl -OutputType PSObject
        return $responseObject.TargetWorkloadId
    }
    catch {
        <#Do this if a terminating exception happens#>
        Write-Warning $_
        return $null
    }
}
