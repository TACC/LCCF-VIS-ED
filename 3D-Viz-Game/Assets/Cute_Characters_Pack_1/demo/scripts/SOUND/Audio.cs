using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace My.DemoScene
{

    public class UIAudioManager : MonoBehaviour
    {
        public Graphic uiElement;

        public AudioSource backgroundMusicSource;
        public AudioSource clickSoundSource;

        public AudioClip backgroundMusic;
        public AudioClip clickSound;

        [Range(0f, 1f)] public float backgroundVolume = 1f;
        [Range(0f, 1f)] public float clickVolume = 1f;
        [Range(0f, 1f)] public float mutedAlpha = 0.3f;
        public float fadeInDuration = 2f;

        public float rightClickPitchMin = 0.8f;
        public float rightClickPitchMax = 1.2f;

        public List<GameObject> clickSoundTargets = new List<GameObject>();

        private bool isMuted = false;
        private bool isFading = false;
        private HashSet<GameObject> registeredObjects = new HashSet<GameObject>();

        void Start()
        {
            if (backgroundMusicSource != null && backgroundMusic != null)
            {
                backgroundMusicSource.clip = backgroundMusic;
                backgroundMusicSource.loop = true;
                backgroundMusicSource.playOnAwake = false;
                backgroundMusicSource.Play();

                if (fadeInDuration > 0f)
                {
                    backgroundMusicSource.volume = 0f;
                    StartCoroutine(FadeIn());
                }
            }

            if (uiElement != null)
            {
                registeredObjects.Add(uiElement.gameObject);

                EventTrigger trigger = uiElement.gameObject.GetComponent<EventTrigger>()
                    ?? uiElement.gameObject.AddComponent<EventTrigger>();

                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                entry.callback.AddListener((_) => OnMuteClicked());
                trigger.triggers.Add(entry);
            }

            foreach (var target in clickSoundTargets)
            {
                if (target == null) continue;
                CollectWithParents(target);
            }
        }

        void CollectWithParents(GameObject obj)
        {
            TryRegister(obj);
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                TryRegister(parent.gameObject);
                parent = parent.parent;
            }
        }

        void TryRegister(GameObject obj)
        {
            bool hasGraphicRaycaster = obj.GetComponent<GraphicRaycaster>() != null;
            bool hasGraphic = obj.GetComponent<Graphic>() != null;
            if (hasGraphic || hasGraphicRaycaster)
                registeredObjects.Add(obj);
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                GameObject clicked = GetClickedObject();
                if (clicked != null && registeredObjects.Contains(clicked))
                    PlayClickSound(false);
            }

            if (Input.GetMouseButtonDown(1))
            {
                GameObject clicked = GetClickedObject();
                if (clicked != null && registeredObjects.Contains(clicked))
                    PlayClickSound(true);
            }

            if (backgroundMusicSource != null && !isMuted && !isFading)
                backgroundMusicSource.volume = backgroundVolume;

            if (clickSoundSource != null)
                clickSoundSource.volume = clickVolume;
        }

        GameObject GetClickedObject()
        {
            if (EventSystem.current == null) return null;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
                return results[0].gameObject;

            return null;
        }

        IEnumerator FadeIn()
        {
            isFading = true;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                backgroundMusicSource.volume = Mathf.Lerp(0f, backgroundVolume, elapsed / fadeInDuration);
                yield return null;
            }
            backgroundMusicSource.volume = backgroundVolume;
            isFading = false;
        }

        void OnMuteClicked()
        {
            isMuted = !isMuted;

            if (backgroundMusicSource != null)
                backgroundMusicSource.volume = isMuted ? 0f : backgroundVolume;

            if (uiElement != null)
            {
                Color c = uiElement.color;
                c.a = isMuted ? mutedAlpha : 1f;
                uiElement.color = c;
            }

            PlayClickSound(false);
        }

        void PlayClickSound(bool randomPitch)
        {
            if (clickSoundSource == null || clickSound == null) return;

            clickSoundSource.pitch = randomPitch
                ? Random.Range(rightClickPitchMin, rightClickPitchMax)
                : 1f;

            clickSoundSource.PlayOneShot(clickSound, clickVolume);
        }
    }
}