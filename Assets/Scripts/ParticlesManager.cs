using UnityEngine;
using System.Collections;
namespace MatrixParticle
{
    public class ParticlesManager : MonoBehaviour
    {
        public MatrixParticles particle;
        public AudioSource audio;
        private float[] data = new float[64];
        void Update()
        {
            audio.GetSpectrumData(data, 0, FFTWindow.BlackmanHarris);
            float max = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (max < data[i])
                    max = data[i];
            }
            //Debug.Log(max);
            if (max > 0.08)
            {
                Rect rect = new Rect(Vector2.zero, new Vector2(80f, 50f));
                Vector3 pos = new Vector3(Random.Range(rect.xMin, rect.xMax), Random.Range(rect.yMin, rect.yMax), 0);
                particle.EmitParticles(pos, 80* max);
            }
        }
    }
}