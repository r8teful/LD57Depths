using UnityEngine;

public class AudioParams
{
    [System.Serializable]
    public class Pitch
    {
        public enum Variation
        {
            Small,
            Medium,
            Large,
            VerySmall,
        }

        public float pitch;

        public Pitch(float exact)
        {
            pitch = exact;
        }

        public Pitch(float minRandom, float maxRandom)
        {
            pitch = Random.Range(minRandom, maxRandom);
        }

        public Pitch(Variation randomVariation,float offset = 0)
        {
            switch (randomVariation)
            {
                case Variation.VerySmall:
                    pitch = Random.Range(0.95f, 1.05f);
                    pitch += offset;
                    break;
                case Variation.Small:
                    pitch = Random.Range(0.9f, 1.1f);
                    pitch += offset;
                    break;
                case Variation.Medium:
                    pitch = Random.Range(0.75f, 1.25f);
                    pitch += offset;
                    break;
                case Variation.Large:
                    pitch = Random.Range(0.5f, 1.5f);
                    pitch += offset;
                    break;
            }
        }
    }

    [System.Serializable]
    public class Repetition
    {
        public float minRepetitionFrequency;
        public string entryId;

        public Repetition(float minRepetitionFrequency, string entryId = "")
        {
            this.minRepetitionFrequency = minRepetitionFrequency;
            this.entryId = entryId;
        }
    }

    [System.Serializable]
    public class Randomization
    {
        public bool noRepeating;

        public Randomization(bool noRepeating = true)
        {
            this.noRepeating = noRepeating;
        }
    }

    [System.Serializable]
    public class Distortion
    {
        public bool muffled;
        public float cutoff;
        
        public bool reverb;
        public AudioReverbPreset reverbPreset;

        public Distortion(bool muffled = false, bool reverb = false, AudioReverbPreset reverbPreset = AudioReverbPreset.Alley) {
            this.muffled = muffled;
            this.reverb = reverb;
            this.reverbPreset = reverbPreset;
        }
        public Distortion(float cutoff, bool reverb = false) {
            muffled = true;
            this.reverb = reverb;
            this.cutoff = cutoff;
        }

    }
   
}
