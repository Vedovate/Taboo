import { HubConnectionBuilder } from '@microsoft/signalr';
import { spawn, execSync } from 'child_process';
import { writeFileSync } from 'fs';
import path from 'path';

const artDir = 'C:\\Users\\Lvedo\\.gemini\\antigravity\\brain\\059ac086-9322-48b0-b259-0415a12611b0';
const edgePath = 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe';

async function run() {
  console.log('--- Iniciando simulação multiplayer do Tipoo ---');

  const hubUrl = 'http://localhost:5123/gamehub';
  const roomCode = 'DEMO' + Math.floor(1000 + Math.random() * 9000);

  // 1. Host / Explicador
  const connHost = new HubConnectionBuilder().withUrl(hubUrl).build();
  await connHost.start();
  await connHost.invoke('CriarSala', roomCode, 'Leonardo', 'host-sess-1');
  await connHost.invoke('EscolherTime', 'Vermelho');

  // 2. Vigia
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
  console.log(`Partida iniciada na sala ${roomCode}!`);

  // Captura 1: Tela do Explicador (Leonardo)
  const fileClue = path.join(artDir, 'view_1_cluegiver.png');
  execSync(`"${edgePath}" --headless=new --disable-gpu --virtual-time-budget=4000 --screenshot="${fileClue}" --window-size=1920,1080 "http://localhost:4200/jogo"`, {
    env: { ...process.env }
  });
  console.log('Capturada Tela 1: Explicador');

  // Simular palpites no chat
  await connGuesser.invoke('EnviarPalpite', 'Será que é papel?');
  await connGuesser.invoke('EnviarPalpite', 'Grampo de metal?');

  // Buzinar
  await connWatcher.invoke('Buzinar', 'papel', 'Palavra Proibida');
  console.log('Buzina acionada pelo fiscal!');

  // Captura 2: Modal de Explicação
  const fileExplanation = path.join(artDir, 'view_2_explanation_modal.png');
  execSync(`"${edgePath}" --headless=new --disable-gpu --virtual-time-budget=3000 --screenshot="${fileExplanation}" --window-size=1920,1080 "http://localhost:4200/jogo"`);
  console.log('Capturada Tela 2: Modal de Explicação');

  // Finalizar explicação e finalizar rodada para entrar na revisão
  await connHost.invoke('FinalizarTempoExplicacao');
  await connHost.invoke('FinalizarRodada');

  // Simular votos na revisão
  await connWatcher.invoke('VotarCarta', 0, 'aceitar');
  await connHost.invoke('VotarCarta', 0, 'reverter');

  // Captura 3: Tela de Revisão e Votação (Stopots)
  const fileReview = path.join(artDir, 'view_3_review_stopots.png');
  execSync(`"${edgePath}" --headless=new --disable-gpu --virtual-time-budget=3000 --screenshot="${fileReview}" --window-size=1920,1080 "http://localhost:4200/jogo"`);
  console.log('Capturada Tela 3: Revisão Stopots');

  // Avançar rodadas até o fim
  await connHost.invoke('AvancarRodada');
  await connHost.invoke('AvancarRodada');
  await connHost.invoke('AvancarRodada');
  await connHost.invoke('AvancarRodada');
  await connHost.invoke('AvancarRodada');
  await connHost.invoke('AvancarRodada');

  // Captura 4: Tela Final de Jogo e Pódio
  const fileGameOver = path.join(artDir, 'view_4_gameover_podium.png');
  execSync(`"${edgePath}" --headless=new --disable-gpu --virtual-time-budget=3000 --screenshot="${fileGameOver}" --window-size=1920,1080 "http://localhost:4200/jogo"`);
  console.log('Capturada Tela 4: Pódio Final');

  await connHost.stop();
  await connWatcher.stop();
  await connGuesser.stop();

  console.log('--- Simulação concluída com sucesso! ---');
}

run().catch(console.error);
