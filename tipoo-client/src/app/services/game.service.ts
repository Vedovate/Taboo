import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { LobbyPlayer } from '../models/lobby-player';
import { CardOptions } from '../models/card-options';
import { createDefaultGameSettings, GameSettings } from '../models/game-settings';
import { GameState } from '../models/game-state';
import { BuzzerEvent } from '../models/buzzer-event';
import { ChatMessage } from '../models/chat-message';
import { PlayedCard } from '../models/played-card';

import { SoundService } from './sound.service';

@Injectable({ providedIn: 'root' })
export class GameService {
  constructor(
    private router: Router,
    private soundService: SoundService,
  ) {}
  private readonly hubUrl = environment.signalR.hubUrl;
  private hubConnection?: HubConnection;
  private currentRoomCode = '';
  private userName = '';
  private errorTimeout: ReturnType<typeof setTimeout> | null = null;
  private errorCountdownInterval: ReturnType<typeof setInterval> | null = null;

  readonly error = signal<string>('');
  readonly errorTimeLeft = signal(0);
  readonly isRoomFull = signal(false);

  readonly messages = signal<string[]>([]);
  readonly connected = signal(false);
  readonly roomCode = signal('');
  readonly players = signal<LobbyPlayer[]>([]);
  readonly playerCount = computed(() => this.players().length);
  readonly meuConnectionId = signal('');
  readonly settings = signal<GameSettings>(createDefaultGameSettings());
  readonly cardOptions = signal<CardOptions>({ dificuldades: [], categorias: [] });

  // Estado do Jogo em tempo real
  readonly gameState = signal<GameState | null>(null);
  readonly activeBuzzer = computed<BuzzerEvent | null>(() => this.gameState()?.activeBuzzer ?? null);
  readonly roundCards = computed<PlayedCard[]>(() => this.gameState()?.roundCards ?? []);
  readonly chatMessages = computed<ChatMessage[]>(() => this.gameState()?.chatMessages ?? []);
  readonly endStats = computed(() => this.gameState()?.endStats ?? null);
  readonly currentCard = computed(() => this.gameState()?.currentCard ?? null);
  readonly fase = computed(() => this.gameState()?.phase ?? 'jogando');
  readonly empateSorteadoAlerta = signal(false);
  private empateTimeout: ReturnType<typeof setTimeout> | null = null;

  readonly meuJogador = computed(() =>
    this.players().find(p => p.connectionId === this.meuConnectionId())
  );

  readonly meuPapel = computed<'explicador' | 'vigia' | 'adivinhador' | 'espera'>(() => {
    const st = this.gameState();
    const me = this.meuJogador();
    if (!st || !me || !me.team) {
      return 'espera';
    }

    if (me.connectionId === st.spokespersonId) {
      return 'explicador';
    }

    if (me.team !== st.activeTeam) {
      return 'vigia';
    }

    return 'adivinhador';
  });

  readonly nomeFinalizado = computed(() => {
    const eu = this.players().find(p => p.connectionId === this.meuConnectionId());
    return eu ? eu.name !== eu.connectionId : false;
  });

  async conectar(codigoSala: string, nomeUsuario: string): Promise<void> {
    const sala = codigoSala.trim();
    const usuario = nomeUsuario.trim();

    if (!sala || !usuario) {
      this.setError('Por favor, preencha o código da sala e seu nome.');
      return;
    }

    this.clearError();
    this.isRoomFull.set(false);
    this.connected.set(false);
    this.currentRoomCode = sala;
    this.roomCode.set(sala);
    this.userName = usuario;
    this.messages.set([]);

    const connection = this.getOrCreateConnection();

    try {
      if (connection.state !== HubConnectionState.Connected) {
        await connection.start();
      }
      this.meuConnectionId.set(connection.connectionId ?? '');

      const resultado = await connection.invoke<boolean>('EntrarNaSala', sala, usuario);
      if (!resultado) {
        if (!this.isRoomFull()) {
          this.setError('Sala não encontrada. Verifique o código e tente novamente.');
        }
        return;
      }
      this.connected.set(true);
      sessionStorage.setItem('tipoo_room', sala);
      sessionStorage.setItem('tipoo_user', usuario);
    } catch (error: any) {
      console.error('Erro ao conectar:', error);
      const errorMsg = error?.message || 'Ocorreu um erro desconhecido.';
      if (!errorMsg.includes('already connected')) {
        this.setError(errorMsg);
      } else {
        this.connected.set(true);
      }
    }
  }

  async enviarMensagem(mensagem: string): Promise<void> {
    const texto = mensagem.trim();

    if (!texto || !this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) {
      return;
    }

    await this.hubConnection.invoke('EnviarMensagem', this.currentRoomCode, texto);
  }

  async desconectar(): Promise<void> {
    try {
      if (this.hubConnection) {
        await this.hubConnection.stop();
        this.hubConnection = undefined;
      }

      this.currentRoomCode = '';
      this.roomCode.set('');
      this.userName = '';
      this.connected.set(false);
      this.players.set([]);
      this.messages.set([]);
      this.meuConnectionId.set('');
      this.settings.set(createDefaultGameSettings());
      this.cardOptions.set({ dificuldades: [], categorias: [] });
      this.gameState.set(null);
      sessionStorage.removeItem('tipoo_room');

      this.error.set('');
      this.errorTimeLeft.set(0);
    } catch (error: any) {
      console.error('Erro ao desconectar:', error);
    }
  }

  async alterarNome(novoNome: string): Promise<boolean> {
    if (!this.hubConnection || !this.currentRoomCode) {
      return false;
    }
    return await this.hubConnection.invoke<boolean>('AlterarNome', this.currentRoomCode, novoNome);
  }

  async expulsarJogador(connectionIdAlvo: string): Promise<void> {
    if (!this.hubConnection || !this.currentRoomCode) {
      return;
    }
    await this.hubConnection.invoke('ExpulsarJogador', this.currentRoomCode, connectionIdAlvo);
  }

  async sairDaSala(): Promise<boolean> {
    if (!this.hubConnection || !this.currentRoomCode) {
      return false;
    }
    return await this.hubConnection.invoke<boolean>('SairDaSala', this.currentRoomCode);
  }

  async escolherTime(cor: string): Promise<boolean> {
    if (!this.hubConnection) {
      return false;
    }
    return await this.hubConnection.invoke<boolean>('EscolherTime', cor);
  }

  async alternarPronto(): Promise<boolean> {
    if (!this.hubConnection) {
      return false;
    }
    return await this.hubConnection.invoke<boolean>('AlternarPronto');
  }

  async randomizarTime(): Promise<string | null> {
    if (!this.hubConnection) {
      return null;
    }
    return await this.hubConnection.invoke<string | null>('RandomizarTime');
  }

  async forcarIniciar(): Promise<boolean> {
    if (!this.hubConnection) {
      return false;
    }
    return await this.hubConnection.invoke<boolean>('ForcarIniciar');
  }

  async createRoom(codigoSala: string, nomeUsuario: string, hostSessionId = ''): Promise<void> {
    const sala = codigoSala.trim();
    const usuario = nomeUsuario.trim();

    if (!sala || !usuario) {
      this.setError('Por favor, preencha o código da sala e seu nome.');
      return;
    }

    this.clearError();
    this.currentRoomCode = sala;
    this.roomCode.set(sala);
    this.userName = usuario;
    this.messages.set([]);

    const connection = this.getOrCreateConnection();

    try {
      if (connection.state !== HubConnectionState.Connected) {
        await connection.start();
      }
      this.meuConnectionId.set(connection.connectionId ?? '');

      const resultado = await connection.invoke<boolean>('CriarSala', sala, usuario, hostSessionId);
      if (!resultado) {
        this.setError('Já existe uma sala com esse código. Tente novamente.');
        return;
      }
      this.connected.set(true);
      sessionStorage.setItem('tipoo_room', sala);
      sessionStorage.setItem('tipoo_user', usuario);
    } catch (error: any) {
      console.error('Erro ao criar sala:', error);
      this.setError(error?.message || 'Ocorreu um erro ao criar a sala.');
    }
  }

  async configurarPartida(configuracoes: GameSettings): Promise<GameSettings | null> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) {
      return null;
    }
    try {
      const resultado = await this.hubConnection.invoke<GameSettings | null>('ConfigurarPartida', configuracoes);
      if (resultado) {
        this.settings.set(resultado);
      }
      return resultado;
    } catch (error: any) {
      console.error('Erro ao configurar partida:', error);
      return null;
    }
  }

  async obterConfiguracoes(): Promise<GameSettings | null> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) {
      return null;
    }
    return await this.hubConnection.invoke<GameSettings>('ObterConfiguracoes');
  }

  async obterOpcoesCartas(): Promise<CardOptions | null> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) {
      return null;
    }
    return await this.hubConnection.invoke<CardOptions>('ObterOpcoesCartas');
  }

  // =========================================================================
  // MÉTODOS DE JOGO EM TEMPO REAL
  // =========================================================================

  async obterEstadoJogo(): Promise<GameState | null> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) {
      return null;
    }
    try {
      const st = await this.hubConnection.invoke<GameState | null>('ObterEstadoJogo');
      if (st) {
        this.gameState.set(st);
      }
      return st;
    } catch {
      return null;
    }
  }

  async acertarCarta(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      this.soundService.tocarAcerto();
      await this.hubConnection.invoke('AcertarCarta');
    } catch (error) {
      console.error('Erro ao acertar carta:', error);
    }
  }

  async pularCarta(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      await this.hubConnection.invoke('PularCarta');
    } catch (error) {
      console.error('Erro ao pular carta:', error);
    }
  }

  async buzinar(palavraInfracao: string, tipoInfracao: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      const soundType = this.settings()?.buzzerSounds?.[0] || 'erro';
      this.soundService.tocarBuzina(soundType);
      await this.hubConnection.invoke('Buzinar', palavraInfracao, tipoInfracao);
    } catch (error) {
      console.error('Erro ao buzinar:', error);
    }
  }

  async enviarPalpite(palpite: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      await this.hubConnection.invoke('EnviarPalpite', palpite);
    } catch (error) {
      console.error('Erro ao enviar palpite:', error);
    }
  }

  async finalizarTempoExplicacao(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      await this.hubConnection.invoke('FinalizarTempoExplicacao');
    } catch (error) {
      console.error('Erro ao finalizar tempo de explicação:', error);
    }
  }

  async finalizarRodada(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      await this.hubConnection.invoke('FinalizarRodada');
    } catch (error) {
      console.error('Erro ao finalizar rodada:', error);
    }
  }

  async marcarCartaParaJulgamento(cardIndex: number, contestar: boolean): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      await this.hubConnection.invoke('MarcarCartaParaJulgamento', cardIndex, contestar);
    } catch (error) {
      console.error('Erro ao marcar carta para julgamento:', error);
    }
  }

  async confirmarSelecaoReanalise(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      await this.hubConnection.invoke('ConfirmarSelecaoReanalise');
    } catch (error) {
      console.error('Erro ao confirmar seleção de reanálise:', error);
    }
  }

  async votarJulgamentoCarta(cardIndex: number, opcaoVoto: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      await this.hubConnection.invoke('VotarJulgamentoCarta', cardIndex, opcaoVoto);
    } catch (error) {
      console.error('Erro ao votar no julgamento da carta:', error);
    }
  }

  async votarCarta(cardIndex: number, opcaoVoto: string): Promise<void> {
    await this.votarJulgamentoCarta(cardIndex, opcaoVoto);
  }

  async confirmarProntoTransicao(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      await this.hubConnection.invoke('ConfirmarProntoTransicao');
    } catch (error) {
      console.error('Erro ao confirmar pronto de transição:', error);
    }
  }

  async avancarRodada(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      await this.hubConnection.invoke('AvancarRodada');
    } catch (error) {
      console.error('Erro ao avançar rodada:', error);
    }
  }

  async reiniciarPartida(): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) return;
    try {
      await this.hubConnection.invoke('ReiniciarPartida');
    } catch (error) {
      console.error('Erro ao reiniciar partida:', error);
    }
  }

  setError(msg: string): void {
    this.clearError();
    this.error.set(msg);
    this.errorTimeLeft.set(8);
    this.errorCountdownInterval = setInterval(() => {
      this.errorTimeLeft.update(v => {
        if (v <= 1) {
          this.error.set('');
          this.errorTimeLeft.set(0);
          if (this.errorCountdownInterval) {
            clearInterval(this.errorCountdownInterval);
            this.errorCountdownInterval = null;
          }
          return 0;
        }
        return v - 1;
      });
    }, 1000);
    this.errorTimeout = setTimeout(() => {
      if (this.errorCountdownInterval) {
        clearInterval(this.errorCountdownInterval);
        this.errorCountdownInterval = null;
      }
    }, 8000);
  }

  clearError(): void {
    this.error.set('');
    this.errorTimeLeft.set(0);
    if (this.errorTimeout) {
      clearTimeout(this.errorTimeout);
      this.errorTimeout = null;
    }
    if (this.errorCountdownInterval) {
      clearInterval(this.errorCountdownInterval);
      this.errorCountdownInterval = null;
    }
  }

  private getOrCreateConnection(): HubConnection {
    if (this.hubConnection) {
      return this.hubConnection;
    }

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .withAutomaticReconnect()
      .build();

    this.registerHandlers();
    return this.hubConnection;
  }

  private registerHandlers(): void {
    if (!this.hubConnection) {
      return;
    }

    this.hubConnection.on('ReceberMensagem', (mensagem: string) => {
      this.messages.update(current => [...current, mensagem]);
    });

    this.hubConnection.on('AtualizarJogadores', (players: LobbyPlayer[]) => {
      this.players.set(players);
      const eu = players.find(p => p.connectionId === this.meuConnectionId());
      if (eu && eu.name === this.meuConnectionId()) {
        const novoNome = `Jogador ${players.length}`;
        this.alterarNome(novoNome);
      }
    });

    this.hubConnection.on('SalaCheia', (message: string) => {
      this.isRoomFull.set(true);
      this.setError(message);
    });

    this.hubConnection.on('AtualizarConfiguracoes', (settings: GameSettings) => {
      this.settings.set(settings);
    });

    this.hubConnection.on('ReceberConfiguracoes', (settings: GameSettings) => {
      this.settings.set(settings);
    });

    this.hubConnection.on('ReceberOpcoesCartas', (options: CardOptions) => {
      this.cardOptions.set(options);
    });

    this.hubConnection.on('PartidaIniciada', (estado: GameState) => {
      this.gameState.set(estado);
      this.router.navigate(['/jogo']);
    });

    this.hubConnection.on('AtualizarEstadoJogo', (estado: GameState) => {
      this.gameState.set(estado);
    });

    this.hubConnection.on('ReceberBuzina', (buzzer: BuzzerEvent) => {
      this.gameState.update(st => st ? { ...st, activeBuzzer: buzzer, phase: 'explicacao_buzina' } : null);
      const soundType = this.settings()?.buzzerSounds?.[0] || 'erro';
      this.soundService.tocarBuzina(soundType);
    });

    this.hubConnection.on('ReceberPalpite', (msg: ChatMessage) => {
      if (msg.isCorrect) {
        this.soundService.tocarAcerto();
      }
      this.gameState.update(st => {
        if (!st) return null;
        const msgs = [...st.chatMessages, msg];
        if (msgs.length > 100) msgs.shift();
        return { ...st, chatMessages: msgs };
      });
    });

    this.hubConnection.on('AtualizarVotacaoCarta', (carta: PlayedCard, empateSorteado: boolean) => {
      this.gameState.update(st => {
        if (!st) return null;
        const cards = [...st.roundCards];
        cards[carta.cardIndex] = carta;
        return { ...st, roundCards: cards };
      });

      if (empateSorteado) {
        this.empateSorteadoAlerta.set(true);
        if (this.empateTimeout) clearTimeout(this.empateTimeout);
        this.empateTimeout = setTimeout(() => this.empateSorteadoAlerta.set(false), 4500);
      }
    });

    this.hubConnection.on('FoiExpulso', async () => {
      await this.desconectar();
      this.setError('GAME.FOI_EXPULSO');
      this.router.navigate(['/']);
    });

    this.hubConnection.onreconnecting(() => {
      this.connected.set(false);
    });

    this.hubConnection.onreconnected(async () => {
      this.connected.set(true);

      if (this.currentRoomCode && this.userName) {
        await this.hubConnection?.invoke('EntrarNaSala', this.currentRoomCode, this.userName);
      }
    });

    this.hubConnection.onclose(() => {
      if (!this.connected()) {
        this.connected.set(false);
        this.messages.set([]);
        setTimeout(() => {
          this.error.set('');
        }, 2000);
      }
    });
  }
}
