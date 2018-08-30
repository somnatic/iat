namespace IatConsole.Assembly.Pcb.Gerber
{
    public abstract class ApertureData
    {
        public enum ApertureType { Circle, Rectangular, Oval };

        public abstract ApertureType GetApertureType();
    }
}
