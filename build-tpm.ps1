$base    = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)
. (Join-Path $base "..\Shared\BuildTpm.Common.ps1")

Build-AbrTpm -Base $base -TpmName "PaveDetail" `
    -DllPath "bin\Debug\net48\Abr.PaveDetail.dll" `
    -PluginFiles @("PaveDetail.plugin", "t_pave_detail_tab.plugin") `
    -NeedsSetupExe
