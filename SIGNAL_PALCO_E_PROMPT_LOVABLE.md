# 🎤 Signal Palco — Monitor de Retorno Inteligente com Raspberry Pi 3

> **Documento de Especificação Técnica, Arquitetura de Hardware e Prompt de Implementação para a Lovable.**

---

## 📌 1. Visão Geral do Projeto

O **Signal Palco** é uma solução de **Monitor de Retorno (Confidence Monitor / Stage Display)** projetada para igrejas e estúdios de transmissão ao vivo. 

Em vez de usar computadores caros ou monitores estáticos no palco, o sistema utiliza um **Raspberry Pi 3** conectado diretamente via HDMI a uma TV ou monitor no chão do palco/púlpito. Ele opera como um **Appliance Dedicado**: liga direto na tomada, entra em tela cheia instantaneamente e alterna de forma inteligente entre **letras de louvor/Bíblia** e um **painel informativo de palco com relógio e cronômetro**.

---

## 🎯 2. O Desafio e a Solução Inteligente

### ❌ O Problema Tradicional
Em cultos e eventos, o monitor de palco costuma ficar:
- **Preto ou congelado** nos momentos em que o louvor não está cantando (durante orações, pregação, avisos e transições);
- Dependente de computadores convencionais no palco, sujeitos a atualizações indesejadas do Windows, travamentos ou cabos desconectados;
- Com interfaces poluídas ou fontes pequenas que dificultam a leitura dos cantores e ministrantes a 5–15 metros de distância.

### ✅ A Solução Signal Palco
Uma única página web responsiva (`/palco`) integrada ao **Signal**, orquestrada para exibir dois estados dinâmicos com transições fluidas:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                            ESTADOS DA TELA /PALCO                           │
├──────────────────────────────────────┬──────────────────────────────────────┤
│ MODO 1: LOUVOR / BÍBLIA ATIVA        │ MODO 2: REPOUSO / PREGAÇÃO / CRONÔ   │
│ (Quando o software projeta letra)    │ (Quando a projeção está vazia)       │
├──────────────────────────────────────┼──────────────────────────────────────┤
│ • Estrofe atual com tipografia       │ • Relógio digital gigante            │
│   gigante e alto contraste          │   (HH:MM:SS) em tempo real           │
│ • Linha da próxima estrofe (preview) │ • Cronômetro de pregação             │
│   em tamanho menor com opacidade 60% │   (contagem regressiva / progresso)  │
│ • Barra de status discreta no topo:  │ • Alerta de NO AR / Transmissão      │
│   horário atual + indicador AO VIVO  │ • Avisos rápidos da técnica          │
│ • Transição fade suave ao trocar     │   (Ex: "5 MINUTOS", "CORTAR MIC 2")  │
└──────────────────────────────────────┴──────────────────────────────────────┘
```

---

## 🖥️ 3. Arquitetura de Hardware (Raspberry Pi 3 Appliance)

O Raspberry Pi 3 Model B (com 1GB de RAM) foi escolhido por ser acessível, compacto, silencioso e poder ficar embutido atrás da TV do palco.

```
                    ┌────────────────────────┐
                    │      ROTEADOR / REDE   │
                    └───┬────────────────┬───┘
          Cabo RJ45     │                │ Wi-Fi Backup
          (Métrica 100) │                │ (Métrica 200)
                        ▼                ▼
             ┌──────────────────────────────────────┐
             │       Raspberry Pi 3 (Appliance)     │
             │  • SO: Linux DietPi / Pi OS Lite     │
             │  • Kiosk: Chromium Wayland/Cage      │
             │  • Disco: OverlayFS (Read-Only)      │
             │  • Acesso Remoto: Tailscale VPN      │
             └──────────────────┬───────────────────┘
                                │ Cabo HDMI
                                ▼
                     ┌──────────────────────┐
                     │ TV / MONITOR DE PALCO│
                     │  signal.mathguthmann │
                     │       .com/palco     │
                     └──────────────────────┘
```

### Destaques da Infraestrutura:
1. **Rede Híbrida com Failover Instantâneo**:
   - O cabo de rede (`eth0`) é a rota principal.
   - A placa Wi-Fi (`wlan0`) fica conectada como rota de contingência. Se alguém tropeçar no cabo de rede, a conexão comuta para o Wi-Fi sem interromper o WebSocket ou a projeção.
2. **Blindagem contra Queda de Energia (Read-Only / OverlayFS)**:
   - O cartão MicroSD opera em modo somente leitura. O equipamento pode ser desligado puxando a tomada ou desligando o disjuntor da igreja sem qualquer risco de corromper o sistema operacional.
3. **Acesso Remoto Global via Tailscale**:
   - Permite que você acerte configurações ou visualize o status do terminal via SSH de qualquer lugar (em casa ou pelo celular), sem abrir portas no roteador da igreja.
4. **Sem Manutenção no Palco**:
   - Como a lógica visual roda na nuvem/Lovable no Signal, qualquer atualização no design ou nas regras do palco é aplicada instantaneamente assim que o deploy é feito.

---

## 💬 4. Prompt para a Lovable (Copiar e Colar)

Copie o bloco de texto abaixo e envie diretamente no chat da **Lovable** dentro do projeto do Signal (`media-hub-hq`):

```markdown
Crie uma nova rota chamada `/palco` (Monitor de Retorno / Stage Display) desenvolvida especificamente para telas de retorno de palco e TVs de púlpito.

### 🎨 Identidade Visual e Ergonomia de Palco:
- Fundo ultra-escuro (Deep Dark `#0A090F` ou `#000000`) para evitar vazamento excessivo de luz branca no rosto de quem está no palco.
- Tipografia moderna, negrito, de alta legibilidade a longa distância (Inter / Roboto / Outfit), com letras grandes e espaçamento confortável.
- Acentos nas cores oficiais do Signal: Laranja vibrante `#FF7A00` e Roxo `#7B2CBF`.
- Tela travada em 100vh sem nenhuma barra de rolagem (`overflow-hidden`, `select-none`).
- Atalho de teclado: Tecla `F` alterna tela cheia (Fullscreen), tecla `S` abre modal de configurações/teste.

---

### 🔄 Lógica de Estados da Tela:

A página deve alternar suavemente com animação de Fade (`framer-motion` ou transições CSS de 300ms) entre dois modos:

#### 1. MODO LOUVOR / BÍBLIA (Quando houver texto ativo recebido):
- **Área Central (Destaque Principal)**:
  - Exibe a estrofe/verso atual com tamanho de fonte dinâmico e auto-ajustável para preencher a tela harmoniosamente em letras grandes e brancas com alto contraste.
- **Área Inferior (Próxima Linha / Preview)**:
  - Exibe a próxima estrofe ou frase seguinte com fonte ligeiramente menor, cor cinza/âmbar suave e opacidade de 60%, permitindo que os cantores e ministrantes antecipem a letra.
- **Barra Superior Discreta**:
  - Relógio atual compacto no canto direito (ex: `19:42`).
  - Badge no canto esquerdo com o status do culto ou indicador "AO VIVO" pulsante.

#### 2. MODO REPOUSO / INFORMATIVO DE PALCO (Quando NÃO houver letra ativa ou texto for vazio):
- Quando a projeção de letras for limpa (ex: durante pregação, orações, avisos ou introduções), a tela transiciona automaticamente para o painel de palco:
  1. **Relógio Digital Gigante**:
     - Hora atual com segundos em tamanho grande centralizado (ex: `19:45:20`).
  2. **Cronômetro de Mensagem / Culto**:
     - Temporizador de contagem regressiva ou progressiva com minutos e segundos (ex: `24:15 restante`).
     - Alerta visual suave quando o tempo estiver acabando (fica amarelo em 5 min e vermelho piscando em 00:00).
  3. **Banner de Status do Signal**:
     - Indicador se a transmissão está "NO AR" (Live Streaming ativo) ou "OFFLINE / PREPARAÇÃO".
  4. **Painel de Avisos Rápidos da Técnica**:
     - Área para mensagens de texto curtas enviadas pela equipe de som/corte (ex: "Favor liberar o púlpito", "Microfone 3 sem pilha").

---

### 🔌 Fontes de Dados e Integração:
1. **Recebimento de Letras**:
   - Crie um hook/contexto preparado para receber texto de letras via WebSocket ou Webhook/Polling de API (compatível com Holyrics / ProPresenter / Web Renderer).
   - Inclua suporte a parâmetros na URL para teste ou configuração direta (ex: `/palco?preview=true` ou `/palco?clock=true`).
2. **Modo de Demonstração / Simulador (para testar sem o Holyrics ligado)**:
   - No modal de configurações (acessível clicando num ícone discreto no canto ou apertando `S`), adicione botões para:
     - "Simular Letra de Louvor"
     - "Simular Versículo Bíblico"
     - "Limpar Letra (Ir para Modo Relógio/Palco)"
     - "Ajustar Tempo do Cronômetro"

Garanta que o visual seja moderno, impecável, com animações suaves e sem nenhum travamento na renderização!
```

---

## 📋 5. Próximos Passos de Implementação

1. **Passo 1**: Cole o prompt acima na **Lovable** e aguarde a geração da página `/palco`.
2. **Passo 2**: Teste a rota `/palco` no seu navegador no computador.
3. **Passo 3**: Assim que a rota estiver publicada na Vercel/Signal, geramos a imagem do cartão MicroSD para o seu **Raspberry Pi 3** apontando diretamente para `https://signal.mathguthmann.com/palco`.
