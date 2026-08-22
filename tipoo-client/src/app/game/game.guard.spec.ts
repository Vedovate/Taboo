import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { gameGuard } from './game.guard';
import { GameService } from '../services/game.service';
import { signal } from '@angular/core';

describe('gameGuard', () => {
  let mockGameService: {
    connected: ReturnType<typeof signal<boolean>>;
    roomCode: ReturnType<typeof signal<string>>;
    conectar: (room: string, user: string) => Promise<void>;
    obterEstadoJogo: () => Promise<any>;
  };
  let mockRouter: {
    createUrlTree: any;
  };

  beforeEach(() => {
    mockGameService = {
      connected: signal(false),
      roomCode: signal(''),
      conectar: vi.fn().mockResolvedValue(undefined),
      obterEstadoJogo: vi.fn().mockResolvedValue(undefined),
    };

    mockRouter = {
      createUrlTree: vi.fn().mockReturnValue('/redirect'),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: GameService, useValue: mockGameService },
        { provide: Router, useValue: mockRouter },
      ],
    });

    sessionStorage.clear();
  });

  it('should allow navigation if already connected and in a room', async () => {
    mockGameService.connected.set(true);
    mockGameService.roomCode.set('ABC12');

    const result = await TestBed.runInInjectionContext(() => gameGuard({} as any, {} as any));
    expect(result).toBe(true);
  });

  it('should attempt auto-reconnect if sessionStorage has room and user', async () => {
    sessionStorage.setItem('tipoo_room', 'ABC12');
    sessionStorage.setItem('tipoo_user', 'João');

    mockGameService.conectar = vi.fn().mockImplementation(async () => {
      mockGameService.connected.set(true);
    });

    const result = await TestBed.runInInjectionContext(() => gameGuard({} as any, {} as any));
    expect(mockGameService.conectar).toHaveBeenCalledWith('ABC12', 'João');
    expect(result).toBe(true);
  });

  it('should redirect to home if not connected and no sessionStorage', async () => {
    const result = await TestBed.runInInjectionContext(() => gameGuard({} as any, {} as any));
    expect(mockRouter.createUrlTree).toHaveBeenCalledWith(['/']);
    expect(result).toBe('/redirect');
  });
});
