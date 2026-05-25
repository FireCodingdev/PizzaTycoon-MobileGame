using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace PizzaTycoon.Editor
{
    // Gera clips de áudio procedurais (beeps WAV) para todos os SFX do jogo
    // Menu: PizzaTycoon > 3.5 Generate Audio
    // Os arquivos .wav são gravados em Assets/_Project/Audio/SFX/ e importados automaticamente
    public static class AudioClipGenerator
    {
        private const string SFX_PATH   = "Assets/_Project/Audio/SFX";
        private const string MUSIC_PATH = "Assets/_Project/Audio/Music";
        private const int    SAMPLE_RATE = 44100;

        [MenuItem("PizzaTycoon/3.5 Generate Audio Clips")]
        public static void GenerateAll()
        {
            EnsureDirectory(SFX_PATH);
            EnsureDirectory(MUSIC_PATH);

            int count = 0;

            // Tom agudo curto — coleta de item (440Hz, 0.10s)
            if (SaveWav("SFX_ItemCollect",  SFX_PATH, GenerateSine(440f, 0.10f))) count++;

            // Tom médio — depositar item (330Hz, 0.15s)
            if (SaveWav("SFX_ItemDeposit",  SFX_PATH, GenerateSine(330f, 0.15f))) count++;

            // Acorde positivo — venda bem-sucedida (523+659Hz, 0.30s)
            if (SaveWav("SFX_Coin",         SFX_PATH, GenerateChord(new[]{ 523f, 659f, 784f }, 0.30f))) count++;

            // Tom descendente — cliente insatisfeito (300→180Hz, 0.40s)
            if (SaveWav("SFX_CustomerAngry",SFX_PATH, GenerateSweep(300f, 180f, 0.40f))) count++;

            // Tick curto — clique de UI (600Hz, 0.05s)
            if (SaveWav("SFX_Click",        SFX_PATH, GenerateSine(600f, 0.05f))) count++;

            // Fanfarra curta — upgrade comprado (ascendente, 3 notas)
            if (SaveWav("SFX_Upgrade",      SFX_PATH, GenerateFanfare(0.45f))) count++;

            // Tom suave — pizza pronta (C5+E5, 0.25s)
            if (SaveWav("SFX_PizzaReady",   SFX_PATH, GenerateChord(new[]{ 523f, 659f }, 0.25f))) count++;

            // Tom grave — cliente feliz (220Hz sweep up, 0.20s)
            if (SaveWav("SFX_CustomerHappy",SFX_PATH, GenerateSweep(330f, 523f, 0.20f))) count++;

            // Música de fundo simples — loop de 4 notas (gerada como drone)
            if (SaveWav("Music_Background", MUSIC_PATH, GenerateDrone(0.4f, 8.0f))) count++;

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Pizza Tycoon — Audio",
                $"✅ {count} clips de áudio gerados!\n\n" +
                "Arraste os clips para o AudioManager no Inspector:\n" +
                "• SFX_ItemCollect → Item Collect SFX\n" +
                "• SFX_Coin → Coin Pickup SFX\n" +
                "• SFX_PizzaReady → Pizza Ready SFX\n" +
                "• SFX_CustomerHappy → Customer Happy SFX\n" +
                "• SFX_Click → Button Click SFX\n" +
                "• SFX_Upgrade → Upgrade Purchase SFX\n" +
                "• Music_Background → Background Music",
                "OK");
        }

        // ══════════════════════════════════════════════════════════════════════
        // GERADORES DE SINAL DE ÁUDIO
        // ══════════════════════════════════════════════════════════════════════

        // Onda senoidal simples com envelope ADSR simplificado
        private static float[] GenerateSine(float frequency, float duration)
        {
            int samples = Mathf.RoundToInt(duration * SAMPLE_RATE);
            float[] data = new float[samples];
            float attackSamples  = SAMPLE_RATE * 0.005f;  // 5ms attack
            float releaseSamples = SAMPLE_RATE * 0.03f;   // 30ms release

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = ApplyEnvelope(i, samples, attackSamples, releaseSamples);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.7f;
            }
            return data;
        }

        // Múltiplas frequências sobrepostas (acorde)
        private static float[] GenerateChord(float[] frequencies, float duration)
        {
            int samples = Mathf.RoundToInt(duration * SAMPLE_RATE);
            float[] data = new float[samples];
            float attackSamples  = SAMPLE_RATE * 0.01f;
            float releaseSamples = SAMPLE_RATE * 0.08f;
            float amplitude = 0.5f / frequencies.Length;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float envelope = ApplyEnvelope(i, samples, attackSamples, releaseSamples);
                float sample = 0f;
                foreach (float freq in frequencies)
                    sample += Mathf.Sin(2f * Mathf.PI * freq * t);
                data[i] = sample * amplitude * envelope;
            }
            return data;
        }

        // Sweep de frequência (glissando)
        private static float[] GenerateSweep(float startFreq, float endFreq, float duration)
        {
            int samples = Mathf.RoundToInt(duration * SAMPLE_RATE);
            float[] data = new float[samples];
            float attackSamples  = SAMPLE_RATE * 0.008f;
            float releaseSamples = SAMPLE_RATE * 0.05f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float ratio = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, ratio);
                float envelope = ApplyEnvelope(i, samples, attackSamples, releaseSamples);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.65f;
            }
            return data;
        }

        // Fanfarra — 3 notas ascendentes em sequência
        private static float[] GenerateFanfare(float totalDuration)
        {
            float[] notes = { 523f, 659f, 784f }; // C5, E5, G5
            float noteDuration = totalDuration / notes.Length;
            int totalSamples = Mathf.RoundToInt(totalDuration * SAMPLE_RATE);
            float[] data = new float[totalSamples];

            for (int n = 0; n < notes.Length; n++)
            {
                int start = Mathf.RoundToInt(n * noteDuration * SAMPLE_RATE);
                int end   = Mathf.RoundToInt((n + 1) * noteDuration * SAMPLE_RATE);
                end = Mathf.Min(end, totalSamples);
                int noteSamples = end - start;
                float attackS  = noteSamples * 0.05f;
                float releaseS = noteSamples * 0.15f;

                for (int i = 0; i < noteSamples; i++)
                {
                    float t = (float)(start + i) / SAMPLE_RATE;
                    float env = ApplyEnvelope(i, noteSamples, attackS, releaseS);
                    data[start + i] = Mathf.Sin(2f * Mathf.PI * notes[n] * t) * env * 0.6f;
                }
            }
            return data;
        }

        // Drone/pad para música de fundo (acorde de várias oitavas + leve modulação)
        private static float[] GenerateDrone(float baseFreq, float duration)
        {
            int samples = Mathf.RoundToInt(duration * SAMPLE_RATE);
            float[] data = new float[samples];
            float[] harmonics = { 1f, 2f, 3f, 4f }; // fundamental + overtones
            float[] amplitudes = { 0.4f, 0.2f, 0.1f, 0.05f };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                // Fade in/out suave para loop contínuo
                float fadeLen = SAMPLE_RATE * 0.3f;
                float fade = 1f;
                if (i < fadeLen) fade = i / fadeLen;
                else if (i > samples - fadeLen) fade = (samples - i) / fadeLen;

                float sample = 0f;
                for (int h = 0; h < harmonics.Length; h++)
                {
                    float freq = baseFreq * harmonics[h];
                    // Vibrato leve
                    float vibrato = 1f + 0.003f * Mathf.Sin(2f * Mathf.PI * 5f * t);
                    sample += Mathf.Sin(2f * Mathf.PI * freq * vibrato * t) * amplitudes[h];
                }
                data[i] = sample * fade;
            }
            return data;
        }

        // Envelope ADSR simplificado (attack / sustain / release)
        private static float ApplyEnvelope(int sampleIndex, int totalSamples,
            float attackSamples, float releaseSamples)
        {
            if (sampleIndex < attackSamples)
                return sampleIndex / attackSamples;
            if (sampleIndex > totalSamples - releaseSamples)
                return (totalSamples - sampleIndex) / releaseSamples;
            return 1f;
        }

        // ══════════════════════════════════════════════════════════════════════
        // ENCODER WAV
        // ══════════════════════════════════════════════════════════════════════

        // Salva array de float[-1,1] como arquivo .wav em disco; retorna true se criado
        private static bool SaveWav(string name, string directory, float[] samples)
        {
            string filePath = Path.Combine(directory, $"{name}.wav");
            if (File.Exists(filePath)) return false;

            byte[] wav = EncodeWav(samples, 1, SAMPLE_RATE);
            File.WriteAllBytes(filePath, wav);
            return true;
        }

        // Encoda amostras float para bytes WAV PCM 16-bit
        private static byte[] EncodeWav(float[] samples, int channels, int sampleRate)
        {
            int bitsPerSample  = 16;
            int byteRate       = sampleRate * channels * (bitsPerSample / 8);
            int blockAlign     = channels * (bitsPerSample / 8);
            int dataSize       = samples.Length * blockAlign;
            int fileSize       = 44 + dataSize;

            byte[] wav = new byte[fileSize];
            int offset = 0;

            // RIFF header
            WriteBytes(wav, ref offset, System.Text.Encoding.ASCII.GetBytes("RIFF"));
            WriteInt32(wav, ref offset, fileSize - 8);
            WriteBytes(wav, ref offset, System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            WriteBytes(wav, ref offset, System.Text.Encoding.ASCII.GetBytes("fmt "));
            WriteInt32(wav, ref offset, 16);             // chunk size
            WriteInt16(wav, ref offset, 1);              // PCM format
            WriteInt16(wav, ref offset, (short)channels);
            WriteInt32(wav, ref offset, sampleRate);
            WriteInt32(wav, ref offset, byteRate);
            WriteInt16(wav, ref offset, (short)blockAlign);
            WriteInt16(wav, ref offset, (short)bitsPerSample);

            // data chunk
            WriteBytes(wav, ref offset, System.Text.Encoding.ASCII.GetBytes("data"));
            WriteInt32(wav, ref offset, dataSize);

            // Sample data (converte float → Int16)
            foreach (float s in samples)
            {
                short pcm = (short)Mathf.Clamp(s * 32767f, -32768f, 32767f);
                WriteInt16(wav, ref offset, pcm);
            }

            return wav;
        }

        private static void WriteBytes(byte[] buf, ref int offset, byte[] data)
        {
            Buffer.BlockCopy(data, 0, buf, offset, data.Length);
            offset += data.Length;
        }

        private static void WriteInt32(byte[] buf, ref int offset, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(bytes, 0, buf, offset, 4);
            offset += 4;
        }

        private static void WriteInt16(byte[] buf, ref int offset, short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(bytes, 0, buf, offset, 2);
            offset += 2;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
