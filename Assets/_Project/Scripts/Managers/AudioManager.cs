using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PizzaTycoon.Utils;

namespace PizzaTycoon.Managers
{
    // Gerencia todos os sons e músicas do jogo com pool de AudioSources.
    //
    // CORREÇÕES (v2):
    //   1. Adicionado campo _customerAngrySFX e método PlayCustomerAngry().
    //   2. Adicionado campo _itemDepositSFX e método PlayItemDeposit().
    //   3. GetClip() agora é case-insensitive (ToLowerInvariant).
    //   4. ReturnToPoolAfterPlay usa WaitForSeconds com guard de null.
    //   5. PlaySFX retorna silenciosamente se clip == null (sem erro de console).
    [DefaultExecutionOrder(-50)]
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("Configurações")]
        [SerializeField] private int _sfxPoolSize = 10;
        [SerializeField] [Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume    = 1f;
        [SerializeField] [Range(0f, 1f)] private float _musicVolume  = 0.6f;

        [Header("Música")]
        [SerializeField] private AudioClip _backgroundMusic;

        [Header("SFX — Gameplay")]
        [SerializeField] private AudioClip _coinPickupSFX;
        [SerializeField] private AudioClip _itemCollectSFX;
        [SerializeField] private AudioClip _itemDepositSFX;      // ← NOVO
        [SerializeField] private AudioClip _pizzaReadySFX;
        [SerializeField] private AudioClip _customerHappySFX;
        [SerializeField] private AudioClip _customerAngrySFX;    // ← NOVO

        [Header("SFX — UI")]
        [SerializeField] private AudioClip _buttonClickSFX;
        [SerializeField] private AudioClip _upgradePurchaseSFX;

        private AudioSource _musicSource;
        private Queue<AudioSource> _sfxPool = new Queue<AudioSource>();

        // ── Unity lifecycle ─────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            SetupMusicSource();
            SetupSFXPool();
            LoadVolumeSettings();
        }

        private void Start()
        {
            PlayMusic(_backgroundMusic);
        }

        // ── Setup ────────────────────────────────────────────────────────────

        private void SetupMusicSource()
        {
            _musicSource             = gameObject.AddComponent<AudioSource>();
            _musicSource.loop        = true;
            _musicSource.playOnAwake = false;
        }

        private void SetupSFXPool()
        {
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                var source           = gameObject.AddComponent<AudioSource>();
                source.playOnAwake   = false;
                _sfxPool.Enqueue(source);
            }
        }

        private void LoadVolumeSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            _sfxVolume    = PlayerPrefs.GetFloat("SFXVolume",    1f);
            _musicVolume  = PlayerPrefs.GetFloat("MusicVolume",  0.6f);
            ApplyVolumes();
        }

        // ── Música ───────────────────────────────────────────────────────────

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null || _musicSource == null) return;
            if (_musicSource.clip == clip) return;
            _musicSource.clip   = clip;
            _musicSource.volume = _musicVolume * _masterVolume;
            _musicSource.Play();
        }

        // ── SFX genérico ─────────────────────────────────────────────────────

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;

            var source  = GetAvailableSFXSource();
            source.clip = clip;
            source.volume = _sfxVolume * _masterVolume * volumeScale;
            source.Play();
            StartCoroutine(ReturnToPoolAfterPlay(source, clip.length));
        }

        // ── Atalhos para SFXs comuns ─────────────────────────────────────────

        public void PlayCoinPickup()       => PlaySFX(_coinPickupSFX);
        public void PlayItemCollect()      => PlaySFX(_itemCollectSFX);
        public void PlayItemDeposit()      => PlaySFX(_itemDepositSFX);     // ← NOVO
        public void PlayPizzaReady()       => PlaySFX(_pizzaReadySFX);
        public void PlayCustomerHappy()    => PlaySFX(_customerHappySFX);
        public void PlayCustomerAngry()    => PlaySFX(_customerAngrySFX);   // ← NOVO
        public void PlayButtonClick()      => PlaySFX(_buttonClickSFX);
        public void PlayUpgradePurchase()  => PlaySFX(_upgradePurchaseSFX);

        // ── Volume ───────────────────────────────────────────────────────────

        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("MasterVolume", _masterVolume);
            ApplyVolumes();
        }

        public void SetSFXVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("SFXVolume", _sfxVolume);
        }

        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("MusicVolume", _musicVolume);
            if (_musicSource != null)
                _musicSource.volume = _musicVolume * _masterVolume;
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
                _musicSource.volume = _musicVolume * _masterVolume;
        }

        // Propriedades de volume para PauseMenuUI e MainMenuUI
        public float MasterVolume => _masterVolume;
        public float SFXVolume    => _sfxVolume;
        public float MusicVolume  => _musicVolume;

        // ── GetClip (case-insensitive) ────────────────────────────────────────

        /// <summary>
        /// Retorna um AudioClip por nome curto.
        /// Aceita qualquer capitalização: "coin", "Coin", "COIN" são equivalentes.
        /// </summary>
        public AudioClip GetClip(string clipName)
        {
            // CORREÇÃO: case-insensitive para evitar bug com "Click" vs "click"
            return clipName.ToLowerInvariant() switch
            {
                "coin"     => _coinPickupSFX,
                "collect"  => _itemCollectSFX,
                "deposit"  => _itemDepositSFX,
                "pizza"    => _pizzaReadySFX,
                "happy"    => _customerHappySFX,
                "angry"    => _customerAngrySFX,
                "click"    => _buttonClickSFX,
                "upgrade"  => _upgradePurchaseSFX,
                _          => _buttonClickSFX
            };
        }

        // ── Pool interno ─────────────────────────────────────────────────────

        private AudioSource GetAvailableSFXSource()
        {
            if (_sfxPool.Count > 0)
                return _sfxPool.Dequeue();

            // Expande pool se necessário
            var source           = gameObject.AddComponent<AudioSource>();
            source.playOnAwake   = false;
            return source;
        }

        // CORREÇÃO: guard de null para o caso do AudioManager ser destruído
        // antes do coroutine terminar (ex: troca de cena).
        private IEnumerator ReturnToPoolAfterPlay(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (source == null) yield break;
            source.Stop();
            _sfxPool.Enqueue(source);
        }
    }
}