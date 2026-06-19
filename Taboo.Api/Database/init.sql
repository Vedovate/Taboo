-- Criar tabela de Cartas adaptada ao seu modelo
CREATE TABLE IF NOT EXISTS Cards (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MainWord TEXT NOT NULL,
    Taboo1 TEXT NOT NULL,
    Taboo2 TEXT NOT NULL,
    Taboo3 TEXT NOT NULL,
    Taboo4 TEXT NOT NULL,
    Taboo5 TEXT NOT NULL,
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

-- Inserir as suas cartas oficiais
INSERT OR IGNORE INTO Cards (MainWord, Taboo1, Taboo2, Taboo3, Taboo4, Taboo5, Difficulty, Category) VALUES
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
('CORTADOR DE GRAMA', 'jardim', 'cortar', 'aparar', 'grama/mato', 'verde', 'Fácil', 'Objeto');


