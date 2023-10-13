using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TxtBoxController : MonoBehaviour
{
    public TextMeshProUGUI barText;
    public TextMeshProUGUI personNameText;

    private int sentenceIndex = -1;
    private StoryScene currentScene;
    private State state = State.COMPLETED;
    private Animator animator;
    private bool isHidden = false;

    private Dictionary<Speaker, SpriteController> sprites;
    public GameObject spritesPrefab;

    private bool skipText = false;

    private enum State
    {
        PLAYING, COMPLETED
    }

    private void Start()
    {
        sprites = new Dictionary<Speaker, SpriteController>();
        animator = GetComponent<Animator>();
    }

    // skip txt
    private void Update()
    {
        // Check for input to skip text
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && !IsCompleted())
        {
            skipText = true;
        }
        if (Input.GetKeyDown(KeyCode.Space) && !IsCompleted())
        {
            skipText = true;
        }
    }

    public int GetSentenceIndex()
    {
        return sentenceIndex;
    }

    public void SetSentenceIndex(int sentenceIndex)
    {
        this.sentenceIndex = sentenceIndex;
    }

    public void Hide()
    {
        if (!isHidden)
        {
            animator.SetTrigger("Hide");
            isHidden = true;
        }
    }

    public void Show()
    {
        animator.SetTrigger("Show");
        isHidden = false;
    }

    public void ClearText()
    {
        barText.text = "";
    }

    public void PlayScene(StoryScene scene)
    {
        currentScene = scene;
        sentenceIndex = -1;
        PlayNextSentence();
    }


    public void PlayNextSentence()
    {
        StartCoroutine(TypeText(currentScene.sentences[++sentenceIndex].text));
        personNameText.text = currentScene.sentences[sentenceIndex].speaker.speakerName;
        personNameText.color = currentScene.sentences[sentenceIndex].speaker.textColor;
        ActSpeakers();
    }

    public void GoBack()
    {
        sentenceIndex--;
        StopTyping();
        HideSprites();
        PlaySentence(false);
    }

    public bool IsCompleted()
    {
        return state == State.COMPLETED;
    }

    public bool IsLastSentence()
    {
        return sentenceIndex + 1 == currentScene.sentences.Count;
    }

    public bool IsFirstSentence()
    {
        return sentenceIndex == 0;
    }

    public void StopTyping()
    {
        state = State.COMPLETED;
        StopCoroutine(typingCoroutine);
    }

    public void HideSprites()
    {
        while (spritesPrefab.transform.childCount > 0)
        {
            DestroyImmediate(spritesPrefab.transform.GetChild(0).gameObject);
        }
        sprites.Clear();
    }

    private void PlaySentence(bool isAnimated = true)
    {
        StoryScene.Sentence sentence = currentScene.sentences[sentenceIndex];
        typingCoroutine = StartCoroutine(TypeText(sentence.text));
        personNameText.text = sentence.speaker.speakerName;
        personNameText.color = sentence.speaker.textColor;
        if (sentence.audio)
        {
            voicePlayer.clip = sentence.audio;
            voicePlayer.Play();
        }
        else
        {
            voicePlayer.Stop();
        }
        ActSpeakers(isAnimated);
    }

    private IEnumerator TypeText(string text)
    {
        barText.text = "";
        state = State.PLAYING;
        int wordIndex = 0;  

        while (state != State.COMPLETED)
        {
            barText.text += text[wordIndex];

            if (skipText) // Check if skipText flag is set
            {
                // If skipping, display the entire text immediately
                barText.text = text;
                state = State.COMPLETED;
                break;
            }

            yield return new WaitForSeconds(0.05f);

            if (++wordIndex == text.Length)
            {
                state = State.COMPLETED;
                break;
            }
        }

        // Reset the skipText flag after the text is displayed
        skipText = false;
        
    }

    private void ActSpeakers()
    {
        List<StoryScene.Sentence.Action> actions = currentScene.sentences[sentenceIndex].actions;
        for (int i = 0; i < actions.Count; i++)
        {
            ActSpeaker(actions[i]);
        }
    }

    private void ActSpeaker(StoryScene.Sentence.Action action)
    {
        SpriteController controller = null;
        switch (action.actionType)
        {
            case StoryScene.Sentence.Action.Type.APPEAR:
                if (!sprites.ContainsKey(action.speaker))
                {
                    controller = Instantiate(action.speaker.prefab.gameObject, spritesPrefab.transform)
                        .GetComponent<SpriteController>();
                    sprites.Add(action.speaker, controller);
                }
                else
                {
                    controller = sprites[action.speaker];
                }
                controller.Setup(action.speaker.sprites[action.spriteIndex]);
                controller.Show(action.coords);
                return;
            case StoryScene.Sentence.Action.Type.MOVE:
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                    controller.Move(action.coords, action.moveSpeed);
                }
                break;
            case StoryScene.Sentence.Action.Type.DISAPPEAR:
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                    controller.Hide();
                }
                break;
            case StoryScene.Sentence.Action.Type.NONE:
                if (sprites.ContainsKey(action.speaker))
                {
                    controller = sprites[action.speaker];
                }
                break;
        }
        if (controller != null)
        {
            controller.SwitchSprite(action.speaker.sprites[action.spriteIndex]);
        }
    }
}
