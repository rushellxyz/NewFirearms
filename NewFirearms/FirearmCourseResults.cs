using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NewFirearms
{
    // Created without UI; displays final results only, including after a scene change.
    public class FirearmCourseResults : MonoBehaviour
    {
        private Text title, status, score;

        internal static string FormatTime(float seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
            return ((int)time.TotalMinutes).ToString("00") + ":" + time.Seconds.ToString("00");
        }

        private void BuildResultsPanel()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
            gameObject.AddComponent<GraphicRaycaster>();
            var panel = new GameObject("Course results", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, -16f);
            rect.sizeDelta = new Vector2(480f, 190f);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.04f, 0.92f);
            panel.GetComponent<Image>().raycastTarget = false;
            title = MakeText(panel.transform, "Title", 12f, 28f);
            status = MakeText(panel.transform, "Objective", 44f, 58f);
            score = MakeText(panel.transform, "Score", 105f, 28f);
            title.text = ModLocale.Course("ui.resultsTitle");
            Button leave = MakeButton(panel.transform, ModLocale.Course("ui.return"), 12f);
            leave.onClick.AddListener(() =>
            {
                if (TutorialHandler.main != null) TutorialHandler.main.EndCourse();
                SceneManager.LoadScene("PreGen");
                Destroy(gameObject);
            });
            Button dismiss = MakeButton(panel.transform, ModLocale.Course("ui.dismiss"), 245f);
            dismiss.onClick.AddListener(() => Destroy(gameObject));
        }

        private static Text MakeText(Transform parent, string name, float top, float height)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -top);
            rect.sizeDelta = new Vector2(456f, height);
            Text text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 19;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(Transform parent, string caption, float left)
        {
            var obj = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -145f);
            rect.sizeDelta = new Vector2(220f, 32f);
            obj.GetComponent<Image>().color = new Color(0.23f, 0.23f, 0.23f);
            Text label = MakeText(obj.transform, "Label", 3f, 26f);
            label.rectTransform.sizeDelta = new Vector2(196f, 26f);
            label.text = caption;
            return obj.GetComponent<Button>();
        }

        internal void Show(string reason, float survived, float highscore)
        {
            if (title == null) BuildResultsPanel();
            title.text = ModLocale.Course("ui.resultsTitle");
            status.text = ModLocale.Course("ui.result", reason, FormatTime(survived));
            score.text = ModLocale.Course("ui.best", FormatTime(highscore));
        }
    }
}
