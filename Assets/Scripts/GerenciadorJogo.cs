using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public enum EstadoJogo { Dialogo, Jogando, Resultado, FimDeJogo, Vitoria }

// Controla todo o fluxo do jogo: noites, relógio, surgimento de invasores, câmeras, alarme e sons.
public class GerenciadorJogo : MonoBehaviour
{
    class InvasorAtivo
    {
        public TipoInvasor tipo;
        public int camera;
        public float tempoVisivel;   // só diminui enquanto a câmera dele está na tela
        public float tempoAusente;   // tempo até escapar se ninguém olhar
        public RectTransform rt;
        public Vector2 velocidade;
    }

    [Header("Ajustes de jogo")]
    public float duracaoNoite = 240f;        // 4 minutos reais = 00:00 às 07:00
    public float tempoLimiteSemOlhar = 14f;  // tempo máximo sem olhar a câmera antes de escapar
    public float intervaloAlarme = 0.35f;
    public int maxSimultaneos = 3;
    [Range(0f, 1f)] public float chanceReaparecer = 0.4f;

    [Header("Tela de jogo")]
    public RectTransform areaInvasores;
    public Image imagemComodo;
    public Sprite[] imagensComodos;
    public Text textoCamera;
    public Text textoNomeComodo;
    public Text textoRelogio;
    public Text textoNoite;
    public Text textoVidas;
    public Text textoNota;
    public Text textoAviso;
    public Image preenchimentoNota;
    public Image indicadorGravacao;
    public Image sobreposicaoFalha;
    public Image brilhoAlarme;
    public Button[] botoesCamera;
    public Image[] placasCamera;
    public Button botaoAlarme;
    public Sprite spriteInvasor;

    [Header("Painéis")]
    public GameObject painelJogo;
    public GameObject painelDialogo;
    public GameObject painelResultado;
    public GameObject painelFimDeJogo;
    public GameObject painelVitoria;

    [Header("Diálogo, resultado e fim")]
    public Button botaoDialogo;
    public Text textoFalante;
    public Text textoFala;
    public Text textoTituloResultado;
    public Text textoCorpoResultado;
    public Button botaoProximaNoite;
    public Text textoCorpoFimDeJogo;
    public Button botaoReiniciar;
    public Text textoCorpoVitoria;
    public Button botaoJogarNovamente;

    [Header("Áudio")]
    public AudioClip somAmbiente;
    public AudioClip somCliqueCamera;
    public AudioClip somAlarmeCerto;
    public AudioClip somAlarmeFalso;
    public AudioClip somInvasorEscapou;
    public AudioClip somFimDaNoite;
    public AudioClip somDerrota;
    public AudioClip somVitoria;
    public AudioClip somToqueDialogo;
    [Range(0f, 1f)] public float volumeAmbiente = 0.5f;
    [Range(0f, 1f)] public float volumeEfeitos = 0.8f;
    public float duracaoSirene = 2.5f;       // a sirene é cortada depois desse tempo

    static readonly string[] NomesComodos =
    {
        "Hall de Entrada", "Sala de Estar", "Cozinha",
        "Corredor Superior", "Quarto da Família", "Jardim / Fundos"
    };

    readonly List<InvasorAtivo> invasores = new List<InvasorAtivo>();
    EstadoJogo estado = EstadoJogo.Dialogo;
    int cameraAtual;
    float[] ultimaVez = new float[6];   // última vez que cada câmera foi observada

    float relogio;
    float temporizadorSpawn;
    int nota;
    int reaparicoes;
    int capturadosNaNoite;
    int escapadosNaNoite;
    float proximoAlarme;
    float tempoAviso;
    float temporizadorFalha;
    float duracaoFalha;

    string[] falas;
    int indiceFala;
    Action aoTerminarFalas;

    AudioSource fonteAmbiente;
    AudioSource fonteEfeitos;
    AudioSource fonteSirene;
    float fimDaSirene;

    void Awake()
    {
        SessaoJogo.Reiniciar();
        CriarFontesDeAudio();
        for (int i = 0; i < botoesCamera.Length; i++)
        {
            int indice = i;
            botoesCamera[i].onClick.AddListener(() => AoTocarCamera(indice));
        }
        botaoAlarme.onClick.AddListener(AoApertarAlarme);
        botaoDialogo.onClick.AddListener(AoTocarDialogo);
        botaoProximaNoite.onClick.AddListener(AoPassarNoite);
        botaoReiniciar.onClick.AddListener(AoReiniciar);
        botaoJogarNovamente.onClick.AddListener(AoReiniciar);
    }

    void Start()
    {
        IniciarIntroducaoDaNoite();
    }

    // ---------------------------------------------------------------- áudio

    void CriarFontesDeAudio()
    {
        fonteAmbiente = gameObject.AddComponent<AudioSource>();
        fonteAmbiente.playOnAwake = false;
        fonteAmbiente.loop = true;
        fonteAmbiente.spatialBlend = 0f;

        fonteEfeitos = gameObject.AddComponent<AudioSource>();
        fonteEfeitos.playOnAwake = false;
        fonteEfeitos.spatialBlend = 0f;

        fonteSirene = gameObject.AddComponent<AudioSource>();
        fonteSirene.playOnAwake = false;
        fonteSirene.spatialBlend = 0f;
    }

    void TocarEfeito(AudioClip clipe, float escala = 1f)
    {
        if (clipe == null) return;
        fonteEfeitos.PlayOneShot(clipe, volumeEfeitos * escala);
    }

    void TocarSirene()
    {
        if (somAlarmeCerto == null) return;
        fonteSirene.clip = somAlarmeCerto;
        fonteSirene.volume = volumeEfeitos;
        fonteSirene.Play();
        fimDaSirene = Time.time + duracaoSirene;
    }

    void PararSirene()
    {
        if (fonteSirene != null && fonteSirene.isPlaying) fonteSirene.Stop();
    }

    // O zumbido de fundo fica mais forte conforme as noites avançam
    void AtualizarAmbiente(bool tocar)
    {
        if (fonteAmbiente == null || somAmbiente == null) return;
        if (!tocar)
        {
            fonteAmbiente.Stop();
            return;
        }
        float progresso = (SessaoJogo.Noite - 1) / (float)(ConfigNoites.TotalNoites - 1);
        fonteAmbiente.volume = volumeAmbiente * Mathf.Lerp(0.6f, 1f, progresso);
        if (!fonteAmbiente.isPlaying)
        {
            fonteAmbiente.clip = somAmbiente;
            fonteAmbiente.Play();
        }
    }

    // ---------------------------------------------------------------- fluxo

    void MostrarPainel(GameObject painel)
    {
        painelJogo.SetActive(painel == painelJogo);
        painelDialogo.SetActive(painel == painelDialogo);
        painelResultado.SetActive(painel == painelResultado);
        painelFimDeJogo.SetActive(painel == painelFimDeJogo);
        painelVitoria.SetActive(painel == painelVitoria);

        bool emJogo = painel == painelJogo || painel == painelDialogo;
        AtualizarAmbiente(emJogo);
        if (!emJogo) PararSirene();
    }

    void IniciarIntroducaoDaNoite()
    {
        MostrarDialogo("SRA. HOYARI", ConfigNoites.Falas(SessaoJogo.Noite), IniciarNoite);
    }

    void MostrarDialogo(string falante, string[] linhas, Action aoTerminar)
    {
        estado = EstadoJogo.Dialogo;
        falas = linhas;
        indiceFala = 0;
        aoTerminarFalas = aoTerminar;
        textoFalante.text = falante;
        textoFala.text = linhas[0];
        MostrarPainel(painelDialogo);
    }

    void AoTocarDialogo()
    {
        if (estado != EstadoJogo.Dialogo) return;
        TocarEfeito(somToqueDialogo, 0.7f);
        indiceFala++;
        if (indiceFala >= falas.Length)
        {
            if (aoTerminarFalas != null) aoTerminarFalas();
        }
        else
        {
            textoFala.text = falas[indiceFala];
        }
    }

    void IniciarNoite()
    {
        LimparInvasores();
        StopAllCoroutines();
        relogio = 0f;
        nota = ConfigNoites.NotaMaxima;
        reaparicoes = 0;
        capturadosNaNoite = 0;
        escapadosNaNoite = 0;
        temporizadorSpawn = Random.Range(3f, 5f);
        temporizadorFalha = Random.Range(6f, 12f);
        duracaoFalha = 0f;
        for (int i = 0; i < ultimaVez.Length; i++) ultimaVez[i] = Time.time;
        textoNoite.text = "NOITE " + SessaoJogo.Noite;
        estado = EstadoJogo.Jogando;
        MostrarPainel(painelJogo);
        SelecionarCamera(0);
        AtualizarHud();
        AtualizarRelogio();
        textoAviso.text = "";
    }

    void TerminarNoite()
    {
        StopAllCoroutines();
        LimparInvasores();
        if (SessaoJogo.Noite >= ConfigNoites.TotalNoites)
        {
            estado = EstadoJogo.Vitoria;
            textoCorpoVitoria.text =
                "Você sobreviveu às sete noites.\nEstá aprovado no período de teste e efetivado no cargo!\n\n" +
                "Invasores capturados: " + SessaoJogo.Capturados + "\nInvasores escapados: " + SessaoJogo.Escapados;
            MostrarPainel(painelVitoria);
            TocarEfeito(somVitoria);
            return;
        }

        estado = EstadoJogo.Resultado;
        textoTituloResultado.text = "NOITE " + SessaoJogo.Noite + " CONCLUÍDA";
        textoCorpoResultado.text =
            "Nota da noite: " + nota + "/" + ConfigNoites.NotaMaxima + "\n" +
            "Vidas restantes: " + SessaoJogo.Vidas + "\n" +
            "Capturados: " + capturadosNaNoite + "   Escaparam: " + escapadosNaNoite + "\n\n" +
            "Sra. Hoyari: \"" + ConfigNoites.Comentario(nota) + "\"";
        MostrarPainel(painelResultado);
        TocarEfeito(somFimDaNoite);
    }

    void AoPassarNoite()
    {
        if (estado != EstadoJogo.Resultado) return;
        SessaoJogo.Noite++;
        IniciarIntroducaoDaNoite();
    }

    void FimDeJogo(string motivo)
    {
        StopAllCoroutines();
        LimparInvasores();
        estado = EstadoJogo.FimDeJogo;
        textoCorpoFimDeJogo.text =
            motivo + "\n\nVocê chegou até a noite " + SessaoJogo.Noite + ".\n" +
            "Capturados: " + SessaoJogo.Capturados + "   Escaparam: " + SessaoJogo.Escapados;
        MostrarPainel(painelFimDeJogo);
        TocarEfeito(somDerrota);
    }

    void AoReiniciar()
    {
        SessaoJogo.Reiniciar();
        IniciarIntroducaoDaNoite();
    }

    // ---------------------------------------------------------------- loop principal

    void Update()
    {
        float dt = Time.deltaTime;
        AnimarEfeitos(dt);
        if (fonteSirene != null && fonteSirene.isPlaying && Time.time >= fimDaSirene) fonteSirene.Stop();
        if (estado != EstadoJogo.Jogando) return;

        ultimaVez[cameraAtual] = Time.time;

        relogio += dt;
        AtualizarRelogio();

        temporizadorSpawn -= dt;
        if (temporizadorSpawn <= 0f)
        {
            TentarGerarInvasor();
            temporizadorSpawn = ConfigNoites.IntervaloSpawn(SessaoJogo.Noite) * Random.Range(0.8f, 1.2f);
        }

        AtualizarInvasores(dt);
        if (estado != EstadoJogo.Jogando) return;

        if (relogio >= duracaoNoite) TerminarNoite();
    }

    void AnimarEfeitos(float dt)
    {
        // Bolinha vermelha de gravação
        if (indicadorGravacao != null)
        {
            Color c = indicadorGravacao.color;
            c.a = Mathf.Sin(Time.time * 4f) > 0f ? 1f : 0.15f;
            indicadorGravacao.color = c;
        }

        // Falhas de imagem nas noites 6 e 7: a tela escurece por instantes
        if (sobreposicaoFalha != null)
        {
            Color c = sobreposicaoFalha.color;
            if (estado == EstadoJogo.Jogando && SessaoJogo.Noite >= 6)
            {
                if (duracaoFalha > 0f)
                {
                    duracaoFalha -= dt;
                    c.a = (Mathf.FloorToInt(duracaoFalha * 14f) % 2 == 0) ? 0.9f : 0.15f;
                }
                else
                {
                    c.a = 0f;
                    temporizadorFalha -= dt;
                    if (temporizadorFalha <= 0f)
                    {
                        temporizadorFalha = Random.Range(8f, 15f);
                        duracaoFalha = Random.Range(0.3f, 0.6f);
                    }
                }
            }
            else
            {
                c.a = 0f;
            }
            sobreposicaoFalha.color = c;
        }

        // Brilho do alarme
        if (brilhoAlarme != null)
        {
            Color c = brilhoAlarme.color;
            c.a = Mathf.MoveTowards(c.a, 0f, dt * 0.9f);
            brilhoAlarme.color = c;
        }

        // Mensagem rápida
        if (textoAviso != null && tempoAviso > 0f)
        {
            tempoAviso -= dt;
            Color c = textoAviso.color;
            c.a = Mathf.Clamp01(tempoAviso / 0.5f);
            textoAviso.color = c;
        }
    }

    void AtualizarRelogio()
    {
        float total = Mathf.Clamp01(relogio / duracaoNoite) * 420f; // 7 horas em minutos
        int h = (int)(total / 60f);
        int m = (int)(total % 60f);
        textoRelogio.text = string.Format("{0:00}:{1:00}", h, m);
    }

    // ---------------------------------------------------------------- câmeras

    // Toque do jogador em um botão de câmera
    void AoTocarCamera(int i)
    {
        if (estado != EstadoJogo.Jogando) return;
        if (i != cameraAtual) TocarEfeito(somCliqueCamera, 0.8f);
        SelecionarCamera(i);
    }

    void SelecionarCamera(int i)
    {
        if (estado != EstadoJogo.Jogando) return;
        cameraAtual = Mathf.Clamp(i, 0, 5);
        if (imagensComodos != null && cameraAtual < imagensComodos.Length) imagemComodo.sprite = imagensComodos[cameraAtual];
        textoCamera.text = "CAM " + (cameraAtual + 1);
        textoNomeComodo.text = NomesComodos[cameraAtual];
        for (int k = 0; k < placasCamera.Length; k++)
        {
            bool selecionada = k == cameraAtual;
            placasCamera[k].color = selecionada ? new Color(1f, 1f, 1f, 1f) : new Color(0.50f, 0.52f, 0.52f, 1f);
            placasCamera[k].transform.parent.localScale = selecionada ? new Vector3(1.07f, 1.07f, 1f) : Vector3.one;
        }
    }

    // ---------------------------------------------------------------- invasores

    void TentarGerarInvasor()
    {
        if (invasores.Count >= maxSimultaneos) return;
        TipoInvasor tipo = SortearTipo();
        int cam = SortearCamera(-1);
        if (cam < 0) return;
        CriarInvasor(tipo, cam);
    }

    TipoInvasor SortearTipo()
    {
        float total = 0f;
        foreach (TipoInvasor t in Enum.GetValues(typeof(TipoInvasor)))
            if (ConfigNoites.Liberado(t, SessaoJogo.Noite)) total += ConfigNoites.Peso(t);

        float r = Random.value * total;
        foreach (TipoInvasor t in Enum.GetValues(typeof(TipoInvasor)))
        {
            if (!ConfigNoites.Liberado(t, SessaoJogo.Noite)) continue;
            r -= ConfigNoites.Peso(t);
            if (r <= 0f) return t;
        }
        return TipoInvasor.Comum;
    }

    // Dá mais peso às câmeras que o jogador não vê há mais tempo
    int SortearCamera(int excluir)
    {
        float[] pesos = new float[6];
        float total = 0f;
        for (int c = 0; c < 6; c++)
        {
            if (c == excluir || EstaOcupada(c)) continue;
            float semOlhar = Mathf.Clamp(Time.time - ultimaVez[c], 0f, 40f);
            float peso = 1f + semOlhar;
            if (c == cameraAtual) peso *= 0.15f;
            pesos[c] = peso;
            total += peso;
        }
        if (total <= 0f) return -1;

        float r = Random.value * total;
        for (int c = 0; c < 6; c++)
        {
            if (pesos[c] <= 0f) continue;
            r -= pesos[c];
            if (r <= 0f) return c;
        }
        return -1;
    }

    bool EstaOcupada(int cam)
    {
        foreach (var inv in invasores) if (inv.camera == cam) return true;
        return false;
    }

    void CriarInvasor(TipoInvasor tipo, int cam)
    {
        var go = new GameObject("Invasor" + tipo, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(areaInvasores, false);
        var img = go.GetComponent<Image>();
        img.sprite = spriteInvasor;
        img.color = ConfigNoites.CorInvasor(tipo);
        img.preserveAspect = true;
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 180f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = PosicaoAleatoria();

        float vel = ConfigNoites.Velocidade(tipo);
        var inv = new InvasorAtivo
        {
            tipo = tipo,
            camera = cam,
            tempoVisivel = ConfigNoites.TempoVisivel(tipo),
            tempoAusente = tempoLimiteSemOlhar,
            rt = rt,
            velocidade = Random.insideUnitCircle.normalized * vel
        };
        invasores.Add(inv);
        go.SetActive(cam == cameraAtual);
    }

    // Os invasores andam apenas pela parte do chão do cômodo
    void LimitesDoChao(out float mx, out float yMin, out float yMax)
    {
        Rect r = areaInvasores.rect;
        mx = Mathf.Max(10f, r.width * 0.5f - 90f);
        yMin = -r.height * 0.5f + 105f;
        yMax = yMin + r.height * 0.42f;
    }

    Vector2 PosicaoAleatoria()
    {
        float mx, yMin, yMax;
        LimitesDoChao(out mx, out yMin, out yMax);
        return new Vector2(Random.Range(-mx, mx), Random.Range(yMin, yMax));
    }

    void AtualizarInvasores(float dt)
    {
        float mx, yMin, yMax;
        LimitesDoChao(out mx, out yMin, out yMax);

        for (int i = invasores.Count - 1; i >= 0; i--)
        {
            var inv = invasores[i];
            bool observado = inv.camera == cameraAtual;
            inv.rt.gameObject.SetActive(observado);

            if (observado)
            {
                inv.tempoVisivel -= dt;
                Vector2 p = inv.rt.anchoredPosition + inv.velocidade * dt;
                if (p.x < -mx || p.x > mx) { inv.velocidade.x = -inv.velocidade.x; p.x = Mathf.Clamp(p.x, -mx, mx); }
                if (p.y < yMin || p.y > yMax) { inv.velocidade.y = -inv.velocidade.y; p.y = Mathf.Clamp(p.y, yMin, yMax); }
                inv.rt.anchoredPosition = p;
            }
            else
            {
                inv.tempoAusente -= dt;
            }

            if (inv.tempoVisivel <= 0f || inv.tempoAusente <= 0f)
            {
                InvasorEscapou(inv);
                if (estado != EstadoJogo.Jogando) return;
            }
        }
    }

    void InvasorEscapou(InvasorAtivo inv)
    {
        int penalidade = ConfigNoites.PenalidadeFuga(inv.tipo);
        nota = Mathf.Max(0, nota - penalidade);
        SessaoJogo.Escapados++;
        escapadosNaNoite++;
        int camOrigem = inv.camera;
        TipoInvasor tipo = inv.tipo;
        RemoverInvasor(inv);

        TocarEfeito(somInvasorEscapou, 0.9f);
        MostrarAviso("Um invasor escapou!  Nota -" + penalidade);
        AtualizarHud();
        if (VerificarDerrota()) return;

        // Chance de reaparecer em outro cômodo
        if (reaparicoes < ConfigNoites.MaxReaparicoes(SessaoJogo.Noite) && Random.value < chanceReaparecer)
        {
            reaparicoes++;
            StartCoroutine(Reaparecer(tipo, camOrigem));
        }
    }

    IEnumerator Reaparecer(TipoInvasor tipo, int camOrigem)
    {
        yield return new WaitForSeconds(3f);
        if (estado != EstadoJogo.Jogando || invasores.Count >= maxSimultaneos) yield break;
        int cam = SortearCamera(camOrigem);
        if (cam >= 0) CriarInvasor(tipo, cam);
    }

    void RemoverInvasor(InvasorAtivo inv)
    {
        invasores.Remove(inv);
        if (inv.rt != null) Destroy(inv.rt.gameObject);
    }

    void LimparInvasores()
    {
        for (int i = invasores.Count - 1; i >= 0; i--)
            if (invasores[i].rt != null) Destroy(invasores[i].rt.gameObject);
        invasores.Clear();
    }

    // ---------------------------------------------------------------- alarme

    void AoApertarAlarme()
    {
        if (estado != EstadoJogo.Jogando || Time.time < proximoAlarme) return;
        proximoAlarme = Time.time + intervaloAlarme;

        InvasorAtivo alvo = null;
        foreach (var inv in invasores)
        {
            if (inv.camera == cameraAtual) { alvo = inv; break; }
        }

        if (alvo != null)
        {
            RemoverInvasor(alvo);
            SessaoJogo.Capturados++;
            capturadosNaNoite++;
            TocarSirene();
            Brilhar(new Color(0.2f, 0.9f, 0.3f, 0.35f));
            MostrarAviso("ALARME! Invasor capturado.");
        }
        else
        {
            SessaoJogo.Vidas--;
            nota = Mathf.Max(0, nota - 1);
            TocarEfeito(somAlarmeFalso);
            Brilhar(new Color(1f, 0.1f, 0.1f, 0.5f));
            MostrarAviso("ALARME FALSO!  -1 vida   Nota -1");
            AtualizarHud();
            VerificarDerrota();
        }
    }

    bool VerificarDerrota()
    {
        if (SessaoJogo.Vidas <= 0)
        {
            FimDeJogo("Suas vidas acabaram por excesso de alarmes falsos.");
            return true;
        }
        if (nota <= 0)
        {
            FimDeJogo("A sua Nota chegou a zero. Invasores demais escaparam.");
            return true;
        }
        return false;
    }

    // ---------------------------------------------------------------- HUD

    void AtualizarHud()
    {
        textoVidas.text = "x" + Mathf.Max(0, SessaoJogo.Vidas);
        textoNota.text = "NOTA " + nota + "/" + ConfigNoites.NotaMaxima;
        preenchimentoNota.fillAmount = nota / (float)ConfigNoites.NotaMaxima;
    }

    void Brilhar(Color cor)
    {
        if (brilhoAlarme != null) brilhoAlarme.color = cor;
    }

    void MostrarAviso(string mensagem)
    {
        if (textoAviso == null) return;
        textoAviso.text = mensagem;
        textoAviso.color = Color.white;
        tempoAviso = 2.2f;
    }
}
