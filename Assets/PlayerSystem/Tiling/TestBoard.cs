using UnityEngine;

namespace PlayerSystem.Tiling
{
    public class TestBoard:Board
    {
        protected override Vector3 cellPos2Real(int x, int y)
        {
            Debug.Log((x, y));
            return new Vector3(-490+x*80, -150+y*80, 0);
        }
    }
}