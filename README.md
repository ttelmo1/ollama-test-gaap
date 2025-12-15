# ollama-test-gaap

# 1. Baixe o instalador em: https://ollama.com/download/windows
# 2. Faça o download do modelo
ollama pull qwen2.5
# Verificar versão
ollama -v
# Iniciar o serviço(Terminal separado)
ollama serve

# Verificar dotnet
dotnet --version

# Build do servidor
cd Server

dotnet build -c Release

# Build do cliente
cd ../Client

dotnet build -c Release


# Lembre-se de ajustar o caminho
// Linha onde inicia o MCP (ajuste o caminho!)

cd src/Client/Program.cs
await mcpClient.StartAsync(@"../Server/bin/Release/net10.0/Server.exe");

// Use qwen2.5 se baixou esse modelo (melhor para tool calling)

var ollamaClient = new OllamaClient("qwen2.5");

# Lembre-se de garantir que o Ollama está rodando
ollama serve

# Execute o cliente
cd Client

dotnet run


