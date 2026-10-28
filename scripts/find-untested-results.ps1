# Fase 23: encontra os resultados de casos de uso que nenhum teste confere.
#
# Cada caso de uso da Application devolve um enum (ManageMealResult, LoginStatus...). A cobertura de linhas
# não mostra se cada valor desse enum tem um teste: um "return X.NotFound" pode ser executado por acaso,
# sem nenhum Assert sobre ele. Este script lista os valores que não aparecem em nenhum arquivo de teste.
#
# Uso (na raiz do repositório):
#   powershell -File scripts/find-untested-results.ps1
#
# Sai com código 1 quando encontra algum valor sem teste, para poder rodar numa verificação automática.

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$sourceDir = Join-Path $root 'src/NogVita.Application'
$testsDir = Join-Path $root 'tests/NogVita.UnitTests'

# Todo o texto dos testes, de uma vez: a busca é por "Enum.Valor".
# As pastas de build ficam de fora; o filtro aceita "\" (Windows) e "/" (o Linux do GitHub Actions).
$buildFolders = '[\\/](bin|obj)[\\/]'

$testText = (Get-ChildItem $testsDir -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch $buildFolders } |
    ForEach-Object { Get-Content $_.FullName -Raw -Encoding UTF8 }) -join "`n"

$enumPattern = 'enum\s+(?<name>\w+(?:Result|Status))\s*\{(?<body>[^}]*)\}'
$missing = @()
$total = 0

Get-ChildItem $sourceDir -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch $buildFolders } |
    ForEach-Object {
        $file = $_
        $code = Get-Content $file.FullName -Raw -Encoding UTF8
        if (-not $code) { return } # arquivo vazio

        foreach ($match in [regex]::Matches($code, $enumPattern)) {
            $enumName = $match.Groups['name'].Value
            $members = $match.Groups['body'].Value -split ',' |
                ForEach-Object { ($_ -replace '//.*', '').Trim() } |
                Where-Object { $_ -match '^\w+$' }

            foreach ($member in $members) {
                $total++
                if ($testText -notmatch "\b$enumName\.$member\b") {
                    $missing += [pscustomobject]@{
                        Resultado = "$enumName.$member"
                        Arquivo   = $file.FullName.Substring($root.Length + 1)
                    }
                }
            }
        }
    }

if ($missing.Count -eq 0) {
    Write-Host "Todos os $total resultados de casos de uso aparecem em algum teste."
    exit 0
}

Write-Host "$($missing.Count) de $total resultados de casos de uso sem nenhum teste:"
$missing | Sort-Object Resultado | Format-Table -AutoSize | Out-String -Width 200 | Write-Host
exit 1
