using UnityEngine;

// Tipos de invasor do jogo
public enum TipoInvasor { Comum, Rapido, Furtivo }

// Tabela de configuração por noite e dados de cada tipo de invasor.
public static class ConfigNoites
{
    public const int TotalNoites = 7;
    public const int NotaMaxima = 10;

    // Intervalo médio entre o surgimento de invasores
    public static float IntervaloSpawn(int noite)
    {
        if (noite <= 2) return 15f;
        if (noite <= 4) return 12f;
        if (noite <= 6) return 10f;
        return 8f;
    }

    // Rápido a partir da 3ª noite, Furtivo a partir da 5ª
    public static bool Liberado(TipoInvasor tipo, int noite)
    {
        switch (tipo)
        {
            case TipoInvasor.Rapido: return noite >= 3;
            case TipoInvasor.Furtivo: return noite >= 5;
            default: return true;
        }
    }

    // Tempo que o invasor fica visível na câmera
    public static float TempoVisivel(TipoInvasor tipo)
    {
        switch (tipo)
        {
            case TipoInvasor.Rapido: return Random.Range(3f, 4f);
            case TipoInvasor.Furtivo: return Random.Range(1f, 2f);
            default: return Random.Range(6f, 8f);
        }
    }

    // Pontos de Nota perdidos quando o invasor escapa
    public static int PenalidadeFuga(TipoInvasor tipo)
    {
        switch (tipo)
        {
            case TipoInvasor.Rapido: return 3;
            case TipoInvasor.Furtivo: return 4;
            default: return 2;
        }
    }

    // Velocidade de movimento na tela
    public static float Velocidade(TipoInvasor tipo)
    {
        switch (tipo)
        {
            case TipoInvasor.Rapido: return 150f;
            case TipoInvasor.Furtivo: return 30f;
            default: return 50f;
        }
    }

    // Comum é o mais claro, Furtivo o mais escuro
    public static Color CorInvasor(TipoInvasor tipo)
    {
        switch (tipo)
        {
            case TipoInvasor.Rapido: return new Color(0.16f, 0.16f, 0.16f, 1f);
            case TipoInvasor.Furtivo: return new Color(0.07f, 0.07f, 0.07f, 1f);
            default: return new Color(0.62f, 0.60f, 0.60f, 1f);
        }
    }

    // Quantas vezes um invasor pode reaparecer por noite
    public static int MaxReaparicoes(int noite)
    {
        return Mathf.Min(4, noite);
    }

    // Peso para sortear o tipo entre os liberados
    public static float Peso(TipoInvasor tipo)
    {
        switch (tipo)
        {
            case TipoInvasor.Rapido: return 2f;
            case TipoInvasor.Furtivo: return 2f;
            default: return 3f;
        }
    }

    // Falas da Sra. Hoyari antes de cada noite
    public static string[] Falas(int noite)
    {
        switch (noite)
        {
            case 1:
                return new[]
                {
                    "Bem-vindo à mansão, vigia. Eu sou a Sra. Hoyari. Esta casa é a minha família.",
                    "Você terá sete noites de teste. Observe as câmeras e identifique qualquer movimentação suspeita.",
                    "Só acione o alarme quando tiver certeza da ameaça. Alarmes falsos custam vidas e desempenho.",
                    "Você começa com 3 vidas, e a sua Nota volta a 10 no início de cada noite. Boa sorte."
                };
            case 2:
                return new[] { "Uma noite superada. Continue atento: eles sempre voltam." };
            case 3:
                return new[]
                {
                    "Estranho... ouvi barulhos que as câmeras não registraram.",
                    "E hoje cedo encontrei portas abertas, mesmo sem nenhum alarme acionado.",
                    "Fique de olho. Alguns deles parecem se mover mais rápido."
                };
            case 4:
                return new[] { "Os barulhos continuam. Mantenha o foco, vigia." };
            case 5:
                return new[]
                {
                    "Vigia... acho que nem todos os intrusos foram capturados.",
                    "Alguns podem ainda estar escondidos dentro da própria mansão.",
                    "Hoje, cuidado com os que aparecem por poucos instantes."
                };
            case 6:
                return new[]
                {
                    "Eles estão cada vez mais rápidos.",
                    "E a imagem das câmeras tem falhado de vez em quando. Mantenha a calma."
                };
            default:
                return new[]
                {
                    "A última noite. Se passar por ela, o cargo é seu.",
                    "Não deixe nada escapar... mas não acione o alarme sem certeza."
                };
        }
    }

    // Comentário da Sra. Hoyari ao fim da noite, conforme a Nota
    public static string Comentario(int nota)
    {
        if (nota >= 9) return "Impecável. Dormi muito tranquila.";
        if (nota >= 6) return "Bom trabalho. Ainda dá para melhorar.";
        if (nota >= 3) return "Passou por pouco. Preciso de mais atenção.";
        return "Isso foi arriscado demais, vigia.";
    }
}
