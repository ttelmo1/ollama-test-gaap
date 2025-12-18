using System;
using System.Collections.Generic;
using System.Text;

namespace GaapMcp.Domain
{
    public class OllamaOptions
    {
        public const string SectionName = "Ollama";

        public string Model { get; set; } = "llama3.2";
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public int TimeoutMinutes { get; set; } = 5;

        public string SystemPrompt { get; set; } = @"
        Você é um assistente útil. Regras importantes sobre ferramentas:

        1. Use ferramentas APENAS quando o usuário explicitamente solicitar a informação que a ferramenta fornece
        2. Para saudações (olá, oi, tudo bem) ou conversa casual, NUNCA use ferramentas - responda diretamente
        3. A ferramenta 'gettime' deve ser usada SOMENTE quando o usuário perguntar: ""que horas são?"", ""qual a data?"", ""me diga a hora"", etc.
        4. Se não tiver certeza, prefira NÃO usar a ferramenta e responder normalmente

        Lembre-se: ferramentas têm custo computacional. Use com sabedoria.
        ";
    }
}
