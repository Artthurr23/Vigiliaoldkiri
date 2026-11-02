#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: Vigilia > Ligar Audios
// Procura os áudios em Assets/Audios e liga cada um ao GerenciadorJogo pelo nome do arquivo.
public static class LigadorAudios
{
    const string PastaAudios = "Assets/Audios";

    [MenuItem("Vigilia/Ligar Audios")]
    public static void Ligar()
    {
        var ger = Object.FindFirstObjectByType<GerenciadorJogo>();
        if (ger == null)
        {
            Debug.LogWarning("[Vigilia] GerenciadorJogo não encontrado na cena. Rode antes: Vigilia > Construir Cena.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { PastaAudios });
        var clipes = new AudioClip[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            clipes[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[i]));

        ger.somAmbiente = Achar(clipes, "ambient", "drone", "ambiente");
        ger.somCliqueCamera = Achar(clipes, "camera", "switch", "clique");
        ger.somAlarmeCerto = Achar(clipes, "alarm", "sirene", "siren");
        ger.somAlarmeFalso = Achar(clipes, "buzzer", "wrong", "falso");
        ger.somInvasorEscapou = Achar(clipes, "sting", "horror", "escapou");
        ger.somFimDaNoite = Achar(clipes, "morning", "fim_noite", "fimnoite");
        ger.somDerrota = Achar(clipes, "gameover", "game-over", "derrota");
        ger.somVitoria = Achar(clipes, "victory", "vitoria");
        ger.somToqueDialogo = Achar(clipes, "tap", "button", "dialogo");

        // O áudio de fundo é grande: deixa comprimido na memória (melhor para celular)
        if (ger.somAmbiente != null)
        {
            string caminho = AssetDatabase.GetAssetPath(ger.somAmbiente);
            var imp = AssetImporter.GetAtPath(caminho) as AudioImporter;
            if (imp != null)
            {
                AudioImporterSampleSettings cfg = imp.defaultSampleSettings;
                cfg.loadType = AudioClipLoadType.CompressedInMemory;
                imp.defaultSampleSettings = cfg;
                imp.SaveAndReimport();
            }
        }

        EditorUtility.SetDirty(ger);
        EditorSceneManager.MarkSceneDirty(ger.gameObject.scene);
        EditorSceneManager.SaveScene(ger.gameObject.scene);

        Debug.Log("[Vigilia] Áudios ligados: ambiente=" + Nome(ger.somAmbiente) + " | camera=" + Nome(ger.somCliqueCamera) +
                  " | alarme=" + Nome(ger.somAlarmeCerto) + " | falso=" + Nome(ger.somAlarmeFalso) +
                  " | escapou=" + Nome(ger.somInvasorEscapou) + " | fimNoite=" + Nome(ger.somFimDaNoite) +
                  " | derrota=" + Nome(ger.somDerrota) + " | vitoria=" + Nome(ger.somVitoria) +
                  " | dialogo=" + Nome(ger.somToqueDialogo));
    }

    static AudioClip Achar(AudioClip[] clipes, params string[] chaves)
    {
        foreach (AudioClip c in clipes)
        {
            if (c == null) continue;
            string nome = c.name.ToLower();
            foreach (string chave in chaves)
                if (nome.Contains(chave)) return c;
        }
        return null;
    }

    static string Nome(AudioClip c)
    {
        return c == null ? "(não achou)" : c.name;
    }
}
#endif
