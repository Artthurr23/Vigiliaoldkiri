#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu: Vigilia > Construir Cena
// Gera os sprites e os cômodos e monta toda a hierarquia do jogo na cena ativa.
public static class ConstrutorCena
{
    const string PastaSprites = "Assets/Sprites";
    const string PastaComodos = "Assets/Sprites/Comodos";
    const string CaminhoBotoes = "Assets/Botoes/New_Piskel-removebg-preview.png";
    static Font fonteCache;

    [MenuItem("Vigilia/Construir Cena")]
    public static void Construir()
    {
        if (!AssetDatabase.IsValidFolder(PastaSprites)) AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder(PastaComodos)) AssetDatabase.CreateFolder(PastaSprites, "Comodos");

        Sprite invasor = SalvarSprite(CriarInvasor(), PastaSprites + "/Invasor.png", true, FilterMode.Point);
        Sprite alarme = SalvarSprite(CriarBotaoAlarme(), PastaSprites + "/BotaoAlarme.png", true, FilterMode.Point);
        Sprite moeda = SalvarSprite(CriarMoeda(), PastaSprites + "/Moeda.png", true, FilterMode.Point);
        Sprite pixel = SalvarSprite(CriarPixel(), PastaSprites + "/Pixel.png", true, FilterMode.Point);
        Sprite circulo = SalvarSprite(CriarCirculo(), PastaSprites + "/Circulo.png", true, FilterMode.Bilinear);

        var comodos = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            Tela tela = Pintor.Gerar(i);
            comodos[i] = SalvarSprite(TelaParaTextura(tela), PastaComodos + "/Comodo" + (i + 1) + ".png", true, FilterMode.Bilinear);
        }

        Sprite[] desenhosBotoes = FatiarBotoes();
        if (desenhosBotoes == null) Debug.LogWarning("[Vigilia] Folha de botões não encontrada em " + CaminhoBotoes);

        Scene cena = SceneManager.GetActiveScene();
        foreach (GameObject raiz in cena.GetRootGameObjects()) Object.DestroyImmediate(raiz);

        // Câmera
        var camGo = new GameObject("Camera Principal");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        new GameObject("SistemaEventos", typeof(EventSystem), typeof(StandaloneInputModule));

        // Canvas
        var telaGo = new GameObject("Tela", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = telaGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escala = telaGo.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escala.matchWidthOrHeight = 0.5f;

        // ---------------- Painel do jogo
        RectTransform painelJogo = NovoRect("PainelJogo", telaGo.transform);
        Esticar(painelJogo);

        Image fundo = NovaImagem("FundoMansao", painelJogo, new Color(0.20f, 0.22f, 0.21f, 1f));
        fundo.sprite = comodos[0];
        Esticar(fundo.rectTransform);
        fundo.raycastTarget = false;

        // moldura branca (fica atrás da imagem da câmera)
        Image moldura = NovaImagem("MolduraCamera", painelJogo, new Color(0.92f, 0.92f, 0.92f, 1f));
        Posicionar(moldura.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(350f, -272f), new Vector2(1212f, 666f));
        moldura.raycastTarget = false;

        RectTransform telaCamera = NovoRect("TelaCamera", painelJogo);
        Posicionar(telaCamera, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(356f, -278f), new Vector2(1200f, 654f));

        Image imagemComodo = NovaImagem("ImagemComodo", telaCamera, Color.white);
        imagemComodo.sprite = comodos[0];
        Esticar(imagemComodo.rectTransform);
        imagemComodo.raycastTarget = false;

        RectTransform areaInvasores = NovoRect("AreaInvasores", telaCamera);
        Esticar(areaInvasores);

        Image falha = NovaImagem("SobreposicaoFalha", telaCamera, new Color(0f, 0f, 0f, 0f));
        Esticar(falha.rectTransform);
        falha.raycastTarget = false;

        Image brilho = NovaImagem("BrilhoAlarme", telaCamera, new Color(1f, 0f, 0f, 0f));
        Esticar(brilho.rectTransform);
        brilho.raycastTarget = false;

        Text textoCamera = NovoTexto("TextoCamera", telaCamera, "CAM 1", 56, TextAnchor.MiddleLeft, Color.white);
        Posicionar(textoCamera.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -18f), new Vector2(500f, 70f));
        Text textoNomeComodo = NovoTexto("TextoNomeComodo", telaCamera, "Hall de Entrada", 30, TextAnchor.MiddleLeft, new Color(0.85f, 0.9f, 0.85f));
        Posicionar(textoNomeComodo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -84f), new Vector2(700f, 40f));

        Image gravacao = NovaImagem("IndicadorGravacao", telaCamera, new Color(0.9f, 0.05f, 0.05f, 1f));
        gravacao.sprite = circulo;
        gravacao.raycastTarget = false;
        Posicionar(gravacao.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-34f, -34f), new Vector2(30f, 30f));

        // Botões de câmera (coluna à esquerda)
        RectTransform botoesCamera = NovoRect("BotoesCamera", painelJogo);
        Esticar(botoesCamera);
        var botoes = new Button[6];
        var placas = new Image[6];
        for (int i = 0; i < 6; i++)
        {
            Image area = NovaImagem("BotaoCamera" + (i + 1), botoesCamera, new Color(0f, 0f, 0f, 0f));
            Posicionar(area.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(75f, -(278f + i * 112f)), new Vector2(180f, 100f));
            var botao = area.gameObject.AddComponent<Button>();
            botao.targetGraphic = area;
            botao.transition = Selectable.Transition.None;

            Image placa = NovaImagem("Placa", area.transform, new Color(0.5f, 0.52f, 0.52f, 1f));
            Esticar(placa.rectTransform);
            placa.rectTransform.offsetMin = new Vector2(6f, 6f);
            placa.rectTransform.offsetMax = new Vector2(-6f, -6f);
            placa.raycastTarget = false;

            Image desenho = NovaImagem("Desenho", area.transform, Color.white);
            if (desenhosBotoes != null) desenho.sprite = desenhosBotoes[i];
            desenho.preserveAspect = false;
            desenho.raycastTarget = false;
            Esticar(desenho.rectTransform);

            botoes[i] = botao;
            placas[i] = placa;
        }

        // Noite e relógio (canto superior direito)
        Text textoNoite = NovoTexto("TextoNoite", painelJogo, "NOITE 1", 54, TextAnchor.MiddleRight, Color.white);
        Posicionar(textoNoite.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, -34f), new Vector2(500f, 70f));
        Text textoRelogio = NovoTexto("TextoRelogio", painelJogo, "00:00", 68, TextAnchor.MiddleRight, new Color(0.88f, 0.1f, 0.1f));
        Posicionar(textoRelogio.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-70f, -102f), new Vector2(500f, 84f));

        // Vidas e Nota (canto superior esquerdo)
        Image iconeMoeda = NovaImagem("IconeMoeda", painelJogo, Color.white);
        iconeMoeda.sprite = moeda;
        iconeMoeda.raycastTarget = false;
        Posicionar(iconeMoeda.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -30f), new Vector2(64f, 64f));
        Text textoVidas = NovoTexto("TextoVidas", painelJogo, "x3", 46, TextAnchor.MiddleLeft, new Color(1f, 0.9f, 0.3f));
        Posicionar(textoVidas.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(135f, -30f), new Vector2(130f, 64f));
        Text textoNota = NovoTexto("TextoNota", painelJogo, "NOTA 10/10", 40, TextAnchor.MiddleLeft, Color.white);
        Posicionar(textoNota.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(280f, -30f), new Vector2(330f, 64f));

        Image barraNota = NovaImagem("BarraNota", painelJogo, Color.black);
        barraNota.raycastTarget = false;
        Posicionar(barraNota.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(620f, -44f), new Vector2(440f, 36f));
        Image preenchimentoNota = NovaImagem("PreenchimentoNota", barraNota.transform, new Color(1f, 0.1f, 0.1f, 1f));
        preenchimentoNota.sprite = pixel;
        preenchimentoNota.type = Image.Type.Filled;
        preenchimentoNota.fillMethod = Image.FillMethod.Horizontal;
        preenchimentoNota.fillOrigin = 0;
        preenchimentoNota.fillAmount = 1f;
        preenchimentoNota.raycastTarget = false;
        Esticar(preenchimentoNota.rectTransform);
        preenchimentoNota.rectTransform.offsetMin = new Vector2(4f, 4f);
        preenchimentoNota.rectTransform.offsetMax = new Vector2(-4f, -4f);

        Text textoAviso = NovoTexto("TextoAviso", painelJogo, "", 42, TextAnchor.MiddleCenter, Color.white);
        Posicionar(textoAviso.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1300f, 70f));

        // Botão do alarme (canto inferior direito)
        Image imgAlarme;
        Button botaoAlarme = NovoBotao("BotaoAlarme", painelJogo, "APERTAR", 40, Color.white, out imgAlarme);
        imgAlarme.sprite = alarme;
        imgAlarme.preserveAspect = true;
        botaoAlarme.GetComponentInChildren<Text>().color = Color.black;
        Posicionar(imgAlarme.rectTransform, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-165f, 225f), new Vector2(260f, 260f));

        // ---------------- Diálogo
        Image imgDialogo = NovaImagem("PainelDialogo", telaGo.transform, new Color(0f, 0f, 0f, 0.95f));
        Esticar(imgDialogo.rectTransform);
        Button botaoDialogo = imgDialogo.gameObject.AddComponent<Button>();
        botaoDialogo.targetGraphic = imgDialogo;
        botaoDialogo.transition = Selectable.Transition.None;
        Text textoFalante = NovoTexto("TextoFalante", imgDialogo.transform, "SRA. HOYARI", 54, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));
        Posicionar(textoFalante.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(1400f, 80f));
        Text textoFala = NovoTexto("TextoFala", imgDialogo.transform, "", 46, TextAnchor.MiddleCenter, Color.white);
        Posicionar(textoFala.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1400f, 360f));
        Text textoDica = NovoTexto("TextoDica", imgDialogo.transform, "Toque para continuar", 34, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.6f));
        Posicionar(textoDica.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(800f, 60f));

        // ---------------- Resultado da noite
        Image imgResultado = NovaImagem("PainelResultado", telaGo.transform, new Color(0.03f, 0.06f, 0.05f, 1f));
        Esticar(imgResultado.rectTransform);
        Text tituloResultado = NovoTexto("TextoTitulo", imgResultado.transform, "NOITE CONCLUÍDA", 74, TextAnchor.MiddleCenter, new Color(0.6f, 1f, 0.7f));
        Posicionar(tituloResultado.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1500f, 110f));
        Text corpoResultado = NovoTexto("TextoCorpo", imgResultado.transform, "", 46, TextAnchor.MiddleCenter, Color.white);
        Posicionar(corpoResultado.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1500f, 460f));
        Image imgProxima;
        Button botaoProxima = NovoBotao("BotaoPassarNoite", imgResultado.transform, "PASSAR A NOITE", 46, new Color(0.8f, 0f, 0f, 1f), out imgProxima);
        Posicionar(imgProxima.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(560f, 130f));

        // ---------------- Fim de jogo
        Image imgFim = NovaImagem("PainelFimDeJogo", telaGo.transform, new Color(0.12f, 0f, 0f, 1f));
        Esticar(imgFim.rectTransform);
        Text tituloFim = NovoTexto("TextoTitulo", imgFim.transform, "FIM DE JOGO", 96, TextAnchor.MiddleCenter, new Color(1f, 0.2f, 0.2f));
        Posicionar(tituloFim.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1500f, 130f));
        Text corpoFim = NovoTexto("TextoCorpo", imgFim.transform, "", 46, TextAnchor.MiddleCenter, Color.white);
        Posicionar(corpoFim.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1500f, 460f));
        Image imgReiniciar;
        Button botaoReiniciar = NovoBotao("BotaoTentarNovamente", imgFim.transform, "TENTAR NOVAMENTE", 46, new Color(0.8f, 0f, 0f, 1f), out imgReiniciar);
        Posicionar(imgReiniciar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(620f, 130f));

        // ---------------- Vitória
        Image imgVitoria = NovaImagem("PainelVitoria", telaGo.transform, new Color(0.02f, 0.08f, 0.04f, 1f));
        Esticar(imgVitoria.rectTransform);
        Text tituloVitoria = NovoTexto("TextoTitulo", imgVitoria.transform, "VITÓRIA", 96, TextAnchor.MiddleCenter, new Color(0.5f, 1f, 0.6f));
        Posicionar(tituloVitoria.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1500f, 130f));
        Text corpoVitoria = NovoTexto("TextoCorpo", imgVitoria.transform, "", 46, TextAnchor.MiddleCenter, Color.white);
        Posicionar(corpoVitoria.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1500f, 460f));
        Image imgJogarNovamente;
        Button botaoJogarNovamente = NovoBotao("BotaoJogarNovamente", imgVitoria.transform, "JOGAR NOVAMENTE", 46, new Color(0.1f, 0.5f, 0.2f, 1f), out imgJogarNovamente);
        Posicionar(imgJogarNovamente.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -330f), new Vector2(620f, 130f));

        // ---------------- GerenciadorJogo
        var gerGo = new GameObject("GerenciadorJogo");
        var ger = gerGo.AddComponent<GerenciadorJogo>();
        ger.areaInvasores = areaInvasores;
        ger.imagemComodo = imagemComodo;
        ger.imagensComodos = comodos;
        ger.textoCamera = textoCamera;
        ger.textoNomeComodo = textoNomeComodo;
        ger.textoRelogio = textoRelogio;
        ger.textoNoite = textoNoite;
        ger.textoVidas = textoVidas;
        ger.textoNota = textoNota;
        ger.textoAviso = textoAviso;
        ger.preenchimentoNota = preenchimentoNota;
        ger.indicadorGravacao = gravacao;
        ger.sobreposicaoFalha = falha;
        ger.brilhoAlarme = brilho;
        ger.botoesCamera = botoes;
        ger.placasCamera = placas;
        ger.botaoAlarme = botaoAlarme;
        ger.spriteInvasor = invasor;
        ger.painelJogo = painelJogo.gameObject;
        ger.painelDialogo = imgDialogo.gameObject;
        ger.painelResultado = imgResultado.gameObject;
        ger.painelFimDeJogo = imgFim.gameObject;
        ger.painelVitoria = imgVitoria.gameObject;
        ger.botaoDialogo = botaoDialogo;
        ger.textoFalante = textoFalante;
        ger.textoFala = textoFala;
        ger.textoTituloResultado = tituloResultado;
        ger.textoCorpoResultado = corpoResultado;
        ger.botaoProximaNoite = botaoProxima;
        ger.textoCorpoFimDeJogo = corpoFim;
        ger.botaoReiniciar = botaoReiniciar;
        ger.textoCorpoVitoria = corpoVitoria;
        ger.botaoJogarNovamente = botaoJogarNovamente;

        // Deixa só o painel de jogo visível no editor
        imgDialogo.gameObject.SetActive(false);
        imgResultado.gameObject.SetActive(false);
        imgFim.gameObject.SetActive(false);
        imgVitoria.gameObject.SetActive(false);

        PlayerSettings.productName = "Vigília Old Kiri";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        AssetDatabase.SaveAssets();
        Debug.Log("[Vigilia] Cena construída com sucesso.");
    }

    // ------------------------------------------------------------ ajudantes de interface

    static Font PegarFonte()
    {
        if (fonteCache == null) fonteCache = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return fonteCache;
    }

    static RectTransform NovoRect(string nome, Transform pai)
    {
        var go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return go.GetComponent<RectTransform>();
    }

    static void Esticar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void Posicionar(RectTransform rt, Vector2 ancora, Vector2 pivo, Vector2 pos, Vector2 tamanho)
    {
        rt.anchorMin = ancora;
        rt.anchorMax = ancora;
        rt.pivot = pivo;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tamanho;
    }

    static Image NovaImagem(string nome, Transform pai, Color cor)
    {
        RectTransform rt = NovoRect(nome, pai);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = cor;
        return img;
    }

    static Text NovoTexto(string nome, Transform pai, string conteudo, int tamanho, TextAnchor alinhamento, Color cor)
    {
        RectTransform rt = NovoRect(nome, pai);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = PegarFonte();
        t.text = conteudo;
        t.fontSize = tamanho;
        t.alignment = alinhamento;
        t.color = cor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static Button NovoBotao(string nome, Transform pai, string rotulo, int tamanhoFonte, Color fundo, out Image img)
    {
        img = NovaImagem(nome, pai, fundo);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        Text t = NovoTexto("Rotulo", img.transform, rotulo, tamanhoFonte, TextAnchor.MiddleCenter, Color.white);
        Esticar(t.rectTransform);
        return b;
    }

    // ------------------------------------------------------------ sprites

    static Texture2D NovaTextura(int w, int h, Color32[] px)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // Coordenadas com origem no canto superior esquerdo
    static void Preencher(Color32[] px, int w, int h, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                if (x >= 0 && x < w && y >= 0 && y < h) px[(h - 1 - y) * w + x] = c;
    }

    static Texture2D CriarInvasor()
    {
        const int W = 32, H = 48;
        var px = new Color32[W * H];
        var branco = new Color32(255, 255, 255, 255);
        var vermelho = new Color32(255, 0, 0, 255);

        for (int y = 0; y < 19; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = x - 15.5f, dy = y - 9f;
                if (dx * dx + dy * dy <= 8.5f * 8.5f) px[(H - 1 - y) * W + x] = branco;
            }

        Preencher(px, W, H, 14, 17, 18, 22, branco);   // pescoço
        Preencher(px, W, H, 10, 21, 22, 38, branco);   // tronco
        for (float t = 0f; t <= 1f; t += 0.02f)        // braços
        {
            int cx = Mathf.RoundToInt(Mathf.Lerp(9f, 2f, t));
            int cy = Mathf.RoundToInt(Mathf.Lerp(22f, 33f, t));
            Preencher(px, W, H, cx - 1, cy - 1, cx + 3, cy + 2, branco);
            int mx = W - 1 - cx;
            Preencher(px, W, H, mx - 2, cy - 1, mx + 2, cy + 2, branco);
        }
        Preencher(px, W, H, 10, 38, 15, 48, branco);   // pernas
        Preencher(px, W, H, 17, 38, 22, 48, branco);
        Preencher(px, W, H, 12, 8, 14, 10, vermelho);  // olhos
        Preencher(px, W, H, 18, 8, 20, 10, vermelho);
        return NovaTextura(W, H, px);
    }

    static Texture2D CriarBotaoAlarme()
    {
        const int S = 64;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - 31.5f, dy = y - 31.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 31.5f) continue;
                Color c = d > 27f ? new Color(0.26f, 0f, 0f) : d > 23f ? new Color(0.13f, 0.13f, 0.13f) : new Color(0.4f, 0.02f, 0.02f);
                px[y * S + x] = c;
            }
        return NovaTextura(S, S, px);
    }

    static Texture2D CriarMoeda()
    {
        const int S = 64;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - 31.5f, dy = y - 31.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 31.5f) continue;
                Color c = d > 27f ? new Color(0.75f, 0.65f, 0f) : new Color(0.97f, 0.87f, 0.2f);
                px[y * S + x] = c;
            }
        var escuro = new Color32(25, 25, 25, 255);
        Preencher(px, S, S, 30, 14, 34, 50, escuro);
        Preencher(px, S, S, 24, 20, 40, 24, escuro);
        Preencher(px, S, S, 24, 20, 28, 32, escuro);
        Preencher(px, S, S, 24, 30, 40, 34, escuro);
        Preencher(px, S, S, 36, 32, 40, 44, escuro);
        Preencher(px, S, S, 24, 40, 40, 44, escuro);
        return NovaTextura(S, S, px);
    }

    static Texture2D CriarPixel()
    {
        var px = new Color32[16];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        return NovaTextura(4, 4, px);
    }

    static Texture2D CriarCirculo()
    {
        const int S = 64;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x - 31.5f, dy = y - 31.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(31.5f - d);
                px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        return NovaTextura(S, S, px);
    }

    static Texture2D TelaParaTextura(Tela tela)
    {
        var px = new Color32[tela.L * tela.A];
        for (int y = 0; y < tela.A; y++)
            for (int x = 0; x < tela.L; x++)
            {
                Cor c = tela.px[y * tela.L + x];
                px[(tela.A - 1 - y) * tela.L + x] = new Color32(Byte(c.r), Byte(c.g), Byte(c.b), 255);
            }
        return NovaTextura(tela.L, tela.A, px);
    }

    static byte Byte(float v)
    {
        return (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);
    }

    static Sprite SalvarSprite(Texture2D tex, string caminhoAsset, bool comoSprite, FilterMode filtro)
    {
        string completo = Path.Combine(Application.dataPath, caminhoAsset.Substring("Assets/".Length));
        File.WriteAllBytes(completo, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(caminhoAsset, ImportAssetOptions.ForceUpdate);

        var imp = (TextureImporter)AssetImporter.GetAtPath(caminhoAsset);
        imp.textureType = comoSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
        if (comoSprite)
        {
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 100f;
        }
        imp.filterMode = filtro;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(caminhoAsset);
    }

    // Corta a folha de botões do jogador (CAM 1 a CAM 6) em 6 sprites, cada um com ID próprio
    static Sprite[] FatiarBotoes()
    {
        var imp = AssetImporter.GetAtPath(CaminhoBotoes) as TextureImporter;
        if (imp == null) return null;
        var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(CaminhoBotoes);
        if (textura == null) return null;
        int alturaTextura = textura.height;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = 100f;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        var fabrica = new SpriteDataProviderFactories();
        fabrica.Init();
        ISpriteEditorDataProvider provedor = fabrica.GetSpriteEditorDataProviderFromObject(imp);
        provedor.InitSpriteEditorDataProvider();

        // posição (x, y a partir do topo) de cada botão na folha; cada um tem 124x69 px
        int[,] posicoes = { { 2, 27 }, { 130, 27 }, { 2, 155 }, { 130, 155 }, { 2, 283 }, { 130, 283 } };
        var retangulos = new SpriteRect[6];
        var pares = new List<SpriteNameFileIdPair>();
        for (int i = 0; i < 6; i++)
        {
            var r = new SpriteRect();
            r.name = "BotaoCam" + (i + 1);
            r.rect = new Rect(posicoes[i, 0], alturaTextura - (posicoes[i, 1] + 69), 124, 69);
            r.alignment = SpriteAlignment.Center;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.spriteID = GUID.Generate();
            retangulos[i] = r;
            pares.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
        }
        provedor.SetSpriteRects(retangulos);
        var nomes = provedor.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nomes != null) nomes.SetNameFileIdPairs(pares);
        provedor.Apply();
        imp.SaveAndReimport();

        var resultado = new Sprite[6];
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(CaminhoBotoes))
        {
            var s = o as Sprite;
            if (s == null) continue;
            for (int i = 0; i < 6; i++)
                if (s.name == "BotaoCam" + (i + 1)) resultado[i] = s;
        }
        return resultado;
    }
}
#endif
