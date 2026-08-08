-- Criar tabela de Cartas adaptada ao seu modelo
CREATE TABLE IF NOT EXISTS Cards (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MainWord TEXT NOT NULL,
    Forbidden1 TEXT NOT NULL,
    Forbidden2 TEXT NOT NULL,
    Forbidden3 TEXT NOT NULL,
    Forbidden4 TEXT NOT NULL,
    Forbidden5 TEXT NOT NULL,
    Difficulty TEXT NOT NULL,
    Category TEXT NOT NULL
);

-- Criar tabela de Histórico atrelada ao Navegador do Host (Cache)
CREATE TABLE IF NOT EXISTS GameHostHistory (
    HostSessionId TEXT NOT NULL,
    CardId INTEGER NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (HostSessionId, CardId),
    FOREIGN KEY (CardId) REFERENCES Cards(Id) ON DELETE CASCADE
);

-- Criar tabela de Configurações da Partida atrelada ao Navegador do Host (Cache)
CREATE TABLE IF NOT EXISTS GameHostSettings (
    HostSessionId TEXT PRIMARY KEY,
    SettingsJson TEXT NOT NULL,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Criar tabela de Partidas (estatísticas). A chave é o código da sala + data/hora de início.
-- O registro é criado ao INICIAR a partida e atualizado ao ENCERRAR.
CREATE TABLE IF NOT EXISTS Matches (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MatchKey TEXT NOT NULL UNIQUE,
    RoomCode TEXT NOT NULL,
    HostSessionId TEXT,
    StartedAt DATETIME NOT NULL,
    SettingsJson TEXT NOT NULL,
    StartedPlayers INTEGER NOT NULL,
    WasStarted INTEGER NOT NULL DEFAULT 1,
    Completed INTEGER NOT NULL DEFAULT 0,
    EndedAt DATETIME,
    FinishedPlayers INTEGER,
    FinalScoreRed INTEGER,
    FinalScoreBlue INTEGER,
    WinnerTeam TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Inserir as suas cartas oficiais
INSERT OR IGNORE INTO Cards (MainWord, Forbidden1, Forbidden2, Forbidden3, Forbidden4, Forbidden5, Difficulty, Category) VALUES
('CLIPE', 'papel', 'escritório', 'grampo', 'metal', 'junto', 'Fácil', 'Objeto'),
('PASTA', 'trabalho', 'papéis', 'negócios', 'carregar', 'executivo', 'Fácil', 'Objeto'),
('ÂNCORA', 'navio', 'barco', 'noticiário', 'jogar', 'içar', 'Fácil', 'Objeto'),
('INTELIGENTE', 'burro', 'esperto', 'intelectual', 'brilhante', 'estúpido', 'Médio', 'Adjetivo'),
('SOFTWARE', 'programa', 'computador', 'instalar', 'disquete/CD-ROM', 'linguagem', 'Fácil', 'Tecnologia'),
('MARACUJÁ', 'rugas', 'azedo', 'semente', 'fruta', 'amarelo', 'Fácil', 'Alimento'),
('PRISÃO', 'cadeia', 'grades', 'cárcere', 'cela', 'criminoso', 'Fácil', 'Local'),
('ROXO', 'cor', 'azul', 'violeta', 'raiva', 'lavanda', 'Fácil', 'Cor'),
('AUSTRÁLIA', 'canguru', 'Sidnei', 'coala', 'Crocodilo Dundee', 'Oceania', 'Médio', 'Geografia'),
('TAPA', 'cabeça', 'costas', 'olho', 'mão', 'briga', 'Fácil', 'Ação'),
('BILHAR', 'mesa', 'jogo', 'caçapa', 'bola', 'taco', 'Fácil', 'Esporte/Jogo'),
('CHAMPANHE', 'vinho', 'bolhas', 'rolha', 'brinde', 'Dom Perignon', 'Fácil', 'Bebida'),
('CARATÊ', 'chute', 'artes marciais', 'faixa', 'mão', 'Kid', 'Fácil', 'Esporte'),
('CENÁRIO', 'vista', 'beleza', 'panorama', 'paisagem', 'mudança', 'Médio', 'Conceito'),
('VAGÃO', 'engate', 'estação', 'puxar', 'trem', 'trilho', 'Fácil', 'Veículo'),
('CORTADOR DE GRAMA', 'jardim', 'cortar', 'aparar', 'grama/mato', 'verde', 'Fácil', 'Objeto'),
('ALFABETO', 'letras', 'abc', 'escola', 'alfabetização', 'vogais', 'Difícil', 'Conceito'),
('GASTRONOMIA', 'culinária', 'chef', 'restaurante', 'prato', 'receita', 'Difícil', 'Conceito');


