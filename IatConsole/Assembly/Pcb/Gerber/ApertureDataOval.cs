namespace IatConsole.Assembly.Pcb.Gerber
{
    public class ApertureDataOval : ApertureData
    {
        public float Width { get; }

        public float Height { get; }

        public ApertureDataOval(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public override ApertureType GetApertureType()
        {
            return ApertureType.Oval;
        }
    }
}
