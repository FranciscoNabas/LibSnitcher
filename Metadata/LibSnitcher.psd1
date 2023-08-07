@{
    GUID = 'A3DD0591-F976-4D78-ADC4-62AEF94668D2'
    ModuleVersion = '1.1.0'
    RootModule = 'LibSnitcher.dll'
    PowerShellVersion = '5.1'
    CompatiblePSEditions = @(
        'Desktop',
        'Core'
    )
    Author = 'Francisco Nabas'
    Copyright = '(c) Francisco Nabas. All rights reserved.'
    Description = 'This module contains tools to manage module dependencies.'
    RequiredAssemblies = @(
        'LibSnitcher.dll',
        'LibSCore.dll'
    )
    TypesToProcess = @('LibSnitcher.Types.ps1xml')
    FormatsToProcess = @()
    FunctionsToExport = @()
    CmdletsToExport = @(
        'Get-PeDependencyChain',
        'Get-PeFailedDependency',
        'Get-PeHeaders'
    )
    AliasesToExport = @(
        'getfaildep',
        'getdepchain'
    )
    PrivateData = @{
        PSData = @{
            LicenseUri = 'https://github.com/FranciscoNabas/LibSnitcher/blob/main/LICENSE'
            ProjectUri = 'https://github.com/FranciscoNabas/LibSnitcher'
            ReleaseNotes = 'https://github.com/FranciscoNabas/LibSnitcher/blob/main/CHANGELOG.md'
        }
    }
}