using CruiseProcessing.Data;
using CruiseProcessing.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace CruiseProcessing.Test.Processing
{
    public class CalculateTreeValuesTestBase : TestBase
    {
        public CalculateTreeValuesTestBase(ITestOutputHelper output) : base(output)
        {
        }

        public enum CompareCalculateTreeValueFlags
        {
            None = 0,
            IgnoreBiomass = 1,
        }

        protected static void ReprocessCruise(CpDataLayer dataLayer, ICalculateTreeValues ctv)
        {
            var strata = dataLayer.GetStrata();

            dataLayer.DeleteLogStock();
            dataLayer.deleteTreeCalculatedValues();
            dataLayer.DAL.BeginTransaction();
            foreach (var st in strata)
            {
                ctv.ProcessTrees(st.Code, st.Method, st.Stratum_CN.Value);
            }
            dataLayer.DAL.CommitTransaction();
        }
    }
}
