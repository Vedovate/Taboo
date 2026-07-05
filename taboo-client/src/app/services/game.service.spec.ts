import { TestBed } from '@angular/core/testing';
import { GameService } from './game.service';

const signalrMock = vi.hoisted(() => {
  const handlers = new Map<string, (...args: any[]) => void>();

  const mockConnection = {
    connectionId: 'conn1',
    state: 'Disconnected' as string,
    start: vi.fn().mockImplementation(() => {
      mockConnection.state = 'Connected';
      return Promise.resolve();
    }),
    stop: vi.fn().mockResolvedValue(undefined),
    invoke: vi.fn().mockResolvedValue(undefined),
    on: vi.fn((event: string, handler: (...args: any[]) => void) => {
      handlers.set(event, handler);
    }),
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
    onclose: vi.fn(),
  };

  return {
    mockConnection,
    triggerEvent: (event: string, ...args: any[]) => {
      const handler = handlers.get(event);
      if (handler) handler(...args);
    },
    setConnectionState: (state: string) => {
      mockConnection.state = state;
    },
    resetHandlers: () => handlers.clear(),
  };
});

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn(function () {
    return {
      withUrl: vi.fn().mockReturnThis(),
      withAutomaticReconnect: vi.fn().mockReturnThis(),
      build: vi.fn(() => signalrMock.mockConnection),
    };
  }),
  HubConnectionState: {
    Connected: 'Connected',
    Disconnected: 'Disconnected',
  },
}));

describe('GameService', () => {
  let service: GameService;

  beforeEach(() => {
    signalrMock.resetHandlers();
    signalrMock.setConnectionState('Disconnected');
    signalrMock.mockConnection.start.mockClear();
    signalrMock.mockConnection.invoke.mockClear();
    signalrMock.mockConnection.stop.mockClear();
    signalrMock.mockConnection.invoke.mockResolvedValue(undefined);
    signalrMock.mockConnection.start.mockImplementation(() => {
      signalrMock.mockConnection.state = 'Connected';
      return Promise.resolve();
    });
    signalrMock.mockConnection.stop.mockResolvedValue(undefined);

    TestBed.configureTestingModule({});
    service = TestBed.inject(GameService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('createRoom', () => {
    it('should create a room and set connected signal', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);

      await service.createRoom('ABC12', 'Player1');

      expect(service.connected()).toBe(true);
      expect(service.roomCode()).toBe('ABC12');
      expect(service.meuConnectionId()).toBe('conn1');
      expect(service.error()).toBe('');
    });

    it('should set error when room code already exists', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(false);

      await service.createRoom('ABC12', 'Player1');

      expect(service.connected()).toBe(false);
      expect(service.error()).toBeTruthy();
      expect(service.errorTimeLeft()).toBe(8);
    });

    it('should set error when input is empty', async () => {
      await service.createRoom('', '');

      expect(service.connected()).toBe(false);
      expect(service.error()).toBe('Por favor, preencha o código da sala e seu nome.');
    });

    it('should set error when invoke throws', async () => {
      signalrMock.mockConnection.invoke.mockRejectedValue(new Error('Connection failed'));

      await service.createRoom('ABC12', 'Player1');

      expect(service.error()).toBe('Connection failed');
    });

    it('should start connection if not connected', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);
      signalrMock.setConnectionState('Disconnected');

      await service.createRoom('ABC12', 'Player1');

      expect(signalrMock.mockConnection.start).toHaveBeenCalled();
    });
  });

  describe('conectar', () => {
    it('should connect to existing room and set signals', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);

      await service.conectar('ABC12', 'Player2');

      expect(service.connected()).toBe(true);
      expect(service.roomCode()).toBe('ABC12');
      expect(service.meuConnectionId()).toBe('conn1');
    });

    it('should set error when room not found', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(false);

      await service.conectar('ABC12', 'Player2');

      expect(service.connected()).toBe(false);
      expect(service.error()).toBe('Sala não encontrada. Verifique o código e tente novamente.');
    });

    it('should set error when input is empty', async () => {
      await service.conectar('', '');

      expect(service.error()).toBe('Por favor, preencha o código da sala e seu nome.');
    });

    it('should handle "already connected" error gracefully', async () => {
      signalrMock.mockConnection.invoke.mockRejectedValue(new Error('already connected'));

      await service.conectar('ABC12', 'Player1');

      expect(service.connected()).toBe(true);
    });
  });

  describe('enviarMensagem', () => {
    it('should send message via SignalR', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);
      await service.createRoom('ABC12', 'Player1');
      signalrMock.mockConnection.invoke.mockClear();

      await service.enviarMensagem('Hello!');

      expect(signalrMock.mockConnection.invoke).toHaveBeenCalledWith('EnviarMensagem', 'ABC12', 'Hello!');
    });

    it('should not send empty message', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);
      await service.createRoom('ABC12', 'Player1');
      signalrMock.mockConnection.invoke.mockClear();

      await service.enviarMensagem('');

      expect(signalrMock.mockConnection.invoke).not.toHaveBeenCalled();
    });

    it('should not send when not connected', async () => {
      await service.enviarMensagem('Hello!');

      expect(signalrMock.mockConnection.invoke).not.toHaveBeenCalled();
    });
  });

  describe('desconectar', () => {
    it('should stop connection and reset signals', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);
      await service.createRoom('ABC12', 'Player1');
      expect(service.connected()).toBe(true);

      await service.desconectar();

      expect(signalrMock.mockConnection.stop).toHaveBeenCalled();
      expect(service.connected()).toBe(false);
      expect(service.roomCode()).toBe('');
      expect(service.players()).toEqual([]);
      expect(service.messages()).toEqual([]);
      expect(service.error()).toBe('');
      expect(service.errorTimeLeft()).toBe(0);
      expect(service.meuConnectionId()).toBe('');
    });
  });

  describe('signal handlers', () => {
    it('should add message when ReceberMensagem is received', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);
      await service.createRoom('ABC12', 'Player1');

      signalrMock.triggerEvent('ReceberMensagem', 'Player1: Hello!');

      expect(service.messages()).toEqual(['Player1: Hello!']);
    });

    it('should update players when AtualizarJogadores is received', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);
      await service.conectar('ABC12', 'Player2');

      signalrMock.triggerEvent('AtualizarJogadores', [
        { connectionId: 'conn1', name: 'Player1', isHost: true },
        { connectionId: 'conn2', name: 'Player2', isHost: false },
      ]);

      expect(service.players()).toEqual([
        { connectionId: 'conn1', name: 'Player1', isHost: true },
        { connectionId: 'conn2', name: 'Player2', isHost: false },
      ]);
    });

    it('should set error and isRoomFull when SalaCheia is received', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);
      await service.createRoom('ABC12', 'Player1');

      signalrMock.triggerEvent('SalaCheia', 'A sala já está cheia.');

      expect(service.error()).toBe('A sala já está cheia.');
      expect(service.isRoomFull()).toBe(true);
    });

    it('should not override SalaCheia error with "Sala não encontrada"', async () => {
      signalrMock.mockConnection.invoke.mockImplementation(async () => {
        signalrMock.triggerEvent('SalaCheia', 'A sala já está cheia.');
        return false;
      });

      await service.conectar('ABC12', 'Player2');

      expect(service.error()).toBe('A sala já está cheia.');
      expect(service.isRoomFull()).toBe(true);
    });

    it('should auto-rename when player name matches connectionId on AtualizarJogadores', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);
      await service.createRoom('ABC12', 'Player1');

      signalrMock.triggerEvent('AtualizarJogadores', [
        { connectionId: 'conn1', name: 'conn1', isHost: true },
      ]);

      expect(signalrMock.mockConnection.invoke).toHaveBeenCalledWith('AlterarNome', 'ABC12', 'Jogador 1');
    });

    it('should not auto-rename when player name is already set', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);
      await service.createRoom('ABC12', 'Player1');

      signalrMock.triggerEvent('AtualizarJogadores', [
        { connectionId: 'conn1', name: 'Player1', isHost: true },
      ]);

      expect(signalrMock.mockConnection.invoke).not.toHaveBeenCalledWith('AlterarNome', expect.any(String), expect.any(String));
    });

    it('should compute nomeFinalizado as true when name differs from connectionId', async () => {
      signalrMock.mockConnection.invoke.mockResolvedValue(true);
      await service.createRoom('ABC12', 'Player1');

      expect(service.nomeFinalizado()).toBe(false);

      signalrMock.triggerEvent('AtualizarJogadores', [
        { connectionId: 'conn1', name: 'Player1', isHost: true },
      ]);

      expect(service.nomeFinalizado()).toBe(true);
    });

    it('should set connected false on reconnecting', () => {
      signalrMock.triggerEvent('onreconnecting');

      expect(service.connected()).toBe(false);
    });
  });

  describe('clearError', () => {
    it('should clear the error and errorTimeLeft signals', () => {
      service.error.set('Some error');
      service.errorTimeLeft.set(8);
      expect(service.error()).toBe('Some error');

      service.clearError();

      expect(service.error()).toBe('');
      expect(service.errorTimeLeft()).toBe(0);
    });
  });
});
