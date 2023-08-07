param (
    [switch]$Local
)

$release_dir = '.\Release\LibSnitcher'
$copy_path = @(
    '.\LICENSE'
    '.\Metadata\LibSnitcher.psd1'
    '.\Metadata\LibSnitcher.Types.ps1xml'
)

Write-Host 'Building...' -ForegroundColor DarkGreen
if ($Local) {
    [void](. dotnet.exe build .\LibSnitcher.csproj --arch x64 --configuration Release --output $release_dir --no-incremental)
}
else {
    . dotnet.exe build .\LibSnitcher.csproj --arch x64 --configuration Release --output $release_dir --no-incremental
}

Write-Host 'Cleaning files...' -ForegroundColor DarkGreen
foreach ($file in @(
    '*.config', '*.pdb', '*.json', '*.exp', '*.lib', '*.dll.metagen', 'LibSnitcher.xml'
)) {
    Remove-Item -Path "$release_dir\*" -Filter $file -Force
}

Write-Host 'Copying support files...' -ForegroundColor DarkGreen
Copy-Item -Path $copy_path -Destination $release_dir -Force

if (!(Test-Path -Path "$release_dir\en-us")) { [void](mkdir "$release_dir\en-us") }
Move-Item -Path "$release_dir\LibSnitcher.dll-help.xml" -Destination "$release_dir\en-us" -Force