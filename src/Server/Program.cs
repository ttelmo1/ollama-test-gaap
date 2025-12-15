using System.Text.Json;

var inputReader = Console.In;
var outputWriter = Console.Out;

var httpClient = new HttpClient();

while (true)
{
    var line = await inputReader.ReadLineAsync();
    if (string.IsNullOrWhiteSpace(line)) continue;

    try
    {
        var request = JsonSerializer.Deserialize<JsonElement>(line);
        var method = request.GetProperty("method").GetString();
        var id = request.GetProperty("id");

        if (method == "tools/list")
        {
            var response = new
            {
                jsonrpc = "2.0",
                id = id.GetInt32(),
                result = new
                {
                    tools = new object[]
                    {
                        new
                        {
                            name = "gettime",
                            description = "Obtém a data e hora atual do servidor. Use APENAS quando o usuário perguntar explicitamente sobre horário.",
                            inputSchema = new 
                            { 
                                type = "object", 
                                properties = new { }, 
                                required = new string[] { } 
                            }
                        },
                        new
                        {
                            name = "getpokemon",
                            description = "Busca informações detalhadas sobre um Pokémon específico através da PokéAPI. Retorna nome, tipo, habilidades, altura, peso e estatísticas base.",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    nome = new { type = "string", description = "Nome ou ID do Pokémon (ex: pikachu, charizard, 25)" }
                                },
                                required = new[] { "nome" }
                            }
                        },
                        new
                        {
                            name = "listpokemonsbytype",
                            description = "Lista até 10 Pokémons de um tipo específico. Tipos válidos: fire (fogo), water (água), grass (grama), electric (elétrico), ice (gelo), fighting (lutador), poison (veneno), ground (terra), flying (voador), psychic (psíquico), bug (inseto), rock (pedra), ghost (fantasma), dragon (dragão), dark (sombrio), steel (aço), fairy (fada), normal.",
                            inputSchema = new
                            {
                                type = "object",
                                properties = new
                                {
                                    tipo = new { type = "string", description = "Tipo do Pokémon em inglês (ex: fire, water, grass, electric)" }
                                },
                                required = new[] { "tipo" }
                            }
                        }
                    }
                }
            };
            await outputWriter.WriteLineAsync(JsonSerializer.Serialize(response));
        }
        else if (method == "tools/call")
        {
            var parameters = request.GetProperty("params");
            var toolName = parameters.GetProperty("name").GetString();
            
            var arguments = parameters.TryGetProperty("arguments", out var argsProp) 
                ? argsProp 
                : new JsonElement();

            string resultText;

            if (toolName == "gettime")
            {
                var now = DateTime.Now;
                resultText = $"Data e hora atual do servidor: {now:dddd, dd/MM/yyyy HH:mm:ss}";
            }
            else if (toolName == "getpokemon")
            {
                var pokemonNome = arguments.GetProperty("nome").GetString()?.ToLower() ?? "";

                try
                {
                    var apiUrl = $"https://pokeapi.co/api/v2/pokemon/{pokemonNome}";
                    
                    var apiResponse = await httpClient.GetStringAsync(apiUrl);
                    var pokemonData = JsonSerializer.Deserialize<JsonElement>(apiResponse);

                    var nome = pokemonData.GetProperty("name").GetString() ?? "desconhecido";
                    var pokemonId = pokemonData.GetProperty("id").GetInt32();
                    var altura = pokemonData.GetProperty("height").GetInt32() / 10.0;
                    var peso = pokemonData.GetProperty("weight").GetInt32() / 10.0;

                    var tipos = pokemonData.GetProperty("types").EnumerateArray()
                        .Select(t => t.GetProperty("type").GetProperty("name").GetString() ?? "")
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();

                    var habilidades = pokemonData.GetProperty("abilities").EnumerateArray()
                        .Take(3)
                        .Select(a => a.GetProperty("ability").GetProperty("name").GetString() ?? "")
                        .Where(a => !string.IsNullOrEmpty(a))
                        .ToList();

                    var stats = pokemonData.GetProperty("stats").EnumerateArray()
                        .Select(s => new
                        {
                            nome = s.GetProperty("stat").GetProperty("name").GetString() ?? "",
                            valor = s.GetProperty("base_stat").GetInt32()
                        })
                        .ToList();

                    resultText = $" Pokémon: {nome.ToUpper()} (#{pokemonId})\n\n" +
                                $" Altura: {altura}m\n" +
                                $" Peso: {peso}kg\n" +
                                $" Tipo(s): {string.Join(", ", tipos)}\n" +
                                $" Habilidades: {string.Join(", ", habilidades)}\n\n" +
                                $" Estatísticas Base:\n" +
                                $"  • HP: {stats[0].valor}\n" +
                                $"  • Ataque: {stats[1].valor}\n" +
                                $"  • Defesa: {stats[2].valor}\n" +
                                $"  • Ataque Especial: {stats[3].valor}\n" +
                                $"  • Defesa Especial: {stats[4].valor}\n" +
                                $"  • Velocidade: {stats[5].valor}";
                }
                catch (HttpRequestException)
                {
                    resultText = $" Pokémon '{pokemonNome}' não encontrado na PokéDex.";
                }
            }
            else if (toolName == "listpokemonsbytype")
            {
                var tipo = arguments.GetProperty("tipo").GetString()?.ToLower() ?? "";

                try
                {
                    var apiUrl = $"https://pokeapi.co/api/v2/type/{tipo}";
                    
                    var apiResponse = await httpClient.GetStringAsync(apiUrl);
                    var typeData = JsonSerializer.Deserialize<JsonElement>(apiResponse);

                    var pokemonList = typeData.GetProperty("pokemon").EnumerateArray()
                        .Take(10)
                        .Select((p, index) => 
                        {
                            var pokemonName = p.GetProperty("pokemon").GetProperty("name").GetString() ?? "desconhecido";
                            return $"{index + 1}. {pokemonName}";
                        })
                        .ToList();


                    resultText = $"Lista de 10 Pokémons do tipo {tipo.ToUpper()}:\n\n" +
                                string.Join("\n", pokemonList);
                }
                catch (HttpRequestException)
                {
                    resultText = $" Tipo '{tipo}' não encontrado. Tipos válidos: fire, water, grass, electric, ice, fighting, poison, ground, flying, psychic, bug, rock, ghost, dragon, dark, steel, fairy, normal.";
                }
            }
            else
            {
                resultText = $"Ferramenta '{toolName}' não encontrada.";
            }

            var response = new
            {
                jsonrpc = "2.0",
                id = id.GetInt32(),
                result = new
                {
                    content = new object[]
                    {
                        new { type = "text", text = resultText }
                    }
                }
            };

            await outputWriter.WriteLineAsync(JsonSerializer.Serialize(response));
        }
    }
    catch (Exception ex)
    {
        await Console.Error.WriteLineAsync($"Erro: {ex.Message}");
    }
}
