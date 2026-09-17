#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class JingaiSceneBuilder
{
    private const string CharacterPath = "Assets/Scripts/Data/C02/Charas/C02_Alicia.asset";
    private const string MessagePrefabPath = "Assets/Prefab/MessageBubble.prefab";

    [MenuItem("Tools/Jingai Match/Rebuild Editable Scenes")]
    public static void BuildAllScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(CharacterPath);
        ChatMessageView messagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MessagePrefabPath)?.GetComponent<ChatMessageView>();
        if (character == null)
        {
            Debug.LogError("Alicia CharacterData was not found: " + CharacterPath);
            return;
        }
        if (messagePrefab == null)
        {
            Debug.LogError("MessageBubble prefab was not found: " + MessagePrefabPath);
            return;
        }

        Scene previous = SceneManager.GetActiveScene();
        BuildScene("Assets/Scenes/MatchingScene.unity", scene => BuildMatching(scene, character));
        BuildScene("Assets/Scenes/ChatScene.unity", scene => BuildChat(scene, character, messagePrefab));
        BuildScene("Assets/Scenes/EndingScene.unity", BuildEnding);
        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        AssetDatabase.SaveAssets();
        Debug.Log("Editable MatchingScene, ChatScene and EndingScene were rebuilt.");
    }

    private static void BuildScene(string path, System.Action<Scene> build)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool wasLoaded = scene.IsValid() && scene.isLoaded;
        if (!wasLoaded) scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        foreach (GameObject root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
        build(scene);
        EditorSceneManager.SaveScene(scene, path);
        if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
    }

    private static void BuildMatching(Scene scene, CharacterData character)
    {
        MatchingSceneController controller = new GameObject("MatchingSceneController").AddComponent<MatchingSceneController>();
        RectTransform canvas = SceneUiFactory.CreateCanvas("MatchingCanvas");
        TMP_Text title = SceneUiFactory.Text("Title", canvas, "JINGAI MATCH", 42f);
        SceneUiFactory.SetRect(title.rectTransform, new Vector2(0f, .92f), Vector2.one, new Vector2(40, 0), new Vector2(-40, -30));
        RectTransform card = SceneUiFactory.PanelObject("CharacterCard", canvas, SceneUiFactory.Panel);
        SceneUiFactory.SetRect(card, new Vector2(.07f, .19f), new Vector2(.93f, .9f), Vector2.zero, Vector2.zero);
        Image portrait = SceneUiFactory.PanelObject("Portrait", card, new Color(.18f, .14f, .22f, 1)).GetComponent<Image>();
        SceneUiFactory.SetRect(portrait.rectTransform, new Vector2(.04f, .36f), new Vector2(.96f, .96f), Vector2.zero, Vector2.zero);
        TMP_Text name = SceneUiFactory.Text("CharacterName", card, character.CharacterName, 62, TextAlignmentOptions.Left);
        SceneUiFactory.SetRect(name.rectTransform, new Vector2(.07f, .24f), new Vector2(.93f, .35f), Vector2.zero, Vector2.zero);
        TMP_Text summary = SceneUiFactory.Text("Summary", card, "PROFILE SUMMARY", 30, TextAlignmentOptions.Left);
        SceneUiFactory.SetRect(summary.rectTransform, new Vector2(.07f, .16f), new Vector2(.93f, .24f), Vector2.zero, Vector2.zero);
        Button pass = SceneUiFactory.Button("PassButton", canvas, "×", SceneUiFactory.Muted);
        SceneUiFactory.SetRect((RectTransform)pass.transform, new Vector2(.12f, .06f), new Vector2(.39f, .15f), Vector2.zero, Vector2.zero);
        Button like = SceneUiFactory.Button("LikeButton", canvas, "♥ LIKE", SceneUiFactory.Accent);
        SceneUiFactory.SetRect((RectTransform)like.transform, new Vector2(.44f, .06f), new Vector2(.88f, .15f), Vector2.zero, Vector2.zero);

        TMP_Text details = SceneUiFactory.Text("ProfileDetails", card, "PROFILE DETAILS", 34, TextAlignmentOptions.TopLeft);
        SceneUiFactory.SetRect(details.rectTransform, new Vector2(.07f, .03f), new Vector2(.93f, .15f), Vector2.zero, Vector2.zero);
        controller.ConfigureView(character, canvas, portrait, name, summary, details, pass, like);
        UnityEventTools.AddVoidPersistentListener(pass.onClick, controller.Pass);
        UnityEventTools.AddVoidPersistentListener(like.onClick, controller.Match);
        EditorUtility.SetDirty(controller);
    }

    private static void BuildChat(Scene scene, CharacterData character, ChatMessageView messagePrefab)
    {
        ChatSceneController controller = new GameObject("ChatSceneController").AddComponent<ChatSceneController>();
        RectTransform canvas = SceneUiFactory.CreateCanvas("ChatCanvas");
        RectTransform header = SceneUiFactory.PanelObject("Header", canvas, SceneUiFactory.Panel);
        SceneUiFactory.SetRect(header, new Vector2(0, .88f), Vector2.one, Vector2.zero, Vector2.zero);
        TMP_Text name = SceneUiFactory.Text("CharacterName", header, character.CharacterName, 42, TextAlignmentOptions.Left);
        SceneUiFactory.SetRect(name.rectTransform, new Vector2(.05f, .18f), new Vector2(.55f, .88f), Vector2.zero, Vector2.zero);
        TMP_Text honesty = SceneUiFactory.Text("Honesty", header, "HONESTY  --", 28, TextAlignmentOptions.Right);
        SceneUiFactory.SetRect(honesty.rectTransform, new Vector2(.55f, .5f), new Vector2(.95f, .88f), Vector2.zero, Vector2.zero);
        TMP_Text wordCount = SceneUiFactory.Text("Words", header, "WORDS  --", 24, TextAlignmentOptions.Right);
        SceneUiFactory.SetRect(wordCount.rectTransform, new Vector2(.55f, .12f), new Vector2(.95f, .5f), Vector2.zero, Vector2.zero);

        RectTransform viewport = SceneUiFactory.PanelObject("MessageViewport", canvas, new Color(.075f, .06f, .095f, 1));
        SceneUiFactory.SetRect(viewport, new Vector2(.03f, .29f), new Vector2(.97f, .88f), Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = CreateVerticalContent("MessageContent", viewport, 14, new RectOffset(24, 24, 24, 24));
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 30f;

        RectTransform commands = SceneUiFactory.PanelObject("CommandPanel", canvas, SceneUiFactory.Panel);
        SceneUiFactory.SetRect(commands, Vector2.zero, new Vector2(1, .29f), Vector2.zero, Vector2.zero);
        TMP_Text topic = SceneUiFactory.Text("Topic", commands, "会話を選択してください", 28);
        SceneUiFactory.SetRect(topic.rectTransform, new Vector2(.04f, .73f), new Vector2(.96f, .96f), Vector2.zero, Vector2.zero);
        Button aizuchi = FixedButton("AizuchiButton", commands, "相槌", new Vector2(.04f, .39f), new Vector2(.32f, .7f), SceneUiFactory.Muted);
        Button deepDive = FixedButton("DeepDiveButton", commands, "深掘り", new Vector2(.36f, .39f), new Vector2(.64f, .7f), SceneUiFactory.Muted);
        Button smallTalk = FixedButton("SmallTalkButton", commands, "話題変更", new Vector2(.68f, .39f), new Vector2(.96f, .7f), SceneUiFactory.Muted);

        RectTransform overlay = SceneUiFactory.PanelObject("InvitationOverlay", canvas, new Color(.03f, .02f, .04f, .98f));
        SceneUiFactory.Stretch(overlay);
        TMP_Text inviteHeading = SceneUiFactory.Text("InvitationHeading", overlay, "場所: ---　行動: ---\n目的: ---", 40);
        SceneUiFactory.SetRect(inviteHeading.rectTransform, new Vector2(.08f, .78f), new Vector2(.92f, .94f), Vector2.zero, Vector2.zero);
        RectTransform wordListPanel = SceneUiFactory.PanelObject("WordListPanel", overlay, SceneUiFactory.Panel);
        SceneUiFactory.SetRect(wordListPanel, new Vector2(.08f, .2f), new Vector2(.92f, .76f), Vector2.zero, Vector2.zero);
        RectTransform wordList = CreateVerticalContent("WordList", wordListPanel, 12, new RectOffset(20, 20, 20, 20));
        Button decide = FixedButton("InvitationDecideButton", overlay, "このプランで誘う", new Vector2(.17f, .07f), new Vector2(.83f, .16f), SceneUiFactory.Accent);
        controller.ConfigureView(character, canvas, scroll, content, messagePrefab,
            aizuchi, deepDive, smallTalk, overlay.gameObject, wordList, inviteHeading, decide);
        UnityEventTools.AddVoidPersistentListener(aizuchi.onClick, controller.ChooseAizuchi);
        UnityEventTools.AddVoidPersistentListener(deepDive.onClick, controller.ChooseDeepDive);
        UnityEventTools.AddVoidPersistentListener(smallTalk.onClick, controller.ChooseSmallTalk);
        UnityEventTools.AddVoidPersistentListener(decide.onClick, controller.CompleteInvitation);
        EditorUtility.SetDirty(controller);
    }

    private static void BuildEnding(Scene scene)
    {
        EndingSceneController controller = new GameObject("EndingSceneController").AddComponent<EndingSceneController>();
        RectTransform canvas = SceneUiFactory.CreateCanvas("EndingCanvas");
        TMP_Text eyebrow = SceneUiFactory.Text("Eyebrow", canvas, "ENDING & RESULT", 30);
        SceneUiFactory.SetRect(eyebrow.rectTransform, new Vector2(.08f, .87f), new Vector2(.92f, .95f), Vector2.zero, Vector2.zero);
        RectTransform panel = SceneUiFactory.PanelObject("EndingPanel", canvas, SceneUiFactory.Panel);
        SceneUiFactory.SetRect(panel, new Vector2(.07f, .2f), new Vector2(.93f, .86f), Vector2.zero, Vector2.zero);
        TMP_Text title = SceneUiFactory.Text("EndingTitle", panel, "ENDING TITLE", 58);
        SceneUiFactory.SetRect(title.rectTransform, new Vector2(.08f, .72f), new Vector2(.92f, .92f), Vector2.zero, Vector2.zero);
        TMP_Text body = SceneUiFactory.Text("EndingBody", panel, "EndingData の本文がここに表示されます。", 34, TextAlignmentOptions.TopLeft);
        SceneUiFactory.SetRect(body.rectTransform, new Vector2(.1f, .08f), new Vector2(.9f, .68f), Vector2.zero, Vector2.zero);
        Button retry = FixedButton("RetryButton", canvas, "もう一度遊ぶ", new Vector2(.15f, .07f), new Vector2(.85f, .15f), SceneUiFactory.Accent);
        controller.ConfigureView(canvas, title, body, retry);
        EditorUtility.SetDirty(controller);
    }

    private static Button FixedButton(string name, Transform parent, string label, Vector2 min, Vector2 max, Color color)
    {
        Button button = SceneUiFactory.Button(name, parent, label, color);
        SceneUiFactory.SetRect((RectTransform)button.transform, min, max, Vector2.zero, Vector2.zero);
        return button;
    }

    private static RectTransform CreateVerticalContent(string name, Transform parent, float spacing, RectOffset padding)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = go.GetComponent<VerticalLayoutGroup>();
        layout.padding = padding; layout.spacing = spacing; layout.childControlHeight = true; layout.childControlWidth = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }
}
#endif
