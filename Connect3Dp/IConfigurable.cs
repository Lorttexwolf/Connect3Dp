namespace Connect3Dp
{
    // https://github.com/greghesp/ha-bambulab/issues/1448
    // https://www.youtube.com/watch?v=44s4A_yNPOw

    public interface IConfigurable
    {
        public object GetConfiguration();

        public static abstract object MakeWithConfiguration(object configuration);
    }
}
