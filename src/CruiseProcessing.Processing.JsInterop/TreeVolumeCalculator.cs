
using CruiseProcessing.Processing.Models;
using Microsoft.JavaScript.NodeApi;

namespace CruiseProcessing.Processing
{
    [JSExport]
    public class TreeVolumeCalculator
    {
        public static NgTreeVolume CalculateTreeVolume(
            NgCruiseInfo cruiseInfo, NgTreeInfo tree, NgUtilizationInfo utilizationValues, IReadOnlyCollection<NgLogInfo> logs)
        {
            return new NgTreeVolume()
            {
                TreeID = tree.TreeID,
            };
        }
    }
}
