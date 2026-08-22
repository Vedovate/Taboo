import { HubConnectionBuilder } from '@microsoft/signalr';
import { spawn } from 'child_process';
import http from 'http';
import WebSocket from 'ws';
import { writeFileSync } from 'fs';
import path from 'path';

const artDir = 'C:\\Users\\Lvedo\\.gemini\\antigravity\\brain\\059ac086-9322-48b0-b259-0415a12611b0';
const edgePath = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function getWsDebuggerUrl(port) {
  for (let i = 0; i < 15; i++) {
    try {
      const url = await new Promise((resolve, reject) => {
        const req = http.get(`http://127.0.0.1:${port}/json/version`, res => {
          let data = '';
          res.on('data', chunk => (data += chunk));
          res.on('end', () => {
            try {
              if (!data) return reject(new Error('Empty response'));
              const json = JSON.parse(data);
              resolve(json.webSocketDebuggerUrl);
            } catch (e) {
              reject(e);
            }
          });
        });
        req.on('error', reject);
        req.setTimeout(1000, () => req.destroy());
      });
      if (url) return url;
    } catch {
      await sleep(500);
    }
  }
  throw new Error('Não foi possível conectar ao DevTools do Edge');
}

class CdpClient {
  constructor(wsUrl) {
    this.ws = new WebSocket(wsUrl);
    this.id = 1;
    this.callbacks = new Map();
  }

  async connect() {
    return new Promise((resolve, reject) => {
      this.ws.on('open', resolve);
      this.ws.on('error', reject);
      this.ws.on('message', data => {
        const msg = JSON.parse(data.toString());
        if (msg.id && this.callbacks.has(msg.id)) {
          this.callbacks.get(msg.id)(msg.result);
          this.callbacks.delete(msg.id);
        }
      });
    });
  }

  send(method, params = {}) {
    const id = this.id++;
    return new Promise(resolve => {
      this.callbacks.set(id, resolve);
      this.ws.send(JSON.stringify({ id, method, params }));
    });
  }

  async close() {
    this.ws.close();
  }
}

async function run() {
  console.log('Iniciando Edge headless com porta 9222...');
  const tempProfile = path.join(artDir, 'scratch', 'edge-profile');
  const edgeProc = spawn(edgePath, [
    '--headless=new',
    '--disable-gpu',
    '--remote-debugging-port=9222',
    `--user-data-dir=${tempProfile}`,
    '--window-size=1920,1080',
    'about:blank'
  ]);

  const wsUrl = await getWsDebuggerUrl(9222);
  const cdp = new CdpClient(wsUrl);
  await cdp.connect();

  console.log('CDP Conectado!');
  await cdp.send('Page.enable');
  await cdp.send('Runtime.enable');

  const hubUrl = 'http://localhost:5123/gamehub';
  const roomCode = 'ROOM' + Math.floor(1000 + Math.random() * 9000);

  // 1. Host (Vermelho)
  const connHost = new HubConnectionBuilder().withUrl(hubUrl).build();
  await connHost.start();
  await connHost.invoke('CriarSala', roomCode, 'Leonardo', 'host-session-cdp');
  await connHost.invoke('EscolherTime', 'Vermelho');

  // 2. Vigia (Azul)
  const connWatcher = new HubConnectionBuilder().withUrl(hubUrl).build();
  await connWatcher.start();
  await connWatcher.invoke('EntrarNaSala', roomCode, 'Fiscal Carlos');
  await connWatcher.invoke('EscolherTime', 'Azul');

  // 3. Adivinhador
  const connGuesser = new HubConnectionBuilder().withUrl(hubUrl).build();
  await connGuesser.start();
  await connGuesser.invoke('EntrarNaSala', roomCode, 'Mariana');
  await connGuesser.invoke('EscolherTime', 'Vermelho');

  // Iniciar partida
  await connHost.invoke('ForcarIniciar');

  // Navegar para home e injetar sessionStorage para o Explicador
  await cdp.send('Page.navigate', { url: 'http://localhost:4200' });
  await sleep(1500);
  await cdp.send('Runtime.evaluate', {
    expression: `sessionStorage.setItem('tipoo_room', '${roomCode}'); sessionStorage.setItem('tipoo_user', 'Leonardo'); location.href = '/jogo';`
  });
  await sleep(2500);

  // Screenshot 1: Explicador
  let screenshot = await cdp.send('Page.captureScreenshot', { format: 'png' });
  writeFileSync(path.join(artDir, 'view_1_cluegiver.png'), Buffer.from(screenshot.data, 'base64'));
  console.log('Salva view_1_cluegiver.png');

  // Injetar sessionStorage para o Vigia (Fiscal Carlos)
  await cdp.send('Runtime.evaluate', {
    expression: `sessionStorage.setItem('tipoo_room', '${roomCode}'); sessionStorage.setItem('tipoo_user', 'Fiscal Carlos'); location.href = '/jogo';`
  });
  await sleep(2500);

  // Screenshot 2: Vigia (Fiscal com Buzinas por palavra)
  screenshot = await cdp.send('Page.captureScreenshot', { format: 'png' });
  writeFileSync(path.join(artDir, 'view_2_watcher_buzzer.png'), Buffer.from(screenshot.data, 'base64'));
  console.log('Salva view_2_watcher_buzzer.png');

  // Injetar sessionStorage para a Adivinhadora (Mariana)
  await cdp.send('Runtime.evaluate', {
    expression: `sessionStorage.setItem('tipoo_room', '${roomCode}'); sessionStorage.setItem('tipoo_user', 'Mariana'); location.href = '/jogo';`
  });
  await sleep(2500);

  // Screenshot 3: Adivinhadores (Guesser com Chat)
  screenshot = await cdp.send('Page.captureScreenshot', { format: 'png' });
  writeFileSync(path.join(artDir, 'view_3_guesser_chat.png'), Buffer.from(screenshot.data, 'base64'));
  console.log('Salva view_3_guesser_chat.png');

  // Acionar buzina
  await connWatcher.invoke('Buzinar', 'papel', 'Palavra Proibida');
  await sleep(1500);

  // Screenshot 4: Modal de Explicação pós-buzina
  screenshot = await cdp.send('Page.captureScreenshot', { format: 'png' });
  writeFileSync(path.join(artDir, 'view_4_explanation_modal.png'), Buffer.from(screenshot.data, 'base64'));
  console.log('Salva view_4_explanation_modal.png');

  // Ir para a fase de revisão
  await connHost.invoke('FinalizarTempoExplicacao');
  await connHost.invoke('FinalizarRodada');
  await connWatcher.invoke('VotarCarta', 0, 'aceitar');
  await connHost.invoke('VotarCarta', 0, 'reverter');
  await sleep(1500);

  // Screenshot 5: Tela de Revisão e Votação (Stopots)
  screenshot = await cdp.send('Page.captureScreenshot', { format: 'png' });
  writeFileSync(path.join(artDir, 'view_5_review_stopots.png'), Buffer.from(screenshot.data, 'base64'));
  console.log('Salva view_5_review_stopots.png');

  // Finalizar partida para tela de pódio
  await connHost.invoke('AvancarRodada');
  await connHost.invoke('AvancarRodada');
  await connHost.invoke('AvancarRodada');
  await connHost.invoke('AvancarRodada');
  await connHost.invoke('AvancarRodada');
  await connHost.invoke('AvancarRodada');
  await sleep(1500);

  // Screenshot 6: Tela Final & Pódio
  screenshot = await cdp.send('Page.captureScreenshot', { format: 'png' });
  writeFileSync(path.join(artDir, 'view_6_gameover_podium.png'), Buffer.from(screenshot.data, 'base64'));
  console.log('Salva view_6_gameover_podium.png');

  await cdp.close();
  edgeProc.kill();
  await connHost.stop();
  await connWatcher.stop();
  await connGuesser.stop();

  console.log('Todas as 6 capturas concluídas com sucesso!');
}

run().catch(console.error);
