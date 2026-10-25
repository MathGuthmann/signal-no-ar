# 🎬 Integração vMix ➔ Signal (Detecção Automática de Ao Vivo / No Ar)

> **Guia completo de arquitetura, funcionamento da API do vMix e sincronização automática em tempo real com todas as telas do Signal.**

---

## 📌 1. Como o vMix Funciona e Como Ele "Fala" com o Mundo

O **vMix** possui uma **API HTTP Web nativa** que já vem integrada de fábrica. Quando o vMix está aberto no computador da transmissão, ele disponibiliza um servidor web interno na porta padrão **`8088`**:

```text
http://localhost:8088/api
```

*(Ou `http://IP_DO_PC_VMIX:8088/api` para outros aparelhos na mesma rede da igreja)*.

### O que essa API retorna?
Uma chamada simples `GET http://localhost:8088/api` retorna instantaneamente um XML completo com o estado do vMix:

```xml
<vmix>
    <version>27.0.0.85</version>
    
    <!-- 🔴 ESTADO DE TRANSMISSÃO AO VIVO -->
    <streaming>True</streaming>
    
    <!-- 💾 ESTADO DE GRAVAÇÃO LOCAL -->
    <recording>True</recording>
    
    <!-- 📺 ENTRADAS ATIVAS -->
    <active>1</active>       <!-- Entrada no PGM / Ao Vivo no Corte -->
    <preview>3</preview>      <!-- Entrada no Preview / Próxima a entrar -->
    
    <fadeToBlack>False</fadeToBlack>
    ...
</vmix>
```

- Quando o operador clica no botão **Stream** no vMix: `<streaming>` muda imediatamente para **`True`**.
- Quando o operador encerra a live: `<streaming>` muda imediatamente para **`False`**.

---

## 🏗️ 2. A Arquitetura da Solução: "Signal vMix Bridge"

Para fazer essa informação chegar em **todas as telas** (telas de estúdio, TV de retorno do palco com Raspberry Pi, e painéis dos operadores), usamos o **Signal vMix Bridge**:

```
 ┌──────────────────────────────────────────────┐
 │             PC DO VMIX (CABINE)              │
 │                                              │
 │  ┌──────────────┐         ┌───────────────┐  │
 │  │     vMix     │ ◄─────► │ Signal vMix   │  │
 │  │  (Port 8088) │  /api   │    Bridge     │  │
 │  └──────────────┘         └───────┬───────┘  │
 └───────────────────────────────────┼──────────┘
                                     │
           Atualiza em Tempo Real    │ (Supabase Realtime / Webhook)
                                     ▼
                      ┌────────────────────────────┐
                      │    SIGNAL (NUVEM / WEB)    │
                      │  signal.mathguthmann.com   │
                      └──────────────┬─────────────┘
                                     │
              Distribui Instantaneamente (< 100ms)
                                     │
        ┌────────────────────────────┼───────────────────────────┐
        ▼                            ▼                           ▼
┌──────────────────┐       ┌──────────────────┐        ┌──────────────────┐
│  TELA NO AR      │       │ MONITOR DE PALCO │        │ CELULARES /      │
│  (Display Kiosk) │       │ (Raspberry Pi 3) │        │ PAINEL DA EQUIPE │
│                  │       │                  │        │                  │
│  🔴 NO AR        │       │ 🔴 AO VIVO       │        │ 🔴 TRANSMISSÃO   │
│  (Estúdio/Corte) │       │ (Letras/Relógio) │        │    EM ANDAMENTO  │
└──────────────────┘       └──────────────────┘        └──────────────────┘
```

---

## 🚀 3. As Vantagens dessa Abordagem

1. **Automação 100% Zero-Touch**:
   - A equipe de som, mídia ou o pastor não precisam lembrar de "ativar o site".
   - O operador de vídeo apenas aperta o botão normal de **Stream** no vMix. Automaticamente, as placas de "NO AR" no estúdio acendem em vermelho e a tela do palco ativa o modo de transmissão ao vivo!
2. **Histórico e Métricas Automáticas no Signal**:
   - O Signal passa a saber a que horas exatamente a live começou e terminou, calculando a duração real do culto/evento.
3. **Resiliente e Sem Sobrecarga**:
   - O Bridge consome menos de **0.1% de CPU** do computador do vMix (apenas uma leitura leve por segundo).
   - Se o vMix for fechado, o Bridge fica em silêncio aguardando ele abrir novamente, sem gerar erros.

---

## 💻 4. Script de Teste / Demonstração Rápida (`vmix_monitor.ps1`)

Você pode testar isso no computador onde o vMix roda executando o script abaixo no PowerShell. Ele monitora o vMix e avisa no terminal sempre que a live começar ou parar:

```powershell
# ==========================================================
# Signal vMix Monitor - Teste de Leitura da API do vMix
# ==========================================================
$vmixUrl = "http://localhost:8088/api"
$lastStreaming = $null

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  📡 SIGNAL VMIX BRIDGE - Monitorando vMix...  " -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan

while ($true) {
    try {
        $xml = Invoke-RestMethod -Uri $vmixUrl -Method Get -TimeoutSec 2 -ErrorAction Stop
        $isStreaming = ($xml.vmix.streaming -eq "True")
        $isRecording = ($xml.vmix.recording -eq "True")
        $pgmInput = $xml.vmix.active

        if ($isStreaming -ne $lastStreaming) {
            $lastStreaming = $isStreaming
            $timestamp = Get-Date -Format "HH:mm:ss"

            if ($isStreaming) {
                Write-Host "[$timestamp] 🔴 VMIX AO VIVO! Transmissão ATIVA (PGM: Entrada $pgmInput)" -ForegroundColor Red
                # AQUI: Envia para o Supabase/Signal -> { no_ar: true, live_started_at: ... }
            } else {
                Write-Host "[$timestamp] ⚪ VMIX OFFLINE. Transmissão PARADA" -ForegroundColor DarkGray
                # AQUI: Envia para o Supabase/Signal -> { no_ar: false }
            }
        }
    } catch {
        if ($lastStreaming -ne "DISCONNECTED") {
            $lastStreaming = "DISCONNECTED"
            $timestamp = Get-Date -Format "HH:mm:ss"
            Write-Host "[$timestamp] ⚠️ Aguardando vMix abrir na porta 8088..." -ForegroundColor Yellow
        }
    }
    Start-Sleep -Seconds 1
}
```

---

## 💬 5. Prompt para a Lovable (Adicionar Suporte a vMix / Live Status no Signal)

Copie o prompt abaixo para enviar no chat da **Lovable** no projeto do Signal para que o banco e as telas fiquem preparadas para receber o status do vMix:

```markdown
Adicione suporte ao monitoramento de status da transmissão ao vivo (vMix / Streaming Status) no Signal:

1. **Banco de Dados / Supabase**:
   - Crie uma tabela (ou adicione na configuração do sistema) chamada `stream_status`:
     - `id`: uuid (primary key)
     - `is_live`: boolean (default false)
     - `is_recording`: boolean (default false)
     - `started_at`: timestamp with time zone (nullable)
     - `ended_at`: timestamp with time zone (nullable)
     - `active_input`: text (nullable)
     - `source`: text (default 'vmix')
     - `updated_at`: timestamp with time zone
   - Ative o **Supabase Realtime** nessa tabela para que qualquer alteração seja transmitida instantaneamente para os clientes conectados.

2. **Hook de Status Global (`useStreamStatus`)**:
   - Crie um hook React que assina as alterações em tempo real de `stream_status`.
   - Disponibilize `isLive`, `isRecording`, `liveDuration` (cronômetro de quanto tempo a live está no ar).

3. **Integração nas Telas**:
   - Na tela `/no-ar`: quando `isLive === true`, exiba o badge e visual "🔴 NO AR - AO VIVO". Quando `false`, exiba "⚪ OFFLINE / PREPARAÇÃO".
   - Na tela `/palco`: integre esse status no banner superior e no painel de descanso do monitor de palco.
   - Crie uma rota de API segura ou webhook simples (ex: `/api/vmix-status`) para que o aplicativo Bridge no PC do vMix possa atualizar o status com uma chave de autenticação.
```

---

## 🎯 6. Resumo e Como Implementar

1. **No Signal (Lovable)**: A Lovable cria a tabela com Realtime e o hook que faz as telas (`/no-ar` e `/palco`) reagirem ao vivo.
2. **No PC do vMix**: Colocamos o **Signal vMix Bridge** (um executável levinho que roda ao lado do relógio do Windows). Assim que o vMix dá Stream, ele avisa o Signal e todas as telas do prédio respondem na mesma hora!
