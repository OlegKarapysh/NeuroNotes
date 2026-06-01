# Stop hook: format the .cs files changed in the working tree, once per turn.
# Keeps agent- and human-written code consistently formatted to .editorconfig
# without the per-edit latency of formatting on every Edit/Write.
#
# Notes:
#  - Only files that are part of NeuroNotes.slnx are formatted (dotnet format --include
#    silently ignores paths outside the solution), so out-of-solution scratch projects
#    are left alone.
#  - Always exits 0 so it can never block the agent.
$ErrorActionPreference = 'SilentlyContinue'

$root = $env:CLAUDE_PROJECT_DIR
if ([string]::IsNullOrWhiteSpace($root)) { $root = (Get-Location).Path }

$solution = Join-Path $root 'NeuroNotes.slnx'
if (-not (Test-Path -LiteralPath $solution)) { exit 0 }

# Changed (tracked, staged) + newly added (untracked) C# files, relative to repo root.
$changed = @()
$changed += git -C $root diff      --name-only --diff-filter=ACMR -- '*.cs'
$changed += git -C $root diff      --name-only --cached --diff-filter=ACMR -- '*.cs'
$changed += git -C $root ls-files  --others --exclude-standard -- '*.cs'

$files = $changed | Where-Object { $_ } | Sort-Object -Unique
if (-not $files) { exit 0 }

dotnet format $solution --include $files --no-restore *> $null
exit 0
