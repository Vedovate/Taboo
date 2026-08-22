import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { GameService } from '../services/game.service';

export const gameGuard: CanActivateFn = async () => {
  const gameService = inject(GameService);
  const router = inject(Router);

  if (gameService.connected() && gameService.roomCode()) {
    return true;
  }

  const savedRoom = sessionStorage.getItem('tipoo_room');
  const savedUser = sessionStorage.getItem('tipoo_user');

  if (savedRoom && savedUser) {
    try {
      await gameService.conectar(savedRoom, savedUser);
      if (gameService.connected()) {
        await gameService.obterEstadoJogo();
        return true;
      }
    } catch {
      // Reconnect failed
    }
  }

  return router.createUrlTree(['/']);
};
