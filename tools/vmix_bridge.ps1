<#
.SYNOPSIS
    Signal vMix Bridge - Monitor de Transmissão ao Vivo em Tempo Real
.DESCRIPTION
    Monitora a API HTTP nativa do vMix (porta 8088) e detecta quando a transmissão
    ao vivo (Stream) e gravação (Record) são iniciadas ou paradas.
    Pode enviar o status diretamente para o Signal / Supabase em tempo real.
#>

param (
    [string]$VmixUrl = "http://localhost:8088/api",
    [int]$IntervalSec = 1,
    [string]$SignalApiUrl = "", # URL do endpoint do Signal ou Supabase
    [string]$ApiKey = ""
)

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Magenta
Write-Host "       📡 SIGNAL VMIX BRIDGE - MONITOR AO VIVO           " -ForegroundColor Yellow
Write-Host "==========================================================" -ForegroundColor Magenta
Write-Host " Conectando na API do vMix em: $VmixUrl" -ForegroundColor Cyan
if ($SignalApiUrl) {
    Write-Host " Sincronizando com Signal em: $SignalApiUrl" -ForegroundColor Cyan
} else {
    Write-Host " Modo Local: Logs visuais no terminal (defina -SignalApiUrl para sincronizar com nuvem)" -ForegroundColor DarkGray
}
Write-Host " Pressione Ctrl + C para encerrar." -ForegroundColor DarkGray
Write-Host "----------------------------------------------------------" -ForegroundColor DarkGray
Write-Host ""

$lastStreaming = $null
$lastRecording = $null
$lastActiveInput = $null
$liveStartTime = $null

while ($true) {
    try {
        $xml = Invoke-RestMethod -Uri $VmixUrl -Method Get -TimeoutSec 2 -ErrorAction Stop
        
        $isStreaming = ($xml.vmix.streaming -eq "True")
        $isRecording = ($xml.vmix.recording -eq "True")
        $activeInput = $xml.vmix.active
        $previewInput = $xml.vmix.preview

        # Mudança no status de streaming
        if ($isStreaming -ne $lastStreaming) {
            $lastStreaming = $isStreaming
            $time = Get-Date -Format "HH:mm:ss"

            if ($isStreaming) {
                $liveStartTime = Get-Date
                Write-Host "[$time] 🔴 VMIX AO VIVO! Transmissão INICIADA (Entrada PGM: $activeInput)" -ForegroundColor Red
            } else {
                $duration = ""
                if ($liveStartTime) {
                    $elapsed = (Get-Date) - $liveStartTime
                    $duration = " (Duração: $($elapsed.ToString('hh\:mm\:ss')))"
                    $liveStartTime = $null
                }
                Write-Host "[$time] ⚪ VMIX OFFLINE. Transmissão ENCERRADA$duration" -ForegroundColor Gray
            }

            # Envia atualização para a API se configurado
            if ($SignalApiUrl) {
                try {
                    $body = @{
                        is_live = $isStreaming
                        is_recording = $isRecording
                        active_input = $activeInput
                        updated_at = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
                    } | ConvertTo-Json

                    $headers = @{ "Content-Type" = "application/json" }
                    if ($ApiKey) { $headers["apikey"] = $ApiKey; $headers["Authorization"] = "Bearer $ApiKey" }

                    Invoke-RestMethod -Uri $SignalApiUrl -Method Post -Body $body -Headers $headers -TimeoutSec 3 | Out-Null
                    Write-Host "       └─ Sincronizado com o Signal!" -ForegroundColor Green
                } catch {
                    Write-Host "       └─ [Aviso] Falha ao enviar para o Signal: $($_.Exception.Message)" -ForegroundColor DarkYellow
                }
            }
        }

        # Mudança no status de gravação
        if ($isRecording -ne $lastRecording) {
            $lastRecording = $isRecording
            $time = Get-Date -Format "HH:mm:ss"
            if ($isRecording) {
                Write-Host "[$time] 💾 Gravação Local INICIADA" -ForegroundColor DarkCyan
            } else {
                Write-Host "[$time] 💾 Gravação Local PARADA" -ForegroundColor DarkGray
            }
        }

    } catch {
        if ($lastStreaming -ne "DISCONNECTED") {
            $lastStreaming = "DISCONNECTED"
            $lastRecording = $null
            $time = Get-Date -Format "HH:mm:ss"
            Write-Host "[$time] ⚠️ vMix desconectado (Aguardando vMix abrir na porta 8088...)" -ForegroundColor Yellow
        }
    }

    Start-Sleep -Seconds $IntervalSec
}
