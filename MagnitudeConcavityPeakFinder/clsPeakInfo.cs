
namespace MagnitudeConcavityPeakFinder
{
    public class clsPeak
    {
        public int LocationIndex { get; set; }
        public int LeftEdge { get; set; }
        public int RightEdge { get; set; }
        public double Area { get; set; }
        public bool IsValid { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsPeak() : this(0)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public clsPeak(int locationIndex)
        {
            if (locationIndex < 0)
                locationIndex = 0;

            LocationIndex = locationIndex;
            LeftEdge = locationIndex;
            RightEdge = locationIndex;
        }

        /// <summary>
        /// Peak width (in points)
        /// </summary>
        public int PeakWidth => RightEdge - LeftEdge + 1;

        public new string ToString()
        {
            return "Center index " + LocationIndex + ", from " + LeftEdge + " to " + RightEdge + "; Area " + Area.ToString("0");
        }
    }
}
