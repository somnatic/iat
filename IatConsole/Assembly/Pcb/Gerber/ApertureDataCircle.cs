namespace IatConsole.Assembly.Pcb.Gerber
{
    public class ApertureDataCircle : ApertureData
    {
        public ApertureDataCircle(float diameter)
        {
            Diameter = diameter;
        }

        public float Diameter { get; }

        public override ApertureType GetApertureType()
        {
            return ApertureType.Circle;
        }
    }
}
