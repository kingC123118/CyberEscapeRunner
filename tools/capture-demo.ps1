# =====================================================================
# capture-demo.ps1 - Capture demo frames from the built CyberEscapeRunner
# Usage:
#   menu : launch, capture one menu frame (to locate the Start button), exit
#   play : launch, click Start at (-ClickX/-ClickY), play with scripted keys
# NOTE: keep this file ASCII-only (Windows PowerShell 5.1 reads no-BOM
#       files as ANSI/GBK, non-ASCII comments break the parser).
# =====================================================================
param(
    [string]$Root = "e:\CyberEscape\CyberEscapeRunner\unity-3d-endless-runner-game-main",
    [string]$ExeRel = "Builds\CyberEscapeRunner\CyberEscapeRunner.exe",
    [string]$Phase = "menu",
    [int]$ClickX = -1,
    [int]$ClickY = -1,
    [int]$Frames = 120,
    [int]$FrameGapMs = 65
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class W {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out R r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint vk, uint type);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr wp, IntPtr lp);
    public struct R { public int L, T, Rt, B; }
}
"@

[W]::SetProcessDPIAware() | Out-Null
Get-Process -Name "CyberEscapeRunner" -ErrorAction SilentlyContinue | Stop-Process -Force

$out = Join-Path $Root "media\capture"
New-Item -ItemType Directory -Force -Path $out | Out-Null

$exe = Join-Path $Root $ExeRel
if (-not (Test-Path $exe)) { throw "exe not found: $exe" }

$p = Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe -Parent) -PassThru

# Wait for the player window (up to 20 s)
$h = [IntPtr]::Zero
for ($try = 0; $try -lt 40 -and $h -eq [IntPtr]::Zero; $try++) {
    Start-Sleep -Milliseconds 500
    $proc = Get-Process -Id $p.Id -ErrorAction SilentlyContinue
    if ($null -ne $proc -and $proc.MainWindowHandle -ne [IntPtr]::Zero) { $h = $proc.MainWindowHandle }
}
if ($h -eq [IntPtr]::Zero) { Stop-Process -Id $p.Id -Force; throw "game window not found" }

$r = New-Object W+R
[W]::GetClientRect($h, [ref]$r) | Out-Null
$w = $r.Rt - $r.L; $ht = $r.B - $r.T
Write-Output "client rect: ${w}x${ht}, hwnd=$h"

function Key([uint32]$vk, [int]$ms) {
    $scan = [W]::MapVirtualKey($vk, 0)
    $lpDown = [IntPtr]([int64]1 -bor ([int64]$scan -shl 16))
    $lpUp   = [IntPtr]([int64]1 -bor ([int64]$scan -shl 16) -bor ([int64]1 -shl 30) -bor ([int64]1 -shl 31))
    [W]::PostMessage($h, 0x0100, [IntPtr]$vk, $lpDown) | Out-Null
    Start-Sleep -Milliseconds $ms
    [W]::PostMessage($h, 0x0101, [IntPtr]$vk, $lpUp) | Out-Null
}

# Click at client coordinates via WM_LBUTTONDOWN / WM_LBUTTONUP
function Click([int]$cx, [int]$cy) {
    $lp = [IntPtr]((([int64]$cy -band 0xFFFF) -shl 16) -bor ([int64]$cx -band 0xFFFF))
    [W]::PostMessage($h, 0x0200, [IntPtr]0x0001, $lp) | Out-Null   # WM_MOUSEMOVE, MK_LBUTTON
    Start-Sleep -Milliseconds 80
    [W]::PostMessage($h, 0x0201, [IntPtr]0x0001, $lp) | Out-Null   # WM_LBUTTONDOWN
    Start-Sleep -Milliseconds 60
    [W]::PostMessage($h, 0x0202, [IntPtr]0,     $lp) | Out-Null    # WM_LBUTTONUP
}

$bmp = New-Object System.Drawing.Bitmap $w, $ht
$g = [System.Drawing.Graphics]::FromImage($bmp)

function Snap([string]$name) {
    $hdc = $g.GetHdc()
    [W]::PrintWindow($h, $hdc, 3) | Out-Null
    $g.ReleaseHdc($hdc)
    $bmp.Save((Join-Path $out $name), [System.Drawing.Imaging.ImageFormat]::Png)
}

try {
    Start-Sleep -Milliseconds 1500

    if ($Phase -eq "menu") {
        Snap "menu_000.png"
        Write-Output "menu frame saved - inspect it to find the Start button"
    } else {
        # click Start, then play with scripted input
        if ($ClickX -ge 0 -and $ClickY -ge 0) {
            Click $ClickX $ClickY
            Start-Sleep -Milliseconds 1200
        }
        $frame = 0
        for ($i = 0; $i -lt $Frames; $i++) {
            $vk = 0; $holdMs = 0
            switch ($i % 20) {
                3  { $vk = 0x44; $holdMs = 180 }   # D - switch to right lane
                7  { $vk = 0x20; $holdMs = 120 }   # Space - jump
                10 { $vk = 0x41; $holdMs = 180 }   # A - switch to left lane
                15 { $vk = 0x20; $holdMs = 120 }   # Space - jump
            }
            if ($vk -ne 0) { Key $vk $holdMs }
            Snap ("game_{0:d3}.png" -f $frame)
            $frame++
            Start-Sleep -Milliseconds $FrameGapMs
        }
        Write-Output "captured $frame frames"
    }
} finally {
    $g.Dispose(); $bmp.Dispose()
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
}
