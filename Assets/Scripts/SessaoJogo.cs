// Dados que persistem durante uma tentativa.
// As vidas NÃO são restauradas entre noites; a Nota é reiniciada a cada noite.
public static class SessaoJogo
{
    public const int VidasIniciais = 3;

    public static int Noite = 1;
    public static int Vidas = VidasIniciais;
    public static int Capturados;
    public static int Escapados;

    public static void Reiniciar()
    {
        Noite = 1;
        Vidas = VidasIniciais;
        Capturados = 0;
        Escapados = 0;
    }
}
