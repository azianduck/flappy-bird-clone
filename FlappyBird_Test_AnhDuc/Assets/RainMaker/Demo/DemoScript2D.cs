using UnityEngine;
using System.Collections;

namespace DigitalRuby.RainMaker
{
    public class DemoScript2D : MonoBehaviour
    {
        public RainScript2D RainScript;
        public float rainIntensity;
        private bool isRain;

        private void Start()
        {
            RainScript.RainIntensity = rainIntensity;
            RainScript.CollisionMask = -1;
            // RainScript.EnableWind = true;
            RainToggleChanged();
        }

        private void Update()
        {
            Vector3 worldBottomLeft = Camera.main.ViewportToWorldPoint(Vector3.zero);
            Vector3 worldTopRight = Camera.main.ViewportToWorldPoint(Vector3.one);
            float visibleWorldWidth = worldTopRight.x - worldBottomLeft.x;

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                Camera.main.transform.Translate(Time.deltaTime * -(visibleWorldWidth * 0.1f), 0.0f, 0.0f);
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                Camera.main.transform.Translate(Time.deltaTime * (visibleWorldWidth * 0.1f), 0.0f, 0.0f);
            }
        }


        public void RainToggleChanged()
        {
            if (isRain)
            {
                RainScript.RainIntensity = 0;
                isRain = false;
            }
            else
            {
                RainScript.RainIntensity = rainIntensity;
                isRain = true;
            }
        }


    }
}