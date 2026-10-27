<div align="center">

# 🔴 Signal No Ar — Display Kiosk & Monitor vMix

**Sistema autônomo de exibição contínua para estúdios, púlpito e cabines de transmissão.**

[![Download Direto](https://img.shields.io/badge/⬇️_DOWNLOAD_DIRETO-Instalador_Signal_No_Ar.exe-ff7a00?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/MathGuthmann/signal-no-ar/releases/download/latest/Instalador_Signal_No_Ar.exe)
[![Última Versão](https://img.shields.io/github/v/release/MathGuthmann/signal-no-ar?style=for-the-badge&color=7B2CBF&label=Vers%C3%A3o)](https://github.com/MathGuthmann/signal-no-ar/releases/latest)
[![Status do Build](https://img.shields.io/github/actions/workflow/status/MathGuthmann/signal-no-ar/build.yml?branch=main&style=for-the-badge&label=Build%20%26%20Release)](https://github.com/MathGuthmann/signal-no-ar/actions)

<p align="center">
  <a href="https://github.com/MathGuthmann/signal-no-ar/releases/download/latest/Instalador_Signal_No_Ar.exe">
    <b>👉 Clique aqui para Baixar o Instalador (Versão Mais Recente)</b>
  </a>
</p>

*O link acima faz o download direto da versão estável mais recente publicada no repositório.*

---

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Plataforma-Windows%2010%20%2F%2011%20x64-0078D6?logo=windows)](https://microsoft.com/windows)
[![WebView2](https://img.shields.io/badge/Chromium-WebView2-0078D4?logo=microsoft-edge)](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)
[![vMix](https://img.shields.io/badge/Integra%C3%A7%C3%A3o-vMix%20API-E50914)](https://www.vmix.com/)

</div>

Aplicativo cliente em modo Kiosk desenvolvido especificamente para o ecossistema **Signal** ([media-hub-hq](https://github.com/MathGuthmann/media-hub-hq)). Ele é projetado para rodar em estúdios, igrejas, cabines de streaming e salas de controle, exibindo a página de **NO AR** (`https://signal.mathguthmann.com/no-ar`) ou **Monitor de Palco** em uma tela dedicada de forma contínua, resiliente e automática.

---

## ⚡ Download e Instalação Rápida

1. Baixe o instalador oficial:  
   👉 **[Instalador_Signal_No_Ar.exe (Download Direto)](https://github.com/MathGuthmann/signal-no-ar/releases/download/latest/Instalador_Signal_No_Ar.exe)**
2. Execute o arquivo baixado e clique em **Instalar Agora** (ou **Atualizar Agora** se já tiver uma versão anterior).
3. **Dependências Automáticas**: Se o computador não tiver o componente *Microsoft Edge WebView2*, o próprio instalador baixa e instala automaticamente em segundo plano.
4. Escolha o monitor onde o sinal deve ser exibido. Pronto!

> [!TIP]
> **Atualizações em um clique**: Quando sair uma nova versão no GitHub, você não precisa reinstalar manualmente! O aplicativo atualiza sozinho na inicialização ou através do botão **"Procurar Atualizações"** no painel de configurações.

---

## 🚀 Principais Recursos

- 🖥️ **Multi-Telas Simultâneas (No Ar + Palco)**: Capacidade de exibir o **Display 1 (No Ar)** no monitor do púlpito e simultaneamente o **Display 2 (Palco / Retorno)** no monitor dos músicos a partir do mesmo aplicativo.
- ⚡ **Latência Zero na Rede Local**: Comunicação direta entre o aplicativo nativo e as páginas web via memória RAM (`window.postMessage`), garantindo atualizações instantâneas (< 1ms) sem depender de conexões com a nuvem ou delay de banco de dados.
- 🎵 **Leitor Local do Holyrics Integrado**: Conecta diretamente ao servidor local da API do Holyrics (`http://localhost:8091`), monitorando letras, slides ativos, versículos bíblicos e cronômetro com latência zero.
- 📡 **Leitor Automático do vMix Integrado**: Monitora a porta `8088` do vMix em tempo real (`http://localhost:8088/api`) e detecta se o streaming está **AO VIVO** ou em **Standby**.
- 💤 **Prevenção de Suspensão de Tela (Sleep Blocker)**: Mantém os monitores de estúdio/palco e o computador permanentemente ligados durante o evento, impedindo descanso de tela e modo de suspensão do Windows via Win32 `SetThreadExecutionState`.
- 🛡️ **Tela de Emergência / Fallback Inteligente**: Se a internet oscilar, nada de tela branca do navegador (`ERR_INTERNET_DISCONNECTED`); o Signal No Ar exibe uma interface escura elegante com reconexão automática e mostra em tempo real o status dos equipamentos locais (vMix e Holyrics 100% operacionais).
- 📦 **Instalador Auto-suficiente**: Baixa e instala o *Microsoft Edge WebView2 Evergreen Runtime* silenciosamente da Microsoft se a máquina não o possuir.
- 🔄 **Atualizações Automáticas na Inicialização**: Ao ligar o computador, o Signal No Ar checa o GitHub em segundo plano. Se houver versão nova, ele atualiza e reinicia suavemente sem perder suas configurações.
- 🔍 **Botão Manual de Atualização**: Localizado no Painel de Controle e no menu do ícone da barra de tarefas (bandeja do Windows).
- 🚀 **Inicialização com o Windows**: Inicia automaticamente em segundo plano ao ligar ou reiniciar o computador (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`).
- 🖥️ **Tela Cheia no Monitor Escolhido**: Preenche 100% da tela do monitor selecionado sem bordas, menus ou barras de navegação.
- 🛡️ **Blindagem contra Fechamento Acidental**: Bloqueia teclas de fechamento acidental (`Alt + F4`, `Ctrl + W`), com reinicialização automática do renderizador em caso de falha de processo.
- 🔕 **100% Oculto do Alt + Tab e Barra de Tarefas**: O display funciona em segundo plano no monitor de destino sem aparecer no switcher de janelas (`Alt + Tab`), evitando que os operadores do switcher/vMix alternem acidentalmente para a tela de retorno.
- 🔌 **Detecção de Monitores em Tempo Real (Hotplug)**: Monitora conexões e desconexões de telas (cabos HDMI/DisplayPort). Se a tela do estúdio for desligada ou desconectada, o aplicativo pausa o sinal com segurança e avisa o usuário.
- 🚪 **Saída de Emergência [ESC]**: Ao pressionar a tecla `ESC` ou `Ctrl + Q`, o display é minimizado para a bandeja imediatamente. Ao passar o mouse no topo da tela, uma barra discreta de controle também se torna visível.
- 🔄 **Reconexão Inteligente**: Caso o PC ligue antes do Wi-Fi/cabo de rede estar pronto, uma tela de conexão exibe a contagem regressiva ao vivo e reconecta automaticamente assim que o servidor responder.
- ⚙️ **Painel de Controle na Bandeja (System Tray)**: Ícone discreto próximo ao relógio do Windows permitindo:
  - Trocar de monitor a qualquer momento;
  - Testar conectividade com a API do vMix;
  - Desativar temporariamente para a sessão atual (volta a abrir sozinho no próximo boot);
  - Identificar os monitores numerados piscando na tela;
  - Alternar modo janela (`F11`).

---

## 🏗️ Estrutura do Repositório

```
SignalNoAr/
├── .github/workflows/
│   └── build.yml                     # Pipeline CI/CD que gera e publica novos releases automaticamente
├── src/
│   ├── SignalNoAr/                   # Aplicativo principal (Display Kiosk + Tray + Painel + vMix + Updates)
│   │   ├── Models/AppConfig.cs       # Gerenciamento de configuração JSON e registro
│   │   ├── Services/
│   │   │   ├── VmixWatcher.cs        # Monitor contínuo de polling da API do vMix
│   │   │   └── UpdateService.cs      # Motor de download e checagem de atualizações no GitHub
│   │   ├── UI/
│   │   │   ├── DisplayForm.cs        # Janela Kiosk borderless com WebView2 e auto-recovery
│   │   │   ├── SettingsForm.cs       # Painel de controle responsivo com cartões de vMix e Atualização
│   │   │   └── MonitorIdentifyForm.cs # Sobreposição numérica para identificação de telas
│   │   ├── AppContext.cs             # Gerenciamento da bandeja, hotplug, IPC e auto-update
│   │   └── Program.cs                # Ponto de entrada com Mutex e passagem de comandos
│   │
│   └── Installer/                    # Instalador visual autocontido com suporte a auto-update
│       ├── InstallerForm.cs          # Interface de instalação, download do WebView2 e atalhos
│       └── Program.cs
│
├── dist/                             # Binários gerados
│   └── Instalador_Signal_No_Ar.exe   # Instalador autocontido final
│
├── version.json                      # Controle de versão para os clientes checarem atualizações
├── pack_payload.py                   # Script de empacotamento do payload.zip
└── README.md
```

---

## 🛠️ Como Compilar Localmente

### Pré-requisitos:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Python 3.10+
- Windows 10 ou 11 (64-bit)

### 1. Compilando o Aplicativo Principal:
```bash
dotnet publish src/SignalNoAr/SignalNoAr.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o staging
```

### 2. Empacotando e Compilando o Instalador:
```bash
copy src\SignalNoAr\app.ico staging\app.ico
python pack_payload.py
dotnet publish src/Installer/Installer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o dist
copy dist\Instalador_SignalNoAr.exe dist\Instalador_Signal_No_Ar.exe
```

---

## 📄 Licença e Créditos

Desenvolvido para integração com o ecossistema [Signal](https://signal.mathguthmann.com) por Matheus Guthmann.
