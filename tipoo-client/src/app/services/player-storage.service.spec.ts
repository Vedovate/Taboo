import { TestBed } from '@angular/core/testing';
import { PlayerStorageService } from './player-storage.service';

function createMockStorage(): Storage {
  let store: Record<string, string> = {};
  return {
    getItem: vi.fn((key: string) => store[key] ?? null),
    setItem: vi.fn((key: string, value: string) => { store[key] = value; }),
    removeItem: vi.fn((key: string) => { delete store[key]; }),
    clear: vi.fn(() => { store = {}; }),
    get length() { return Object.keys(store).length; },
    key: vi.fn((index: number) => Object.keys(store)[index] ?? null),
  };
}

describe('PlayerStorageService', () => {
  let service: PlayerStorageService;
  let mockStorage: Storage;

  beforeEach(() => {
    mockStorage = createMockStorage();
    vi.stubGlobal('localStorage', mockStorage);
    TestBed.configureTestingModule({});
    service = TestBed.inject(PlayerStorageService);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should save and load a name', () => {
    service.saveName('Player1');
    expect(service.loadName()).toBe('Player1');
  });

  it('should return null when no name is stored', () => {
    expect(service.loadName()).toBeNull();
  });

  it('should overwrite an existing name', () => {
    service.saveName('First');
    service.saveName('Second');
    expect(service.loadName()).toBe('Second');
  });

  it('should not throw if localStorage is unavailable', () => {
    mockStorage.getItem = vi.fn(() => { throw new Error('unavailable'); });
    mockStorage.setItem = vi.fn(() => { throw new Error('unavailable'); });

    expect(() => service.saveName('Test')).not.toThrow();
    expect(service.loadName()).toBeNull();
  });
});
