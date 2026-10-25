<#
.SYNOPSIS
    Simulador Interativo de vMix para Testes Locais
.DESCRIPTION
    Cria um servidor HTTP local na porta 8088 imitando 100% a API do vMix.
    Permite alternar entre Ao Vivo (Stream ON) e Offline (Stream OFF) teclando 'S'.
#>

$port = 8088
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$port/")

try {
    $listener.Start()
} catch {
    Write-Error "Nao foi possivel iniciar na porta $port. Verifique se o vMix real ja esta aberto ou execute como Administrador."
    exit 1
}

$isStreaming = $false
$isRecording = $false
$activeInput = 1

Clear-Host
Write-Host "==========================================================" -ForegroundColor Magenta
Write-Host "       🎬 SIMULADOR DE VMIX PARA TESTES DO SIGNAL         " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Magenta
Write-Host " Servidor rodando em: http://localhost:$port/api" -ForegroundColor Cyan
Write-Host ""
Write-Host " CONTROLES INTERATIVOS NO TECLADO:" -ForegroundColor White
Write-Host "  [S] -> Alternar Ao Vivo (Stream ON / OFF)" -ForegroundColor Yellow
Write-Host "  [R] -> Alternar Gravacao (Record ON / OFF)" -ForegroundColor Cyan
Write-Host "  [1-4] -> Mudar Camera do Corte (PGM)" -ForegroundColor Green
Write-Host "  [Q] -> Encerrar Simulador" -ForegroundColor Red
Write-Host "----------------------------------------------------------" -ForegroundColor DarkGray
Write-Host " Estado atual: ⚪ OFFLINE (Stream: False | Rec: False | PGM: 1)" -ForegroundColor Gray
Write-Host ""

$running = $true

# Thread para processar requisicoes HTTP
$scriptBlock = {
    param($listener, $refStreaming, $refRecording, $refInput)
    while ($listener.IsListening) {
        try {
            $context = $listener.GetContext()
            $request = $context.Request
            $response = $context.Response

            if ($request.Url.LocalPath -eq "/api" -or $request.Url.LocalPath -eq "/api/") {
                $strStream = if ($refStreaming.Value) { "True" } else { "False" }
                $strRec = if ($refRecording.Value) { "True" } else { "False" }
                $inp = $refInput.Value

                $xml = @"
<?xml version="1.0" encoding="utf-8"?>
<vmix>
    <version>27.0.0.85</version>
    <edition>Pro</edition>
    <preset>C:\Igreja\Culto.vmix</preset>
    <inputs>
        <input key="cam1" number="1" type="Camera" title="Camera 1 - Pulpito" state="Running" position="0" duration="0" loop="False" muted="False" volume="100" balance="0" solo="False" audiobusses="M">Camera 1</input>
        <input key="cam2" number="2" type="Camera" title="Camera 2 - Louvor" state="Running" position="0" duration="0" loop="False" muted="False" volume="100" balance="0" solo="False" audiobusses="M">Camera 2</input>
        <input key="holyrics" number="3" type="Browser" title="Letras - Holyrics" state="Running" position="0" duration="0" loop="False" muted="True" volume="100" balance="0" solo="False" audiobusses="M">Letras</input>
    </inputs>
    <overlays>
        <overlay number="1" />
        <overlay number="2" />
    </overlays>
    <preview>2</preview>
    <active>$inp</active>
    <fadeToBlack>False</fadeToBlack>
    <transitions>
        <transition number="1" name="Fade" duration="500" />
    </transitions>
    <recording>$strRec</recording>
    <external>False</external>
    <streaming>$strStream</streaming>
    <playList>False</playList>
    <multiCorder>False</multiCorder>
    <fullscreen>True</fullscreen>
    <audio>
        <master volume="100" muted="False" meterF1="0.5" meterF2="0.5" />
    </audio>
</vmix>
"@
                $buffer = [System.Text.Encoding]::UTF8.GetBytes($xml)
                $response.ContentType = "text/xml"
                $response.ContentLength64 = $buffer.Length
                $response.StatusCode = 200
                $response.OutputStream.Write($buffer, 0, $buffer.Length)
                $response.OutputStream.Close()
            } else {
                $response.StatusCode = 404
                $response.Close()
            }
        } catch {
            break
        }
    }
}

$refStreaming = [ref]$isStreaming
$refRecording = [ref]$isRecording
$refInput = [ref]$activeInput

$job = Start-Job -ScriptBlock $scriptBlock -ArgumentList $listener, $refStreaming, $refRecording, $refInput

# Loop principal de teclado
try {
    while ($running) {
        if ([Console]::KeyAvailable) {
            $key = [Console]::ReadKey($true).Key
            if ($key -eq [ConsoleKey]::S) {
                $refStreaming.Value = -not $refStreaming.Value
                if ($refStreaming.Value) {
                    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 🔴 AO VIVO ATIVADO! (vMix Streaming: True)" -ForegroundColor Red
                } else {
                    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ⚪ OFFLINE ATIVADO! (vMix Streaming: False)" -ForegroundColor Gray
                }
            } elseif ($key -eq [ConsoleKey]::R) {
                $refRecording.Value = -not $refRecording.Value
                if ($refRecording.Value) {
                    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 💾 GRAVACAO INICIADA! (vMix Recording: True)" -ForegroundColor DarkCyan
                } else {
                    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 💾 GRAVACAO PARADA! (vMix Recording: False)" -ForegroundColor DarkGray
                }
            } elseif ($key -ge [ConsoleKey]::D1 -and $key -le [ConsoleKey]::D4) {
                $refInput.Value = [int]$key - [int][ConsoleKey]::D0
                Write-Host "[$(Get-Date -Format 'HH:mm:ss')] 📺 CORTE PGM ALTERADO -> Entrada $($refInput.Value)" -ForegroundColor Green
            } elseif ($key -eq [ConsoleKey]::Q) {
                $running = $false
                break
            }
        }
        Start-Sleep -Milliseconds 100
    }
} finally {
    $listener.Stop()
    $listener.Close()
    Stop-Job $job -ErrorAction SilentlyContinue
    Remove-Job $job -ErrorAction SilentlyContinue
    Write-Host "`nSimulador de vMix encerrado com sucesso." -ForegroundColor Yellow
}
