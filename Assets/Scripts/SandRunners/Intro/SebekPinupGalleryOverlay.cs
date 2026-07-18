using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SebekPinupGalleryOverlay : MonoBehaviour
{
    public string resourceFolder = "SandRunners/Art/Pinups/Sebek";
    public bool showPrototypeHint = true;

    private Texture2D[] cards;
    private int currentIndex;
    private bool isOpen;
    private GUIStyle titleStyle;
    private GUIStyle captionStyle;
    private GUIStyle hintStyle;

    private void Awake()
    {
        LoadCards();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.gKey.wasPressedThisFrame)
            isOpen = !isOpen;
        if (!isOpen)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
            isOpen = false;
        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
            Step(1);
        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
            Step(-1);
    }

    private void LoadCards()
    {
        cards = Resources.LoadAll<Texture2D>(resourceFolder);
        if (cards == null)
        {
            cards = Array.Empty<Texture2D>();
            return;
        }

        Array.Sort(cards, (left, right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
    }

    private void Step(int direction)
    {
        if (cards == null || cards.Length == 0)
            return;

        currentIndex = (currentIndex + direction + cards.Length) % cards.Length;
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (!isOpen)
        {
            if (showPrototypeHint)
                GUI.Label(new Rect(Screen.width - 260f, 18f, 230f, 28f), "G: Sebek archive", hintStyle);
            return;
        }

        DrawOverlayBackground(new Color(0.015f, 0.012f, 0.01f, 0.86f));

        if (cards == null || cards.Length == 0)
        {
            GUI.Label(new Rect(32f, 32f, 620f, 40f), "Sebek archive is empty.", titleStyle);
            return;
        }

        Texture2D card = cards[Mathf.Clamp(currentIndex, 0, cards.Length - 1)];
        Rect imageRect = FitTextureRect(card, new Rect(46f, 54f, Screen.width - 92f, Screen.height - 138f));
        GUI.DrawTexture(imageRect, card, ScaleMode.ScaleToFit, true);

        GUI.Label(new Rect(46f, 18f, Screen.width - 92f, 30f), "Sebek Archive " + (currentIndex + 1) + " / " + cards.Length, titleStyle);
        GUI.Label(new Rect(46f, Screen.height - 70f, Screen.width - 92f, 44f), CaptionFor(card.name), captionStyle);
    }

    private static Rect FitTextureRect(Texture2D texture, Rect bounds)
    {
        float textureAspect = texture.width / (float)Mathf.Max(1, texture.height);
        float boundsAspect = bounds.width / Mathf.Max(1f, bounds.height);

        if (textureAspect > boundsAspect)
        {
            float height = bounds.width / textureAspect;
            return new Rect(bounds.x, bounds.y + (bounds.height - height) * 0.5f, bounds.width, height);
        }

        float width = bounds.height * textureAspect;
        return new Rect(bounds.x + (bounds.width - width) * 0.5f, bounds.y, width, bounds.height);
    }

    private static void DrawOverlayBackground(Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private string CaptionFor(string cardName)
    {
        if (cardName.Contains("FirstRadioHook"))
            return "Radio hook / bathroom threshold reference.";
        if (cardName.Contains("PunkCorridor"))
            return "Combat-adjacent costume and corridor attitude reference.";
        if (cardName.Contains("Vanity"))
            return "Vanity, mirror, jewelry, and private-room prop reference.";
        if (cardName.Contains("RedEmpress"))
            return "Later rival / alternate imperial mood card.";
        if (cardName.Contains("ExteriorPyramid"))
            return "Exterior pyramid posture and sunset palette card.";
        if (cardName.Contains("Prisoner"))
            return "Prisoner-hook tone card.";
        if (cardName.Contains("BathroomExit"))
            return "Bathroom exit / towel costume beat.";
        if (cardName.Contains("WakingBed"))
            return "Waking bedroom mood reference.";
        if (cardName.Contains("SleepingBed"))
            return "Sleeping bedroom mood reference.";
        if (cardName.Contains("BlackDress"))
            return "Black nightwear / vanity costume reference.";
        if (cardName.Contains("BackCharacter"))
            return "Back-view costume reference.";
        if (cardName.Contains("Chariot"))
            return "Later victory/loading card.";
        if (cardName.Contains("NightPool"))
            return "Private bath / moonlit pool mood reference.";
        if (cardName.Contains("BedroomLuxury"))
            return "Luxury bedroom lighting and fabric reference.";

        return cardName;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.84f, 0.45f, 1f) }
        };

        captionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(0.95f, 0.9f, 0.78f, 1f) }
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperRight,
            normal = { textColor = new Color(0.94f, 0.82f, 0.55f, 0.86f) }
        };
    }
}
