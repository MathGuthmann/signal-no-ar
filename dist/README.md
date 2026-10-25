# 📡 Signal No Ar — Display Kiosk Automatizado

Aplicativo oficial para exibir o sinal **NO AR** (`https://signal.mathguthmann.com/no-ar`) em tela cheia na inicialização do Windows, com controle de monitores e identidade visual do Signal.

---

## 🚀 Como Instalar

Dê um duplo clique no executável instalador:
👉 **`Instalador_Signal_No_Ar.exe`** (ou `Instalador_SignalNoAr.exe`)

1. A janela do instalador abrirá com o layout espaçoso e a identidade visual oficial do Signal.
2. Clique em **"Instalar Agora"**.
3. O instalador copiará os arquivos, registrará no Windows para iniciar com o computador, criará os atalhos e abrirá o assistente para você escolher o monitor.

*(Se preferir via linha de comando ou script direto, também pode executar o **`Instalar_Rapido.bat`**).*

---

## 🎯 Recursos e Funcionamento

1. **Inicialização Automática com o Windows**:
   - Inicia sozinho em segundo plano ao ligar ou reiniciar o PC (`HKCU\...\Run`).

2. **Tela Cheia no Monitor Escolhido**:
   - Abre o endereço `https://signal.mathguthmann.com/no-ar` ocupando 100% da tela do monitor configurado, sem bordas.

3. **Blindagem Contínua (Não Fecha)**:
   - Bloqueia `Alt + F4` e `Ctrl + W`.
   - Recuperação automática em caso de falha de memória ou processo.
   - Reconexão inteligente caso a internet demore a conectar na inicialização do Windows.

4. **100% Oculto da Barra de Tarefas**:
   - Funciona em modo kiosk transparente para o sistema, não poluindo a barra de tarefas do Windows.

5. **Assistente de Primeiro Uso & Identificar Telas**:
   - Assim que instalado, solicita o monitor uma única vez e memoriza a escolha.
   - Botão **"Identificar Telas"** para piscar números nos monitores físicos.

6. **Painel na Bandeja & Desativação Temporária**:
   - Ícone do Signal na bandeja do sistema (perto do relógio).
   - Clique duplo ou botão direito:
     - **Trocar de Monitor**: Altere o monitor a qualquer momento.
     - **Pausar nesta sessão (Desativar Temporariamente)**: Oculta a tela cheia para a sessão atual. Ao reiniciar o computador, ele volta a abrir automaticamente!
     - **Reativar Agora**: Restaura o sinal em tela cheia imediatamente.

7. **Desinstalação Fácil**:
   - Para remover tudo, basta rodar `Desinstalar.bat`.
