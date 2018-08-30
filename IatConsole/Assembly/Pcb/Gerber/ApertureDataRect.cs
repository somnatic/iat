namespace IatConsole.Assembly.Pcb.Gerber
{
    public class ApertureDataRect : ApertureData
    {
        public ApertureDataRect(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public float Width { get; }

        public float Height { get; }

        public override ApertureType GetApertureType()
        {
            return ApertureType.Rectangular;
        }
    }
}
