using namespace System.Diagnostics.CodeAnalysis
[SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '', Scope='Function', Target='Get-WorkloadOwner')]
[SuppressMessageAttribute('PSUseSingularNouns', '', Scope='Function', Target='Get-AppSettings')]
param()


function Get-AppSettings ()
{
    # read app settings from Azure App Config
    $appSettingsPath = "$env:TEMP/appSettings.json"
    # Support Reading Settings from a Custom Label, otherwise default to Development
    $settingsLabel = $env:RAPTOR_CONFIGLABEL
    if ([string]::IsNullOrWhiteSpace($settingsLabel))
    {
        $settingsLabel = "Development"
    }
    az appconfig kv export --connection-string $env:RAPTOR_CONFIGCONNECTIONSTRING --label $settingsLabel --destination file --path $appSettingsPath --format json --yes
    $appSettings = Get-Content $AppSettingsPath -Raw | ConvertFrom-Json
    Remove-Item $appSettingsPath

    if (    !$appSettings.CertificateThumbprint `
            -or !$appSettings.ClientID `
            -or !$appSettings.Username `
            -or !$appSettings.Password `
            -or !$appSettings.TenantID)
    {
        Write-Error -ErrorAction Stop -Message "please provide CertificateThumbprint, ClientID, Username, Password and TenantID in appsettings.json"
    }
    return $appSettings
}

function Get-CurrentDomain (
    [PSObject]$AppSettings,
    [Bool]$IsEducation=$false
)
{
    $username = $IsEducation ? $AppSettings.EducationUsername : $AppSettings.Username
    $domain = $username.Split("@")[1]
    return $domain
}

function Get-DefaultAppApplicationToken
{
    param(
        $AppSettings,
        $IsEducation = $false
        )
    $grantType = "client_credentials"
    $domain = Get-CurrentDomain -AppSettings $AppSettings -IsEducation $IsEducation
    $tokenEndpoint = "https://login.microsoftonline.com/$domain/oauth2/v2.0/token"
    $ScopeString = "https://graph.microsoft.com/.default" # Always use this for app permissions
    $clientID, $clientSecret = $IsEducation ? ($AppSettings.EducationClientID, $AppSettings.EducationClientSecret) : ($AppSettings.ClientID, $AppSettings.ClientSecret)
    $body = "grant_type=$grantType&client_id=$clientID&client_secret=$ClientSecret&scope=$($ScopeString)"
    try
    {
        $response = Invoke-WebRequest -Method Post -Uri $tokenEndpoint `
         -Body $body -ContentType 'application/x-www-form-urlencoded' `
         -SessionVariable "session" | ConvertFrom-Json

        Write-Debug "Received Token with .default Scopes"
        return $response.access_token, $session
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
        [Parameter(Mandatory=$true)]
        [string] $Endpoint,
        [bool] $IsFullUri=$true,
        [string] $GraphApiVersion="v1.0"
    )

    $appSettings = Get-AppSettings

    $appToken, $defaultAppSession = Get-DefaultAppApplicationToken -AppSettings $AppSettings
    $defaultAppSession.Headers.Add("Authorization", "Bearer $appToken")
    $ownerEndpoint = $Endpoint.contains("?") ? ($Endpoint + "&`$whatif"):($Endpoint + "?`$whatif")
    $fullUrl = $IsFullUri ? $ownerEndpoint : "https://graph.microsoft.com/$GraphApiVersion/$ownerEndpoint"

    $responseObject = Invoke-WebRequest -Method "GET" -Uri $fullUrl -WebSession $defaultAppSession

    return $responseObject.TargetWorkloadId
}
