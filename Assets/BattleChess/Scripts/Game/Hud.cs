using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BattleChess.Core;

namespace BattleChess
{
    /// <summary>
    /// The whole user interface, built in code with uGUI so there is nothing to wire up in the editor.
    /// Scales from portrait phones to landscape TVs (CanvasScaler + safe area).
    /// Replace with your own designed UI later — GameController only uses the public methods below.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        static readonly Color Panel = new Color(0.14f, 0.11f, 0.08f, 0.92f);
        static readonly Color Ink = new Color(0.95f, 0.91f, 0.85f);
        static readonly Color Muted = new Color(0.66f, 0.59f, 0.5f);
        static readonly Color Accent = new Color(0.91f, 0.71f, 0.36f);
        static readonly Color Alert = new Color(0.89f, 0.33f, 0.25f);

        GameController game;
        ThemeDefinition theme;
        Font font;
        RectTransform safe, topBar, bottomBar;
        Text title, mode, status, whiteName, blackName, whiteCap, blackCap;
        Image whiteDot, blackDot;
        Button undoBtn, newBtn;
        Text camLabel, soundLabel;
        GameObject newPanel, promoPanel, endPanel;
        Text endTitle, endSub;
        Action endAgain;
        Rect lastSafe;

        public static Font LoadFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            return f;
        }

        public void Bind(GameController g, ThemeDefinition th)
        {
            game = g; theme = th;
            font = LoadFont();
            EnsureEventSystem();
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = Screen.width > Screen.height ? 1f : 0f;

            safe = R("Safe", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            ApplySafeArea();

            // top bar
            topBar = R("Top", safe, new Vector2(0, 1), Vector2.one, new Vector2(0, -230), Vector2.zero);
            Img(topBar, new Color(0, 0, 0, 0.35f));
            title = Txt(topBar, th.title, 46, Ink, TextAnchor.MiddleLeft, new Vector2(0, 0.55f), Vector2.one, new Vector2(36, 0), new Vector2(-300, 0));
            title.fontStyle = FontStyle.Bold;
            mode = Txt(topBar, "", 30, Muted, TextAnchor.MiddleRight, new Vector2(0, 0.55f), Vector2.one, new Vector2(0, 0), new Vector2(-36, 0));
            whiteDot = Dot(topBar, new Vector2(0.03f, 0.27f), th.white.color);
            whiteName = Txt(topBar, th.white.name, 34, Ink, TextAnchor.MiddleLeft, new Vector2(0.07f, 0.05f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            whiteCap = Txt(topBar, "", 28, Accent, TextAnchor.MiddleLeft, new Vector2(0.07f, 0.0f), new Vector2(0.5f, 0.2f), Vector2.zero, Vector2.zero);
            blackDot = Dot(topBar, new Vector2(0.53f, 0.27f), th.black.color);
            blackName = Txt(topBar, th.black.name, 34, Ink, TextAnchor.MiddleLeft, new Vector2(0.57f, 0.05f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
            blackCap = Txt(topBar, "", 28, Accent, TextAnchor.MiddleLeft, new Vector2(0.57f, 0.0f), new Vector2(1f, 0.2f), Vector2.zero, Vector2.zero);

            // bottom bar
            bottomBar = R("Bottom", safe, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 300));
            Img(bottomBar, new Color(0, 0, 0, 0.35f));
            status = Txt(bottomBar, "", 40, Ink, TextAnchor.MiddleCenter, new Vector2(0, 0.62f), Vector2.one, new Vector2(20, 0), new Vector2(-20, 0));
            var row = R("Buttons", bottomBar, Vector2.zero, new Vector2(1, 0.62f), new Vector2(20, 20), new Vector2(-20, -10));
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 16; hl.childForceExpandWidth = true; hl.childForceExpandHeight = true; hl.childControlWidth = true; hl.childControlHeight = true;
            newBtn = Btn(row, "New", () => ShowNewGame());
            undoBtn = Btn(row, "Undo", () => game.Undo());
            Btn(row, "Rotate", () => game.Rotate());
            var cb = Btn(row, "Action cam: on", () => { bool on = game.ToggleCinematic(); camLabel.text = on ? "Action cam: on" : "Action cam: off"; });
            camLabel = cb.GetComponentInChildren<Text>();
            var sb = Btn(row, "Sound: on", () => { bool on = game.ToggleSound(); soundLabel.text = on ? "Sound: on" : "Sound: off"; });
            soundLabel = sb.GetComponentInChildren<Text>();

            BuildNewGamePanel();
            BuildPromoPanel();
            BuildEndPanel();
        }

        void Update()
        {
            if (safe != null && Screen.safeArea != lastSafe) ApplySafeArea();
        }

        void ApplySafeArea()
        {
            lastSafe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var sa = Screen.safeArea;
            safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            var scaler = safe.GetComponentInParent<CanvasScaler>();
            if (scaler != null) scaler.matchWidthOrHeight = Screen.width > Screen.height ? 1f : 0f;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        // ---------------- public API used by GameController ----------------
        public bool IsBlocking(Vector2 screenPos)
        {
            if ((newPanel != null && newPanel.activeSelf) || (promoPanel != null && promoPanel.activeSelf) || (endPanel != null && endPanel.activeSelf)) return true;
            return RectTransformUtility.RectangleContainsScreenPoint(topBar, screenPos, null) ||
                   RectTransformUtility.RectangleContainsScreenPoint(bottomBar, screenPos, null);
        }

        public void SetStatus(string text, bool alert) { status.text = text; status.color = alert ? Alert : Ink; }
        public void SetMode(string text) { mode.text = text; }

        public void SetTurn(bool whiteToMove, bool over)
        {
            whiteName.color = !over && whiteToMove ? Accent : Ink;
            blackName.color = !over && !whiteToMove ? Accent : Ink;
            whiteDot.transform.localScale = Vector3.one * (!over && whiteToMove ? 1.3f : 1f);
            blackDot.transform.localScale = Vector3.one * (!over && !whiteToMove ? 1.3f : 1f);
        }

        public void SetCaptured(GameState s)
        {
            int[] start = { 0, 8, 2, 2, 2, 1, 0 };
            int[] w = new int[7], b = new int[7]; int mw = 0, mb = 0;
            foreach (var p in s.Board) { if (p.Empty || p.T == PType.K) continue; if (p.White) { w[(int)p.T]++; mw += Chess.Value(p.T); } else { b[(int)p.T]++; mb += Chess.Value(p.T); } }
            int capByWhite = 0, capByBlack = 0;
            for (int t = 1; t <= 5; t++) { capByWhite += Mathf.Max(0, start[t] - b[t]); capByBlack += Mathf.Max(0, start[t] - w[t]); }
            int adv = (mw - mb) / 100;
            whiteCap.text = capByWhite > 0 ? "Captured " + capByWhite + (adv > 0 ? "   +" + adv : "") : "";
            blackCap.text = capByBlack > 0 ? "Captured " + capByBlack + (adv < 0 ? "   +" + (-adv) : "") : "";
        }

        public void SetButtons(bool canUndo, bool canNew) { undoBtn.interactable = canUndo; newBtn.interactable = canNew; }

        public void SetToggles(bool cinematic, bool sound)
        {
            camLabel.text = cinematic ? "Action cam: on" : "Action cam: off";
            soundLabel.text = sound ? "Sound: on" : "Sound: off";
        }

        // ---------------- new game ----------------
        GameSettings pending;
        readonly List<Action> refreshers = new List<Action>();

        void BuildNewGamePanel()
        {
            newPanel = Overlay("NewGame");
            var card = Card(newPanel.transform, 0.62f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 40, 40); v.spacing = 18; v.childForceExpandHeight = false; v.childControlHeight = true; v.childControlWidth = true;
            Line(card, "New battle", 56, Ink, FontStyle.Bold, 80);
            Line(card, theme.white.name + " vs " + theme.black.name, 34, Muted, FontStyle.Normal, 50);
            Line(card, "OPPONENT", 28, Muted, FontStyle.Bold, 44);
            Options(card, new[] { "Computer", "Friend (same device)" }, () => pending.vsComputer ? 0 : 1, i => pending.vsComputer = i == 0);
            Line(card, "DIFFICULTY", 28, Muted, FontStyle.Bold, 44);
            Options(card, new[] { "Easy", "Medium", "Hard" }, () => pending.level - 1, i => pending.level = i + 1);
            Line(card, "COMMAND", 28, Muted, FontStyle.Bold, 44);
            Options(card, new[] { theme.white.name + " (first)", theme.black.name }, () => pending.humanWhite ? 0 : 1, i => pending.humanWhite = i == 0);
            var start = Btn(card, "To battle!", () => { newPanel.SetActive(false); game.NewGame(pending); }, Accent, new Color(0.16f, 0.11f, 0.05f));
            start.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
            var cancel = Btn(card, "Cancel", () => newPanel.SetActive(false), new Color(0, 0, 0, 0), Muted);
            cancel.gameObject.AddComponent<LayoutElement>().preferredHeight = 90;
            newPanel.SetActive(false);
        }

        public void ShowNewGame()
        {
            pending = game.Settings.Copy();
            foreach (var r in refreshers) r();
            newPanel.SetActive(true);
        }

        void Options(RectTransform parent, string[] labels, Func<int> get, Action<int> set)
        {
            var row = R("Options", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12; hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true;
            var buttons = new List<Button>();
            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                Button b = null;
                b = Btn(row, labels[i], () => { set(idx); foreach (var r in refreshers) r(); });
                buttons.Add(b);
            }
            Action refresh = () =>
            {
                if (pending == null) return;
                int cur = get();
                for (int i = 0; i < buttons.Count; i++)
                {
                    buttons[i].GetComponent<Image>().color = i == cur ? Accent : new Color(0.1f, 0.08f, 0.06f, 1f);
                    buttons[i].GetComponentInChildren<Text>().color = i == cur ? new Color(0.16f, 0.11f, 0.05f) : Muted;
                }
            };
            refreshers.Add(refresh);
        }

        // ---------------- promotion ----------------
        Action<PType> promoCallback;
        Text[] promoLabels;
        void BuildPromoPanel()
        {
            promoPanel = Overlay("Promotion");
            var card = Card(promoPanel.transform, 0.36f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 40, 40); v.spacing = 16; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            Line(card, "Promote your pawn", 50, Ink, FontStyle.Bold, 80);
            var types = new[] { PType.Q, PType.R, PType.B, PType.N };
            promoLabels = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                var t = types[i];
                var b = Btn(card, "", () => { promoPanel.SetActive(false); if (promoCallback != null) promoCallback(t); });
                b.gameObject.AddComponent<LayoutElement>().preferredHeight = 110;
                promoLabels[i] = b.GetComponentInChildren<Text>();
            }
            promoPanel.SetActive(false);
        }

        public void AskPromotion(bool white, Action<PType> cb)
        {
            promoCallback = cb;
            var types = new[] { PType.Q, PType.R, PType.B, PType.N };
            for (int i = 0; i < 4; i++) promoLabels[i].text = theme.Get(types[i], white).displayName + "  ·  " + GameController.RoleName(types[i]);
            promoPanel.SetActive(true);
        }

        // ---------------- end ----------------
        void BuildEndPanel()
        {
            endPanel = Overlay("End");
            var card = Card(endPanel.transform, 0.36f);
            var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(40, 40, 50, 40); v.spacing = 16; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            endTitle = Line(card, "", 70, Ink, FontStyle.Bold, 110);
            endSub = Line(card, "", 36, Muted, FontStyle.Normal, 70);
            var again = Btn(card, "Fight again", () => { endPanel.SetActive(false); if (endAgain != null) endAgain(); }, Accent, new Color(0.16f, 0.11f, 0.05f));
            again.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
            var view = Btn(card, "View battlefield", () => endPanel.SetActive(false), new Color(0, 0, 0, 0), Muted);
            view.gameObject.AddComponent<LayoutElement>().preferredHeight = 90;
            endPanel.SetActive(false);
        }

        public void ShowEnd(string t, string s, Action again, Action view)
        {
            endTitle.text = t; endSub.text = s; endAgain = again;
            endPanel.SetActive(true);
        }

        public void HideEnd() { if (endPanel != null) endPanel.SetActive(false); }

        // ---------------- tiny UI toolkit ----------------
        static RectTransform R(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
            return rt;
        }

        static Image Img(RectTransform rt, Color c) { var i = rt.gameObject.AddComponent<Image>(); i.color = c; return i; }

        Text Txt(RectTransform parent, string text, int size, Color c, TextAnchor anchor, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var rt = R("Text", parent, aMin, aMax, oMin, oMax);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font; t.text = text; t.fontSize = size; t.color = c; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        Text Line(RectTransform parent, string text, int size, Color c, FontStyle style, float height)
        {
            var t = Txt(parent, text, size, c, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            t.fontStyle = style;
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return t;
        }

        Image Dot(RectTransform parent, Vector2 anchor, Color c)
        {
            var rt = R("Dot", parent, anchor, anchor, new Vector2(-18, -18), new Vector2(18, 18));
            return Img(rt, c);
        }

        Button Btn(RectTransform parent, string label, Action onClick) { return Btn(parent, label, onClick, new Color(0.14f, 0.11f, 0.08f, 0.95f), Ink); }

        Button Btn(RectTransform parent, string label, Action onClick, Color bg, Color fg)
        {
            var rt = R("Button", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var img = Img(rt, bg);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => onClick());
            var t = Txt(rt, label, 32, fg, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(6, 0), new Vector2(-6, 0));
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 18; t.resizeTextMaxSize = 34;
            return b;
        }

        GameObject Overlay(string name)
        {
            var rt = R(name, safe, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bg = Img(rt, new Color(0.04f, 0.03f, 0.02f, 0.65f));
            bg.raycastTarget = true;
            return rt.gameObject;
        }

        RectTransform Card(Transform parent, float heightFraction)
        {
            var rt = R("Card", parent, new Vector2(0.06f, 0.5f - heightFraction / 2), new Vector2(0.94f, 0.5f + heightFraction / 2), Vector2.zero, Vector2.zero);
            Img(rt, Panel);
            return rt;
        }
    }
}
