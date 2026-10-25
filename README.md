# 📡 Signal No Ar — Display Kiosk Automatizado

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?logo=windows)](https://microsoft.com/windows)
[![WebView2](https://img.shields.io/badge/Chromium-WebView2-0078D4?logo=microsoft-edge)](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

Aplicativo cliente em modo Kiosk desenvolvido especificamente para o ecossistema **Signal** ([media-hub-hq](https://github.com/MathGuthmann/media-hub-hq)). Ele é projetado para rodar em estúdios, cabines de streaming e salas de controle, exibindo o status do sinal de **NO AR** (`https://signal.mathguthmann.com/no-ar`) em uma tela dedicada de forma contínua, resiliente e automática.

---

## 🎯 Principais Recursos

- ⚡ **Inicialização com o Windows**: Inicia automaticamente em segundo plano ao ligar ou reiniciar o computador (`HKCU\...\Run`).
- 📺 **Tela Cheia no Monitor Escolhido**: Preenche 100% da tela do monitor selecionado sem bordas, menus ou barras de navegação.
- 🛡️ **Blindagem contra Fechamento Acidental**: Bloqueia teclas de fechamento acidental (`Alt + F4`, `Ctrl + W`), com reinicialização automática do renderizador em caso de falha de processo.
- 🚫 **100% Oculto da Barra de Tarefas**: O display funciona em segundo plano no monitor de destino sem criar ícone na barra de tarefas principal, evitando distrações e cliques acidentais.
- 🔌 **Detecção de Monitores em Tempo Real (Hotplug)**: Monitora conexões e desconexões de telas (cabos HDMI/DisplayPort). Se a tela do estúdio for desligada ou desconectada, o aplicativo pausa o sinal com segurança e avisa o usuário.
- 🚨 **Saída de Emergência [ESC]**: Ao pressionar a tecla `ESC` ou `Ctrl + Q`, o display é minimizado para a bandeja imediatamente. Ao passar o mouse no topo da tela, uma barra discreta de controle também se torna visível.
- 🔄 **Reconexão Inteligente**: Caso o PC ligue antes do Wi-Fi/cabo de rede estar pronto, uma tela de conexão exibe a contagem regressiva ao vivo e reconecta automaticamente assim que o servidor responder.
- ⚙️ **Painel de Controle na Bandeja (System Tray)**: Ícone discreto próximo ao relógio do Windows permitindo:
  - Trocar de monitor a qualquer momento;
  - Desativar temporariamente para a sessão atual (volta a abrir sozinho no próximo boot);
  - Identificar os monitores numerados piscando na tela;
  - Alternar modo janela (`F11`).

---

## 🏗️ Estrutura da Solução

```
SignalNoAr/
├── src/
│   ├── SignalNoAr/                 # Aplicativo principal (Display Kiosk + Tray + Painel)
│   │   ├── Models/AppConfig.cs     # Gerenciamento de configuração JSON e registro
│   │   ├── UI/DisplayForm.cs       # Janela Kiosk borderless com WebView2
│   │   ├── UI/SettingsForm.cs      # Painel de controle responsivo
│   │   ├── UI/MonitorIdentifyForm.cs # Sobreposição numérica para identificação de telas
│   │   ├── AppContext.cs           # Gerenciamento da bandeja, hotplug e IPC
│   │   └── Program.cs              # Ponto de entrada com Mutex e passagem de comandos
│   │
│   └── Installer/                  # Instalador visual autocontido
│       ├── InstallerForm.cs        # Interface de instalação, atalhos e inicialização
│       └── Program.cs
│
├── dist/                           # Binários e scripts para distribuição
│   ├── Instalador_Signal_No_Ar.exe # Instalador autocontido
│   ├── Instalar_Rapido.bat         # Script alternativo de instalação silenciosa
│   └── Desinstalar.bat             # Desinstalador completo
│
└── README.md
```

---

## 🛠️ Como Compilar o Projeto

### Pré-requisitos:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10 ou 11 (64-bit)

### Compilando o Aplicativo Principal:
```bash
cd src/SignalNoAr
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../../staging
```

### Empacotando e Compilando o Instalador:
```bash
python pack_payload.py
cd src/Installer
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ../../dist
```

---

## 📄 Licença e Créditos

Desenvolvido para integração com o projeto [Signal](https://signal.mathguthmann.com) por Matheus Guthmann.
