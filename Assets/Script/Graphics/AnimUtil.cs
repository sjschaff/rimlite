namespace BB
{
    public static class AnimUtil
    {
        public static float EaseOut(float t)
        {
            float a = (1 - t);
            return 1 - a * a * a * a;
        }
    }
}
