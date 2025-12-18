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
            await HandleToolsListAsync(id, outputWriter);
        }
        else if (method == "tools/call")
        {
            await HandleToolsCallAsync(request, id, outputWriter, httpClient);
        }
    }
    catch (Exception ex)
    {
        await Console.Error.WriteLineAsync($"Erro: {ex.Message}");
    }
}

static async Task HandleToolsListAsync(JsonElement id, TextWriter outputWriter)
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
                    description = "Busca informações detalhadas sobre um Pokémon específico através da PokéAPI.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            nome = new { type = "string", description = "Nome ou ID do Pokémon" }
                        },
                        required = new[] { "nome" }
                    }
                },
                new
                {
                    name = "listpokemonsbytype",
                    description = "Lista até 10 Pokémons de um tipo específico.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            tipo = new { type = "string", description = "Tipo do Pokémon em inglês" }
                        },
                        required = new[] { "tipo" }
                    }
                }
            }
        }
    };

    await outputWriter.WriteLineAsync(JsonSerializer.Serialize(response));
}

static async Task HandleToolsCallAsync(
    JsonElement request,
    JsonElement id,
    TextWriter outputWriter,
    HttpClient httpClient)
{
    var parameters = request.GetProperty("params");
    var toolName = parameters.GetProperty("name").GetString();
    var arguments = parameters.TryGetProperty("arguments", out var argsProp)
        ? argsProp
        : new JsonElement();

    string resultText = toolName switch
    {
        "gettime" => GetTime(),
        "getpokemon" => await GetPokemonAsync(arguments, httpClient),
        "listpokemonsbytype" => await ListPokemonsByTypeAsync(arguments, httpClient),
        _ => $"Ferramenta '{toolName}' não encontrada."
    };

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

static string GetTime()
{
    var now = DateTime.Now;
    return $"Data e hora atual do servidor: {now:dddd, dd/MM/yyyy HH:mm:ss}";
}

static async Task<string> GetPokemonAsync(JsonElement arguments, HttpClient httpClient)
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

        return $"Pokémon: {nome.ToUpper()} (#{pokemonId})\n\n" +
               $"Altura: {altura}m\n" +
               $"Peso: {peso}kg\n" +
               $"Tipo(s): {string.Join(", ", tipos)}\n" +
               $"Habilidades: {string.Join(", ", habilidades)}\n\n" +
               $"Estatísticas Base:\n" +
               $"  • HP: {stats[0].valor}\n" +
               $"  • Ataque: {stats[1].valor}\n" +
               $"  • Defesa: {stats[2].valor}\n" +
               $"  • Ataque Especial: {stats[3].valor}\n" +
               $"  • Defesa Especial: {stats[4].valor}\n" +
               $"  • Velocidade: {stats[5].valor}";
    }
    catch (HttpRequestException)
    {
        return $"❌ Pokémon '{pokemonNome}' não encontrado na PokéDex.";
    }
}

static async Task<string> ListPokemonsByTypeAsync(JsonElement arguments, HttpClient httpClient)
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

        return $"Lista de 10 Pokémons do tipo {tipo.ToUpper()}:\n\n" +
               string.Join("\n", pokemonList);
    }
    catch (HttpRequestException)
    {
        return $"❌ Tipo '{tipo}' não encontrado.";
    }
}
