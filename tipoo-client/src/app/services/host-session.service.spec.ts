import { TestBed } from '@angular/core/testing';
import { HostSessionService } from './host-session.service';

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

describe('HostSessionService', () => {
  let service: HostSessionService;

  beforeEach(() => {
    vi.stubGlobal('localStorage', createMockStorage());
    TestBed.configureTestingModule({});
    service = TestBed.inject(HostSessionService);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should generate and persist a token on first call', () => {
    const token = service.getOrCreate();

    expect(token).toBeTruthy();
    expect(localStorage.getItem('tipoo_host_session')).toBe(token);
  });

  it('should return the same token on subsequent calls', () => {
    const first = service.getOrCreate();
    const second = service.getOrCreate();

    expect(second).toBe(first);
  });

  it('should reuse an existing token from storage', () => {
    localStorage.setItem('tipoo_host_session', 'existing-token');

    const token = service.getOrCreate();

    expect(token).toBe('existing-token');
  });
});
